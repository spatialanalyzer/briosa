using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointGroupFromPointNameRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_group_from_point_name_ref_list", "Construct Point Group from Point Name Ref List",
        "briosa.ConstructionOperations", "ConstructPointGroupFromPointNameRefList", "/briosa.ConstructionOperations/ConstructPointGroupFromPointNameRefList",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointGroupFromPointNameRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name List", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNameList, "point_name_list"), "SetPointNameRefListArg"),
            new("Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupName, "group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointGroupFromPointNameRefListResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
