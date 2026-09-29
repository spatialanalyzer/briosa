using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetFeatureCheckCylinderEvalOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_feature_check_cylinder_eval_options", "Get Feature Check Cylinder Eval Options",
        "briosa.GdtOperations", "GetFeatureCheckCylinderEvalOptions", "/briosa.GdtOperations/GetFeatureCheckCylinderEvalOptions",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("options.enable_actual_diameter_override", "Enable Actual Diameter Override", WorkerMpValueKind.Logical),
        new("options.actual_diameter_override", "Actual Diameter Override", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetFeatureCheckCylinderEvalOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2")],
            [
                new("Enable Actual Diameter Override", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Actual Diameter Override", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetFeatureCheckCylinderEvalOptionsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Options = new Api.FeatureCheckCylinderEvalOptions
            {
                EnableActualDiameterOverride = values[0].RequireValue<WorkerBooleanValue>().Value,
                ActualDiameterOverride = values[1].RequireValue<WorkerDoubleValue>().Value
            },
            Execution = completed.Details
        };
    }
}
