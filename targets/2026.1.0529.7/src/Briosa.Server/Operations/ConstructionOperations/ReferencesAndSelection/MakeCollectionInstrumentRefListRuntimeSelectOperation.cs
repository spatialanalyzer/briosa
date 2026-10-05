using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionInstrumentRefListRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_instrument_ref_list_runtime_select",
        "Make a Collection Instrument Reference List- Runtime Select", "briosa.ConstructionOperations",
        "MakeCollectionInstrumentRefListRuntimeSelect",
        "/briosa.ConstructionOperations/MakeCollectionInstrumentRefListRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_collection_instrument_ref_list", "Resultant Collection Instrument Reference List",
            WorkerMpValueKind.CollectionInstrumentIdList)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionInstrumentRefListRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Resultant Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList,
                "GetColInstIdRefListArg")]);
    }

    public static Api.MakeCollectionInstrumentRefListRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakeCollectionInstrumentRefListRuntimeSelectResult { Execution = completed.Details };
        result.ResultantCollectionInstrumentRefList.AddRange(InstrumentIdMapper.ToProtocolList(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionInstrumentIdListValue>()));
        return result;
    }
}
