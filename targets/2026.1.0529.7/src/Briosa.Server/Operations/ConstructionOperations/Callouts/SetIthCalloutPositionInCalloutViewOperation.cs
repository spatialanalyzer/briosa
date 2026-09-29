using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class SetIthCalloutPositionInCalloutViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.set_ith_callout_position_in_callout_view", "Set I-th Callout Position in Callout View",
        "briosa.ConstructionOperations", "SetIthCalloutPositionInCalloutView", "/briosa.ConstructionOperations/SetIthCalloutPositionInCalloutView",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetIthCalloutPositionInCalloutViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.CalloutView, "callout_view", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameArg2"),
            new("Callout View Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.CalloutViewIndex), "SetIntegerArg"),
            new("X Position", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.XPosition), "SetIntegerArg"),
            new("Y Position", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.YPosition), "SetIntegerArg")
        ], []);
    }
    public static Api.SetIthCalloutPositionInCalloutViewResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
