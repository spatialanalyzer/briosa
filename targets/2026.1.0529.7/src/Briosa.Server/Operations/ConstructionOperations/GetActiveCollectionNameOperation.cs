using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

/// <summary>
/// Implements the exact "Get Active Collection Name" MP command contract.
/// </summary>
internal static class GetActiveCollectionNameOperation
{
    public const string OperationId =
        "construction_operations.get_active_collection_name";
    public const string StepName = "Get Active Collection Name";
    public const string CurrentlyActiveCollectionNameArgumentName =
        "Currently Active Collection Name";
    // Exact-target View SDK Code specifies GetStringArg. The prior-release
    // ObjectiveSA wrapper used GetCollectionNameArg, so it is not binding parity.
    public const string CurrentlyActiveCollectionNameGetter = "GetStringArg";

    public static OperationDescriptor Descriptor { get; } = new(
        OperationId,
        StepName,
        "briosa.ConstructionOperations",
        "GetActiveCollectionName",
        "/briosa.ConstructionOperations/GetActiveCollectionName",
        "read_only",
        Api.OperationExecutionScope.GlobalStateRead,
        Api.ReplaySafety.Safe,
        []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new(
            "currently_active_collection_name",
            CurrentlyActiveCollectionNameArgumentName,
            WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetActiveCollectionNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new WorkerMpCommand(
            OperationId,
            StepName,
            inputArguments: [],
            outputArguments:
            [
                new(
                    CurrentlyActiveCollectionNameArgumentName,
                    WorkerMpValueKind.Text,
                    CurrentlyActiveCollectionNameGetter)
            ]);
    }

    public static Api.GetActiveCollectionNameResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var activeCollectionName = completed.Execution.OutputValues.Single(value =>
            value.Name == CurrentlyActiveCollectionNameArgumentName &&
            value.Kind == WorkerMpValueKind.Text);

        return new Api.GetActiveCollectionNameResult
        {
            CurrentlyActiveCollectionName = activeCollectionName.StringValue!,
            Execution = completed.Details
        };
    }
}
