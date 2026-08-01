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

    public static Api.GetNumberOfCollectionsResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var totalCount = completed.Execution.OutputValues.Single(value =>
            value.Name == TotalCountArgumentName &&
            value.Kind == WorkerMpValueKind.WholeNumber);

        return new Api.GetNumberOfCollectionsResult
        {
            TotalCount = totalCount.IntegerValue!.Value,
            Execution = completed.Details
        };
    }
}
