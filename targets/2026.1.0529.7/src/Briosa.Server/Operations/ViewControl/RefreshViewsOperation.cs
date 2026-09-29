using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class RefreshViewsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.refresh_views", "Refresh Views", "briosa.ViewControl",
        "RefreshViews", "/briosa.ViewControl/RefreshViews", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RefreshViewsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.RefreshViewsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
