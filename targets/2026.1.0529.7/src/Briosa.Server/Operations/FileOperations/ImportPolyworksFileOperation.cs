using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportPolyworksFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_polyworks_file", "Import Polyworks File", "briosa.FileOperations", "ImportPolyworksFile",
        "/briosa.FileOperations/ImportPolyworksFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ImportPolyworksFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Name", WorkerMpValueKind.CollectionObjectName, CollectionObjectNameMapper.Required(request.CloudName, "cloud_name"), "SetCollectionObjectNameArg2"),
            new("File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")
        ], []);
    }
    public static Api.ImportPolyworksFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
