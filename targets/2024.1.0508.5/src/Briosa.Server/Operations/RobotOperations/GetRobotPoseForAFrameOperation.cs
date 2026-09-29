using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Operations.Values;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RobotOperations;

internal static class GetRobotPoseForAFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "robot_operations.get_robot_pose_for_a_frame", "Get Robot Pose for a Frame",
        "briosa.RobotOperations", "GetRobotPoseForAFrame", "/briosa.RobotOperations/GetRobotPoseForAFrame",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("goal_pose", "Goal Pose", WorkerMpValueKind.DoubleArray)];

    public static WorkerMpCommand CreateCommand(Api.GetRobotPoseForAFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MachineId is null || string.IsNullOrWhiteSpace(request.MachineId.CollectionName))
            throw new ArgumentException("Machine ID is required.", nameof(request));

        var arraySize = request.HasGoalPoseCount ? request.GoalPoseCount : (int?)null;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Machine ID", WorkerMpValueKind.CollectionMachineId,
                    new WorkerCollectionMachineIdValue(request.MachineId.CollectionName, request.MachineId.MachineId),
                    "SetColMachineIdArg"),
                new("Goal Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.GoalFrame, "goal_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2"),
                new("Reference Pose", WorkerMpValueKind.DoubleArray,
                    new WorkerDoubleArrayValue(request.ReferencePose), "SetDoubleArrayArg")
            ],
            [new("Goal Pose", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", ArraySize: arraySize)]);
    }

    public static Api.GetRobotPoseForAFrameResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetRobotPoseForAFrameResult { Execution = completed.Details };
        result.GoalPose.AddRange(completed.Execution.OutputValues[0].RequireValue<WorkerDoubleArrayValue>().Values);
        return result;
    }
}
