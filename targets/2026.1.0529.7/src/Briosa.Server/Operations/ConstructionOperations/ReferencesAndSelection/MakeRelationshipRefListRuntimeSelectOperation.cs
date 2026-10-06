using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeRelationshipRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_relationship_ref_list_runtime_select",
        "Make a Relationship Reference List- Runtime Select", "briosa.ConstructionOperations",
        "MakeRelationshipRefListRuntimeSelect", "/briosa.ConstructionOperations/MakeRelationshipRefListRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_relationship_ref_list", "Resultant Relationship Reference List", WorkerMpValueKind.CollectionItemNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeRelationshipRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Resultant Relationship Reference List", WorkerMpValueKind.CollectionItemNameList,
                "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeRelationshipRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeRelationshipRefListRuntimeSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameListValue>().Values)
            result.ResultantRelationshipRefList.Add(CollectionItemNameMapper.ToProtocol(value));
        return result;
    }
}
