using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class GetCollectionInstrumentRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.get_collection_instrument_ref_list_variable",
        "Get Collection Instrument Ref List Variable", "briosa.ConstructionOperations",
        "GetCollectionInstrumentRefListVariable", "/briosa.ConstructionOperations/GetCollectionInstrumentRefListVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.CollectionInstrumentIdList)];

    public static WorkerMpCommand CreateCommand(Api.GetCollectionInstrumentRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.CollectionInstrumentIdList, "GetColInstIdRefListArg")]);
    }

    public static Api.GetCollectionInstrumentRefListVariableResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetCollectionInstrumentRefListVariableResult { Execution = completed.Details };
        result.Value.AddRange(InstrumentIdMapper.ToProtocolList(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionInstrumentIdListValue>()));
        return result;
    }
}
