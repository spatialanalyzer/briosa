using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetFeatureCheckCylinderEvalOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_feature_check_cylinder_eval_options", "Set Feature Check Cylinder Eval Options",
        "briosa.GdtOperations", "SetFeatureCheckCylinderEvalOptions", "/briosa.GdtOperations/SetFeatureCheckCylinderEvalOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetFeatureCheckCylinderEvalOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2"),
                new("Enable Actual Diameter Override", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasEnableActualDiameterOverride && request.EnableActualDiameterOverride), "SetBoolArg"),
                new("Actual Diameter Override", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasActualDiameterOverride ? request.ActualDiameterOverride : 0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetFeatureCheckCylinderEvalOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
