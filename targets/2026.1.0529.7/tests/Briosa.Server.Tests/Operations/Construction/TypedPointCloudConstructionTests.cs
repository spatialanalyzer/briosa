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

public sealed class TypedPointCloudConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsPointCloudsWithExactTargetArgumentsAndDefaults()
    {
        var worker = new PointCloudWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var defaultThinning = await client.ConstructPointCloudFromExistingCloudsAsync(new()
        {
            ExistingPointCloudList = { Object("Scans", "A"), Object("Scans", "B") },
            NewCloudName = Object("Results", "Merged")
        }, options);
        var configuredThinning = await client.ConstructPointCloudFromExistingCloudsAsync(new()
        {
            ExistingPointCloudList = { Object("Scans", "C") },
            NewCloudName = Object("Results", "Thinned"),
            CloudThinningSettings = new()
            {
                Mode = Api.CloudThinningMode.NthPoint,
                PointIncrement = 7,
                MinimumNumberOfPoints = 11,
                MaximumNumberOfPoints = 22
            },
            HideOriginalPointClouds = false,
            SetCloudPointRgbFromVoxels = true
        }, options);
        var visiblePoints = await client.ConstructPointCloudFromVisibleCloudPointsAsync(new()
        {
            SourceClouds = { Object("Scans", "VisibleA"), Object("Scans", "VisibleB") },
            DestinationCloudName = Object("Results", "Visible")
        }, options);
        var fromGroup = await client.ConstructPointCloudsFromExistingPointGroupAsync(new()
        {
            PointGroupName = Object("Points", "Control"),
            CloudName = Object("Results", "ControlCloud")
        }, options);

        Assert.All(new[] { defaultThinning.Execution, configuredThinning.Execution, visiblePoints.Execution, fromGroup.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(4, worker.Commands.Count);

        var defaults = worker.Commands[0];
        Assert.Equal("Construct Point Cloud from Existing Clouds", defaults.StepName);
        Assert.Equal(new[] {
            new WorkerCollectionObjectNameValue("Scans", "A", WorkerObjectTypeValue.Any),
            new WorkerCollectionObjectNameValue("Scans", "B", WorkerObjectTypeValue.Any)
        }, defaults.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal(new WorkerCollectionObjectNameValue("Results", "Merged", WorkerObjectTypeValue.Cloud),
            defaults.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>());
        Assert.Equal(new WorkerCloudThinningOptionsValue(0, 1, 0, 0),
            defaults.InputArguments[2].RequireValue<WorkerCloudThinningOptionsValue>());
        Assert.True(defaults.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(defaults.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        var configured = worker.Commands[1];
        Assert.Equal(new WorkerCloudThinningOptionsValue(2, 7, 11, 22),
            configured.InputArguments[2].RequireValue<WorkerCloudThinningOptionsValue>());
        Assert.False(configured.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(configured.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        Assert.Equal(new[] {
            new WorkerCollectionObjectNameValue("Scans", "VisibleA", WorkerObjectTypeValue.Any),
            new WorkerCollectionObjectNameValue("Scans", "VisibleB", WorkerObjectTypeValue.Any)
        }, worker.Commands[2].InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            worker.Commands[3].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            worker.Commands[3].InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);

        var missingSourceClouds = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointCloudFromVisibleCloudPointsAsync(new()
            {
                DestinationCloudName = Object("Results", "Invalid")
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingSourceClouds.StatusCode);
        Assert.Equal(4, worker.Commands.Count);
    }

    [Fact]
    public void PointCloudConstructionOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        foreach (var operation in new[]
        {
            ConstructPointCloudFromExistingCloudsOperation.Descriptor,
            ConstructPointCloudFromVisibleCloudPointsOperation.Descriptor,
            ConstructPointCloudsFromExistingPointGroupOperation.Descriptor
        })
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

    private sealed class PointCloudWorker : IWorkerCommandExecutor
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
