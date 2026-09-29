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

public sealed class TypedPointMutationTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedPointMutationRoutesAndDefaults()
    {
        var worker = new PointMutationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var deleteNamed = await client.DeletePointsAsync(new()
        {
            PointNames = { new Api.PointName { TargetName = "A" }, new Api.PointName { TargetName = "B" } }
        }, options);
        var deleteWildcard = await client.DeletePointsWildcardSelectionAsync(new()
        {
            GroupsToDeleteFrom = { new Api.CollectionObjectName { ObjectName = "Group" } },
            WildcardSelectionNames = new() { TargetName = "Point_*" }
        }, options);
        var rename = await client.RenamePointAsync(new()
        {
            OriginalPointName = new() { TargetName = "Old" },
            NewPointName = new() { TargetName = "New" }
        }, options);
        var renamePattern = await client.RenamePointsWithNamePatternAsync(new()
        {
            PointNames = { new Api.PointName { TargetName = "A" }, new Api.PointName { TargetName = "B" } }
        }, options);

        Assert.All(new[] { deleteNamed.Execution, deleteWildcard.Execution, rename.Execution, renamePattern.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal(4, worker.Commands.Count);
        Assert.Equal(2, worker.Commands[0].InputArguments[0].RequireValue<WorkerPointNameListValue>().Values.Count);
        Assert.Equal("SetPointNameRefListArg", worker.Commands[0].InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[1].InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values[0].ObjectType);
        Assert.False(worker.Commands[2].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("NewName_%d", worker.Commands[3].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(1, worker.Commands[3].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);

        var emptyDeleteList = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.DeletePointsAsync(new(), options));
        var emptyDeleteGroups = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.DeletePointsWildcardSelectionAsync(new()
            {
                WildcardSelectionNames = new() { TargetName = "Point_*" }
            }, options));
        var missingOldPoint = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.RenamePointAsync(new()
            {
                NewPointName = new() { TargetName = "New" }
            }, options));
        Assert.All(new[] { emptyDeleteList, emptyDeleteGroups, missingOldPoint },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(4, worker.Commands.Count);
    }

    [Fact]
    public void PointMutationOperationsPreserveRiskAndExecutionMetadata()
    {
        var operations = new[]
        {
            DeletePointsOperation.Descriptor,
            DeletePointsWildcardSelectionOperation.Descriptor,
            RenamePointOperation.Descriptor,
            RenamePointsWithNamePatternOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
        }
        Assert.All(new[] { DeletePointsOperation.Descriptor, DeletePointsWildcardSelectionOperation.Descriptor },
            operation => Assert.Equal(["destructive"], operation.RiskFlags));
        Assert.Empty(RenamePointOperation.Descriptor.RiskFlags);
        Assert.Empty(RenamePointsWithNamePatternOperation.Descriptor.RiskFlags);
    }

    private sealed class PointMutationWorker : IWorkerCommandExecutor
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
