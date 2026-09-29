using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportAsciiFrameSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_ascii_frame_set", "Export ASCII Frame Set", "briosa.FileOperations", "ExportAsciiFrameSet",
        "/briosa.FileOperations/ExportAsciiFrameSet", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportAsciiFrameSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("Frame Set Container", WorkerMpValueKind.CollectionObjectName, CollectionObjectNameMapper.Required(request.FrameSetContainer, "frame_set_container"), "SetCollectionObjectNameArg2"),
            new("Data Delimiter", WorkerMpValueKind.ExportDataDelimiterType, AsciiFileOperationValueMapper.RequiredDelimiter(request.HasDataDelimiter ? request.DataDelimiter : null, "data_delimiter"), "SetExportDataDelimeterTypeArg"),
            new("File Format", WorkerMpValueKind.AsciiImportFileFormat, AsciiFileOperationValueMapper.RequiredFileFormat(request.HasFileFormat ? request.FileFormat : null, "file_format"), "SetAsciiFileFormatArg"),
            new("Include Export Format Info?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeExportFormatInfo && request.IncludeExportFormatInfo), "SetBoolArg"),
            new("Decimal Precision", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasDecimalPrecision ? request.DecimalPrecision : 6), "SetIntegerArg"),
            new("Append?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAppend && request.Append), "SetBoolArg")
        ], []);
    }

    public static Api.ExportAsciiFrameSetResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
