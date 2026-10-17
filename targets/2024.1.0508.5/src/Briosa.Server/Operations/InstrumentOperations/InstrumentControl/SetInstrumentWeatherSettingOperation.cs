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
        // #243 D2 (R2): set_automatically is required. The three values are required
        // only when it is false; when it is true SA ignores them, so an omitted value
        // is still sent as 0.
        var automatic = RequiredValueMapper.Boolean(
            request.HasSetAutomatically, request.SetAutomatically, "set_automatically");
        WorkerDoubleValue Setting(bool present, double value, string fieldName) =>
            automatic.Value ? new(present ? value : 0) : RequiredValueMapper.Double(present, value, fieldName);

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Temperature (F)", WorkerMpValueKind.FloatingPoint,
                    Setting(request.HasTemperature, request.Temperature, "temperature"), "SetDoubleArg"),
                new("Pressure (mmHg)", WorkerMpValueKind.FloatingPoint,
                    Setting(request.HasPressure, request.Pressure, "pressure"), "SetDoubleArg"),
                new("Humidity (%Rel)", WorkerMpValueKind.FloatingPoint,
                    Setting(request.HasRelativeHumidity, request.RelativeHumidity, "relative_humidity"), "SetDoubleArg"),
                new("Set Automatically? (Ignore above values)", WorkerMpValueKind.Logical,
                    automatic, "SetBoolArg")
            ], []);
    }

    public static Api.SetInstrumentWeatherSettingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
