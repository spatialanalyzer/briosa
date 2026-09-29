using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedEvaluateFeatureCheckTests
{
    private static readonly string[] InputBindings = ["SetCollectionObjectNameArg2", "SetBoolArg", "SetBoolArg"];
    private static readonly string[] OutputBindings =
    [
        "GetBoolArg", "GetStringArg", "GetBoolArg", "GetDoubleArg", "GetDoubleArg", "GetWorldTransformArg",
        "GetDoubleArg", "GetDoubleArg", "GetWorldTransformArg", "GetStringArg", "GetStringArg",
        "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg",
        "GetDoubleArg", "GetDoubleArg", "GetDoubleArg"
    ];

    [Fact]
    public void MapsFeatureCheckDefaultsBindingsAndCompleteResult()
    {
        var command = EvaluateFeatureCheckOperation.CreateCommand(new()
        {
            FeatureCheck = new() { CollectionName = "Checks", ItemName = "FC1" }
        });

        Assert.Equal(InputBindings, command.InputArguments.Select(argument => argument.SdkBinding));
        Assert.Equal(WorkerItemTypeValue.FeatureCheck,
            command.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal([true, false], command.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal(OutputBindings, command.OutputArguments.Select(argument => argument.SdkBinding));

        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, CreateOutputs(), "completed");
        var details = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 };
        var result = EvaluateFeatureCheckOperation.CreateResult(new SuccessfulOperationExecution(execution, details));

        Assert.True(result.CheckEvaluated);
        Assert.Equal("Pass", result.CheckResult);
        Assert.False(result.NonUniqueResult);
        Assert.Equal(1.1, result.MeasuredDeviationUpper);
        Assert.Equal(2.1, result.DistanceOutOfToleranceUpper);
        Assert.Equal(2.0, result.EvalDeltaTransformUpper.ScaleFactor);
        Assert.Equal(16, result.EvalDeltaTransformUpper.Transform.Values.Count);
        Assert.Equal(3.1, result.MeasuredDeviationLower);
        Assert.Equal(4.1, result.DistanceOutOfToleranceLower);
        Assert.Equal(3.0, result.EvalDeltaTransformLower.ScaleFactor);
        Assert.Equal("Diameter", result.CheckType);
        Assert.Equal("Simple", result.ToleranceType);
        Assert.Equal([5.1, 6.1, 7.1, 8.1, 9.1, 10.1, 11.1, 12.1],
            [result.ToleranceSimple, result.ToleranceCompositeUpper, result.ToleranceCompositeLower,
                result.ToleranceRangeMin, result.ToleranceRangeMax, result.ToleranceNominalPlusMinusNominal,
                result.ToleranceNominalPlusMinusMinus, result.ToleranceNominalPlusMinusPlus]);
        Assert.Same(details, result.Execution);

        var explicitValues = EvaluateFeatureCheckOperation.CreateCommand(new()
        {
            FeatureCheck = new() { ItemName = "FC1" },
            PerformEvaluation = false,
            SimultaneousEvaluation = true
        });
        Assert.Equal([false, true], explicitValues.InputArguments.Skip(1)
            .Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Throws<ArgumentException>(() => EvaluateFeatureCheckOperation.CreateCommand(new()));

        var id = EvaluateFeatureCheckOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }

    private static WorkerRetrievedOutput[] CreateOutputs() =>
    [
        new("Check Evaluated?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
        new("Check Result", WorkerMpValueKind.Text, new WorkerTextValue("Pass")),
        new("Non-unique Result?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
        new("Measured Deviation (Upper)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1.1)),
        new("Distance Out of Tolerance (Upper)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.1)),
        new("Eval Delta Transform (Upper)", WorkerMpValueKind.WorldTransform,
            new WorkerWorldTransformValue(new WorkerTransformValue(Enumerable.Range(1, 16).Select(value => (double)value).ToArray()), 2.0)),
        new("Measured Deviation (Lower)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3.1)),
        new("Distance Out of Tolerance (Lower)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4.1)),
        new("Eval Delta Transform (Lower)", WorkerMpValueKind.WorldTransform,
            new WorkerWorldTransformValue(new WorkerTransformValue(Enumerable.Range(1, 16).Select(value => (double)-value).ToArray()), 3.0)),
        new("Check Type", WorkerMpValueKind.Text, new WorkerTextValue("Diameter")),
        new("Tolerance Type", WorkerMpValueKind.Text, new WorkerTextValue("Simple")),
        new("Tolerance, Simple", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5.1)),
        new("Tolerance, Composite (Upper)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(6.1)),
        new("Tolerance, Composite (Lower)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(7.1)),
        new("Tolerance, Range (Min)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(8.1)),
        new("Tolerance, Range (Max)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(9.1)),
        new("Tolerance, NominalPlusMinus (Nominal)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(10.1)),
        new("Tolerance, NominalPlusMinus (Minus)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(11.1)),
        new("Tolerance, NominalPlusMinus (Plus)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(12.1))
    ];
}
