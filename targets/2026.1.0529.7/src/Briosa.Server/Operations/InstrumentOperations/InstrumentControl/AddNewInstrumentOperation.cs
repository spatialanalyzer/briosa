using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AddNewInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.add_new_instrument", "Add New Instrument",
        "briosa.InstrumentOperations", "AddNewInstrument", "/briosa.InstrumentOperations/AddNewInstrument",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("instrument_added", "Instrument Added (result)", WorkerMpValueKind.CollectionInstrumentId)];

    public static WorkerMpCommand CreateCommand(Api.AddNewInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.InstrumentType is null)
            throw new ArgumentException("Instrument Type is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument Type", WorkerMpValueKind.InstrumentTypeName,
                new WorkerTextValue(request.InstrumentType.Value), "SetInstTypeNameArg")],
            [new("Instrument Added (result)", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg")]);
    }

    public static Api.AddNewInstrumentResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        InstrumentAdded = InstrumentIdMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionInstrumentIdValue>()),
        Execution = completed.Details
    };
}