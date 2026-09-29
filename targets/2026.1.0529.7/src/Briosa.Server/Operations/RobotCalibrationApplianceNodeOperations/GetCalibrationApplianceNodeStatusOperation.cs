using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotCalibrationApplianceNodeOperations;

internal static class GetCalibrationApplianceNodeStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = CalibrationApplianceNodeOperation.ReadOnlyDescriptor(
        "robot_calibration_appliance_node_operations.get_calibration_appliance_node_status",
        "Get Calibration Appliance Node Status", "GetCalibrationApplianceNodeStatus");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("instrument_connected", "Instrument Connected?", WorkerMpValueKind.Logical),
        new("calibration_appliance_connected", "Calibration Appliance Connected?", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCalibrationApplianceNodeStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CalibrationApplianceNodeOperation.Command(Descriptor,
            [CalibrationApplianceNodeOperation.NodeArgument(request.CalibrationApplianceNode)],
            [new("Instrument Connected?", WorkerMpValueKind.Logical, "GetBoolArg"),
             new("Calibration Appliance Connected?", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.GetCalibrationApplianceNodeStatusResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        InstrumentConnected = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        CalibrationApplianceConnected = completed.Execution.OutputValues[1].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}
