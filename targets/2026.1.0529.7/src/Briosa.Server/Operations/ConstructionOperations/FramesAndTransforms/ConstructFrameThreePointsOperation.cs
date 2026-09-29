using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameThreePointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_three_points", "Construct Frame, 3 Points",
        "briosa.ConstructionOperations", "ConstructFrameThreePoints",
        "/briosa.ConstructionOperations/ConstructFrameThreePoints", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameThreePointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Construction Method", WorkerMpValueKind.Text,
                new WorkerTextValue(FrameConstructionChoiceMapper.Method(request.ConstructionMethod)), "SetStringArg"),
            new("Origin Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.OriginPoint, "origin_point"), "SetPointNameArg"),
            new("Primary Axis Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PrimaryAxisPoint, "primary_axis_point"), "SetPointNameArg"),
            new("Secondary Axis Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SecondaryAxisPoint, "secondary_axis_point"), "SetPointNameArg")
        };
        if (request.HasFrameName)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.FrameName,
                new WorkerTextValue(request.FrameName), "SetFrameNameArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameThreePointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
