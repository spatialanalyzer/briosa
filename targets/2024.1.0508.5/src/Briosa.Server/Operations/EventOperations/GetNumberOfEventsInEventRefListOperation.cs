using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.EventOperations;

internal static class GetNumberOfEventsInEventRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "event_operations.get_number_of_events_in_event_ref_list", "Get Number of Events in Event Ref List",
        "briosa.EventOperations", "GetNumberOfEventsInEventRefList", "/briosa.EventOperations/GetNumberOfEventsInEventRefList",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_count", "Total Count", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetNumberOfEventsInEventRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Event List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.EventList, "event_list"), "SetCollectionObjectNameRefListArg")],
            [new("Total Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetNumberOfEventsInEventRefListResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TotalCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
