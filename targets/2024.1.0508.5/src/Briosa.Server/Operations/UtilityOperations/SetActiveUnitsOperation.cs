using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetActiveUnitsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_active_units", "Set Active Units", "briosa.UtilityOperations",
        "SetActiveUnits", "/briosa.UtilityOperations/SetActiveUnits", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetActiveUnitsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var length = request.HasLength ? request.Length : Api.DistanceUnits.Inches;
        var temperature = request.HasTemperature ? request.Temperature : Api.TemperatureUnits.Fahrenheit;
        var angular = request.HasAngular ? request.Angular : Api.AngularUnits.Degrees;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Length", WorkerMpValueKind.DistanceUnit,
                new WorkerDistanceUnitChoice(ToWorker(length)), "SetDistanceUnitsArg"),
            new("Display Inch Fractions?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.DisplayInchFractions), "SetBoolArg"),
            new("Inch Fraction Denominator?", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasInchFractionDenominator ? request.InchFractionDenominator : 16), "SetDoubleArg"),
            new("Simplify Inch Fraction?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasSimplifyInchFraction || request.SimplifyInchFraction), "SetBoolArg"),
            new("Temperature", WorkerMpValueKind.TemperatureUnit,
                new WorkerTemperatureUnitChoice(ToWorker(temperature)), "SetTemperatureUnitsArg"),
            new("Angular", WorkerMpValueKind.AngularUnit,
                new WorkerAngularUnitChoice(ToWorker(angular)), "SetAngularUnitsArg")
        ], []);
    }

    public static Api.SetActiveUnitsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    private static WorkerDistanceUnitValue ToWorker(Api.DistanceUnits value) => value switch
    {
        Api.DistanceUnits.Meters => WorkerDistanceUnitValue.Meters,
        Api.DistanceUnits.Centimeters => WorkerDistanceUnitValue.Centimeters,
        Api.DistanceUnits.Millimeters => WorkerDistanceUnitValue.Millimeters,
        Api.DistanceUnits.Feet => WorkerDistanceUnitValue.Feet,
        Api.DistanceUnits.Inches => WorkerDistanceUnitValue.Inches,
        Api.DistanceUnits.UsSurveyFeet => WorkerDistanceUnitValue.UsSurveyFeet,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Distance unit is not supported.")
    };

    private static WorkerTemperatureUnitValue ToWorker(Api.TemperatureUnits value) => value switch
    {
        Api.TemperatureUnits.Fahrenheit => WorkerTemperatureUnitValue.Fahrenheit,
        Api.TemperatureUnits.Celsius => WorkerTemperatureUnitValue.Celsius,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Temperature unit is not supported.")
    };

    private static WorkerAngularUnitValue ToWorker(Api.AngularUnits value) => value switch
    {
        Api.AngularUnits.Degrees => WorkerAngularUnitValue.Degrees,
        Api.AngularUnits.DegreesMinutesSeconds => WorkerAngularUnitValue.DegreesMinutesSeconds,
        Api.AngularUnits.Radians => WorkerAngularUnitValue.Radians,
        Api.AngularUnits.Milliradians => WorkerAngularUnitValue.Milliradians,
        Api.AngularUnits.GonsGrad => WorkerAngularUnitValue.GonsGrad,
        Api.AngularUnits.Mils => WorkerAngularUnitValue.Mils,
        Api.AngularUnits.Arcseconds => WorkerAngularUnitValue.Arcseconds,
        Api.AngularUnits.DegreesMinutes => WorkerAngularUnitValue.DegreesMinutes,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Angular unit is not supported.")
    };
}
