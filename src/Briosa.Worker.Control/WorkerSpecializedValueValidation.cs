namespace Briosa.Worker.Control;

internal static class WorkerSpecializedValueValidation
{
    public static bool HasInputValueForKind(WorkerMpInputArgument argument) =>
        argument.Kind switch
        {
            WorkerMpValueKind.AsciiFileFormat => IsEnum(argument, 44),
            WorkerMpValueKind.AxisIdentifier => IsEnum(argument, 9),
            WorkerMpValueKind.BaseColorType => IsEnum(argument, 3),
            WorkerMpValueKind.BaseMidColorType => IsEnum(argument, 4),
            WorkerMpValueKind.ChartType => IsEnum(argument, 3),
            WorkerMpValueKind.CollimationBaselineType => IsEnum(argument, 3),
            WorkerMpValueKind.CollimationType => IsEnum(argument, 2),
            WorkerMpValueKind.ColorRangeMethod => IsEnum(argument, 6),
            WorkerMpValueKind.CoordinateSystemType => IsEnum(argument, 3),
            WorkerMpValueKind.DatasetType => IsEnum(argument, 4),
            WorkerMpValueKind.DynamicCircleMode => IsEnum(argument, 7),
            WorkerMpValueKind.DynamicEllipseMode => IsEnum(argument, 2),
            WorkerMpValueKind.DynamicLineMode => IsEnum(argument, 5),
            WorkerMpValueKind.DynamicPlaneMode => IsEnum(argument, 8),
            WorkerMpValueKind.DynamicPointMode => IsEnum(argument, 5),
            WorkerMpValueKind.EdgeMode => IsEnum(argument, 3),
            WorkerMpValueKind.ExportDataDelimiterType => IsEnum(argument, 3),
            WorkerMpValueKind.ExportTargetNameFormat => IsEnum(argument, 4),
            WorkerMpValueKind.ExportVectorNameFormat => IsEnum(argument, 4),
            WorkerMpValueKind.GeometryType => IsEnum(argument, 10),
            WorkerMpValueKind.InstrumentType => IsEnum(argument, 40),
            WorkerMpValueKind.ObjectType => IsEnum(argument, 25),
            WorkerMpValueKind.OffsetDirectionType => IsEnum(argument, 3),
            WorkerMpValueKind.PointFilterInputType => IsEnum(argument, 3),
            WorkerMpValueKind.RelationshipWeightingMode => IsEnum(argument, 5),
            WorkerMpValueKind.RenderModeType => IsEnum(argument, 4),
            WorkerMpValueKind.ReportPageOrientation => IsEnum(argument, 2),
            WorkerMpValueKind.SaturationLimitType => IsEnum(argument, 3),
            WorkerMpValueKind.ShowUsmnDialogType => IsEnum(argument, 3),
            WorkerMpValueKind.SurfaceAnalysisMode => IsEnum(argument, 11),
            WorkerMpValueKind.SurfaceDissectionModeType => IsEnum(argument, 2),
            WorkerMpValueKind.TargetComputationMethod => IsEnum(argument, 6),
            WorkerMpValueKind.TranslucencyType => IsEnum(argument, 3),
            WorkerMpValueKind.AutoFilterProximitySettings =>
                IsValid(argument.AutoFilterProximitySettingsValue),
            WorkerMpValueKind.CloudThinningOptions =>
                IsValid(argument.CloudThinningOptionsValue),
            WorkerMpValueKind.ColorizationOptions =>
                IsValid(argument.ColorizationOptionsValue),
            WorkerMpValueKind.FitConstraintScalarOptions =>
                IsValid(argument.FitConstraintScalarOptionsValue),
            WorkerMpValueKind.FitDegreeOfFreedomOptions =>
                argument.FitDegreeOfFreedomOptionsValue is not null,
            WorkerMpValueKind.PointDeltaReportOptions =>
                IsValid(argument.PointDeltaReportOptionsValue),
            WorkerMpValueKind.ProjectionOptions =>
                IsValid(argument.ProjectionOptionsValue),
            WorkerMpValueKind.ReportOutputOptions =>
                IsValid(argument.ReportOutputOptionsValue),
            WorkerMpValueKind.ReportViewOptions =>
                IsValid(argument.ReportViewOptionsValue),
            WorkerMpValueKind.ToleranceScalarOptions =>
                IsValid(argument.ToleranceScalarOptionsValue),
            _ => false
        };

    public static bool HasOutputValueForKind(WorkerMpOutputValue output) =>
        output.Kind switch
        {
            WorkerMpValueKind.FitConstraintScalarOptions =>
                IsValid(output.FitConstraintScalarOptionsValue),
            WorkerMpValueKind.ToleranceScalarOptions =>
                IsValid(output.ToleranceScalarOptionsValue),
            _ => false
        };

    private static bool IsEnum(WorkerMpInputArgument argument, int valueCount) =>
        argument.SpecializedEnumValue is { } value &&
        (uint)value.Value < (uint)valueCount;

    private static bool IsValid(WorkerAutoFilterProximitySettingsValue? value) =>
        value is not null &&
        IsEnumValue(value.SurfaceProximityMode, 3) &&
        IsEnumValue(value.PlanarProximityMode, 3) &&
        IsEnumValue(value.RadialProximityMode, 3);

    private static bool IsValid(WorkerCloudThinningOptionsValue? value) =>
        value is not null && IsEnumValue(value.Mode, 3);

    private static bool IsValid(WorkerColorizationOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.ColorRangeMethod, 6) &&
        IsEnumValue(value.BaseHighColor, 3) &&
        IsEnumValue(value.BaseMidColor, 4) &&
        IsEnumValue(value.BaseLowColor, 3);

    private static bool IsValid(WorkerFitConstraintScalarOptionsValue? value) =>
        value is { High: not null, Low: not null };

    private static bool IsValid(WorkerPointDeltaReportOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.CoordinateSystem, 3) &&
        IsEnumValue(value.DetailsFormat, 4);

    private static bool IsValid(WorkerProjectionOptionsValue? value) =>
        value is not null && IsEnumValue(value.ProjectionType, 7);

    private static bool IsValid(WorkerReportOutputOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.OutputType, 5) &&
        value.PathOrEmbeddedName is not null;

    private static bool IsValid(WorkerReportViewOptionsValue? value) =>
        value is not null &&
        IsEnumValue(value.ViewType, 3) &&
        value.CollectionName is not null &&
        value.CalloutName is not null;

    private static bool IsValid(WorkerToleranceScalarOptionsValue? value) =>
        value is { High: not null, Low: not null };

    private static bool IsEnumValue(int value, int valueCount) =>
        (uint)value < (uint)valueCount;
}
