using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class CaptureScreenToFileBmpJpgPngGifTiffOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.capture_screen_to_file_bmp_jpg_png_gif_tiff",
        "Capture Screen to File (BMP/JPG/PNG/GIF/TIFF)", "briosa.ReportingOperations",
        "CaptureScreenToFileBmpJpgPngGifTiff", "/briosa.ReportingOperations/CaptureScreenToFileBmpJpgPngGifTiff",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CaptureScreenToFileBmpJpgPngGifTiffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("File to save to", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileToSaveTo, "file_to_save_to"), "SetFilePathArg")], []);
    }

    public static Api.CaptureScreenToFileBmpJpgPngGifTiffResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
