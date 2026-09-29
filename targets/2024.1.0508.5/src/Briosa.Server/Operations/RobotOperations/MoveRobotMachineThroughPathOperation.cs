using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class MoveRobotMachineThroughPathOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.move_robot_machine_through_path", "Move Robot/Machine through Path",
        "briosa.RobotOperations", "MoveRobotMachineThroughPath", "/briosa.RobotOperations/MoveRobotMachineThroughPath",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveRobotMachineThroughPathRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"),
                new("Path Frames", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.PathFrames, "path_frames"), "SetCollectionObjectNameRefListArg"),
                new("Use SA Kinematics", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasUseSaKinematics || request.UseSaKinematics), "SetBoolArg"),
                new("Linear Segments", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasLinearSegments && request.LinearSegments), "SetBoolArg"),
                new("Acknowledge Arrival", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasAcknowledgeArrival || request.AcknowledgeArrival), "SetBoolArg")
            ], []);
    }

    public static Api.MoveRobotMachineThroughPathResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}