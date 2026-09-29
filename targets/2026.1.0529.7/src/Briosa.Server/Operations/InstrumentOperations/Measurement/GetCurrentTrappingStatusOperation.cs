using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetCurrentTrappingStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_current_trapping_status", "Get Current Trapping Status",
        "briosa.InstrumentOperations", "GetCurrentTrappingStatus",
        "/briosa.InstrumentOperations/GetCurrentTrappingStatus",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("status", "Trapping Active?", WorkerMpValueKind.Logical),
        new("status", "Relationship / Feature Check Name", WorkerMpValueKind.CollectionItemName),
        new("status", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCurrentTrappingStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
        [
            new("Trapping Active?", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Relationship / Feature Check Name", WorkerMpValueKind.CollectionItemName,
                "GetCollectionObjectNameArg"),
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg")
        ]);
    }

    public static Api.GetCurrentTrappingStatusResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Status = new Api.CurrentTrappingStatus
            {
                Active = values[0].RequireValue<WorkerBooleanValue>().Value,
                FocusedItem = CollectionItemNameMapper.ToProtocol(
                    values[1].RequireValue<WorkerCollectionItemNameValue>()),
                Instrument = InstrumentIdMapper.ToProtocol(
                    values[2].RequireValue<WorkerCollectionInstrumentIdValue>())
            },
            Execution = completed.Details
        };
    }
}
