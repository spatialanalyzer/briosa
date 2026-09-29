using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MoveMeasurementObservationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.move_measurement_observation", "Move Measurement Observation",
        "briosa.InstrumentOperations", "MoveMeasurementObservation",
        "/briosa.InstrumentOperations/MoveMeasurementObservation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveMeasurementObservationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SourcePointName, "source_point_name"), "SetPointNameArg"),
            new("Observation index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.ObservationIndex), "SetIntegerArg"),
            new("Delete point if no measurements remain?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.DeletePointIfNoMeasurementsRemain), "SetBoolArg"),
            new("Destination Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.DestinationPointName, "destination_point_name"), "SetPointNameArg"),
            new("Force observation to be active?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasForceObservationActive ? request.ForceObservationActive : true),
                "SetBoolArg")
        ], []);
    }

    public static Api.MoveMeasurementObservationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
