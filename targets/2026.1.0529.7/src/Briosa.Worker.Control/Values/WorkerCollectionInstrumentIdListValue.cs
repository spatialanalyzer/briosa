using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerCollectionInstrumentIdListValue
{
    public WorkerCollectionInstrumentIdListValue(IReadOnlyList<WorkerCollectionInstrumentIdValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerCollectionInstrumentIdValue> Values { get; }
}
