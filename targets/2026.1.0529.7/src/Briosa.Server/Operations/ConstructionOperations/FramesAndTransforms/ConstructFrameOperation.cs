using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame", "Construct Frame", "briosa.ConstructionOperations",
        "ConstructFrame", "/briosa.ConstructionOperations/ConstructFrame", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("New Frame Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewFrameName, "new_frame_name", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2"),
            new("Transform in Working Coordinates", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.TransformInWorkingCoordinates, "transform_in_working_coordinates"),
                "SetTransformArg")
        ], []);
    }

    public static Api.ConstructFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
