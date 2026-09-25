namespace Briosa.Worker.Control;

/// <summary>ExecuteStep returned true, but GetMPStepResult did not retrieve a result.</summary>
public sealed record WorkerMpResultUnavailable(long DurationMilliseconds, string? DiagnosticCode)
    : WorkerMpExecutionResult(DurationMilliseconds, DiagnosticCode);
