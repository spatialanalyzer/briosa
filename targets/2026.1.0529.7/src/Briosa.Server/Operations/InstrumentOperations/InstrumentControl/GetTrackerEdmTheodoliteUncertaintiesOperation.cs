using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetTrackerEdmTheodoliteUncertaintiesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_tracker_edm_theodolite_uncertainties",
        "Get Tracker/EDM Theodolite Uncertainties", "GetTrackerEdmTheodoliteUncertainties");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("theta_dispersion", "Theta Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint),
        new("theta_threshold", "Theta Threshold (linear units)", WorkerMpValueKind.FloatingPoint),
        new("phi_dispersion", "Phi Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint),
        new("phi_threshold", "Phi Threshold (linear units)", WorkerMpValueKind.FloatingPoint),
        new("distance", "Distance (PPM)", WorkerMpValueKind.FloatingPoint),
        new("distance_threshold", "Distance Threshold (linear units)", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetTrackerEdmTheodoliteUncertaintiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
            [
                new("Theta Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Theta Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Phi Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Phi Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Distance (PPM)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Distance Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetTrackerEdmTheodoliteUncertaintiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            ThetaDispersion = values[0].RequireValue<WorkerDoubleValue>().Value,
            ThetaThreshold = values[1].RequireValue<WorkerDoubleValue>().Value,
            PhiDispersion = values[2].RequireValue<WorkerDoubleValue>().Value,
            PhiThreshold = values[3].RequireValue<WorkerDoubleValue>().Value,
            Distance = values[4].RequireValue<WorkerDoubleValue>().Value,
            DistanceThreshold = values[5].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
