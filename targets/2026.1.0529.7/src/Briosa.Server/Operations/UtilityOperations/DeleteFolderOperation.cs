using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class DeleteFolderOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.delete_folder", "Delete Folder", "briosa.UtilityOperations",
        "DeleteFolder", "/briosa.UtilityOperations/DeleteFolder", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteFolderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Folder Path", WorkerMpValueKind.Text,
                RequiredValueMapper.NonEmptyText(request.HasFolderPath, request.FolderPath, "folder_path"),
                "SetStringArg")], []);
    }

    public static Api.DeleteFolderResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
