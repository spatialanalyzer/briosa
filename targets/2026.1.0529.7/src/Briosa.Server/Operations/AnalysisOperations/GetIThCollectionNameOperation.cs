using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

/// <summary>
/// Implements the exact "Get i-th Collection Name" MP command contract.
/// </summary>
internal static class GetIThCollectionNameOperation
{
    public const string OperationId = "analysis_operations.get_i_th_collection_name";
    public const string StepName = "Get i-th Collection Name";
    public const string CollectionIndexArgumentName = "Collection Index";
    public const string CollectionIndexSetter = "SetIntegerArg";
    public const string ResultantNameArgumentName = "Resultant Name";
    public const string ResultantNameGetter = "GetCollectionNameArg";

    public static OperationDescriptor Descriptor { get; } = new(
        OperationId,
        StepName,
        "briosa.AnalysisOperations",
        "GetIThCollectionName",
        "/briosa.AnalysisOperations/GetIThCollectionName",
        "read_only",
        Api.OperationExecutionScope.GlobalStateRead,
        Api.ReplaySafety.Safe,
        []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_name", ResultantNameArgumentName, WorkerMpValueKind.CollectionName)];

    public static WorkerMpCommand CreateCommand(Api.GetIThCollectionNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasCollectionIndex)
        {
            throw new ArgumentException(
                "Collection Index must be present.",
                nameof(request));
        }

        return new WorkerMpCommand(
            OperationId,
            StepName,
            inputArguments:
            [
                new(
                    CollectionIndexArgumentName,
                    WorkerMpValueKind.WholeNumber,
                    IntegerValue: request.CollectionIndex,
                    SdkBinding: CollectionIndexSetter)
            ],
            outputArguments:
            [
                new(
                    ResultantNameArgumentName,
                    WorkerMpValueKind.CollectionName,
                    ResultantNameGetter)
            ]);
    }

    public static Api.GetIThCollectionNameResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var resultantName = completed.Execution.OutputValues.Single(value =>
            value.Name == ResultantNameArgumentName &&
            value.Kind == WorkerMpValueKind.CollectionName);
        return new Api.GetIThCollectionNameResult
        {
            ResultantName = resultantName.StringValue!,
            Execution = completed.Details
        };
    }
}
