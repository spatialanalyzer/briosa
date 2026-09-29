using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class DeleteCalibrationApplianceNodeOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.delete_calibration_appliance_node",
        "Delete Calibration Appliance Node", "DeleteCalibrationApplianceNode", ["destructive"]);

    public static WorkerMpCommand CreateCommand(Api.DeleteCalibrationApplianceNodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNodeToDelete,
                "Calibration Appliance Node to Delete", "calibration_appliance_node_to_delete"));
    }

    public static Api.DeleteCalibrationApplianceNodeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
