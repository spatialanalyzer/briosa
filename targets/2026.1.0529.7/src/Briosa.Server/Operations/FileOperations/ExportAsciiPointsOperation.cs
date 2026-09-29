using System.Collections.Immutable;
using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportAsciiPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_ascii_points", "Export ASCII Points", "briosa.FileOperations", "ExportAsciiPoints",
        "/briosa.FileOperations/ExportAsciiPoints", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportAsciiPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.GroupNamesToExport.Count == 0)
            throw new ArgumentException("Request field 'group_names_to_export' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("Group Names to export", WorkerMpValueKind.CollectionGroupNameList, new WorkerCollectionGroupNameListValue(request.GroupNamesToExport.Select(value => new WorkerCollectionGroupNameValue(value.CollectionName, value.GroupName)).ToImmutableArray()), "SetCollectionGroupNameRefListArg"),
            new("Data Delimiter", WorkerMpValueKind.ExportDataDelimiterType, AsciiFileOperationValueMapper.RequiredDelimiter(request.HasDataDelimiter ? request.DataDelimiter : null, "data_delimiter"), "SetExportDataDelimeterTypeArg"),
            new("Target Name Format", WorkerMpValueKind.ExportTargetNameFormat, AsciiFileOperationValueMapper.RequiredTargetNameFormat(request.HasTargetNameFormat ? request.TargetNameFormat : null, "target_name_format"), "SetExportTargetNameFormatArg"),
            new("Desired Coordinate System", WorkerMpValueKind.CoordinateSystemType, new WorkerChoiceValue<WorkerCoordinateSystemTypeValue>(CoordinateSystemTypeMapper.OrDefault(request.HasDesiredCoordinateSystem ? request.DesiredCoordinateSystem : null, Api.CoordinateSystemType.Unspecified)), "SetCoordinateSystemTypeArg"),
            new("Include Target Offsets?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeTargetOffsets && request.IncludeTargetOffsets), "SetBoolArg"),
            new("Include Target Comments?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeTargetComments && request.IncludeTargetComments), "SetBoolArg"),
            new("Include Timestamps?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeTimestamps && request.IncludeTimestamps), "SetBoolArg"),
            new("Include Tolerances?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeTolerances && request.IncludeTolerances), "SetBoolArg"),
            new("Include Coordinate Uncertainties?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeCoordinateUncertainties && request.IncludeCoordinateUncertainties), "SetBoolArg"),
            new("Include SA version and frame comments?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeSaVersionAndFrameComments && request.IncludeSaVersionAndFrameComments), "SetBoolArg"),
            new("Include Axis Comments?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeAxisComments && request.IncludeAxisComments), "SetBoolArg"),
            new("Include Export Format Info?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeExportFormatInfo && request.IncludeExportFormatInfo), "SetBoolArg"),
            new("Include Weights?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeWeights && request.IncludeWeights), "SetBoolArg"),
            new("Include Measurement Details?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasIncludeMeasurementDetails && request.IncludeMeasurementDetails), "SetBoolArg"),
            new("Maximum Precision (Scientific Notation)?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasMaximumPrecision && request.MaximumPrecision), "SetBoolArg"),
            new("Decimal Precision", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasDecimalPrecision ? request.DecimalPrecision : 6), "SetIntegerArg"),
            new("Append?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAppend && request.Append), "SetBoolArg")
        ], []);
    }

    public static Api.ExportAsciiPointsResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
