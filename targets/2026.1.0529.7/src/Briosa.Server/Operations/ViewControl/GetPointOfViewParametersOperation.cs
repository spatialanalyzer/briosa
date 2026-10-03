using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class GetPointOfViewParametersOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.get_point_of_view_parameters", "Get point of view parameters", "briosa.ViewControl",
        "GetPointOfViewParameters", "/briosa.ViewControl/GetPointOfViewParameters", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("rotation_x", "Rotation (x)", WorkerMpValueKind.FloatingPoint),
        new("rotation_y", "Rotation (y)", WorkerMpValueKind.FloatingPoint),
        new("rotation_z", "Rotation (z)", WorkerMpValueKind.FloatingPoint),
        new("restore_zoom_settings", "Restore Zoom Settings?", WorkerMpValueKind.Logical),
        new("scale_factor", "Scale Factor", WorkerMpValueKind.FloatingPoint),
        new("origin_x", "Origin (x)", WorkerMpValueKind.FloatingPoint),
        new("origin_y", "Origin (y)", WorkerMpValueKind.FloatingPoint),
        new("restore_render_mode", "Restore Render Mode?", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPointOfViewParametersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("View Name", WorkerMpValueKind.ViewName,
            ViewNameMapper.Required(request.ViewName, "view_name"), "SetViewNameArg")],
        [
            new("Rotation (x)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Rotation (y)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Rotation (z)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Restore Zoom Settings?", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Scale Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Origin (x)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Origin (y)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Restore Render Mode?", WorkerMpValueKind.Logical, "GetBoolArg")
        ]);
    }

    public static Api.GetPointOfViewParametersResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new Api.GetPointOfViewParametersResult
        {
            Execution = completed.Details,
            RotationX = values[0].RequireValue<WorkerDoubleValue>().Value,
            RotationY = values[1].RequireValue<WorkerDoubleValue>().Value,
            RotationZ = values[2].RequireValue<WorkerDoubleValue>().Value,
            RestoreZoomSettings = values[3].RequireValue<WorkerBooleanValue>().Value,
            ScaleFactor = values[4].RequireValue<WorkerDoubleValue>().Value,
            OriginX = values[5].RequireValue<WorkerDoubleValue>().Value,
            OriginY = values[6].RequireValue<WorkerDoubleValue>().Value,
            RestoreRenderMode = values[7].RequireValue<WorkerBooleanValue>().Value
        };
    }
}
