using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerCollectionGroupNameListValue
{
    public WorkerCollectionGroupNameListValue(IReadOnlyList<WorkerCollectionGroupNameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerCollectionGroupNameValue> Values { get; }
}
