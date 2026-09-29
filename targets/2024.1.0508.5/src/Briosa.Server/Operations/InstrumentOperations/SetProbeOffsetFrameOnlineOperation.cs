using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetProbeOffsetFrameOnlineOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_probe_offset_frame_online",
        "Set Probe Offset Frame Online (Measure Raw Frame)", "SetProbeOffsetFrameOnline");

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetProbeOffsetFrameOnlineRequest request)
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
                new("Measure Profile Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.MeasureProfileName), "SetStringArg"),
                new("Timeout in Seconds", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasTimeoutSeconds ? request.TimeoutSeconds : 15), "SetDoubleArg"),
                new("Offset Frame", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.OffsetFrame, "offset_frame", WorkerObjectTypeValue.Frame),
                    "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetProbeOffsetFrameOnlineResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
