using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeMeasurementTargetOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_target",
        "Set Calibration Appliance Node Measurement Target", "SetCalibrationApplianceNodeMeasurementTarget");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeMeasurementTargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = request.HasMeasurementTarget ? request.MeasurementTarget : string.Empty;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Measurement Target", WorkerMpValueKind.Text, new WorkerTextValue(target), "SetStringArg"));
    }

    public static Api.SetCalibrationApplianceNodeMeasurementTargetResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
