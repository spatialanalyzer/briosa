using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportVdaFsFilePartialModelOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_vda_fs_file_partial_model", "Export VDA/FS File - Partial Model", "briosa.FileOperations", "ExportVdaFsFilePartialModel",
        "/briosa.FileOperations/ExportVdaFsFilePartialModel", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ExportVdaFsFilePartialModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("VDA/FS File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.VdaFsFilePath, "vda_fs_file_path"), "SetFilePathArg"),
            new("Object Name List", WorkerMpValueKind.CollectionObjectNameList, CollectionObjectNameMapper.RequiredList(request.ObjectNameList, "object_name_list"), "SetCollectionObjectNameRefListArg")
        ], []);
    }
    public static Api.ExportVdaFsFilePartialModelResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
