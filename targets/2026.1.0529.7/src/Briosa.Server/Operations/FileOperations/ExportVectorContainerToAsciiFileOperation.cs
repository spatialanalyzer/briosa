using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportVectorContainerToAsciiFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_vector_container_to_ascii_file", "Export Vector Container to ASCII File", "briosa.FileOperations", "ExportVectorContainerToAsciiFile",
        "/briosa.FileOperations/ExportVectorContainerToAsciiFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportVectorContainerToAsciiFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Ascii File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("Vector group(s) to export", WorkerMpValueKind.CollectionVectorGroupNameList, CollectionVectorGroupNameListMapper.RequiredList(request.VectorGroupsToExport, "vector_groups_to_export"), "SetCollectionVectorGroupNameRefListArg"),
            new("Overwrite existing file? (FALSE = Append)", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasOverwriteExistingFile || request.OverwriteExistingFile), "SetBoolArg"),
            new("Use Full Precision (Scientific Notation)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasUseFullPrecision && request.UseFullPrecision), "SetBoolArg"),
            new("Vector Name Format", WorkerMpValueKind.ExportVectorNameFormat, AsciiFileOperationValueMapper.RequiredVectorNameFormat(request.HasVectorNameFormat ? request.VectorNameFormat : null, "vector_name_format"), "SetExportVectorNameFormatArg"),
            new("Include Vector Length?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasIncludeVectorLength || request.IncludeVectorLength), "SetBoolArg")
        ], []);
    }

    public static Api.ExportVectorContainerToAsciiFileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
