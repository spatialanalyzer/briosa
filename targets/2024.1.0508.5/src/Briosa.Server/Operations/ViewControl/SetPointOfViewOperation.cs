using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetPointOfViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_point_of_view", "Set point of view", "briosa.ViewControl",
        "SetPointOfView", "/briosa.ViewControl/SetPointOfView", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointOfViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("View Name", WorkerMpValueKind.ViewName,
            ViewNameMapper.Required(request.ViewName, "view_name"), "SetViewNameArg")], []);
    }

    public static Api.SetPointOfViewResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
