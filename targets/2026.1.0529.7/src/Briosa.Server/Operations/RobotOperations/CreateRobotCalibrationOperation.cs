using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class CreateRobotCalibrationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.create_robot_calibration", "Create Robot Calibration",
        "briosa.RobotOperations", "CreateRobotCalibration", "/briosa.RobotOperations/CreateRobotCalibration",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateRobotCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                    "SetColMachineIdArg"),
                new("Calibration Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.CalibrationName), "SetStringArg")
            ], []);
    }

    public static Api.CreateRobotCalibrationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
