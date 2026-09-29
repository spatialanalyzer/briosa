using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class AsciiFileOperationValueMapper
{
    public static WorkerChoiceValue<WorkerAsciiImportFileFormatValue> RequiredFileFormat(
        Api.AsciiFileFormat? value, string fieldName)
    {
        var ordinal = (int?)value ?? 0;
        // The exact-target protobuf choices are contiguous and match the worker values by ordinal.
        if (ordinal is < 1 or > 44)
            throw new ArgumentException($"Request field '{fieldName}' is required or unsupported.", nameof(value));
        return new((WorkerAsciiImportFileFormatValue)(ordinal - 1));
    }

    public static WorkerChoiceValue<WorkerExportDataDelimiterTypeValue> RequiredDelimiter(
        Api.ExportDataDelimeterType? value, string fieldName) => value switch
    {
        Api.ExportDataDelimeterType.Space => new(WorkerExportDataDelimiterTypeValue.Space),
        Api.ExportDataDelimeterType.Comma => new(WorkerExportDataDelimiterTypeValue.Comma),
        Api.ExportDataDelimeterType.Tab => new(WorkerExportDataDelimiterTypeValue.Tab),
        _ => throw new ArgumentException($"Request field '{fieldName}' is required or unsupported.", nameof(value))
    };

    public static WorkerChoiceValue<WorkerExportTargetNameFormatValue> RequiredTargetNameFormat(
        Api.ExportTargetNameFormat? value, string fieldName) => value switch
    {
        Api.ExportTargetNameFormat.CollectionGroupTarget => new(WorkerExportTargetNameFormatValue.CollectionGroupTarget),
        Api.ExportTargetNameFormat.GroupTarget => new(WorkerExportTargetNameFormatValue.GroupTarget),
        Api.ExportTargetNameFormat.Target => new(WorkerExportTargetNameFormatValue.Target),
        Api.ExportTargetNameFormat.None => new(WorkerExportTargetNameFormatValue.None),
        _ => throw new ArgumentException($"Request field '{fieldName}' is required or unsupported.", nameof(value))
    };

    public static WorkerChoiceValue<WorkerExportVectorNameFormatValue> RequiredVectorNameFormat(
        Api.ExportVectorNameFormat? value, string fieldName) => value switch
    {
        Api.ExportVectorNameFormat.CollectionGroupVector => new(WorkerExportVectorNameFormatValue.CollectionGroupVector),
        Api.ExportVectorNameFormat.GroupVector => new(WorkerExportVectorNameFormatValue.GroupVector),
        Api.ExportVectorNameFormat.Vector => new(WorkerExportVectorNameFormatValue.Vector),
        Api.ExportVectorNameFormat.None => new(WorkerExportVectorNameFormatValue.None),
        _ => throw new ArgumentException($"Request field '{fieldName}' is required or unsupported.", nameof(value))
    };

    public static WorkerAngularUnitChoice AngularUnitsOrDefault(Api.AngularUnits? value) =>
        new((value ?? Api.AngularUnits.Degrees) switch
        {
            Api.AngularUnits.Degrees => WorkerAngularUnitValue.Degrees,
            Api.AngularUnits.DegreesMinutesSeconds => WorkerAngularUnitValue.DegreesMinutesSeconds,
            Api.AngularUnits.Radians => WorkerAngularUnitValue.Radians,
            Api.AngularUnits.Milliradians => WorkerAngularUnitValue.Milliradians,
            Api.AngularUnits.GonsGrad => WorkerAngularUnitValue.GonsGrad,
            Api.AngularUnits.Mils => WorkerAngularUnitValue.Mils,
            Api.AngularUnits.Arcseconds => WorkerAngularUnitValue.Arcseconds,
            Api.AngularUnits.DegreesMinutes => WorkerAngularUnitValue.DegreesMinutes,
            _ => throw new ArgumentException("Angular units are not supported by this SA target.", nameof(value))
        });
}
