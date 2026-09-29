using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class TransformPointsByDeltaAboutWorkingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.transform_points_by_delta_about_working_frame", "Transform Points by Delta (About Working Frame)",
        "briosa.ConstructionOperations", "TransformPointsByDeltaAboutWorkingFrame", "/briosa.ConstructionOperations/TransformPointsByDeltaAboutWorkingFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TransformPointsByDeltaAboutWorkingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name List", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNameList, "point_name_list"), "SetPointNameRefListArg"),
            new("Delta In Working Coordinates", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.DeltaInWorkingCoordinates, "delta_in_working_coordinates"), "SetVectorArg")
        ], []);
    }

    public static Api.TransformPointsByDeltaAboutWorkingFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
