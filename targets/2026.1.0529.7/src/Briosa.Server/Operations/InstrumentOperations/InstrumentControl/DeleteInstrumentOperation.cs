using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class DeleteInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.delete_instrument", "Delete Instrument",
        "briosa.InstrumentOperations", "DeleteInstrument", "/briosa.InstrumentOperations/DeleteInstrument",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Prompt user to confirm?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasPromptUserToConfirm && request.PromptUserToConfirm), "SetBoolArg"),
                new("Keep resulting points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasKeepResultingPoints || request.KeepResultingPoints), "SetBoolArg")
            ], []);
    }

    public static Api.DeleteInstrumentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}