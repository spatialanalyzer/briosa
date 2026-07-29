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
            DateTimeOffset.UnixEpoch,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    "2026.1.0529.7",
                    WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)));

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
        Assert.Equal(
            "2026.1.0529.7",
            received.Connection!.RuntimeIdentity!.ActivatedSdk.Version);
        Assert.Null(
            received.Connection.RuntimeIdentity.ConnectedSpatialAnalyzer.Version);
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

    [Fact]
    public void RuntimeVerifiedIdentityRequiresAnObservedVersion()
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var connection = new WorkerConnectionSnapshot(
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.Unverified,
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 1,
            "connect-ex-connected",
            DateTimeOffset.UnixEpoch,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)));

        Assert.Throws<InvalidDataException>(() =>
            channel.Send(WorkerControlMessage.Ready(12, connection)));
    }

    public static TheoryData<string> MalformedRuntimeVersions => new()
    {
        "2026.1\nforged",
        new string('x', 129)
    };

    [Theory]
    [MemberData(nameof(MalformedRuntimeVersions))]
    public void RuntimeVerifiedIdentityRejectsUnsafeVersionShape(string version)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        var connection = new WorkerConnectionSnapshot(
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.Unverified,
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 1,
            "connect-ex-connected",
            DateTimeOffset.UnixEpoch,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    version,
                    WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)));

        Assert.Throws<InvalidDataException>(() =>
            channel.Send(WorkerControlMessage.Ready(12, connection)));
    }
}
