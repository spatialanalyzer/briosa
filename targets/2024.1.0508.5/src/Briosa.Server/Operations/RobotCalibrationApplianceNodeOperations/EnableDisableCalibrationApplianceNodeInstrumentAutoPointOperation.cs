using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class EnableDisableCalibrationApplianceNodeInstrumentAutoPointOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point",
        "Enable/Disable Calibration Appliance Node Instrument Auto Point", "EnableDisableCalibrationApplianceNodeInstrumentAutoPoint");

    public static WorkerMpCommand CreateCommand(Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var enable = request.HasEnableInstrumentAutoPoint ? request.EnableInstrumentAutoPoint : true;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Enable Instrument Auto Point?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(enable), "SetBoolArg"));
    }

    public static Api.EnableDisableCalibrationApplianceNodeInstrumentAutoPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
