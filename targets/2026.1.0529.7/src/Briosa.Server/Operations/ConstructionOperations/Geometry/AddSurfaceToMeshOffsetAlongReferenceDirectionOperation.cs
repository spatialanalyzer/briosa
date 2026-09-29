using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class AddSurfaceToMeshOffsetAlongReferenceDirectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.add_surface_to_mesh_offset_along_reference_direction", "Add Surface To Mesh Offset Along Reference Direction",
        "briosa.ConstructionOperations", "AddSurfaceToMeshOffsetAlongReferenceDirection", "/briosa.ConstructionOperations/AddSurfaceToMeshOffsetAlongReferenceDirection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddSurfaceToMeshOffsetAlongReferenceDirectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference Frame Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ReferenceFrameNames, "reference_frame_names"), "SetCollectionObjectNameRefListArg"),
            new("Surface for Offset Distance Computation", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SurfaceForOffsetDistanceComputation, "surface_for_offset_distance_computation", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Surface Offset Range", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSurfaceOffsetRange ? request.SurfaceOffsetRange : 10), "SetDoubleArg"),
            new("Collection for Result Frames", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionForResultFrames ? request.CollectionForResultFrames : string.Empty), "SetStringArg"),
            new("Object Providing Direction Reference", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectProvidingDirectionReference, "object_providing_direction_reference"), "SetCollectionObjectNameArg2"),
            new("Bi-directional projection?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasBiDirectionalProjection || request.BiDirectionalProjection), "SetBoolArg"),
            new("Mesh Serving As Projection Target", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.MeshServingAsProjectionTarget, "mesh_serving_as_projection_target", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.AddSurfaceToMeshOffsetAlongReferenceDirectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
