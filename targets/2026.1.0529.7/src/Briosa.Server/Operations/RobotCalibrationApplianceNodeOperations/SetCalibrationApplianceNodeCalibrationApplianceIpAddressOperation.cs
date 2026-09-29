using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeCalibrationApplianceIpAddressOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address",
        "Set Calibration Appliance Node Calibration Appliance IP Address",
        "SetCalibrationApplianceNodeCalibrationApplianceIpAddress");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var address = request.HasCalibrationApplianceIpAddress ? request.CalibrationApplianceIpAddress : "0.0.0.0";
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Calibration Appliance IP Address", WorkerMpValueKind.Text, new WorkerTextValue(address), "SetStringArg"));
    }

    public static Api.SetCalibrationApplianceNodeCalibrationApplianceIpAddressResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
