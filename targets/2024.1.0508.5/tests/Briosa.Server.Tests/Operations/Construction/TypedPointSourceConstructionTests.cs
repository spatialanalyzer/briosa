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

public sealed class TypedPointSourceConstructionTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedPointSourceRoutesAndPreservesDefaults()
    {
        var worker = new PointSourceWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var splineSurface = await client.ConstructPointAtIntersectionOfBSplineAndSurfacesAsync(new()
        {
            BSplineName = new() { ObjectName = "Spline" },
            SurfaceList = { new Api.CollectionObjectName { ObjectName = "Surface" } },
            PointName = new() { TargetName = "Intersection" }
        }, options);
        var projection = await client.ConstructPointAtProjectionOfPointOntoObjectAsync(new()
        {
            PointToProject = new() { TargetName = "Source" },
            ObjectName = new() { ObjectName = "Surface" },
            ResultingPointName = new() { TargetName = "Projected" }
        }, options);
        var fitted = await client.ConstructPointFitToPointsAsync(new()
        {
            PointNames = { new Api.PointName { TargetName = "A" }, new Api.PointName { TargetName = "B" } },
            ResultingPointName = new() { TargetName = "Fit" }
        }, options);
        var defaultSurvey = await client.ConstructPointFromSurveyTargetCenterAsync(new()
        {
            CloudContainingTarget = new() { ObjectName = "Cloud" },
            ReferenceSeedPoint = new() { TargetName = "Seed" },
            ResultCenterPointName = new() { TargetName = "TargetCenter" }
        }, options);
        var circleSurvey = await client.ConstructPointFromSurveyTargetCenterAsync(new()
        {
            CloudContainingTarget = new() { ObjectName = "Cloud" },
            ReferenceSeedPoint = new() { TargetName = "Seed" },
            SurveyTargetType = Api.SurveyTargetType.Circle,
            SearchDiameter = 12.5,
            ResultCenterPointName = new() { TargetName = "CircleCenter" }
        }, options);

        Assert.All(new[] { splineSurface.Execution, projection.Execution, fitted.Execution,
            defaultSurvey.Execution, circleSurvey.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(5, worker.Commands.Count);
        Assert.Equal(WorkerObjectTypeValue.BSpline,
            worker.Commands[0].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[0].InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.Equal(0.001, worker.Commands[0].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("SetCollectionObjectNameRefListArg", worker.Commands[0].InputArguments[1].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[1].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(2, worker.Commands[2].InputArguments[0].RequireValue<WorkerPointNameListValue>().Values.Count);
        Assert.Equal("Triangle", worker.Commands[3].InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0d, worker.Commands[3].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("Circle", worker.Commands[4].InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(12.5, worker.Commands[4].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);

        var emptySurfaceList = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointAtIntersectionOfBSplineAndSurfacesAsync(new()
            {
                BSplineName = new() { ObjectName = "Spline" },
                PointName = new() { TargetName = "Invalid" }
            }, options));
        var emptyPointList = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointFitToPointsAsync(new()
            {
                ResultingPointName = new() { TargetName = "Invalid" }
            }, options));
        var unknownSurveyType = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointFromSurveyTargetCenterAsync(new()
            {
                CloudContainingTarget = new() { ObjectName = "Cloud" },
                ReferenceSeedPoint = new() { TargetName = "Seed" },
                SurveyTargetType = (Api.SurveyTargetType)99,
                ResultCenterPointName = new() { TargetName = "Invalid" }
            }, options));
        Assert.All(new[] { emptySurfaceList, emptyPointList, unknownSurveyType },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(5, worker.Commands.Count);
    }

    [Fact]
    public void PointSourceOperationsAreUnsafeGlobalMutations()
    {
        var operations = new[]
        {
            ConstructPointAtIntersectionOfBSplineAndSurfacesOperation.Descriptor,
            ConstructPointAtProjectionOfPointOntoObjectOperation.Descriptor,
            ConstructPointFitToPointsOperation.Descriptor,
            ConstructPointFromSurveyTargetCenterOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private sealed class PointSourceWorker : IWorkerCommandExecutor
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
