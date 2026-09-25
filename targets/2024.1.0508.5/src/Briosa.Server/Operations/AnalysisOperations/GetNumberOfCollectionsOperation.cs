using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

/// <summary>
/// Implements the exact "Get Number of Collections" MP command contract.
/// </summary>
internal static class GetNumberOfCollectionsOperation
{
    public const string OperationId = "analysis_operations.get_number_of_collections";
    public const string StepName = "Get Number of Collections";
    public const string TotalCountArgumentName = "Total Count";
    public const string TotalCountGetter = "GetIntegerArg";

    public static OperationDescriptor Descriptor { get; } = new(
        OperationId,
        StepName,
        "briosa.AnalysisOperations",
        "GetNumberOfCollections",
        "/briosa.AnalysisOperations/GetNumberOfCollections",
        "read_only",
        Api.OperationExecutionScope.GlobalStateRead,
        Api.ReplaySafety.Safe,
        []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_count", TotalCountArgumentName, WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetNumberOfCollectionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new WorkerMpCommand(
            OperationId,
            StepName,
            inputArguments: [],
            outputArguments:
            [
                new(
                    TotalCountArgumentName,
                    WorkerMpValueKind.WholeNumber,
                    TotalCountGetter)
            ]);
    }

    // OperationExecutor validates ordered output shape and retrieval before mapping.
    public static Api.GetNumberOfCollectionsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TotalCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
