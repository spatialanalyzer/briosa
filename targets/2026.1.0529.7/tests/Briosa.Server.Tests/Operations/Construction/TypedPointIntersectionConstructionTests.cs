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

public sealed class TypedPointIntersectionConstructionTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedPointIntersectionRoutes()
    {
        var worker = new PointIntersectionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var planes = await client.ConstructPointAtIntersectionOfPlanesAsync(new()
        {
            Plane1Name = new() { ObjectName = "P1" },
            Plane2Name = new() { ObjectName = "P2" },
            Plane3Name = new() { ObjectName = "P3" },
            PointName = new() { TargetName = "Intersection" }
        }, options);
        var splines = await client.ConstructPointAtIntersectionOfTwoBSplinesAsync(new()
        {
            FirstBSplineName = new() { ObjectName = "S1" },
            SecondBSplineName = new() { ObjectName = "S2" },
            PointName = new() { TargetName = "Intersection" }
        }, options);
        var lines = await client.ConstructPointAtIntersectionOfTwoLinesAsync(new()
        {
            FirstLineName = new() { ObjectName = "L1" },
            SecondLineName = new() { ObjectName = "L2" },
            ResultingPointName = new() { TargetName = "Intersection" }
        }, options);
        var circleAndLine = await client.ConstructPointsAtIntersectionOfCircleAndLineAsync(new()
        {
            CircleName = new() { ObjectName = "Circle" },
            LineName = new() { ObjectName = "Line" },
            BasePointNameForResults = new() { TargetName = "Intersection" }
        }, options);

        Assert.All(new[] { planes.Execution, splines.Execution, lines.Execution, circleAndLine.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(4, worker.Commands.Count);
        Assert.All(worker.Commands[0].InputArguments.Take(3), argument =>
            Assert.Equal(WorkerObjectTypeValue.Plane,
                argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectType));
        Assert.All(worker.Commands[1].InputArguments.Take(2), argument =>
            Assert.Equal(WorkerObjectTypeValue.BSpline,
                argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectType));
        Assert.All(worker.Commands[2].InputArguments.Take(2), argument =>
            Assert.Equal(WorkerObjectTypeValue.Line,
                argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectType));
        Assert.Equal(WorkerObjectTypeValue.Circle,
            worker.Commands[3].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Line,
            worker.Commands[3].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.All(worker.Commands.SelectMany(command => command.InputArguments), argument =>
            Assert.StartsWith("Set", argument.SdkBinding, StringComparison.Ordinal));

        var missingPlane = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointAtIntersectionOfPlanesAsync(new()
            {
                Plane2Name = new() { ObjectName = "P2" }, Plane3Name = new() { ObjectName = "P3" },
                PointName = new() { TargetName = "Invalid" }
            }, options));
        var missingSpline = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointAtIntersectionOfTwoBSplinesAsync(new()
            {
                FirstBSplineName = new() { ObjectName = "S1" }, PointName = new() { TargetName = "Invalid" }
            }, options));
        Assert.All(new[] { missingPlane, missingSpline },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(4, worker.Commands.Count);
    }

    [Fact]
    public void PointIntersectionOperationsAreUnsafeGlobalMutations()
    {
        var operations = new[]
        {
            ConstructPointAtIntersectionOfPlanesOperation.Descriptor,
            ConstructPointAtIntersectionOfTwoBSplinesOperation.Descriptor,
            ConstructPointAtIntersectionOfTwoLinesOperation.Descriptor,
            ConstructPointsAtIntersectionOfCircleAndLineOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private sealed class PointIntersectionWorker : IWorkerCommandExecutor
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
