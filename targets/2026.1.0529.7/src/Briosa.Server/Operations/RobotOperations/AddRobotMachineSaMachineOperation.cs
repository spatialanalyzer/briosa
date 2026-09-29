using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class AddRobotMachineSaMachineOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.add_robot_machine_sa_machine", "Add Robot/Machine (.SAMachine)",
        "briosa.RobotOperations", "AddRobotMachineSaMachine", "/briosa.RobotOperations/AddRobotMachineSaMachine",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddRobotMachineSaMachineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        WorkerMpInputArgument[] inputs = request.SaMachineFile is null
            ? []
            : [new(".SAMachine File", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(request.SaMachineFile.Path, request.SaMachineFile.EmbeddedFile), "SetFilePathArg")];
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.AddRobotMachineSaMachineResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
