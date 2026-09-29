using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class RenameCalloutViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.rename_callout_view", "Rename Callout View", "briosa.ConstructionOperations", "RenameCalloutView",
        "/briosa.ConstructionOperations/RenameCalloutView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.RenameCalloutViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Original Callout View Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.OriginalCalloutViewName, "original_callout_view_name", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameArg2"),
            new("New Callout View Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.NewCalloutViewName, "new_callout_view_name", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameArg2"),
            new("Overwrite if exists?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.OverwriteIfExists), "SetBoolArg")
        ], []);
    }
    public static Api.RenameCalloutViewResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
