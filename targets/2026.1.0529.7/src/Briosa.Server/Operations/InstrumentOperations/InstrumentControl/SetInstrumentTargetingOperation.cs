using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentTargetingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_instrument_targeting", "Set Instrument Targeting",
        "briosa.InstrumentOperations", "SetInstrumentTargeting",
        "/briosa.InstrumentOperations/SetInstrumentTargeting",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentTargetingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var targetingName = request.HasTargetingName ? request.TargetingName : string.Empty;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Targeting Name", WorkerMpValueKind.Text, new WorkerTextValue(targetingName), "SetStringArg")
        ], []);
    }

    public static Api.SetInstrumentTargetingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
