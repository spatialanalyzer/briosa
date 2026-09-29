using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionItemNameRefListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_item_name_ref_list_wildcard_selection",
        "Make a Collection Item Name Reference List - WildCard Selection",
        "briosa.ConstructionOperations", "MakeCollectionItemNameRefListWildcardSelection",
        "/briosa.ConstructionOperations/MakeCollectionItemNameRefListWildcardSelection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_item_name_ref_list", "Resultant Collection Item Name Reference List",
            WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionItemNameRefListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasItemType || request.ItemType == Api.ItemType.Unspecified || !Enum.IsDefined(request.ItemType))
            throw new ArgumentException("Item type must specify a supported value.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Item Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasItemWildcardCriteria ? request.ItemWildcardCriteria : "*"), "SetStringArg"),
            // Exact-target live validation established the string setter for this typed choice.
            new("Item Type", WorkerMpValueKind.ItemType,
                new WorkerChoiceValue<WorkerItemTypeValue>((WorkerItemTypeValue)(int)request.ItemType), "SetStringArg")
        ], [new("Resultant Collection Item Name Reference List", WorkerMpValueKind.CollectionItemNameList,
            "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCollectionItemNameRefListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionItemNameRefListWildcardSelectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.ResultantCollectionItemNameRefList.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
