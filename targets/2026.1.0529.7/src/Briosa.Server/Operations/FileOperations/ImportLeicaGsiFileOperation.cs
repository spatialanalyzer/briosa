using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportLeicaGsiFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_leica_gsi_file", "Import Leica GSI File", "briosa.FileOperations",
        "ImportLeicaGsiFile", "/briosa.FileOperations/ImportLeicaGsiFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportLeicaGsiFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.InstrumentId, "instrument_id"), "SetColInstIdArg"),
                new("Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.GroupName, "group_name"), "SetCollectionObjectNameArg2"),
                new("File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")], []);
    }

    public static Api.ImportLeicaGsiFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
