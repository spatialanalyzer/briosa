using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInstrumentGroupAndTargetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_instrument_group_and_target", "Set Instrument Group and Target",
        "briosa.InstrumentOperations", "SetInstrumentGroupAndTarget",
        "/briosa.InstrumentOperations/SetInstrumentGroupAndTarget",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInstrumentGroupAndTargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg")
        ], []);
    }

    public static Api.SetInstrumentGroupAndTargetResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
