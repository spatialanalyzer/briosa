using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class JumpInstrumentToNewLocationOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.jump_instrument_to_new_location", "Jump Instrument To New Location", "JumpInstrumentToNewLocation");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.JumpInstrumentToNewLocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Live Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.LiveInstrument, "live_instrument"), "SetColInstIdArg"),
                new("Hide the Previous Instrument?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasHidePreviousInstrument && request.HidePreviousInstrument), "SetBoolArg")
            ], []);
    }

    public static Api.JumpInstrumentToNewLocationResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
