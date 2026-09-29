using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionObjectNameMapper
{
    public static WorkerCollectionObjectNameValue Required(Api.CollectionObjectName? value, string fieldName)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ObjectName))
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        return ToWorker(value);
    }

    private static WorkerCollectionObjectNameValue ToWorker(Api.CollectionObjectName value)
    {
        if (!Enum.IsDefined(value.ObjectType))
        {
            throw new ArgumentException("Object type is not supported by this SA target.", nameof(value));
        }

        // The reviewed collection-object binding treats an omitted type as Any.
        var type = value.ObjectType == Api.ObjectType.Unspecified
            ? WorkerObjectTypeValue.Any : (WorkerObjectTypeValue)value.ObjectType;
        return new(value.CollectionName, value.ObjectName, type);
    }

    public static WorkerCollectionObjectNameListValue RequiredList(IReadOnlyList<Api.CollectionObjectName> values, string fieldName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        }

        return new(values.Select(ToWorker).ToImmutableArray());
    }

    public static Api.CollectionObjectName ToProtocol(WorkerCollectionObjectNameValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.ObjectType == WorkerObjectTypeValue.Unspecified || !Enum.IsDefined(value.ObjectType))
            throw new InvalidOperationException("SpatialAnalyzer returned an unsupported object type.");
        return new()
        {
            CollectionName = value.CollectionName,
            ObjectName = value.ObjectName,
            ObjectType = (Api.ObjectType)value.ObjectType
        };
    }
}
