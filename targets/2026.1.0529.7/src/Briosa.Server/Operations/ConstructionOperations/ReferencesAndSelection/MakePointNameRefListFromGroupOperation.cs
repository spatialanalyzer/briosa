using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakePointNameRefListFromGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_point_name_ref_list_from_group", "Make a Point Name Ref List From a Group",
        "briosa.ConstructionOperations", "MakePointNameRefListFromGroup", "/briosa.ConstructionOperations/MakePointNameRefListFromGroup",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakePointNameRefListFromGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupName, "group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")],
            [new("Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.MakePointNameRefListFromGroupResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakePointNameRefListFromGroupResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
            result.ResultantPointNameList.Add(PointNameMapper.ToProtocol(value));
        return result;
    }
}
