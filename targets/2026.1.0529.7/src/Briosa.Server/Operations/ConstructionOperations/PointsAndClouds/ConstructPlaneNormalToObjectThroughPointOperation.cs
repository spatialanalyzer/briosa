using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPlaneNormalToObjectThroughPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_plane_normal_to_object_through_point", "Construct Plane, Normal to Object, Through Point",
        "briosa.ConstructionOperations", "ConstructPlaneNormalToObjectThroughPoint",
        "/briosa.ConstructionOperations/ConstructPlaneNormalToObjectThroughPoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPlaneNormalToObjectThroughPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resultant Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultantPlaneName, "resultant_plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("'Normal to' Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NormalToObjectName, "normal_to_object_name"), "SetCollectionObjectNameArg2"),
            new("'Through' Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ThroughPointName, "through_point_name"), "SetPointNameArg"),
            new("Plane Edge Dimension", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPlaneEdgeDimension ? request.PlaneEdgeDimension : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructPlaneNormalToObjectThroughPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
