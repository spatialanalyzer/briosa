namespace Briosa.Worker.Control;

public sealed record WorkerExecutionResponse(
    WorkerExecutionResponseStatus Status,
    WorkerMpExecutionResult? Execution,
    WorkerConnectionSnapshot Connection,
    string? DiagnosticCode);
