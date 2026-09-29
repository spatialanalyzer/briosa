using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedCreatePointsToObjectsMapTests
{
    [Fact]
    public void CreateCommandMapsListsAndReviewedDefaults()
    {
        var command = CreatePointsToObjectsMapOperation.CreateCommand(Request());

        Assert.Equal("relationship_operations.create_points_to_objects_map", command.OperationId);
        Assert.Equal(new WorkerPointNameValue("Points", "Group A", "P1"),
            Assert.Single(command.InputArguments[0].RequireValue<WorkerPointNameListValue>().Values));
        Assert.Equal(new WorkerCollectionObjectNameValue("Parts", "Group A", WorkerObjectTypeValue.Any),
            Assert.Single(command.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values));
        Assert.Equal(new WorkerCollectionObjectNameValue("Parts", "Sphere 1", WorkerObjectTypeValue.Any),
            Assert.Single(command.InputArguments[2].RequireValue<WorkerCollectionObjectNameListValue>().Values));
        Assert.Equal(0.0, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("Empty", command.InputArguments[4].RequireValue<WorkerTextValue>().Value);

        var missingPoints = Request();
        missingPoints.Points = null;
        Assert.Throws<ArgumentException>(() => CreatePointsToObjectsMapOperation.CreateCommand(missingPoints));
        var emptyGroups = Request();
        emptyGroups.Groups = new();
        Assert.Throws<ArgumentException>(() => CreatePointsToObjectsMapOperation.CreateCommand(emptyGroups));
        var emptyObjects = Request();
        emptyObjects.Objects.Clear();
        Assert.Throws<ArgumentException>(() => CreatePointsToObjectsMapOperation.CreateCommand(emptyObjects));
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedPointsToObjectsMap()
    {
        var worker = new PointsToObjectsMapWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);
        var request = Request();
        request.ProximityTolerance = 0.25;
        request.PointsToObjectsMapName = "Map-1";

        var result = await client.CreatePointsToObjectsMapAsync(request,
            deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(2, result.Execution.MpResultCode);
        var command = Assert.Single(worker.Commands);
        Assert.Equal(CreatePointsToObjectsMapOperation.Descriptor.OperationId, command.OperationId);
        Assert.Equal("SetPointNameRefListArg", command.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameRefListArg", command.InputArguments[1].SdkBinding);
        Assert.Equal("SetCollectionObjectNameRefListArg", command.InputArguments[2].SdkBinding);
        Assert.Equal("SetDoubleArg", command.InputArguments[3].SdkBinding);
        Assert.Equal(0.25, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("Map-1", command.InputArguments[4].RequireValue<WorkerTextValue>().Value);

    }

    private static Api.CreatePointsToObjectsMapRequest Request()
    {
        var request = new Api.CreatePointsToObjectsMapRequest
        {
            Points = new(),
            Groups = new()
        };
        request.Points.Values.Add(new Api.PointName { CollectionName = "Points", GroupName = "Group A", TargetName = "P1" });
        request.Groups.Values.Add(new Api.CollectionObjectName { CollectionName = "Parts", ObjectName = "Group A" });
        request.Objects.Add(new Api.CollectionObjectName { CollectionName = "Parts", ObjectName = "Sphere 1" });
        return request;
    }

    private sealed class PointsToObjectsMapWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid();
            command = RoundTrip(WorkerControlMessage.Execute(requestId, command)).Command!;
            Commands.Add(command);
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(requestId, new(
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
