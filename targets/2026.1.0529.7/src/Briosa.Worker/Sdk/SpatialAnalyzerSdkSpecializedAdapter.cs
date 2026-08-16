using System.Diagnostics;

namespace Briosa.Worker.Sdk;

internal sealed partial class SpatialAnalyzerSdkAdapter
{
    private static bool SetSpecializedInputArgument(
        ISpatialAnalyzerSdkCalls sdk,
        SdkInputArgument argument) =>
        argument.Kind switch
        {
            SdkValueKind.AsciiImportFileFormat when EnumValue<SdkAsciiImportFileFormatValue>(argument) is { } value =>
                sdk.SetAsciiFileFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.AsciiFrameSetFormat when EnumValue<SdkAsciiFrameSetFormatValue>(argument) is { } value =>
                sdk.SetAsciiFileFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.AxisIdentifier when EnumValue<SdkAxisIdentifierValue>(argument) is { } value =>
                sdk.SetAxisNameArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.WcfAxisIdentifier when EnumValue<SdkWcfAxisIdentifierValue>(argument) is { } value =>
                sdk.SetAxisNameArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.BaseColorType when EnumValue<SdkBaseColorTypeValue>(argument) is { } value =>
                sdk.SetBaseColorTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.BaseMidColorType when EnumValue<SdkBaseMidColorTypeValue>(argument) is { } value =>
                sdk.SetBaseMidColorTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ChartType when EnumValue<SdkChartTypeValue>(argument) is { } value =>
                sdk.SetChartTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.CollimationBaselineType when EnumValue<SdkCollimationBaselineTypeValue>(argument) is { } value =>
                sdk.SetCollimationBaselineTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.CollimationType when EnumValue<SdkCollimationTypeValue>(argument) is { } value =>
                sdk.SetCollimationTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ColorRangeMethod when EnumValue<SdkColorRangeMethodValue>(argument) is { } value =>
                sdk.SetColorRangeMethodArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.CoordinateSystemType when EnumValue<SdkCoordinateSystemTypeValue>(argument) is { } value =>
                sdk.SetCoordinateSystemTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.VectorComponent when EnumValue<SdkVectorComponentValue>(argument) is { } value =>
                sdk.SetDatasetTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.DynamicCircleMode when EnumValue<SdkDynamicCircleModeValue>(argument) is { } value =>
                sdk.SetDynamicCircleModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.DynamicEllipseMode when EnumValue<SdkDynamicEllipseModeValue>(argument) is { } value =>
                sdk.SetDynamicEllipseModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.DynamicLineMode when EnumValue<SdkDynamicLineModeValue>(argument) is { } value =>
                sdk.SetDynamicLineModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.DynamicPlaneMode when EnumValue<SdkDynamicPlaneModeValue>(argument) is { } value =>
                sdk.SetDynamicPlaneModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.DynamicPointMode when EnumValue<SdkDynamicPointModeValue>(argument) is { } value =>
                sdk.SetDynamicPointModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.EdgeMode when EnumValue<SdkEdgeModeValue>(argument) is { } value =>
                sdk.SetEdgeModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ExportDataDelimiterType when EnumValue<SdkExportDataDelimiterTypeValue>(argument) is { } value =>
                sdk.SetExportDataDelimeterTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ExportTargetNameFormat when EnumValue<SdkExportTargetNameFormatValue>(argument) is { } value =>
                sdk.SetExportTargetNameFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ExportVectorNameFormat when EnumValue<SdkExportVectorNameFormatValue>(argument) is { } value =>
                sdk.SetExportVectorNameFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.GeometryType when EnumValue<SdkGeometryTypeValue>(argument) is { } value =>
                sdk.SetGeometryTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.InstrumentType when EnumValue<SdkInstrumentTypeValue>(argument) is { } value =>
                sdk.SetInstTypeNameArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ObjectType when EnumValue<SdkObjectTypeValue>(argument) is { } value =>
                sdk.SetObjectTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.OffsetDirectionType when EnumValue<SdkOffsetDirectionTypeValue>(argument) is { } value =>
                sdk.SetOffsetDirectionTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.PointFilterInputType when EnumValue<SdkPointFilterInputTypeValue>(argument) is { } value =>
                sdk.SetPointFilterInputTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.RelationshipWeightingMode when EnumValue<SdkRelationshipWeightingModeValue>(argument) is { } value =>
                sdk.SetRelWeightingModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.RenderModeType when EnumValue<SdkRenderModeTypeValue>(argument) is { } value =>
                sdk.SetRenderModeTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ReportPageOrientation when EnumValue<SdkReportPageOrientationValue>(argument) is { } value =>
                sdk.SetReportPageSettingsArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.SaturationLimitType when EnumValue<SdkSaturationLimitTypeValue>(argument) is { } value =>
                sdk.SetSaturationLimitTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.ShowUsmnDialogType when EnumValue<SdkShowUsmnDialogTypeValue>(argument) is { } value =>
                sdk.SetShowUsmnDialogTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.SurfaceAnalysisMode when EnumValue<SdkSurfaceAnalysisModeValue>(argument) is { } value =>
                sdk.SetSurfaceAnalysisModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.SurfaceDissectionModeType when EnumValue<SdkSurfaceDissectionModeTypeValue>(argument) is { } value =>
                sdk.SetSurfDissectModeTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.TargetComputationMethod when EnumValue<SdkTargetComputationMethodValue>(argument) is { } value =>
                sdk.SetTargetComputationMethodArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.TranslucencyType when EnumValue<SdkTranslucencyTypeValue>(argument) is { } value =>
                sdk.SetTranslucencyTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.CompTechnique when EnumValue<SdkCompTechniqueValue>(argument) is { } value =>
                sdk.SetCompTechniqueArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.DegreeOfFreedom when EnumValue<SdkDegreeOfFreedomValue>(argument) is { } value =>
                sdk.SetDegreeOfFreedomArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.FitMethod when EnumValue<SdkFitMethodValue>(argument) is { } value =>
                sdk.SetFitMethodArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.MeasuredSideForPlanarOffset when EnumValue<SdkMeasuredSideForPlanarOffsetValue>(argument) is { } value =>
                sdk.SetMeasuredSideForPlanarOffsetArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.MeasuredSideForRadialOffset when EnumValue<SdkMeasuredSideForRadialOffsetValue>(argument) is { } value =>
                sdk.SetMeasuredSideForRadialOffsetArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.MpDialogInteractionMode when EnumValue<SdkMpDialogInteractionModeValue>(argument) is { } value =>
                sdk.SetMPDialogInteractionModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.MpInteractionMode when EnumValue<SdkMpInteractionModeValue>(argument) is { } value =>
                sdk.SetMPInteractionModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.NormalDirection when EnumValue<SdkNormalDirectionValue>(argument) is { } value =>
                sdk.SetNormalDirectionArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.SaInteractionMode when EnumValue<SdkSaInteractionModeValue>(argument) is { } value =>
                sdk.SetSAInteractionModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.SlotType when EnumValue<SdkSlotTypeValue>(argument) is { } value =>
                sdk.SetSlotTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.SphereFitComputationMode when EnumValue<SdkSphereFitComputationModeValue>(argument) is { } value =>
                sdk.SetSphereFitComputationModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.WindowState when EnumValue<SdkWindowStateValue>(argument) is { } value =>
                sdk.SetWindowStateArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            SdkValueKind.AutoFilterProximitySettings when argument.AutoFilterProximitySettingsValue is { } value =>
                SetAutoFilterProximitySettings(sdk, argument.Name, value),
            SdkValueKind.CloudThinningOptions when argument.CloudThinningOptionsValue is { } value =>
                sdk.SetCloudThinningOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString(value.Mode),
                    value.PointIncrement,
                    value.MinimumNumberOfPoints,
                    value.MaximumNumberOfPoints),
            SdkValueKind.ColorizationOptions when argument.ColorizationOptionsValue is { } value =>
                SetColorizationOptions(sdk, argument.Name, value),
            SdkValueKind.FitConstraintScalarOptions when argument.FitConstraintScalarOptionsValue is { } value =>
                sdk.SetFitConstraintScalarOptionsArg(
                    argument.Name,
                    value.High.Enabled,
                    value.High.Value,
                    value.Low.Enabled,
                    value.Low.Value),
            SdkValueKind.FitDegreeOfFreedomOptions when argument.FitDegreeOfFreedomOptionsValue is { } value =>
                sdk.SetFitDofOptionsArg(
                    argument.Name,
                    value.AllowX,
                    value.AllowY,
                    value.AllowZ,
                    value.AllowRx,
                    value.AllowRy,
                    value.AllowRz,
                    value.RotateAboutCentroid),

            SdkValueKind.ReportOutputOptions
                when argument.ReportOutputOptionsValue is { } value &&
                     ReportDestination(value) is { } destination =>
                sdk.SetReportOutputOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString(value.OutputType),
                    destination),
            SdkValueKind.ReportViewOptions when argument.ReportViewOptionsValue is { } value =>
                sdk.SetReportViewOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString(value.ViewType),
                    value.CollectionName,
                    value.CalloutName),
            SdkValueKind.ToleranceScalarOptions when argument.ToleranceScalarOptionsValue is { } value =>
                sdk.SetToleranceScalarOptionsArg(
                    argument.Name,
                    value.High.Enabled,
                    value.High.Value,
                    value.Low.Enabled,
                    value.Low.Value),
            SdkValueKind.ProjectionOptions when argument.ProjectionOptionsValue is { } value =>
                sdk.SetProjectionOptionsArg(
                    argument.Name,
                    value.ProjectionType,
                    value.IgnoreEdgeProjections,
                    value.OverrideTargetOffsets,
                    value.OverrideTargetOffsetsValue,
                    value.AddExtraMaterialThickness,
                    value.ExtraMaterialThicknessValue),
            SdkValueKind.PointDeltaReportOptions when argument.PointDeltaReportOptionsValue is { } value =>
                sdk.SetPointDeltaReportOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString(value.CoordinateSystem),
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
                    value.ColorizeInToleranceFields),
            _ => false
        };

    private static SdkOutputValue GetSpecializedOutputValue(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument) =>
        argument.Kind switch
        {
            SdkValueKind.FitConstraintScalarOptions => GetFitConstraintScalarOptions(sdk, argument),
            SdkValueKind.ToleranceScalarOptions => GetToleranceScalarOptions(sdk, argument),
            _ => new SdkOutputValue(argument.Name, argument.Kind, Retrieved: false)
        };

    private static string SpecializedExpectedSetter(SdkValueKind kind) => kind switch
    {
        SdkValueKind.AsciiImportFileFormat => "SetAsciiFileFormatArg",
        SdkValueKind.AsciiFrameSetFormat => "SetAsciiFileFormatArg",
        SdkValueKind.AutoFilterProximitySettings => "SetAutoFilterProximitySettingsArg",
        SdkValueKind.AxisIdentifier => "SetAxisNameArg",
        SdkValueKind.WcfAxisIdentifier => "SetAxisNameArg",
        SdkValueKind.BaseColorType => "SetBaseColorTypeArg",
        SdkValueKind.BaseMidColorType => "SetBaseMidColorTypeArg",
        SdkValueKind.ChartType => "SetChartTypeArg",
        SdkValueKind.CloudThinningOptions => "SetCloudThinningOptionsArg",
        SdkValueKind.CollimationBaselineType => "SetCollimationBaselineTypeArg",
        SdkValueKind.CollimationType => "SetCollimationTypeArg",
        SdkValueKind.ColorRangeMethod => "SetColorRangeMethodArg",
        SdkValueKind.ColorizationOptions => "SetColorizationOptionsArg",
        SdkValueKind.CoordinateSystemType => "SetCoordinateSystemTypeArg",
        SdkValueKind.VectorComponent => "SetDatasetTypeArg",
        SdkValueKind.DynamicCircleMode => "SetDynamicCircleModeArg",
        SdkValueKind.DynamicEllipseMode => "SetDynamicEllipseModeArg",
        SdkValueKind.DynamicLineMode => "SetDynamicLineModeArg",
        SdkValueKind.DynamicPlaneMode => "SetDynamicPlaneModeArg",
        SdkValueKind.DynamicPointMode => "SetDynamicPointModeArg",
        SdkValueKind.EdgeMode => "SetEdgeModeArg",
        SdkValueKind.ExportDataDelimiterType => "SetExportDataDelimeterTypeArg",
        SdkValueKind.ExportTargetNameFormat => "SetExportTargetNameFormatArg",
        SdkValueKind.ExportVectorNameFormat => "SetExportVectorNameFormatArg",
        SdkValueKind.FitConstraintScalarOptions => "SetFitConstraintScalarOptionsArg",
        SdkValueKind.FitDegreeOfFreedomOptions => "SetFitDofOptionsArg",
        SdkValueKind.GeometryType => "SetGeometryTypeArg",
        SdkValueKind.InstrumentType => "SetInstTypeNameArg",
        SdkValueKind.ObjectType => "SetObjectTypeArg",
        SdkValueKind.OffsetDirectionType => "SetOffsetDirectionTypeArg",

        SdkValueKind.PointFilterInputType => "SetPointFilterInputTypeArg",

        SdkValueKind.RelationshipWeightingMode => "SetRelWeightingModeArg",
        SdkValueKind.RenderModeType => "SetRenderModeTypeArg",
        SdkValueKind.ReportOutputOptions => "SetReportOutputOptionsArg",
        SdkValueKind.ReportPageOrientation => "SetReportPageSettingsArg",
        SdkValueKind.ReportViewOptions => "SetReportViewOptionsArg",
        SdkValueKind.SaturationLimitType => "SetSaturationLimitTypeArg",
        SdkValueKind.ShowUsmnDialogType => "SetShowUsmnDialogTypeArg",
        SdkValueKind.SurfaceAnalysisMode => "SetSurfaceAnalysisModeArg",
        SdkValueKind.SurfaceDissectionModeType => "SetSurfDissectModeTypeArg",
        SdkValueKind.TargetComputationMethod => "SetTargetComputationMethodArg",
        SdkValueKind.ToleranceScalarOptions => "SetToleranceScalarOptionsArg",
        SdkValueKind.TranslucencyType => "SetTranslucencyTypeArg",
        SdkValueKind.CompTechnique => "SetCompTechniqueArg",
        SdkValueKind.DegreeOfFreedom => "SetDegreeOfFreedomArg",
        SdkValueKind.FitMethod => "SetFitMethodArg",
        SdkValueKind.MeasuredSideForPlanarOffset => "SetMeasuredSideForPlanarOffsetArg",
        SdkValueKind.MeasuredSideForRadialOffset => "SetMeasuredSideForRadialOffsetArg",
        SdkValueKind.MpDialogInteractionMode => "SetMPDialogInteractionModeArg",
        SdkValueKind.MpInteractionMode => "SetMPInteractionModeArg",
        SdkValueKind.NormalDirection => "SetNormalDirectionArg",
        SdkValueKind.SaInteractionMode => "SetSAInteractionModeArg",
        SdkValueKind.SlotType => "SetSlotTypeArg",
        SdkValueKind.SphereFitComputationMode => "SetSphereFitComputationModeArg",
        SdkValueKind.WindowState => "SetWindowStateArg",
        SdkValueKind.ProjectionOptions => "SetProjectionOptionsArg",
        SdkValueKind.PointDeltaReportOptions => "SetPointDeltaReportOptionsArg",
        _ => throw new UnreachableException()
    };

    private static string SpecializedExpectedGetter(SdkValueKind kind) => kind switch
    {
        SdkValueKind.FitConstraintScalarOptions => "GetFitConstraintScalarOptionsArg",
        SdkValueKind.ToleranceScalarOptions => "GetToleranceScalarOptionsArg",
        _ => string.Empty
    };

    private static T? EnumValue<T>(SdkInputArgument argument) where T : struct, Enum =>
        argument.SpecializedEnumValue is SdkSpecializedEnumValue<T> value &&
        Enum.IsDefined(value.Value)
            ? value.Value
            : null;

    private static bool SetAutoFilterProximitySettings(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkAutoFilterProximitySettingsValue value) =>
        sdk.SetAutoFilterProximitySettingsArg(
            name,
            value.SurfaceInclusionProximity,
            value.EdgeExclusionProximity,
            value.PlanarInclusionProximity,
            value.PlanarExclusionProximity,
            value.RadialInclusionProximity,
            value.GeometryExtractionTolerance,
            SdkSpecializedValueCodec.ToSdkOffsetMode(value.SurfaceProximityMode),
            SdkSpecializedValueCodec.ToSdkOffsetMode(value.PlanarProximityMode),
            SdkSpecializedValueCodec.ToSdkOffsetMode(value.RadialProximityMode),
            value.ProjectToPlane,
            value.AssertPlaneBoundaries);

    private static bool SetColorizationOptions(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        SdkColorizationOptionsValue value) =>
        sdk.SetColorizationOptionsArg(
            name,
            SdkSpecializedValueCodec.ToSdkString(value.ColorRangeMethod),
            SdkSpecializedValueCodec.ToSdkString(value.BaseHighColor),
            SdkSpecializedValueCodec.ToSdkString(value.BaseMidColor),
            SdkSpecializedValueCodec.ToSdkString(value.BaseLowColor),
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


    private static string? ReportDestination(SdkReportOutputOptionsValue value)
    {
        if ((value.ExternalPath is not null) == (value.EmbeddedFile is not null))
        {
            return null;
        }

        return value.ExternalPath ??
            $"{value.EmbeddedFile!.CollectionName}::{value.EmbeddedFile.FileName}";
    }
    private static SdkOutputValue GetFitConstraintScalarOptions(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var highEnabled = false;
        var highValue = 0d;
        var lowEnabled = false;
        var lowValue = 0d;
        var retrieved = sdk.GetFitConstraintScalarOptionsArg(
            argument.Name,
            ref highEnabled,
            ref highValue,
            ref lowEnabled,
            ref lowValue);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            FitConstraintScalarOptionsValue: retrieved
                ? new SdkFitConstraintScalarOptionsValue(
                    new SdkToleranceLimit(highEnabled, highValue),
                    new SdkToleranceLimit(lowEnabled, lowValue))
                : null);
    }

    private static SdkOutputValue GetToleranceScalarOptions(
        ISpatialAnalyzerSdkCalls sdk,
        SdkOutputArgument argument)
    {
        var highEnabled = false;
        var highValue = 0d;
        var lowEnabled = false;
        var lowValue = 0d;
        var retrieved = sdk.GetToleranceScalarOptionsArg(
            argument.Name,
            ref highEnabled,
            ref highValue,
            ref lowEnabled,
            ref lowValue);
        return new SdkOutputValue(
            argument.Name,
            argument.Kind,
            retrieved,
            ToleranceScalarOptionsValue: retrieved
                ? new SdkToleranceScalarOptionsValue(
                    new SdkToleranceLimit(highEnabled, highValue),
                    new SdkToleranceLimit(lowEnabled, lowValue))
                : null);
    }
}
