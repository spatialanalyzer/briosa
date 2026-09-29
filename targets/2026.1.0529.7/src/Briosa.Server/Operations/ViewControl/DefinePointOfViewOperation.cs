using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class DefinePointOfViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.define_point_of_view", "Define Point of View", "briosa.ViewControl",
        "DefinePointOfView", "/briosa.ViewControl/DefinePointOfView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DefinePointOfViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("View Name", WorkerMpValueKind.ViewName, ViewNameMapper.Required(request.ViewName, "view_name"), "SetViewNameArg"),
            new("Rotation (x)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.RotationX), "SetDoubleArg"),
            new("Rotation (y)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.RotationY), "SetDoubleArg"),
            new("Rotation (z)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.RotationZ), "SetDoubleArg"),
            new("Restore Zoom Settings?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.RestoreZoomSettings), "SetBoolArg"),
            new("Scale Factor", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasScaleFactor ? request.ScaleFactor : 1d), "SetDoubleArg"),
            new("Origin (x)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.OriginX), "SetDoubleArg"),
            new("Origin (y)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.OriginY), "SetDoubleArg"),
            new("Restore Render Mode?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.RestoreRenderMode), "SetBoolArg"),
            new("Rendering Mode", WorkerMpValueKind.RenderModeType,
                RenderModeTypeMapper.ToWorker(request.HasRenderingMode ? request.RenderingMode : Api.RenderModeType.Wireframe),
                "SetRenderModeTypeArg")
        ], []);
    }

    public static Api.DefinePointOfViewResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
