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
            new WorkerMpResultAvailable(-1, 4, [], "mp-failed"),
            new WorkerMpResultAvailable(2, 5,
                [new("Value", WorkerMpValueKind.FloatingPoint, true, DoubleValue: 0)], null)
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
        }
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
            [new("Value", WorkerMpValueKind.FloatingPoint, true, DoubleValue: 42)], null));

    [Fact]
    public void OutcomeOwnsTheOutputCollection()
    {
        var values = new List<WorkerMpOutputValue> { new("Value", WorkerMpValueKind.FloatingPoint, true, DoubleValue: 42) };
        var outcome = new WorkerMpResultAvailable(2, 0, values, null);
        values.Clear();
        Assert.Equal(42, Assert.Single(outcome.OutputValues).DoubleValue);
    }
}
