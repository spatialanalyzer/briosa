using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameFromTransformInWorldOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_from_transform_in_world", "Construct Frame From Transform In World",
        "briosa.ConstructionOperations", "ConstructFrameFromTransformInWorld",
        "/briosa.ConstructionOperations/ConstructFrameFromTransformInWorld", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameFromTransformInWorldRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("New Frame Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewFrameName, "new_frame_name", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2"),
            new("Transform in World Coordinates", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.TransformInWorldCoordinates, "transform_in_world_coordinates"),
                "SetTransformArg")
        ], []);
    }

    public static Api.ConstructFrameFromTransformInWorldResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
