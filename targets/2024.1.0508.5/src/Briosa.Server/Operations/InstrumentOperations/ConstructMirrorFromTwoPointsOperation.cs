using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ConstructMirrorFromTwoPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.construct_mirror_from_two_points", "Construct Mirror from Two Points",
        "ConstructMirrorFromTwoPoints");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("mirror_plane", "Mirror Plane", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.ConstructMirrorFromTwoPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Mirror Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasMirrorName ? request.MirrorName : string.Empty), "SetStringArg"),
            new("Point Measured Directly", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointMeasuredDirectly, "point_measured_directly"), "SetPointNameArg"),
            new("Point Measured Through Mirror", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointMeasuredThroughMirror, "point_measured_through_mirror"), "SetPointNameArg"),
            new("Send Mirror to Instrument?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasSendMirrorToInstrument || request.SendMirrorToInstrument), "SetBoolArg")
        ], [new("Mirror Plane", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.ConstructMirrorFromTwoPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            MirrorPlane = CollectionObjectNameMapper.ToProtocol(completed.Execution.OutputValues[0]
                .RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
}
