using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeIntegerValueOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_integer_value",
        "Set Calibration Appliance Node Integer Value", "SetCalibrationApplianceNodeIntegerValue");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeIntegerValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var index = request.HasIndexOffset ? request.IndexOffset : 0;
        var value = request.HasIntegerValue ? request.IntegerValue : 0;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Index Offset", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(index), "SetIntegerArg"),
            new WorkerMpInputArgument("Integer Value", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(value), "SetIntegerArg"));
    }

    public static Api.SetCalibrationApplianceNodeIntegerValueResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
