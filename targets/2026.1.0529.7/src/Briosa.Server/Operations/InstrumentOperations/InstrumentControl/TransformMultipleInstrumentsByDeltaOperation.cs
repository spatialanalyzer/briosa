using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class TransformMultipleInstrumentsByDeltaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.transform_multiple_instruments_by_delta", "Transform Multiple Instruments By Delta",
        "briosa.InstrumentOperations", "TransformMultipleInstrumentsByDelta", "/briosa.InstrumentOperations/TransformMultipleInstrumentsByDelta",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TransformMultipleInstrumentsByDeltaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instruments to Move", WorkerMpValueKind.CollectionInstrumentIdList,
                    InstrumentIdMapper.RequiredList(request.Instruments, "instruments"), "SetColInstIdRefListArg"),
                new("Delta Transform", WorkerMpValueKind.WorldTransform,
                    WorldTransformMapper.Required(request.DeltaTransform, "delta_transform"), "SetWorldTransformArg"),
                new("Apply Scale from Transform to Instrument", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.ApplyScaleToInstruments), "SetBoolArg")
            ], []);
    }

    public static Api.TransformMultipleInstrumentsByDeltaResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
