using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportSatFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_sat_file", "Import SAT File", "briosa.FileOperations", "ImportSatFile",
        "/briosa.FileOperations/ImportSatFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ImportSatFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("SAT File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.SatFilePath, "sat_file_path"), "SetFilePathArg")], []);
    }
    public static Api.ImportSatFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
