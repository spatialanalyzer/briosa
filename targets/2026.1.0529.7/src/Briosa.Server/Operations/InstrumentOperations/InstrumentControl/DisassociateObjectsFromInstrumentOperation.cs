using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class DisassociateObjectsFromInstrumentOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.disassociate_objects_from_instrument", "Disassociate Objects from Instrument",
        "briosa.InstrumentOperations", "DisassociateObjectsFromInstrument", "/briosa.InstrumentOperations/DisassociateObjectsFromInstrument",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DisassociateObjectsFromInstrumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.DisassociateObjectsFromInstrumentResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}