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

public sealed class TypedPointCloudDerivationTests
{
    [Fact]
    public async Task GeneratedClientConstructsBoundaryAndDerivedClouds()
    {
        var worker = new PointCloudDerivationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var boundary = await client.ConstructBoundaryPointsFromCloudAsync(new()
        {
            SourceCloudName = Object("Scans", "Source"),
            DestinationCloudName = Object("Results", "Boundary")
        }, options);
        var limited = await client.ConstructPointCloudLimitingProbingDirectionsAsync(new()
        {
            SourceCloudName = Object("Scans", "Source"),
            NormalToObjectName = Object("Geometry", "Plane"),
            DestinationCloudName = Object("Results", "Limited")
        }, options);
        var uniform = await client.ConstructPointCloudsFromExistingCloudsUniformSpacingAsync(new()
        {
            ExistingPointCloudList = { Object("Scans", "A"), Object("Scans", "B") },
            DesiredPointSpacing = 0.05,
            MinimumPointsPerOutputPoint = 4,
            NewCloudName = Object("Results", "Uniform"),
            HideOriginalPointClouds = false
        }, options);

        Assert.All(new[] { boundary.Execution, limited.Execution, uniform.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(3, worker.Commands.Count);

        Assert.Equal(WorkerObjectTypeValue.Cloud, ObjectArgument(worker.Commands[0], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Cloud, ObjectArgument(worker.Commands[0], 1).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Cloud, ObjectArgument(worker.Commands[1], 0).ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Any, ObjectArgument(worker.Commands[1], 1).ObjectType);
        Assert.Equal(30, worker.Commands[1].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(worker.Commands[1].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0.05, worker.Commands[2].InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(4, worker.Commands[2].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Cloud, ObjectArgument(worker.Commands[2], 3).ObjectType);
        Assert.False(worker.Commands[2].InputArguments[4].RequireValue<WorkerBooleanValue>().Value);

        var missingDestination = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructBoundaryPointsFromCloudAsync(new()
            {
                SourceCloudName = Object("Scans", "Invalid")
            }, options));
        Assert.Equal(StatusCode.InvalidArgument, missingDestination.StatusCode);
        Assert.Equal(3, worker.Commands.Count);
    }

    [Fact]
    public void PointCloudDerivationOperationsAreUnsafeGlobalMutationsOutsideTheGenericCatalog()
    {
        foreach (var operation in new[]
        {
            ConstructBoundaryPointsFromCloudOperation.Descriptor,
            ConstructPointCloudLimitingProbingDirectionsOperation.Descriptor,
            ConstructPointCloudsFromExistingCloudsUniformSpacingOperation.Descriptor
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

    private static WorkerCollectionObjectNameValue ObjectArgument(WorkerMpCommand command, int index) =>
        command.InputArguments[index].RequireValue<WorkerCollectionObjectNameValue>();

    private sealed class PointCloudDerivationWorker : IWorkerCommandExecutor
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
