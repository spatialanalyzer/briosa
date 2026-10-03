namespace Briosa.Worker.Control;

/// <summary>
/// ExecuteStep returned true, but GetMPStepResult did not retrieve a result. A fault
/// in ExecuteStep or GetMPStepResult also reports this unknown outcome, marked by its
/// <see cref="WorkerSdkFaultDiagnosticCodes"/> code.
/// </summary>
public sealed record WorkerMpResultUnavailable(long DurationMilliseconds, string? DiagnosticCode)
    : WorkerMpExecutionResult(DurationMilliseconds, DiagnosticCode);
