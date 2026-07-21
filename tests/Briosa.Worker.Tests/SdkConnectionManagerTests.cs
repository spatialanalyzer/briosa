using Briosa.Worker.Sdk;
using Briosa.Worker.Testing;

namespace Briosa.Worker.Tests;

public sealed class SdkConnectionManagerTests
{
    [Fact]
    public async Task ConnectedOwnerCapturesDiagnosticsAndExecutesOnItsSingleStaAdapter()
    {
        var plan = new ScriptedSdkPlan()
            .ConnectsSuccessfully(statusCode: 23)
            .Then(ScriptedExecution.Success());
        var manager = CreateManager("sa-lab", plan, maximumAttempts: 3);
        try
        {
            var connection = await manager.ConnectAsync();
            var request = await manager.ExecuteAsync(new SdkCommand("connected-operation"));

            Assert.Equal(SdkConnectionState.Connected, connection.State);
            Assert.Equal("sa-lab", connection.TargetHost);
            Assert.Equal(23, connection.StatusCode);
            Assert.Equal(1, connection.Attempt);
            Assert.Equal(3, connection.MaximumAttempts);
            Assert.Equal(SdkRequestStatus.Completed, request.Status);
            Assert.True(request.Execution!.MpResult.Succeeded);
            Assert.Equal(["sa-lab"], plan.ConnectionHosts);
            Assert.Equal(1, plan.AdapterCreationCount);
            Assert.Equal(1, plan.MaximumActiveAdapterCount);
            Assert.Equal(ApartmentState.STA, plan.AdapterApartmentState);
        }
        finally
        {
            await manager.DisposeAsync();
        }

        Assert.Equal(1, plan.AdapterDisposalCount);
        Assert.Equal(ApartmentState.STA, plan.AdapterDisposalApartmentState);
    }

    [Fact]
    public async Task WorkIsRejectedWithStableOutcomeInEveryNonConnectedState()
    {
        using var gate = new ManualResetEventSlim();
        var failedConnection = new SdkConnectionResult(
            SdkConnectionStatus.Unavailable,
            StatusCode: 17,
            "scripted-connection-failure");
        var plan = new ScriptedSdkPlan().DelaysConnection(gate, failedConnection);
        var manager = CreateManager("localhost", plan, maximumAttempts: 1);
        try
        {
            AssertUnavailable(
                await manager.ExecuteAsync(new SdkCommand("while-disconnected")),
                SdkConnectionState.Disconnected);

            var connecting = manager.ConnectAsync();
            Assert.True(
                SpinWait.SpinUntil(
                    () => manager.Current.State == SdkConnectionState.Connecting &&
                        plan.ConnectionCallCount == 1,
                    TimeSpan.FromSeconds(2)));
            AssertUnavailable(
                await manager.ExecuteAsync(new SdkCommand("while-connecting")),
                SdkConnectionState.Connecting);

            gate.Set();
            var faulted = await connecting;
            Assert.Equal(SdkConnectionState.Faulted, faulted.State);
            AssertUnavailable(
                await manager.ExecuteAsync(new SdkCommand("while-faulted")),
                SdkConnectionState.Faulted);

            await manager.DisposeAsync();
            AssertUnavailable(
                await manager.ExecuteAsync(new SdkCommand("while-stopping")),
                SdkConnectionState.Stopping);
            Assert.Empty(plan.Events);
        }
        finally
        {
            gate.Set();
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task ConnectionRetriesAreBoundedAndPreserveConfiguredHostAndStatus()
    {
        var plan = new ScriptedSdkPlan()
            .FailsConnection(statusCode: 11)
            .FailsConnection(statusCode: 12)
            .ConnectsSuccessfully(statusCode: 0);
        await using var manager = CreateManager("192.0.2.10", plan, maximumAttempts: 3);

        var connection = await manager.ConnectAsync();

        Assert.Equal(SdkConnectionState.Connected, connection.State);
        Assert.Equal(3, connection.Attempt);
        Assert.Equal(0, connection.StatusCode);
        Assert.Equal(3, plan.ConnectionCallCount);
        Assert.Equal(
            ["192.0.2.10", "192.0.2.10", "192.0.2.10"],
            plan.ConnectionHosts);
        Assert.Equal(1, plan.AdapterCreationCount);
        Assert.Equal(1, plan.MaximumActiveAdapterCount);
    }

    [Fact]
    public async Task FaultedConnectionRequiresAnExplicitNewBoundedCycle()
    {
        var plan = new ScriptedSdkPlan()
            .FailsConnection(statusCode: 31)
            .FailsConnection(statusCode: 32)
            .ConnectsSuccessfully(statusCode: 0);
        await using var manager = CreateManager("localhost", plan, maximumAttempts: 2);

        var firstCycle = await manager.ConnectAsync();
        var secondCycle = await manager.ConnectAsync();

        Assert.Equal(SdkConnectionState.Faulted, firstCycle.State);
        Assert.Equal(32, firstCycle.StatusCode);
        Assert.Equal(2, firstCycle.Attempt);
        Assert.Equal(SdkConnectionState.Connected, secondCycle.State);
        Assert.Equal(1, secondCycle.Attempt);
        Assert.Equal(3, plan.ConnectionCallCount);
        Assert.Equal(1, plan.AdapterCreationCount);
    }

    [Fact]
    public async Task ConcurrentConnectCallersShareOneConnectionOwner()
    {
        using var gate = new ManualResetEventSlim();
        var connected = new SdkConnectionResult(SdkConnectionStatus.Connected, 0, null);
        var plan = new ScriptedSdkPlan().DelaysConnection(gate, connected);
        await using var manager = CreateManager("localhost", plan, maximumAttempts: 1);

        var first = manager.ConnectAsync();
        Assert.True(
            SpinWait.SpinUntil(
                () => manager.Current.State == SdkConnectionState.Connecting &&
                    plan.ConnectionCallCount == 1,
                TimeSpan.FromSeconds(2)));
        var second = manager.ConnectAsync();

        Assert.False(second.IsCompleted);
        gate.Set();
        var results = await Task.WhenAll(first, second);

        Assert.All(results, item => Assert.Equal(SdkConnectionState.Connected, item.State));
        Assert.Equal(1, plan.ConnectionCallCount);
        Assert.Equal(1, plan.AdapterCreationCount);
        Assert.Equal(1, plan.MaximumActiveAdapterCount);
    }

    [Fact]
    public async Task AdapterActivationFailuresConsumeOnlyTheConfiguredAttemptBudget()
    {
        var activations = 0;
        await using var manager = new SdkConnectionManager(
            "localhost",
            new SdkConnectionPolicy(maximumAttempts: 2, retryDelay: TimeSpan.Zero),
            () =>
            {
                Interlocked.Increment(ref activations);
                throw new InvalidOperationException("scripted activation failure");
            });

        var connection = await manager.ConnectAsync();

        Assert.Equal(SdkConnectionState.Faulted, connection.State);
        Assert.Equal(2, connection.Attempt);
        Assert.Equal("sdk-client-activation-failed", connection.DiagnosticCode);
        Assert.Equal(2, activations);
    }

    private static SdkConnectionManager CreateManager(
        string host,
        ScriptedSdkPlan plan,
        int maximumAttempts) =>
        new(
            host,
            new SdkConnectionPolicy(maximumAttempts, TimeSpan.Zero),
            plan.CreateSdk);

    private static void AssertUnavailable(
        SdkRequestResult result,
        SdkConnectionState expectedState)
    {
        Assert.Equal(SdkRequestStatus.Unavailable, result.Status);
        Assert.Null(result.Execution);
        Assert.Equal(SdkConnectionManager.NotReadyDiagnosticCode, result.DiagnosticCode);
        Assert.Equal(expectedState, result.Connection.State);
    }
}
