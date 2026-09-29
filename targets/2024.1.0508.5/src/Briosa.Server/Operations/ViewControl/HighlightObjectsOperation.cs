using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class HighlightObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.highlight_objects", "Highlight Objects", "briosa.ViewControl",
        "HighlightObjects", "/briosa.ViewControl/HighlightObjects", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.HighlightObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object Names (Empty to clear all)", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.AllowEmptyList(request.ObjectNames), "SetCollectionObjectNameRefListArg"),
            new("HighLight Objects?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasHighLightObjects && request.HighLightObjects), "SetBoolArg")
        ], []);
    }

    public static Api.HighlightObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
