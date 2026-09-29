using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineFromInstrumentShotOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_from_instrument_shot", "Construct Line From Instrument Shot",
        "briosa.ConstructionOperations", "ConstructLineFromInstrumentShot",
        "/briosa.ConstructionOperations/ConstructLineFromInstrumentShot",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineFromInstrumentShotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
            new("Observation Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasObservationIndex ? request.ObservationIndex : 0), "SetIntegerArg"),
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructLineFromInstrumentShotResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
