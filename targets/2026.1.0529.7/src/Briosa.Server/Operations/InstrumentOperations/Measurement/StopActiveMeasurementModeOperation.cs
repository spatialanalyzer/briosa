using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class StopActiveMeasurementModeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.stop_active_measurement_mode", "Stop Active Measurement Mode",
        "briosa.InstrumentOperations", "StopActiveMeasurementMode",
        "/briosa.InstrumentOperations/StopActiveMeasurementMode",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StopActiveMeasurementModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
            InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")], []);
    }

    public static Api.StopActiveMeasurementModeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
