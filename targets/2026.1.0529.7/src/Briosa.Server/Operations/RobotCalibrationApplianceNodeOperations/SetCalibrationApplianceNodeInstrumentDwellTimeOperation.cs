using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeInstrumentDwellTimeOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument_dwell_time",
        "Set Calibration Appliance Node Instrument Dwell Time", "SetCalibrationApplianceNodeInstrumentDwellTime");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeInstrumentDwellTimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dwellTime = request.HasMeasurementDwellTime ? request.MeasurementDwellTime : 0d;
        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Measurement Dwell Time (Seconds)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(dwellTime), "SetDoubleArg"));
    }

    public static Api.SetCalibrationApplianceNodeInstrumentDwellTimeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
