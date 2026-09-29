using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class AddCollectionInstrumentsToRefListWildcardSelectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.add_collection_instruments_to_ref_list_wildcard_selection",
        "Add Collection Instruments to a Ref List - WildCard Selection", "briosa.ConstructionOperations",
        "AddCollectionInstrumentsToRefListWildcardSelection",
        "/briosa.ConstructionOperations/AddCollectionInstrumentsToRefListWildcardSelection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("collection_instrument_ref_list", "Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList)];

    public static WorkerMpCommand CreateCommand(Api.AddCollectionInstrumentsToRefListWildcardSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList,
                CollectionInstrumentIdMapper.RequiredList(request.CollectionInstrumentRefList, "collection_instrument_ref_list"),
                "SetColInstIdRefListArg"),
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Instrument Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasInstrumentWildcardCriteria ? request.InstrumentWildcardCriteria : "*"), "SetStringArg")
        ], [new("Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList,
            "GetColInstIdRefListArg")]);
    }

    public static Api.AddCollectionInstrumentsToRefListWildcardSelectionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.AddCollectionInstrumentsToRefListWildcardSelectionResult { Execution = completed.Details };
        result.CollectionInstrumentRefList.AddRange(InstrumentIdMapper.ToProtocolList(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionInstrumentIdListValue>()));
        return result;
    }
}
