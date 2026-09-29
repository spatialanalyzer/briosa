namespace Briosa.Worker.Control;

/// <summary>ExecuteStep returned false; this does not prove the MP did not start.</summary>
public sealed record WorkerExecuteRejected(long DurationMilliseconds, string? DiagnosticCode)
    : WorkerMpExecutionResult(DurationMilliseconds, DiagnosticCode);
