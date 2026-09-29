using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportAsciiPointSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_ascii_point_set", "Export ASCII Point Set", "briosa.FileOperations", "ExportAsciiPointSet",
        "/briosa.FileOperations/ExportAsciiPointSet", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportAsciiPointSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("Point Set Container", WorkerMpValueKind.CollectionObjectName, CollectionObjectNameMapper.Required(request.PointSetContainer, "point_set_container"), "SetCollectionObjectNameArg2"),
            new("Data Delimiter", WorkerMpValueKind.ExportDataDelimiterType, AsciiFileOperationValueMapper.RequiredDelimiter(request.HasDataDelimiter ? request.DataDelimiter : null, "data_delimiter"), "SetExportDataDelimeterTypeArg"),
            new("Target Name Format", WorkerMpValueKind.ExportTargetNameFormat, AsciiFileOperationValueMapper.RequiredTargetNameFormat(request.HasTargetNameFormat ? request.TargetNameFormat : null, "target_name_format"), "SetExportTargetNameFormatArg"),
            new("Desired Coordinate System", WorkerMpValueKind.CoordinateSystemType, new WorkerChoiceValue<WorkerCoordinateSystemTypeValue>(CoordinateSystemTypeMapper.OrDefault(request.HasDesiredCoordinateSystem ? request.DesiredCoordinateSystem : null, Api.CoordinateSystemType.Unspecified)), "SetCoordinateSystemTypeArg"),
            new("Include Target Offsets?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeTargetOffsets && request.IncludeTargetOffsets), "SetBoolArg"),
            new("Include Timestamps?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeTimestamps && request.IncludeTimestamps), "SetBoolArg"),
            new("Include SA version and frame comments?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeSaVersionAndFrameComments && request.IncludeSaVersionAndFrameComments), "SetBoolArg"),
            new("Include Axis Comments?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeAxisComments && request.IncludeAxisComments), "SetBoolArg"),
            new("Include Export Format Info?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeExportFormatInfo && request.IncludeExportFormatInfo), "SetBoolArg"),
            new("Maximum Precision (Scientific Notation)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasMaximumPrecision && request.MaximumPrecision), "SetBoolArg"),
            new("Decimal Precision", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasDecimalPrecision ? request.DecimalPrecision : 6), "SetIntegerArg"),
            new("Append?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAppend && request.Append), "SetBoolArg")
        ], []);
    }

    public static Api.ExportAsciiPointSetResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
