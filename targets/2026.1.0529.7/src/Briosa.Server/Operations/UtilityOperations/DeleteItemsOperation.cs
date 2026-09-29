using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class DeleteItemsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.delete_items", "Delete Items", "briosa.UtilityOperations",
        "DeleteItems", "/briosa.UtilityOperations/DeleteItems", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteItemsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Item List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.ItemList, "item_list"),
                "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.DeleteItemsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
