using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class SetRobotMachineParameterOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.set_robot_machine_parameter", "Set Robot/Machine Parameter",
        "briosa.RobotOperations", "SetRobotMachineParameter", "/briosa.RobotOperations/SetRobotMachineParameter",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRobotMachineParameterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                    "SetColMachineIdArg"),
                new("Parameter Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.ParameterName), "SetStringArg"),
                new("Parameter Value", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.ParameterValue), "SetDoubleArg")
            ], []);
    }

    public static Api.SetRobotMachineParameterResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
