using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class StartStopRobotCalibrationTrappingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.start_stop_robot_calibration_trapping", "Start/Stop Robot Calibration Trapping",
        "briosa.RobotOperations", "StartStopRobotCalibrationTrapping", "/briosa.RobotOperations/StartStopRobotCalibrationTrapping",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartStopRobotCalibrationTrappingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));
        if (request.InstrumentId is null || string.IsNullOrWhiteSpace(request.InstrumentId.CollectionName))
            throw new ArgumentException("Instrument ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"),
                new("Calibration Name", WorkerMpValueKind.Text, new WorkerTextValue(request.CalibrationName), "SetStringArg"),
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    new WorkerCollectionInstrumentIdValue(request.InstrumentId.CollectionName, request.InstrumentId.InstrumentId), "SetColInstIdArg"),
                new("Start Trapping (FALSE = Stop)", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasStartTrapping && request.StartTrapping), "SetBoolArg")
            ], []);
    }

    public static Api.StartStopRobotCalibrationTrappingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}