using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SavePointOfViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.save_point_of_view", "Save point of view", "briosa.ViewControl",
        "SavePointOfView", "/briosa.ViewControl/SavePointOfView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SavePointOfViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("View Name", WorkerMpValueKind.ViewName,
                ViewNameMapper.Required(request.ViewName, "view_name"), "SetViewNameArg"),
            new("Restore Zoom Settings?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasRestoreZoomSettings || request.RestoreZoomSettings), "SetBoolArg")
        ], []);
    }

    public static Api.SavePointOfViewResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
