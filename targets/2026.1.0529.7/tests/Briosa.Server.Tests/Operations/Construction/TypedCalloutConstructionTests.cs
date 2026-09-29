using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedCalloutConstructionTests
{
    [Fact]
    public async Task GeneratedClientCreatesCalloutsWithExactBindingsDefaultsAndOptionalArguments()
    {
        var worker = new CalloutWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var calloutView = Item("Views", "Main", Api.ItemType.CalloutView);

        await client.CreatePictureCalloutAsync(new()
        {
            DestinationCalloutView = calloutView,
            PictureName = Item("Images", "Photo", Api.ItemType.Picture)
        }, options);
        await client.CreatePictureCalloutAsync(new()
        {
            DestinationCalloutView = calloutView,
            PictureName = Item("Images", "Photo2", Api.ItemType.Picture),
            ScaleImagePercent = 85,
            ObjectForCalloutAnchorPoint = Item("Points", "P1", Api.ItemType.Any)
        }, options);
        await client.CreateRelationshipCalloutAsync(new()
        {
            DestinationCalloutView = calloutView,
            RelationshipName = Item("Relationships", "Alignment", Api.ItemType.Relationship)
        }, options);
        await client.CreateTextCalloutAsync(new() { DestinationCalloutView = calloutView }, options);
        await client.CreateTextCalloutAsync(new()
        {
            DestinationCalloutView = calloutView,
            Text = { "First line", "Second line" },
            CalloutAnchorPoint = Point("Points", "Control", "P1")
        }, options);
        await client.CreatePointCalloutAsync(new()
        {
            DestinationCalloutView = Item("Views", "PointDestination", Api.ItemType.Any),
            Point = Point("Points", "Control", "P1"),
            DesiredCoordinateSystem = Api.CoordinateSystemType.Cartesian,
            Notes = { "point note" }
        }, options);
        await client.CreatePointComparisonCalloutAsync(new()
        {
            DestinationCalloutView = Item("Views", "ComparisonDestination", Api.ItemType.Any),
            FirstPoint = Point("Points", "Control", "P1"),
            SecondPoint = Point("Points", "Control", "P2"),
            AdditionalNotes = { "comparison note" }
        }, options);
        await client.CreateVectorCalloutAsync(new()
        {
            DestinationCalloutView = calloutView,
            VectorGroupName = Object("Results", "Vectors"),
            AdditionalNotes = { "vector note" }
        }, options);
        await client.CreateMinMaxVectorGroupCalloutAsync(new()
        {
            DestinationCalloutView = calloutView,
            VectorGroupName = Object("Results", "Vectors")
        }, options);

        Assert.Equal(9, worker.Commands.Count);
        AssertCalloutName(worker.Commands[0].InputArguments[0], "Views", "Main", WorkerItemTypeValue.CalloutView);
        Assert.Equal(4, worker.Commands[0].InputArguments.Count);
        Assert.Equal(0.4, worker.Commands[0].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.6, worker.Commands[0].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(6, worker.Commands[1].InputArguments.Count);
        Assert.Equal(85, worker.Commands[1].InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerItemTypeValue.Any,
            worker.Commands[1].InputArguments[5].RequireValue<WorkerCollectionItemNameValue>().ItemType);

        var relationship = worker.Commands[2];
        Assert.Equal(5, relationship.InputArguments.Count);
        Assert.Equal(0, relationship.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Empty(relationship.InputArguments[4].RequireValue<WorkerStringListValue>().Values);

        Assert.Equal(3, worker.Commands[3].InputArguments.Count);
        Assert.Equal(0.4, worker.Commands[3].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.6, worker.Commands[3].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(5, worker.Commands[4].InputArguments.Count);
        Assert.Equal("First line", worker.Commands[4].InputArguments[1].RequireValue<WorkerStringListValue>().Values[0]);
        Assert.Equal("Second line", worker.Commands[4].InputArguments[1].RequireValue<WorkerStringListValue>().Values[1]);

        var pointCallout = worker.Commands[5];
        Assert.Equal(18, pointCallout.InputArguments.Count);
        Assert.False(pointCallout.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(pointCallout.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(pointCallout.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerCoordinateSystemTypeValue.Cartesian,
            pointCallout.InputArguments[15].RequireValue<WorkerChoiceValue<WorkerCoordinateSystemTypeValue>>().Value);
        Assert.Equal("point note", pointCallout.InputArguments[16].RequireValue<WorkerStringListValue>().Values.Single());

        var comparison = worker.Commands[6];
        Assert.Equal(22, comparison.InputArguments.Count);
        Assert.True(comparison.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(comparison.InputArguments[13].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("comparison note", comparison.InputArguments[20].RequireValue<WorkerStringListValue>().Values.Single());

        var vector = worker.Commands[7];
        Assert.Equal(22, vector.InputArguments.Count);
        Assert.Equal(string.Empty, vector.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.True(vector.InputArguments[7].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("vector note", vector.InputArguments[19].RequireValue<WorkerStringListValue>().Values.Single());

        var minMax = worker.Commands[8];
        Assert.Equal(21, minMax.InputArguments.Count);
        Assert.Equal(1, minMax.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(1, minMax.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(minMax.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(minMax.InputArguments[19].RequireValue<WorkerBooleanValue>().Value);

        var invalidDestination = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreateTextCalloutAsync(new(), options));
        var invalidPointCallout = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreatePointCalloutAsync(new()
            {
                DestinationCalloutView = calloutView,
                Point = Point("Points", "Control", "P1")
            }, options));
        var invalidPictureName = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CreatePictureCalloutAsync(new() { DestinationCalloutView = calloutView }, options));
        Assert.Equal(StatusCode.InvalidArgument, invalidDestination.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, invalidPointCallout.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, invalidPictureName.StatusCode);
        Assert.Equal(9, worker.Commands.Count);
    }

    [Fact]
    public void CalloutConstructionOperationsAreTypedUnsafeMutations()
    {
        foreach (var operation in new[]
        {
            CreateMinMaxVectorGroupCalloutOperation.Descriptor,
            CreatePictureCalloutOperation.Descriptor,
            CreatePointCalloutOperation.Descriptor,
            CreatePointComparisonCalloutOperation.Descriptor,
            CreateRelationshipCalloutOperation.Descriptor,
            CreateTextCalloutOperation.Descriptor,
            CreateVectorCalloutOperation.Descriptor
        })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private static Api.CollectionItemName Item(string collection, string name, Api.ItemType type) => new()
    {
        CollectionName = collection,
        ItemName = name,
        ItemType = type
    };

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private static Api.PointName Point(string collection, string group, string name) => new()
    {
        CollectionName = collection,
        GroupName = group,
        TargetName = name
    };

    private static void AssertCalloutName(WorkerMpInputArgument argument,
        string collection, string item, WorkerItemTypeValue itemType) =>
        Assert.Equal(new WorkerCollectionItemNameValue(collection, item, itemType),
            argument.RequireValue<WorkerCollectionItemNameValue>());

    private sealed class CalloutWorker : IWorkerCommandExecutor
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
