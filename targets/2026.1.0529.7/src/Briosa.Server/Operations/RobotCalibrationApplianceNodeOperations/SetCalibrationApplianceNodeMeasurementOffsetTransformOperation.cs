using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Operations.Values;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeMeasurementOffsetTransformOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_offset_transform",
        "Set Calibration Appliance Node Measurement Offset Transform", "SetCalibrationApplianceNodeMeasurementOffsetTransform");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeMeasurementOffsetTransformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Measurement Offset Transform", WorkerMpValueKind.Transform,
                TransformMapper.Required(request.MeasurementOffsetTransform, "measurement_offset_transform"), "SetTransformArg"));
    }

    public static Api.SetCalibrationApplianceNodeMeasurementOffsetTransformResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
