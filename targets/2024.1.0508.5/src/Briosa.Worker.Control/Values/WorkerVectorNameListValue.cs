using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerVectorNameListValue : WorkerMpValue
{
    public WorkerVectorNameListValue(IReadOnlyList<WorkerVectorNameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerVectorNameValue> Values { get; }
}
