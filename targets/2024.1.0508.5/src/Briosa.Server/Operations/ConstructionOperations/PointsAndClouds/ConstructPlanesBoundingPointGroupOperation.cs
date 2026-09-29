using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPlanesBoundingPointGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_planes_bounding_point_group", "Construct Planes, Bounding Point Group",
        "briosa.ConstructionOperations", "ConstructPlanesBoundingPointGroup",
        "/briosa.ConstructionOperations/ConstructPlanesBoundingPointGroup",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPlanesBoundingPointGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferencePlaneName, "reference_plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Group to bound", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupToBound, "group_to_bound", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Resulting 'High' Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingHighPlaneName, "resulting_high_plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Resulting 'Low' Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingLowPlaneName, "resulting_low_plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Override Target/Point Offsets", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOverrideTargetPointOffsets && request.OverrideTargetPointOffsets), "SetBoolArg"),
            new("Offset Value", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasOffsetValue ? request.OffsetValue : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructPlanesBoundingPointGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
