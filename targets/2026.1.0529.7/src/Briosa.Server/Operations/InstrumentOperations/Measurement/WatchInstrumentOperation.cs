using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class WatchInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.watch_instrument", "Watch Instrument",
        "briosa.InstrumentOperations", "WatchInstrument", "/briosa.InstrumentOperations/WatchInstrument",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.WatchInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Pause MP Until Closed", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasPauseMpUntilClosed && request.PauseMpUntilClosed), "SetBoolArg"),
                new("3 DOF Watch Window Properties", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.WatchWindowProperties, "watch_window_properties"), "SetCollectionObjectNameArg2"),
                new("Window Top Left X Position", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.WindowTopLeftX), "SetIntegerArg"),
                new("Window Top Left Y Position", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.WindowTopLeftY), "SetIntegerArg"),
                new("Window Width", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.WindowWidth), "SetIntegerArg"),
                new("Window Height", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.WindowHeight), "SetIntegerArg")
            ], []);
    }

    public static Api.WatchInstrumentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}