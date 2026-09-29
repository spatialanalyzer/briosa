using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeMeasurementFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_frame",
        "Set Calibration Appliance Node Measurement Frame", "SetCalibrationApplianceNodeMeasurementFrame");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeMeasurementFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            CalibrationApplianceNodeOperation.CollectionObjectArgument("Measurement Reference Frame",
                request.MeasurementReferenceFrame, "measurement_reference_frame", WorkerObjectTypeValue.Frame));
    }

    public static Api.SetCalibrationApplianceNodeMeasurementFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
