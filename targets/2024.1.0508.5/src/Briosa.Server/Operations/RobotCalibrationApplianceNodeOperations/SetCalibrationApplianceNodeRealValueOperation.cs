using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeRealValueOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_real_value",
        "Set Calibration Appliance Node Real Value", "SetCalibrationApplianceNodeRealValue");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeRealValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        var value = request.HasRealValue ? request.RealValue : 0d;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Index Offset", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(index), "SetIntegerArg"),
            new WorkerMpInputArgument("Real Value", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(value), "SetDoubleArg"));
    }

    public static Api.SetCalibrationApplianceNodeRealValueResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
