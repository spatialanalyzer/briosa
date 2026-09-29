using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSurfacePrimitiveMigrationTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedCurvePrimitiveAndSelectionMappings()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var hostLifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var spline = await client.ConstructBSplineFromIntersectionOfPlaneAndSurfaceAsync(new()
        {
            ResultingBSplineName = Object("curve"), PlaneName = Object("plane"), SurfaceName = Object("surface")
        }, options);
        var splines = await client.ConstructBSplinesFromSurfacesAsync(new()
        {
            SurfaceList = { Object("surface") }
        }, options);
        var circles = await client.ConstructCirclesLinesFromSurfacesAsync(new()
        {
            Surfaces = { Object("surface") }, CircleLineMode = Api.CircleLineMode.Circle,
            DestinationCollectionName = new() { Name = "derived" }
        }, options);
        var selected = await client.ConstructCylindersFromSurfaceFacesRuntimeSelectAsync(new(), options);

        Assert.All(new[] { spline.Execution, splines.Execution, circles.Execution, selected.Execution },
            detail => Assert.Equal(Api.MpExecutionState.Succeeded, detail.State));
        Assert.Equal(Api.ObjectType.BSpline, Assert.Single(splines.BSplineList).ObjectType);
        Assert.Equal("result", Assert.Single(circles.GeometryObjects).ObjectName);
        Assert.Equal(4, worker.Commands.Count);
        Assert.Equal(WorkerObjectTypeValue.BSpline,
            worker.Commands[0].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane,
            worker.Commands[0].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(0.0001, worker.Commands[0].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Single(worker.Commands[1].InputArguments);
        Assert.Equal("SetCollectionObjectNameRefListArg", worker.Commands[1].InputArguments[0].SdkBinding);
        Assert.Equal("Circle", worker.Commands[2].InputArguments[5].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0.02, worker.Commands[2].InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Empty(worker.Commands[3].InputArguments);

        var missingSurface = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructBSplinesFromSurfacesAsync(new(), options));
        var invalidMode = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructCirclesLinesFromSurfacesAsync(new()
            {
                Surfaces = { Object("surface") }, DestinationCollectionName = new() { Name = "derived" }
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingSurface.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, invalidMode.StatusCode);
        Assert.Equal(4, worker.Commands.Count);
    }

    [Fact]
    public void PrimitiveMappingsPreserveDefaultsAndTypedRegistration()
    {
        var line = ConstructLineFromInstrumentShotOperation.CreateCommand(new()
        {
            PointName = new() { CollectionName = "part", GroupName = "probe", TargetName = "p1" },
            LineName = Object("axis")
        });
        Assert.Equal(0, line.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Line,
            line.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        var bounding = ConstructPlanesBoundingPointGroupOperation.CreateCommand(new()
        {
            ReferencePlaneName = Object("datum"), GroupToBound = Object("points"),
            ResultingHighPlaneName = Object("high"), ResultingLowPlaneName = Object("low")
        });
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            bounding.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(bounding.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, bounding.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);

        OperationDescriptor[] descriptors =
        [
            ConstructBSplineFromIntersectionOfPlaneAndSurfaceOperation.Descriptor,
            ConstructBSplineFromIntersectionOfSurfacesOperation.Descriptor,
            ConstructBSplinesFromSurfacesOperation.Descriptor,
            ConstructCirclesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructCirclesLinesFromSurfacesOperation.Descriptor,
            ConstructConesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructCylindersFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructLineFromInstrumentShotOperation.Descriptor,
            ConstructLinesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructPlaneNormalToObjectThroughPointOperation.Descriptor,
            ConstructPlanesBisectTwoPlanesOperation.Descriptor,
            ConstructPlanesBoundingPointGroupOperation.Descriptor,
            ConstructPlanesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructSpheresFromSurfaceFacesRuntimeSelectOperation.Descriptor
        ];
        foreach (var descriptor in descriptors)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == descriptor.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, descriptor.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
        }
    }

    [Fact]
    public void RemainingSplineAndPlaneCommandsPreserveExactBindingsAndRejectMissingGeometry()
    {
        var twoSurfaces = ConstructBSplineFromIntersectionOfSurfacesOperation.CreateCommand(new()
        {
            ResultingBSplineName = Object("intersection"), FirstSurfaceName = Object("left"),
            SecondSurfaceName = Object("right"), ApproximationTolerance = 0.125
        });
        Assert.Equal("Construct B-Spline From Intersection of Surfaces", twoSurfaces.StepName);
        Assert.Equal(["Resulting B-Spline Name", "First Surface Name", "Second Surface Name", "Approximation Tolerance"],
            twoSurfaces.InputArguments.Select(argument => argument.Name));
        Assert.All(twoSurfaces.InputArguments.Take(3), argument => Assert.Equal("SetCollectionObjectNameArg2", argument.SdkBinding));
        Assert.Equal(WorkerObjectTypeValue.BSpline,
            twoSurfaces.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.All(twoSurfaces.InputArguments.Skip(1).Take(2), argument =>
            Assert.Equal(WorkerObjectTypeValue.Surface, argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectType));
        Assert.Equal(0.125, twoSurfaces.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Throws<ArgumentException>(() => ConstructBSplineFromIntersectionOfSurfacesOperation.CreateCommand(new()
        {
            ResultingBSplineName = Object("intersection"), FirstSurfaceName = Object("left")
        }));

        var normal = ConstructPlaneNormalToObjectThroughPointOperation.CreateCommand(new()
        {
            ResultantPlaneName = Object("normal"), NormalToObjectName = Object("source"),
            ThroughPointName = new() { CollectionName = "part", GroupName = "points", TargetName = "p1" }
        });
        Assert.Equal(["Resultant Plane Name", "'Normal to' Object Name", "'Through' Point Name", "Plane Edge Dimension"],
            normal.InputArguments.Select(argument => argument.Name));
        Assert.Equal("SetPointNameArg", normal.InputArguments[2].SdkBinding);
        Assert.Equal(0, normal.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Throws<ArgumentException>(() => ConstructPlaneNormalToObjectThroughPointOperation.CreateCommand(new()
        {
            ResultantPlaneName = Object("normal"), NormalToObjectName = Object("source")
        }));

        var bisected = ConstructPlanesBisectTwoPlanesOperation.CreateCommand(new()
        {
            ResultantPlaneName = Object("middle"), FirstPlane = Object("left"), SecondPlane = Object("right")
        });
        Assert.Equal(["Resultant Plane Name", "First Plane", "Second Plane"],
            bisected.InputArguments.Select(argument => argument.Name));
        Assert.All(bisected.InputArguments, argument =>
        {
            Assert.Equal("SetCollectionObjectNameArg2", argument.SdkBinding);
            Assert.Equal(WorkerObjectTypeValue.Plane, argument.RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        });
        Assert.Throws<ArgumentException>(() => ConstructPlanesBisectTwoPlanesOperation.CreateCommand(new()
        {
            ResultantPlaneName = Object("middle"), FirstPlane = Object("left")
        }));
    }

    [Fact]
    public void SurfaceFaceSelectorsRetainDistinctStepsAndNoSdkArguments()
    {
        WorkerMpCommand[] commands =
        [
            ConstructCirclesFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new()),
            ConstructConesFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new()),
            ConstructCylindersFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new()),
            ConstructLinesFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new()),
            ConstructPlanesFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new()),
            ConstructSpheresFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new())
        ];
        Assert.Equal(
            ["Construct Circles From Surface Faces - Runtime Select", "Construct Cones From Surface Faces - Runtime Select",
             "Construct Cylinders From Surface Faces - Runtime Select", "Construct Lines From Surface Faces - Runtime Select",
             "Construct Planes From Surface Faces - Runtime Select", "Construct Spheres From Surface Faces - Runtime Select"],
            commands.Select(command => command.StepName));
        Assert.Equal(6, commands.Select(command => command.OperationId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(commands, command =>
        {
            Assert.Empty(command.InputArguments);
            Assert.Empty(command.OutputArguments);
        });
    }

    private static Api.CollectionObjectName Object(string name) =>
        new() { CollectionName = "part", ObjectName = name };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.construct_b_splines_from_surfaces" =>
                [new WorkerRetrievedOutput("B-Spline List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("part", "spline", WorkerObjectTypeValue.BSpline)]))],
                "construction_operations.construct_circles_lines_from_surfaces" =>
                [new WorkerRetrievedOutput("Geometry Objects", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("part", "result", WorkerObjectTypeValue.Circle)]))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
