using System.Collections.Immutable;

namespace Briosa.Worker.Control;

/// <summary>A retrieved raw MP code; only codes 2 and 4 permit output retrieval.</summary>
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
        if (!WorkerMpResultCodes.PermitsOutputRetrieval(resultCode) && outputs.Count != 0)
        {
            throw new ArgumentException("Outputs cannot be retrieved after an unsuccessful MP result.", nameof(outputs));
        }

        ResultCode = resultCode;
        _outputs = [.. outputs];
    }

    public int ResultCode { get; }
    public IReadOnlyList<WorkerMpOutputValue> Outputs => _outputs;
}
