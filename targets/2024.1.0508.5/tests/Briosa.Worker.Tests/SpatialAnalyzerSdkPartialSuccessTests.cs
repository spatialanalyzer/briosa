using System.Buffers.Binary;
using System.Text.Json;
using Briosa.Worker.Control;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

// #351: MP code 4 (PARTIAL SUCCESS) did its work, so the worker retrieves the
// outputs as it does after code 2 and carries the code to the host.
public sealed partial class SpatialAnalyzerSdkAdapterTests
{
    [Fact]
    public void PartialSuccessRetrievesRequestedOutputs()
    {
        using var calls = new RecordingSdkCalls { MpResultCode = 4 };
        using var adapter = new SpatialAnalyzerSdkAdapter(calls);
        var command = new WorkerMpCommand(
            "partial-operation",
            "Partial Operation",
            inputArguments: [],
            [new WorkerMpOutputArgument("Result", WorkerMpValueKind.Text)]);

        var result = adapter.Execute(command);

        Assert.Equal(
            ["SetStep:Partial Operation", "ExecuteStep", "GetMPStepResult", "GetStringArg:Result"],
            calls.Events);
        var available = Assert.IsType<WorkerMpResultAvailable>(result);
        Assert.Equal(4, available.ResultCode);
        Assert.True(result.MpSucceeded);
        Assert.True(result.MpPartialSuccess);
        Assert.Equal(4, result.MpResultCode);
        Assert.True(Assert.Single(result.OutputValues).Retrieved);
        Assert.Null(result.DiagnosticCode);
    }

    [Fact]
    public void GetterFaultAfterPartialSuccessCarriesTheCode()
    {
        using var calls = new RecordingSdkCalls { MpResultCode = 4, FaultPhase = SdkFaultPhase.Getter, FaultOrdinal = 2 };
        var result = ExecuteOnce(calls);

        var fault = AssertFault(result, WorkerSdkCallPhase.OutputGetter);
        Assert.Equal(4, fault.ResultCode);
        Assert.Equal(4, result.MpResultCode);
        Assert.True(result.MpPartialSuccess);
        Assert.Equal([true, false, true], result.OutputValues.Select(output => output.Retrieved));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The connection manager owns and disposes the adapter, and the adapter owns the fake SDK calls.")]
    public async Task ReadinessProbeStillRequiresFullSuccess()
    {
        var calls = new RecordingSdkCalls { MpResultCode = 4 };
        var manager = new SdkConnectionManager("localhost",
            new SdkConnectionPolicy(1, TimeSpan.Zero), () => new SpatialAnalyzerSdkAdapter(calls));
        await using var lifetime = manager.ConfigureAwait(false);
        await manager.StartAsync();
        await manager.ConnectAsync();

        var verified = await manager.VerifyExecutionAsync();

        Assert.Equal(SdkExecutionReadinessState.OperatorRecoveryRequired, verified.ExecutionReadinessState);
        Assert.Equal("execution-readiness-probe-mp-failed", verified.DiagnosticCode);
    }
}

public sealed class WorkerPartialSuccessEvidenceTests
{
    [Fact]
    public void CodeFourPermitsOutputsAndOtherFailureCodesDoNot()
    {
        WorkerMpOutputValue[] outputs = [new WorkerRetrievedOutput("Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(1))];

        var partial = new WorkerMpResultAvailable(4, 1, outputs, null);

        Assert.True(partial.MpSucceeded);
        Assert.True(partial.MpPartialSuccess);
        Assert.False(new WorkerMpResultAvailable(2, 1, outputs, null).MpPartialSuccess);
        Assert.Throws<ArgumentException>(() => new WorkerMpResultAvailable(3, 1, outputs, "mp-command-failed"));
        Assert.IsType<WorkerMpResultAvailable>(WorkerMpExecutionResult.FromEvidence(true, true, true, 4, 1, outputs, null));
        Assert.Throws<ArgumentException>(() => WorkerMpExecutionResult.FromEvidence(true, true, false, 4, 1, [], null));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void TheRetrievedCodeCrossesThePrivateChannel(int resultCode)
    {
        WorkerMpExecutionResult[] outcomes =
        [
            new WorkerMpResultAvailable(resultCode, 1,
                [new WorkerRetrievedOutput("Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(1))], null),
            new WorkerMpOutputsUnavailable(resultCode, 2, WorkerExecutionDelivery.OutputEncodingRejected),
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 3,
                [new WorkerUnavailableOutput("Value", WorkerMpValueKind.WholeNumber,
                    reason: WorkerUnavailableOutputReason.SdkCallFaulted)],
                WorkerSdkFaultDiagnosticCodes.OutputGetter, resultCode),
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 4, [], WorkerSdkFaultDiagnosticCodes.OutputGetter, resultCode)
        ];
        foreach (var outcome in outcomes)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream, leaveOpen: true);
            channel.Send(Message(outcome));
            using var document = JsonDocument.Parse(stream.ToArray().AsMemory(sizeof(int)));
            Assert.Equal(resultCode, document.RootElement.GetProperty("executionResponse")
                .GetProperty("execution").GetProperty("resultCode").GetInt32());
            stream.Position = 0;
            var decoded = channel.Receive().ExecutionResponse!.Execution!;
            Assert.Equal(outcome.GetType(), decoded.GetType());
            Assert.Equal(resultCode, decoded.MpResultCode);
            Assert.True(decoded.MpSucceeded);
            Assert.Equal(resultCode == 4, decoded.MpPartialSuccess);
        }
    }

    [Fact]
    public void BoundedDeliveryKeepsTheCode()
    {
        var oversized = new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text,
            new WorkerTextValue(new string('x', WorkerControlProtocol.MaximumMessageBytes + 1)));
        var replacement = WorkerExecutionDelivery.Undeliverable(Guid.NewGuid(),
            Response(new WorkerMpResultAvailable(4, 1, [oversized], null)));
        var fault = WorkerExecutionDelivery.Undeliverable(Guid.NewGuid(), Response(new WorkerSdkCallFaulted(
            WorkerSdkCallPhase.OutputGetter, 1,
            [oversized, new WorkerUnavailableOutput("Other", WorkerMpValueKind.Text, reason: WorkerUnavailableOutputReason.SdkCallFaulted)],
            WorkerSdkFaultDiagnosticCodes.OutputGetter, 4)));

        Assert.Equal(4, Assert.IsType<WorkerMpOutputsUnavailable>(replacement.ExecutionResponse!.Execution).ResultCode);
        Assert.Equal(4, Assert.IsType<WorkerSdkCallFaulted>(fault.ExecutionResponse!.Execution).ResultCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(3)]
    public void OutputsUnavailableWithoutARetrievableCodeIsAChannelFailure(int? resultCode) =>
        AssertMalformed(new Dictionary<string, object?>
        {
            ["outcome"] = "outputs-unavailable",
            ["durationMilliseconds"] = 1,
            ["diagnosticCode"] = WorkerExecutionDelivery.OutputEncodingRejected
        }, resultCode);

    [Theory]
    [InlineData(null)]
    [InlineData(3)]
    public void OutputGetterFaultWithoutARetrievableCodeIsAChannelFailure(int? resultCode) =>
        AssertMalformed(new Dictionary<string, object?>
        {
            ["outcome"] = "sdk-call-faulted",
            ["phase"] = (int)WorkerSdkCallPhase.OutputGetter,
            ["durationMilliseconds"] = 1,
            ["outputs"] = Array.Empty<object>(),
            ["diagnosticCode"] = WorkerSdkFaultDiagnosticCodes.OutputGetter
        }, resultCode);

    [Theory]
    [InlineData((int)WorkerSdkCallPhase.BeforeExecute)]
    [InlineData((int)WorkerSdkCallPhase.ExecuteStep)]
    [InlineData((int)WorkerSdkCallPhase.MpResultRetrieval)]
    public void FaultBeforeAnyRetrievedCodeCannotClaimOne(int phase)
    {
        Assert.Throws<ArgumentException>(() => new WorkerSdkCallFaulted(
            (WorkerSdkCallPhase)phase, 1, [], WorkerSdkFaultDiagnosticCodes.For((WorkerSdkCallPhase)phase), 2));
        AssertMalformed(new Dictionary<string, object?>
        {
            ["outcome"] = "sdk-call-faulted",
            ["phase"] = phase,
            ["durationMilliseconds"] = 1,
            ["outputs"] = Array.Empty<object>(),
            ["diagnosticCode"] = WorkerSdkFaultDiagnosticCodes.For((WorkerSdkCallPhase)phase)
        }, 4);
    }

    private static void AssertMalformed(Dictionary<string, object?> execution, int? resultCode)
    {
        if (resultCode is not null)
        {
            execution["resultCode"] = resultCode;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            ProtocolVersion = WorkerControlProtocol.CurrentVersion,
            Kind = WorkerControlMessageKind.ExecutionResult,
            CorrelationId = Guid.NewGuid(),
            ExecutionResponse = new
            {
                Status = WorkerExecutionResponseStatus.Completed,
                Execution = execution,
                Connection = Connection()
            }
        }, JsonSerializerOptions.Web);
        using var stream = new MemoryStream();
        Span<byte> header = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        stream.Write(header);
        stream.Write(payload);
        stream.Position = 0;
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        Assert.Throws<InvalidDataException>(() => channel.Receive());
    }

    private static WorkerControlMessage Message(WorkerMpExecutionResult execution) =>
        WorkerControlMessage.ExecutionResult(Guid.NewGuid(), Response(execution));

    private static WorkerExecutionResponse Response(WorkerMpExecutionResult execution) =>
        new(WorkerExecutionResponseStatus.Completed, execution, Connection(), null);

    private static WorkerConnectionSnapshot Connection() =>
        new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
            null, 0, 1, "disconnected", DateTimeOffset.UnixEpoch);
}
