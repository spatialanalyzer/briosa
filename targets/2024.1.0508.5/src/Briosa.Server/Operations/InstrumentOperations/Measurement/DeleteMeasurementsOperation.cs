using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class DeleteMeasurementsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.delete_measurements", "Delete Measurements",
        "briosa.InstrumentOperations", "DeleteMeasurements", "/briosa.InstrumentOperations/DeleteMeasurements",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteMeasurementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
            new("Delete point if no measurements remain?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.DeletePointIfNoMeasurementsRemain), "SetBoolArg")
        ], []);
    }

    public static Api.DeleteMeasurementsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
