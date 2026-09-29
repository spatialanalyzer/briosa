using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetWorkingColorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_working_color", "Set Working Color", "briosa.ViewControl",
        "SetWorkingColor", "/briosa.ViewControl/SetWorkingColor", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetWorkingColorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("New Working Color Name", WorkerMpValueKind.RgbColor,
            ViewControlColorMapper.WithDefault(request.NewWorkingColorName), "SetColorArg")], []);
    }

    public static Api.SetWorkingColorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
