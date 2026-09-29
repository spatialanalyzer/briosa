using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideCalloutViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_callout_view", "Show/Hide Callout View", "briosa.ViewControl",
        "ShowHideCalloutView", "/briosa.ViewControl/ShowHideCalloutView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideCalloutViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Callout View To Show", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.CalloutViewToShow, "callout_view_to_show"), "SetCollectionObjectNameArg2"),
            new("Show Callout View?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasShowCalloutView || request.ShowCalloutView), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideCalloutViewResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
