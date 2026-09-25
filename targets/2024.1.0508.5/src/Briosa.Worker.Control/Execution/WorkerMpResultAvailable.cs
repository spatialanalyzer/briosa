using System.Collections.Immutable;

namespace Briosa.Worker.Control;

/// <summary>A retrieved raw MP code; only code 2 permits output retrieval.</summary>
public sealed record WorkerMpResultAvailable : WorkerMpExecutionResult
{
    private readonly ImmutableArray<WorkerMpOutputValue> _outputs;

    public WorkerMpResultAvailable(
        int resultCode,
        long durationMilliseconds,
        IReadOnlyList<WorkerMpOutputValue> outputs,
        string? diagnosticCode)
        : base(durationMilliseconds, diagnosticCode)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        if (resultCode != 2 && outputs.Count != 0)
        {
            throw new ArgumentException("Outputs cannot be retrieved after an unsuccessful MP result.", nameof(outputs));
        }

        ResultCode = resultCode;
        _outputs = [.. outputs];
    }

    public int ResultCode { get; }
    public IReadOnlyList<WorkerMpOutputValue> Outputs => _outputs;
}
