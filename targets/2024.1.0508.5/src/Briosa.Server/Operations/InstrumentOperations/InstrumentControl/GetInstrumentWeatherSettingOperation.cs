using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentWeatherSettingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_weather_setting", "Get Instrument Weather Setting",
        "briosa.InstrumentOperations", "GetInstrumentWeatherSetting",
        "/briosa.InstrumentOperations/GetInstrumentWeatherSetting",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("temperature", "Temperature (F)", WorkerMpValueKind.FloatingPoint),
        new("pressure", "Pressure (mmHg)", WorkerMpValueKind.FloatingPoint),
        new("relative_humidity", "Humidity (%Rel)", WorkerMpValueKind.FloatingPoint),
        new("set_automatically", "Was Set Automatically? (using Inst or external sensor", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentWeatherSettingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("Temperature (F)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pressure (mmHg)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Humidity (%Rel)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Was Set Automatically? (using Inst or external sensor", WorkerMpValueKind.Logical, "GetBoolArg")
            ]);
    }

    public static Api.GetInstrumentWeatherSettingResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Temperature = values[0].RequireValue<WorkerDoubleValue>().Value,
            Pressure = values[1].RequireValue<WorkerDoubleValue>().Value,
            RelativeHumidity = values[2].RequireValue<WorkerDoubleValue>().Value,
            SetAutomatically = values[3].RequireValue<WorkerBooleanValue>().Value,
            Execution = completed.Details
        };
    }
}
