using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.EventOperations;

internal static class DeleteEventOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "event_operations.delete_event", "Delete Event",
        "briosa.EventOperations", "DeleteEvent", "/briosa.EventOperations/DeleteEvent",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteEventRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Event Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.EventName, "event_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeleteEventResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
