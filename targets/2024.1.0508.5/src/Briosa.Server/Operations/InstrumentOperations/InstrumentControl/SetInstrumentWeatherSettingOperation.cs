using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentWeatherSettingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_instrument_weather_setting", "Set Instrument Weather Setting",
        "briosa.InstrumentOperations", "SetInstrumentWeatherSetting",
        "/briosa.InstrumentOperations/SetInstrumentWeatherSetting",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentWeatherSettingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Temperature (F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.Temperature), "SetDoubleArg"),
                new("Pressure (mmHg)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.Pressure), "SetDoubleArg"),
                new("Humidity (%Rel)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.RelativeHumidity), "SetDoubleArg"),
                new("Set Automatically? (Ignore above values)", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.SetAutomatically), "SetBoolArg")
            ], []);
    }

    public static Api.SetInstrumentWeatherSettingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
