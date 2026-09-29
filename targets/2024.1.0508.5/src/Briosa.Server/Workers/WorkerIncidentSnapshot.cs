namespace Briosa.Server.Workers;

internal sealed record WorkerIncidentSnapshot(
    int Generation,
    WorkerTerminationKind Termination,
    WorkerExecutionDisposition? ExecutionDisposition,
    string? OperationId,
    string DiagnosticCode);
