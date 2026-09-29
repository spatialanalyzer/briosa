using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerCollectionObjectNameListValue : WorkerMpValue
{
    public WorkerCollectionObjectNameListValue(IReadOnlyList<WorkerCollectionObjectNameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerCollectionObjectNameValue> Values { get; }
}
