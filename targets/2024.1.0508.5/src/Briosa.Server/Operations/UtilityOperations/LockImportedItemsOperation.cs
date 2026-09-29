using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class LockImportedItemsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.lock_imported_items", "Lock Imported Items", "briosa.UtilityOperations",
        "LockImportedItems", "/briosa.UtilityOperations/LockImportedItems", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LockImportedItemsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Lock Items?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.LockItems), "SetBoolArg")], []);
    }

    public static Api.LockImportedItemsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
