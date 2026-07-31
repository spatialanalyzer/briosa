using System.Diagnostics;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Control;

internal static partial class WorkerControlHost
{
    private static ISdkSpecializedEnumValue? ToSdkSpecializedEnum(
        WorkerMpInputArgument argument) =>
        argument.SpecializedEnumValue is null
            ? null
            : argument.Kind switch
            {
                WorkerMpValueKind.AsciiImportFileFormat => ToSdkEnum<SdkAsciiImportFileFormatValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.AsciiFrameSetFormat => ToSdkEnum<SdkAsciiFrameSetFormatValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.AxisIdentifier => ToSdkEnum<SdkAxisIdentifierValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.WcfAxisIdentifier => ToSdkEnum<SdkWcfAxisIdentifierValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.BaseColorType => ToSdkEnum<SdkBaseColorTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.BaseMidColorType => ToSdkEnum<SdkBaseMidColorTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ChartType => ToSdkEnum<SdkChartTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.CollimationBaselineType => ToSdkEnum<SdkCollimationBaselineTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.CollimationType => ToSdkEnum<SdkCollimationTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ColorRangeMethod => ToSdkEnum<SdkColorRangeMethodValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.CoordinateSystemType => ToSdkEnum<SdkCoordinateSystemTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.VectorComponent => ToSdkEnum<SdkVectorComponentValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.DynamicCircleMode => ToSdkEnum<SdkDynamicCircleModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.DynamicEllipseMode => ToSdkEnum<SdkDynamicEllipseModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.DynamicLineMode => ToSdkEnum<SdkDynamicLineModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.DynamicPlaneMode => ToSdkEnum<SdkDynamicPlaneModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.DynamicPointMode => ToSdkEnum<SdkDynamicPointModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.EdgeMode => ToSdkEnum<SdkEdgeModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ExportDataDelimiterType => ToSdkEnum<SdkExportDataDelimiterTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ExportTargetNameFormat => ToSdkEnum<SdkExportTargetNameFormatValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ExportVectorNameFormat => ToSdkEnum<SdkExportVectorNameFormatValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.GeometryType => ToSdkEnum<SdkGeometryTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.InstrumentType => ToSdkEnum<SdkInstrumentTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ObjectType => ToSdkEnum<SdkObjectTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.OffsetDirectionType => ToSdkEnum<SdkOffsetDirectionTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.PointFilterInputType => ToSdkEnum<SdkPointFilterInputTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.RelationshipWeightingMode => ToSdkEnum<SdkRelationshipWeightingModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.RenderModeType => ToSdkEnum<SdkRenderModeTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ReportPageOrientation => ToSdkEnum<SdkReportPageOrientationValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.SaturationLimitType => ToSdkEnum<SdkSaturationLimitTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.ShowUsmnDialogType => ToSdkEnum<SdkShowUsmnDialogTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.SurfaceAnalysisMode => ToSdkEnum<SdkSurfaceAnalysisModeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.SurfaceDissectionModeType => ToSdkEnum<SdkSurfaceDissectionModeTypeValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.TargetComputationMethod => ToSdkEnum<SdkTargetComputationMethodValue>(argument.SpecializedEnumValue),
                WorkerMpValueKind.TranslucencyType => ToSdkEnum<SdkTranslucencyTypeValue>(argument.SpecializedEnumValue),
                _ => null
            };

    private static SdkSpecializedEnumValue<T> ToSdkEnum<T>(WorkerSpecializedEnumValue value)
        where T : struct, Enum =>
        new(ToSdkEnumValue<T>(value.Value));

    private static T ToSdkEnumValue<T>(int value)
        where T : struct, Enum
    {
        var typedValue = (T)Enum.ToObject(typeof(T), value);
        if (!Enum.IsDefined(typedValue))
        {
            throw new InvalidDataException(
                $"The specialized value {value} is not valid for {typeof(T).Name}.");
        }

        return typedValue;
    }

    private static SdkValueKind ToSdkSpecializedValueKind(WorkerMpValueKind kind) =>
        kind switch
        {
            WorkerMpValueKind.AsciiImportFileFormat => SdkValueKind.AsciiImportFileFormat,
            WorkerMpValueKind.AsciiFrameSetFormat => SdkValueKind.AsciiFrameSetFormat,
            WorkerMpValueKind.AutoFilterProximitySettings => SdkValueKind.AutoFilterProximitySettings,
            WorkerMpValueKind.AxisIdentifier => SdkValueKind.AxisIdentifier,
            WorkerMpValueKind.WcfAxisIdentifier => SdkValueKind.WcfAxisIdentifier,
            WorkerMpValueKind.BaseColorType => SdkValueKind.BaseColorType,
            WorkerMpValueKind.BaseMidColorType => SdkValueKind.BaseMidColorType,
            WorkerMpValueKind.ChartType => SdkValueKind.ChartType,
            WorkerMpValueKind.CloudThinningOptions => SdkValueKind.CloudThinningOptions,
            WorkerMpValueKind.CollimationBaselineType => SdkValueKind.CollimationBaselineType,
            WorkerMpValueKind.CollimationType => SdkValueKind.CollimationType,
            WorkerMpValueKind.ColorRangeMethod => SdkValueKind.ColorRangeMethod,
            WorkerMpValueKind.ColorizationOptions => SdkValueKind.ColorizationOptions,
            WorkerMpValueKind.CoordinateSystemType => SdkValueKind.CoordinateSystemType,
            WorkerMpValueKind.VectorComponent => SdkValueKind.VectorComponent,
            WorkerMpValueKind.DynamicCircleMode => SdkValueKind.DynamicCircleMode,
            WorkerMpValueKind.DynamicEllipseMode => SdkValueKind.DynamicEllipseMode,
            WorkerMpValueKind.DynamicLineMode => SdkValueKind.DynamicLineMode,
            WorkerMpValueKind.DynamicPlaneMode => SdkValueKind.DynamicPlaneMode,
            WorkerMpValueKind.DynamicPointMode => SdkValueKind.DynamicPointMode,
            WorkerMpValueKind.EdgeMode => SdkValueKind.EdgeMode,
            WorkerMpValueKind.ExportDataDelimiterType => SdkValueKind.ExportDataDelimiterType,
            WorkerMpValueKind.ExportTargetNameFormat => SdkValueKind.ExportTargetNameFormat,
            WorkerMpValueKind.ExportVectorNameFormat => SdkValueKind.ExportVectorNameFormat,
            WorkerMpValueKind.FitConstraintScalarOptions => SdkValueKind.FitConstraintScalarOptions,
            WorkerMpValueKind.FitDegreeOfFreedomOptions => SdkValueKind.FitDegreeOfFreedomOptions,
            WorkerMpValueKind.GeometryType => SdkValueKind.GeometryType,
            WorkerMpValueKind.InstrumentType => SdkValueKind.InstrumentType,
            WorkerMpValueKind.ObjectType => SdkValueKind.ObjectType,
            WorkerMpValueKind.OffsetDirectionType => SdkValueKind.OffsetDirectionType,
            WorkerMpValueKind.PointFilterInputType => SdkValueKind.PointFilterInputType,
            WorkerMpValueKind.RelationshipWeightingMode => SdkValueKind.RelationshipWeightingMode,
            WorkerMpValueKind.RenderModeType => SdkValueKind.RenderModeType,
            WorkerMpValueKind.ReportOutputOptions => SdkValueKind.ReportOutputOptions,
            WorkerMpValueKind.ReportPageOrientation => SdkValueKind.ReportPageOrientation,
            WorkerMpValueKind.ReportViewOptions => SdkValueKind.ReportViewOptions,
            WorkerMpValueKind.SaturationLimitType => SdkValueKind.SaturationLimitType,
            WorkerMpValueKind.ShowUsmnDialogType => SdkValueKind.ShowUsmnDialogType,
            WorkerMpValueKind.SurfaceAnalysisMode => SdkValueKind.SurfaceAnalysisMode,
            WorkerMpValueKind.SurfaceDissectionModeType => SdkValueKind.SurfaceDissectionModeType,
            WorkerMpValueKind.TargetComputationMethod => SdkValueKind.TargetComputationMethod,
            WorkerMpValueKind.ToleranceScalarOptions => SdkValueKind.ToleranceScalarOptions,
            WorkerMpValueKind.TranslucencyType => SdkValueKind.TranslucencyType,
            _ => throw new UnreachableException()
        };

    private static WorkerMpValueKind ToControlSpecializedValueKind(SdkValueKind kind) =>
        kind switch
        {
            SdkValueKind.AsciiImportFileFormat => WorkerMpValueKind.AsciiImportFileFormat,
            SdkValueKind.AsciiFrameSetFormat => WorkerMpValueKind.AsciiFrameSetFormat,
            SdkValueKind.AutoFilterProximitySettings => WorkerMpValueKind.AutoFilterProximitySettings,
            SdkValueKind.AxisIdentifier => WorkerMpValueKind.AxisIdentifier,
            SdkValueKind.WcfAxisIdentifier => WorkerMpValueKind.WcfAxisIdentifier,
            SdkValueKind.BaseColorType => WorkerMpValueKind.BaseColorType,
            SdkValueKind.BaseMidColorType => WorkerMpValueKind.BaseMidColorType,
            SdkValueKind.ChartType => WorkerMpValueKind.ChartType,
            SdkValueKind.CloudThinningOptions => WorkerMpValueKind.CloudThinningOptions,
            SdkValueKind.CollimationBaselineType => WorkerMpValueKind.CollimationBaselineType,
            SdkValueKind.CollimationType => WorkerMpValueKind.CollimationType,
            SdkValueKind.ColorRangeMethod => WorkerMpValueKind.ColorRangeMethod,
            SdkValueKind.ColorizationOptions => WorkerMpValueKind.ColorizationOptions,
            SdkValueKind.CoordinateSystemType => WorkerMpValueKind.CoordinateSystemType,
            SdkValueKind.VectorComponent => WorkerMpValueKind.VectorComponent,
            SdkValueKind.DynamicCircleMode => WorkerMpValueKind.DynamicCircleMode,
            SdkValueKind.DynamicEllipseMode => WorkerMpValueKind.DynamicEllipseMode,
            SdkValueKind.DynamicLineMode => WorkerMpValueKind.DynamicLineMode,
            SdkValueKind.DynamicPlaneMode => WorkerMpValueKind.DynamicPlaneMode,
            SdkValueKind.DynamicPointMode => WorkerMpValueKind.DynamicPointMode,
            SdkValueKind.EdgeMode => WorkerMpValueKind.EdgeMode,
            SdkValueKind.ExportDataDelimiterType => WorkerMpValueKind.ExportDataDelimiterType,
            SdkValueKind.ExportTargetNameFormat => WorkerMpValueKind.ExportTargetNameFormat,
            SdkValueKind.ExportVectorNameFormat => WorkerMpValueKind.ExportVectorNameFormat,
            SdkValueKind.FitConstraintScalarOptions => WorkerMpValueKind.FitConstraintScalarOptions,
            SdkValueKind.FitDegreeOfFreedomOptions => WorkerMpValueKind.FitDegreeOfFreedomOptions,
            SdkValueKind.GeometryType => WorkerMpValueKind.GeometryType,
            SdkValueKind.InstrumentType => WorkerMpValueKind.InstrumentType,
            SdkValueKind.ObjectType => WorkerMpValueKind.ObjectType,
            SdkValueKind.OffsetDirectionType => WorkerMpValueKind.OffsetDirectionType,
            SdkValueKind.PointFilterInputType => WorkerMpValueKind.PointFilterInputType,
            SdkValueKind.RelationshipWeightingMode => WorkerMpValueKind.RelationshipWeightingMode,
            SdkValueKind.RenderModeType => WorkerMpValueKind.RenderModeType,
            SdkValueKind.ReportOutputOptions => WorkerMpValueKind.ReportOutputOptions,
            SdkValueKind.ReportPageOrientation => WorkerMpValueKind.ReportPageOrientation,
            SdkValueKind.ReportViewOptions => WorkerMpValueKind.ReportViewOptions,
            SdkValueKind.SaturationLimitType => WorkerMpValueKind.SaturationLimitType,
            SdkValueKind.ShowUsmnDialogType => WorkerMpValueKind.ShowUsmnDialogType,
            SdkValueKind.SurfaceAnalysisMode => WorkerMpValueKind.SurfaceAnalysisMode,
            SdkValueKind.SurfaceDissectionModeType => WorkerMpValueKind.SurfaceDissectionModeType,
            SdkValueKind.TargetComputationMethod => WorkerMpValueKind.TargetComputationMethod,
            SdkValueKind.ToleranceScalarOptions => WorkerMpValueKind.ToleranceScalarOptions,
            SdkValueKind.TranslucencyType => WorkerMpValueKind.TranslucencyType,
            _ => throw new UnreachableException()
        };

    private static SdkAutoFilterProximitySettingsValue? ToSdkAutoFilter(
        WorkerAutoFilterProximitySettingsValue? value) =>
        value is null
            ? null
            : new(
                value.SurfaceInclusionProximity,
                value.EdgeExclusionProximity,
                value.PlanarInclusionProximity,
                value.PlanarExclusionProximity,
                value.RadialInclusionProximity,
                value.GeometryExtractionTolerance,
                ToSdkEnumValue<SdkOffsetDirectionTypeValue>(value.SurfaceProximityMode),
                ToSdkEnumValue<SdkOffsetDirectionTypeValue>(value.PlanarProximityMode),
                ToSdkEnumValue<SdkOffsetDirectionTypeValue>(value.RadialProximityMode),
                value.ProjectToPlane,
                value.AssertPlaneBoundaries);

    private static SdkCloudThinningOptionsValue? ToSdkCloudThinning(
        WorkerCloudThinningOptionsValue? value) =>
        value is null
            ? null
            : new(
                ToSdkEnumValue<SdkCloudThinningModeValue>(value.Mode),
                value.PointIncrement,
                value.MinimumNumberOfPoints,
                value.MaximumNumberOfPoints);

    private static SdkColorizationOptionsValue? ToSdkColorization(
        WorkerColorizationOptionsValue? value) =>
        value is null
            ? null
            : new(
                ToSdkEnumValue<SdkColorRangeMethodValue>(value.ColorRangeMethod),
                ToSdkEnumValue<SdkBaseColorTypeValue>(value.BaseHighColor),
                ToSdkEnumValue<SdkBaseMidColorTypeValue>(value.BaseMidColor),
                ToSdkEnumValue<SdkBaseColorTypeValue>(value.BaseLowColor),
                value.DrawTubes,
                value.DrawArrowheads,
                value.IndicateValues,
                value.VectorMagnification,
                value.VectorWidth,
                value.DrawBlotches,
                value.BlotchSize,
                value.ShowOutOfToleranceOnly,
                value.ShowColorBarInView,
                value.ShowColorBarPercentages,
                value.ShowColorBarFractions,
                value.HighSaturationLimit,
                value.LowSaturationLimit,
                value.HighTolerance,
                value.LowTolerance);

    private static SdkFitConstraintScalarOptionsValue? ToSdkFitConstraintScalar(
        WorkerFitConstraintScalarOptionsValue? value) =>
        value is null ? null : new(ToSdkScalarToleranceLimit(value.High), ToSdkScalarToleranceLimit(value.Low));

    private static SdkFitDegreeOfFreedomOptionsValue? ToSdkFitDegreeOfFreedom(
        WorkerFitDegreeOfFreedomOptionsValue? value) =>
        value is null
            ? null
            : new(value.AllowX, value.AllowY, value.AllowZ, value.AllowRx, value.AllowRy, value.AllowRz, value.RotateAboutCentroid);


    private static SdkReportOutputOptionsValue? ToSdkReportOutput(
        WorkerReportOutputOptionsValue? value) =>
        value is null
            ? null
            : new(
                ToSdkEnumValue<SdkReportOutputTypeValue>(value.OutputType),
                value.ExternalPath,
                value.EmbeddedFile is null
                    ? null
                    : new(value.EmbeddedFile.CollectionName, value.EmbeddedFile.FileName));

    private static SdkReportViewOptionsValue? ToSdkReportView(
        WorkerReportViewOptionsValue? value) =>
        value is null
            ? null
            : new(
                ToSdkEnumValue<SdkReportViewTypeValue>(value.ViewType),
                value.CollectionName,
                value.CalloutName);

    private static SdkToleranceScalarOptionsValue? ToSdkToleranceScalar(
        WorkerToleranceScalarOptionsValue? value) =>
        value is null ? null : new(ToSdkScalarToleranceLimit(value.High), ToSdkScalarToleranceLimit(value.Low));

    private static SdkToleranceLimit ToSdkScalarToleranceLimit(WorkerScalarToleranceLimit value) =>
        new(value.Enabled, value.Value);

    private static WorkerFitConstraintScalarOptionsValue? ToControlFitConstraintScalar(
        SdkFitConstraintScalarOptionsValue? value) =>
        value is null
            ? null
            : new(ToControlScalarToleranceLimit(value.High), ToControlScalarToleranceLimit(value.Low));

    private static WorkerToleranceScalarOptionsValue? ToControlToleranceScalar(
        SdkToleranceScalarOptionsValue? value) =>
        value is null
            ? null
            : new(ToControlScalarToleranceLimit(value.High), ToControlScalarToleranceLimit(value.Low));

    private static WorkerScalarToleranceLimit ToControlScalarToleranceLimit(SdkToleranceLimit value) =>
        new(value.Enabled, value.Value);
}
