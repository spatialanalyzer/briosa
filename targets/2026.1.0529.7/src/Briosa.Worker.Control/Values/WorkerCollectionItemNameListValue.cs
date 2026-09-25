using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerCollectionItemNameListValue : WorkerMpValue
{
    public WorkerCollectionItemNameListValue(IReadOnlyList<WorkerCollectionItemNameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerCollectionItemNameValue> Values { get; }
}
