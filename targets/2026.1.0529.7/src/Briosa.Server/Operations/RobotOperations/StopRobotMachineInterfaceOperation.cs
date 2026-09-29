using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class StopRobotMachineInterfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.stop_robot_machine_interface", "Stop Robot/Machine Interface",
        "briosa.RobotOperations", "StopRobotMachineInterface", "/briosa.RobotOperations/StopRobotMachineInterface",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StopRobotMachineInterfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Machine ID", WorkerMpValueKind.CollectionInstrumentId,
                new WorkerCollectionInstrumentIdValue(request.MachineId.CollectionName, request.MachineId.InstrumentId),
                "SetColInstIdArg")], []);
    }

    public static Api.StopRobotMachineInterfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
