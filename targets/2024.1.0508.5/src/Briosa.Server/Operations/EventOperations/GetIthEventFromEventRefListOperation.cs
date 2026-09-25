using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.EventOperations;

internal static class GetIthEventFromEventRefListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "event_operations.get_ith_event_from_event_ref_list", "Get i-th Event From Event Ref List",
        "briosa.EventOperations", "GetIthEventFromEventRefList", "/briosa.EventOperations/GetIthEventFromEventRefList",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_item", "Resultant Item", WorkerMpValueKind.CollectionItemName)];

    public static WorkerMpCommand CreateCommand(Api.GetIthEventFromEventRefListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Event List", WorkerMpValueKind.CollectionItemNameList,
                    CollectionItemNameMapper.RequiredList(request.EventList, "event_list"), "SetCollectionObjectNameRefListArg"),
                new("Event Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.EventIndex), "SetIntegerArg")
            ],
            [new("Resultant Item", WorkerMpValueKind.CollectionItemName, "GetCollectionObjectNameArg")]);
    }

    public static Api.GetIthEventFromEventRefListResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ResultantItem = CollectionItemNameMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionItemNameValue>()),
        Execution = completed.Details
    };
}
