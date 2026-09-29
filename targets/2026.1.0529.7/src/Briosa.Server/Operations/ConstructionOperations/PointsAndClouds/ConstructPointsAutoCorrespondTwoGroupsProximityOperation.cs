using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsAutoCorrespondTwoGroupsProximityOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_auto_correspond_two_groups_proximity", "Construct Points Auto-Correspond 2 groups Proximity",
        "briosa.ConstructionOperations", "ConstructPointsAutoCorrespondTwoGroupsProximity", "/briosa.ConstructionOperations/ConstructPointsAutoCorrespondTwoGroupsProximity",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsAutoCorrespondTwoGroupsProximityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference group (known point names)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Group to be copied (unknown point names)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupToBeCopied, "group_to_be_copied", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Auto-correspond same-point tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSamePointTolerance ? request.SamePointTolerance : 0.25), "SetDoubleArg"),
            new("Group to contain matched points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupToContainMatchedPoints, "group_to_contain_matched_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointsAutoCorrespondTwoGroupsProximityResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
