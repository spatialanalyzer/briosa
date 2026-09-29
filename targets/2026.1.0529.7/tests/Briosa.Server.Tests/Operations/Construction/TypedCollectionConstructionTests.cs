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

public sealed class TypedCollectionConstructionTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedCollectionAndFolderRoutes()
    {
        var worker = new CollectionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var source = new Api.CollectionObjectName { CollectionName = "source", ObjectName = "plane" };

        var created = await client.ConstructCollectionAsync(new() { CollectionName = new() { Name = "new" } }, options);
        var folders = await client.ConstructFoldersAsync(new(), options);
        var copied = await client.CopyObjectsToACollectionAsync(new()
        {
            SourceObjects = { source }, DestinationCollectionName = new() { Name = "copy" }
        }, options);
        var moved = await client.MoveObjectsToACollectionAsync(new()
        {
            SourceObjects = { source }, DestinationCollectionName = new() { Name = "move" }
        }, options);
        var renamed = await client.RenameCollectionAsync(new()
        {
            OriginalCollectionName = new() { Name = "new" }, NewCollectionName = new() { Name = "renamed" }
        }, options);
        var selected = await client.SetOrConstructDefaultCollectionAsync(new() { CollectionName = new() { Name = "renamed" } }, options);
        var deleted = await client.DeleteCollectionAsync(new() { CollectionName = new() { Name = "copy" } }, options);
        var collectionsDeleted = await client.DeleteCollectionsByWildcardAsync(new(), options);
        var foldersDeleted = await client.DeleteFoldersByWildcardAsync(new() { CaseSensitiveSearch = false }, options);

        Assert.All(new[] { created.Execution, folders.Execution, copied.Execution, moved.Execution, renamed.Execution,
            selected.Execution, deleted.Execution, collectionsDeleted.Execution, foldersDeleted.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.True(collectionsDeleted.HasNumDeleted);
        Assert.Equal(0, collectionsDeleted.NumDeleted);
        Assert.True(collectionsDeleted.HasNumFailed);
        Assert.Equal(2, collectionsDeleted.NumFailed);
        Assert.True(foldersDeleted.HasNumDeleted);
        Assert.True(foldersDeleted.HasNumFailed);
        Assert.Equal(2, foldersDeleted.NumFailed);
        Assert.Equal(9, worker.Commands.Count);
        Assert.Equal(9, worker.Commands.Select(command => command.OperationId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("", worker.Commands[0].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.False(worker.Commands[0].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("", worker.Commands[1].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        var objects = worker.Commands[2].InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values;
        Assert.Equal(WorkerObjectTypeValue.Any, Assert.Single(objects).ObjectType);
        Assert.Equal("SetCollectionObjectNameRefListArg", worker.Commands[2].InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionNameArg", worker.Commands[2].InputArguments[1].SdkBinding);
        Assert.Equal("move", worker.Commands[3].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("renamed", worker.Commands[4].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Name of Collection to Delete", worker.Commands[6].InputArguments[0].Name);
        Assert.True(worker.Commands[7].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[7].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[8].InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(worker.Commands[8].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        // Invalid requests must be rejected before they reach the worker.
        var missingName = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructCollectionAsync(new(), options));
        var blankName = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.RenameCollectionAsync(new()
            {
                OriginalCollectionName = new() { Name = "source" }, NewCollectionName = new() { Name = " " }
            }, options));
        var emptyObjects = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CopyObjectsToACollectionAsync(new() { DestinationCollectionName = new() { Name = "copy" } }, options));
        var unknownObjectType = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MoveObjectsToACollectionAsync(new()
            {
                SourceObjects = { new Api.CollectionObjectName { ObjectName = "plane", ObjectType = (Api.ObjectType)9999 } },
                DestinationCollectionName = new() { Name = "move" }
            }, options));
        Assert.All(new[] { missingName, blankName, emptyObjects, unknownObjectType },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Equal(9, worker.Commands.Count);
    }

    [Fact]
    public void CollectionOperationsHaveSingleRegistrationAndPreserveRiskMetadata()
    {
        var operations = new[]
        {
            ConstructCollectionOperation.Descriptor, ConstructFoldersOperation.Descriptor,
            CopyObjectsToACollectionOperation.Descriptor, MoveObjectsToACollectionOperation.Descriptor,
            DeleteCollectionOperation.Descriptor, DeleteCollectionsByWildcardOperation.Descriptor,
            DeleteFoldersByWildcardOperation.Descriptor, RenameCollectionOperation.Descriptor,
            SetOrConstructDefaultCollectionOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
        }
        Assert.Equal(["destructive"], DeleteCollectionOperation.Descriptor.RiskFlags);
        Assert.All(operations.Where(operation => operation != DeleteCollectionOperation.Descriptor),
            operation => Assert.Empty(operation.RiskFlags));
    }

    [Fact]
    public void WildcardFlagsPreserveOmittedAndExplicitValues()
    {
        var collection = DeleteCollectionsByWildcardOperation.CreateCommand(new()
        {
            CaseSensitiveSearch = false, AllowDeletingAllCollections = true
        });
        Assert.False(collection.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(collection.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        var folder = DeleteFoldersByWildcardOperation.CreateCommand(new());
        Assert.True(folder.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(folder.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.All(folder.OutputArguments, output => Assert.Equal("GetIntegerArg", output.SdkBinding));
    }

    private sealed class CollectionWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.delete_collections_by_wildcard" or "construction_operations.delete_folders_by_wildcard" =>
                [
                    new WorkerRetrievedOutput("Num Deleted", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(0)),
                    new WorkerRetrievedOutput("Num Failed", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(2))
                ],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
