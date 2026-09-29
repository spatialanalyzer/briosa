using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetViewClippingPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_view_clipping_plane", "Set View Clipping Plane", "briosa.ViewControl",
        "SetViewClippingPlane", "/briosa.ViewControl/SetViewClippingPlane", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetViewClippingPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Object, "object"), "SetCollectionObjectNameArg2"),
            new("Remove Clipping Plane?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.RemoveClippingPlane), "SetBoolArg")
        ], []);
    }

    public static Api.SetViewClippingPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
