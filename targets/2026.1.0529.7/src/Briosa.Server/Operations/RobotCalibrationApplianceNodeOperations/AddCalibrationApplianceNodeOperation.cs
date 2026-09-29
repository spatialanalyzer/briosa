using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class AddCalibrationApplianceNodeOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.add_calibration_appliance_node",
        "Add Calibration Appliance Node", "AddCalibrationApplianceNode");

    public static WorkerMpCommand CreateCommand(Api.AddCalibrationApplianceNodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNodeToAdd,
                "Calibration Appliance Node to Add", "calibration_appliance_node_to_add"));
    }

    public static Api.AddCalibrationApplianceNodeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
