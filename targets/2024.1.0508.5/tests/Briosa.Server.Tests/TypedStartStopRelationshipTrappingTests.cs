using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedStartStopRelationshipTrappingTests
{
    [Fact]
    public void CreateCommandUsesRequiredBindingsAndDefaultsToStop()
    {
        var command = StartStopRelationshipTrappingOperation.CreateCommand(Request());

        Assert.Equal("relationship_operations.start_stop_relationship_trapping", command.OperationId);
        Assert.Equal("Relationship Name", command.InputArguments[0].Name);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerCollectionItemNameValue("Relations", "R1", WorkerItemTypeValue.Relationship),
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>());
        Assert.Equal("Instrument ID", command.InputArguments[1].Name);
        Assert.Equal("SetColInstIdArg", command.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerCollectionInstrumentIdValue("Tracker", 12),
            command.InputArguments[1].RequireValue<WorkerCollectionInstrumentIdValue>());
        Assert.Equal("Start Trapping (FALSE = Stop)", command.InputArguments[2].Name);
        Assert.Equal("SetBoolArg", command.InputArguments[2].SdkBinding);
        Assert.False(command.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedStartTrappingRequest()
    {
        var worker = new TrappingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.StartStopRelationshipTrappingAsync(Request(start: true),
            deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        var sent = Assert.Single(worker.Command);
        Assert.Equal(StartStopRelationshipTrappingOperation.Descriptor.OperationId, sent.OperationId);
        Assert.True(sent.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

    }

    private static Api.StartStopRelationshipTrappingRequest Request(bool start = false) => new()
    {
        RelationshipName = new() { CollectionName = "Relations", ItemName = "R1" },
        InstrumentId = new() { CollectionName = "Tracker", InstrumentId = 12 },
        StartTrapping = start
    };

    private sealed class TrappingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Command { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid();
            command = RoundTrip(WorkerControlMessage.Execute(requestId, command)).Command!;
            Command.Add(command);
            var result = RoundTrip(WorkerControlMessage.ExecutionResult(requestId, new(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpResultAvailable(2, 1, [], null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "test", DateTimeOffset.UnixEpoch), null)));
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result.ExecutionResponse!.Execution, null, "completed", 1));
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
