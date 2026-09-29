using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetObservationInfoOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_observation_info", "Get Observation Info",
        "briosa.InstrumentOperations", "GetObservationInfo", "/briosa.InstrumentOperations/GetObservationInfo",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("observation", "Resulting Instrument", WorkerMpValueKind.CollectionInstrumentId),
        new("observation", "Resultant Vector", WorkerMpValueKind.Vector),
        new("observation", "Active?", WorkerMpValueKind.Logical),
        new("observation", "Timestamp", WorkerMpValueKind.Text),
        new("observation", "RMS Error", WorkerMpValueKind.FloatingPoint),
        new("observation", "Temperature (deg F)", WorkerMpValueKind.FloatingPoint),
        new("observation", "Pressure (in. Hg)", WorkerMpValueKind.FloatingPoint),
        new("observation", "Humidity (% RH)", WorkerMpValueKind.FloatingPoint),
        new("observation", "Info Data", WorkerMpValueKind.Text)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetObservationInfoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg"),
            new("Observation Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.ObservationIndex), "SetIntegerArg")
        ],
        [
            new("Resulting Instrument", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg"),
            new("Resultant Vector", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Active?", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Timestamp", WorkerMpValueKind.Text, "GetStringArg"),
            new("RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Temperature (deg F)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Pressure (in. Hg)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Humidity (% RH)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Info Data", WorkerMpValueKind.Text, "GetStringArg")
        ]);
    }

    public static Api.GetObservationInfoResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        var vector = values[1].RequireValue<WorkerVectorValue>();
        return new()
        {
            Observation = new Api.ObservationInfo
            {
                Instrument = InstrumentIdMapper.ToProtocol(
                    values[0].RequireValue<WorkerCollectionInstrumentIdValue>()),
                SphericalValues = new Api.ObservationSphericalValues
                {
                    Distance = vector.X,
                    Azimuth = vector.Y,
                    Elevation = vector.Z
                },
                Active = values[2].RequireValue<WorkerBooleanValue>().Value,
                Timestamp = values[3].RequireValue<WorkerTextValue>().Value,
                RmsError = values[4].RequireValue<WorkerDoubleValue>().Value,
                Temperature = values[5].RequireValue<WorkerDoubleValue>().Value,
                Pressure = values[6].RequireValue<WorkerDoubleValue>().Value,
                RelativeHumidity = values[7].RequireValue<WorkerDoubleValue>().Value,
                InfoData = values[8].RequireValue<WorkerTextValue>().Value
            },
            Execution = completed.Details
        };
    }
}
