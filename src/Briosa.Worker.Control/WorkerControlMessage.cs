namespace Briosa.Worker.Control;

public static class WorkerControlProtocol
{
    public const int CurrentVersion = 1;

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

public sealed record WorkerControlMessage(
    int ProtocolVersion,
    WorkerControlMessageKind Kind,
    Guid CorrelationId,
    int? ProcessId = null,
    string? DiagnosticCode = null)
{
    public static WorkerControlMessage Ready(int processId) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.Ready,
            Guid.Empty,
            processId);

    public static WorkerControlMessage Ping(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Ping, correlationId);

    public static WorkerControlMessage Pong(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Pong, correlationId);

    public static WorkerControlMessage Stop(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stop, correlationId);

    public static WorkerControlMessage Stopped(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stopped, correlationId);
}
