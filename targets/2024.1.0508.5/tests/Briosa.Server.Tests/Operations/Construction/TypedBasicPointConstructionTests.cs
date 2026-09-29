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

public sealed class TypedBasicPointConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsPointsFromNamesAndCoordinates()
    {
        var worker = new BasicPointWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var origin = await client.ConstructPointAtObjectOriginAsync(new()
        {
            ObjectName = new() { CollectionName = "Parts", ObjectName = "Frame" },
            ResultantPointName = new() { TargetName = "Origin" }
        }, options);
        var circleCenter = await client.ConstructPointAtCircleCenterAsync(new()
        {
            CircleName = new() { CollectionName = "Parts", ObjectName = "Circle" },
            PointName = new() { TargetName = "Center" }
        }, options);
        var intersection = await client.ConstructPointAtIntersectionOfPlaneAndLineAsync(new()
        {
            PlaneName = new() { CollectionName = "Parts", ObjectName = "Plane" },
            LineName = new() { CollectionName = "Parts", ObjectName = "Line" },
            ResultingPointName = new() { TargetName = "Intersection" }
        }, options);
        var midpoint = await client.ConstructPointAtLineMidpointAsync(new()
        {
            LineName = new() { CollectionName = "Parts", ObjectName = "Line" },
            PointName = new() { TargetName = "Midpoint" }
        }, options);
        var workingPoint = await client.ConstructPointInWorkingCoordinatesAsync(new()
        {
            PointName = new() { TargetName = "Working" },
            WorkingCoordinates = new() { X = 1.5, Y = -2.25, Z = 3.75 }
        }, options);

        Assert.All(new[] { origin.Execution, circleCenter.Execution, intersection.Execution,
            midpoint.Execution, workingPoint.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(new Api.Vector { X = 1, Y = 2, Z = 3 }, origin.Origin.VectorRepresentation);
        Assert.Equal(1, origin.Origin.XValue);
        Assert.Equal(2, origin.Origin.YValue);
        Assert.Equal(3, origin.Origin.ZValue);
        Assert.Equal(5, worker.Commands.Count);

        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[0].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[0].InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Circle,
            worker.Commands[1].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane,
            worker.Commands[2].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Line,
            worker.Commands[2].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Line,
            worker.Commands[3].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(new WorkerVectorValue(1.5, -2.25, 3.75),
            worker.Commands[4].InputArguments[1].RequireValue<WorkerVectorValue>());

        var missingCoordinates = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointInWorkingCoordinatesAsync(new()
            {
                PointName = new() { TargetName = "Invalid" }
            }, options));
        var missingCircle = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointAtCircleCenterAsync(new()
            {
                PointName = new() { TargetName = "Invalid" }
            }, options));
        Assert.All(new[] { missingCoordinates, missingCircle },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(5, worker.Commands.Count);
    }

    [Fact]
    public void BasicPointConstructionOperationsAreUnsafeGlobalMutations()
    {
        var operations = new[]
        {
            ConstructPointAtObjectOriginOperation.Descriptor,
            ConstructPointAtCircleCenterOperation.Descriptor,
            ConstructPointAtIntersectionOfPlaneAndLineOperation.Descriptor,
            ConstructPointAtLineMidpointOperation.Descriptor,
            ConstructPointInWorkingCoordinatesOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private sealed class BasicPointWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId ==
                "construction_operations.construct_point_at_object_origin"
                ?
                [
                    new WorkerRetrievedOutput("Vector Representation", WorkerMpValueKind.Vector,
                        new WorkerVectorValue(1, 2, 3)),
                    new WorkerRetrievedOutput("X Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1)),
                    new WorkerRetrievedOutput("Y Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2)),
                    new WorkerRetrievedOutput("Z Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3))
                ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
