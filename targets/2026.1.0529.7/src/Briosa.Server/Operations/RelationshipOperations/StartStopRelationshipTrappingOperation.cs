using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class StartStopRelationshipTrappingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.start_stop_relationship_trapping", "Start/Stop Relationship Trapping",
        "briosa.RelationshipOperations", "StartStopRelationshipTrapping",
        "/briosa.RelationshipOperations/StartStopRelationshipTrapping",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartStopRelationshipTrappingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionItemNameMapper.Required(
            request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship);
        var instrument = request.InstrumentId ??
            throw new ArgumentException("Request field 'instrument_id' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    relationship, "SetCollectionObjectNameArg2"),
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    new WorkerCollectionInstrumentIdValue(instrument.CollectionName, instrument.InstrumentId), "SetColInstIdArg"),
                new("Start Trapping (FALSE = Stop)", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasStartTrapping && request.StartTrapping), "SetBoolArg")
            ], []);
    }

    public static Api.StartStopRelationshipTrappingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
