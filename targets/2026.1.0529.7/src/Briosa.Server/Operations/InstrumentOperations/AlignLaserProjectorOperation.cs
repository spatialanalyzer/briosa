using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AlignLaserProjectorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.align_laser_projector", "Align Laser Projector",
        "briosa.InstrumentOperations", "AlignLaserProjector", "/briosa.InstrumentOperations/AlignLaserProjector",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AlignLaserProjectorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Group, "group", WorkerObjectTypeValue.PointGroup),
                    "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.AlignLaserProjectorResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
