using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ConstructMirrorFromPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.construct_mirror_from_plane", "Construct Mirror from Plane", "ConstructMirrorFromPlane");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructMirrorFromPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Mirror Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasMirrorName ? request.MirrorName : string.Empty), "SetStringArg"),
            new("Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Plane, "plane"), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructMirrorFromPlaneResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
