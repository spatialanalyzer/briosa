using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedAngleDistanceToleranceOperationTests
{
    [Fact]
    public void RenamedAngleTolerancePreservesSdkLabelAndZeroSentinel()
    {
        var request = new Api.AngleBetweenLineAndPlaneRequest
        {
            SelectedLine = new() { CollectionName = "Collection", ObjectName = "Line", ObjectType = Api.ObjectType.Line },
            SelectedPlane = new() { CollectionName = "Collection", ObjectName = "Plane", ObjectType = Api.ObjectType.Plane },
            AngleTolerance = 0
        };
        var tolerance = Assert.Single(AngleBetweenLineAndPlaneOperation.CreateCommand(request).InputArguments,
            value => value.Name == "Angle Tolerance (0.0 for none)");
        Assert.Equal("SetDoubleArg", tolerance.SdkBinding);
        Assert.Equal(0, tolerance.RequireValue<WorkerDoubleValue>().Value);
        request.AngleTolerance = 0.25;
        tolerance = Assert.Single(AngleBetweenLineAndPlaneOperation.CreateCommand(request).InputArguments,
            value => value.Name == "Angle Tolerance (0.0 for none)");
        Assert.Equal(0.25, tolerance.RequireValue<WorkerDoubleValue>().Value);
    }

    private static readonly string[] MigratedIds =
    [
        "analysis_operations.angle_between_line_and_plane",
        "analysis_operations.angle_between_two_lines",
        "analysis_operations.angle_between_two_planes_normals",
        "analysis_operations.get_point_to_line_distance",
        "analysis_operations.get_point_to_point_distance",
        "analysis_operations.get_point_tolerance"
    ];

    [Fact]
    public void EachMeasurementHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void RequiredObjectsAndZeroNominalDefaultsMatchTheReviewedContracts()
    {
        Assert.Throws<ArgumentException>(() => AngleBetweenLineAndPlaneOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => AngleBetweenTwoLinesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => AngleBetweenTwoPlanesNormalsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointToLineDistanceOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointToPointDistanceOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointToleranceOperation.CreateCommand(new()));
        Assert.Equal("Angle Between Two Planes’ normals", AngleBetweenTwoPlanesNormalsOperation.Descriptor.MpStep);

        var line = new Api.CollectionObjectName { ObjectName = "line" };
        var plane = new Api.CollectionObjectName { ObjectName = "plane" };
        var angle = AngleBetweenLineAndPlaneOperation.CreateCommand(new()
            { SelectedLine = line, SelectedPlane = plane });
        var angleInputs = angle.InputArguments;
        Assert.Equal(4, angleInputs.Count);
        Assert.Equal("SetCollectionObjectNameArg2", angleInputs[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", angleInputs[1].SdkBinding);
        Assert.Equal(0, angleInputs[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, angleInputs[3].RequireValue<WorkerDoubleValue>().Value);

        var point = new Api.PointName { CollectionName = "collection", GroupName = "group", TargetName = "P1" };
        var pointToLine = GetPointToLineDistanceOperation.CreateCommand(new()
            { Point = point, Line = line });
        Assert.Equal("SetPointNameArg", pointToLine.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", pointToLine.InputArguments[1].SdkBinding);
        var pointToPoint = GetPointToPointDistanceOperation.CreateCommand(new()
            { FirstPoint = point, SecondPoint = new Api.PointName { CollectionName = "collection", GroupName = "group", TargetName = "P2" } });
        Assert.All(pointToPoint.InputArguments, item => Assert.Equal("SetPointNameArg", item.SdkBinding));
        var tolerance = GetPointToleranceOperation.CreateCommand(new() { PointName = point });
        Assert.Equal("SetPointNameArg", Assert.Single(tolerance.InputArguments).SdkBinding);
        Assert.Equal(17, tolerance.OutputArguments.Count);
        Assert.Equal("GetToleranceVectorOptionsArg", tolerance.OutputArguments[^1].SdkBinding);
    }

    [Fact]
    public void MeasurementResultsPreserveVectorAndToleranceParts()
    {
        var vector = new WorkerVectorValue(1, 2, 3);
        var angle = AngleBetweenLineAndPlaneOperation.CreateResult(Completed(Output("Angle", new WorkerDoubleValue(45))));
        var linesAngle = AngleBetweenTwoLinesOperation.CreateResult(Completed(Output("Angle", new WorkerDoubleValue(60))));
        var planesAngle = AngleBetweenTwoPlanesNormalsOperation.CreateResult(Completed(Output("Angle", new WorkerDoubleValue(90))));
        var pointToLine = GetPointToLineDistanceOperation.CreateResult(Completed(
            Output("Vector Representation", vector), Output("X Value", new WorkerDoubleValue(1)),
            Output("Y Value", new WorkerDoubleValue(2)), Output("Z Value", new WorkerDoubleValue(3)),
            Output("Magnitude", new WorkerDoubleValue(4))));
        var pointToPoint = GetPointToPointDistanceOperation.CreateResult(Completed(
            Output("Vector Representation", new WorkerVectorValue(4, 5, 6)),
            Output("X Value", new WorkerDoubleValue(4)), Output("Y Value", new WorkerDoubleValue(5)),
            Output("Z Value", new WorkerDoubleValue(6)), Output("Magnitude", new WorkerDoubleValue(8))));
        var tolerance = GetPointToleranceOperation.CreateResult(Completed(
            Output("Use High X Tolerance?", new WorkerBooleanValue(true)), Output("High X Tolerance", new WorkerDoubleValue(1)),
            Output("Use High Y Tolerance?", new WorkerBooleanValue(false)), Output("High Y Tolerance", new WorkerDoubleValue(2)),
            Output("Use High Z Tolerance?", new WorkerBooleanValue(true)), Output("High Z Tolerance", new WorkerDoubleValue(3)),
            Output("Use High Mag Tolerance?", new WorkerBooleanValue(false)), Output("High Mag Tolerance", new WorkerDoubleValue(4)),
            Output("Use Low X Tolerance?", new WorkerBooleanValue(true)), Output("Low X Tolerance", new WorkerDoubleValue(5)),
            Output("Use Low Y Tolerance?", new WorkerBooleanValue(false)), Output("Low Y Tolerance", new WorkerDoubleValue(6)),
            Output("Use Low Z Tolerance?", new WorkerBooleanValue(true)), Output("Low Z Tolerance", new WorkerDoubleValue(7)),
            Output("Use Low Mag Tolerance?", new WorkerBooleanValue(false)), Output("Low Mag Tolerance", new WorkerDoubleValue(8)),
            Output("Vector Tolerance", new WorkerToleranceVectorOptionsValue(
                new(true, 1), new(false, 0), new(true, 3), new(false, 0),
                new(false, 0), new(true, 6), new(false, 0), new(true, 8)))));

        Assert.Equal(45, angle.Angle);
        Assert.Equal(60, linesAngle.Angle);
        Assert.Equal(90, planesAngle.Angle);
        Assert.Equal(3, pointToLine.VectorRepresentation.Z);
        Assert.Equal(4, pointToLine.Magnitude);
        Assert.Equal(5, pointToPoint.YValue);
        Assert.Equal(8, pointToPoint.Magnitude);
        Assert.True(tolerance.UseHighXTolerance);
        Assert.False(tolerance.UseHighYTolerance);
        Assert.Equal(7, tolerance.LowZTolerance);
        Assert.True(tolerance.VectorTolerance.HighZ.Enabled);
        Assert.Equal(8, tolerance.VectorTolerance.LowMagnitude.Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedAngleRoute()
    {
        var worker = new AngleWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .AngleBetweenTwoLinesAsync(new()
            {
                Line1 = new Api.CollectionObjectName { ObjectName = "line-a" },
                Line2 = new Api.CollectionObjectName { ObjectName = "line-b" }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(45, result.Angle);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MigratedIds[1], Assert.Single(worker.Commands).OperationId);
    }

    private static WorkerRetrievedOutput Output(string name, WorkerMpValue value) => new(
        name, value switch
        {
            WorkerVectorValue => WorkerMpValueKind.Vector,
            WorkerDoubleValue => WorkerMpValueKind.FloatingPoint,
            WorkerBooleanValue => WorkerMpValueKind.Logical,
            WorkerToleranceVectorOptionsValue => WorkerMpValueKind.ToleranceVectorOptions,
            _ => throw new InvalidOperationException()
        }, value);

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class AngleWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Angle Between Two Lines", command.StepName);
            var outputs = new[] { Output("Angle", new WorkerDoubleValue(45)) };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
