using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CoordinateSystemTypeMapper
{
    public static WorkerCoordinateSystemTypeValue OrDefault(
        Api.CoordinateSystemType? value, Api.CoordinateSystemType defaultValue, string fieldName) =>
        (value ?? defaultValue) switch
        {
            Api.CoordinateSystemType.Cartesian => WorkerCoordinateSystemTypeValue.Cartesian,
            Api.CoordinateSystemType.Cylindric => WorkerCoordinateSystemTypeValue.Cylindric,
            Api.CoordinateSystemType.Polar => WorkerCoordinateSystemTypeValue.Polar,
            _ => throw (value is null
                ? RequestFieldViolation.Required(fieldName)
                : RequestFieldViolation.SupportedChoice(fieldName))
        };
}
