using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetObservationCollimationShotOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_observation_collimation_shot_options", "Set Observation Collimation Shot Options",
        "briosa.InstrumentOperations", "SetObservationCollimationShotOptions",
        "/briosa.InstrumentOperations/SetObservationCollimationShotOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObservationCollimationShotOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var observationIndex = request.HasObservationIndex ? request.ObservationIndex : 0;
        var isCollimationShot = request.HasIsCollimationShot && request.IsCollimationShot;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg"),
            new("Observation Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(observationIndex), "SetIntegerArg"),
            new("Is Collimation Shot? (FALSE = Normal", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(isCollimationShot), "SetBoolArg"),
            new("Targeted Instrument", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.TargetedInstrument, "targeted_instrument"), "SetColInstIdArg")
        ], []);
    }

    public static Api.SetObservationCollimationShotOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
