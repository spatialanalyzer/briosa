using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionObjectNameMapper
{
    public static WorkerCollectionObjectNameValue Required(Api.CollectionObjectName? value, string fieldName)
        => Required(value, fieldName, WorkerObjectTypeValue.Any);

    public static WorkerCollectionObjectNameValue Required(
        Api.CollectionObjectName? value,
        string fieldName,
        WorkerObjectTypeValue objectTypeWhenOmitted)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ObjectName))
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        return ToWorker(value, objectTypeWhenOmitted);
    }

    private static WorkerCollectionObjectNameValue ToWorker(
        Api.CollectionObjectName value,
        WorkerObjectTypeValue objectTypeWhenOmitted)
    {
        if (!Enum.IsDefined(value.ObjectType))
        {
            throw new ArgumentException("Object type is not supported by this SA target.", nameof(value));
        }

        // Preserve the operation's documented object type when protobuf omits it.
        var type = value.ObjectType == Api.ObjectType.Unspecified
            ? objectTypeWhenOmitted : (WorkerObjectTypeValue)value.ObjectType;
        return new(value.CollectionName, value.ObjectName, type);
    }

    public static WorkerCollectionObjectNameListValue RequiredList(IReadOnlyList<Api.CollectionObjectName> values, string fieldName)
        => RequiredList(values, fieldName, WorkerObjectTypeValue.Any);

    public static WorkerCollectionObjectNameListValue RequiredList(
        IReadOnlyList<Api.CollectionObjectName> values,
        string fieldName,
        WorkerObjectTypeValue objectTypeWhenOmitted)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        }

        return new(values.Select(value => ToWorker(value, objectTypeWhenOmitted)).ToImmutableArray());
    }

    public static WorkerCollectionObjectNameListValue AllowEmptyList(IReadOnlyList<Api.CollectionObjectName> values) =>
        new(values.Select(value => ToWorker(value, WorkerObjectTypeValue.Any)).ToImmutableArray());
    public static WorkerCollectionObjectNameListValue RequiredList(IReadOnlyList<Api.CollectionItemName> values, string fieldName)
    {
        if (values.Count == 0)
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));

        return new(values.Select(value =>
        {
            if (string.IsNullOrWhiteSpace(value.ItemName))
                throw new ArgumentException($"Request field '{fieldName}' contains an empty item name.", nameof(values));
            return new WorkerCollectionObjectNameValue(value.CollectionName, value.ItemName, WorkerObjectTypeValue.Any);
        }).ToImmutableArray());
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
