using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ActivateDeactivateInstrumentToolbarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.activate_deactivate_instrument_toolbar", "Activate/Deactivate Instrument Toolbar",
        "briosa.InstrumentOperations", "ActivateDeactivateInstrumentToolbar",
        "/briosa.InstrumentOperations/ActivateDeactivateInstrumentToolbar",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ActivateDeactivateInstrumentToolbarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Deactivate Toolbar?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasDeactivateToolbar && request.DeactivateToolbar), "SetBoolArg")
            ], []);
    }

    public static Api.ActivateDeactivateInstrumentToolbarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}