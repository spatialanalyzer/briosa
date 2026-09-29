using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SaveCurrentViewBmpJpgPngGifTiffOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.save_current_view_bmp_jpg_png_gif_tiff",
        "Save Current View (BMP/JPG/PNG/GIF/TIFF)", "briosa.ReportingOperations",
        "SaveCurrentViewBmpJpgPngGifTiff", "/briosa.ReportingOperations/SaveCurrentViewBmpJpgPngGifTiff",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SaveCurrentViewBmpJpgPngGifTiffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("File to save to", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.FileToSaveTo, "file_to_save_to"), "SetFilePathArg"),
            new("Render Scale Factor (1.0 uses window size)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasRenderScaleFactor ? request.RenderScaleFactor : 1d), "SetDoubleArg")
        ], []);
    }

    public static Api.SaveCurrentViewBmpJpgPngGifTiffResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
