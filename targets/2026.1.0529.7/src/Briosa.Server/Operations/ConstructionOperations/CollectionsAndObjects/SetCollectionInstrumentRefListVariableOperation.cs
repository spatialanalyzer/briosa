using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class SetCollectionInstrumentRefListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.set_collection_instrument_ref_list_variable",
        "Set Collection Instrument Ref List Variable", "briosa.ConstructionOperations",
        "SetCollectionInstrumentRefListVariable", "/briosa.ConstructionOperations/SetCollectionInstrumentRefListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCollectionInstrumentRefListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
            new("Value", WorkerMpValueKind.CollectionInstrumentIdList,
                CollectionInstrumentIdMapper.RequiredList(request.Value, "value"), "SetColInstIdRefListArg")
        ], []);
    }

    public static Api.SetCollectionInstrumentRefListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
