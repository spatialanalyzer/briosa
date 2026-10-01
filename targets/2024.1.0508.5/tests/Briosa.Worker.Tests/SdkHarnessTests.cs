using Briosa.Worker.Control;
using Briosa.Worker.Sdk;
using Briosa.Worker.Testing;

namespace Briosa.Worker.Tests;

public sealed class SdkHarnessTests
{
    [Fact]
    public async Task ContractSuccessRunsAdapterOnOwnedSta()
    {
        var plan = new ScriptedSdkPlan()
            .ConnectsSuccessfully()
            .Then(ScriptedExecution.Success());

        await SdkContractAssertions.ConnectedAdapterExecutesOnSta(
            plan.CreateSdk,
            () => plan.AdapterApartmentState);
    }

    [Fact]
    public async Task ContractPreservesMpFailureWhenExecuteStepReturnsTrue()
    {
        var plan = new ScriptedSdkPlan()
            .Then(ScriptedExecution.MpFailure());

        await SdkContractAssertions.MpFailureIsIndependentFromExecuteStep(plan.CreateSdk);
    }

    [Fact]
    public async Task ConnectionFailureRemainsAConnectionOutcome()
    {
        var plan = new ScriptedSdkPlan().FailsConnection(statusCode: 17);
        await using var executor = new SerializedSdkExecutor(plan.CreateSdk);

        var result = await executor.ConnectAsync("localhost");

        Assert.Equal(SdkConnectionStatus.Unavailable, result.Status);
        Assert.Equal(17, result.StatusCode);
        Assert.Equal("scripted-connection-failure", result.DiagnosticCode);
    }

    [Fact]
    public async Task ConcurrentCommandsCannotInterleave()
    {
        using var delayGate = new ManualResetEventSlim();
        var plan = new ScriptedSdkPlan()
            .Then(ScriptedExecution.Delay(delayGate))
            .Then(ScriptedExecution.Success());
        await using var executor = new SerializedSdkExecutor(plan.CreateSdk);

        var first = executor.ExecuteAsync(new WorkerMpCommand("first", "first", [], []));
        Assert.True(
            SpinWait.SpinUntil(
                () => plan.Events.Any(item => item.OperationId == "first"),
                TimeSpan.FromSeconds(2)));

        var second = executor.ExecuteAsync(new WorkerMpCommand("second", "second", [], []));

        Assert.False(second.IsCompleted);
        Assert.DoesNotContain(plan.Events, item => item.OperationId == "second");

        delayGate.Set();
        await Task.WhenAll(first, second);

        Assert.Collection(
            plan.Events.OrderBy(item => item.Sequence),
            item => AssertEvent(item, "first", ScriptedCallPhase.Started),
            item => AssertEvent(item, "first", ScriptedCallPhase.Completed),
            item => AssertEvent(item, "second", ScriptedCallPhase.Started),
            item => AssertEvent(item, "second", ScriptedCallPhase.Completed));
    }

    private static void AssertEvent(
        ScriptedCallEvent callEvent,
        string operationId,
        ScriptedCallPhase phase)
    {
        Assert.Equal(operationId, callEvent.OperationId);
        Assert.Equal(phase, callEvent.Phase);
    }
}
