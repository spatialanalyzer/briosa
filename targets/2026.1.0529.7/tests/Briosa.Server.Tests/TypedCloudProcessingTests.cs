using Briosa.Server.Operations;
using Briosa.Server.Operations.CloudAndMeshOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Tests retain xUnit synchronization behavior.")]
public sealed class TypedCloudProcessingTests
{
    private static readonly string[] DestructiveRiskFlags = ["destructive"];
    private static bool HasTargetOnlyVectorGroupFilterArgument =>
        string.Equals(SpatialAnalyzerApi.TargetVersion, "2026.1.0529.7", StringComparison.Ordinal);

    [Fact]
    public async Task GeneratedClientMapsInspectionFilteringAndMeshGenerationOperations()
    {
        var worker = new CloudProcessingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var cloud = Object("Scans", "CloudA");
        var clouds = new[] { cloud };
        var plane = Object("Geometry", "PlaneA");
        var surface = Object("Geometry", "SurfaceA");
        var group = Object("Results", "GroupA");
        var spline = Object("Geometry", "SplineA");
        var pointA = new Api.PointName { TargetName = "P1" };
        var pointB = new Api.PointName { TargetName = "P2" };

        var raster = await client.RasterScanEdgeInspectionAsync(new()
        {
            CloudNames = { clouds },
            EdgeSurfaceName = surface,
            BSplineEdgeList = { spline },
            PrefixForOutputGroups = group
        }, options);
        await client.NewRasterScanEdgeInspectionAsync(new()
        {
            EdgeCloudNames = { clouds },
            EdgeSurfaceName = surface,
            EdgeBSplineName = spline,
            OutputPrefix = group
        }, options);
        await client.NewRasterScanEdgeInspectionAsync(new()
        {
            EdgeCloudNames = { clouds },
            EdgeSurfaceName = surface,
            EdgeBSplineName = spline,
            OutputPrefix = group,
            IntermediateCalculationResultsFile = new() { Path = "C:\\temp\\inspection.json" }
        }, options);
        await client.FilterCloudsToPlaneAsync(new()
        {
            CloudNames = { clouds }, FilterPlaneName = plane, OutputGroupName = group
        }, options);
        await client.FilterCloudsToGroupAsync(new()
        {
            CloudNames = { clouds }, FilterGroupName = group, OutputGroupName = Object("Results", "ByGroup"),
            OutputType = Api.PointOutputType.CloudPoints
        }, options);
        await client.FilterCloudsToSurfaceAsync(new()
        {
            CloudNames = { clouds }, FilterSurfaceName = surface, OutputGroupName = Object("Results", "BySurface")
        }, options);
        await client.FilterCloudsToBSplinesAsync(new()
        {
            CloudNames = { clouds }, FilterBSplineNames = { spline }, OutputGroupName = Object("Results", "BySpline")
        }, options);
        await client.FilterCloudsToLineSegmentAsync(new()
        {
            CloudNames = { clouds }, FirstLineEndPoint = pointA, SecondLineEndPoint = pointB,
            OutputGroupName = Object("Results", "ByLine")
        }, options);
        await client.FilterCloudsToVectorGroupsResolvePointsAsync(new()
        {
            CloudNames = { clouds }, VectorGroupNames = { Object("Vectors", "VG1") },
            OutputGroupName = Object("Results", "ByVectors")
        }, options);
        await client.RGBCloudPointFilterAsync(new() { CloudsToBeFiltered = { clouds } }, options);
        await client.RGBCloudPointFilterAsync(new()
        {
            FilterName = "Blue Selection",
            CloudsToBeFiltered = { clouds },
            RedEnabled = false,
            RgbFilterOperation = Api.RGBFilterOperation.IncrementallyApplyFilter
        }, options);
        await client.DeleteCloudPointsByRadialDistanceFromPointsAsync(new()
        {
            CloudNames = { clouds }, Points = { pointA }
        }, options);
        await client.DeleteCloudPointsByXYZRangeAsync(new() { CloudNames = { clouds } }, options);
        await client.GenerateGeneralMeshAsync(new()
        {
            OutputMeshName = Object("Meshes", "MeshA"), CloudsToMesh = { clouds }
        }, options);
        await client.GenerateGeneralMeshAsync(new()
        {
            OutputMeshName = Object("Meshes", "MeshB"), CloudsToMesh = { clouds },
            JsonFile = new() { Path = "C:\\temp\\mesh.json" }
        }, options);

        Assert.Equal("Inspection completed", raster.SummaryResult);
        Assert.Equal(15, worker.Commands.Count);
        Assert.Equal(7, worker.Commands[0].InputArguments.Count);
        Assert.Equal(0, worker.Commands[0].InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, worker.Commands[0].InputArguments[5].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0, worker.Commands[0].InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Single(worker.Commands[0].OutputArguments);

        Assert.Equal(10, worker.Commands[1].InputArguments.Count);
        Assert.Equal(0, worker.Commands[1].InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(worker.Commands[1].InputArguments[8].RequireValue<WorkerBooleanValue>().Value);
        Assert.DoesNotContain(worker.Commands[1].InputArguments,
            argument => argument.Kind == WorkerMpValueKind.FileReference);
        Assert.Equal("C:\\temp\\inspection.json",
            worker.Commands[2].InputArguments[10].RequireValue<WorkerFileReferenceValue>().Path);

        Assert.Equal("Both", worker.Commands[3].InputArguments[4].RequireValue<WorkerChoiceValue<WorkerOffsetDirectionTypeValue>>().Value.ToString());
        Assert.Equal("Points", worker.Commands[3].InputArguments[5].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Cloud Points", worker.Commands[4].InputArguments[5].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.BSpline,
            worker.Commands[6].InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            worker.Commands[7].InputArguments[3].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(HasTargetOnlyVectorGroupFilterArgument ? 9 : 8,
            worker.Commands[8].InputArguments.Count);
        if (HasTargetOnlyVectorGroupFilterArgument)
            Assert.False(worker.Commands[8].InputArguments[8].RequireValue<WorkerBooleanValue>().Value);

        var rgb = worker.Commands[9];
        Assert.Equal(23, rgb.InputArguments.Count);
        Assert.Equal("Default Filter", rgb.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.True(rgb.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(255, rgb.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("Reset and Apply Filter", rgb.InputArguments[22].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Blue Selection", worker.Commands[10].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.False(worker.Commands[10].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("Incrementally Apply Filter", worker.Commands[10].InputArguments[22].RequireValue<WorkerTextValue>().Value);

        Assert.Equal(0, worker.Commands[11].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(worker.Commands[11].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(["Cloud Names", "Delete Inside"], worker.Commands[12].InputArguments.Select(argument => argument.Name));
        Assert.Equal(6, worker.Commands[13].InputArguments.Count);
        Assert.True(worker.Commands[13].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[13].InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("C:\\temp\\mesh.json", worker.Commands[14].InputArguments[6].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task MissingRequiredReferencesAndUnsupportedFilterEnumsAreRejected()
    {
        var worker = new CloudProcessingWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var cloud = Object("Scans", "CloudA");

        await AssertInvalid(() => client.RasterScanEdgeInspectionAsync(new(), options).ResponseAsync);
        await AssertInvalid(() => client.FilterCloudsToPlaneAsync(new()
        {
            CloudNames = { cloud }, FilterPlaneName = Object("Geometry", "P"), OutputGroupName = Object("Results", "G"),
            OutputType = Api.PointOutputType.Unspecified
        }, options).ResponseAsync);
        await AssertInvalid(() => client.FilterCloudsToPlaneAsync(new()
        {
            CloudNames = { cloud }, FilterPlaneName = Object("Geometry", "P"), OutputGroupName = Object("Results", "G"),
            AllowableOffsetDirection = (Api.OffsetDirectionType)99
        }, options).ResponseAsync);
        await AssertInvalid(() => client.RGBCloudPointFilterAsync(new(), options).ResponseAsync);
        await AssertInvalid(() => client.RGBCloudPointFilterAsync(new()
        {
            CloudsToBeFiltered = { cloud }, RgbFilterOperation = Api.RGBFilterOperation.Unspecified
        }, options).ResponseAsync);
        await AssertInvalid(() => client.DeleteCloudPointsByRadialDistanceFromPointsAsync(new()
        {
            CloudNames = { cloud }
        }, options).ResponseAsync);
        await AssertInvalid(() => client.GenerateGeneralMeshAsync(new()
        {
            OutputMeshName = Object("Meshes", "M")
        }, options).ResponseAsync);
        Assert.Empty(worker.Commands);
    }

    [Fact]
    public void ProcessingOperationsAreTypedAndCloudPointDeletionIsMarkedDestructive()
    {
        var operations = new[]
        {
            RasterScanEdgeInspectionOperation.Descriptor,
            NewRasterScanEdgeInspectionOperation.Descriptor,
            FilterCloudsToPlaneOperation.Descriptor,
            FilterCloudsToGroupOperation.Descriptor,
            FilterCloudsToSurfaceOperation.Descriptor,
            FilterCloudsToBSplinesOperation.Descriptor,
            FilterCloudsToLineSegmentOperation.Descriptor,
            FilterCloudsToVectorGroupsResolvePointsOperation.Descriptor,
            RGBCloudPointFilterOperation.Descriptor,
            DeleteCloudPointsByRadialDistanceFromPointsOperation.Descriptor,
            DeleteCloudPointsByXYZRangeOperation.Descriptor,
            GenerateGeneralMeshOperation.Descriptor
        };

        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, candidate => candidate.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
        }

        Assert.Equal(DestructiveRiskFlags, DeleteCloudPointsByRadialDistanceFromPointsOperation.Descriptor.RiskFlags);
        Assert.Equal(DestructiveRiskFlags, DeleteCloudPointsByXYZRangeOperation.Descriptor.RiskFlags);
    }



    private static async Task AssertInvalid(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(action).ConfigureAwait(false);
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private sealed class CloudProcessingWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId is
                "cloud_and_mesh_operations.raster_scan_edge_inspection" or
                "cloud_and_mesh_operations.new_raster_scan_edge_inspection"
                ? [new WorkerRetrievedOutput("Summary Result", WorkerMpValueKind.Text, new WorkerTextValue("Inspection completed"))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
