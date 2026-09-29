using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AutoCorrespondWithProximityTriggerOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.auto_correspond_with_proximity_trigger",
        "Auto-Correspond with Proximity Trigger",
        "briosa.InstrumentOperations", "AutoCorrespondWithProximityTrigger",
        "/briosa.InstrumentOperations/AutoCorrespondWithProximityTrigger",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoCorrespondWithProximityTriggerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasDeviationVectorGroupName)
            throw new ArgumentException("Request field 'deviation_vector_group_name' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Nominal Point Group or Vector Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.NominalGroup, "nominal_group"), "SetCollectionObjectNameArg2"),
                new("Results Point Group for measurements", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ResultsGroup, "results_group",
                        WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Point distance threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasPointDistanceThreshold ? request.PointDistanceThreshold : 0.5),
                    "SetDoubleArg"),
                new("Vector axis threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasVectorAxisThreshold ? request.VectorAxisThreshold : 0.25),
                    "SetDoubleArg"),
                new("Project results to nominal vector", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasProjectResultsToNominalVector &&
                        request.ProjectResultsToNominalVector), "SetBoolArg"),
                new("Warbler ramp start zone distance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasWarblerRampStartDistance ?
                        request.WarblerRampStartDistance : 12), "SetDoubleArg"),
                new("Show Watch window on startup", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowWatchWindow && request.ShowWatchWindow), "SetBoolArg"),
                new("Vector Group to make while Measuring (blank means ignore)", WorkerMpValueKind.VectorGroupName,
                    new WorkerTextValue(request.DeviationVectorGroupName), "SetVectorGroupNameArg"),
                new("Make unmeasured group when done", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasMakeUnmeasuredGroup && request.MakeUnmeasuredGroup), "SetBoolArg"),
                new("Measure each point only once", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasMeasureEachPointOnlyOnce && request.MeasureEachPointOnlyOnce),
                    "SetBoolArg")
            ], []);
    }

    public static Api.AutoCorrespondWithProximityTriggerResult CreateResult(
        SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
