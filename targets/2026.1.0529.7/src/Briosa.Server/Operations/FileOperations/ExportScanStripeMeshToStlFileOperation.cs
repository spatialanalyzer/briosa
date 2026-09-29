using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportScanStripeMeshToStlFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_scan_stripe_mesh_to_stl_file", "Export Scan Stripe Mesh to STL File", "briosa.FileOperations", "ExportScanStripeMeshToStlFile",
        "/briosa.FileOperations/ExportScanStripeMeshToStlFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ExportScanStripeMeshToStlFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("STL File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.StlFilePath, "stl_file_path"), "SetFilePathArg"),
            new("Mesh", WorkerMpValueKind.CollectionObjectName, CollectionObjectNameMapper.Required(request.Mesh, "mesh"), "SetCollectionObjectNameArg2")
        ], []);
    }
    public static Api.ExportScanStripeMeshToStlFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
