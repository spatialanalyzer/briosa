using Briosa.Worker.Control;

namespace Briosa.Worker.Sdk;

// Temporary boundary for specialized input options. Basic values and execution
// results already use the target-local shared model without conversion/copying.
internal static class SdkCommandMapper
{
    internal static SdkCommand CreateCommand(WorkerMpCommand command) =>
        new(
            command.OperationId,
            command.StepName,
            [.. command.InputArguments.Select(ToSdkInputArgument)],
            command.OutputArguments);

    private static SdkInputArgument ToSdkInputArgument(WorkerMpInputArgument argument) =>
        new(
            argument.Name, argument.Kind,
            BooleanValue: argument.BooleanValue,
            IntegerValue: argument.IntegerValue,
            DoubleValue: argument.DoubleValue,
            StringValue: argument.StringValue,
            PointNameValue: argument.PointNameValue,
            VectorValue: argument.VectorValue,
            ToleranceVectorOptionsValue: argument.ToleranceVectorOptionsValue,
            CollectionInstrumentIdValue: argument.CollectionInstrumentIdValue,
            CollectionInstrumentIdListValue: argument.CollectionInstrumentIdListValue,
            CollectionMachineIdValue: argument.CollectionMachineIdValue,
            CollectionItemNameValue: argument.CollectionItemNameValue,
            CollectionItemNameListValue: argument.CollectionItemNameListValue,
            CollectionObjectNameValue: argument.CollectionObjectNameValue,
            CollectionObjectNameListValue: argument.CollectionObjectNameListValue,
            CollectionGroupNameListValue: argument.CollectionGroupNameListValue,
            CollectionVectorGroupNameValue: argument.CollectionVectorGroupNameValue,
            CollectionVectorGroupNameListValue: argument.CollectionVectorGroupNameListValue,
            PointNameListValue: argument.PointNameListValue,
            StringListValue: argument.StringListValue,
            VectorNameListValue: argument.VectorNameListValue,
            DoubleArrayValue: argument.DoubleArrayValue,
            TransformValue: argument.TransformValue,
            WorldTransformValue: argument.WorldTransformValue,
            RgbColorValue: argument.RgbColorValue,
            FileReferenceValue: argument.FileReferenceValue,
            AngularUnitValue: argument.AngularUnitValue,
            DistanceUnitValue: argument.DistanceUnitValue,
            TemperatureUnitValue: argument.TemperatureUnitValue,
            FontValue: argument.FontValue,
            SpecializedEnumValue: ToSdkSpecializedEnum(argument),
            AutoFilterProximitySettingsValue: ToSdkAutoFilter(argument.AutoFilterProximitySettingsValue),
            BSplineFitOptionsValue: ToSdkBSplineFit(argument.BSplineFitOptionsValue),
            CloudThinningOptionsValue: ToSdkCloudThinning(argument.CloudThinningOptionsValue),
            ColorizationOptionsValue: ToSdkColorization(argument.ColorizationOptionsValue),
            FitConstraintScalarOptionsValue: argument.FitConstraintScalarOptionsValue,
            FitDegreeOfFreedomOptionsValue: ToSdkFitDegreeOfFreedom(argument.FitDegreeOfFreedomOptionsValue),
            ReportOutputOptionsValue: ToSdkReportOutput(argument.ReportOutputOptionsValue),
            ReportViewOptionsValue: ToSdkReportView(argument.ReportViewOptionsValue),
            ToleranceScalarOptionsValue: argument.ToleranceScalarOptionsValue,
            ProjectionOptionsValue: ToSdkProjection(argument.ProjectionOptionsValue),
            PointDeltaReportOptionsValue: ToSdkPointDeltaReport(argument.PointDeltaReportOptionsValue),
            UdpTransmitSettingsValue: argument.UdpTransmitSettingsValue is { } udp
                ? new SdkUdpTransmitSettingsValue(udp.Enabled, udp.Broadcast, udp.IpAddress, udp.Port) : null,
            SdkBinding: argument.SdkBinding);


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
                WorkerMpValueKind.ObjectType => ToSdkEnum((WorkerObjectTypeValue)(argument.SpecializedEnumValue.Value + 1)),
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

}
