using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeDisplayRobotOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_display_robot",
        "Set Calibration Appliance Node Display Robot", "SetCalibrationApplianceNodeDisplayRobot");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeDisplayRobotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null)
            throw RequestFieldViolation.Required("machine_id");
        if (string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw RequestFieldViolation.NotBlank("machine_id.collection_name");

        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Machine ID", WorkerMpValueKind.CollectionMachineId,
                new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"));
    }

    public static Api.SetCalibrationApplianceNodeDisplayRobotResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
