using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetTrackerEdmTheodoliteUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_tracker_edm_theodolite_uncertainties",
        "Set Tracker/EDM Theodolite Uncertainties", "SetTrackerEdmTheodoliteUncertainties");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetTrackerEdmTheodoliteUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Theta Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasThetaDispersion ? request.ThetaDispersion : 1.0), "SetDoubleArg"),
                new("Theta Threshold (linear units)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasThetaThreshold ? request.ThetaThreshold : 0.001), "SetDoubleArg"),
                new("Phi Dispersion(arcseconds)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasPhiDispersion ? request.PhiDispersion : 1.0), "SetDoubleArg"),
                new("Phi Threshold(linear units)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasPhiThreshold ? request.PhiThreshold : 0.001), "SetDoubleArg"),
                new("Distance (PPM)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasDistance ? request.Distance : 2.5), "SetDoubleArg"),
                new("Distance Threshold (linear units)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasDistanceThreshold ? request.DistanceThreshold : 0.0003), "SetDoubleArg")
            ], []);
    }

    public static Api.SetTrackerEdmTheodoliteUncertaintiesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
