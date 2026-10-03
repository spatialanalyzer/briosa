using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetPointOfViewFromFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_point_of_view_from_frame", "Set Point of View from Frame", "briosa.ViewControl",
        "SetPointOfViewFromFrame", "/briosa.ViewControl/SetPointOfViewFromFrame", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointOfViewFromFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Frame", WorkerMpValueKind.CollectionObjectName,
            CollectionObjectNameMapper.Required(request.Frame, "frame"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.SetPointOfViewFromFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
