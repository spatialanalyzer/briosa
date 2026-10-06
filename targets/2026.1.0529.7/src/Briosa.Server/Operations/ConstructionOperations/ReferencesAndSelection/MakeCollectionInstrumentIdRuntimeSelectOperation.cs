using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakeCollectionInstrumentIdRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_collection_instrument_id_runtime_select",
        "Make a Collection Instrument ID - Runtime Select", "briosa.ConstructionOperations",
        "MakeCollectionInstrumentIdRuntimeSelect", "/briosa.ConstructionOperations/MakeCollectionInstrumentIdRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("instrument_id", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId)];

    public static WorkerMpCommand CreateCommand(Api.MakeCollectionInstrumentIdRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg")]);
    }

    public static Api.MakeCollectionInstrumentIdRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            InstrumentId = InstrumentIdMapper.ToProtocol(
                completed.Execution.OutputValues[0].RequireValue<WorkerCollectionInstrumentIdValue>()),
            Execution = completed.Details
        };
}
