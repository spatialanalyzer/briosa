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
public sealed class TypedCloudAndMeshSettingsQueryTests
{
    [Fact]
    public async Task GeneratedClientMapsCloudAndMeshSettingsAndQueriesWithDefaultsAndOutputs()
    {
        var worker = new CloudAndMeshWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var cloud = Object("Scans", "CloudA");
        var crossSectionCloud = Object("Results", "Sections");

        await client.CloudDisplayControlAsync(new(), options);
        await client.CloudDisplayControlAsync(new() { Thin = 4, PointSize = 7 }, options);
        var defaultBounds = await client.ResetCloudBoundingBoxAsync(new() { CloudName = cloud }, options);
        var configuredBounds = await client.ResetCloudBoundingBoxAsync(new()
        {
            CloudName = cloud,
            CloudBoxType = Api.CloudBoxType.MinimumOrientedBoxUnconditional,
            ShowBoundingBox = false,
            UseAllPoints = true,
            DesiredPointCount = 250
        }, options);
        var pointCount = await client.GetCloudPointCountAsync(new() { CloudName = cloud }, options);
        await client.SetCloudDefaultClippingPlaneAsync(new(), options);
        await client.SetCloudDefaultClippingPlaneAsync(new()
        {
            EnableCloudClipping = true,
            ReferenceObject = Object("Geometry", "PlaneA")
        }, options);
        await client.EnableAllCloudCrossSectionsAsync(new() { CrossSectionCloudName = crossSectionCloud }, options);
        await client.EnableDisableCloudCrossSectionsAsync(new() { CrossSectionCloudName = crossSectionCloud }, options);
        await client.EnableDisableCloudCrossSectionsAsync(new()
        {
            CrossSectionCloudName = crossSectionCloud,
            CrossSectionId = 4,
            Enable = false
        }, options);
        await client.EnableSingleCloudCrossSectionAsync(new() { CrossSectionCloudName = crossSectionCloud }, options);
        await client.EnableSingleCloudCrossSectionAsync(new()
        {
            CrossSectionCloudName = crossSectionCloud,
            CrossSectionId = 5
        }, options);
        var crossSectionCount = await client.GetNumberOfCrossSectionsInCrossSectionCloudAsync(
            new() { CrossSectionCloudName = crossSectionCloud }, options);
        var volume = await client.MeshVolumeAsync(new()
        {
            Mesh = Object("Meshes", "ScanStripe"),
            Plane = Object("Geometry", "CutPlane")
        }, options);

        Assert.Equal(1, defaultBounds.PointsUsedForBoundingBox);
        Assert.Equal(1, defaultBounds.XAxisInWorld.X);
        Assert.Equal(16, defaultBounds.ReferenceTransformInWorld.Values.Count);
        Assert.Equal(1, configuredBounds.PointsUsedForBoundingBox);
        Assert.Equal(12, pointCount.PointsCount);
        Assert.Equal(1.5, pointCount.PlanarOffset);
        Assert.Equal(2.5, pointCount.RadialOffset);
        Assert.Equal(3, pointCount.ActiveClippingPlanes);
        Assert.Equal(6, crossSectionCount.CrossSectionCount);
        Assert.Equal(0.75, volume.Above);
        Assert.Equal(1.25, volume.Below);
        Assert.Equal(14, worker.Commands.Count);

        var display = worker.Commands[0];
        Assert.Equal(1, display.InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(1, display.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(4, worker.Commands[1].InputArguments[0].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(7, worker.Commands[1].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);

        var bounds = worker.Commands[2];
        Assert.Equal("SetCollectionObjectNameArg2", bounds.InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            bounds.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("World Axis Aligned Box", bounds.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.True(bounds.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(bounds.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(1000, bounds.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("Minimum Oriented Box - Unconditional",
            worker.Commands[3].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(worker.Commands[3].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(worker.Commands[3].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(250, worker.Commands[3].InputArguments[4].RequireValue<WorkerIntegerValue>().Value);

        var disabledByDefault = Assert.Single(worker.Commands[5].InputArguments);
        Assert.False(disabledByDefault.RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[6].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.CrossSectionCloud,
            worker.Commands[7].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(0, worker.Commands[8].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(worker.Commands[8].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(4, worker.Commands[9].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(worker.Commands[9].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0, worker.Commands[10].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(5, worker.Commands[11].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.ScanStripeMesh,
            worker.Commands[13].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane,
            worker.Commands[13].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
    }

    [Fact]
    public async Task InvalidRequiredInputsAndUnknownEnumValuesAreRejectedBeforeWorkerExecution()
    {
        var worker = new CloudAndMeshWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await AssertInvalid(() => client.GetCloudPointCountAsync(new(), options).ResponseAsync);
        await AssertInvalid(() => client.ResetCloudBoundingBoxAsync(new(), options).ResponseAsync);
        await AssertInvalid(() => client.ResetCloudBoundingBoxAsync(new()
        {
            CloudName = Object("Scans", "CloudA"),
            CloudBoxType = (Api.CloudBoxType)99
        }, options).ResponseAsync);
        await AssertInvalid(() => client.SetCloudDefaultClippingPlaneAsync(new()
        {
            EnableCloudClipping = true
        }, options).ResponseAsync);
        await AssertInvalid(() => client.MeshVolumeAsync(new(), options).ResponseAsync);
        Assert.Empty(worker.Commands);
    }

    [Fact]
    public void CloudAndMeshSettingsAndQueryOperationsAreRegisteredAsTypedOperations()
    {
        var typed = new[]
        {
            CloudDisplayControlOperation.Descriptor,
            ResetCloudBoundingBoxOperation.Descriptor,
            GetCloudPointCountOperation.Descriptor,
            SetCloudDefaultClippingPlaneOperation.Descriptor,
            EnableAllCloudCrossSectionsOperation.Descriptor,
            EnableDisableCloudCrossSectionsOperation.Descriptor,
            EnableSingleCloudCrossSectionOperation.Descriptor,
            GetNumberOfCrossSectionsInCrossSectionCloudOperation.Descriptor,
            MeshVolumeOperation.Descriptor
        };
        var readOnly = new HashSet<string>(StringComparer.Ordinal)
        {
            GetCloudPointCountOperation.Descriptor.OperationId,
            GetNumberOfCrossSectionsInCrossSectionCloudOperation.Descriptor.OperationId,
            MeshVolumeOperation.Descriptor.OperationId
        };

        foreach (var operation in typed)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, candidate => candidate.OperationId == operation.OperationId);
            Assert.Equal(readOnly.Contains(operation.OperationId)
                    ? Api.OperationExecutionScope.GlobalStateRead
                    : Api.OperationExecutionScope.GlobalStateMutation,
                operation.ExecutionScope);
            Assert.Empty(operation.RiskFlags);
        }
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

    private sealed class CloudAndMeshWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "cloud_and_mesh_operations.reset_cloud_bounding_box" =>
                [
                    new WorkerRetrievedOutput("X-Axis Dimension", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1)),
                    new WorkerRetrievedOutput("Y-Axis Dimension", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2)),
                    new WorkerRetrievedOutput("Z-Axis Dimension", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3)),
                    new WorkerRetrievedOutput("X-Axis (in WORLD)", WorkerMpValueKind.Vector, new WorkerVectorValue(1, 2, 3)),
                    new WorkerRetrievedOutput("Y-Axis (in WORLD)", WorkerMpValueKind.Vector, new WorkerVectorValue(4, 5, 6)),
                    new WorkerRetrievedOutput("Z-Axis (in WORLD)", WorkerMpValueKind.Vector, new WorkerVectorValue(7, 8, 9)),
                    new WorkerRetrievedOutput("Centroid (in WORLD)", WorkerMpValueKind.Vector, new WorkerVectorValue(10, 11, 12)),
                    new WorkerRetrievedOutput("Reference Transform (in WORLD)", WorkerMpValueKind.Transform,
                        new WorkerTransformValue(Enumerable.Repeat(1d, 16).ToArray())),
                    new WorkerRetrievedOutput("Reference Transform (in WORKING)", WorkerMpValueKind.Transform,
                        new WorkerTransformValue(Enumerable.Repeat(2d, 16).ToArray())),
                    new WorkerRetrievedOutput("Points Used for Bounding Box", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(1))
                ],
                "cloud_and_mesh_operations.get_cloud_point_count" =>
                [
                    new WorkerRetrievedOutput("Points Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(12)),
                    new WorkerRetrievedOutput("Planar Offset", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.5)),
                    new WorkerRetrievedOutput("Radial Offset", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5)),
                    new WorkerRetrievedOutput("Active Clipping Planes", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(3))
                ],
                "cloud_and_mesh_operations.get_number_of_cross_sections_in_cross_section_cloud" =>
                [new WorkerRetrievedOutput("Cross Section Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(6))],
                "cloud_and_mesh_operations.mesh_volume" =>
                [
                    new WorkerRetrievedOutput("Above", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.75)),
                    new WorkerRetrievedOutput("Below", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.25))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
