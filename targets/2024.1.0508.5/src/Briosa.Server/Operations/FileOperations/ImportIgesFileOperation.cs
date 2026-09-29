using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportIgesFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_iges_file", "Import IGES File", "briosa.FileOperations", "ImportIgesFile",
        "/briosa.FileOperations/ImportIgesFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ImportIgesFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("IGES File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.IgesFilePath, "iges_file_path"), "SetFilePathArg")], []);
    }
    public static Api.ImportIgesFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
