using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetProbeOffsetFrameOfflineOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_probe_offset_frame_offline",
        "Set Probe Offset Frame Offline (Select Previously Measured Frame)", "SetProbeOffsetFrameOffline");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetProbeOffsetFrameOfflineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Probe Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.ProbeName), "SetStringArg"),
                new("Face ID ", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.FaceId), "SetIntegerArg"),
                new("Raw Measured Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RawMeasuredFrame, "raw_measured_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2"),
                new("Offset Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.OffsetFrame, "offset_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetProbeOffsetFrameOfflineResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
