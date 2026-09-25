using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionItemNameMapper
{
    private static WorkerCollectionItemNameValue ToWorker(Api.CollectionItemName value)
    {
        // Absence uses Any; explicitly supplied Unspecified is not a supported choice.
        var type = value.HasItemType ? (WorkerItemTypeValue)value.ItemType : WorkerItemTypeValue.Any;
        if (type == WorkerItemTypeValue.Unspecified || !Enum.IsDefined(type))
        {
            throw new ArgumentException("Item type is not supported by this SA target.", nameof(value));
        }

        return new(value.CollectionName, value.ItemName, type);
    }

    public static WorkerCollectionItemNameValue Required(Api.CollectionItemName? value, string fieldName)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ItemName))
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        return ToWorker(value);
    }

    public static WorkerCollectionItemNameListValue RequiredList(IReadOnlyList<Api.CollectionItemName> values, string fieldName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        }

        return new(values.Select(ToWorker).ToImmutableArray());
    }

    public static Api.CollectionItemName ToProtocol(WorkerCollectionItemNameValue value) => new()
    {
        CollectionName = value.CollectionName,
        ItemName = value.ItemName,
        ItemType = (Api.ItemType)value.ItemType
    };
}
