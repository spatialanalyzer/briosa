using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFoldersOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_folders", "Construct Folder(s)",
        "briosa.ConstructionOperations", "ConstructFolders", "/briosa.ConstructionOperations/ConstructFolders",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFoldersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Folder Path", WorkerMpValueKind.Text, new WorkerTextValue(request.FolderPath), "SetStringArg")], []);
    }

    public static Api.ConstructFoldersResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
