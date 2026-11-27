using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionItemNameMapper
{
    private static WorkerCollectionItemNameValue ToWorker(
        Api.CollectionItemName value,
        string fieldName,
        WorkerItemTypeValue itemTypeWhenOmitted = WorkerItemTypeValue.Any)
    {
        // A reviewed operation-specific fallback may be supplied; other omissions use Any.
        // Explicit Unspecified is never a supported choice.
        var type = value.HasItemType ? (WorkerItemTypeValue)value.ItemType : itemTypeWhenOmitted;
        if (type == WorkerItemTypeValue.Unspecified || !Enum.IsDefined(type))
        {
            throw RequestFieldViolation.SupportedChoice(RequestFieldViolation.Path(fieldName, "item_type"));
        }

        return new(value.CollectionName, value.ItemName, type);
    }

    public static WorkerCollectionItemNameValue Required(
        Api.CollectionItemName? value,
        string fieldName,
        WorkerItemTypeValue itemTypeWhenOmitted = WorkerItemTypeValue.Any)
    {
        if (value is null)
            throw RequestFieldViolation.Required(fieldName);
        if (string.IsNullOrWhiteSpace(value.ItemName))
            throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "item_name"));

        return ToWorker(value, fieldName, itemTypeWhenOmitted);
    }

    public static WorkerCollectionItemNameListValue RequiredList(
        IReadOnlyList<Api.CollectionItemName> values, string fieldName,
        WorkerItemTypeValue itemTypeWhenOmitted = WorkerItemTypeValue.Any)
    {
        if (values.Count == 0)
        {
            throw RequestFieldViolation.NotEmpty(fieldName);
        }

        return new(values.Select(value => ToWorker(value, fieldName, itemTypeWhenOmitted)).ToImmutableArray());
    }

    public static WorkerCollectionItemNameListValue AllowEmptyList(
        IReadOnlyList<Api.CollectionItemName> values, string fieldName) =>
        new(values.Select(value => ToWorker(value, fieldName)).ToImmutableArray());

    public static Api.CollectionItemName ToProtocol(WorkerCollectionItemNameValue value) => new()
    {
        CollectionName = value.CollectionName,
        ItemName = value.ItemName,
        ItemType = (Api.ItemType)value.ItemType
    };
}
