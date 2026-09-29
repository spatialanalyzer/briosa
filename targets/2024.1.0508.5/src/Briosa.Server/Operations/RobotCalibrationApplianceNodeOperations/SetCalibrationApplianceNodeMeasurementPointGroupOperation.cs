using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeMeasurementPointGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_point_group",
        "Set Calibration Appliance Node Measurement Point Group", "SetCalibrationApplianceNodeMeasurementPointGroup");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeMeasurementPointGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            CalibrationApplianceNodeOperation.CollectionObjectArgument("Point Group Name", request.PointGroupName,
                "point_group_name", WorkerObjectTypeValue.PointGroup));
    }

    public static Api.SetCalibrationApplianceNodeMeasurementPointGroupResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
