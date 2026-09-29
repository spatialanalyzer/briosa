using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentMeasurementModeProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_instrument_measurement_mode_profile", "Set Instrument Measurement Mode/Profile",
        "briosa.InstrumentOperations", "SetInstrumentMeasurementModeProfile",
        "/briosa.InstrumentOperations/SetInstrumentMeasurementModeProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentMeasurementModeProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var modeProfile = request.HasModeProfile ? request.ModeProfile : string.Empty;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument to set", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Mode/Profile", WorkerMpValueKind.Text, new WorkerTextValue(modeProfile), "SetStringArg")
        ], []);
    }

    public static Api.SetInstrumentMeasurementModeProfileResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
