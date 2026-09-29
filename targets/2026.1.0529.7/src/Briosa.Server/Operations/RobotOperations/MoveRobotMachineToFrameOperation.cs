using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class MoveRobotMachineToFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.move_robot_machine_to_frame", "Move Robot/Machine to Frame",
        "briosa.RobotOperations", "MoveRobotMachineToFrame", "/briosa.RobotOperations/MoveRobotMachineToFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("actual_transform_in_working", "Actual Transform In Working (result)", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.MoveRobotMachineToFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId), "SetColMachineIdArg"),
                new("Destination Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.DestinationFrame, "destination_frame", WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2"),
                new("Use SA Kinematics", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseSaKinematics && request.UseSaKinematics), "SetBoolArg"),
                new("Acknowledge Arrival", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasAcknowledgeArrival && request.AcknowledgeArrival), "SetBoolArg")
            ],
            [new("Actual Transform In Working (result)", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.MoveRobotMachineToFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ActualTransformInWorking = TransformMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerTransformValue>()),
        Execution = completed.Details
    };
}