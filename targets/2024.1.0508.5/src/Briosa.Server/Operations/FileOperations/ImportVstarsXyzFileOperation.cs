using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportVstarsXyzFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_vstars_xyz_file", "Import VSTARS .xyz File", "briosa.FileOperations",
        "ImportVstarsXyzFile", "/briosa.FileOperations/ImportVstarsXyzFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportVstarsXyzFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")], []);
    }

    public static Api.ImportVstarsXyzFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
