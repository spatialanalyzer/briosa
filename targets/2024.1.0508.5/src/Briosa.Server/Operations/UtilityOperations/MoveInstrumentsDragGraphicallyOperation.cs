using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class MoveInstrumentsDragGraphicallyOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.move_instruments_drag_graphically", "Move Instruments Drag Graphically",
        "briosa.UtilityOperations", "MoveInstrumentsDragGraphically",
        "/briosa.UtilityOperations/MoveInstrumentsDragGraphically", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveInstrumentsDragGraphicallyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Instruments.Count == 0)
            throw new ArgumentException("Request field 'instruments' is required.", nameof(request));
        var instruments = new WorkerCollectionInstrumentIdListValue(request.Instruments
            .Select(item => new WorkerCollectionInstrumentIdValue(item.CollectionName, item.InstrumentId)).ToArray());
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instruments", WorkerMpValueKind.CollectionInstrumentIdList, instruments, "SetColInstIdRefListArg")], []);
    }

    public static Api.MoveInstrumentsDragGraphicallyResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
