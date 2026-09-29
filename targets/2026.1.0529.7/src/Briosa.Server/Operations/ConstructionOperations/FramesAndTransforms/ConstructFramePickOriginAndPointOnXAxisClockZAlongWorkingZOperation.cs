using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_pick_origin_and_point_on_x_axis_clock_z_along_working_z",
        "Construct Frame, Pick origin and point on X axis - clock Z along working Z", "briosa.ConstructionOperations",
        "ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZ",
        "/briosa.ConstructionOperations/ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZ", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Origin Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.OriginPoint, "origin_point"), "SetPointNameArg"),
            new("Point on X-Axis", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointOnXAxis, "point_on_x_axis"), "SetPointNameArg")
        };
        if (request.HasFrameName)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.FrameName,
                new WorkerTextValue(request.FrameName), "SetFrameNameArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
