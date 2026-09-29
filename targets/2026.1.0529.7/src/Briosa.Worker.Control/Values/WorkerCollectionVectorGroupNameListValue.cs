using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerCollectionVectorGroupNameListValue : WorkerMpValue
{
    public WorkerCollectionVectorGroupNameListValue(IReadOnlyList<WorkerCollectionVectorGroupNameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerCollectionVectorGroupNameValue> Values { get; }
}
