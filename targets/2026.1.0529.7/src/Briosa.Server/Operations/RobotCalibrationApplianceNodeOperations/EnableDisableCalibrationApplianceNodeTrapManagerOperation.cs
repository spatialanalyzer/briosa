using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class EnableDisableCalibrationApplianceNodeTrapManagerOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager",
        "Enable/Disable Calibration Appliance Node Trap Manager", "EnableDisableCalibrationApplianceNodeTrapManager");

    public static WorkerMpCommand CreateCommand(Api.EnableDisableCalibrationApplianceNodeTrapManagerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var enable = request.HasEnable ? request.Enable : true;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Enable(TRUE), Disable(FALSE)?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(enable), "SetBoolArg"));
    }

    public static Api.EnableDisableCalibrationApplianceNodeTrapManagerResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
