using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SaveCloudViewerPointCloudFileOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.save_cloud_viewer_point_cloud_file", "Save Point Cloud File", "SaveCloudViewerPointCloudFile");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SaveCloudViewerPointCloudFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")
        };
        if (request.HasFilePath)
            inputs.Add(new("File Path", WorkerMpValueKind.FileReference,
                new WorkerFileReferenceValue(request.FilePath, false), "SetFilePathArg"));
        inputs.Add(new("Save as Ascii", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.HasSaveAsAscii && request.SaveAsAscii), "SetBoolArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.SaveCloudViewerPointCloudFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
