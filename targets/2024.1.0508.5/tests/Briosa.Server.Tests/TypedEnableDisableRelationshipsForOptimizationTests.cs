using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedEnableDisableRelationshipsForOptimizationTests
{
    [Fact]
    public void CreateCommandMapsRelationshipListAndFalseDefault()
    {
        var command = EnableDisableRelationshipsForOptimizationOperation.CreateCommand(new()
        {
            Relationships =
            {
                new Api.CollectionItemName { CollectionName = "first", ItemName = "rel-a" },
                new Api.CollectionItemName { CollectionName = "second", ItemName = "rel-b", ItemType = Api.ItemType.Relationship }
            }
        });

        Assert.Equal("Enable/Disable Relationships for Optimization", command.StepName);
        Assert.Equal(["SetCollectionObjectNameRefListArg", "SetBoolArg"],
            command.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(new WorkerCollectionItemNameValue("first", "rel-a", WorkerItemTypeValue.Any),
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values[0]);
        Assert.Equal(new WorkerCollectionItemNameValue("second", "rel-b", WorkerItemTypeValue.Relationship),
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values[1]);
        Assert.False(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Empty(command.OutputArguments);
        Assert.Throws<ArgumentException>(() =>
            EnableDisableRelationshipsForOptimizationOperation.CreateCommand(new()));
    }

    [Fact]
    public void CreateCommandPreservesEnabledValue()
    {
        var command = EnableDisableRelationshipsForOptimizationOperation.CreateCommand(new()
        {
            Relationships = { new Api.CollectionItemName { CollectionName = "collection", ItemName = "relationship" } },
            Enable = true
        });

        Assert.True(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedRelationshipListSetter()
    {
        var worker = new RelationshipOptimizationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.EnableDisableRelationshipsForOptimizationAsync(new()
        {
            Relationships = { new Api.CollectionItemName { CollectionName = "collection", ItemName = "relationship" } },
            Enable = true
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Empty(result.Execution.OutputRetrievals);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class RelationshipOptimizationWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(EnableDisableRelationshipsForOptimizationOperation.Descriptor.OperationId,
                command.OperationId);
            Assert.Single(command.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values);
            Assert.True(command.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
            Assert.Empty(command.OutputArguments);
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
