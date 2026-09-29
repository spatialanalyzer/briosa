using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class AddRobotMachineManipKinOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.add_robot_machine_manip_kin", "Add Robot/Machine (.ManipKin)",
        "briosa.RobotOperations", "AddRobotMachineManipKin", "/briosa.RobotOperations/AddRobotMachineManipKin",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddRobotMachineManipKinRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // The reviewed MP binding omits the file argument when the optional field is absent.
        WorkerMpInputArgument[] inputs = request.ManipKinFile is null
            ? []
            : [new(".ManipKin File", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(request.ManipKinFile.Path, request.ManipKinFile.EmbeddedFile), "SetFilePathArg")];
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.AddRobotMachineManipKinResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
