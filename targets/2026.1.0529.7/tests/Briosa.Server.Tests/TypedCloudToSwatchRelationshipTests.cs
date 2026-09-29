using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedCloudToSwatchRelationshipTests
{
    [Fact]
    public void Preserves2026OnlyDefaultsAndTypeDomains()
    {
        var request = new Api.MakeCloudToSwatchRelationshipRequest
        {
            RelationshipName = new() { CollectionName = "Relationships", ItemName = "R1" },
            InputCloudName = new() { CollectionName = "Clouds", ObjectName = "C1" },
            ReferencePoint = new() { CollectionName = "Points", GroupName = "G", TargetName = "P1" },
            CardinalPointGroupName = new() { CollectionName = "Groups", ObjectName = "Cardinals" }
        };
        var command = MakeCloudToSwatchRelationshipOperation.CreateCommand(request);
        Assert.Equal("relationship_operations.make_cloud_to_swatch_relationship", command.OperationId);
        Assert.Equal(["SetCollectionObjectNameArg2", "SetCollectionObjectNameArg2", "SetStringArg", "SetPointNameArg",
            "SetDoubleArg", "SetDoubleArg", "SetDoubleArg", "SetCollectionObjectNameArg2"],
            command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            command.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("Empty", command.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal([0.125, -0.125, 0.125], command.InputArguments.Skip(4).Take(3)
            .Select(argument => argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            command.InputArguments[7].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        request.SurfaceFaceList = "Face 1";
        request.MinimumAxialOffset = 0;
        command = MakeCloudToSwatchRelationshipOperation.CreateCommand(request);
        Assert.Equal("Face 1", command.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0d, command.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
        request.ReferencePoint = null;
        Assert.Throws<ArgumentException>(() => MakeCloudToSwatchRelationshipOperation.CreateCommand(request));
    }

    [Fact]
    public async Task GeneratedClientUses2026OnlyTypedRoute()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(host.Channel);
        var request = new Api.MakeCloudToSwatchRelationshipRequest
        {
            RelationshipName = new() { ItemName = "R1" },
            InputCloudName = new() { ObjectName = "C1" },
            ReferencePoint = new() { TargetName = "P1" },
            CardinalPointGroupName = new() { ObjectName = "G1" }
        };
        var result = await client.MakeCloudToSwatchRelationshipAsync(request);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal("relationship_operations.make_cloud_to_swatch_relationship", Assert.Single(worker.Commands).OperationId);
        request.InputCloudName = null;
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakeCloudToSwatchRelationshipAsync(request));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Single(worker.Commands);
    }

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
