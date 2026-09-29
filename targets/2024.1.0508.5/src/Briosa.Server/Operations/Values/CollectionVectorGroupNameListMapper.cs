using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionVectorGroupNameListMapper
{
    public static WorkerCollectionVectorGroupNameListValue RequiredList(
        IReadOnlyList<Api.CollectionVectorGroupName> values, string fieldName)
    {
        if (values.Count == 0)
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        return new(values.Select(value =>
            new WorkerCollectionVectorGroupNameValue(value.CollectionName, value.VectorGroupName)).ToImmutableArray());
    }
}
