using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DeleteCalloutViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.delete_callout_view", "Delete Callout View", "briosa.ConstructionOperations", "DeleteCalloutView",
        "/briosa.ConstructionOperations/DeleteCalloutView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.DeleteCalloutViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.CalloutView, "callout_view", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameArg2")], []);
    }
    public static Api.DeleteCalloutViewResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
