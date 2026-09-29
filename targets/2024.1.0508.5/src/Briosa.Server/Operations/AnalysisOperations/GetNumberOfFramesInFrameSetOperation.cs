using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetNumberOfFramesInFrameSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_number_of_frames_in_frame_set", "Get Number of Frames In Frame Set",
        "briosa.AnalysisOperations", "GetNumberOfFramesInFrameSet",
        "/briosa.AnalysisOperations/GetNumberOfFramesInFrameSet",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_count", "Total Count", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetNumberOfFramesInFrameSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Frame Set Container", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FrameSetContainer, "frame_set_container"), "SetCollectionObjectNameArg2")],
            [new("Total Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetNumberOfFramesInFrameSetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TotalCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
