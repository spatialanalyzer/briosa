using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSetRelationshipAssociatedDataTests
{
    [Fact]
    public void CreateCommandUsesTypedRelationshipAndRequiredAssociatedData()
    {
        var command = SetRelationshipAssociatedDataOperation.CreateCommand(Request(ignoreEmpty: false));

        Assert.Equal("relationship_operations.set_relationship_associated_data", command.OperationId);
        Assert.Equal(WorkerItemTypeValue.Relationship,
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Equal(new WorkerPointNameValue("Points", "Group A", "P1"),
            Assert.Single(command.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values));
        Assert.Equal(new WorkerCollectionObjectNameValue("Groups", "G1", WorkerObjectTypeValue.PointGroup),
            Assert.Single(command.InputArguments[2].RequireValue<WorkerCollectionObjectNameListValue>().Values));
        Assert.Equal(new WorkerCollectionObjectNameValue("Clouds", "C1", WorkerObjectTypeValue.Cloud),
            Assert.Single(command.InputArguments[3].RequireValue<WorkerCollectionObjectNameListValue>().Values));
        Assert.Equal(new WorkerCollectionObjectNameValue("Objects", "O1", WorkerObjectTypeValue.Sphere),
            Assert.Single(command.InputArguments[4].RequireValue<WorkerCollectionObjectNameListValue>().Values));
        Assert.False(command.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);

        var missingLists = Request();
        missingLists.PointGroups = null;
        Assert.Throws<ArgumentException>(() => SetRelationshipAssociatedDataOperation.CreateCommand(missingLists));
    }

    [Fact]
    public async Task GeneratedClientExecutesTypedSetterWithDefaultIgnoreEmptyValue()
    {
        var worker = new AssociatedDataWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        await client.SetRelationshipAssociatedDataAsync(Request(), deadline: DateTime.UtcNow.AddSeconds(10));

        var sent = Assert.Single(worker.Commands);
        Assert.Equal(SetRelationshipAssociatedDataOperation.Descriptor.OperationId, sent.OperationId);
        Assert.True(sent.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
    }

    private static Api.SetRelationshipAssociatedDataRequest Request(bool ignoreEmpty = true)
    {
        var request = new Api.SetRelationshipAssociatedDataRequest
        {
            RelationshipName = new() { CollectionName = "Relationships", ItemName = "R1" },
            IndividualPoints = new(), PointGroups = new(), PointClouds = new(), Objects = new(),
            IgnoreEmptyArguments = ignoreEmpty
        };
        request.IndividualPoints.Values.Add(new Api.PointName { CollectionName = "Points", GroupName = "Group A", TargetName = "P1" });
        request.PointGroups.Values.Add(new Api.CollectionObjectName { CollectionName = "Groups", ObjectName = "G1", ObjectType = Api.ObjectType.PointGroup });
        request.PointClouds.Values.Add(new Api.CollectionObjectName { CollectionName = "Clouds", ObjectName = "C1", ObjectType = Api.ObjectType.Cloud });
        request.Objects.Values.Add(new Api.CollectionObjectName { CollectionName = "Objects", ObjectName = "O1", ObjectType = Api.ObjectType.Sphere });
        return request;
    }

    private sealed class AssociatedDataWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid();
            command = RoundTrip(WorkerControlMessage.Execute(requestId, command)).Command!;
            Commands.Add(command);
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(requestId, new(
                WorkerExecutionResponseStatus.Completed, new WorkerMpResultAvailable(2, 1, [], null),
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
