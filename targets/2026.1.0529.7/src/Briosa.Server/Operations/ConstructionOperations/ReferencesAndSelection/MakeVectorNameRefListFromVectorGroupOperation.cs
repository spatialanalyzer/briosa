using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeVectorNameRefListFromVectorGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_vector_name_ref_list_from_vector_group",
        "Make a Vector Name Ref List From a Vector Group", "briosa.ConstructionOperations",
        "MakeVectorNameRefListFromVectorGroup", "/briosa.ConstructionOperations/MakeVectorNameRefListFromVectorGroup",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_vector_name_list", "Resultant Vector Name List", WorkerMpValueKind.VectorNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeVectorNameRefListFromVectorGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name", WorkerObjectTypeValue.VectorGroup),
                "SetCollectionObjectNameArg2")],
            [new("Resultant Vector Name List", WorkerMpValueKind.VectorNameList, "GetVectorNameRefListArg")]);
    }

    public static Api.MakeVectorNameRefListFromVectorGroupResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeVectorNameRefListFromVectorGroupResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerVectorNameListValue>().Values)
            result.ResultantVectorNameList.Add(VectorNameMapper.ToProtocol(value));
        return result;
    }
}
