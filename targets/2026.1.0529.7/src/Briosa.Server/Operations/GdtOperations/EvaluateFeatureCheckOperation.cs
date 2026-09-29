using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class EvaluateFeatureCheckOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.evaluate_feature_check", "Evaluate Feature Check",
        "briosa.GdtOperations", "EvaluateFeatureCheck", "/briosa.GdtOperations/EvaluateFeatureCheck",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("check_evaluated", "Check Evaluated?", WorkerMpValueKind.Logical),
        new("check_result", "Check Result", WorkerMpValueKind.Text),
        new("non_unique_result", "Non-unique Result?", WorkerMpValueKind.Logical),
        new("measured_deviation_upper", "Measured Deviation (Upper)", WorkerMpValueKind.FloatingPoint),
        new("distance_out_of_tolerance_upper", "Distance Out of Tolerance (Upper)", WorkerMpValueKind.FloatingPoint),
        new("eval_delta_transform_upper", "Eval Delta Transform (Upper)", WorkerMpValueKind.WorldTransform),
        new("measured_deviation_lower", "Measured Deviation (Lower)", WorkerMpValueKind.FloatingPoint),
        new("distance_out_of_tolerance_lower", "Distance Out of Tolerance (Lower)", WorkerMpValueKind.FloatingPoint),
        new("eval_delta_transform_lower", "Eval Delta Transform (Lower)", WorkerMpValueKind.WorldTransform),
        new("check_type", "Check Type", WorkerMpValueKind.Text),
        new("tolerance_type", "Tolerance Type", WorkerMpValueKind.Text),
        new("tolerance_simple", "Tolerance, Simple", WorkerMpValueKind.FloatingPoint),
        new("tolerance_composite_upper", "Tolerance, Composite (Upper)", WorkerMpValueKind.FloatingPoint),
        new("tolerance_composite_lower", "Tolerance, Composite (Lower)", WorkerMpValueKind.FloatingPoint),
        new("tolerance_range_min", "Tolerance, Range (Min)", WorkerMpValueKind.FloatingPoint),
        new("tolerance_range_max", "Tolerance, Range (Max)", WorkerMpValueKind.FloatingPoint),
        new("tolerance_nominal_plus_minus_nominal", "Tolerance, NominalPlusMinus (Nominal)", WorkerMpValueKind.FloatingPoint),
        new("tolerance_nominal_plus_minus_minus", "Tolerance, NominalPlusMinus (Minus)", WorkerMpValueKind.FloatingPoint),
        new("tolerance_nominal_plus_minus_plus", "Tolerance, NominalPlusMinus (Plus)", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.EvaluateFeatureCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2"),
            new("Perform Evaluation?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasPerformEvaluation ? request.PerformEvaluation : true), "SetBoolArg"),
            new("Simultaneous Evaluation?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasSimultaneousEvaluation && request.SimultaneousEvaluation), "SetBoolArg")
        ],
        [
            new("Check Evaluated?", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Check Result", WorkerMpValueKind.Text, "GetStringArg"),
            new("Non-unique Result?", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Measured Deviation (Upper)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Distance Out of Tolerance (Upper)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Eval Delta Transform (Upper)", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg"),
            new("Measured Deviation (Lower)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Distance Out of Tolerance (Lower)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Eval Delta Transform (Lower)", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg"),
            new("Check Type", WorkerMpValueKind.Text, "GetStringArg"),
            new("Tolerance Type", WorkerMpValueKind.Text, "GetStringArg"),
            new("Tolerance, Simple", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Tolerance, Composite (Upper)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Tolerance, Composite (Lower)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Tolerance, Range (Min)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Tolerance, Range (Max)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Tolerance, NominalPlusMinus (Nominal)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Tolerance, NominalPlusMinus (Minus)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Tolerance, NominalPlusMinus (Plus)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.EvaluateFeatureCheckResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            CheckEvaluated = values[0].RequireValue<WorkerBooleanValue>().Value,
            CheckResult = values[1].RequireValue<WorkerTextValue>().Value,
            NonUniqueResult = values[2].RequireValue<WorkerBooleanValue>().Value,
            MeasuredDeviationUpper = values[3].RequireValue<WorkerDoubleValue>().Value,
            DistanceOutOfToleranceUpper = values[4].RequireValue<WorkerDoubleValue>().Value,
            EvalDeltaTransformUpper = ToProtocol(values[5].RequireValue<WorkerWorldTransformValue>()),
            MeasuredDeviationLower = values[6].RequireValue<WorkerDoubleValue>().Value,
            DistanceOutOfToleranceLower = values[7].RequireValue<WorkerDoubleValue>().Value,
            EvalDeltaTransformLower = ToProtocol(values[8].RequireValue<WorkerWorldTransformValue>()),
            CheckType = values[9].RequireValue<WorkerTextValue>().Value,
            ToleranceType = values[10].RequireValue<WorkerTextValue>().Value,
            ToleranceSimple = values[11].RequireValue<WorkerDoubleValue>().Value,
            ToleranceCompositeUpper = values[12].RequireValue<WorkerDoubleValue>().Value,
            ToleranceCompositeLower = values[13].RequireValue<WorkerDoubleValue>().Value,
            ToleranceRangeMin = values[14].RequireValue<WorkerDoubleValue>().Value,
            ToleranceRangeMax = values[15].RequireValue<WorkerDoubleValue>().Value,
            ToleranceNominalPlusMinusNominal = values[16].RequireValue<WorkerDoubleValue>().Value,
            ToleranceNominalPlusMinusMinus = values[17].RequireValue<WorkerDoubleValue>().Value,
            ToleranceNominalPlusMinusPlus = values[18].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }

    private static Api.WorldTransform ToProtocol(WorkerWorldTransformValue value) => new()
    {
        Transform = new Api.Transform { Values = { value.Transform.Values } },
        ScaleFactor = value.ScaleFactor
    };
}
