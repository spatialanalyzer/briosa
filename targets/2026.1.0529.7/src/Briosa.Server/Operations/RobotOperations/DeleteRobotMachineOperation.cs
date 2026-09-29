using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class DeleteRobotMachineOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.delete_robot_machine", "Delete Robot/Machine",
        "briosa.RobotOperations", "DeleteRobotMachine", "/briosa.RobotOperations/DeleteRobotMachine",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteRobotMachineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                "SetColMachineIdArg")], []);
    }

    public static Api.DeleteRobotMachineResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
