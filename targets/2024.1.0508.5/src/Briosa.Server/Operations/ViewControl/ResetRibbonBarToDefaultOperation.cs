using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ResetRibbonBarToDefaultOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.reset_ribbon_bar_to_default", "Reset Ribbon Bar to Default", "briosa.ViewControl",
        "ResetRibbonBarToDefault", "/briosa.ViewControl/ResetRibbonBarToDefault", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ResetRibbonBarToDefaultRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.ResetRibbonBarToDefaultResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
