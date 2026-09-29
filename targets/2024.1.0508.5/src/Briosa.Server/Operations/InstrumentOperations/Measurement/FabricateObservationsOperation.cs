using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class FabricateObservationsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.fabricate_observations", "Fabricate Observations",
        "briosa.InstrumentOperations", "FabricateObservations",
        "/briosa.InstrumentOperations/FabricateObservations",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.FabricateObservationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var introduceInstrumentError = request.HasIntroduceInstrumentError && request.IntroduceInstrumentError;
        var limitDistance = request.HasLimitDistance && request.LimitDistance;
        var minimumDistance = request.HasMinimumDistance ? request.MinimumDistance : 0d;
        var maximumDistance = request.HasMaximumDistance ? request.MaximumDistance : 1_000_000d;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument to shoot", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Group name to shoot", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PointGroup, "point_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Introduce instrument error?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(introduceInstrumentError), "SetBoolArg"),
            new("Limit Distance?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(limitDistance), "SetBoolArg"),
            new("Min Distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(minimumDistance), "SetDoubleArg"),
            new("Max Distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(maximumDistance), "SetDoubleArg")
        ], []);
    }

    public static Api.FabricateObservationsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
