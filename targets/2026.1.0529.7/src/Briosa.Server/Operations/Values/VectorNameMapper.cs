using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class VectorNameMapper
{
    public static WorkerVectorNameListValue RequiredList(IReadOnlyList<Api.VectorName> values, string fieldName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        }

        return new(values.Select(value => new WorkerVectorNameValue(value.CollectionName, value.GroupName, value.Name)).ToImmutableArray());
    }

    public static Api.VectorName ToProtocol(WorkerVectorNameValue value) => new()
    {
        CollectionName = value.CollectionName,
        GroupName = value.GroupName,
        Name = value.VectorName
    };
}
