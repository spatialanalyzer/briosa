using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class EnableDisableDatumAlignmentForFeatureCheckOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.enable_disable_datum_alignment_for_feature_check",
        "Enable/Disable Datum Alignment for Feature Check",
        "briosa.GdtOperations", "EnableDisableDatumAlignmentForFeatureCheck",
        "/briosa.GdtOperations/EnableDisableDatumAlignmentForFeatureCheck",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableDisableDatumAlignmentForFeatureCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        var alignment = CollectionItemNameMapper.Required(request.Alignment, "alignment");
        if (alignment.ItemType == WorkerItemTypeValue.Any)
            alignment = alignment with { ItemType = WorkerItemTypeValue.Alignment };

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2"),
            new("Enable Datum Alignment?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasEnableDatumAlignment ? request.EnableDatumAlignment : true), "SetBoolArg"),
            new("Enable Custom Initial Alignment?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.EnableCustomInitialAlignment), "SetBoolArg"),
            new("Enable Initial Datum Alignment?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasEnableInitialDatumAlignment ? request.EnableInitialDatumAlignment : true), "SetBoolArg"),
            new("Alignment", WorkerMpValueKind.CollectionItemName, alignment, "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.EnableDisableDatumAlignmentForFeatureCheckResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
