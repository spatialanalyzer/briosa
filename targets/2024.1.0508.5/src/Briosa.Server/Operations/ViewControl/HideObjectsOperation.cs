using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class HideObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.hide_objects", "Hide Objects", "briosa.ViewControl",
        "HideObjects", "/briosa.ViewControl/HideObjects", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.HideObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Objects To Hide", WorkerMpValueKind.CollectionObjectNameList,
            CollectionObjectNameMapper.RequiredList(request.ObjectsToHide, "objects_to_hide"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.HideObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
