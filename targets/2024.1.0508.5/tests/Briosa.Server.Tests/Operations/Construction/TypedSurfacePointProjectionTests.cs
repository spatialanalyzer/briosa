using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSurfacePointProjectionTests
{
    [Fact]
    public async Task GeneratedClientExecutesTypedProjectionAndUvGridRoutes()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var lifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var projected = await client.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisAsync(new()
        {
            SurfaceList = { Object("surface") }, PointNames = { Point("source") }, Axis = Api.WcfAxis.Y
        }, options);
        var grid = await client.ConstructPointsFromSurfacesOnUvGridAsync(new()
        {
            SurfaceList = { Object("surface") }, EdgePointMode = Api.EdgePointMode.ExcludeEdges
        }, options);
        Assert.Equal(Api.MpExecutionState.Succeeded, projected.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, grid.Execution.State);
        Assert.Equal(2, worker.Commands.Count);
        Assert.Equal(WorkerAxisIdentifierValue.PositiveY,
            worker.Commands[0].InputArguments[5].RequireValue<WorkerChoiceValue<WorkerAxisIdentifierValue>>().Value);
        Assert.Equal("SetAxisNameArg", worker.Commands[0].InputArguments[5].SdkBinding);
        Assert.Equal(5, worker.Commands[1].InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(5, worker.Commands[1].InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerEdgeModeValue.ExcludeEdges,
            worker.Commands[1].InputArguments[5].RequireValue<WorkerChoiceValue<WorkerEdgeModeValue>>().Value);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisAsync(new()
            {
                SurfaceList = { Object("surface") }, PointNames = { Point("source") }
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(2, worker.Commands.Count);
    }

    [Fact]
    public void EachRemainingPointRouteKeepsExactInputsDefaultsAndRegistration()
    {
        var intersect = ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation.CreateCommand(new()
        {
            AxisObjectList = { Object("axis") }, SurfaceList = { Object("surface") },
            ResultantGroupName = Object("result")
        });
        Assert.Equal(["Axis Object List", "Surface List", "Resultant Group Name"],
            intersect.InputArguments.Select(argument => argument.Name));
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            intersect.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Throws<ArgumentException>(() => ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation.CreateCommand(new()
        {
            AxisObjectList = { Object("axis") }, ResultantGroupName = Object("result")
        }));

        var radial = ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisOperation.CreateCommand(new()
        {
            SurfaceList = { Object("surface") }, PointNames = { Point("source") }, Axis = Api.WcfAxis.Z
        });
        Assert.Equal("Construct Points at Projection on Surfaces - Radial from WCF Axis", radial.StepName);
        Assert.Equal(WorkerAxisIdentifierValue.PositiveZ,
            radial.InputArguments[5].RequireValue<WorkerChoiceValue<WorkerAxisIdentifierValue>>().Value);
        Assert.All(radial.InputArguments.Skip(2).Take(3), argument =>
            Assert.Equal(string.Empty, argument.RequireValue<WorkerTextValue>().Value));
        Assert.Throws<ArgumentException>(() => ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisOperation.CreateCommand(new()
        {
            SurfaceList = { Object("surface") }, PointNames = { Point("source") }
        }));

        var spherical = ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation.CreateCommand(new()
        {
            SurfaceList = { Object("surface") }, PointNames = { Point("source") }
        });
        Assert.Equal("Construct Points at Projection on Surfaces - Spherical from WCF Origin", spherical.StepName);
        Assert.Equal(5, spherical.InputArguments.Count);
        Assert.All(spherical.InputArguments.Skip(2), argument =>
            Assert.Equal(string.Empty, argument.RequireValue<WorkerTextValue>().Value));
        Assert.Throws<ArgumentException>(() => ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation.CreateCommand(new()
        {
            SurfaceList = { Object("surface") }
        }));

        Assert.Throws<ArgumentException>(() => ConstructPointsFromSurfacesOnUvGridOperation.CreateCommand(new()
        {
            SurfaceList = { Object("surface") }
        }));

        var cylinder = ConstructPointsFromCylinderOperation.CreateCommand(new()
        {
            CylinderName = Object("cylinder"), GroupName = Object("group")
        });
        Assert.Equal(WorkerObjectTypeValue.Cylinder,
            cylinder.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            cylinder.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Throws<ArgumentException>(() => ConstructPointsFromCylinderOperation.CreateCommand(new()
        {
            CylinderName = Object("cylinder")
        }));

        var selector = ConstructPointsFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new());
        Assert.Equal("Construct Points From Surface Faces - Runtime Select", selector.StepName);
        Assert.Empty(selector.InputArguments);
        Assert.Empty(selector.OutputArguments);

        var clicking = ConstructPointsOnSurfacesByClickingOperation.CreateCommand(new()
        {
            GroupNameForPoints = Object("group")
        });
        Assert.Equal("p0", clicking.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => ConstructPointsOnSurfacesByClickingOperation.CreateCommand(new()));

        var shifted = ShiftPlaneOperation.CreateCommand(new() { Plane = Object("plane") });
        Assert.Equal(["Plane", "Shift Along Normal", "Grow Bounds by Factor"],
            shifted.InputArguments.Select(argument => argument.Name));
        Assert.All(shifted.InputArguments.Skip(1), argument =>
            Assert.Equal(0, argument.RequireValue<WorkerDoubleValue>().Value));
        Assert.Throws<ArgumentException>(() => ShiftPlaneOperation.CreateCommand(new()));

        OperationDescriptor[] descriptors =
        [
            ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation.Descriptor,
            ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisOperation.Descriptor,
            ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisOperation.Descriptor,
            ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation.Descriptor,
            ConstructPointsFromCylinderOperation.Descriptor,
            ConstructPointsFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructPointsFromSurfacesOnUvGridOperation.Descriptor,
            ConstructPointsOnSurfacesByClickingOperation.Descriptor,
            ShiftPlaneOperation.Descriptor
        ];
        foreach (var descriptor in descriptors)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == descriptor.OperationId);
            Assert.Equal(Api.ReplaySafety.Unsafe, descriptor.ReplaySafety);
        }
    }

    private static Api.CollectionObjectName Object(string name) => new() { CollectionName = "part", ObjectName = name };
    private static Api.PointName Point(string name) => new() { CollectionName = "part", GroupName = "source", TargetName = name };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];
        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
