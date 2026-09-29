using Briosa.Server.Operations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedUtilityFolderOperationTests
{
    private static readonly string[] Ids =
    [
        "utility_operations.delete_folder", "utility_operations.delete_items",
        "utility_operations.delete_objects", "utility_operations.get_folder_collections",
        "utility_operations.get_folders_by_wildcard", "utility_operations.increment_point_name",
        "utility_operations.move_collection_to_folder", "utility_operations.move_folder_to_folder"
    ];

    [Fact]
    public void EveryFolderAndItemOperationHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void MappingsPreserveBindingsRequiredValuesDefaultsAndOutputs()
    {
        var deleteFolder = DeleteFolderOperation.CreateCommand(new() { FolderPath = "Folder" });
        var deleteItems = DeleteItemsOperation.CreateCommand(new()
        {
            ItemList = { new Api.CollectionItemName { CollectionName = "C", ItemName = "point" } }
        });
        var deleteObjects = DeleteObjectsOperation.CreateCommand(new()
        {
            ObjectNames = { new Api.CollectionObjectName { CollectionName = "C", ObjectName = "frame" } }
        });
        var folderCollections = GetFolderCollectionsOperation.CreateCommand(new());
        var folders = GetFoldersByWildcardOperation.CreateCommand(new() { SearchString = "F*" });
        var increment = IncrementPointNameOperation.CreateCommand(new()
        {
            BasePointName = new Api.PointName { CollectionName = "C", GroupName = "G", TargetName = "P" }
        });
        var moveCollection = MoveCollectionToFolderOperation.CreateCommand(new()
        {
            Collection = new Api.CollectionName { Name = "C" }, FolderPath = "Folder"
        });
        var moveFolder = MoveFolderToFolderOperation.CreateCommand(new()
        {
            SourceFolderPath = "Old", DestinationFolderPath = "New"
        });

        Assert.Equal("SetStringArg", Assert.Single(deleteFolder.InputArguments).SdkBinding);
        Assert.Equal("SetCollectionObjectNameRefListArg", Assert.Single(deleteItems.InputArguments).SdkBinding);
        Assert.Single(deleteItems.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values);
        Assert.Equal("SetCollectionObjectNameRefListArg", Assert.Single(deleteObjects.InputArguments).SdkBinding);
        Assert.Single(deleteObjects.InputArguments[0].RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal("", Assert.Single(folderCollections.InputArguments).RequireValue<WorkerTextValue>().Value);
        Assert.Equal("GetStringRefListArg", Assert.Single(folderCollections.OutputArguments).SdkBinding);
        Assert.True(folders.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("GetStringRefListArg", Assert.Single(folders.OutputArguments).SdkBinding);
        Assert.Equal(0, increment.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("SetPointNameArg", increment.InputArguments[0].SdkBinding);
        Assert.Equal("SetCollectionNameArg", moveCollection.InputArguments[0].SdkBinding);
        Assert.Equal("Folder", moveCollection.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(["Old", "New"], moveFolder.InputArguments
            .Select(argument => argument.RequireValue<WorkerTextValue>().Value));

        Assert.Throws<ArgumentException>(() => DeleteItemsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => DeleteObjectsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => IncrementPointNameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => MoveCollectionToFolderOperation.CreateCommand(new()));

        var listResult = GetFolderCollectionsOperation.CreateResult(
            Success([Strings("Collection List", "Folder A", "Folder B")]));
        Assert.Equal(["Folder A", "Folder B"], listResult.CollectionList);
        var wildcardResult = GetFoldersByWildcardOperation.CreateResult(
            Success([Strings("Folder List", "Folder A")]));
        Assert.Equal(["Folder A"], wildcardResult.FolderList);
        var pointResult = IncrementPointNameOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Resultant Point Name", WorkerMpValueKind.PointName,
                new WorkerPointNameValue("C", "G", "P1"))]));
        Assert.Equal("P1", pointResult.ResultantPointName.TargetName);
    }

    [Fact]
    public async Task GeneratedClientRoutesFolderAndPointCallsToTypedCommands()
    {
        var worker = new FolderWorker();
        var grpcHost = await GrpcTestHost.StartAsync<UtilityOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.UtilityOperations.UtilityOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var collections = await client.GetFolderCollectionsAsync(new() { FolderPath = "Parent" }, options);
        var folders = await client.GetFoldersByWildcardAsync(new() { SearchString = "Child*" }, options);
        var point = await client.IncrementPointNameAsync(new()
        {
            BasePointName = new Api.PointName { CollectionName = "C", GroupName = "G", TargetName = "P" }
        }, options);
        var moved = await client.MoveFolderToFolderAsync(new()
        {
            SourceFolderPath = "Old", DestinationFolderPath = "New"
        }, options);

        Assert.Equal(["Child1", "Child2"], collections.CollectionList);
        Assert.Equal(["Child1"], folders.FolderList);
        Assert.Equal("P1", point.ResultantPointName.TargetName);
        Assert.Equal(Api.MpExecutionState.Succeeded, moved.Execution.State);
        Assert.Equal(
        [
            "utility_operations.get_folder_collections", "utility_operations.get_folders_by_wildcard",
            "utility_operations.increment_point_name", "utility_operations.move_folder_to_folder"
        ], worker.Commands.Select(command => command.OperationId));
    }

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

    private static WorkerRetrievedOutput Strings(string name, params string[] values) =>
        new(name, WorkerMpValueKind.StringList, new WorkerStringListValue(values));

    private sealed class FolderWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "utility_operations.get_folder_collections" =>
                    [Strings("Collection List", "Child1", "Child2")],
                "utility_operations.get_folders_by_wildcard" =>
                    [Strings("Folder List", "Child1")],
                "utility_operations.increment_point_name" =>
                    [new WorkerRetrievedOutput("Resultant Point Name", WorkerMpValueKind.PointName,
                        new WorkerPointNameValue("C", "G", "P1"))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
