using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetGeomRelationshipAutoMeasureNominalFeatureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_geom_relationship_auto_measure_nominal_feature",
        "Set Geom Relationship Auto Measure Nominal Feature",
        "briosa.RelationshipOperations", "SetGeomRelationshipAutoMeasureNominalFeature",
        "/briosa.RelationshipOperations/SetGeomRelationshipAutoMeasureNominalFeature",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGeomRelationshipAutoMeasureNominalFeatureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        var instrument = request.InstrumentId ??
            throw new ArgumentException("Request field 'instrument_id' is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Trap Clouds? (FALSE = Geometry)", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasTrapClouds || request.TrapClouds), "SetBoolArg"),
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    new WorkerCollectionInstrumentIdValue(instrument.CollectionName, instrument.InstrumentId), "SetColInstIdArg"),
                new("Measurement Mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasMeasurementMode ? request.MeasurementMode : "Empty"), "SetStringArg")
            ], []);
    }

    public static Api.SetGeomRelationshipAutoMeasureNominalFeatureResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
