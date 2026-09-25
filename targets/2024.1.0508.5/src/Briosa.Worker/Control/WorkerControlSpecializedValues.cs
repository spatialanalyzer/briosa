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
                WorkerMpValueKind.AsciiImportFileFormat => ToSdkEnum((SdkAsciiImportFileFormatValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.AsciiFrameSetFormat => ToSdkEnum((SdkAsciiFrameSetFormatValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.AxisIdentifier => ToSdkEnum((SdkAxisIdentifierValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.WcfAxisIdentifier => ToSdkEnum((SdkWcfAxisIdentifierValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.BaseColorType => ToSdkEnum((SdkBaseColorTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.BaseMidColorType => ToSdkEnum((SdkBaseMidColorTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ChartType => ToSdkEnum((SdkChartTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.CollimationBaselineType => ToSdkEnum((SdkCollimationBaselineTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.CollimationType => ToSdkEnum((SdkCollimationTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ColorRangeMethod => ToSdkEnum((SdkColorRangeMethodValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.CoordinateSystemType => ToSdkEnum((SdkCoordinateSystemTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.VectorComponent => ToSdkEnum((SdkVectorComponentValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.DynamicCircleMode => ToSdkEnum((SdkDynamicCircleModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.DynamicEllipseMode => ToSdkEnum((SdkDynamicEllipseModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.DynamicLineMode => ToSdkEnum((SdkDynamicLineModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.DynamicPlaneMode => ToSdkEnum((SdkDynamicPlaneModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.DynamicPointMode => ToSdkEnum((SdkDynamicPointModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.EdgeMode => ToSdkEnum((SdkEdgeModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ExportDataDelimiterType => ToSdkEnum((SdkExportDataDelimiterTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ExportTargetNameFormat => ToSdkEnum((SdkExportTargetNameFormatValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ExportVectorNameFormat => ToSdkEnum((SdkExportVectorNameFormatValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.GeometryType => ToSdkEnum((SdkGeometryTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.GdtDistanceBetweenMode => ToSdkEnum((SdkGdtDistanceBetweenModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.GdtEvaluationMethod => ToSdkEnum((SdkGdtEvaluationMethodValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.InstrumentType => ToSdkEnum((SdkInstrumentTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ObjectType => ToSdkEnum((SdkObjectTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.OffsetDirectionType => ToSdkEnum((SdkOffsetDirectionTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.PointFilterInputType => ToSdkEnum((SdkPointFilterInputTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.RelationshipWeightingMode => ToSdkEnum((SdkRelationshipWeightingModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.RenderModeType => ToSdkEnum((SdkRenderModeTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ReportPageOrientation => ToSdkEnum((SdkReportPageOrientationValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.SaturationLimitType => ToSdkEnum((SdkSaturationLimitTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.ShowUsmnDialogType => ToSdkEnum((SdkShowUsmnDialogTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.SurfaceAnalysisMode => ToSdkEnum((SdkSurfaceAnalysisModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.SurfaceDissectionModeType => ToSdkEnum((SdkSurfaceDissectionModeTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.TargetComputationMethod => ToSdkEnum((SdkTargetComputationMethodValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.TranslucencyType => ToSdkEnum((SdkTranslucencyTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.CompTechnique => ToSdkEnum((SdkCompTechniqueValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.DegreeOfFreedom => ToSdkEnum((SdkDegreeOfFreedomValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.FitMethod => ToSdkEnum((SdkFitMethodValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.MeasuredSideForPlanarOffset => ToSdkEnum((SdkMeasuredSideForPlanarOffsetValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.MeasuredSideForRadialOffset => ToSdkEnum((SdkMeasuredSideForRadialOffsetValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.MpDialogInteractionMode => ToSdkEnum((SdkMpDialogInteractionModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.MpInteractionMode => ToSdkEnum((SdkMpInteractionModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.NormalDirection => ToSdkEnum((SdkNormalDirectionValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.SaInteractionMode => ToSdkEnum((SdkSaInteractionModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.SlotType => ToSdkEnum((SdkSlotTypeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.SphereFitComputationMode => ToSdkEnum((SdkSphereFitComputationModeValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.WindowState => ToSdkEnum((SdkWindowStateValue)argument.SpecializedEnumValue.Value),
                WorkerMpValueKind.SystemString => ToSdkEnum((SdkSystemStringValue)argument.SpecializedEnumValue.Value),
                _ => null
            };

    private static SdkSpecializedEnumValue<T> ToSdkEnum<T>(T value)
        where T : struct, Enum =>
        new(RequireDefined(value));

    private static T RequireDefined<T>(T value)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new InvalidDataException(
                $"The specialized value {value} is not valid for {typeof(T).Name}.");
        }

        return value;
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
            WorkerMpValueKind.BSplineFitOptions => SdkValueKind.BSplineFitOptions,
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
            WorkerMpValueKind.GdtDistanceBetweenMode => SdkValueKind.GdtDistanceBetweenMode,
            WorkerMpValueKind.GdtEvaluationMethod => SdkValueKind.GdtEvaluationMethod,
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
            WorkerMpValueKind.CompTechnique => SdkValueKind.CompTechnique,
            WorkerMpValueKind.DegreeOfFreedom => SdkValueKind.DegreeOfFreedom,
            WorkerMpValueKind.FitMethod => SdkValueKind.FitMethod,
            WorkerMpValueKind.MeasuredSideForPlanarOffset => SdkValueKind.MeasuredSideForPlanarOffset,
            WorkerMpValueKind.MeasuredSideForRadialOffset => SdkValueKind.MeasuredSideForRadialOffset,
            WorkerMpValueKind.MpDialogInteractionMode => SdkValueKind.MpDialogInteractionMode,
            WorkerMpValueKind.MpInteractionMode => SdkValueKind.MpInteractionMode,
            WorkerMpValueKind.NormalDirection => SdkValueKind.NormalDirection,
            WorkerMpValueKind.SaInteractionMode => SdkValueKind.SaInteractionMode,
            WorkerMpValueKind.SlotType => SdkValueKind.SlotType,
            WorkerMpValueKind.SphereFitComputationMode => SdkValueKind.SphereFitComputationMode,
            WorkerMpValueKind.WindowState => SdkValueKind.WindowState,
            WorkerMpValueKind.SystemString => SdkValueKind.SystemString,
            WorkerMpValueKind.ProjectionOptions => SdkValueKind.ProjectionOptions,
            WorkerMpValueKind.PointDeltaReportOptions => SdkValueKind.PointDeltaReportOptions,
            WorkerMpValueKind.UdpTransmitSettings => SdkValueKind.UdpTransmitSettings,
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
            SdkValueKind.BSplineFitOptions => WorkerMpValueKind.BSplineFitOptions,
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
            SdkValueKind.GdtDistanceBetweenMode => WorkerMpValueKind.GdtDistanceBetweenMode,
            SdkValueKind.GdtEvaluationMethod => WorkerMpValueKind.GdtEvaluationMethod,
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
            SdkValueKind.CompTechnique => WorkerMpValueKind.CompTechnique,
            SdkValueKind.DegreeOfFreedom => WorkerMpValueKind.DegreeOfFreedom,
            SdkValueKind.FitMethod => WorkerMpValueKind.FitMethod,
            SdkValueKind.MeasuredSideForPlanarOffset => WorkerMpValueKind.MeasuredSideForPlanarOffset,
            SdkValueKind.MeasuredSideForRadialOffset => WorkerMpValueKind.MeasuredSideForRadialOffset,
            SdkValueKind.MpDialogInteractionMode => WorkerMpValueKind.MpDialogInteractionMode,
            SdkValueKind.MpInteractionMode => WorkerMpValueKind.MpInteractionMode,
            SdkValueKind.NormalDirection => WorkerMpValueKind.NormalDirection,
            SdkValueKind.SaInteractionMode => WorkerMpValueKind.SaInteractionMode,
            SdkValueKind.SlotType => WorkerMpValueKind.SlotType,
            SdkValueKind.SphereFitComputationMode => WorkerMpValueKind.SphereFitComputationMode,
            SdkValueKind.WindowState => WorkerMpValueKind.WindowState,
            SdkValueKind.SystemString => WorkerMpValueKind.SystemString,
            SdkValueKind.ProjectionOptions => WorkerMpValueKind.ProjectionOptions,
            SdkValueKind.PointDeltaReportOptions => WorkerMpValueKind.PointDeltaReportOptions,
            SdkValueKind.UdpTransmitSettings => WorkerMpValueKind.UdpTransmitSettings,
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
                RequireDefined((SdkOffsetDirectionTypeValue)value.SurfaceProximityMode),
                RequireDefined((SdkOffsetDirectionTypeValue)value.PlanarProximityMode),
                RequireDefined((SdkOffsetDirectionTypeValue)value.RadialProximityMode),
                value.ProjectToPlane,
                value.AssertPlaneBoundaries);

    private static SdkCloudThinningOptionsValue? ToSdkCloudThinning(
        WorkerCloudThinningOptionsValue? value) =>
        value is null
            ? null
            : new(
                RequireDefined((SdkCloudThinningModeValue)value.Mode),
                value.PointIncrement,
                value.MinimumNumberOfPoints,
                value.MaximumNumberOfPoints);

    private static SdkBSplineFitOptionsValue? ToSdkBSplineFit(
        WorkerBSplineFitOptionsValue? value) =>
        value is null
            ? null
            : new(
                value.UseInterpolationFit,
                value.OpenCurve,
                value.SortMethod,
                value.TerminateMethod,
                value.Degree,
                value.TerminateLength,
                value.TerminateAverageMultiplier,
                value.NumberOfControlPoints,
                value.UniqueCheck,
                value.UniqueThreshold,
                value.Extension,
                value.UseGlobalTessellationOptions,
                value.MaximumChordalDeviation,
                value.MaximumTrimEdgeAngle);

    private static SdkColorizationOptionsValue? ToSdkColorization(
        WorkerColorizationOptionsValue? value) =>
        value is null
            ? null
            : new(
                RequireDefined((SdkColorRangeMethodValue)value.ColorRangeMethod),
                RequireDefined((SdkBaseColorTypeValue)value.BaseHighColor),
                RequireDefined((SdkBaseMidColorTypeValue)value.BaseMidColor),
                RequireDefined((SdkBaseColorTypeValue)value.BaseLowColor),
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
                RequireDefined((SdkReportOutputTypeValue)value.OutputType),
                value.ExternalPath,
                value.EmbeddedFile is null
                    ? null
                    : new(value.EmbeddedFile.CollectionName, value.EmbeddedFile.FileName));

    private static SdkReportViewOptionsValue? ToSdkReportView(
        WorkerReportViewOptionsValue? value) =>
        value is null
            ? null
            : new(
                RequireDefined((SdkReportViewTypeValue)value.ViewType),
                value.CollectionName,
                value.CalloutName);

    private static SdkToleranceScalarOptionsValue? ToSdkToleranceScalar(
        WorkerToleranceScalarOptionsValue? value) =>
        value is null ? null : new(ToSdkScalarToleranceLimit(value.High), ToSdkScalarToleranceLimit(value.Low));

    private static SdkProjectionOptionsValue? ToSdkProjection(
        WorkerProjectionOptionsValue? value) =>
        value is null
            ? null
            : new(
                value.ProjectionType,
                value.IgnoreEdgeProjections,
                value.OverrideTargetOffsets,
                value.OverrideTargetOffsetsValue,
                value.AddExtraMaterialThickness,
                value.ExtraMaterialThicknessValue);

    private static SdkPointDeltaReportOptionsValue? ToSdkPointDeltaReport(
        WorkerPointDeltaReportOptionsValue? value) =>
        value is null
            ? null
            : new(
                RequireDefined((SdkCoordinateSystemTypeValue)value.CoordinateSystem),
                value.DetailsFormat,
                value.ShowPointA,
                value.ShowPointB,
                value.ShowDelta,
                value.ShowMagnitude,
                value.ShowComponent1,
                value.ShowComponent2,
                value.ShowComponent3,
                value.SortPointNames,
                value.ShowToleranceFields,
                value.ColorizeInToleranceFields);

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
