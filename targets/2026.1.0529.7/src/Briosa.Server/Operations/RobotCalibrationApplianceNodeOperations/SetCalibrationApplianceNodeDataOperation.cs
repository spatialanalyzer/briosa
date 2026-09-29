using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeDataOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_data",
        "Set Calibration Appliance Node Data", "SetCalibrationApplianceNodeData");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Real Values", WorkerMpValueKind.DoubleArray,
                new WorkerDoubleArrayValue(request.RealValues), "SetDoubleArrayArg"));
    }

    public static Api.SetCalibrationApplianceNodeDataResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
