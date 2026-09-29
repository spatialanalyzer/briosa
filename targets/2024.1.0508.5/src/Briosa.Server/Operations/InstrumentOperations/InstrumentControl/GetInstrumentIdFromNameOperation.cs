using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentIdFromNameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_id_from_name", "Get Instrument ID from Name",
        "briosa.InstrumentOperations", "GetInstrumentIdFromName", "/briosa.InstrumentOperations/GetInstrumentIdFromName",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentIdFromNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg")]);
    }

    public static Api.GetInstrumentIdFromNameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Instrument = InstrumentIdMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionInstrumentIdValue>()),
        Execution = completed.Details
    };
}