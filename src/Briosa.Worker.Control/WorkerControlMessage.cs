namespace Briosa.Worker.Control;

public static class WorkerControlProtocol
{
    public const int CurrentVersion = 2;

    public const int MaximumMessageBytes = 64 * 1024;
}

public enum WorkerControlMessageKind
{
    None = 0,
    Ready,
    Ping,
    Pong,
    Stop,
    Stopped
}

public enum WorkerConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted,
    Stopping
}

public sealed record WorkerConnectionSnapshot(
    WorkerConnectionState State,
    string TargetHost,
    int? StatusCode,
    int Attempt,
    int MaximumAttempts,
    string DiagnosticCode,
    DateTimeOffset TransitionedAt);

public sealed record WorkerControlMessage(
    int ProtocolVersion,
    WorkerControlMessageKind Kind,
    Guid CorrelationId,
    int? ProcessId = null,
    string? DiagnosticCode = null,
    WorkerConnectionSnapshot? Connection = null)
{
    public static WorkerControlMessage Ready(
        int processId,
        WorkerConnectionSnapshot connection) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.Ready,
            Guid.Empty,
            processId,
            Connection: connection ?? throw new ArgumentNullException(nameof(connection)));

    public static WorkerControlMessage Ping(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Ping, correlationId);

    public static WorkerControlMessage Pong(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Pong, correlationId);

    public static WorkerControlMessage Stop(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stop, correlationId);

    public static WorkerControlMessage Stopped(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stopped, correlationId);
}
