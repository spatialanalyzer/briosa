using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportAsciiFramesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_ascii_frames", "Export ASCII Frames", "briosa.FileOperations", "ExportAsciiFrames",
        "/briosa.FileOperations/ExportAsciiFrames", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportAsciiFramesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("Object List", WorkerMpValueKind.CollectionObjectNameList, CollectionObjectNameMapper.RequiredList(request.ObjectList, "object_list"), "SetCollectionObjectNameRefListArg"),
            new("Export Frame Mode", WorkerMpValueKind.Text, new WorkerTextValue(request.HasExportFrameMode ? request.ExportFrameMode : "Fixed XYZ"), "SetStringArg"),
            new("Overwrite existing file?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasOverwriteExistingFile && request.OverwriteExistingFile), "SetBoolArg")
        ], []);
    }

    public static Api.ExportAsciiFramesResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
