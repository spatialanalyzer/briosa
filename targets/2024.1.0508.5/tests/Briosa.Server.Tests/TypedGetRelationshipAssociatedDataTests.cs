using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGetRelationshipAssociatedDataTests
{
    [Fact]
    public void CreateCommandUsesRelationshipItemAndExactOutputBindings()
    {
        var command = GetRelationshipAssociatedDataOperation.CreateCommand(Request());

        Assert.Equal(WorkerItemTypeValue.Relationship,
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal("SetCollectionObjectNameArg2", command.InputArguments[0].SdkBinding);
        Assert.Collection(command.OutputArguments,
            output => Assert.Equal(("Relationship Type", "GetStringArg"), (output.Name, output.SdkBinding)),
            output => Assert.Equal(("Individual Points", "GetPointNameRefListArg"), (output.Name, output.SdkBinding)),
            output => Assert.Equal(("Point Groups", "GetCollectionObjectNameRefListArg"), (output.Name, output.SdkBinding)),
            output => Assert.Equal(("Point Clouds", "GetCollectionObjectNameRefListArg"), (output.Name, output.SdkBinding)),
            output => Assert.Equal(("Objects", "GetCollectionObjectNameRefListArg"), (output.Name, output.SdkBinding)));

        var result = GetRelationshipAssociatedDataOperation.CreateResult(Completed(AssociatedDataWorker.Outputs));
        Assert.Equal("Point to Point", result.AssociatedData.RelationshipType);
        Assert.Equal("P1", Assert.Single(result.AssociatedData.IndividualPoints).TargetName);
        Assert.Equal(Api.ObjectType.PointGroup, Assert.Single(result.AssociatedData.PointGroups).ObjectType);
        Assert.Equal(Api.ObjectType.Cloud, Assert.Single(result.AssociatedData.PointClouds).ObjectType);
        Assert.Equal(Api.ObjectType.Sphere, Assert.Single(result.AssociatedData.Objects).ObjectType);
        Assert.Throws<ArgumentException>(() => GetRelationshipAssociatedDataOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientReceivesTypedAssociatedData()
    {
        var worker = new AssociatedDataWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var result = await client.GetRelationshipAssociatedDataAsync(Request(),
            deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal("Point to Point", result.AssociatedData.RelationshipType);
        Assert.Equal(GetRelationshipAssociatedDataOperation.Descriptor.OperationId,
            Assert.Single(worker.Commands).OperationId);

    }

    private static Api.GetRelationshipAssociatedDataRequest Request() => new()
    {
        RelationshipName = new() { CollectionName = "Relationships", ItemName = "R1" }
    };

    private static SuccessfulOperationExecution Completed(IReadOnlyList<WorkerMpOutputValue> outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class AssociatedDataWorker : IWorkerCommandExecutor
    {
        public static IReadOnlyList<WorkerMpOutputValue> Outputs =>
        [
            new WorkerRetrievedOutput("Relationship Type", WorkerMpValueKind.Text,
                new WorkerTextValue("Point to Point")),
            new WorkerRetrievedOutput("Individual Points", WorkerMpValueKind.PointNameList,
                new WorkerPointNameListValue([new("Points", "Group A", "P1")])),
            new WorkerRetrievedOutput("Point Groups", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("Groups", "G1", WorkerObjectTypeValue.PointGroup)])),
            new WorkerRetrievedOutput("Point Clouds", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("Clouds", "C1", WorkerObjectTypeValue.Cloud)])),
            new WorkerRetrievedOutput("Objects", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue([new("Objects", "O1", WorkerObjectTypeValue.Sphere)]))
        ];

        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, Outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
