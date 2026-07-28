using Briosa.Worker.Control;

namespace Briosa.Worker.Tests;

public sealed class WorkerControlExecutionVerificationTests
{
    [Fact]
    public void VerificationResultRoundTripsOnlySafeState()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var correlationId = Guid.NewGuid();
        var connection = new WorkerConnectionSnapshot(
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady,
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 1,
            "execution-readiness-verified",
            DateTimeOffset.UnixEpoch);

        channel.Send(WorkerControlMessage.ExecutionVerificationResult(
            correlationId,
            connection));
        stream.Position = 0;
        var received = channel.Receive();

        Assert.Equal(
            WorkerControlMessageKind.ExecutionVerificationResult,
            received.Kind);
        Assert.Equal(correlationId, received.CorrelationId);
        Assert.Equal(connection, received.Connection);
        Assert.Null(received.Command);
        Assert.Null(received.ExecutionResponse);
        Assert.DoesNotContain(
            typeof(WorkerConnectionSnapshot).GetProperties(),
            property => property.Name.Contains("Host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VerificationResultRequiresConnectionSnapshot()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var invalid = new WorkerControlMessage(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.ExecutionVerificationResult,
            Guid.NewGuid());

        Assert.Throws<InvalidDataException>(() => channel.Send(invalid));
    }
}
