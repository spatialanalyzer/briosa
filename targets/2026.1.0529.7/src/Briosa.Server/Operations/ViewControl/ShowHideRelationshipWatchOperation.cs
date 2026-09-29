using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideRelationshipWatchOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_relationship_watch", "Show/Hide Relationship Watch", "briosa.ViewControl",
        "ShowHideRelationshipWatch", "/briosa.ViewControl/ShowHideRelationshipWatch", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideRelationshipWatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
            new("Show Relationship Watch", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowRelationshipWatch), "SetBoolArg"),
            new("Relationship Watch Window Properties", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipWatchWindowProperties,
                    "relationship_watch_window_properties"), "SetCollectionObjectNameArg2"),
            new("Window Top Left X Position", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.WindowTopLeftXPosition), "SetIntegerArg"),
            new("Window Top Left Y Position", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.WindowTopLeftYPosition), "SetIntegerArg"),
            new("Window Width", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.WindowWidth), "SetIntegerArg"),
            new("Window Height", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.WindowHeight), "SetIntegerArg")
        ], []);
    }

    public static Api.ShowHideRelationshipWatchResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
