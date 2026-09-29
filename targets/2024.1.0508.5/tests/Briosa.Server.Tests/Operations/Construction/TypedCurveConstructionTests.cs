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

public sealed class TypedCurveConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsSplinesPerimetersAndCurvePoints()
    {
        var worker = new CurveConstructionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var fromPointsRequest = new Api.ConstructBSplineFromPointsRequest
        {
            ResultingBSplineName = Object("Result", "FromPoints")
        };
        fromPointsRequest.PointList.Add(Point("A"));
        fromPointsRequest.PointList.Add(Point("B"));
        var fromPoints = await client.ConstructBSplineFromPointsAsync(fromPointsRequest, options);
        var fromPointSet = await client.ConstructBSplineFromPointSetAsync(new()
        {
            ResultingBSplineName = Object("Result", "FromSet"),
            BSplineFitOptions = new Api.BSplineFitOptions
            {
                UseInterpolationForFit = false,
                OpenCurve = false,
                DegreeOfCurve = 5
            },
            PointSetContainer = Object("Parts", "FitSet")
        }, options);
        var joined = await client.ConstructBSplineFromSeveralBSplinesAsync(new()
        {
            ResultingBSplineName = Object("Result", "Joined"),
            BSplineList = { Object("Parts", "First"), Object("Parts", "Second") }
        }, options);
        var linesRequest = new Api.ConstructBSplinesFromLinesRequest();
        linesRequest.LineList.Add(Object("Parts", "LineA"));
        var fromLinesWithoutPrefix = await client.ConstructBSplinesFromLinesAsync(linesRequest, options);
        var fromLinesWithPrefix = await client.ConstructBSplinesFromLinesAsync(new()
        {
            ResultingBSplineNamePrefix = "Profile",
            LineList = { Object("Parts", "LineB") }
        }, options);
        var perimeterRequest = new Api.ConstructPerimeterFromPointsRequest
        {
            ResultingPerimeterName = Object("Result", "Boundary")
        };
        perimeterRequest.PointList.Add(Point("A"));
        perimeterRequest.PointList.Add(Point("B"));
        perimeterRequest.PointList.Add(Point("C"));
        var perimeter = await client.ConstructPerimeterFromPointsAsync(perimeterRequest, options);
        var nSpaced = await client.ConstructPointsNSpacedOnCurvesAsync(new()
        {
            BSplineList = { Object("Parts", "Spline") },
            ResultantGroupName = Object("Result", "EvenPoints")
        }, options);
        var byDeviation = await client.ConstructPointsOnCurvesUsingMaxChordalDeviationAsync(new()
        {
            BSplineList = { Object("Parts", "Spline") },
            ResultantGroupName = Object("Result", "ChordalPoints")
        }, options);
        var byDistance = await client.ConstructPointsSpacedAtDistanceOnCurvesAsync(new()
        {
            BSplineList = { Object("Parts", "Spline") },
            ResultantGroupName = Object("Result", "DistancePoints")
        }, options);

        Assert.All(new[] { fromPoints.Execution, fromPointSet.Execution, joined.Execution,
            fromLinesWithoutPrefix.Execution, fromLinesWithPrefix.Execution, perimeter.Execution,
            nSpaced.Execution, byDeviation.Execution, byDistance.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(9, worker.Commands.Count);

        var fitDefaults = worker.Commands[0].InputArguments[1].RequireValue<WorkerBSplineFitOptionsValue>();
        Assert.Equal(new WorkerBSplineFitOptionsValue(true, true, 0, 0, 3, 0, 10, 8, false, 0, 0, true, 0.05, 15), fitDefaults);
        var explicitFit = worker.Commands[1].InputArguments[1].RequireValue<WorkerBSplineFitOptionsValue>();
        Assert.False(explicitFit.UseInterpolationFit);
        Assert.False(explicitFit.OpenCurve);
        Assert.Equal(5, explicitFit.Degree);
        Assert.Equal(WorkerObjectTypeValue.PointSet,
            ObjectArgument(worker.Commands[1], 2).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.BSpline,
            ObjectArgument(worker.Commands[2], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[2].InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.False(worker.Commands[2].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Single(worker.Commands[3].InputArguments);
        Assert.Equal("SetCollectionObjectNameRefListArg", worker.Commands[3].InputArguments[0].SdkBinding);
        Assert.Equal(2, worker.Commands[4].InputArguments.Count);
        Assert.Equal("Profile", worker.Commands[4].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Spline", Assert.Single(fromLinesWithoutPrefix.BSplineList).ObjectName);
        Assert.Equal(Api.ObjectType.BSpline, Assert.Single(fromLinesWithPrefix.BSplineList).ObjectType);

        Assert.Equal(WorkerObjectTypeValue.Perimeter, ObjectArgument(worker.Commands[5], 0).ObjectType);
        Assert.False(worker.Commands[5].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.PointGroup, ObjectArgument(worker.Commands[6], 2).ObjectType);
        Assert.Equal(10, worker.Commands[6].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(string.Empty, worker.Commands[6].InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0.05, worker.Commands[7].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(15, worker.Commands[7].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, worker.Commands[7].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.5, worker.Commands[8].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(string.Empty, worker.Commands[8].InputArguments[3].RequireValue<WorkerTextValue>().Value);

        var missingPoints = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructBSplineFromPointsAsync(new()
            {
                ResultingBSplineName = Object("Result", "Invalid")
            }, options));
        var missingCurveList = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsSpacedAtDistanceOnCurvesAsync(new()
            {
                ResultantGroupName = Object("Result", "Invalid")
            }, options));
        Assert.All(new[] { missingPoints, missingCurveList },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(9, worker.Commands.Count);
    }

    [Fact]
    public void CurveConstructionOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        var operations = new[]
        {
            ConstructBSplineFromPointSetOperation.Descriptor,
            ConstructBSplineFromPointsOperation.Descriptor,
            ConstructBSplineFromSeveralBSplinesOperation.Descriptor,
            ConstructBSplinesFromLinesOperation.Descriptor,
            ConstructPerimeterFromPointsOperation.Descriptor,
            ConstructPointsNSpacedOnCurvesOperation.Descriptor,
            ConstructPointsOnCurvesUsingMaxChordalDeviationOperation.Descriptor,
            ConstructPointsSpacedAtDistanceOnCurvesOperation.Descriptor
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

    private sealed class CurveConstructionWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "construction_operations.construct_b_splines_from_lines"
                ? [new WorkerRetrievedOutput("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue(
                        [new WorkerCollectionObjectNameValue("Result", "Spline", WorkerObjectTypeValue.BSpline)]))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
