using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class LockUnlockSelectedItemsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.lock_unlock_selected_items", "Lock/Unlock Selected Items", "briosa.UtilityOperations",
        "LockUnlockSelectedItems", "/briosa.UtilityOperations/LockUnlockSelectedItems", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LockUnlockSelectedItemsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Instruments.Count == 0)
            throw new ArgumentException("Request field 'instruments' is required.", nameof(request));

        var instruments = new WorkerCollectionInstrumentIdListValue(request.Instruments
            .Select(item => new WorkerCollectionInstrumentIdValue(item.CollectionName, item.InstrumentId)).ToArray());
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Item List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.ItemList, "item_list"), "SetCollectionObjectNameRefListArg"),
            new("Instruments", WorkerMpValueKind.CollectionInstrumentIdList, instruments, "SetColInstIdRefListArg"),
            new("Lock Items?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.LockItems), "SetBoolArg")
        ], []);
    }

    public static Api.LockUnlockSelectedItemsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
