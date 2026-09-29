using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportStlFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_stl_file", "Import STL File", "briosa.FileOperations", "ImportStlFile",
        "/briosa.FileOperations/ImportStlFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.ImportStlFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("STL File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.StlFilePath, "stl_file_path"), "SetFilePathArg"),
            new("Units", WorkerMpValueKind.DistanceUnit, DistanceUnitMapper.OrDefault(
                request.HasUnits ? request.Units : null, Api.DistanceUnits.Millimeters), "SetDistanceUnitsArg"),
            new("Import Mesh", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasImportMesh || request.ImportMesh), "SetBoolArg"),
            new("Import Point Cloud", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasImportPointCloud && request.ImportPointCloud), "SetBoolArg")
        ], []);
    }
    public static Api.ImportStlFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
