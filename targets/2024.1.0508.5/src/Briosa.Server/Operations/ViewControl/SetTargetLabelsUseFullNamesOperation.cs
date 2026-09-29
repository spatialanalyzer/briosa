using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetTargetLabelsUseFullNamesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_target_labels_use_full_names", "Set Target Labels Use Full Names", "briosa.ViewControl",
        "SetTargetLabelsUseFullNames", "/briosa.ViewControl/SetTargetLabelsUseFullNames", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetTargetLabelsUseFullNamesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Use Full Names?", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.UseFullNames), "SetBoolArg")], []);
    }

    public static Api.SetTargetLabelsUseFullNamesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
