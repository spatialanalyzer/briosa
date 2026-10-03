using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportQdasCatalogFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_qdas_catalog_file", "Import QDAS Catalog File", "briosa.FileOperations",
        "ImportQdasCatalogFile", "/briosa.FileOperations/ImportQdasCatalogFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportQdasCatalogFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("QDAS DFD File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.QdasDfdFilePath, "qdas_dfd_file_path"), "SetFilePathArg")
        ], []);
    }

    public static Api.ImportQdasCatalogFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
