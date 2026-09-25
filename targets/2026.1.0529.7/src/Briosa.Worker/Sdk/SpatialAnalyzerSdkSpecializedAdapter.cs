using Briosa.Worker.Control;
using System.Diagnostics;

namespace Briosa.Worker.Sdk;

internal sealed partial class SpatialAnalyzerSdkAdapter
{
    private static bool SetSpecializedInputArgument(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpInputArgument argument) =>
        argument.Kind switch
        {
            WorkerMpValueKind.AsciiImportFileFormat when EnumValue<WorkerAsciiImportFileFormatValue>(argument) is { } value =>
                sdk.SetAsciiFileFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.AsciiFrameSetFormat when EnumValue<WorkerAsciiFrameSetFormatValue>(argument) is { } value =>
                sdk.SetAsciiFileFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.AxisIdentifier when EnumValue<WorkerAxisIdentifierValue>(argument) is { } value =>
                sdk.SetAxisNameArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.WcfAxisIdentifier when EnumValue<WorkerWcfAxisIdentifierValue>(argument) is { } value =>
                sdk.SetAxisNameArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.BaseColorType when EnumValue<WorkerBaseColorTypeValue>(argument) is { } value =>
                sdk.SetBaseColorTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.BaseMidColorType when EnumValue<WorkerBaseMidColorTypeValue>(argument) is { } value =>
                sdk.SetBaseMidColorTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ChartType when EnumValue<WorkerChartTypeValue>(argument) is { } value =>
                sdk.SetChartTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.CollimationBaselineType when EnumValue<WorkerCollimationBaselineTypeValue>(argument) is { } value =>
                sdk.SetCollimationBaselineTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.CollimationType when EnumValue<WorkerCollimationTypeValue>(argument) is { } value =>
                sdk.SetCollimationTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ColorRangeMethod when EnumValue<WorkerColorRangeMethodValue>(argument) is { } value =>
                sdk.SetColorRangeMethodArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.CoordinateSystemType when EnumValue<WorkerCoordinateSystemTypeValue>(argument) is { } value =>
                sdk.SetCoordinateSystemTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.VectorComponent when EnumValue<WorkerVectorComponentValue>(argument) is { } value =>
                sdk.SetDatasetTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.DynamicCircleMode when EnumValue<WorkerDynamicCircleModeValue>(argument) is { } value =>
                sdk.SetDynamicCircleModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.DynamicEllipseMode when EnumValue<WorkerDynamicEllipseModeValue>(argument) is { } value =>
                sdk.SetDynamicEllipseModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.DynamicLineMode when EnumValue<WorkerDynamicLineModeValue>(argument) is { } value =>
                sdk.SetDynamicLineModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.DynamicPlaneMode when EnumValue<WorkerDynamicPlaneModeValue>(argument) is { } value =>
                sdk.SetDynamicPlaneModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.DynamicPointMode when EnumValue<WorkerDynamicPointModeValue>(argument) is { } value =>
                sdk.SetDynamicPointModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.EdgeMode when EnumValue<WorkerEdgeModeValue>(argument) is { } value =>
                sdk.SetEdgeModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ExportDataDelimiterType when EnumValue<WorkerExportDataDelimiterTypeValue>(argument) is { } value =>
                sdk.SetExportDataDelimeterTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ExportTargetNameFormat when EnumValue<WorkerExportTargetNameFormatValue>(argument) is { } value =>
                sdk.SetExportTargetNameFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ExportVectorNameFormat when EnumValue<WorkerExportVectorNameFormatValue>(argument) is { } value =>
                sdk.SetExportVectorNameFormatArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.GeometryType when EnumValue<WorkerGeometryTypeValue>(argument) is { } value =>
                sdk.SetGeometryTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.GdtDistanceBetweenMode when EnumValue<WorkerGdtDistanceBetweenModeValue>(argument) is { } value =>
                sdk.SetMPGDTOptionsDistanceBetweenModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.GdtEvaluationMethod when EnumValue<WorkerGdtEvaluationMethodValue>(argument) is { } value =>
                sdk.SetMPGDTOptionsCheckValidatorTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.InstrumentType when EnumValue<WorkerInstrumentTypeValue>(argument) is { } value =>
                sdk.SetInstTypeNameArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ObjectType when EnumValue<WorkerObjectTypeValue>(argument) is { } value =>
                sdk.SetObjectTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.OffsetDirectionType when EnumValue<WorkerOffsetDirectionTypeValue>(argument) is { } value =>
                sdk.SetOffsetDirectionTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.PointFilterInputType when EnumValue<WorkerPointFilterInputTypeValue>(argument) is { } value =>
                sdk.SetPointFilterInputTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.RelationshipWeightingMode when EnumValue<WorkerRelationshipWeightingModeValue>(argument) is { } value =>
                sdk.SetRelWeightingModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.RenderModeType when EnumValue<WorkerRenderModeTypeValue>(argument) is { } value =>
                sdk.SetRenderModeTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ReportPageOrientation when EnumValue<WorkerReportPageOrientationValue>(argument) is { } value =>
                sdk.SetReportPageSettingsArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.SaturationLimitType when EnumValue<WorkerSaturationLimitTypeValue>(argument) is { } value =>
                sdk.SetSaturationLimitTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.ShowUsmnDialogType when EnumValue<WorkerShowUsmnDialogTypeValue>(argument) is { } value =>
                sdk.SetShowUsmnDialogTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.SurfaceAnalysisMode when EnumValue<WorkerSurfaceAnalysisModeValue>(argument) is { } value =>
                sdk.SetSurfaceAnalysisModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.SurfaceDissectionModeType when EnumValue<WorkerSurfaceDissectionModeTypeValue>(argument) is { } value =>
                sdk.SetSurfDissectModeTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.TargetComputationMethod when EnumValue<WorkerTargetComputationMethodValue>(argument) is { } value =>
                sdk.SetTargetComputationMethodArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.TranslucencyType when EnumValue<WorkerTranslucencyTypeValue>(argument) is { } value =>
                sdk.SetTranslucencyTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.CompTechnique when EnumValue<WorkerCompTechniqueValue>(argument) is { } value =>
                sdk.SetCompTechniqueArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.DegreeOfFreedom when EnumValue<WorkerDegreeOfFreedomValue>(argument) is { } value =>
                sdk.SetDegreeOfFreedomArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.FitMethod when EnumValue<WorkerFitMethodValue>(argument) is { } value =>
                sdk.SetFitMethodArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.MeasuredSideForPlanarOffset when EnumValue<WorkerMeasuredSideForPlanarOffsetValue>(argument) is { } value =>
                sdk.SetMeasuredSideForPlanarOffsetArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.MeasuredSideForRadialOffset when EnumValue<WorkerMeasuredSideForRadialOffsetValue>(argument) is { } value =>
                sdk.SetMeasuredSideForRadialOffsetArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.MpDialogInteractionMode when EnumValue<WorkerMpDialogInteractionModeValue>(argument) is { } value =>
                sdk.SetMPDialogInteractionModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.MpInteractionMode when EnumValue<WorkerMpInteractionModeValue>(argument) is { } value =>
                sdk.SetMPInteractionModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.NormalDirection when EnumValue<WorkerNormalDirectionValue>(argument) is { } value =>
                sdk.SetNormalDirectionArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.SaInteractionMode when EnumValue<WorkerSaInteractionModeValue>(argument) is { } value =>
                sdk.SetSAInteractionModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.SlotType when EnumValue<WorkerSlotTypeValue>(argument) is { } value =>
                sdk.SetSlotTypeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.SphereFitComputationMode when EnumValue<WorkerSphereFitComputationModeValue>(argument) is { } value =>
                sdk.SetSphereFitComputationModeArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.WindowState when EnumValue<WorkerWindowStateValue>(argument) is { } value =>
                sdk.SetWindowStateArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.SystemString when EnumValue<WorkerSystemStringValue>(argument) is { } value =>
                sdk.SetSystemStringArg(argument.Name, SdkSpecializedValueCodec.ToSdkString(value)),
            WorkerMpValueKind.UdpTransmitSettings when argument.Value is WorkerUdpTransmitSettingsValue value =>
                sdk.SetUdpTransmitSettingsArg(argument.Name, value.Enabled, value.Broadcast, value.IpAddress, value.Port),
            WorkerMpValueKind.AutoFilterProximitySettings when argument.Value is WorkerAutoFilterProximitySettingsValue value =>
                SetAutoFilterProximitySettings(sdk, argument.Name, value),
            WorkerMpValueKind.CloudThinningOptions when argument.Value is WorkerCloudThinningOptionsValue value =>
                sdk.SetCloudThinningOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString((WorkerCloudThinningModeValue)value.Mode),
                    value.PointIncrement,
                    value.MinimumNumberOfPoints,
                    value.MaximumNumberOfPoints),
            WorkerMpValueKind.BSplineFitOptions when argument.Value is WorkerBSplineFitOptionsValue value =>
                sdk.SetBSplineFitOptionsArg(
                    argument.Name,
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
                    value.MaximumTrimEdgeAngle),
            WorkerMpValueKind.ColorizationOptions when argument.Value is WorkerColorizationOptionsValue value =>
                SetColorizationOptions(sdk, argument.Name, value),
            WorkerMpValueKind.FitConstraintScalarOptions when argument.Value is WorkerFitConstraintScalarOptionsValue value =>
                sdk.SetFitConstraintScalarOptionsArg(
                    argument.Name,
                    value.High.Enabled,
                    value.High.Value,
                    value.Low.Enabled,
                    value.Low.Value),
            WorkerMpValueKind.FitDegreeOfFreedomOptions when argument.Value is WorkerFitDegreeOfFreedomOptionsValue value =>
                sdk.SetFitDofOptionsArg(
                    argument.Name,
                    value.AllowX,
                    value.AllowY,
                    value.AllowZ,
                    value.AllowRx,
                    value.AllowRy,
                    value.AllowRz,
                    value.RotateAboutCentroid),

            WorkerMpValueKind.ReportOutputOptions
                when argument.Value is WorkerReportOutputOptionsValue value &&
                     ReportDestination(value) is { } destination =>
                sdk.SetReportOutputOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString((WorkerReportOutputTypeValue)value.OutputType),
                    destination),
            WorkerMpValueKind.ReportViewOptions when argument.Value is WorkerReportViewOptionsValue value =>
                sdk.SetReportViewOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString((WorkerReportViewTypeValue)value.ViewType),
                    value.CollectionName,
                    value.CalloutName),
            WorkerMpValueKind.ToleranceScalarOptions when argument.Value is WorkerToleranceScalarOptionsValue value =>
                sdk.SetToleranceScalarOptionsArg(
                    argument.Name,
                    value.High.Enabled,
                    value.High.Value,
                    value.Low.Enabled,
                    value.Low.Value),
            WorkerMpValueKind.ProjectionOptions when argument.Value is WorkerProjectionOptionsValue value =>
                sdk.SetProjectionOptionsArg(
                    argument.Name,
                    value.ProjectionType,
                    value.IgnoreEdgeProjections,
                    value.OverrideTargetOffsets,
                    value.OverrideTargetOffsetsValue,
                    value.AddExtraMaterialThickness,
                    value.ExtraMaterialThicknessValue),
            WorkerMpValueKind.PointDeltaReportOptions when argument.Value is WorkerPointDeltaReportOptionsValue value =>
                sdk.SetPointDeltaReportOptionsArg(
                    argument.Name,
                    SdkSpecializedValueCodec.ToSdkString((WorkerCoordinateSystemTypeValue)value.CoordinateSystem),
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

    private static WorkerMpOutputValue GetSpecializedOutputValue(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument) =>
        argument.Kind switch
        {
            WorkerMpValueKind.FitConstraintScalarOptions => GetFitConstraintScalarOptions(sdk, argument),
            WorkerMpValueKind.ToleranceScalarOptions => GetToleranceScalarOptions(sdk, argument),
            _ => new WorkerUnavailableOutput(argument.Name, argument.Kind)
        };

    private static string SpecializedExpectedSetter(WorkerMpValueKind kind) => kind switch
    {
        WorkerMpValueKind.AsciiImportFileFormat => "SetAsciiFileFormatArg",
        WorkerMpValueKind.AsciiFrameSetFormat => "SetAsciiFileFormatArg",
        WorkerMpValueKind.AutoFilterProximitySettings => "SetAutoFilterProximitySettingsArg",
        WorkerMpValueKind.AxisIdentifier => "SetAxisNameArg",
        WorkerMpValueKind.WcfAxisIdentifier => "SetAxisNameArg",
        WorkerMpValueKind.BaseColorType => "SetBaseColorTypeArg",
        WorkerMpValueKind.BaseMidColorType => "SetBaseMidColorTypeArg",
        WorkerMpValueKind.ChartType => "SetChartTypeArg",
        WorkerMpValueKind.BSplineFitOptions => "SetBSplineFitOptionsArg",
        WorkerMpValueKind.CloudThinningOptions => "SetCloudThinningOptionsArg",
        WorkerMpValueKind.CollimationBaselineType => "SetCollimationBaselineTypeArg",
        WorkerMpValueKind.CollimationType => "SetCollimationTypeArg",
        WorkerMpValueKind.ColorRangeMethod => "SetColorRangeMethodArg",
        WorkerMpValueKind.ColorizationOptions => "SetColorizationOptionsArg",
        WorkerMpValueKind.CoordinateSystemType => "SetCoordinateSystemTypeArg",
        WorkerMpValueKind.VectorComponent => "SetDatasetTypeArg",
        WorkerMpValueKind.DynamicCircleMode => "SetDynamicCircleModeArg",
        WorkerMpValueKind.DynamicEllipseMode => "SetDynamicEllipseModeArg",
        WorkerMpValueKind.DynamicLineMode => "SetDynamicLineModeArg",
        WorkerMpValueKind.DynamicPlaneMode => "SetDynamicPlaneModeArg",
        WorkerMpValueKind.DynamicPointMode => "SetDynamicPointModeArg",
        WorkerMpValueKind.EdgeMode => "SetEdgeModeArg",
        WorkerMpValueKind.ExportDataDelimiterType => "SetExportDataDelimeterTypeArg",
        WorkerMpValueKind.ExportTargetNameFormat => "SetExportTargetNameFormatArg",
        WorkerMpValueKind.ExportVectorNameFormat => "SetExportVectorNameFormatArg",
        WorkerMpValueKind.FitConstraintScalarOptions => "SetFitConstraintScalarOptionsArg",
        WorkerMpValueKind.FitDegreeOfFreedomOptions => "SetFitDofOptionsArg",
        WorkerMpValueKind.GeometryType => "SetGeometryTypeArg",
        WorkerMpValueKind.GdtDistanceBetweenMode => "SetMPGDTOptionsDistanceBetweenModeArg",
        WorkerMpValueKind.GdtEvaluationMethod => "SetMPGDTOptionsCheckValidatorTypeArg",
        WorkerMpValueKind.InstrumentType => "SetInstTypeNameArg",
        WorkerMpValueKind.ObjectType => "SetObjectTypeArg",
        WorkerMpValueKind.OffsetDirectionType => "SetOffsetDirectionTypeArg",

        WorkerMpValueKind.PointFilterInputType => "SetPointFilterInputTypeArg",

        WorkerMpValueKind.RelationshipWeightingMode => "SetRelWeightingModeArg",
        WorkerMpValueKind.RenderModeType => "SetRenderModeTypeArg",
        WorkerMpValueKind.ReportOutputOptions => "SetReportOutputOptionsArg",
        WorkerMpValueKind.ReportPageOrientation => "SetReportPageSettingsArg",
        WorkerMpValueKind.ReportViewOptions => "SetReportViewOptionsArg",
        WorkerMpValueKind.SaturationLimitType => "SetSaturationLimitTypeArg",
        WorkerMpValueKind.ShowUsmnDialogType => "SetShowUsmnDialogTypeArg",
        WorkerMpValueKind.SurfaceAnalysisMode => "SetSurfaceAnalysisModeArg",
        WorkerMpValueKind.SurfaceDissectionModeType => "SetSurfDissectModeTypeArg",
        WorkerMpValueKind.TargetComputationMethod => "SetTargetComputationMethodArg",
        WorkerMpValueKind.ToleranceScalarOptions => "SetToleranceScalarOptionsArg",
        WorkerMpValueKind.TranslucencyType => "SetTranslucencyTypeArg",
        WorkerMpValueKind.CompTechnique => "SetCompTechniqueArg",
        WorkerMpValueKind.DegreeOfFreedom => "SetDegreeOfFreedomArg",
        WorkerMpValueKind.FitMethod => "SetFitMethodArg",
        WorkerMpValueKind.MeasuredSideForPlanarOffset => "SetMeasuredSideForPlanarOffsetArg",
        WorkerMpValueKind.MeasuredSideForRadialOffset => "SetMeasuredSideForRadialOffsetArg",
        WorkerMpValueKind.MpDialogInteractionMode => "SetMPDialogInteractionModeArg",
        WorkerMpValueKind.MpInteractionMode => "SetMPInteractionModeArg",
        WorkerMpValueKind.NormalDirection => "SetNormalDirectionArg",
        WorkerMpValueKind.SaInteractionMode => "SetSAInteractionModeArg",
        WorkerMpValueKind.SlotType => "SetSlotTypeArg",
        WorkerMpValueKind.SphereFitComputationMode => "SetSphereFitComputationModeArg",
        WorkerMpValueKind.WindowState => "SetWindowStateArg",
        WorkerMpValueKind.SystemString => "SetSystemStringArg",
        WorkerMpValueKind.UdpTransmitSettings => "SetUdpTransmitSettingsArg",
        WorkerMpValueKind.ProjectionOptions => "SetProjectionOptionsArg",
        WorkerMpValueKind.PointDeltaReportOptions => "SetPointDeltaReportOptionsArg",
        _ => throw new UnreachableException()
    };

    private static string SpecializedExpectedGetter(WorkerMpValueKind kind) => kind switch
    {
        WorkerMpValueKind.FitConstraintScalarOptions => "GetFitConstraintScalarOptionsArg",
        WorkerMpValueKind.ToleranceScalarOptions => "GetToleranceScalarOptionsArg",
        _ => string.Empty
    };

    private static T? EnumValue<T>(WorkerMpInputArgument argument) where T : struct, Enum =>
        argument.Value is WorkerChoiceValue<T> value &&
        Enum.IsDefined(value.Value)
            ? value.Value
            : null;

    private static bool SetAutoFilterProximitySettings(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerAutoFilterProximitySettingsValue value) =>
        sdk.SetAutoFilterProximitySettingsArg(
            name,
            value.SurfaceInclusionProximity,
            value.EdgeExclusionProximity,
            value.PlanarInclusionProximity,
            value.PlanarExclusionProximity,
            value.RadialInclusionProximity,
            value.GeometryExtractionTolerance,
            SdkSpecializedValueCodec.ToSdkOffsetMode((WorkerOffsetDirectionTypeValue)value.SurfaceProximityMode),
            SdkSpecializedValueCodec.ToSdkOffsetMode((WorkerOffsetDirectionTypeValue)value.PlanarProximityMode),
            SdkSpecializedValueCodec.ToSdkOffsetMode((WorkerOffsetDirectionTypeValue)value.RadialProximityMode),
            value.ProjectToPlane,
            value.AssertPlaneBoundaries);

    private static bool SetColorizationOptions(
        ISpatialAnalyzerSdkCalls sdk,
        string name,
        WorkerColorizationOptionsValue value) =>
        sdk.SetColorizationOptionsArg(
            name,
            SdkSpecializedValueCodec.ToSdkString((WorkerColorRangeMethodValue)value.ColorRangeMethod),
            SdkSpecializedValueCodec.ToSdkString((WorkerBaseColorTypeValue)value.BaseHighColor),
            SdkSpecializedValueCodec.ToSdkString((WorkerBaseMidColorTypeValue)value.BaseMidColor),
            SdkSpecializedValueCodec.ToSdkString((WorkerBaseColorTypeValue)value.BaseLowColor),
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


    private static string? ReportDestination(WorkerReportOutputOptionsValue value)
    {
        if ((value.ExternalPath is not null) == (value.EmbeddedFile is not null))
        {
            return null;
        }

        return value.ExternalPath ??
            $"{value.EmbeddedFile!.CollectionName}::{value.EmbeddedFile.FileName}";
    }
    private static WorkerMpOutputValue GetFitConstraintScalarOptions(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
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
        return WorkerMpOutputValue.FromRetrieval(
            argument.Name, argument.Kind, retrieved,
            retrieved
                ? new WorkerFitConstraintScalarOptionsValue(
                    new WorkerToleranceLimit(highEnabled, highValue),
                    new WorkerToleranceLimit(lowEnabled, lowValue))
                : null);
    }

    private static WorkerMpOutputValue GetToleranceScalarOptions(
        ISpatialAnalyzerSdkCalls sdk,
        WorkerMpOutputArgument argument)
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
        return WorkerMpOutputValue.FromRetrieval(
            argument.Name, argument.Kind, retrieved,
            retrieved
                ? new WorkerToleranceScalarOptionsValue(
                    new WorkerToleranceLimit(highEnabled, highValue),
                    new WorkerToleranceLimit(lowEnabled, lowValue))
                : null);
    }
}
