using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class MoveCollectionToFolderOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.move_collection_to_folder", "Move Collection to Folder", "briosa.UtilityOperations",
        "MoveCollectionToFolder", "/briosa.UtilityOperations/MoveCollectionToFolder", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MoveCollectionToFolderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Collection is null || string.IsNullOrWhiteSpace(request.Collection.Name))
            throw new ArgumentException("Collection is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection", WorkerMpValueKind.CollectionName,
                new WorkerTextValue(request.Collection.Name), "SetCollectionNameArg"),
            new("Folder Path", WorkerMpValueKind.Text,
                new WorkerTextValue(request.FolderPath), "SetStringArg")
        ], []);
    }

    public static Api.MoveCollectionToFolderResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
