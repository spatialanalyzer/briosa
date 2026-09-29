using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class WatchPointToPointWithViewZoomingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.watch_point_to_point_with_view_zooming",
        "Watch Point To Point With View Zooming",
        "briosa.InstrumentOperations", "WatchPointToPointWithViewZooming",
        "/briosa.InstrumentOperations/WatchPointToPointWithViewZooming",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.WatchPointToPointWithViewZoomingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Reference Point", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.ReferencePoint, "reference_point"), "SetPointNameArg"),
                new("Update(TRUE),Close(FALSE)", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasUpdate || request.Update), "SetBoolArg")
            ], []);
    }

    public static Api.WatchPointToPointWithViewZoomingResult CreateResult(
        SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
