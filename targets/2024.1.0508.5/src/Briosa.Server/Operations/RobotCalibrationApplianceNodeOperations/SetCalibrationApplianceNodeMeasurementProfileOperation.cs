using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeMeasurementProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_profile",
        "Set Calibration Appliance Node Measurement Profile", "SetCalibrationApplianceNodeMeasurementProfile");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeMeasurementProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profile = request.HasMeasurementProfile ? request.MeasurementProfile : string.Empty;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Measurement Profile", WorkerMpValueKind.Text, new WorkerTextValue(profile), "SetStringArg"));
    }

    public static Api.SetCalibrationApplianceNodeMeasurementProfileResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
