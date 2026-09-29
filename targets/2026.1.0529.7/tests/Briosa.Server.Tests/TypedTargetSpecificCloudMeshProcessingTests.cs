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
public sealed class TypedTargetSpecificCloudMeshProcessingTests
{
    [Fact]
    public async Task GeneratedClientMaps2026OnlyCloudAndMeshProcessingRoutes()
    {
        var worker = new TargetSpecificCloudMeshWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var cloud = Object("Scans", "CloudA");
        var vectorGroup = Object("Vectors", "VG1");

        var resolved = await client.FilterCloudsToVectorGroupsResolveCloudsAsync(new()
        {
            CloudNames = { cloud },
            VectorGroupNames = { vectorGroup },
            OutputCollectionName = "Filtered"
        }, options);
        await client.SubdivideCloudByPointSpacingAsync(new()
        {
            SourceCloudName = Object("Scans", "EnhancedA"),
            NewCloudName = Object("Results", "SubdividedA")
        }, options);
        await client.SubdivideCloudByPointSpacingAsync(new()
        {
            SourceCloudName = Object("Scans", "EnhancedB"),
            PointSpacing = 0.25,
            MinimumPointsPerGroup = 8,
            NewCloudName = Object("Results", "SubdividedB"),
            KeepAllGroups = false
        }, options);
        await client.ConsolidateMeshAsync(new() { Mesh = Object("Meshes", "MeshA") }, options);
        await client.MeshFillHolesAsync(new() { Mesh = Object("Meshes", "MeshA") }, options);
        await client.MeshFillHolesAsync(new()
        {
            Mesh = Object("Meshes", "MeshB"),
            MaximumTriangleLength = 0.4,
            Tension = 0.2,
            UnconditionalFilling = true,
            FillAllHoles = false
        }, options);

        Assert.Single(resolved.FilteredClouds);
        Assert.Equal(Api.ObjectType.Cloud, resolved.FilteredClouds[0].ObjectType);
        Assert.Equal("FilteredCloudA", resolved.FilteredClouds[0].ObjectName);
        Assert.Equal(6, worker.Commands.Count);

        var resolve = worker.Commands[0];
        Assert.Equal(6, resolve.InputArguments.Count);
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            resolve.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.Equal(WorkerObjectTypeValue.VectorGroup,
            resolve.InputArguments[1].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.Equal(0.1, resolve.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(-0.1, resolve.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.1, resolve.InputArguments[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("Filtered", resolve.InputArguments[5].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("GetCollectionObjectNameRefListArg", Assert.Single(resolve.OutputArguments).SdkBinding);

        var subdivide = worker.Commands[1];
        Assert.Equal(WorkerObjectTypeValue.EnhancedCloud,
            subdivide.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(0, subdivide.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, subdivide.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.EnhancedCloud,
            subdivide.InputArguments[3].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.True(subdivide.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0.25, worker.Commands[2].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(8, worker.Commands[2].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(worker.Commands[2].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.ScanStripeMesh,
            worker.Commands[3].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var fill = worker.Commands[4];
        Assert.Equal(-1, fill.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, fill.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(fill.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(fill.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0.4, worker.Commands[5].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.2, worker.Commands[5].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(worker.Commands[5].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[5].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task MissingCloudMeshInputsAreRejectedBeforeWorkerExecution()
    {
        var worker = new TargetSpecificCloudMeshWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await AssertInvalid(() => client.FilterCloudsToVectorGroupsResolveCloudsAsync(new()
        {
            CloudNames = { Object("Scans", "CloudA") },
            VectorGroupNames = { Object("Vectors", "VG1") }
        }, options).ResponseAsync);
        await AssertInvalid(() => client.SubdivideCloudByPointSpacingAsync(new()
        {
            SourceCloudName = Object("Scans", "CloudA")
        }, options).ResponseAsync);
        await AssertInvalid(() => client.ConsolidateMeshAsync(new(), options).ResponseAsync);
        await AssertInvalid(() => client.MeshFillHolesAsync(new(), options).ResponseAsync);
        Assert.Empty(worker.Commands);
    }

    [Fact]
    public void Target2026SpecificProcessingRoutesAreRegisteredAsUnsafeMutations()
    {
        var operations = new[]
        {
            FilterCloudsToVectorGroupsResolveCloudsOperation.Descriptor,
            SubdivideCloudByPointSpacingOperation.Descriptor,
            ConsolidateMeshOperation.Descriptor,
            MeshFillHolesOperation.Descriptor
        };

        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, candidate => candidate.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
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

    private sealed class TargetSpecificCloudMeshWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId ==
                "cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_clouds"
                ?
                [new WorkerRetrievedOutput("Filtered Clouds", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue(
                    [new WorkerCollectionObjectNameValue("Filtered", "FilteredCloudA", WorkerObjectTypeValue.Unspecified)]))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
