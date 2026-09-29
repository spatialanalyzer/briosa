using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetTimestampForIthFrameInFrameSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_timestamp_for_ith_frame_in_frame_set", "Get Timestamp for i-th Frame in Frame Set",
        "briosa.AnalysisOperations", "GetTimestampForIthFrameInFrameSet",
        "/briosa.AnalysisOperations/GetTimestampForIthFrameInFrameSet",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("timestamp", "Timestamp", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetTimestampForIthFrameInFrameSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Frame Set", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FrameSet, "frame_set"), "SetCollectionObjectNameArg2"),
                new("Frame Set Index", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasFrameSetIndex ? request.FrameSetIndex : 0), "SetIntegerArg")
            ],
            [new("Timestamp", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetTimestampForIthFrameInFrameSetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Timestamp = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
