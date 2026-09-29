using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedMeshProjectionTests
{
    [Fact]
    public async Task GeneratedClientProjectsFramesAndPointsOnMeshAndAddsSurfaceOffsets()
    {
        var worker = new MeshProjectionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var addSurface = await client.AddSurfaceToMeshOffsetAlongReferenceDirectionAsync(new()
        {
            ReferenceFrameNames = { Object("Frames", "Reference") },
            SurfaceForOffsetDistanceComputation = Object("Geometry", "Surface"),
            ObjectProvidingDirectionReference = Object("Geometry", "Direction"),
            MeshServingAsProjectionTarget = Object("Scans", "Mesh")
        }, options);
        var frameDirection = await client.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionAsync(new()
        {
            ReferenceFrameNames = { Object("Frames", "Reference") },
            BaseNameForProjectedFrames = Object("Results", "FrameAlongFrame"),
            MeshServingAsProjectionTarget = Object("Scans", "Mesh")
        }, options);
        var referenceDirection = await client.ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionAsync(new()
        {
            ReferenceFrameNames = { Object("Frames", "Reference") },
            BaseNameForProjectedFrames = Object("Results", "FrameAlongVector"),
            ObjectProvidingDirectionReference = Object("Geometry", "Direction"),
            MeshServingAsProjectionTarget = Object("Scans", "Mesh")
        }, options);
        var projectedPoints = await client.ConstructPointsByProjectingPointsOnMeshAlongDirectionAsync(new()
        {
            ReferencePointNames = { Point("Control", "A"), Point("Control", "B") },
            GroupNameForProjectedPoints = Object("Results", "ProjectedPoints"),
            ObjectProvidingDirectionReference = Object("Geometry", "Direction"),
            MeshServingAsProjectionTarget = Object("Scans", "Mesh")
        }, options);

        Assert.All(new[] { addSurface.Execution, frameDirection.Execution, referenceDirection.Execution, projectedPoints.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(4, worker.Commands.Count);

        var addCommand = worker.Commands[0];
        Assert.Equal(7, addCommand.InputArguments.Count);
        Assert.Equal(10, addCommand.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(string.Empty, addCommand.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(addCommand, 4).ObjectType);
        Assert.True(addCommand.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.ScanStripeMesh, ObjectArgument(addCommand, 6).ObjectType);

        var frameDirectionCommand = worker.Commands[1];
        Assert.Equal(4, frameDirectionCommand.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.Frame, ObjectArgument(frameDirectionCommand, 1).ObjectType);
        Assert.True(frameDirectionCommand.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Single(frameDirection.ResultantFrameNameList);
        Assert.Equal(Api.ObjectType.Frame, Assert.Single(frameDirection.ResultantFrameNameList).ObjectType);

        var referenceDirectionCommand = worker.Commands[2];
        Assert.Equal(5, referenceDirectionCommand.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(referenceDirectionCommand, 2).ObjectType);
        Assert.True(referenceDirectionCommand.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Single(referenceDirection.ResultantFrameNameList);

        var pointsCommand = worker.Commands[3];
        Assert.Equal(5, pointsCommand.InputArguments.Count);
        Assert.Equal(2, pointsCommand.InputArguments[0].RequireValue<WorkerPointNameListValue>().Values.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup, ObjectArgument(pointsCommand, 1).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(pointsCommand, 2).ObjectType);
        Assert.True(pointsCommand.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Single(projectedPoints.ResultantPointNameList);
        Assert.Equal(Point("Projected", "Point", "Results"), Assert.Single(projectedPoints.ResultantPointNameList));

        var missingMesh = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionAsync(new()
            {
                ReferenceFrameNames = { Object("Frames", "Reference") },
                BaseNameForProjectedFrames = Object("Results", "Invalid")
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingMesh.StatusCode);
        Assert.Equal(4, worker.Commands.Count);
    }

    [Fact]
    public void MeshProjectionOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        foreach (var operation in new[]
        {
            AddSurfaceToMeshOffsetAlongReferenceDirectionOperation.Descriptor,
            ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionOperation.Descriptor,
            ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionOperation.Descriptor,
            ConstructPointsByProjectingPointsOnMeshAlongDirectionOperation.Descriptor
        })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private static Api.PointName Point(string group, string target, string collection = "Points") => new()
    {
        CollectionName = collection,
        GroupName = group,
        TargetName = target
    };

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class MeshProjectionWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.construct_frames_by_projecting_frames_on_mesh_along_frame_direction" or
                "construction_operations.construct_frames_by_projecting_frames_on_mesh_along_reference_direction" =>
                    [new WorkerRetrievedOutput("Resultant Frame Name List", WorkerMpValueKind.CollectionObjectNameList,
                        new WorkerCollectionObjectNameListValue([
                            new WorkerCollectionObjectNameValue("Results", "ProjectedFrame", WorkerObjectTypeValue.Frame)
                        ]))],
                "construction_operations.construct_points_by_projecting_points_on_mesh_along_direction" =>
                    [new WorkerRetrievedOutput("Resultant Point Name List", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([
                            new WorkerPointNameValue("Results", "Projected", "Point")
                        ]))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
