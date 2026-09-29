using System.Collections.Immutable;

namespace Briosa.Worker.Control;

public sealed record WorkerPointNameListValue : WorkerMpValue
{
    public WorkerPointNameListValue(IReadOnlyList<WorkerPointNameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values.ToImmutableArray();
    }

    public IReadOnlyList<WorkerPointNameValue> Values { get; }
}
