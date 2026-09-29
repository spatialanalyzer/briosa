using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionObjectNameRefListByTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_object_name_ref_list_by_type", "Make a Collection Object Name Ref List - By Type",
        "briosa.ConstructionOperations", "MakeCollectionObjectNameRefListByType", "/briosa.ConstructionOperations/MakeCollectionObjectNameRefListByType",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_object_name_list", "Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameRefListByTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection", WorkerMpValueKind.Text, new WorkerTextValue(request.Collection), "SetStringArg"),
            new("Object Type", WorkerMpValueKind.ObjectType, ObjectTypeMapper.Required(request.ObjectType), "SetObjectTypeArg")
        ], [new("Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCollectionObjectNameRefListByTypeResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionObjectNameRefListByTypeResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.ResultantCollectionObjectNameList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
