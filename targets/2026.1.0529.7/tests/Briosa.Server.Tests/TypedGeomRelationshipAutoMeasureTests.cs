using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGeomRelationshipAutoMeasureTests
{
    [Fact]
    public void CreateCommandPreservesDefaultsAndSuppliedValues()
    {
        var defaultRequest = new Api.SetGeomRelationshipAutoMeasureNominalFeatureRequest
        {
            RelationshipName = new() { CollectionName = "collection", ObjectName = "relationship" },
            InstrumentId = new() { CollectionName = "instruments", InstrumentId = 7 }
        };
        var defaultCommand = SetGeomRelationshipAutoMeasureNominalFeatureOperation.CreateCommand(defaultRequest);

        Assert.Equal("Set Geom Relationship Auto Measure Nominal Feature", defaultCommand.StepName);
        Assert.Equal(
            ["SetCollectionObjectNameArg2", "SetBoolArg", "SetColInstIdArg", "SetStringArg"],
            defaultCommand.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(new WorkerCollectionObjectNameValue("collection", "relationship", WorkerObjectTypeValue.Any),
            defaultCommand.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>());
        Assert.True(defaultCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerCollectionInstrumentIdValue("instruments", 7),
            defaultCommand.InputArguments[2].RequireValue<WorkerCollectionInstrumentIdValue>());
        Assert.Equal(new WorkerTextValue("Empty"), defaultCommand.InputArguments[3].Value);

        var suppliedRequest = new Api.SetGeomRelationshipAutoMeasureNominalFeatureRequest
        {
            RelationshipName = new() { CollectionName = "collection", ObjectName = "relationship" },
            InstrumentId = new() { CollectionName = "instruments", InstrumentId = 7 },
            TrapClouds = false,
            MeasurementMode = "Surface Scan"
        };
        var suppliedCommand = SetGeomRelationshipAutoMeasureNominalFeatureOperation.CreateCommand(suppliedRequest);
        Assert.False(suppliedCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerTextValue("Surface Scan"), suppliedCommand.InputArguments[3].Value);
    }

    [Fact]
    public async Task GeneratedClientExecutesNominalAutoMeasureSetting()
    {
        var worker = new AutoMeasureWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.SetGeomRelationshipAutoMeasureNominalFeatureAsync(new()
        {
            RelationshipName = new() { CollectionName = "collection", ObjectName = "relationship" },
            InstrumentId = new() { CollectionName = "instruments", InstrumentId = 7 },
            TrapClouds = false,
            MeasurementMode = "Surface Scan"
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Empty(result.Execution.OutputRetrievals);
        Assert.Equal(1, worker.Calls);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.SetGeomRelationshipAutoMeasureNominalFeatureAsync(new()
            {
                RelationshipName = new() { CollectionName = "collection", ObjectName = "relationship" }
            }, deadline: DateTime.UtcNow.AddSeconds(10)));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class AutoMeasureWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(SetGeomRelationshipAutoMeasureNominalFeatureOperation.Descriptor.OperationId, command.OperationId);
            Assert.Equal(new WorkerCollectionInstrumentIdValue("instruments", 7),
                command.InputArguments[2].RequireValue<WorkerCollectionInstrumentIdValue>());
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
