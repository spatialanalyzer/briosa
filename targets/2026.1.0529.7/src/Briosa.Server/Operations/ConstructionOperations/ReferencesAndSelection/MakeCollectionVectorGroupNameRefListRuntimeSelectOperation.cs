using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionVectorGroupNameRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_vector_group_name_ref_list_runtime_select",
        "Make a Collection Vector Group Name Ref List - Runtime Select", "briosa.ConstructionOperations",
        "MakeCollectionVectorGroupNameRefListRuntimeSelect",
        "/briosa.ConstructionOperations/MakeCollectionVectorGroupNameRefListRuntimeSelect", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_vector_group_name_reference_list",
            "Resultant Collection Vector Group Name Reference List", WorkerMpValueKind.CollectionVectorGroupNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionVectorGroupNameRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Resultant Collection Vector Group Name Reference List", WorkerMpValueKind.CollectionVectorGroupNameList,
                "GetCollectionVectorGroupNameRefListArg")]);
    }

    public static Api.MakeCollectionVectorGroupNameRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionVectorGroupNameRefListRuntimeSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0]
            .RequireValue<WorkerCollectionVectorGroupNameListValue>().Values)
            result.ResultantCollectionVectorGroupNameReferenceList.Add(new Api.CollectionVectorGroupName
            {
                CollectionName = value.CollectionName,
                VectorGroupName = value.VectorGroupName
            });
        return result;
    }
}
