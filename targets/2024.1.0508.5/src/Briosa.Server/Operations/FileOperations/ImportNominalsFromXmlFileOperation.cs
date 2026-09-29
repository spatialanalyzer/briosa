using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportNominalsFromXmlFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_nominals_from_xml_file", "Import Nominals from XML File",
        "briosa.FileOperations", "ImportNominalsFromXmlFile", "/briosa.FileOperations/ImportNominalsFromXmlFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportNominalsFromXmlFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FilePath, "file_path"), "SetFilePathArg")], []);
    }

    public static Api.ImportNominalsFromXmlFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
