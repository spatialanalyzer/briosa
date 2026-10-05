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

public sealed class TypedCollectionNameConstructionTests
{
    [Fact]
    public async Task GeneratedClientUsesTypedCollectionNameAndReferenceListRoutes()
    {
        var worker = new CollectionNameWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var collection = await client.MakeCollectionNameRuntimeSelectAsync(new(), options);
        var unique = await client.MakeCollectionObjectNameEnsureUniqueAsync(new()
        {
            CollectionObjectName = new() { CollectionName = "Parts", ObjectName = "Plane" }
        }, options);
        var byType = await client.MakeCollectionObjectNameRefListByTypeAsync(new()
        {
            Collection = "Parts", ObjectType = Api.ObjectType.Plane
        }, options);
        var byTypeAndColor = await client.MakeCollectionObjectNameRefListByTypeAndColorAsync(new()
        {
            Collection = "Parts", ObjectType = Api.ObjectType.PointGroup,
            ObjectColor = new() { Red = 12, Green = 34, Blue = 56 }
        }, options);
        var allGroups = await client.MakeCollectionObjectNameRefListFromAllGroupsInCollectionAsync(new()
        {
            CollectionName = new() { Name = "Parts" }
        }, options);
        var listSelection = await client.MakeCollectionObjectNameRefListRuntimeSelectAsync(new()
        {
            UserPrompt = "Pick objects", ObjectType = Api.ObjectType.Surface
        }, options);
        var wildcard = await client.MakeCollectionObjectNameRefListWildcardSelectionAsync(new()
        {
            ObjectType = Api.ObjectType.Frame
        }, options);
        var objectSelection = await client.MakeCollectionObjectNameRuntimeSelectAsync(new()
        {
            UserPrompt = "Pick one", ObjectType = Api.ObjectType.Circle
        }, options);

        Assert.All(new[] { collection.Execution, unique.Execution, byType.Execution,
            byTypeAndColor.Execution, allGroups.Execution, listSelection.Execution,
            wildcard.Execution, objectSelection.Execution },
            details => Assert.Equal(Api.MpExecutionState.Succeeded, details.State));
        Assert.Equal("Selected", collection.ResultantCollectionName.Name);
        Assert.Equal("Object", unique.CollectionObjectName.ObjectName);
        Assert.Equal("Object", Assert.Single(byType.ResultantCollectionObjectNameList).ObjectName);
        Assert.Equal("Object", Assert.Single(byTypeAndColor.ResultantCollectionObjectNameList).ObjectName);
        Assert.Equal("Object", Assert.Single(allGroups.CollectionObjectNameList).ObjectName);
        Assert.Equal("Object", Assert.Single(listSelection.ResultantCollectionObjectNameRefList).ObjectName);
        Assert.Equal("Object", Assert.Single(wildcard.ResultantCollectionObjectNameRefList).ObjectName);
        Assert.Equal("Object", objectSelection.ResultantCollectionObjectName.ObjectName);
        Assert.Equal(8, worker.Commands.Count);
        Assert.Equal(8, worker.Commands.Select(command => command.OperationId).Distinct(StringComparer.Ordinal).Count());

        Assert.Equal(string.Empty, worker.Commands[0].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetCollectionObjectNameArg2", worker.Commands[1].InputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Any,
            worker.Commands[1].InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(WorkerObjectTypeValue.Plane,
            worker.Commands[2].InputArguments[1].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);
        var color = worker.Commands[3].InputArguments[2].RequireValue<WorkerRgbColorValue>();
        Assert.Equal(new WorkerRgbColorValue(12, 34, 56), color);
        Assert.Equal("Parts", worker.Commands[4].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Surface,
            worker.Commands[5].InputArguments[1].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);
        Assert.Equal("*", worker.Commands[6].InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("*", worker.Commands[6].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(WorkerObjectTypeValue.Frame,
            worker.Commands[6].InputArguments[2].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);
        Assert.Equal(WorkerObjectTypeValue.Circle,
            worker.Commands[7].InputArguments[1].RequireValue<WorkerChoiceValue<WorkerObjectTypeValue>>().Value);
        Assert.All(worker.Commands.SelectMany(command => command.OutputArguments), output =>
            Assert.StartsWith("Get", output.SdkBinding, StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvalidCollectionNameRequestsAreRejectedBeforeWorkerDispatch()
    {
        var worker = new CollectionNameWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var missingObjectName = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakeCollectionObjectNameEnsureUniqueAsync(new(), options));
        var missingObjectType = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakeCollectionObjectNameRefListByTypeAsync(new(), options));
        var invalidColor = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.MakeCollectionObjectNameRefListByTypeAndColorAsync(new()
            {
                ObjectType = Api.ObjectType.Plane, ObjectColor = new() { Red = 256 }
            }, options));
        Assert.All(new[] { missingObjectName, missingObjectType, invalidColor },
            exception => Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode));
        Assert.Empty(worker.Commands);
    }

    [Fact]
    public void CollectionNameOperationsHaveSingleReadOnlyRegistration()
    {
        var operations = new[]
        {
            MakeCollectionNameRuntimeSelectOperation.Descriptor,
            MakeCollectionObjectNameEnsureUniqueOperation.Descriptor,
            MakeCollectionObjectNameRefListByTypeOperation.Descriptor,
            MakeCollectionObjectNameRefListByTypeAndColorOperation.Descriptor,
            MakeCollectionObjectNameRefListFromAllGroupsInCollectionOperation.Descriptor,
            MakeCollectionObjectNameRefListRuntimeSelectOperation.Descriptor,
            MakeCollectionObjectNameRefListWildcardSelectionOperation.Descriptor,
            MakeCollectionObjectNameRuntimeSelectOperation.Descriptor
        };
        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, operation.ExecutionScope);
            // Re-prompting an operator is not a safe replay (#242).
            Assert.Equal(
                operation.OperationId.EndsWith("_runtime_select", StringComparison.Ordinal)
                    ? Api.ReplaySafety.Unsafe
                    : Api.ReplaySafety.Safe,
                operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }

        var defaultColor = MakeCollectionObjectNameRefListByTypeAndColorOperation.CreateCommand(new()
        {
            ObjectType = Api.ObjectType.Plane
        }).InputArguments[2].RequireValue<WorkerRgbColorValue>();
        Assert.Equal(new WorkerRgbColorValue(255, 0, 0), defaultColor);
    }

    private sealed class CollectionNameWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.make_collection_name_runtime_select" =>
                [new WorkerRetrievedOutput("Resultant Collection Name", WorkerMpValueKind.CollectionName,
                    new WorkerTextValue("Selected"))],
                "construction_operations.make_collection_object_name_ensure_unique" =>
                [new WorkerRetrievedOutput("Collection Object Name", WorkerMpValueKind.CollectionObjectName,
                    new WorkerCollectionObjectNameValue("Parts", "Object", WorkerObjectTypeValue.Plane))],
                "construction_operations.make_collection_object_name_ref_list_by_type" =>
                [new WorkerRetrievedOutput("Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("Parts", "Object", WorkerObjectTypeValue.Plane)]))],
                "construction_operations.make_collection_object_name_ref_list_by_type_and_color" =>
                [new WorkerRetrievedOutput("Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("Parts", "Object", WorkerObjectTypeValue.PointGroup)]))],
                "construction_operations.make_collection_object_name_ref_list_from_all_groups_in_collection" =>
                [new WorkerRetrievedOutput("Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("Parts", "Object", WorkerObjectTypeValue.PointGroup)]))],
                "construction_operations.make_collection_object_name_ref_list_runtime_select" or
                "construction_operations.make_collection_object_name_ref_list_wildcard_selection" =>
                [new WorkerRetrievedOutput("Resultant Collection Object Name Reference List", WorkerMpValueKind.CollectionObjectNameList,
                    new WorkerCollectionObjectNameListValue([new("Parts", "Object", WorkerObjectTypeValue.Surface)]))],
                "construction_operations.make_collection_object_name_runtime_select" =>
                [new WorkerRetrievedOutput("Resultant Collection Object Name", WorkerMpValueKind.CollectionObjectName,
                    new WorkerCollectionObjectNameValue("Parts", "Object", WorkerObjectTypeValue.Circle))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
