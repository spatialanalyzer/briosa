using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class ConnectDisconnectCalibrationApplianceNodeOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node",
        "Connect/Disconnect Calibration Appliance Node", "ConnectDisconnectCalibrationApplianceNode");

    public static WorkerMpCommand CreateCommand(Api.ConnectDisconnectCalibrationApplianceNodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var connect = request.HasConnect ? request.Connect : true;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Connect(TRUE) or Disconnect(FALSE)?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(connect), "SetBoolArg"));
    }

    public static Api.ConnectDisconnectCalibrationApplianceNodeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
