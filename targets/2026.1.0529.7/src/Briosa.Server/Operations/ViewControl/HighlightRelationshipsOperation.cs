using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class HighlightRelationshipsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.highlight_relationships", "Highlight Relationships", "briosa.ViewControl",
        "HighlightRelationships", "/briosa.ViewControl/HighlightRelationships", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.HighlightRelationshipsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Relationships (Empty to clear all)", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.AllowEmptyList(request.Relationships), "SetCollectionObjectNameRefListArg"),
            new("HighLight Relationships?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasHighLightRelationships && request.HighLightRelationships), "SetBoolArg")
        ], []);
    }

    public static Api.HighlightRelationshipsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
