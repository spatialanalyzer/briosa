using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerStringListValue : WorkerMpValue
{
    public WorkerStringListValue(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<string> Values { get; }
}
