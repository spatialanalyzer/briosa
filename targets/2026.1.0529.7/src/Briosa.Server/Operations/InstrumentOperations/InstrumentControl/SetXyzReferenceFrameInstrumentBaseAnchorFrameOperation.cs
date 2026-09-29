using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetXyzReferenceFrameInstrumentBaseAnchorFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_xyz_reference_frame_instrument_base_anchor_frame", "Set XYZ Reference Frame Instrument Base Anchor Frame", "SetXyzReferenceFrameInstrumentBaseAnchorFrame");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Anchor Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.AnchorFrame, "anchor_frame", WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
