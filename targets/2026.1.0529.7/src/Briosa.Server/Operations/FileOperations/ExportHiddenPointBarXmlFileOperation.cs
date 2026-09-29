using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportHiddenPointBarXmlFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_hidden_point_bar_xml_file", "Export Hidden Point Bar XML File",
        "briosa.FileOperations", "ExportHiddenPointBarXmlFile", "/briosa.FileOperations/ExportHiddenPointBarXmlFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportHiddenPointBarXmlFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("XML File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.XmlFilePath, "xml_file_path"), "SetFilePathArg")], []);
    }

    public static Api.ExportHiddenPointBarXmlFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
