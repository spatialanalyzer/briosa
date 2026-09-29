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

public sealed class TypedRemainingGeometryPropertyOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.get_bspline_properties",
        "analysis_operations.get_cone_properties",
        "analysis_operations.get_cylinder_properties",
        "analysis_operations.get_line_properties",
        "analysis_operations.get_plane_properties",
        "analysis_operations.get_surface_physical_stats",
        "analysis_operations.get_slot_properties",
        "analysis_operations.get_point_coordinate",
        "analysis_operations.get_point_coordinate_cylindrical",
        "analysis_operations.get_point_coordinate_polar",
        "analysis_operations.get_point_properties"
    ];

    [Fact]
    public void EachGeometryQueryHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void GeometryQueriesRequireTheirObjectAndKeepTargetBindings()
    {
        Assert.Throws<ArgumentException>(() => GetBSplinePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetConePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetCylinderPropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetLinePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPlanePropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetSurfacePhysicalStatsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetSlotPropertiesOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointCoordinateOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointCoordinateCylindricalOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointCoordinatePolarOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetPointPropertiesOperation.CreateCommand(new()));

        var spline = GetBSplinePropertiesOperation.CreateCommand(new()
            { BSplineName = new Api.CollectionObjectName { ObjectName = "spline" } });
        var cone = GetConePropertiesOperation.CreateCommand(new()
            { ConeName = new Api.CollectionObjectName { ObjectName = "cone" } });
        var cylinder = GetCylinderPropertiesOperation.CreateCommand(new()
            { CylinderName = new Api.CollectionObjectName { ObjectName = "cylinder" } });
        var line = GetLinePropertiesOperation.CreateCommand(new()
            { LineName = new Api.CollectionObjectName { ObjectName = "line" } });
        var plane = GetPlanePropertiesOperation.CreateCommand(new()
            { PlaneName = new Api.CollectionObjectName { ObjectName = "plane" } });
        var surface = GetSurfacePhysicalStatsOperation.CreateCommand(new()
            { SurfaceName = new Api.CollectionObjectName { ObjectName = "surface" } });
        var slot = GetSlotPropertiesOperation.CreateCommand(new()
            { SlotName = new Api.CollectionObjectName { ObjectName = "slot" } });
        var pointName = new Api.PointName { CollectionName = "collection", GroupName = "group", TargetName = "point" };
        var pointCoordinate = GetPointCoordinateOperation.CreateCommand(new() { PointName = pointName });
        var cylindrical = GetPointCoordinateCylindricalOperation.CreateCommand(new() { PointName = pointName });
        var polar = GetPointCoordinatePolarOperation.CreateCommand(new() { PointName = pointName });
        var pointProperties = GetPointPropertiesOperation.CreateCommand(new() { PointName = pointName });

        foreach (var command in new[] { spline, cone, cylinder, line, plane, surface, slot })
            Assert.Equal("SetCollectionObjectNameArg2", Assert.Single(command.InputArguments).SdkBinding);
        foreach (var command in new[] { pointCoordinate, cylindrical, polar, pointProperties })
            Assert.Equal("SetPointNameArg", Assert.Single(command.InputArguments).SdkBinding);
        Assert.Equal(["GetIntegerArg", "GetIntegerArg", "GetIntegerArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg"],
            spline.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetVectorArg", "GetVectorArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg"],
            cone.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetVectorArg", "GetVectorArg", "GetVectorArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg"],
            line.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetVectorArg", "GetVectorArg", "GetDoubleArg"],
            plane.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetDoubleArg", "GetDoubleArg"],
            surface.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetVectorArg", "GetVectorArg", "GetVectorArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetBoolArg", "GetIntegerArg", "GetBoolArg", "GetDoubleArg", "GetDoubleArg"],
            cylinder.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetTransformArg", "GetVectorArg", "GetVectorArg", "GetDoubleArg", "GetDoubleArg", "GetBoolArg", "GetVectorArg", "GetVectorArg"],
            slot.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetVectorArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg"],
            pointCoordinate.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetDoubleArg", "GetDoubleArg", "GetDoubleArg"],
            cylindrical.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetDoubleArg", "GetDoubleArg", "GetDoubleArg"],
            polar.OutputArguments.Select(item => item.SdkBinding));
        Assert.Equal(["GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetToleranceVectorOptionsArg", "GetVectorArg"],
            pointProperties.OutputArguments.Select(item => item.SdkBinding));
    }

    [Fact]
    public void GeometryResultsPreserveTypesAndOutputOrder()
    {
        var vector = new WorkerVectorValue(1, 2, 3);
        var spline = GetBSplinePropertiesOperation.CreateResult(Completed(
            Output("Degree", new WorkerIntegerValue(3)),
            Output("Knots", new WorkerIntegerValue(8)),
            Output("Control Points", new WorkerIntegerValue(5)),
            Output("Range Min", new WorkerDoubleValue(0.1)),
            Output("Range Max", new WorkerDoubleValue(0.9)),
            Output("Length", new WorkerDoubleValue(7.5))));
        var cone = GetConePropertiesOperation.CreateResult(Completed(
            Output("Cone End Point (in working coordinates)", vector),
            Output("Cone Axis (in working coordinates)", new WorkerVectorValue(0, 0, 1)),
            Output("Cone Length", new WorkerDoubleValue(4)),
            Output("Cone Theta Start", new WorkerDoubleValue(10)),
            Output("Cone Theta Span", new WorkerDoubleValue(20)),
            Output("Cone Included Angle", new WorkerDoubleValue(30)),
            Output("Cut Length from Apex", new WorkerDoubleValue(2))));
        var line = GetLinePropertiesOperation.CreateResult(Completed(
            Output("Begin Coordinate", vector),
            Output("End Coordinate", new WorkerVectorValue(4, 5, 6)),
            Output("Delta Components", new WorkerVectorValue(3, 3, 3)),
            Output("Length", new WorkerDoubleValue(5.2)),
            Output("Angle about +X from +Y in YZ plane", new WorkerDoubleValue(1)),
            Output("Angle about +Y from +Z in XZ plane", new WorkerDoubleValue(2)),
            Output("Angle about +Z from +X in XY plane", new WorkerDoubleValue(3))));
        var plane = GetPlanePropertiesOperation.CreateResult(Completed(
            Output("Normal Direction", vector), Output("Point on Plane", new WorkerVectorValue(4, 5, 6)),
            Output("D Parameter", new WorkerDoubleValue(-2))));
        var surface = GetSurfacePhysicalStatsOperation.CreateResult(Completed(
            Output("Volume", new WorkerDoubleValue(100)), Output("Area", new WorkerDoubleValue(40))));
        var cylinder = GetCylinderPropertiesOperation.CreateResult(Completed(
            Output("Begin Coordinate", vector), Output("End Coordinate", new WorkerVectorValue(4, 5, 6)),
            Output("Axis Direction", new WorkerVectorValue(0, 0, 1)),
            Output("Length", new WorkerDoubleValue(8)), Output("Radius", new WorkerDoubleValue(2)),
            Output("Diameter", new WorkerDoubleValue(4)), Output("Nominals Point Inward", new WorkerBooleanValue(true)),
            Output("Facets", new WorkerIntegerValue(32)),
            Output("Enable Theta Extent Display Mode", new WorkerBooleanValue(false)),
            Output("Theta Start in Degrees", new WorkerDoubleValue(15)),
            Output("Theta Span in Degrees", new WorkerDoubleValue(270))));
        var slot = GetSlotPropertiesOperation.CreateResult(Completed(
            Output("Slot Transform (in working coordinates", new WorkerTransformValue(Enumerable.Range(1, 16).Select(x => (double)x).ToArray())),
            Output("Center (in working coordinates)", vector),
            Output("Normal Direction (in working coordinates)", new WorkerVectorValue(0, 0, 1)),
            Output("Slot Length", new WorkerDoubleValue(12)), Output("Slot Width", new WorkerDoubleValue(4)),
            Output("Round Slot Type", new WorkerBooleanValue(true)),
            Output("Centerline Pt. 1 (in working coordinates)", new WorkerVectorValue(1, 2, 3)),
            Output("Centerline Pt. 2 (in working coordinates)", new WorkerVectorValue(4, 5, 6))));
        var pointCoordinate = GetPointCoordinateOperation.CreateResult(Completed(
            Output("Vector Representation", vector), Output("X Value", new WorkerDoubleValue(1)),
            Output("Y Value", new WorkerDoubleValue(2)), Output("Z Value", new WorkerDoubleValue(3))));
        var cylindrical = GetPointCoordinateCylindricalOperation.CreateResult(Completed(
            Output("Radius Value", new WorkerDoubleValue(4)), Output("Theta Value", new WorkerDoubleValue(90)),
            Output("Z Value", new WorkerDoubleValue(3))));
        var polar = GetPointCoordinatePolarOperation.CreateResult(Completed(
            Output("Radius Value", new WorkerDoubleValue(5)), Output("Theta Value", new WorkerDoubleValue(60)),
            Output("Phi Value", new WorkerDoubleValue(30))));
        var pointProperties = GetPointPropertiesOperation.CreateResult(Completed(
            Output("Planar Offset", new WorkerDoubleValue(0.1)), Output("Radial Offset", new WorkerDoubleValue(0.2)),
            Output("Ux", new WorkerDoubleValue(0.3)), Output("Uy", new WorkerDoubleValue(0.4)),
            Output("Uz", new WorkerDoubleValue(0.5)), Output("Umag", new WorkerDoubleValue(0.6)),
            Output("Position Tolerance", new WorkerToleranceVectorOptionsValue(
                new(true, 1), new(true, 2), new(true, 3), new(true, 4),
                new(false, 0), new(false, 0), new(false, 0), new(false, 0))),
            Output("Component Weights", new WorkerVectorValue(1, 2, 3))));

        Assert.Equal(3, spline.Degree);
        Assert.Equal(8, spline.Knots);
        Assert.Equal(5, spline.ControlPoints);
        Assert.Equal(7.5, spline.Length);
        Assert.Equal(3, cone.ConeEndPoint.Z);
        Assert.Equal(30, cone.ConeIncludedAngle);
        Assert.Equal(2, cone.CutLengthFromApex);
        Assert.Equal(5.2, line.Length);
        Assert.Equal(3, line.AngleAboutZFromXInXyPlane);
        Assert.Equal(-2, plane.DParameter);
        Assert.Equal(100, surface.Volume);
        Assert.Equal(40, surface.Area);
        Assert.Equal(32, cylinder.Facets);
        Assert.True(cylinder.NominalsPointInward);
        Assert.Equal(270, cylinder.ThetaSpanInDegrees);
        Assert.Equal(16, slot.SlotTransform.Values.Count);
        Assert.Equal(12, slot.SlotLength);
        Assert.True(slot.RoundSlotType);
        Assert.Equal(1, pointCoordinate.VectorRepresentation.X);
        Assert.Equal(2, pointCoordinate.YValue);
        Assert.Equal(90, cylindrical.ThetaValue);
        Assert.Equal(3, cylindrical.ZValue);
        Assert.Equal(30, polar.PhiValue);
        Assert.Equal(0.6, pointProperties.Umag);
        Assert.True(pointProperties.PositionTolerance.HighX.Enabled);
        Assert.Equal(4, pointProperties.PositionTolerance.HighMagnitude.Value);
        Assert.Equal(3, pointProperties.ComponentWeights.Z);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedConeRoute()
    {
        var worker = new ConeWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .GetConePropertiesAsync(new()
            {
                ConeName = new Api.CollectionObjectName { ObjectName = "cone" }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(4, result.ConeLength);
        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(MigratedIds[1], Assert.Single(worker.Commands).OperationId);
        Assert.Equal(7, Assert.Single(worker.Commands).OutputArguments.Count);
    }

    private static WorkerRetrievedOutput Output(string name, WorkerMpValue value) => new(
        name, value switch
        {
            WorkerVectorValue => WorkerMpValueKind.Vector,
            WorkerIntegerValue => WorkerMpValueKind.WholeNumber,
            WorkerDoubleValue => WorkerMpValueKind.FloatingPoint,
            WorkerBooleanValue => WorkerMpValueKind.Logical,
            WorkerTransformValue => WorkerMpValueKind.Transform,
            WorkerToleranceVectorOptionsValue => WorkerMpValueKind.ToleranceVectorOptions,
            _ => throw new InvalidOperationException()
        }, value);

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class ConeWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Assert.Equal("Get Cone Properties", command.StepName);
            var outputs = new[]
            {
                Output("Cone End Point (in working coordinates)", new WorkerVectorValue(1, 2, 3)),
                Output("Cone Axis (in working coordinates)", new WorkerVectorValue(0, 0, 1)),
                Output("Cone Length", new WorkerDoubleValue(4)),
                Output("Cone Theta Start", new WorkerDoubleValue(10)),
                Output("Cone Theta Span", new WorkerDoubleValue(20)),
                Output("Cone Included Angle", new WorkerDoubleValue(30)),
                Output("Cut Length from Apex", new WorkerDoubleValue(2))
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
