using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerTransformValue : WorkerMpValue
{
    public WorkerTransformValue(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<double> Values { get; }
}
