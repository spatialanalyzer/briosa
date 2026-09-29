using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AssociateObjectsWithInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.associate_objects_with_instrument", "Associate Objects with Instrument",
        "briosa.InstrumentOperations", "AssociateObjectsWithInstrument", "/briosa.InstrumentOperations/AssociateObjectsWithInstrument",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AssociateObjectsWithInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg")
            ], []);
    }

    public static Api.AssociateObjectsWithInstrumentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}