using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class DockInstrumentInterfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.dock_instrument_interface", "Dock Instrument Interface",
        "briosa.InstrumentOperations", "DockInstrumentInterface", "/briosa.InstrumentOperations/DockInstrumentInterface",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DockInstrumentInterfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Dock Interface?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasDockInterface && request.DockInterface), "SetBoolArg")
            ], []);
    }

    public static Api.DockInstrumentInterfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}