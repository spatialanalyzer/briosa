using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class DistanceUnitMapper
{
    public static WorkerDistanceUnitChoice OrDefault(
        Api.DistanceUnits? value, Api.DistanceUnits defaultValue) =>
        new((value ?? defaultValue) switch
        {
            Api.DistanceUnits.Meters => WorkerDistanceUnitValue.Meters,
            Api.DistanceUnits.Centimeters => WorkerDistanceUnitValue.Centimeters,
            Api.DistanceUnits.Millimeters => WorkerDistanceUnitValue.Millimeters,
            Api.DistanceUnits.Feet => WorkerDistanceUnitValue.Feet,
            Api.DistanceUnits.Inches => WorkerDistanceUnitValue.Inches,
            Api.DistanceUnits.UsSurveyFeet => WorkerDistanceUnitValue.UsSurveyFeet,
            _ => throw new ArgumentException("Distance units are not supported by this SA target.", nameof(value))
        });
}
