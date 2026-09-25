namespace Briosa.Worker.Control;

public sealed record WorkerControlMessage(
    int ProtocolVersion,
    WorkerControlMessageKind Kind,
    Guid CorrelationId,
    int? ProcessId = null,
    string? DiagnosticCode = null,
    WorkerConnectionSnapshot? Connection = null,
    WorkerMpCommand? Command = null,
    WorkerExecutionResponse? ExecutionResponse = null)
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

    public static WorkerControlMessage Pong(
        Guid correlationId,
        WorkerConnectionSnapshot connection) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.Pong,
            correlationId,
            Connection: connection ?? throw new ArgumentNullException(nameof(connection)));

    public static WorkerControlMessage Stop(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stop, correlationId);

    public static WorkerControlMessage Stopped(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stopped, correlationId);

    public static WorkerControlMessage VerifyExecution(Guid correlationId) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.VerifyExecution,
            correlationId);

    public static WorkerControlMessage Connect(Guid correlationId) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.Connect,
            correlationId);

    public static WorkerControlMessage ConnectionResult(
        Guid correlationId,
        WorkerConnectionSnapshot connection) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.ConnectionResult,
            correlationId,
            Connection: connection ?? throw new ArgumentNullException(nameof(connection)));

    public static WorkerControlMessage ExecutionVerificationResult(
        Guid correlationId,
        WorkerConnectionSnapshot connection) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.ExecutionVerificationResult,
            correlationId,
            Connection: connection ?? throw new ArgumentNullException(nameof(connection)));

    public static WorkerControlMessage Execute(Guid correlationId, WorkerMpCommand command) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.Execute,
            correlationId,
            Command: command ?? throw new ArgumentNullException(nameof(command)));

    public static WorkerControlMessage ExecutionResult(
        Guid correlationId,
        WorkerExecutionResponse response) =>
        new(
            WorkerControlProtocol.CurrentVersion,
            WorkerControlMessageKind.ExecutionResult,
            correlationId,
            ExecutionResponse: response ?? throw new ArgumentNullException(nameof(response)));
}
