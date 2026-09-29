using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportEmbeddedFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_embedded_file", "Export Embedded File", "briosa.FileOperations",
        "ExportEmbeddedFile", "/briosa.FileOperations/ExportEmbeddedFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportEmbeddedFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Embedded File Collection Name", WorkerMpValueKind.CollectionName,
                    CollectionNameMapper.Required(request.EmbeddedFileCollectionName, "embedded_file_collection_name"), "SetCollectionNameArg"),
                new("Embedded File Name", WorkerMpValueKind.Text, new WorkerTextValue(request.EmbeddedFileName), "SetStringArg"),
                new("External File Name", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.ExternalFileName, "external_file_name"), "SetFilePathArg"),
                new("Replace Existing?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.ReplaceExisting), "SetBoolArg")], []);
    }

    public static Api.ExportEmbeddedFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
