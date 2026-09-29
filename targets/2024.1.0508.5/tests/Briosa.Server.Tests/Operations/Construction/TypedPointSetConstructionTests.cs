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

public sealed class TypedPointSetConstructionTests
{
    [Fact]
    public async Task GeneratedClientConstructsPointSetsWithDocumentedDefaults()
    {
        var worker = new PointSetWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var layout = await client.ConstructPointsLayoutOnGridAsync(new()
        {
            GroupName = new() { ObjectName = "Grid" }
        }, options);
        var subset = await client.ConstructPointsSubsetWithGreatestSpacingAsync(new()
        {
            PointsToSubsample = { new Api.PointName { TargetName = "A" }, new Api.PointName { TargetName = "B" } },
            GroupForSubset = new() { ObjectName = "Subset" }
        }, options);
        var wildcard = await client.ConstructPointsWildcardSelectionAsync(new()
        {
            GroupsToSelectFrom = { new Api.CollectionObjectName { ObjectName = "Source" } },
            WildcardSelectionNames = new() { TargetName = "Point_*" },
            GroupForNewPoints = new() { ObjectName = "Selected" }
        }, options);

        Assert.All(new[] { layout.Execution, subset.Execution, wildcard.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(3, worker.Commands.Count);
        var layoutArgs = worker.Commands[0].InputArguments;
        Assert.Equal(WorkerObjectTypeValue.Any,
            layoutArgs[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("p", layoutArgs[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0d, layoutArgs[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(100d, layoutArgs[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(10, layoutArgs[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0d, layoutArgs[5].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(50d, layoutArgs[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(10, layoutArgs[7].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0d, layoutArgs[8].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, layoutArgs[9].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(1, layoutArgs[10].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(10, worker.Commands[1].InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            worker.Commands[1].InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.False(worker.Commands[2].InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[2].InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);

        var missingGridGroup = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsLayoutOnGridAsync(new(), options));
        var emptySubset = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsSubsetWithGreatestSpacingAsync(new()
            {
                GroupForSubset = new() { ObjectName = "Subset" }
            }, options));
        var emptyWildcardGroups = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointsWildcardSelectionAsync(new()
            {
                WildcardSelectionNames = new() { TargetName = "Point_*" },
                GroupForNewPoints = new() { ObjectName = "Selected" }
            }, options));
        Assert.All(new[] { missingGridGroup, emptySubset, emptyWildcardGroups },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(3, worker.Commands.Count);
    }

    [Fact]
    public void PointSetOperationsAreUnsafeGlobalMutations()
    {
        var operations = new[]
        {
            ConstructPointsLayoutOnGridOperation.Descriptor,
            ConstructPointsSubsetWithGreatestSpacingOperation.Descriptor,
            ConstructPointsWildcardSelectionOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private sealed class PointSetWorker : IWorkerCommandExecutor
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
