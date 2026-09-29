using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed record WorkerExecutionOutcome(
    WorkerExecutionStatus Status,
    WorkerExecutionDisposition ExecutionDisposition,
    WorkerMpExecutionResult? Execution,
    WorkerConnectionSnapshot? Connection,
    string DiagnosticCode,
    int Generation,
    Guid CorrelationId = default);
