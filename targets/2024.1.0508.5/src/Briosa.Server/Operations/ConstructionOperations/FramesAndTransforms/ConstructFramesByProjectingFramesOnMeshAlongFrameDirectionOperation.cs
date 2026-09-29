using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frames_by_projecting_frames_on_mesh_along_frame_direction", "Construct Frames By Projecting Frames On Mesh Along Frame Direction",
        "briosa.ConstructionOperations", "ConstructFramesByProjectingFramesOnMeshAlongFrameDirection", "/briosa.ConstructionOperations/ConstructFramesByProjectingFramesOnMeshAlongFrameDirection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_frame_name_list", "Resultant Frame Name List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference Frame Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ReferenceFrameNames, "reference_frame_names"), "SetCollectionObjectNameRefListArg"),
            new("Base Name For Projected Frames", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.BaseNameForProjectedFrames, "base_name_for_projected_frames", WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2"),
            new("Bi-directional projection?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasBiDirectionalProjection || request.BiDirectionalProjection), "SetBoolArg"),
            new("Mesh Serving As Projection Target", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.MeshServingAsProjectionTarget, "mesh_serving_as_projection_target", WorkerObjectTypeValue.ScanStripeMesh), "SetCollectionObjectNameArg2")
        ], [new("Resultant Frame Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.ResultantFrameNameList.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
