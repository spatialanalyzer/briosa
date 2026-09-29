using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class StopProjectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.stop_projection", "Stop Projection", "briosa.InstrumentOperations",
        "StopProjection", "/briosa.InstrumentOperations/StopProjection", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["instrument-control", "at-risk-no-runtime-validation"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StopProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")], []);
    }

    public static Api.StopProjectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
