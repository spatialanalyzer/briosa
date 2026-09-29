using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportHiddenPointBarXmlFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_hidden_point_bar_xml_file", "Import Hidden Point Bar XML File",
        "briosa.FileOperations", "ImportHiddenPointBarXmlFile", "/briosa.FileOperations/ImportHiddenPointBarXmlFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportHiddenPointBarXmlFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("XML File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.XmlFilePath, "xml_file_path"), "SetFilePathArg"),
                new("Replace Existing Entries?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.ReplaceExistingEntries), "SetBoolArg")], []);
    }

    public static Api.ImportHiddenPointBarXmlFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
