using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SkipCalibrationApplianceNodeMeasurementOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement",
        "Skip Calibration Appliance Node Measurement", "SkipCalibrationApplianceNodeMeasurement");

    public static WorkerMpCommand CreateCommand(Api.SkipCalibrationApplianceNodeMeasurementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode));
    }

    public static Api.SkipCalibrationApplianceNodeMeasurementResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
