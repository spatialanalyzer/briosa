using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MoveObjectsIn6dUsingInstrumentUpdatesOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.move_objects_in_6d_using_instrument_updates", "Move Objects in 6D using Instrument Updates", "MoveObjectsIn6dUsingInstrumentUpdates");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveObjectsIn6dUsingInstrumentUpdatesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Objects to Move", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectsToMove, "objects_to_move"), "SetCollectionObjectNameRefListArg"),
                new("Measurement Mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasMeasurementMode ? request.MeasurementMode : string.Empty), "SetStringArg")
            ], []);
    }

    public static Api.MoveObjectsIn6dUsingInstrumentUpdatesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
