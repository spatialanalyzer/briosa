using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Briosa.Server.Operations.Values;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetLastInstrumentIndexOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_last_instrument_index", "Get Last Instrument Index",
        "briosa.InstrumentOperations", "GetLastInstrumentIndex", "/briosa.InstrumentOperations/GetLastInstrumentIndex",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("instrument_index", "Instrument ID", WorkerMpValueKind.WholeNumber),
        new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetLastInstrumentIndexRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [
                new("Instrument ID", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg")
            ]);
    }

    public static Api.GetLastInstrumentIndexResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        InstrumentIndex = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Instrument = InstrumentIdMapper.ToProtocol(
            completed.Execution.OutputValues[1].RequireValue<WorkerCollectionInstrumentIdValue>()),
        Execution = completed.Details
    };
}