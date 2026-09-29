using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class StartRobotMachineInterfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.start_robot_machine_interface", "Start Robot/Machine Interface",
        "briosa.RobotOperations", "StartRobotMachineInterface", "/briosa.RobotOperations/StartRobotMachineInterface",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartRobotMachineInterfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionInstrumentId,
                    new WorkerCollectionInstrumentIdValue(request.MachineId.CollectionName, request.MachineId.InstrumentId),
                    "SetColInstIdArg"),
                new("Interface Type", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.InterfaceType), "SetIntegerArg"),
                new("Run in Simulation", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.RunInSimulation), "SetBoolArg")
            ], []);
    }

    public static Api.StartRobotMachineInterfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
