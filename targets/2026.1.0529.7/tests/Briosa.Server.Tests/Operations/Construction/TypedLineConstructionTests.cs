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

public sealed class TypedLineConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsLinesWithTypedArgumentsAndCatalogDefaults()
    {
        var worker = new LineConstructionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var centerOfSlot = await client.ConstructLineCenterOfSlotAsync(new()
        {
            LineName = Object("Result", "Center"),
            SlotName = Object("Parts", "Slot")
        }, options);
        var normalToObject = await client.ConstructLineNormalToObjectAsync(new()
        {
            LineName = Object("Result", "Normal"),
            Object = Object("Parts", "Surface")
        }, options);
        var normalThroughPoint = await client.ConstructLineNormalToObjectThroughPointAsync(new()
        {
            LineToCreate = Object("Result", "NormalThroughPoint"),
            ObjectName = Object("Parts", "Surface"),
            PointName = Point("Anchor")
        }, options);
        var projected = await client.ConstructLineProjectLineToObjectReferencePlaneAsync(new()
        {
            LineToCreate = Object("Result", "Projected"),
            LineToProject = Object("Parts", "SourceLine"),
            ObjectToProjectTo = Object("Parts", "Reference")
        }, options);
        var planeIntersection = await client.ConstructLineTwoPlaneIntersectionAsync(new()
        {
            LineName = Object("Result", "PlaneIntersection"),
            FirstPlane = Object("Parts", "PlaneA"),
            SecondPlane = Object("Parts", "PlaneB")
        }, options);
        var twoPoints = await client.ConstructLineTwoPointsAsync(new()
        {
            LineName = Object("Result", "BetweenPoints"),
            FirstPoint = Point("A"),
            SecondPoint = Point("B")
        }, options);
        var twoVectors = await client.ConstructLineTwoPointsVectorNotationAsync(new()
        {
            LineName = Object("Result", "BetweenVectors"),
            FirstVector = new Api.Vector { X = 1.5, Y = -2.25, Z = 3.75 },
            SecondVector = new Api.Vector { X = -4, Y = 5, Z = 6 }
        }, options);

        Assert.All(new[] { centerOfSlot.Execution, normalToObject.Execution, normalThroughPoint.Execution,
            projected.Execution, planeIntersection.Execution, twoPoints.Execution, twoVectors.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(7, worker.Commands.Count);

        Assert.Equal(WorkerObjectTypeValue.Line, ObjectArgument(worker.Commands[0], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Slot, ObjectArgument(worker.Commands[0], 1).ObjectType);
        Assert.Equal(1.0, worker.Commands[1].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(worker.Commands[1], 2).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Line, ObjectArgument(worker.Commands[2], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(worker.Commands[2], 1).ObjectType);
        Assert.Equal("SetPointNameArg", worker.Commands[2].InputArguments[2].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Line, ObjectArgument(worker.Commands[3], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Line, ObjectArgument(worker.Commands[3], 1).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(worker.Commands[3], 2).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane, ObjectArgument(worker.Commands[4], 1).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane, ObjectArgument(worker.Commands[4], 2).ObjectType);
        Assert.Equal(new WorkerPointNameValue("", "", "A"),
            worker.Commands[5].InputArguments[1].RequireValue<WorkerPointNameValue>());
        Assert.Equal(new WorkerVectorValue(1.5, -2.25, 3.75),
            worker.Commands[6].InputArguments[1].RequireValue<WorkerVectorValue>());
        Assert.Equal(new WorkerVectorValue(-4, 5, 6),
            worker.Commands[6].InputArguments[2].RequireValue<WorkerVectorValue>());

        var missingPoint = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructLineTwoPointsAsync(new()
            {
                LineName = Object("Result", "Invalid"),
                FirstPoint = Point("A")
            }, options));
        var missingVector = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructLineTwoPointsVectorNotationAsync(new()
            {
                LineName = Object("Result", "Invalid")
            }, options));
        Assert.All(new[] { missingPoint, missingVector },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(7, worker.Commands.Count);
    }

    [Fact]
    public void LineConstructionOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        var operations = new[]
        {
            ConstructLineCenterOfSlotOperation.Descriptor,
            ConstructLineNormalToObjectOperation.Descriptor,
            ConstructLineNormalToObjectThroughPointOperation.Descriptor,
            ConstructLineProjectLineToObjectReferencePlaneOperation.Descriptor,
            ConstructLineTwoPlaneIntersectionOperation.Descriptor,
            ConstructLineTwoPointsOperation.Descriptor,
            ConstructLineTwoPointsVectorNotationOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private static Api.PointName Point(string name) => new() { TargetName = name };

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class LineConstructionWorker : IWorkerCommandExecutor
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
