using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetTransformForIthFrameInFrameSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_transform_for_ith_frame_in_frame_set", "Get Transform for i-th Frame in Frame Set",
        "briosa.AnalysisOperations", "GetTransformForIthFrameInFrameSet",
        "/briosa.AnalysisOperations/GetTransformForIthFrameInFrameSet",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("transform_in_working", "Transform in Working", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.GetTransformForIthFrameInFrameSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Frame Set", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FrameSet, "frame_set"), "SetCollectionObjectNameArg2"),
                new("Frame Set Index", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasFrameSetIndex ? request.FrameSetIndex : 0), "SetIntegerArg")
            ],
            [new("Transform in Working", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.GetTransformForIthFrameInFrameSetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TransformInWorking = TransformMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerTransformValue>()),
        Execution = completed.Details
    };
}
