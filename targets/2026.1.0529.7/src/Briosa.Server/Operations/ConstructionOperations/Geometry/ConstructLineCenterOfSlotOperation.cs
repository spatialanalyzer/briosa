using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineCenterOfSlotOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_center_of_slot", "Construct Line Center of Slot",
        "briosa.ConstructionOperations", "ConstructLineCenterOfSlot", "/briosa.ConstructionOperations/ConstructLineCenterOfSlot",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineCenterOfSlotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Slot Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SlotName, "slot_name", WorkerObjectTypeValue.Slot), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructLineCenterOfSlotResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
