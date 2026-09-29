using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideRelationshipReportOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_relationship_report", "Show/Hide Relationship Report", "briosa.ViewControl",
        "ShowHideRelationshipReport", "/briosa.ViewControl/ShowHideRelationshipReport", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideRelationshipReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.CollectionName, "collection_name"), "SetCollectionNameArg"),
            new("Show Relationship Report", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowRelationshipReport), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideRelationshipReportResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
