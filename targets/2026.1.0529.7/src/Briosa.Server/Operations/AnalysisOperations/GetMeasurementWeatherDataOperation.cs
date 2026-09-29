using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetMeasurementWeatherDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_measurement_weather_data", "Get Measurement Weather Data",
        "briosa.AnalysisOperations", "GetMeasurementWeatherData", "/briosa.AnalysisOperations/GetMeasurementWeatherData",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("temperature", "Temperature (deg F)", WorkerMpValueKind.FloatingPoint),
        new("pressure", "Pressure (in. Hg)", WorkerMpValueKind.FloatingPoint),
        new("humidity", "Humidity (% RH)", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetMeasurementWeatherDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")],
            [
                new("Temperature (deg F)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pressure (in. Hg)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Humidity (% RH)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetMeasurementWeatherDataResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Temperature = values[0].RequireValue<WorkerDoubleValue>().Value,
            Pressure = values[1].RequireValue<WorkerDoubleValue>().Value,
            Humidity = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
