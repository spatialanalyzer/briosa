using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerStringListValue
{
    public WorkerStringListValue(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<string> Values { get; }
}
