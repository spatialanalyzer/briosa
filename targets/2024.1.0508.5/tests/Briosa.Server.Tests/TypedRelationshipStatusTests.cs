using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipStatusTests
{
    [Fact]
    public void CreateCommandUsesReviewedRelationshipInputAndBooleanOutputs()
    {
        var request = new Api.GetRelationshipStatusRequest
        {
            RelationshipName = new() { CollectionName = "collection", ItemName = "relationship" }
        };

        var command = GetRelationshipStatusOperation.CreateCommand(request);

        Assert.Equal("Get Relationship Status", command.StepName);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerCollectionItemNameValue("collection", "relationship", WorkerItemTypeValue.Relationship),
            command.InputArguments[0].Value);
        Assert.Equal(
            ["Dormant", "Success", "Measured", "Failed", "Unmeasured"],
            command.OutputArguments.Select(output => output.Name));
        Assert.All(command.OutputArguments, output => Assert.Equal("GetBoolArg", output.SdkBinding));
        Assert.Throws<ArgumentException>(() => GetRelationshipStatusOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientReceivesAllRelationshipStatusFlags()
    {
        var worker = new RelationshipStatusWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.GetRelationshipStatusAsync(new()
        {
            RelationshipName = new() { CollectionName = "collection", ItemName = "relationship" }
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.True(result.Status.Dormant);
        Assert.False(result.Status.Success);
        Assert.True(result.Status.Measured);
        Assert.False(result.Status.Failed);
        Assert.True(result.Status.Unmeasured);
        Assert.Equal(5, result.Execution.OutputRetrievals.Count);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetRelationshipStatusAsync(new(), deadline: DateTime.UtcNow.AddSeconds(10)));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class RelationshipStatusWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(GetRelationshipStatusOperation.Descriptor.OperationId, command.OperationId);
            Assert.Equal(new WorkerCollectionItemNameValue("collection", "relationship", WorkerItemTypeValue.Relationship),
                command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>());
            Assert.Equal(5, command.OutputArguments.Count);
            var outputs = new WorkerMpOutputValue[]
            {
                new WorkerRetrievedOutput("Dormant", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                new WorkerRetrievedOutput("Success", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                new WorkerRetrievedOutput("Measured", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                new WorkerRetrievedOutput("Failed", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                new WorkerRetrievedOutput("Unmeasured", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))
            };
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpResultAvailable(2, 1, outputs, null),
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
