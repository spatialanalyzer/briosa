using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GenerateFeatureCheckSummaryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.generate_feature_check_summary", "Generate Feature Check Summary",
        "briosa.GdtOperations", "GenerateFeatureCheckSummary", "/briosa.GdtOperations/GenerateFeatureCheckSummary",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GenerateFeatureCheckSummaryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.FeatureCheckList, "feature_check_list"),
                "SetCollectionObjectNameRefListArg"),
            new("Summary Table Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasSummaryTableName
                    ? request.SummaryTableName : "GDT Feature Check Summary"), "SetStringArg")
        ], []);
    }

    public static Api.GenerateFeatureCheckSummaryResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
