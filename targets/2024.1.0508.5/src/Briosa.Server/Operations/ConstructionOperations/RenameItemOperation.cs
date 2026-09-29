using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class RenameItemOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.rename_item", "Rename Item", "briosa.ConstructionOperations", "RenameItem",
        "/briosa.ConstructionOperations/RenameItem", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenameItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Original Item Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.OriginalItemName, "original_item_name"), "SetCollectionObjectNameArg2"),
            new("New Item Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.NewItemName, "new_item_name"), "SetCollectionObjectNameArg2"),
            new("Overwrite if exists?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOverwriteIfExists && request.OverwriteIfExists), "SetBoolArg")
        ], []);
    }

    public static Api.RenameItemResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
