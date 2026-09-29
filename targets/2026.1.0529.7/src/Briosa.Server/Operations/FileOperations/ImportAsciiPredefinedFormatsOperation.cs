using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportAsciiPredefinedFormatsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_ascii_predefined_formats", "Import ASCII: Predefined Formats", "briosa.FileOperations", "ImportAsciiPredefinedFormats",
        "/briosa.FileOperations/ImportAsciiPredefinedFormats", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportAsciiPredefinedFormatsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("File Format", WorkerMpValueKind.AsciiImportFileFormat, AsciiFileOperationValueMapper.RequiredFileFormat(request.HasFileFormat ? request.FileFormat : null, "file_format"), "SetAsciiFileFormatArg"),
            new("Units", WorkerMpValueKind.DistanceUnit, DistanceUnitMapper.OrDefault(request.HasUnits ? request.Units : null, Api.DistanceUnits.Inches), "SetDistanceUnitsArg"),
            new("Angular Units", WorkerMpValueKind.AngularUnit, AsciiFileOperationValueMapper.AngularUnitsOrDefault(request.HasAngularUnits ? request.AngularUnits : null), "SetAngularUnitsArg"),
            new("Group Name", WorkerMpValueKind.CollectionObjectName, CollectionObjectNameMapper.Required(request.GroupName, "group_name"), "SetCollectionObjectNameArg2"),
            new("Import as Cloud", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasImportAsCloud && request.ImportAsCloud), "SetBoolArg"),
            new("Ensure New Point Group", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasEnsureNewPointGroup || request.EnsureNewPointGroup), "SetBoolArg"),
            new("Ensure Unique Names", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasEnsureUniqueNames || request.EnsureUniqueNames), "SetBoolArg")
        ], []);
    }

    public static Api.ImportAsciiPredefinedFormatsResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
