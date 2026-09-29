using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetSaWindowSizeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_sa_window_size", "Set SA's Window Size", "briosa.ViewControl",
        "SetSaWindowSize", "/briosa.ViewControl/SetSaWindowSize", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetSaWindowSizeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Width", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.Width), "SetIntegerArg"),
            new("Height", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.Height), "SetIntegerArg")
        ], []);
    }

    public static Api.SetSaWindowSizeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
