using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameAtPointWithWorkingZAndClockedAxisOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_at_point_with_working_z_and_clocked_axis",
        "Construct Frame, at Point, with working Z, and clocked axis", "briosa.ConstructionOperations",
        "ConstructFrameAtPointWithWorkingZAndClockedAxis",
        "/briosa.ConstructionOperations/ConstructFrameAtPointWithWorkingZAndClockedAxis", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameAtPointWithWorkingZAndClockedAxisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Origin Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.OriginPoint, "origin_point"), "SetPointNameArg"),
            new("Clocked axis", WorkerMpValueKind.AxisIdentifier,
                FrameConstructionChoiceMapper.Axis(request.ClockedAxis), "SetAxisNameArg"),
            new("Clocking Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ClockingPoint, "clocking_point"), "SetPointNameArg")
        };
        if (request.HasFrameName)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.FrameName,
                new WorkerTextValue(request.FrameName), "SetFrameNameArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameAtPointWithWorkingZAndClockedAxisResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
