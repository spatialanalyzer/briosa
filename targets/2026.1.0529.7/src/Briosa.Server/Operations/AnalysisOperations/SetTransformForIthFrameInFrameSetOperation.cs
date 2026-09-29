using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetTransformForIthFrameInFrameSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_transform_for_ith_frame_in_frame_set", "Set Transform for i-th Frame in Frame Set",
        "briosa.AnalysisOperations", "SetTransformForIthFrameInFrameSet",
        "/briosa.AnalysisOperations/SetTransformForIthFrameInFrameSet",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetTransformForIthFrameInFrameSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Frame Set", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FrameSet, "frame_set"), "SetCollectionObjectNameArg2"),
                new("Frame Set Index", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasFrameSetIndex ? request.FrameSetIndex : 0), "SetIntegerArg"),
                new("Transform in Working", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.TransformInWorking, "transform_in_working"), "SetTransformArg")
            ], []);
    }

    public static Api.SetTransformForIthFrameInFrameSetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
