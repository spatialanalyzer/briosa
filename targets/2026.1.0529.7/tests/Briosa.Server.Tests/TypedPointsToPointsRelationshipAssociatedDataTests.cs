using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPointsToPointsRelationshipAssociatedDataTests
{
    [Fact]
    public void CreateCommandsUseRelationshipAndPointListBindings()
    {
        var get = GetPointsToPointsRelationshipAssociatedDataOperation.CreateCommand(new()
        {
            RelationshipName = RelationshipName()
        });
        Assert.Equal("SetCollectionObjectNameArg2", get.InputArguments[0].SdkBinding);
        Assert.Equal(WorkerItemTypeValue.Relationship,
            get.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Collection(get.OutputArguments,
            output => Assert.Equal(("Nominal Points", "GetPointNameRefListArg"), (output.Name, output.SdkBinding)),
            output => Assert.Equal(("Actual Points", "GetPointNameRefListArg"), (output.Name, output.SdkBinding)));

        var set = SetPointsToPointsRelationshipAssociatedDataOperation.CreateCommand(SetRequest());
        Assert.Equal(new WorkerPointNameValue("Points", "Group A", "P1"),
            Assert.Single(set.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values));
        Assert.Equal(new WorkerPointNameValue("Points", "Group B", "P2"),
            Assert.Single(set.InputArguments[2].RequireValue<WorkerPointNameListValue>().Values));
        Assert.True(set.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);

        var invalid = SetRequest();
        invalid.ActualPoints = new();
        Assert.Throws<ArgumentException>(() => SetPointsToPointsRelationshipAssociatedDataOperation.CreateCommand(invalid));
    }

    [Fact]
    public async Task GeneratedClientGetsAndSetsTypedAssociatedData()
    {
        var worker = new AssociatedDataWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.GetPointsToPointsRelationshipAssociatedDataAsync(new()
            { RelationshipName = RelationshipName() }, deadline: DateTime.UtcNow.AddSeconds(10));
        await client.SetPointsToPointsRelationshipAssociatedDataAsync(SetRequest(ignoreEmpty: false),
            deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal("P-nominal", Assert.Single(result.AssociatedData.NominalPoints).TargetName);
        Assert.Equal("P-actual", Assert.Single(result.AssociatedData.ActualPoints).TargetName);
        Assert.Equal(2, worker.Commands.Count);
        Assert.Equal("relationship_operations.get_points_to_points_relationship_associated_data", worker.Commands[0].OperationId);
        Assert.Equal("relationship_operations.set_points_to_points_relationship_associated_data", worker.Commands[1].OperationId);
        Assert.False(worker.Commands[1].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
    }

    private static Api.CollectionItemName RelationshipName() => new()
    {
        CollectionName = "Relations", ItemName = "R1"
    };

    private static Api.SetPointsToPointsRelationshipAssociatedDataRequest SetRequest(bool ignoreEmpty = true)
    {
        var request = new Api.SetPointsToPointsRelationshipAssociatedDataRequest
        {
            RelationshipName = RelationshipName(), NominalPoints = new(), ActualPoints = new(),
            IgnoreEmptyArguments = ignoreEmpty
        };
        request.NominalPoints.Values.Add(new Api.PointName { CollectionName = "Points", GroupName = "Group A", TargetName = "P1" });
        request.ActualPoints.Values.Add(new Api.PointName { CollectionName = "Points", GroupName = "Group B", TargetName = "P2" });
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
            IReadOnlyList<WorkerMpOutputValue> outputs = command.OperationId ==
                GetPointsToPointsRelationshipAssociatedDataOperation.Descriptor.OperationId
                ? [
                    new WorkerRetrievedOutput("Nominal Points", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([new("Points", "Group A", "P-nominal")])),
                    new WorkerRetrievedOutput("Actual Points", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([new("Points", "Group B", "P-actual")]))
                ] : [];
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(requestId, new(
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
