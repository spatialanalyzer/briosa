using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGeomRelationshipInputPointTests
{
    [Fact]
    public async Task GeneratedClientRoutesPointIgnoreAndReuseCommands()
    {
        var worker = new InputPointWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(10);

        var ignored = await client.GeomRelationshipIgnoreInputPointsAsync(new()
        {
            RelationshipName = new() { ObjectName = "relation" }
        }, deadline: deadline);
        var reused = await client.GeomRelationshipReuseIgnoredInputPointsAsync(new()
        {
            RelationshipName = new() { ObjectName = "relation" }
        }, deadline: deadline);

        Assert.Equal(Api.MpExecutionState.Succeeded, ignored.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, reused.Execution.State);
        Assert.Equal(2, worker.Commands.Count);
        Assert.Equal("Geom Relationship Ignore Input Points", worker.Commands[0].StepName);
        Assert.Equal("relationship_operations.geom_relationship_ignore_input_points", worker.Commands[0].OperationId);
        Assert.Equal("Geom Relationship Reuse Ignored Input Points", worker.Commands[1].StepName);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[0].InputArguments[0].SdkBinding);
        Assert.Empty(worker.Commands[0].OutputArguments);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GeomRelationshipIgnoreInputPointsAsync(new(), deadline: deadline));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(2, worker.Commands.Count);

    }

    private sealed class InputPointWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Commands.Add(command);
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpResultAvailable(2, 1, [], null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "test", DateTimeOffset.UnixEpoch), null)));
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, response.ExecutionResponse!.Execution, null, "completed", 1));
        }

        private static WorkerControlMessage RoundTrip(WorkerControlMessage message)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream);
            channel.Send(message);
            stream.Position = 0;
            return channel.Receive();
        }
    }
}
