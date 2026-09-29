using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_auto_correspond_two_groups_inter_point_distance", "Construct Points Auto-Correspond 2 groups Inter-Point Distance",
        "briosa.ConstructionOperations", "ConstructPointsAutoCorrespondTwoGroupsInterPointDistance", "/briosa.ConstructionOperations/ConstructPointsAutoCorrespondTwoGroupsInterPointDistance",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference group (known point names)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Group to be copied (unknown point names)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupToBeCopied, "group_to_be_copied", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Auto-correspond same-point tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSamePointTolerance ? request.SamePointTolerance : 0.1), "SetDoubleArg"),
            new("Group to contain matched points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupToContainMatchedPoints, "group_to_contain_matched_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
