using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedSpecialSurfaceMigrationTests
{
    [Fact]
    public async Task GeneratedClientRetrievesGeometryAndNestedGradientThroughTypedMappings()
    {
        var worker = new RecordingWorker();
        var host = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var lifetime = host.ConfigureAwait(true);
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(host.Channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var geometry = await client.ConstructGeometryFromSurfacesAsync(new()
        {
            Surfaces = { Object("surface") }
        }, options);
        var gradient = await client.GetGradientAtProjectedPointOnSurfaceAsync(new()
        {
            PointToProject = Point("source"), SurfaceName = Object("surface")
        }, options);
        Assert.Equal(Api.ObjectType.Cylinder, Assert.Single(geometry.GeometryObjects).ObjectType);
        Assert.Equal(1, gradient.Gradient.ProjectedPoint.X);
        Assert.Equal(6, gradient.Gradient.NormalVector.Z);
        Assert.Equal(10, gradient.Gradient.VDirection.X);
        Assert.Equal(Api.MpExecutionState.Succeeded, geometry.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, gradient.Execution.State);
        Assert.Equal(2, worker.Commands.Count);
        Assert.Equal(["Surfaces", "Minimum Diameter", "Maximum Diameter", "Base Name"],
            worker.Commands[0].InputArguments.Select(argument => argument.Name));
        Assert.Equal(WorkerObjectTypeValue.Surface,
            worker.Commands[0].InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.Equal("Geometry Object", worker.Commands[0].InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(4, worker.Commands[1].OutputArguments.Count);
        Assert.All(worker.Commands[1].OutputArguments, argument => Assert.Equal("GetVectorArg", argument.SdkBinding));

        var missing = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructGeometryFromSurfacesAsync(new(), options));
        Assert.Equal(StatusCode.InvalidArgument, missing.StatusCode);
        Assert.Equal(2, worker.Commands.Count);
    }

    [Fact]
    public void OptionalGeometryBindingsAndOtherSpecialInputsPreserveTargetContract()
    {
        var geometry = ConstructGeometryFromSurfacesOperation.CreateCommand(new()
        {
            Surfaces = { Object("surface") }, ReferenceFrame = Object("frame"),
            DestinationCollectionName = new() { Name = "derived" }, BaseName = "fit"
        });
        Assert.Equal(["Surfaces", "Minimum Diameter", "Maximum Diameter", "Reference Frame",
            "Destination Collection Name", "Base Name"], geometry.InputArguments.Select(argument => argument.Name));
        Assert.Equal(WorkerObjectTypeValue.Frame,
            geometry.InputArguments[3].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetCollectionNameArg", geometry.InputArguments[4].SdkBinding);

        var polygon = ConstructPolygonizedSurfaceFromPointCloudsOperation.CreateCommand(new()
        {
            PointCloudList = { Object("cloud") }, MeshOrientation = Api.MeshOrientationType.UseCurrentWorkingFrame,
            PolygonizedSurfaceName = Object("mesh")
        });
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            polygon.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.Equal("Use Current Working Frame", polygon.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, polygon.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.ScanStripeMesh,
            polygon.InputArguments[3].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Throws<ArgumentException>(() => ConstructPolygonizedSurfaceFromPointCloudsOperation.CreateCommand(new()
        {
            PointCloudList = { Object("cloud") }, PolygonizedSurfaceName = Object("mesh")
        }));

        var edge = GetGradientAtProjectedPointOnSurfaceEdgeOperation.CreateCommand(new()
        {
            PointToProject = Point("source"), SurfaceEdge = Object("edge"), SurfaceName = Object("surface"),
            EdgeOffsetDirection = new() { X = 1, Y = 0, Z = 0 }
        });
        Assert.Equal(WorkerObjectTypeValue.BSpline,
            edge.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetVectorArg", edge.InputArguments[3].SdkBinding);
        Assert.Equal(0.01, edge.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Throws<ArgumentException>(() => GetGradientAtProjectedPointOnSurfaceEdgeOperation.CreateCommand(new()
        {
            PointToProject = Point("source"), SurfaceEdge = Object("edge"), SurfaceName = Object("surface")
        }));
        Assert.Throws<ArgumentException>(() => GetGradientAtProjectedPointOnSurfaceOperation.CreateCommand(new()
        {
            PointToProject = Point("source")
        }));

        var selected = ConstructObjectsFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new()
        {
            ObjectType = Api.ConstructObjectType.CenterPoints
        });
        Assert.Equal(["Object Type", "Point Offset"], selected.InputArguments.Select(argument => argument.Name));
        Assert.Equal("Center Points", selected.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0, selected.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Throws<ArgumentException>(() => ConstructObjectsFromSurfaceFacesRuntimeSelectOperation.CreateCommand(new()));

        OperationDescriptor[] descriptors =
        [
            ConstructGeometryFromSurfacesOperation.Descriptor,
            ConstructObjectsFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructPolygonizedSurfaceFromPointCloudsOperation.Descriptor,
            GetGradientAtProjectedPointOnSurfaceOperation.Descriptor,
            GetGradientAtProjectedPointOnSurfaceEdgeOperation.Descriptor
        ];
        foreach (var descriptor in descriptors)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == descriptor.OperationId);
        }
        Assert.Equal(Api.ReplaySafety.Safe, GetGradientAtProjectedPointOnSurfaceOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Unsafe, ConstructGeometryFromSurfacesOperation.Descriptor.ReplaySafety);
    }

    private static Api.CollectionObjectName Object(string name) => new() { CollectionName = "part", ObjectName = name };
    private static Api.PointName Point(string name) => new() { CollectionName = "part", GroupName = "points", TargetName = name };

    private sealed class RecordingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];
        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.construct_geometry_from_surfaces" =>
                [new WorkerRetrievedOutput("Geometry Objects", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("part", "derived", WorkerObjectTypeValue.Cylinder)]))],
                "construction_operations.get_gradient_at_projected_point_on_surface" =>
                [
                    new WorkerRetrievedOutput("Projected Point", WorkerMpValueKind.Vector, new WorkerVectorValue(1, 2, 3)),
                    new WorkerRetrievedOutput("Normal Vector", WorkerMpValueKind.Vector, new WorkerVectorValue(4, 5, 6)),
                    new WorkerRetrievedOutput("U Direction", WorkerMpValueKind.Vector, new WorkerVectorValue(7, 8, 9)),
                    new WorkerRetrievedOutput("V Direction", WorkerMpValueKind.Vector, new WorkerVectorValue(10, 11, 12))
                ],
                _ => []
            };
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
