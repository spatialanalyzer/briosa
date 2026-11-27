using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MoveInstrumentToAnotherCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.move_instrument_to_another_collection", "Move Instrument to Another Collection",
        "briosa.InstrumentOperations", "MoveInstrumentToAnotherCollection", "/briosa.InstrumentOperations/MoveInstrumentToAnotherCollection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveInstrumentToAnotherCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CollectionName is null)
            throw RequestFieldViolation.Required("collection_name");
        if (string.IsNullOrWhiteSpace(request.CollectionName.Name))
            throw RequestFieldViolation.NotBlank("collection_name.name");

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Collection Name", WorkerMpValueKind.CollectionName,
                    new WorkerTextValue(request.CollectionName.Name), "SetCollectionNameArg")
            ], []);
    }

    public static Api.MoveInstrumentToAnotherCollectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}