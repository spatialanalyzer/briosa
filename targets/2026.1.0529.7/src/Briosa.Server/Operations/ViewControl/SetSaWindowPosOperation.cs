using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetSaWindowPosOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_sa_window_pos", "Set SA’s Window Pos", "briosa.ViewControl",
        "SetSaWindowPos", "/briosa.ViewControl/SetSaWindowPos", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetSaWindowPosRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Pos X", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.PosX), "SetIntegerArg"),
            new("Pos Y", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.PosY), "SetIntegerArg")
        ], []);
    }

    public static Api.SetSaWindowPosResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
