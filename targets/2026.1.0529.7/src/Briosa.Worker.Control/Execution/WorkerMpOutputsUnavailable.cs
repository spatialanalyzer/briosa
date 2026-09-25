namespace Briosa.Worker.Control;

/// <summary>The MP succeeded with raw code 2, but its output values could not be delivered.</summary>
public sealed record WorkerMpOutputsUnavailable : WorkerMpExecutionResult
{
    public WorkerMpOutputsUnavailable(long durationMilliseconds, string? diagnosticCode)
        : base(durationMilliseconds, diagnosticCode) { }
}
