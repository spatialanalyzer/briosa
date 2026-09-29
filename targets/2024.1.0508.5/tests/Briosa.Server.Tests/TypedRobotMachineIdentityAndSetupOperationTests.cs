using Briosa.Server.Operations;
using Briosa.Server.Operations.RobotOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRobotMachineIdentityAndSetupOperationTests
{
    private static readonly string[] OperationIds =
    [
        "robot_operations.compute_robot_machine_adjusted_goal_frame",
        "robot_operations.get_robot_pose_for_a_frame"
    ];

    [Fact]
    public void IdentityAndSetupCommandsAreTypedAndPreserveFrameFallbacks()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var frame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Goal" };
        var compute = ComputeRobotMachineAdjustedGoalFrameOperation.CreateCommand(new()
        {
            OriginalGoalFrame = frame,
            LastAdjustedGoalFrame = frame,
            ActualMeasuredFrame = frame,
            ModifiedGoalFrame = frame
        });
        Assert.Equal(
            ["Original Goal Frame", "Last Adjusted Goal Frame", "Actual Measured Frame", "Modified Goal Frame"],
            compute.InputArguments.Select(argument => argument.Name));
        Assert.All(compute.InputArguments, argument =>
            Assert.Equal(WorkerObjectTypeValue.Frame, argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectType));
        Assert.Throws<ArgumentException>(() => ComputeRobotMachineAdjustedGoalFrameOperation.CreateCommand(new()));

        var pose = GetRobotPoseForAFrameOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robots", MachineId = 12 },
            GoalFrame = frame,
            GoalPoseCount = 6
        });
        Assert.Equal(new WorkerCollectionMachineIdValue("Robots", 12),
            pose.InputArguments[0].RequireValue<WorkerCollectionMachineIdValue>());
        Assert.Equal(WorkerObjectTypeValue.Frame,
            pose.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Empty(pose.InputArguments[2].RequireValue<WorkerDoubleArrayValue>().Values);
        Assert.Equal(6, Assert.Single(pose.OutputArguments).ArraySize);
        Assert.Throws<ArgumentException>(() => GetRobotPoseForAFrameOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedFrameAndPoseRoutes()
    {
        var worker = new RobotWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RobotOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RobotOperations.RobotOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var frame = new Api.CollectionObjectName { CollectionName = "Frames", ObjectName = "Goal" };

        var adjusted = await client.ComputeRobotMachineAdjustedGoalFrameAsync(new()
        {
            OriginalGoalFrame = frame,
            LastAdjustedGoalFrame = frame,
            ActualMeasuredFrame = frame,
            ModifiedGoalFrame = frame
        }, options);
        var pose = await client.GetRobotPoseForAFrameAsync(new()
        {
            MachineId = new() { CollectionName = "Robots", MachineId = 12 },
            GoalFrame = frame,
            ReferencePose = { Enumerable.Range(0, 6).Select(index => (double)index) },
            GoalPoseCount = 6
        }, options);

        Assert.Equal(Enumerable.Range(1, 16).Select(index => (double)index), adjusted.TransformValue.Values);
        Assert.Equal(Enumerable.Range(0, 6).Select(index => (double)index), pose.GoalPose);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
    }

    private sealed class RobotWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "robot_operations.compute_robot_machine_adjusted_goal_frame" =>
                    [new WorkerRetrievedOutput("Transform Value", WorkerMpValueKind.Transform,
                        new WorkerTransformValue(Enumerable.Range(1, 16).Select(index => (double)index).ToArray()))],
                "robot_operations.get_robot_pose_for_a_frame" =>
                    [new WorkerRetrievedOutput("Goal Pose", WorkerMpValueKind.DoubleArray,
                        new WorkerDoubleArrayValue(Enumerable.Range(0, 6).Select(index => (double)index).ToArray()))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
