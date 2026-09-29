using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MeasureNominalFeatureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.measure_nominal_feature", "Measure Nominal Feature",
        "briosa.InstrumentOperations", "MeasureNominalFeature", "/briosa.InstrumentOperations/MeasureNominalFeature",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MeasureNominalFeatureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Feature Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Feature, "feature"), "SetCollectionObjectNameArg2"),
            new("Resulting Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ResultingPoint, "resulting_point"), "SetPointNameArg")
        ], []);
    }

    public static Api.MeasureNominalFeatureResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
