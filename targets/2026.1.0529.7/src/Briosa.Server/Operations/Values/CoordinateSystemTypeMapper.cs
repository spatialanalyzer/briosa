using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CoordinateSystemTypeMapper
{
    public static WorkerCoordinateSystemTypeValue OrDefault(
        Api.CoordinateSystemType? value, Api.CoordinateSystemType defaultValue) =>
        (value ?? defaultValue) switch
        {
            Api.CoordinateSystemType.Cartesian => WorkerCoordinateSystemTypeValue.Cartesian,
            Api.CoordinateSystemType.Cylindric => WorkerCoordinateSystemTypeValue.Cylindric,
            Api.CoordinateSystemType.Polar => WorkerCoordinateSystemTypeValue.Polar,
            _ => throw new ArgumentException("Coordinate system is not supported by this SA target.", nameof(value))
        };
}
