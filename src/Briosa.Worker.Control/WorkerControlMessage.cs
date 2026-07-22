namespace Briosa.Worker.Control;

public static class WorkerControlProtocol
{
    public const int CurrentVersion = 4;

    public const int MaximumMessageBytes = 64 * 1024;
}

public enum WorkerControlMessageKind
{
    None = 0,
    Ready,
    Ping,
    Pong,
    Stop,
    Stopped,
    Execute,
    ExecutionResult
}

public enum WorkerMpValueKind
{
    Logical,
    WholeNumber,
    FloatingPoint,
    Text,
    PointName,
    Vector,
    ToleranceVectorOptions
}

public enum WorkerExecutionResponseStatus
{
    Completed,
    Unavailable
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

public sealed record WorkerPointNameValue(
    string CollectionName,
    string GroupName,
    string TargetName);

public sealed record WorkerVectorValue(double X, double Y, double Z);

public sealed record WorkerToleranceLimit(bool Enabled, double Value);

public sealed record WorkerToleranceVectorOptionsValue(
    WorkerToleranceLimit HighX,
    WorkerToleranceLimit HighY,
    WorkerToleranceLimit HighZ,
    WorkerToleranceLimit HighMagnitude,
    WorkerToleranceLimit LowX,
    WorkerToleranceLimit LowY,
    WorkerToleranceLimit LowZ,
    WorkerToleranceLimit LowMagnitude);

public sealed record WorkerMpInputArgument(
    string Name,
    WorkerMpValueKind Kind,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    WorkerPointNameValue? PointNameValue = null,
    WorkerVectorValue? VectorValue = null,
    WorkerToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
    string? SdkBinding = null);

public sealed record WorkerMpOutputArgument(
    string Name,
    WorkerMpValueKind Kind,
    string? SdkBinding = null);

public sealed record WorkerMpOutputValue(
    string Name,
    WorkerMpValueKind Kind,
    bool Retrieved,
    bool? BooleanValue = null,
    int? IntegerValue = null,
    double? DoubleValue = null,
    string? StringValue = null,
    WorkerPointNameValue? PointNameValue = null,
    WorkerVectorValue? VectorValue = null,
    WorkerToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null);

public sealed record WorkerMpCommand(
    string OperationId,
    string StepName,
    IReadOnlyList<WorkerMpInputArgument> InputArguments,
    IReadOnlyList<WorkerMpOutputArgument> OutputArguments);

public sealed record WorkerMpExecutionResult(
    bool ExecuteStepReturned,
    bool MpSucceeded,
    int MpResultCode,
    long DurationMilliseconds,
    IReadOnlyList<WorkerMpOutputValue> OutputValues,
    string? DiagnosticCode);

public sealed record WorkerExecutionResponse(
    WorkerExecutionResponseStatus Status,
    WorkerMpExecutionResult? Execution,
    WorkerConnectionSnapshot Connection,
    string? DiagnosticCode);

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

    public static WorkerControlMessage Pong(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Pong, correlationId);

    public static WorkerControlMessage Stop(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stop, correlationId);

    public static WorkerControlMessage Stopped(Guid correlationId) =>
        new(WorkerControlProtocol.CurrentVersion, WorkerControlMessageKind.Stopped, correlationId);

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
