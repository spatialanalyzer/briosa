using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetTargetComputationOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_target_computation_options", "Set Target Computation Options", "SetTargetComputationOptions");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetTargetComputationOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Target Computation Method", WorkerMpValueKind.TargetComputationMethod,
                    new WorkerChoiceValue<WorkerTargetComputationMethodValue>(MapMethod(request)), "SetTargetComputationMethodArg"),
                new("Ignore Distance Measurements", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasIgnoreDistanceMeasurements && request.IgnoreDistanceMeasurements), "SetBoolArg")
            ], []);
    }

    public static Api.SetTargetComputationOptionsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };

    private static WorkerTargetComputationMethodValue MapMethod(Api.SetTargetComputationOptionsRequest request) =>
        request.HasComputationMethod ? request.ComputationMethod switch
        {
            Api.TargetComputationMethod.UseMostRecentShotFromEachFace => WorkerTargetComputationMethodValue.UseMostRecentShotFromEachFace,
            Api.TargetComputationMethod.UseOnlyMostRecentShot => WorkerTargetComputationMethodValue.UseOnlyMostRecentShot,
            Api.TargetComputationMethod.DoNotChangePriorMeasurements => WorkerTargetComputationMethodValue.DoNotChangePriorMeasurements,
            Api.TargetComputationMethod.ForceNewPointForEachMeasurement => WorkerTargetComputationMethodValue.ForceNewPointForEachMeasurement,
            Api.TargetComputationMethod.RemoveAllPriorShots => WorkerTargetComputationMethodValue.RemoveAllPriorShots,
            Api.TargetComputationMethod.DeactivateAllPriorShots => WorkerTargetComputationMethodValue.DeactivateAllPriorShots,
            _ => throw new ArgumentException("Request field 'computation_method' is not supported by this SA target.", nameof(request))
        } : throw new ArgumentException("Request field 'computation_method' is required.", nameof(request));
}
