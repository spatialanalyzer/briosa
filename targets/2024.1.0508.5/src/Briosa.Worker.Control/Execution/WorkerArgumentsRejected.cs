namespace Briosa.Worker.Control;

/// <summary>An input setter rejected the command before ExecuteStep was called.</summary>
public sealed record WorkerArgumentsRejected(long DurationMilliseconds, string? DiagnosticCode)
    : WorkerMpExecutionResult(DurationMilliseconds, DiagnosticCode);
