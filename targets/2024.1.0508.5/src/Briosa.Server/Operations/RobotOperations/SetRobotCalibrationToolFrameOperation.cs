using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetRobotCalibrationToolFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_robot_calibration_tool_frame", "Set Robot Calibration Tool Frame",
        "briosa.RobotOperations", "SetRobotCalibrationToolFrame", "/briosa.RobotOperations/SetRobotCalibrationToolFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRobotCalibrationToolFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                    "SetColMachineIdArg"),
                new("Calibration Name", WorkerMpValueKind.Text, new WorkerTextValue(request.CalibrationName), "SetStringArg"),
                new("Tool Frame (relative to flange)", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.ToolFrame, "tool_frame"), "SetTransformArg")
            ], []);
    }

    public static Api.SetRobotCalibrationToolFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
