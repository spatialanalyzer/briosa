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

public sealed class TypedUtilitySelectionAndOpcOperationTests
{
    private static readonly string[] Ids =
    [
        "utility_operations.close_all_watch_windows", "utility_operations.lock_imported_items",
        "utility_operations.lock_unlock_selected_items", "utility_operations.lock_unlock_trapping_control",
        "utility_operations.get_opc_da_tag_value_double", "utility_operations.get_opc_da_tag_value_integer",
        "utility_operations.get_opc_da_tag_value_string", "utility_operations.set_opc_da_tag_value_double",
        "utility_operations.set_opc_da_tag_value_integer", "utility_operations.set_opc_da_tag_value_string"
    ];

    [Fact]
    public void SelectionAndOpcOperationsAreRegisteredAndRemovedFromCatalog()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }

        foreach (var descriptor in new[]
        {
            GetOpcDaTagValueDoubleOperation.Descriptor, GetOpcDaTagValueIntegerOperation.Descriptor,
            GetOpcDaTagValueStringOperation.Descriptor, SetOpcDaTagValueDoubleOperation.Descriptor,
            SetOpcDaTagValueIntegerOperation.Descriptor, SetOpcDaTagValueStringOperation.Descriptor
        })
        {
            Assert.Contains("fixture_validation_pending", descriptor.RiskFlags);
        }
    }

    [Fact]
    public void MappingsPreserveTypesBindingsRequiredListsAndDefaults()
    {
        var close = CloseAllWatchWindowsOperation.CreateCommand(new());
        var lockImported = LockImportedItemsOperation.CreateCommand(new());
        var lockSelected = LockUnlockSelectedItemsOperation.CreateCommand(SelectedItemsRequest());
        var lockTrapping = LockUnlockTrappingControlOperation.CreateCommand(new()
        {
            RelationshipRefList = { new Api.CollectionItemName { CollectionName = "C", ItemName = "R" } },
            FeatureCheckRefList = { new Api.CollectionItemName { CollectionName = "C", ItemName = "F" } },
            DatumRefList = { new Api.CollectionObjectName { CollectionName = "C", ObjectName = "D" } }
        });

        Assert.Empty(close.InputArguments);
        Assert.False(Assert.Single(lockImported.InputArguments).RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetCollectionObjectNameRefListArg", lockSelected.InputArguments[0].SdkBinding);
        Assert.Equal("SetColInstIdRefListArg", lockSelected.InputArguments[1].SdkBinding);
        Assert.Equal("C", Assert.Single(lockSelected.InputArguments[1]
            .RequireValue<WorkerCollectionInstrumentIdListValue>().Values).CollectionName);
        Assert.False(lockSelected.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerMpValueKind.CollectionItemNameList, lockTrapping.InputArguments[0].Kind);
        Assert.Equal(WorkerMpValueKind.CollectionItemNameList, lockTrapping.InputArguments[1].Kind);
        Assert.Equal(WorkerMpValueKind.CollectionObjectNameList, lockTrapping.InputArguments[2].Kind);
        Assert.False(lockTrapping.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => LockUnlockSelectedItemsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => LockUnlockTrappingControlOperation.CreateCommand(new()));

        var getDouble = GetOpcDaTagValueDoubleOperation.CreateCommand(new() { OpcServerDaTagName = "double" });
        var getInteger = GetOpcDaTagValueIntegerOperation.CreateCommand(new() { OpcServerDaTagName = "integer" });
        var getString = GetOpcDaTagValueStringOperation.CreateCommand(new() { OpcServerDaTagName = "string" });
        Assert.Equal("GetDoubleArg", Assert.Single(getDouble.OutputArguments).SdkBinding);
        Assert.Equal("GetIntegerArg", Assert.Single(getInteger.OutputArguments).SdkBinding);
        Assert.Equal("GetStringArg", Assert.Single(getString.OutputArguments).SdkBinding);
        Assert.Equal("double", Assert.Single(getDouble.InputArguments).RequireValue<WorkerTextValue>().Value);

        var setDouble = SetOpcDaTagValueDoubleOperation.CreateCommand(new()
            { OpcServerDaTagName = "double", Value = 2.5 });
        var setInteger = SetOpcDaTagValueIntegerOperation.CreateCommand(new()
            { OpcServerDaTagName = "integer", Value = 8 });
        var setString = SetOpcDaTagValueStringOperation.CreateCommand(new()
            { OpcServerDaTagName = "string", Value = "ready" });
        Assert.Equal(2.5, setDouble.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(8, setInteger.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("ready", setString.InputArguments[1].RequireValue<WorkerTextValue>().Value);

        Assert.Equal(2.5, GetOpcDaTagValueDoubleOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5))])).Value);
        Assert.Equal(8, GetOpcDaTagValueIntegerOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(8))])).Value);
        Assert.Equal("ready", GetOpcDaTagValueStringOperation.CreateResult(Success(
            [new WorkerRetrievedOutput("Value", WorkerMpValueKind.Text, new WorkerTextValue("ready"))])).Value);
    }

    [Fact]
    public async Task GeneratedClientRoutesSelectionAndOpcCallsToTypedWorkerCommands()
    {
        var worker = new SelectionOpcWorker();
        var grpcHost = await GrpcTestHost.StartAsync<UtilityOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.UtilityOperations.UtilityOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var value = await client.GetOpcDaTagValueDoubleAsync(new() { OpcServerDaTagName = "sensor" }, options);
        var updated = await client.SetOpcDaTagValueIntegerAsync(new()
            { OpcServerDaTagName = "counter", Value = 3 }, options);
        var selection = await client.LockUnlockSelectedItemsAsync(SelectedItemsRequest(), options);

        Assert.Equal(1.25, value.Value);
        Assert.Equal(Api.MpExecutionState.Succeeded, updated.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, selection.Execution.State);
        Assert.Equal(
        [
            "utility_operations.get_opc_da_tag_value_double", "utility_operations.set_opc_da_tag_value_integer",
            "utility_operations.lock_unlock_selected_items"
        ], worker.Commands.Select(command => command.OperationId));
    }

    private static Api.LockUnlockSelectedItemsRequest SelectedItemsRequest() => new()
    {
        ItemList = { new Api.CollectionItemName { CollectionName = "C", ItemName = "point" } },
        Instruments = { new Api.CollectionInstrumentId { CollectionName = "C", InstrumentId = 4 } }
    };

    private static SuccessfulOperationExecution Success(IReadOnlyList<WorkerMpOutputValue> outputs)
    {
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return new(execution, new Api.MpExecutionDetails());
    }

    private sealed class SelectionOpcWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId == "utility_operations.get_opc_da_tag_value_double"
                ? [new WorkerRetrievedOutput("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.25))]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
