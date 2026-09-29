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

public sealed class TypedPointTransformConstructionTests
{
    [Fact]
    public async Task GeneratedClientMapsPointTransformsAndObjectDeltaOperations()
    {
        var worker = new PointTransformWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var cylindricalRequest = new Api.ConstructPointsCylindricallyShiftedRequest
        {
            ReferenceObjectName = Object("Parts", "Axis"),
            GroupForNewPoints = Object("Result", "Shifted")
        };
        cylindricalRequest.OriginalPoints.Add(Point("P1"));
        cylindricalRequest.OriginalPoints.Add(Point("P2"));
        var cylindrical = await client.ConstructPointsCylindricallyShiftedAsync(cylindricalRequest, options);
        var shiftedRequest = new Api.ConstructPointsShiftedInWorkingFrameRequest
        {
            GroupForNewPoints = Object("Result", "Translated"),
            ShiftVector = Vector(1, -2, 3)
        };
        shiftedRequest.OriginalPoints.Add(Point("P1"));
        var shifted = await client.ConstructPointsShiftedInWorkingFrameAsync(shiftedRequest, options);
        var copied = await client.CopyObjectsPointToPointDeltaAsync(new()
        {
            ObjectsToCopy = { Object("Parts", "Surface") },
            FirstDeltaPoint = Point("A"),
            SecondDeltaPoint = Point("B")
        }, options);
        var copiedToCollection = await client.CopyObjectsPointToPointDeltaAsync(new()
        {
            ObjectsToCopy = { Object("Parts", "Mesh") },
            FirstDeltaPoint = Point("C"),
            SecondDeltaPoint = Point("D"),
            DestinationCollectionName = new Api.CollectionName { Name = "Copies" }
        }, options);
        var moved = await client.MoveObjectsPointToPointDeltaAsync(new()
        {
            ObjectsToMove = { Object("Parts", "Plane") },
            FirstDeltaPoint = Point("E"),
            SecondDeltaPoint = Point("F")
        }, options);
        var setPosition = await client.SetPointPositionInWorkingCoordinatesAsync(new()
        {
            PointName = Point("P3"),
            PositionInWorkingCoordinates = Vector(4, 5, 6)
        }, options);
        var transformed = await client.TransformPointsByDeltaAboutWorkingFrameAsync(new()
        {
            PointNameList = { Point("P4"), Point("P5") },
            DeltaInWorkingCoordinates = Vector(-1, 0.5, 2)
        }, options);

        Assert.All(new[] { cylindrical.Execution, shifted.Execution, copied.Execution,
                copiedToCollection.Execution, moved.Execution, setPosition.Execution, transformed.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(7, worker.Commands.Count);

        var cylindricalArgs = worker.Commands[0].InputArguments;
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(worker.Commands[0], 0).ObjectType);
        Assert.Equal(2, cylindricalArgs[1].RequireValue<WorkerPointNameListValue>().Values.Count);
        Assert.Equal(WorkerObjectTypeValue.PointGroup, ObjectArgument(worker.Commands[0], 2).ObjectType);
        Assert.Equal(0, cylindricalArgs[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, cylindricalArgs[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, cylindricalArgs[5].RequireValue<WorkerDoubleValue>().Value);

        var shiftedArgs = worker.Commands[1].InputArguments;
        Assert.Equal(WorkerObjectTypeValue.PointGroup, ObjectArgument(worker.Commands[1], 1).ObjectType);
        Assert.Equal(new WorkerVectorValue(1, -2, 3), shiftedArgs[2].RequireValue<WorkerVectorValue>());
        Assert.Equal("SetCollectionObjectNameRefListArg", worker.Commands[2].InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Any,
            Assert.Single(worker.Commands[2].InputArguments[0]
                .RequireValue<WorkerCollectionObjectNameListValue>().Values).ObjectType);
        Assert.Equal(3, worker.Commands[2].InputArguments.Count);
        Assert.Equal(4, worker.Commands[3].InputArguments.Count);
        Assert.Equal("SetCollectionNameArg", worker.Commands[3].InputArguments[3].SdkBinding);
        Assert.Equal("Copies", worker.Commands[3].InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetPointNameArg", worker.Commands[4].InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerVectorValue(4, 5, 6),
            worker.Commands[5].InputArguments[1].RequireValue<WorkerVectorValue>());
        Assert.Equal(2, worker.Commands[6].InputArguments[0]
            .RequireValue<WorkerPointNameListValue>().Values.Count);
        Assert.Equal(new WorkerVectorValue(-1, 0.5, 2),
            worker.Commands[6].InputArguments[1].RequireValue<WorkerVectorValue>());

        var missingCylindricalPoints = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsCylindricallyShiftedAsync(new()
            {
                ReferenceObjectName = Object("Parts", "Axis"),
                GroupForNewPoints = Object("Result", "Invalid")
            }, options));
        var missingShiftVector = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsShiftedInWorkingFrameAsync(new()
            {
                OriginalPoints = { Point("P1") },
                GroupForNewPoints = Object("Result", "Invalid")
            }, options));
        var missingCopyPoint = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CopyObjectsPointToPointDeltaAsync(new()
            {
                ObjectsToCopy = { Object("Parts", "Surface") },
                FirstDeltaPoint = Point("A")
            }, options));
        var missingPosition = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.SetPointPositionInWorkingCoordinatesAsync(new() { PointName = Point("P3") }, options));
        var missingTransformVector = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.TransformPointsByDeltaAboutWorkingFrameAsync(new()
            {
                PointNameList = { Point("P4") }
            }, options));
        Assert.All(new[] { missingCylindricalPoints, missingShiftVector, missingCopyPoint,
                missingPosition, missingTransformVector },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(7, worker.Commands.Count);
    }

    [Fact]
    public void PointTransformOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        var operations = new[]
        {
            ConstructPointsCylindricallyShiftedOperation.Descriptor,
            ConstructPointsShiftedInWorkingFrameOperation.Descriptor,
            CopyObjectsPointToPointDeltaOperation.Descriptor,
            MoveObjectsPointToPointDeltaOperation.Descriptor,
            SetPointPositionInWorkingCoordinatesOperation.Descriptor,
            TransformPointsByDeltaAboutWorkingFrameOperation.Descriptor
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

    private static Api.Vector Vector(double x, double y, double z) => new() { X = x, Y = y, Z = z };

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class PointTransformWorker : IWorkerCommandExecutor
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
