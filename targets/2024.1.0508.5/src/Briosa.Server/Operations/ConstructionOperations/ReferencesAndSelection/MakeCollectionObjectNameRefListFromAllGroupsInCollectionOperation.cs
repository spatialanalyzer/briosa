using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionObjectNameRefListFromAllGroupsInCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_object_name_ref_list_from_all_groups_in_collection", "Make a Collection Object Name Ref List from all Groups in a Collection",
        "briosa.ConstructionOperations", "MakeCollectionObjectNameRefListFromAllGroupsInCollection", "/briosa.ConstructionOperations/MakeCollectionObjectNameRefListFromAllGroupsInCollection",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("collection_object_name_list", "Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.CollectionName, "collection_name"), "SetCollectionNameArg")],
            [new("Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.CollectionObjectNameList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
