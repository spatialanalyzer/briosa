using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetRenderModeTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_render_mode_type", "Set Render Mode Type", "briosa.ViewControl",
        "SetRenderModeType", "/briosa.ViewControl/SetRenderModeType", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRenderModeTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Rendering Mode", WorkerMpValueKind.RenderModeType,
            RenderModeTypeMapper.Required(request.HasRenderingMode ? request.RenderingMode : null, "rendering_mode"),
            "SetRenderModeTypeArg")], []);
    }

    public static Api.SetRenderModeTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
