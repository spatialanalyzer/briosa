using System.Buffers.Binary;
using System.Text.Json;
using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerExecutionOutcomeSerializationTests
{
    [Theory]
    [InlineData("result-unavailable", -1)]
    [InlineData("unknown-outcome", 0)]
    public void InvalidWireOutcomeIsAChannelFailure(string discriminator, long duration)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            ProtocolVersion = WorkerControlProtocol.CurrentVersion,
            Kind = WorkerControlMessageKind.ExecutionResult,
            CorrelationId = Guid.NewGuid(),
            ExecutionResponse = new
            {
                Status = WorkerExecutionResponseStatus.Completed,
                Execution = new { outcome = discriminator, durationMilliseconds = duration, diagnosticCode = "test" },
                Connection = new WorkerConnectionSnapshot(WorkerConnectionState.Disconnected,
                    WorkerExecutionReadinessState.Unverified, null, 0, 1, "disconnected", DateTimeOffset.UnixEpoch)
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

    [Fact]
    public void EveryOutcomeRoundTripsWithIndependentEvidence()
    {
        WorkerMpExecutionResult[] outcomes =
        [
            new WorkerArgumentsRejected(1, "input-refused"),
            new WorkerExecuteRejected(2, "execute-refused"),
            new WorkerMpResultUnavailable(3, "result-missing"),
            new WorkerMpOutputsUnavailable(4, "worker-output-encoding-rejected"),
            new WorkerMpResultAvailable(-1, 4, [], "mp-failed"),
            new WorkerMpResultAvailable(2, 5,
                [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0))], null),
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.BeforeExecute, 6, [], WorkerSdkFaultDiagnosticCodes.BeforeExecute),
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.ExecuteStep, 7, [], WorkerSdkFaultDiagnosticCodes.ExecuteStep),
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.MpResultRetrieval, 8, [], WorkerSdkFaultDiagnosticCodes.MpResultRetrieval),
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 9,
                [
                    new WorkerUnavailableOutput("Missing", WorkerMpValueKind.Text),
                    new WorkerUnavailableOutput("Faulted", WorkerMpValueKind.Text,
                        reason: WorkerUnavailableOutputReason.SdkCallFaulted),
                    new WorkerUnavailableOutput("Rejected", WorkerMpValueKind.FloatingPoint,
                        reason: WorkerUnavailableOutputReason.EncodingRejected),
                    new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1))
                ], WorkerSdkFaultDiagnosticCodes.OutputGetter),
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 10, [], WorkerSdkFaultDiagnosticCodes.OutputGetter)
        ];
        foreach (var outcome in outcomes)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream, leaveOpen: true);
            channel.Send(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed, outcome,
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "disconnected", DateTimeOffset.UnixEpoch), null)));
            using var document = JsonDocument.Parse(stream.ToArray().AsMemory(sizeof(int)));
            var encoded = document.RootElement.GetProperty("executionResponse").GetProperty("execution");
            Assert.True(encoded.TryGetProperty("outcome", out _));
            Assert.False(encoded.TryGetProperty("mpSucceeded", out _));
            Assert.False(encoded.TryGetProperty("mpResultRetrieved", out _));
            Assert.False(encoded.TryGetProperty("executeStepReturned", out _));
            stream.Position = 0;
            var decoded = channel.Receive().ExecutionResponse!.Execution!;
            Assert.Equal(outcome.GetType(), decoded.GetType());
            Assert.Equal(outcome.ExecuteStepReturned, decoded.ExecuteStepReturned);
            Assert.Equal(outcome.MpResultRetrieved, decoded.MpResultRetrieved);
            Assert.Equal(outcome.MpSucceeded, decoded.MpSucceeded);
            Assert.Equal(outcome.MpResultCode, decoded.MpResultCode);
            Assert.Equal(outcome.DurationMilliseconds, decoded.DurationMilliseconds);
            Assert.Equal(outcome.OutputValues, decoded.OutputValues);
            Assert.Equal(outcome.DiagnosticCode, decoded.DiagnosticCode);
            if (outcome is WorkerSdkCallFaulted fault)
            {
                Assert.Equal(fault.Phase, Assert.IsType<WorkerSdkCallFaulted>(decoded).Phase);
            }
        }
    }

    // A missing, zero, or unknown phase must never decode as a NotStarted fault.
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(5)]
    public void SdkCallFaultWithoutAKnownPhaseIsAChannelFailure(int? phase)
    {
        var execution = new Dictionary<string, object?>
        {
            ["outcome"] = "sdk-call-faulted",
            ["durationMilliseconds"] = 1,
            ["outputs"] = Array.Empty<object>(),
            ["diagnosticCode"] = WorkerSdkFaultDiagnosticCodes.BeforeExecute
        };
        if (phase is not null)
        {
            execution["phase"] = phase;
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
                Connection = new WorkerConnectionSnapshot(WorkerConnectionState.Disconnected,
                    WorkerExecutionReadinessState.Unverified, null, 0, 1, "disconnected", DateTimeOffset.UnixEpoch)
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

    [Theory]
    [InlineData((int)WorkerSdkCallPhase.BeforeExecute)]
    [InlineData((int)WorkerSdkCallPhase.ExecuteStep)]
    [InlineData((int)WorkerSdkCallPhase.MpResultRetrieval)]
    public void SdkCallFaultOutputsMustMatchItsPhase(int phase) =>
        Assert.Throws<ArgumentException>(() => new WorkerSdkCallFaulted(
            (WorkerSdkCallPhase)phase, 0,
            [new WorkerUnavailableOutput("Value", WorkerMpValueKind.FloatingPoint)], "sdk-call-faulted"));

    // An output-getter fault with no outputs means the per-output evidence was
    // withheld because it could not be encoded.
    [Fact]
    public void OutputGetterFaultMayWithholdItsPerOutputEvidence() =>
        Assert.Empty(new WorkerSdkCallFaulted(
            WorkerSdkCallPhase.OutputGetter, 0, [], WorkerSdkFaultDiagnosticCodes.OutputGetter).OutputValues);

    [Fact]
    public void OnlyANonDefaultUnavailableReasonCrossesTheChannel()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
            WorkerExecutionResponseStatus.Completed,
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1,
                [
                    new WorkerUnavailableOutput("Plain", WorkerMpValueKind.Text, "local-only"),
                    new WorkerUnavailableOutput("Faulted", WorkerMpValueKind.Text, "local-only",
                        WorkerUnavailableOutputReason.SdkCallFaulted)
                ], WorkerSdkFaultDiagnosticCodes.OutputGetter),
            new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                null, 0, 1, "disconnected", DateTimeOffset.UnixEpoch), null)));
        using var document = JsonDocument.Parse(stream.ToArray().AsMemory(sizeof(int)));
        var outputs = document.RootElement.GetProperty("executionResponse").GetProperty("execution")
            .GetProperty("outputs").EnumerateArray().ToArray();
        Assert.False(outputs[0].TryGetProperty("reason", out _));
        Assert.Equal((int)WorkerUnavailableOutputReason.SdkCallFaulted, outputs[1].GetProperty("reason").GetInt32());
        Assert.DoesNotContain("local-only", System.Text.Encoding.UTF8.GetString(stream.ToArray()), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(3, false)]
    [InlineData(1, true)]
    public void UnknownUnavailableReasonIsAChannelFailure(int reason, bool known)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            ProtocolVersion = WorkerControlProtocol.CurrentVersion,
            Kind = WorkerControlMessageKind.ExecutionResult,
            CorrelationId = Guid.NewGuid(),
            ExecutionResponse = new
            {
                Status = WorkerExecutionResponseStatus.Completed,
                Execution = new Dictionary<string, object?>
                {
                    ["outcome"] = "sdk-call-faulted",
                    ["phase"] = (int)WorkerSdkCallPhase.OutputGetter,
                    ["durationMilliseconds"] = 1,
                    ["outputs"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["retrieval"] = "unavailable",
                            ["name"] = "Value",
                            ["kind"] = (int)WorkerMpValueKind.Text,
                            ["reason"] = reason
                        }
                    },
                    ["diagnosticCode"] = WorkerSdkFaultDiagnosticCodes.OutputGetter
                },
                Connection = new WorkerConnectionSnapshot(WorkerConnectionState.Disconnected,
                    WorkerExecutionReadinessState.Unverified, null, 0, 1, "disconnected", DateTimeOffset.UnixEpoch)
            }
        }, JsonSerializerOptions.Web);
        using var stream = new MemoryStream();
        Span<byte> header = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        stream.Write(header);
        stream.Write(payload);
        stream.Position = 0;
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        if (known)
        {
            var fault = Assert.IsType<WorkerSdkCallFaulted>(channel.Receive().ExecutionResponse!.Execution);
            Assert.Equal(WorkerUnavailableOutputReason.SdkCallFaulted,
                Assert.IsType<WorkerUnavailableOutput>(Assert.Single(fault.Outputs)).Reason);
        }
        else
        {
            Assert.Throws<InvalidDataException>(() => channel.Receive());
        }
    }

    [Fact]
    public void SdkCallFaultRequiresAnUnavailableGetterOutputAndADiagnosticCode()
    {
        Assert.Throws<ArgumentException>(() => new WorkerSdkCallFaulted(
            WorkerSdkCallPhase.OutputGetter, 0,
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1))],
            WorkerSdkFaultDiagnosticCodes.OutputGetter));
        Assert.Throws<ArgumentException>(() => new WorkerSdkCallFaulted(
            WorkerSdkCallPhase.ExecuteStep, 0, [], " "));
    }

    [Theory]
    [InlineData(false, true, false, 3)]
    [InlineData(true, false, true, 2)]
    [InlineData(true, true, false, 2)]
    [InlineData(true, true, true, 3)]
    [InlineData(true, true, true, null)]
    public void ContradictorySdkEvidenceCannotCreateAnOutcome(bool execute, bool retrieved, bool succeeded, int? code) =>
        Assert.Throws<ArgumentException>(() => WorkerMpExecutionResult.FromEvidence(
            execute, retrieved, succeeded, code, 0, [], null));

    [Fact]
    public void FailedMpCannotContainOutputRetrievals() =>
        Assert.Throws<ArgumentException>(() => new WorkerMpResultAvailable(3, 0,
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(42))], null));

    [Fact]
    public void OutcomeOwnsTheOutputCollection()
    {
        var values = new List<WorkerMpOutputValue> { new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(42)) };
        var outcome = new WorkerMpResultAvailable(2, 0, values, null);
        values.Clear();
        Assert.Equal(42, ((Assert.Single(outcome.OutputValues).ReadValue() as WorkerDoubleValue)?.Value));
    }
}
