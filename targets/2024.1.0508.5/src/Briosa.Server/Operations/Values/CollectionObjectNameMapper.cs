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
        if (value is null)
        {
            throw RequestFieldViolation.Required(fieldName);
        }

        if (string.IsNullOrWhiteSpace(value.ObjectName))
        {
            throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "object_name"));
        }

        return ToWorker(value, fieldName, objectTypeWhenOmitted);
    }

    private static WorkerCollectionObjectNameValue ToWorker(
        Api.CollectionObjectName value,
        string fieldName,
        WorkerObjectTypeValue objectTypeWhenOmitted)
    {
        if (!Enum.IsDefined(value.ObjectType))
        {
            throw RequestFieldViolation.SupportedChoice(RequestFieldViolation.Path(fieldName, "object_type"));
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
            throw RequestFieldViolation.NotEmpty(fieldName);
        }

        return new(values.Select(value => ToWorker(value, fieldName, objectTypeWhenOmitted)).ToImmutableArray());
    }

    public static WorkerCollectionObjectNameListValue AllowEmptyList(
        IReadOnlyList<Api.CollectionObjectName> values, string fieldName) =>
        new(values.Select(value => ToWorker(value, fieldName, WorkerObjectTypeValue.Any)).ToImmutableArray());
    public static WorkerCollectionObjectNameListValue RequiredList(IReadOnlyList<Api.CollectionItemName> values, string fieldName)
    {
        if (values.Count == 0)
            throw RequestFieldViolation.NotEmpty(fieldName);

        return new(values.Select(value =>
        {
            if (string.IsNullOrWhiteSpace(value.ItemName))
                throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "item_name"));
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
