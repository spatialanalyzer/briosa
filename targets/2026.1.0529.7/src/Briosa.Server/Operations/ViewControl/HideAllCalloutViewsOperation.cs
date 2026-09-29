using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class HideAllCalloutViewsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.hide_all_callout_views", "Hide All Callout Views", "briosa.ViewControl",
        "HideAllCalloutViews", "/briosa.ViewControl/HideAllCalloutViews", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.HideAllCalloutViewsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.HideAllCalloutViewsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
