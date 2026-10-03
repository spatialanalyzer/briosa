namespace Briosa.Worker.Control;

/// <summary>
/// An input setter rejected the command, or an SDK call faulted, before ExecuteStep
/// was called. <see cref="WorkerSdkFaultDiagnosticCodes.BeforeExecute"/> marks the fault.
/// </summary>
public sealed record WorkerArgumentsRejected(long DurationMilliseconds, string? DiagnosticCode)
    : WorkerMpExecutionResult(DurationMilliseconds, DiagnosticCode);
