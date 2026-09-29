using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_objects", "Show Objects", "briosa.ViewControl",
        "ShowObjects", "/briosa.ViewControl/ShowObjects", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Objects To Show", WorkerMpValueKind.CollectionObjectNameList,
            CollectionObjectNameMapper.RequiredList(request.ObjectsToShow, "objects_to_show"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.ShowObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
