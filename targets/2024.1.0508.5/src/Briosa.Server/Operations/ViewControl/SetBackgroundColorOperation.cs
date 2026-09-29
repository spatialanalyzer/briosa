using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetBackgroundColorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_background_color", "Set Background Color", "briosa.ViewControl",
        "SetBackgroundColor", "/briosa.ViewControl/SetBackgroundColor", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetBackgroundColorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Solid Color Name", WorkerMpValueKind.RgbColor,
                ViewControlColorMapper.WithDefault(request.SolidColorName), "SetColorArg"),
            new("Gradient Start Color Name", WorkerMpValueKind.RgbColor,
                ViewControlColorMapper.WithDefault(request.GradientStartColorName), "SetColorArg"),
            new("Gradient End Color Name", WorkerMpValueKind.RgbColor,
                ViewControlColorMapper.WithDefault(request.GradientEndColorName), "SetColorArg"),
            new("Highlight Color", WorkerMpValueKind.RgbColor,
                ViewControlColorMapper.WithDefault(request.HighlightColor), "SetColorArg")
        ], []);
    }

    public static Api.SetBackgroundColorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
