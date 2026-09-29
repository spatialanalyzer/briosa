using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.make_collection_object_name_ref_list_from_objects_associated_with_instruments",
        "Make Collection Object Name Ref List from Objects associated with Instruments",
        "briosa.InstrumentOperations", "MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstruments",
        "/briosa.InstrumentOperations/MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstruments",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("objects", "Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument IDs", WorkerMpValueKind.CollectionInstrumentIdList,
                InstrumentIdMapper.RequiredList(request.Instruments, "instruments"), "SetColInstIdRefListArg")],
            [new("Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList,
                "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsResult
        {
            Execution = completed.Details
        };
        result.Objects.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values
            .Select(CollectionObjectNameMapper.ToProtocol));
        return result;
    }
}