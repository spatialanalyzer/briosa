using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class SetCalibrationApplianceNodeInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.MutatingDescriptor(
        "robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument",
        "Set Calibration Appliance Node Instrument", "SetCalibrationApplianceNodeInstrument");

    public static WorkerMpCommand CreateCommand(Api.SetCalibrationApplianceNodeInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Instrument is null || string.IsNullOrWhiteSpace(request.Instrument.CollectionName))
            throw new ArgumentException("Instrument ID is required.", nameof(request));

        return CalibrationApplianceNodeOperation.MutatingCommand(Descriptor,
            CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode),
            new WorkerMpInputArgument("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                new WorkerCollectionInstrumentIdValue(request.Instrument.CollectionName, request.Instrument.InstrumentId), "SetColInstIdArg"));
    }

    public static Api.SetCalibrationApplianceNodeInstrumentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = CalibrationApplianceNodeOperation.ExecutionDetails(completed) };
}
