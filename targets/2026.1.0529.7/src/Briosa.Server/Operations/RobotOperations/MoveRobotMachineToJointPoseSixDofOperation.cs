using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class MoveRobotMachineToJointPoseSixDofOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.move_robot_machine_to_joint_pose_six_dof", "Move Robot/Machine to Joint Pose (6DOF)",
        "briosa.RobotOperations", "MoveRobotMachineToJointPoseSixDof", "/briosa.RobotOperations/MoveRobotMachineToJointPoseSixDof",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveRobotMachineToJointPoseSixDofRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"),
                new("Joint 1", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Joint1), "SetDoubleArg"),
                new("Joint 2", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Joint2), "SetDoubleArg"),
                new("Joint 3", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Joint3), "SetDoubleArg"),
                new("Joint 4", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Joint4), "SetDoubleArg"),
                new("Joint 5", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Joint5), "SetDoubleArg"),
                new("Joint 6", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Joint6), "SetDoubleArg")
            ], []);
    }

    public static Api.MoveRobotMachineToJointPoseSixDofResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}