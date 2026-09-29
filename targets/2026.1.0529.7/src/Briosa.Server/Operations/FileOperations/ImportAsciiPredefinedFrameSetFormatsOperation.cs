using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportAsciiPredefinedFrameSetFormatsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_ascii_predefined_frame_set_formats", "Import ASCII: Predefined Frame Set Formats", "briosa.FileOperations", "ImportAsciiPredefinedFrameSetFormats",
        "/briosa.FileOperations/ImportAsciiPredefinedFrameSetFormats", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportAsciiPredefinedFrameSetFormatsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("File Format", WorkerMpValueKind.AsciiImportFileFormat, AsciiFileOperationValueMapper.RequiredFileFormat(request.HasFileFormat ? request.FileFormat : null, "file_format"), "SetAsciiFileFormatArg"),
            new("Units", WorkerMpValueKind.DistanceUnit, DistanceUnitMapper.OrDefault(request.HasUnits ? request.Units : null, Api.DistanceUnits.Inches), "SetDistanceUnitsArg"),
            new("Angular Units", WorkerMpValueKind.AngularUnit, AsciiFileOperationValueMapper.AngularUnitsOrDefault(request.HasAngularUnits ? request.AngularUnits : null), "SetAngularUnitsArg"),
            new("Frame Set Container Name", WorkerMpValueKind.CollectionObjectName, CollectionObjectNameMapper.Required(request.FrameSetContainerName, "frame_set_container_name"), "SetCollectionObjectNameArg2"),
            new("Ensure Unique Name", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasEnsureUniqueName || request.EnsureUniqueName), "SetBoolArg")
        ], []);
    }

    public static Api.ImportAsciiPredefinedFrameSetFormatsResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
