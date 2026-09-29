using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class MoveFolderToFolderOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.move_folder_to_folder", "Move Folder to Folder", "briosa.UtilityOperations",
        "MoveFolderToFolder", "/briosa.UtilityOperations/MoveFolderToFolder", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveFolderToFolderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Folder Path", WorkerMpValueKind.Text,
                new WorkerTextValue(request.SourceFolderPath), "SetStringArg"),
            new("Destination Folder Path", WorkerMpValueKind.Text,
                new WorkerTextValue(request.DestinationFolderPath), "SetStringArg")
        ], []);
    }

    public static Api.MoveFolderToFolderResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
