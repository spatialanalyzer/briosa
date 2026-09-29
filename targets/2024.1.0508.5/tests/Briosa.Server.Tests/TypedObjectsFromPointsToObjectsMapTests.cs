using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedObjectsFromPointsToObjectsMapTests
{
    [Fact]
    public void CreateCommandMapsDefaultsAndRequiredPoints()
    {
        var request = new Api.GetObjectsFromPointsToObjectsMapPointListRequest
        {
            Points = { new Api.PointName { CollectionName = "collection", GroupName = "group", TargetName = "point" } }
        };

        var command = GetObjectsFromPointsToObjectsMapPointListOperation.CreateCommand(request);

        Assert.Equal("Get Objects From Points to Objects Map (Point List)", command.StepName);
        Assert.Equal(["SetStringArg", "SetPointNameRefListArg"],
            command.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(new WorkerTextValue("Empty"), command.InputArguments[0].Value);
        Assert.Equal(new WorkerPointNameValue("collection", "group", "point"),
            command.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values[0]);
        Assert.Equal("GetCollectionObjectNameRefListArg", command.OutputArguments[0].SdkBinding);
        Assert.Throws<ArgumentException>(() =>
            GetObjectsFromPointsToObjectsMapPointListOperation.CreateCommand(new()));
    }

    [Fact]
    public void CreateCommandPreservesSuppliedMapName()
    {
        var command = GetObjectsFromPointsToObjectsMapPointListOperation.CreateCommand(new()
        {
            PointsToObjectsMapName = "map",
            Points = { new Api.PointName { TargetName = "point" } }
        });

        Assert.Equal(new WorkerTextValue("map"), command.InputArguments[0].Value);
    }

    [Fact]
    public async Task GeneratedClientReceivesTypedObjectList()
    {
        var worker = new PointObjectsMapWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.GetObjectsFromPointsToObjectsMapPointListAsync(new()
        {
            PointsToObjectsMapName = "map",
            Points = { new Api.PointName { TargetName = "point" } }
        }, deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        Assert.Single(result.Objects);
        Assert.Equal(new Api.CollectionObjectName
        {
            CollectionName = "geometry", ObjectName = "circle", ObjectType = Api.ObjectType.Any
        }, result.Objects[0]);
        Assert.Equal(1, worker.Calls);

    }

    private sealed class PointObjectsMapWorker : IWorkerCommandExecutor
    {
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Assert.Equal(GetObjectsFromPointsToObjectsMapPointListOperation.Descriptor.OperationId,
                command.OperationId);
            Assert.Equal(new WorkerTextValue("map"), command.InputArguments[0].Value);
            Assert.Single(command.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values);
            Assert.Equal(WorkerMpValueKind.CollectionObjectNameList, command.OutputArguments[0].Kind);
            var output = new WorkerRetrievedOutput("Objects", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue(
                    [new("geometry", "circle", WorkerObjectTypeValue.Any)]));
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpResultAvailable(2, 1, [output], null),
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
