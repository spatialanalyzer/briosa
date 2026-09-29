using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetFoldersByWildcardOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_folders_by_wildcard", "Get Folders by Wildcard", "briosa.UtilityOperations",
        "GetFoldersByWildcard", "/briosa.UtilityOperations/GetFoldersByWildcard", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("folder_list", "Folder List", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.GetFoldersByWildcardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Search String", WorkerMpValueKind.Text,
                new WorkerTextValue(request.SearchString), "SetStringArg"),
            new("Case Sensitive Search", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasCaseSensitiveSearch || request.CaseSensitiveSearch), "SetBoolArg")
        ], [new("Folder List", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.GetFoldersByWildcardResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetFoldersByWildcardResult { Execution = completed.Details };
        result.FolderList.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
