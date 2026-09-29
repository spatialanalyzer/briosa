using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedPointRelationshipCreationTests
{
    [Fact]
    public void AveragePointRelationshipOmitsAbsentOptionalNames()
    {
        var command = MakeAveragePointRelationshipOperation.CreateCommand(AverageRequest());
        Assert.Equal("relationship_operations.make_average_point_relationship", command.OperationId);
        Assert.Equal(WorkerItemTypeValue.Relationship,
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal("SetPointNameRefListArg", command.InputArguments[1].SdkBinding);
        Assert.Equal(2, command.InputArguments.Count);
        Assert.Equal("P1", Assert.Single(command.InputArguments[1]
            .RequireValue<WorkerPointNameListValue>().Values).TargetName);

        var withOptionalNames = MakeAveragePointRelationshipOperation.CreateCommand(AverageRequest(includeNames: true));
        Assert.Equal(4, withOptionalNames.InputArguments.Count);
        Assert.Equal("Average Point Name (Optional)", withOptionalNames.InputArguments[2].Name);
        Assert.Equal("SetPointNameArg", withOptionalNames.InputArguments[2].SdkBinding);
        Assert.Equal("P-average", withOptionalNames.InputArguments[2]
            .RequireValue<WorkerPointNameValue>().TargetName);
        Assert.Equal("P-nominal", withOptionalNames.InputArguments[3]
            .RequireValue<WorkerPointNameValue>().TargetName);
        Assert.Throws<ArgumentException>(() => MakeAveragePointRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship()
        }));
    }

    [Fact]
    public void PointToPointRelationshipMapsBothToleranceVectors()
    {
        var command = MakePointToPointRelationshipOperation.CreateCommand(PointToPointRequest());
        Assert.Equal("relationship_operations.make_point_to_point_relationship", command.OperationId);
        Assert.Equal("P-first", command.InputArguments[1].RequireValue<WorkerPointNameValue>().TargetName);
        Assert.Equal("P-second", command.InputArguments[2].RequireValue<WorkerPointNameValue>().TargetName);
        Assert.Equal(new WorkerToleranceLimit(true, 2.5),
            command.InputArguments[3].RequireValue<WorkerToleranceVectorOptionsValue>().HighX);
        Assert.Equal(new WorkerToleranceLimit(true, 0),
            command.InputArguments[4].RequireValue<WorkerToleranceVectorOptionsValue>().LowZ);
        Assert.Throws<ArgumentException>(() => MakePointToPointRelationshipOperation.CreateCommand(new()
        {
            RelationshipName = Relationship(), FirstPointName = Point("P1"), SecondPointName = Point("P2")
        }));
    }

    [Fact]
    public async Task GeneratedClientReachesBothTypedCreationRoutes()
    {
        var worker = new PointRelationshipWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        await client.MakeAveragePointRelationshipAsync(AverageRequest(includeNames: true));
        await client.MakePointToPointRelationshipAsync(PointToPointRequest());

        Assert.Equal(
            [MakeAveragePointRelationshipOperation.Descriptor.OperationId,
             MakePointToPointRelationshipOperation.Descriptor.OperationId],
            worker.Commands.Select(command => command.OperationId));

    }

    private static Api.MakeAveragePointRelationshipRequest AverageRequest(bool includeNames = false)
    {
        var request = new Api.MakeAveragePointRelationshipRequest { RelationshipName = Relationship() };
        request.PointsInRelationship.Add(Point("P1"));
        if (includeNames)
        {
            request.AveragePointName = Point("P-average");
            request.NominalPointName = Point("P-nominal");
        }
        return request;
    }

    private static Api.MakePointToPointRelationshipRequest PointToPointRequest() => new()
    {
        RelationshipName = Relationship(), FirstPointName = Point("P-first"), SecondPointName = Point("P-second"),
        Tolerance = VectorTolerance(2.5), Constraint = VectorTolerance(0)
    };

    private static Api.ToleranceVectorOptions VectorTolerance(double highX) => new()
    {
        HighX = new Api.ToleranceLimit { Enabled = true, Value = highX },
        LowZ = new Api.ToleranceLimit { Enabled = true, Value = 0 }
    };

    private static Api.CollectionItemName Relationship() => new()
    {
        CollectionName = "Relationships", ItemName = "R1"
    };

    private static Api.PointName Point(string name) => new()
    {
        CollectionName = "Points", GroupName = "Group A", TargetName = name
    };

    private sealed class PointRelationshipWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
