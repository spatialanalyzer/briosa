using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetMpWindowStateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_mp_window_state", "Set MP’s Window State", "briosa.ViewControl",
        "SetMpWindowState", "/briosa.ViewControl/SetMpWindowState", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetMpWindowStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("MP Window State", WorkerMpValueKind.WindowState,
            WindowStateMapper.Required(request.HasMpWindowState ? request.MpWindowState : null, "mp_window_state"),
            "SetWindowStateArg")], []);
    }

    public static Api.SetMpWindowStateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
