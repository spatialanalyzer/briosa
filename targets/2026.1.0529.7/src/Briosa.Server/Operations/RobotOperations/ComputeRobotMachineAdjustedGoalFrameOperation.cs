using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Operations.Values;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class ComputeRobotMachineAdjustedGoalFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.compute_robot_machine_adjusted_goal_frame", "Compute Robot/Machine Adjusted Goal Frame",
        "briosa.RobotOperations", "ComputeRobotMachineAdjustedGoalFrame",
        "/briosa.RobotOperations/ComputeRobotMachineAdjustedGoalFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("transform_value", "Transform Value", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.ComputeRobotMachineAdjustedGoalFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Original Goal Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.OriginalGoalFrame, "original_goal_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2"),
                new("Last Adjusted Goal Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.LastAdjustedGoalFrame, "last_adjusted_goal_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2"),
                new("Actual Measured Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ActualMeasuredFrame, "actual_measured_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2"),
                new("Modified Goal Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ModifiedGoalFrame, "modified_goal_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2")
            ],
            [new("Transform Value", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.ComputeRobotMachineAdjustedGoalFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TransformValue = TransformMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerTransformValue>()),
        Execution = completed.Details
    };
}
