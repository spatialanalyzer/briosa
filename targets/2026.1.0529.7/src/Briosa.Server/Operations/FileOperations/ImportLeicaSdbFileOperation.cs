using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportLeicaSdbFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_leica_sdb_file", "Import Leica SDB File", "briosa.FileOperations",
        "ImportLeicaSdbFile", "/briosa.FileOperations/ImportLeicaSdbFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportLeicaSdbFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.InstrumentId, "instrument_id"), "SetColInstIdArg"),
                new("Scan Cloud Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ScanCloudName, "scan_cloud_name"), "SetCollectionObjectNameArg2"),
                new("File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")], []);
    }

    public static Api.ImportLeicaSdbFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
