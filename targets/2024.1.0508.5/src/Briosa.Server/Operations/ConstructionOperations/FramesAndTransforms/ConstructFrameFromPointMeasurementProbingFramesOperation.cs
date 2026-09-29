using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameFromPointMeasurementProbingFramesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_from_point_measurement_probing_frames",
        "Construct Frame From Point Measurement Probing Frames", "briosa.ConstructionOperations",
        "ConstructFrameFromPointMeasurementProbingFrames",
        "/briosa.ConstructionOperations/ConstructFrameFromPointMeasurementProbingFrames", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameFromPointMeasurementProbingFramesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point List", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointList, "point_list"), "SetPointNameRefListArg"),
            new("Show Frame? (Hide = FALSE)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowFrame), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructFrameFromPointMeasurementProbingFramesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
