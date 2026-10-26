using System.Collections.Immutable;

namespace Briosa.Worker.Control;

/// <summary>
/// One SDK call threw while the worker STA stayed healthy. The result carries the
/// proven <see cref="Phase"/> and a value-free diagnostic code, never exception
/// text. Only an <see cref="WorkerSdkCallPhase.OutputGetter"/> fault follows a
/// retrieved MP code, 2 or 4, which it carries as <see cref="ResultCode"/>; it
/// keeps every output in request order, with at least the faulted output
/// unavailable. An output-getter fault with no outputs means the
/// per-output evidence was withheld because even its value-free form could not
/// be encoded; the host then reports every requested output as failed.
/// </summary>
public sealed record WorkerSdkCallFaulted : WorkerMpExecutionResult
{
    private readonly ImmutableArray<WorkerMpOutputValue> _outputs;

    public WorkerSdkCallFaulted(
        WorkerSdkCallPhase phase,
        long durationMilliseconds,
        IReadOnlyList<WorkerMpOutputValue> outputs,
        string diagnosticCode,
        int? resultCode = null)
        : base(durationMilliseconds, diagnosticCode)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticCode);
        if (!Enum.IsDefined(phase) || phase == WorkerSdkCallPhase.None)
        {
            throw new ArgumentOutOfRangeException(nameof(phase), "The SDK call phase is unknown.");
        }

        if (phase == WorkerSdkCallPhase.OutputGetter
                ? outputs.Count != 0 && !outputs.Any(output => output is { Retrieved: false })
                : outputs.Count != 0)
        {
            throw new ArgumentException(
                "Only an output-getter fault carries outputs, and at least one of them is unavailable.",
                nameof(outputs));
        }

        if (phase == WorkerSdkCallPhase.OutputGetter
                ? resultCode is not { } code || !WorkerMpResultCodes.PermitsOutputRetrieval(code)
                : resultCode is not null)
        {
            throw new ArgumentException(
                "Only an output-getter fault carries the retrieved MP code, and it is 2 or 4.",
                nameof(resultCode));
        }

        Phase = phase;
        ResultCode = resultCode;
        _outputs = [.. outputs];
    }

    public WorkerSdkCallPhase Phase { get; }

    /// <summary>The MP code retrieved before an output getter faulted; otherwise null.</summary>
    public int? ResultCode { get; }

    public IReadOnlyList<WorkerMpOutputValue> Outputs => _outputs;
}
