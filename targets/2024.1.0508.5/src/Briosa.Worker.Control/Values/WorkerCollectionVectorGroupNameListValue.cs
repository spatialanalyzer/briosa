using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerCollectionVectorGroupNameListValue
{
    public WorkerCollectionVectorGroupNameListValue(IReadOnlyList<WorkerCollectionVectorGroupNameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerCollectionVectorGroupNameValue> Values { get; }
}
