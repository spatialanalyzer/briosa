using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_plane", "Construct Plane",
        "briosa.ConstructionOperations", "ConstructPlane", "/briosa.ConstructionOperations/ConstructPlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PlaneName, "plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2"),
            new("Plane Center (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.PlaneCenter, "plane_center"), "SetVectorArg"),
            new("Plane Normal (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.PlaneNormal, "plane_normal"), "SetVectorArg"),
            new("Plane Edge Dimension", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPlaneEdgeDimension ? request.PlaneEdgeDimension : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
