using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportVdaFsFileEntireModelOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_vda_fs_file_entire_model", "Export VDA/FS File - Entire Model", "briosa.FileOperations", "ExportVdaFsFileEntireModel",
        "/briosa.FileOperations/ExportVdaFsFileEntireModel", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ExportVdaFsFileEntireModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("VDA/FS File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.VdaFsFilePath, "vda_fs_file_path"), "SetFilePathArg")], []);
    }
    public static Api.ExportVdaFsFileEntireModelResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
