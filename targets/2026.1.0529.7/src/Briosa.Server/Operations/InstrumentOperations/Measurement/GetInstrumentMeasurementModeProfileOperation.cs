using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentMeasurementModeProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_measurement_mode_profile", "Get Instrument Measurement Mode/Profile",
        "briosa.InstrumentOperations", "GetInstrumentMeasurementModeProfile",
        "/briosa.InstrumentOperations/GetInstrumentMeasurementModeProfile",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("mode_profile", "Mode/Profile", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentMeasurementModeProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument to set", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [new("Mode/Profile", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.GetInstrumentMeasurementModeProfileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ModeProfile = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
