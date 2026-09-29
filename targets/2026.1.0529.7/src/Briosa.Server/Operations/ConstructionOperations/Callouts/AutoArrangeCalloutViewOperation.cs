using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class AutoArrangeCalloutViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.auto_arrange_callout_view", "Auto Arrange Callout View", "briosa.ConstructionOperations", "AutoArrangeCalloutView",
        "/briosa.ConstructionOperations/AutoArrangeCalloutView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.AutoArrangeCalloutViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.CalloutView, "callout_view", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameArg2")], []);
    }
    public static Api.AutoArrangeCalloutViewResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
