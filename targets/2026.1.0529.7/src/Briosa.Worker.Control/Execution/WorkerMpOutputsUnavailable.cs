namespace Briosa.Worker.Control;

/// <summary>
/// The MP did its work with raw code 2 or 4, but its output values could not be
/// delivered. The code crosses the channel so the host reports the right state.
/// </summary>
public sealed record WorkerMpOutputsUnavailable : WorkerMpExecutionResult
{
    public WorkerMpOutputsUnavailable(int resultCode, long durationMilliseconds, string? diagnosticCode)
        : base(durationMilliseconds, diagnosticCode)
    {
        if (!WorkerMpResultCodes.PermitsOutputRetrieval(resultCode))
        {
            throw new ArgumentOutOfRangeException(nameof(resultCode), "Outputs follow only MP code 2 or 4.");
        }

        ResultCode = resultCode;
    }

    public int ResultCode { get; }
}
