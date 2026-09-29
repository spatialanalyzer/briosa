using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DeleteFoldersByWildcardOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.delete_folders_by_wildcard", "Delete Folders by Wildcard",
        "briosa.ConstructionOperations", "DeleteFoldersByWildcard", "/briosa.ConstructionOperations/DeleteFoldersByWildcard",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("num_deleted", "Num Deleted", WorkerMpValueKind.WholeNumber),
        new("num_failed", "Num Failed", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.DeleteFoldersByWildcardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Search String", WorkerMpValueKind.Text, new WorkerTextValue(request.SearchString), "SetStringArg"),
            new("Case Sensitive Search", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasCaseSensitiveSearch ? request.CaseSensitiveSearch : true), "SetBoolArg"),
            new("Allow Deleting all Folders", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AllowDeletingAllFolders), "SetBoolArg")
        ],
        [
            new("Num Deleted", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Num Failed", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }

    public static Api.DeleteFoldersByWildcardResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            NumDeleted = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
            NumFailed = completed.Execution.OutputValues[1].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details
        };
}
