using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class TrackTapeMeasurementOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.track_tape_measurement", "Track Tape Measurement",
        "briosa.InstrumentOperations", "TrackTapeMeasurement",
        "/briosa.InstrumentOperations/TrackTapeMeasurement",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TrackTapeMeasurementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parameterSetName = request.HasParameterSetName ? request.ParameterSetName : string.Empty;
        var initialTargetName = request.HasInitialTargetName ? request.InitialTargetName : string.Empty;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument to scan", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Point on Tape", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointOnTape, "point_on_tape"), "SetPointNameArg"),
            new("Point on Part", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointOnPart, "point_on_part"), "SetPointNameArg"),
            new("Point for Direction", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.DirectionPoint, "direction_point"), "SetPointNameArg"),
            new("Point for Termination", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.TerminationPoint, "termination_point"), "SetPointNameArg"),
            new("Parameter set name", WorkerMpValueKind.Text,
                new WorkerTextValue(parameterSetName), "SetStringArg"),
            new("Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PointGroup, "point_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Initial Target Name", WorkerMpValueKind.Text,
                new WorkerTextValue(initialTargetName), "SetStringArg")
        ], []);
    }

    public static Api.TrackTapeMeasurementResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
