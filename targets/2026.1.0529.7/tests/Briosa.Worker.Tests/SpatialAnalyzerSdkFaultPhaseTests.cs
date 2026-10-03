using System.Runtime.InteropServices;
using System.Text;
using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    private static readonly WorkerMpCommand FaultPhaseCommand = new(
        "fault-phase",
        "Fault Phase",
        [
            new WorkerMpInputArgument("First", WorkerMpValueKind.Text, new WorkerTextValue("a")),
            new WorkerMpInputArgument("Second", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(2)),
            new WorkerMpInputArgument("Third", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))
        ],
        [
            new WorkerMpOutputArgument("Planar Offset", WorkerMpValueKind.FloatingPoint),
            new WorkerMpOutputArgument("Result", WorkerMpValueKind.Text),
            new WorkerMpOutputArgument("Count", WorkerMpValueKind.WholeNumber)
        ]);

    [Fact]
    public void SetStepFaultProvesExecutionDidNotStart()
    {
        using var calls = FaultAt(SdkFaultPhase.SetStep);
        var result = ExecuteOnce(calls);

        Assert.Equal(["SetStep:Fault Phase"], calls.Events);
        var rejected = Assert.IsType<WorkerArgumentsRejected>(result);
        Assert.Equal(WorkerSdkFaultDiagnosticCodes.BeforeExecute, rejected.DiagnosticCode);
        Assert.False(result.ExecuteStepReturned);
        Assert.Empty(result.OutputValues);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NthSetterFaultStopsBeforeExecuteStepWithADistinctCode(int ordinal)
    {
        using var calls = FaultAt(SdkFaultPhase.Setter, ordinal);
        var result = ExecuteOnce(calls);

        string[] setters = ["SetStringArg:First", "SetIntegerArg:Second", "SetBoolArg:Third"];
        string[] expected = ["SetStep:Fault Phase", .. setters.Take(ordinal)];
        Assert.Equal(expected, calls.Events);
        var rejected = Assert.IsType<WorkerArgumentsRejected>(result);
        Assert.Equal(WorkerSdkFaultDiagnosticCodes.BeforeExecute, rejected.DiagnosticCode);
        Assert.NotEqual("sdk-argument-rejected", rejected.DiagnosticCode);
    }

    [Fact]
    public void ExecuteStepFaultIsAnUnknownOutcomeWithoutResultRetrieval()
    {
        using var calls = FaultAt(SdkFaultPhase.ExecuteStep);
        var result = ExecuteOnce(calls);

        Assert.Equal("ExecuteStep", calls.Events[^1]);
        var unavailable = Assert.IsType<WorkerMpResultUnavailable>(result);
        Assert.Equal(WorkerSdkFaultDiagnosticCodes.ExecuteStep, unavailable.DiagnosticCode);
        Assert.False(result.MpResultRetrieved);
        Assert.Null(result.MpResultCode);
    }

    [Fact]
    public void MpResultRetrievalFaultIsAnUnknownOutcomeWithoutOutputGetters()
    {
        using var calls = FaultAt(SdkFaultPhase.GetMPStepResult);
        var result = ExecuteOnce(calls);

        Assert.Equal(["ExecuteStep", "GetMPStepResult"], calls.Events.TakeLast(2));
        var unavailable = Assert.IsType<WorkerMpResultUnavailable>(result);
        Assert.Equal(WorkerSdkFaultDiagnosticCodes.MpResultRetrieval, unavailable.DiagnosticCode);
        Assert.False(result.MpResultRetrieved);
        Assert.Empty(result.OutputValues);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NthGetterFaultCompletesWithOnlyThatOutputUnavailable(int ordinal)
    {
        using var calls = FaultAt(SdkFaultPhase.Getter, ordinal);
        var result = ExecuteOnce(calls);

        Assert.Equal(
            ["GetDoubleArg:Planar Offset", "GetStringArg:Result", "GetIntegerArg:Count"],
            calls.Events.TakeLast(3));
        var available = Assert.IsType<WorkerMpResultAvailable>(result);
        Assert.Equal(2, available.ResultCode);
        Assert.True(result.MpSucceeded);
        Assert.Equal(WorkerSdkFaultDiagnosticCodes.OutputGetter, result.DiagnosticCode);
        for (var index = 0; index < result.OutputValues.Count; index++)
        {
            var output = result.OutputValues[index];
            Assert.Equal(FaultPhaseCommand.OutputArguments[index].Name, output.Name);
            Assert.Equal(index != ordinal - 1, output.Retrieved);
        }

        var faulted = Assert.IsType<WorkerUnavailableOutput>(result.OutputValues[ordinal - 1]);
        Assert.Equal(WorkerSdkFaultDiagnosticCodes.OutputGetter, faulted.DiagnosticCode);
    }

    [Theory]
    [InlineData((int)SdkFaultPhase.SetStep)]
    [InlineData((int)SdkFaultPhase.Setter)]
    [InlineData((int)SdkFaultPhase.ExecuteStep)]
    [InlineData((int)SdkFaultPhase.GetMPStepResult)]
    [InlineData((int)SdkFaultPhase.Getter)]
    public void PerCallFaultCodesAreValueFreeOnThePrivateChannel(int phase)
    {
        using var calls = FaultAt((SdkFaultPhase)phase);
        var result = ExecuteOnce(calls);

        Assert.Matches("^[a-z0-9-]+$", result.DiagnosticCode);
        using var stream = new MemoryStream();
        using (var channel = new WorkerControlChannel(stream, leaveOpen: true))
        {
            channel.Send(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new WorkerExecutionResponse(
                WorkerExecutionResponseStatus.Completed,
                result,
                new WorkerConnectionSnapshot(WorkerConnectionState.Connected,
                    WorkerExecutionReadinessState.ExecutionReady, 0, 1, 1, "connected",
                    DateTimeOffset.UnixEpoch),
                DiagnosticCode: null)));
        }

        var encoded = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains(result.DiagnosticCode!, encoded, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive", encoded, StringComparison.Ordinal);
        Assert.DoesNotContain("fault text", encoded, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive", result.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(InvalidComObjectException))]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(InsufficientExecutionStackException))]
    [InlineData(typeof(ObjectDisposedException))]
    [InlineData(typeof(SEHException))]
    [InlineData(typeof(ThreadInterruptedException))]
    public void UntrustworthyStateFaultsStillEscapeSoTheWorkerFailStops(Type faultType)
    {
        foreach (var phase in Enum.GetValues<SdkFaultPhase>())
        {
            var fault = (Exception)Activator.CreateInstance(faultType, RecordingSdkCalls.SensitiveFaultText)!;
            using var calls = new RecordingSdkCalls { FaultPhase = phase, Fault = fault };
            using var adapter = new SpatialAnalyzerSdkAdapter(calls);

            Assert.Same(fault, Record.Exception(() => adapter.Execute(FaultPhaseCommand)));
        }
    }

    [Fact]
    public void AnAdapterKeepsServingAfterAPerCallFault()
    {
        using var calls = new RecordingSdkCalls
        {
            FaultPhase = SdkFaultPhase.ExecuteStep,
            FaultStepName = "Fault Phase"
        };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);

        Assert.IsType<WorkerMpResultUnavailable>(adapter.Execute(FaultPhaseCommand));
        var next = adapter.Execute(new WorkerMpCommand(
            "next", "Next", [], [new WorkerMpOutputArgument("Result", WorkerMpValueKind.Text)]));

        Assert.True(next.MpSucceeded);
        Assert.Null(next.DiagnosticCode);
        Assert.Equal("scripted-output", (Assert.Single(next.OutputValues).ReadValue() as WorkerTextValue)?.Value);
    }

    [Theory]
    [InlineData((int)SdkFaultPhase.SetStep)]
    [InlineData((int)SdkFaultPhase.ExecuteStep)]
    [InlineData((int)SdkFaultPhase.GetMPStepResult)]
    [InlineData((int)SdkFaultPhase.Getter)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The connection manager owns and disposes the adapter, and the adapter owns the fake SDK calls.")]
    public async Task ReadinessProbeSdkFaultFailsClosedWithTheProbeFailedCode(int phase)
    {
        var calls = FaultAt((SdkFaultPhase)phase);
        var manager = new SdkConnectionManager("localhost",
            new SdkConnectionPolicy(1, TimeSpan.Zero), () => new SpatialAnalyzerSdkAdapter(calls));
        await using var lifetime = manager.ConfigureAwait(false);
        await manager.StartAsync();
        await manager.ConnectAsync();

        var verified = await manager.VerifyExecutionAsync();

        Assert.Equal(SdkExecutionReadinessState.OperatorRecoveryRequired, verified.ExecutionReadinessState);
        Assert.Equal("execution-readiness-probe-failed", verified.DiagnosticCode);
    }

    private static RecordingSdkCalls FaultAt(SdkFaultPhase phase, int ordinal = 1) =>
        new() { FaultPhase = phase, FaultOrdinal = ordinal };

    private static WorkerMpExecutionResult ExecuteOnce(RecordingSdkCalls calls)
    {
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        return adapter.Execute(FaultPhaseCommand);
    }
}
