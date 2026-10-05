using System.Collections.Immutable;

namespace Briosa.Worker.Control;

/// <summary>
/// One SDK call threw while the worker STA stayed healthy. The result carries the
/// proven <see cref="Phase"/> and a value-free diagnostic code, never exception
/// text. Only an <see cref="WorkerSdkCallPhase.OutputGetter"/> fault follows
/// retrieved MP code 2; it keeps every output in request order, with at least the
/// faulted output unavailable.
/// </summary>
public sealed record WorkerSdkCallFaulted : WorkerMpExecutionResult
{
    private readonly ImmutableArray<WorkerMpOutputValue> _outputs;

    public WorkerSdkCallFaulted(
        WorkerSdkCallPhase phase,
        long durationMilliseconds,
        IReadOnlyList<WorkerMpOutputValue> outputs,
        string diagnosticCode)
        : base(durationMilliseconds, diagnosticCode)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticCode);
        if (!Enum.IsDefined(phase) || phase == WorkerSdkCallPhase.None)
        {
            throw new ArgumentOutOfRangeException(nameof(phase), "The SDK call phase is unknown.");
        }

        if (phase == WorkerSdkCallPhase.OutputGetter
                ? !outputs.Any(output => output is { Retrieved: false })
                : outputs.Count != 0)
        {
            throw new ArgumentException(
                "Only an output-getter fault carries outputs, and at least one of them is unavailable.",
                nameof(outputs));
        }

        Phase = phase;
        _outputs = [.. outputs];
    }

    public WorkerSdkCallPhase Phase { get; }

    public IReadOnlyList<WorkerMpOutputValue> Outputs => _outputs;
}
