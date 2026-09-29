using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class DeleteGeneralFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.delete_general_file", "Delete General File", "briosa.FileOperations", "DeleteGeneralFile",
        "/briosa.FileOperations/DeleteGeneralFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteGeneralFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileName, "file_name"), "SetFilePathArg")], []);
    }

    public static Api.DeleteGeneralFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
