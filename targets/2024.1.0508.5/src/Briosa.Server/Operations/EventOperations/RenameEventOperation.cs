using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.EventOperations;

internal static class RenameEventOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "event_operations.rename_event", "Rename Event",
        "briosa.EventOperations", "RenameEvent", "/briosa.EventOperations/RenameEvent",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenameEventRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Original Event Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.OriginalEventName, "original_event_name"), "SetCollectionObjectNameArg2"),
                new("New Event Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.NewEventName, "new_event_name"), "SetCollectionObjectNameArg2"),
                new("Overwrite if exists?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.OverwriteIfExists), "SetBoolArg")
            ], []);
    }

    public static Api.RenameEventResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
