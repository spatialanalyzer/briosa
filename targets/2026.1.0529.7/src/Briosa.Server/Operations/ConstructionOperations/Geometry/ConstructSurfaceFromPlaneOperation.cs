using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSurfaceFromPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_surface_from_plane", "Construct Surface From Plane",
        "briosa.ConstructionOperations", "ConstructSurfaceFromPlane", "/briosa.ConstructionOperations/ConstructSurfaceFromPlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSurfaceFromPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingSurfaceName, "resulting_surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Plane Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PlaneName, "plane_name", WorkerObjectTypeValue.Plane), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructSurfaceFromPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
