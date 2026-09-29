using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class TransformInstrumentByDeltaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.transform_instrument_by_delta", "Transform Instrument by Delta",
        "briosa.InstrumentOperations", "TransformInstrumentByDelta", "/briosa.InstrumentOperations/TransformInstrumentByDelta",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TransformInstrumentByDeltaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to Transform", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Delta Transform", WorkerMpValueKind.WorldTransform,
                    WorldTransformMapper.Required(request.DeltaTransform, "delta_transform"), "SetWorldTransformArg"),
                new("Apply Scale from Transform to Instrument", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.ApplyScaleToInstrument), "SetBoolArg")
            ], []);
    }

    public static Api.TransformInstrumentByDeltaResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
