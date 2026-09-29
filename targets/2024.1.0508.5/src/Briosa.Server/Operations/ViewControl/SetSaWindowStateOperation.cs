using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetSaWindowStateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_sa_window_state", "Set SA's Window State", "briosa.ViewControl",
        "SetSaWindowState", "/briosa.ViewControl/SetSaWindowState", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetSaWindowStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("SA Window State", WorkerMpValueKind.WindowState,
            WindowStateMapper.Required(request.HasSaWindowState ? request.SaWindowState : null, "sa_window_state"),
            "SetWindowStateArg")], []);
    }

    public static Api.SetSaWindowStateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
