using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionObjectNameRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_object_name_ref_list_runtime_select", "Make a Collection Object Name Reference List- Runtime Select",
        "briosa.ConstructionOperations", "MakeCollectionObjectNameRefListRuntimeSelect", "/briosa.ConstructionOperations/MakeCollectionObjectNameRefListRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_object_name_ref_list", "Resultant Collection Object Name Reference List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg"),
            new("Object Type", WorkerMpValueKind.ObjectType, ObjectTypeMapper.Required(request.ObjectType), "SetObjectTypeArg")
        ], [new("Resultant Collection Object Name Reference List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCollectionObjectNameRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionObjectNameRefListRuntimeSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.ResultantCollectionObjectNameRefList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
