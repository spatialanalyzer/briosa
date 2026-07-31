using Briosa.Worker.Sdk;
using Briosa.Worker.Testing;

namespace Briosa.Worker.Tests;

public sealed class SdkConnectionManagerTests
{
    [Fact]
    public async Task ConnectRemainsUnverifiedUntilRedactedProbeSucceeds()
    {
        var plan = new ScriptedSdkPlan()
            .ConnectsSuccessfully(statusCode: 23)
            .Then(ScriptedExecution.Success())
            .Then(ScriptedExecution.Success());
        var manager = CreateManager("sa-lab", plan, maximumAttempts: 3);
        try
        {
            var attached = await manager.ConnectAsync();
            var rejected = await manager.ExecuteAsync(
                new SdkCommand("before-verification"));
            var verified = await manager.VerifyExecutionAsync();
            var request = await manager.ExecuteAsync(
                new SdkCommand("connected-operation"));

            Assert.Equal(SdkConnectionState.Connected, attached.State);
            Assert.Equal(
                SdkExecutionReadinessState.Unverified,
                attached.ExecutionReadinessState);
            AssertUnavailable(rejected, SdkConnectionState.Connected);
            Assert.Equal(
                SdkExecutionReadinessState.ExecutionReady,
                verified.ExecutionReadinessState);
            Assert.Equal("execution-readiness-verified", verified.DiagnosticCode);
            Assert.Equal(SdkRequestStatus.Completed, request.Status);
            Assert.True(request.Execution!.MpResult.Succeeded);
            Assert.Equal(
                [
                    SdkConnectionManager.VerificationOperationId,
                    SdkConnectionManager.VerificationOperationId,
                    "connected-operation",
                    "connected-operation"
                ],
                plan.Events.Select(item => item.OperationId));
            Assert.DoesNotContain(
                manager.History,
                snapshot => snapshot.DiagnosticCode.Contains(
                    "scripted-output",
                    StringComparison.Ordinal));
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
    public async Task WorkIsRejectedWhileVerificationIsInProgress()
    {
        using var gate = new ManualResetEventSlim();
        var plan = new ScriptedSdkPlan()
            .ConnectsSuccessfully()
            .Then(ScriptedExecution.Hang(gate));
        await using var manager = CreateManager("localhost", plan, maximumAttempts: 1);
        await manager.ConnectAsync();

        var verification = manager.VerifyExecutionAsync();
        try
        {
            Assert.True(SpinWait.SpinUntil(
                () => manager.Current.ExecutionReadinessState ==
                    SdkExecutionReadinessState.Verifying,
                TimeSpan.FromSeconds(2)));

            var rejected = await manager.ExecuteAsync(
                new SdkCommand("while-verifying"));
            AssertUnavailable(rejected, SdkConnectionState.Connected);
        }
        finally
        {
            gate.Set();
        }

        Assert.Equal(
            SdkExecutionReadinessState.ExecutionReady,
            (await verification).ExecutionReadinessState);
    }

    [Theory]
    [InlineData("execute-rejected", "execution-readiness-probe-rejected")]
    [InlineData("mp-failure", "execution-readiness-probe-mp-failed")]
    [InlineData("malformed-output", "execution-readiness-probe-output-invalid")]
    [InlineData("adapter-failure", "execution-readiness-probe-failed")]
    public async Task FailedProbeRequiresOperatorRecovery(
        string behavior,
        string expectedDiagnosticCode)
    {
        var execution = behavior switch
        {
            "execute-rejected" => ScriptedExecution.ExecuteRejected(),
            "mp-failure" => ScriptedExecution.MpFailure(),
            "malformed-output" => ScriptedExecution.MalformedOutput(),
            "adapter-failure" => ScriptedExecution.Crash(),
            _ => throw new ArgumentOutOfRangeException(nameof(behavior))
        };
        var plan = new ScriptedSdkPlan()
            .ConnectsSuccessfully()
            .Then(execution);
        await using var manager = CreateManager("localhost", plan, maximumAttempts: 1);
        await manager.ConnectAsync();

        var connection = await manager.VerifyExecutionAsync();
        var rejected = await manager.ExecuteAsync(new SdkCommand("after-failed-probe"));

        Assert.Equal(
            SdkExecutionReadinessState.OperatorRecoveryRequired,
            connection.ExecutionReadinessState);
        Assert.Equal(expectedDiagnosticCode, connection.DiagnosticCode);
        AssertUnavailable(rejected, SdkConnectionState.Connected);
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
            Assert.True(SpinWait.SpinUntil(
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
    public async Task OnlyReviewedTransientStatusCodesAreRetried()
    {
        var plan = new ScriptedSdkPlan()
            .FailsConnection(statusCode: 11)
            .ConnectsSuccessfully(statusCode: 0);
        await using var manager = CreateManager(
            "192.0.2.10",
            plan,
            maximumAttempts: 3,
            transientStatusCodes: [11]);

        var connection = await manager.ConnectAsync();

        Assert.Equal(SdkConnectionState.Connected, connection.State);
        Assert.Equal(SdkExecutionReadinessState.Unverified, connection.ExecutionReadinessState);
        Assert.Equal(2, connection.Attempt);
        Assert.Equal(2, plan.ConnectionCallCount);
    }

    [Fact]
    public async Task UnknownConnectionStatusFailsClosedWithoutRetrying()
    {
        var plan = new ScriptedSdkPlan()
            .FailsConnection(statusCode: 31)
            .ConnectsSuccessfully(statusCode: 0);
        await using var manager = CreateManager("localhost", plan, maximumAttempts: 3);

        var connection = await manager.ConnectAsync();

        Assert.Equal(SdkConnectionState.Faulted, connection.State);
        Assert.Equal(31, connection.StatusCode);
        Assert.Equal(1, connection.Attempt);
        Assert.Equal(1, plan.ConnectionCallCount);
    }

    [Fact]
    public async Task FaultedConnectionRequiresAnExplicitNewCycle()
    {
        var plan = new ScriptedSdkPlan()
            .FailsConnection(statusCode: 31)
            .ConnectsSuccessfully(statusCode: 0);
        await using var manager = CreateManager("localhost", plan, maximumAttempts: 2);

        var firstCycle = await manager.ConnectAsync();
        var secondCycle = await manager.ConnectAsync();

        Assert.Equal(SdkConnectionState.Faulted, firstCycle.State);
        Assert.Equal(SdkConnectionState.Connected, secondCycle.State);
        Assert.Equal(SdkExecutionReadinessState.Unverified, secondCycle.ExecutionReadinessState);
        Assert.Equal(2, plan.ConnectionCallCount);
    }

    [Fact]
    public async Task ConcurrentConnectCallersShareOneUnverifiedConnectionOwner()
    {
        using var gate = new ManualResetEventSlim();
        var connected = new SdkConnectionResult(SdkConnectionStatus.Connected, 0, null);
        var plan = new ScriptedSdkPlan().DelaysConnection(gate, connected);
        await using var manager = CreateManager("localhost", plan, maximumAttempts: 1);

        var first = manager.ConnectAsync();
        Assert.True(SpinWait.SpinUntil(
            () => manager.Current.State == SdkConnectionState.Connecting &&
                plan.ConnectionCallCount == 1,
            TimeSpan.FromSeconds(2)));
        var second = manager.ConnectAsync();

        Assert.False(second.IsCompleted);
        gate.Set();
        var results = await Task.WhenAll(first, second);

        Assert.All(results, item =>
        {
            Assert.Equal(SdkConnectionState.Connected, item.State);
            Assert.Equal(
                SdkExecutionReadinessState.Unverified,
                item.ExecutionReadinessState);
        });
        Assert.Equal(1, plan.ConnectionCallCount);
        Assert.Equal(1, plan.AdapterCreationCount);
    }

    [Fact]
    public async Task AdapterActivationFailureIsNotRetriedWithoutReviewedEvidence()
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
        Assert.Equal(1, connection.Attempt);
        Assert.Equal("sdk-client-activation-failed", connection.DiagnosticCode);
        Assert.Equal(1, activations);
    }

    private static SdkConnectionManager CreateManager(
        string host,
        ScriptedSdkPlan plan,
        int maximumAttempts,
        IEnumerable<int>? transientStatusCodes = null) =>
        new(
            host,
            new SdkConnectionPolicy(
                maximumAttempts,
                TimeSpan.Zero,
                transientStatusCodes),
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
