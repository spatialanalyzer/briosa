using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class RenameInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.rename_instrument", "Rename Instrument",
        "briosa.InstrumentOperations", "RenameInstrument", "/briosa.InstrumentOperations/RenameInstrument",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenameInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("New Name", WorkerMpValueKind.Text, new WorkerTextValue(request.NewName), "SetStringArg")
            ], []);
    }

    public static Api.RenameInstrumentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}