using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetRobotCalibrationMeasurementOffsetInToolFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_robot_calibration_measurement_offset_in_tool_frame", "Set Robot Calibration Measurement Offset In Tool Frame",
        "briosa.RobotOperations", "SetRobotCalibrationMeasurementOffsetInToolFrame", "/briosa.RobotOperations/SetRobotCalibrationMeasurementOffsetInToolFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRobotCalibrationMeasurementOffsetInToolFrameRequest request)
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
                new("Measurement Frame (relative to tool)", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.MeasurementFrame, "measurement_frame"), "SetTransformArg")
            ], []);
    }

    public static Api.SetRobotCalibrationMeasurementOffsetInToolFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
