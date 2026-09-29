using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class DeleteMeasurementObservationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.delete_measurement_observation", "Delete Measurement Observation",
        "briosa.InstrumentOperations", "DeleteMeasurementObservation",
        "/briosa.InstrumentOperations/DeleteMeasurementObservation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteMeasurementObservationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
            new("Observation index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.ObservationIndex), "SetIntegerArg"),
            new("Delete point if no measurements remain?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.DeletePointIfNoMeasurementsRemain), "SetBoolArg")
        ], []);
    }

    public static Api.DeleteMeasurementObservationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
