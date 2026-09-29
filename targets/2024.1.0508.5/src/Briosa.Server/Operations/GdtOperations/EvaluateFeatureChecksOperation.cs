using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class EvaluateFeatureChecksOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.evaluate_feature_checks", "Evaluate Feature Checks",
        "briosa.GdtOperations", "EvaluateFeatureChecks", "/briosa.GdtOperations/EvaluateFeatureChecks",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("total_passed", "Total Passed", WorkerMpValueKind.WholeNumber),
        new("total_failed", "Total Failed", WorkerMpValueKind.WholeNumber),
        new("total_incomplete", "Total Incomplete", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.EvaluateFeatureChecksRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.FeatureCheckList, "feature_check_list"),
                "SetCollectionObjectNameRefListArg"),
            new("Simultaneous Evaluation?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.SimultaneousEvaluation), "SetBoolArg"),
            new("Restrict Evaluations To Listed Checks?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.RestrictEvaluationsToListedChecks), "SetBoolArg")
        ],
        [
            new("Total Passed", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Total Failed", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Total Incomplete", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }

    public static Api.EvaluateFeatureChecksResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            TotalPassed = values[0].RequireValue<WorkerIntegerValue>().Value,
            TotalFailed = values[1].RequireValue<WorkerIntegerValue>().Value,
            TotalIncomplete = values[2].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details
        };
    }
}
