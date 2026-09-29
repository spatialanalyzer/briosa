using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportVdaFsFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_vda_fs_file", "Import VDA/FS File", "briosa.FileOperations", "ImportVdaFsFile",
        "/briosa.FileOperations/ImportVdaFsFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ImportVdaFsFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("VDA/FS File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.VdaFsFilePath, "vda_fs_file_path"), "SetFilePathArg")], []);
    }
    public static Api.ImportVdaFsFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
