using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetObservationStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_observation_status", "Set Observation Status",
        "briosa.InstrumentOperations", "SetObservationStatus",
        "/briosa.InstrumentOperations/SetObservationStatus",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetObservationStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var observationIndex = request.HasObservationIndex ? request.ObservationIndex : 0;
        var active = request.HasActive && request.Active;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg"),
            new("Observation Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(observationIndex), "SetIntegerArg"),
            new("Active?", WorkerMpValueKind.Logical, new WorkerBooleanValue(active), "SetBoolArg")
        ], []);
    }

    public static Api.SetObservationStatusResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
