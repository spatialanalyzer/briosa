using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class UpdateCalibrationApplianceNodeDisplayRobotJointsOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.update_calibration_appliance_node_display_robot_joints",
        "Update Calibration Appliance Node Display Robot Joints", "UpdateCalibrationApplianceNodeDisplayRobotJoints");

    public static WorkerMpCommand CreateCommand(Api.UpdateCalibrationApplianceNodeDisplayRobotJointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var enable = request.HasEnableDisplayRobotJointUpdates ? request.EnableDisplayRobotJointUpdates : true;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Enable Display Robot Joint Updates?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(enable), "SetBoolArg"));
    }

    public static Api.UpdateCalibrationApplianceNodeDisplayRobotJointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
