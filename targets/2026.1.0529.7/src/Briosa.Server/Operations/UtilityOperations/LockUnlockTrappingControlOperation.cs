using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class LockUnlockTrappingControlOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.lock_unlock_trapping_control", "Lock/Unlock Trapping Control", "briosa.UtilityOperations",
        "LockUnlockTrappingControl", "/briosa.UtilityOperations/LockUnlockTrappingControl", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.LockUnlockTrappingControlRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Relationship Ref List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.RelationshipRefList, "relationship_ref_list"), "SetCollectionObjectNameRefListArg"),
            new("Feature Check Ref List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.FeatureCheckRefList, "feature_check_ref_list"), "SetCollectionObjectNameRefListArg"),
            new("Datum Ref List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.DatumRefList, "datum_ref_list"), "SetCollectionObjectNameRefListArg"),
            new("Lock Out Trapping?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.LockOutTrapping), "SetBoolArg")
        ], []);
    }

    public static Api.LockUnlockTrappingControlResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
