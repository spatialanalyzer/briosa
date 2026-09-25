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
            BooleanValue: ((argument.Value as WorkerBooleanValue)?.Value),
            IntegerValue: ((argument.Value as WorkerIntegerValue)?.Value),
            DoubleValue: ((argument.Value as WorkerDoubleValue)?.Value),
            StringValue: ((argument.Value as WorkerTextValue)?.Value),
            PointNameValue: (argument.Value as WorkerPointNameValue),
            VectorValue: (argument.Value as WorkerVectorValue),
            ToleranceVectorOptionsValue: (argument.Value as WorkerToleranceVectorOptionsValue),
            CollectionInstrumentIdValue: (argument.Value as WorkerCollectionInstrumentIdValue),
            CollectionInstrumentIdListValue: (argument.Value as WorkerCollectionInstrumentIdListValue),
            CollectionMachineIdValue: (argument.Value as WorkerCollectionMachineIdValue),
            CollectionItemNameValue: (argument.Value as WorkerCollectionItemNameValue),
            CollectionItemNameListValue: (argument.Value as WorkerCollectionItemNameListValue),
            CollectionObjectNameValue: (argument.Value as WorkerCollectionObjectNameValue),
            CollectionObjectNameListValue: (argument.Value as WorkerCollectionObjectNameListValue),
            CollectionGroupNameListValue: (argument.Value as WorkerCollectionGroupNameListValue),
            CollectionVectorGroupNameValue: (argument.Value as WorkerCollectionVectorGroupNameValue),
            CollectionVectorGroupNameListValue: (argument.Value as WorkerCollectionVectorGroupNameListValue),
            PointNameListValue: (argument.Value as WorkerPointNameListValue),
            StringListValue: (argument.Value as WorkerStringListValue),
            VectorNameListValue: (argument.Value as WorkerVectorNameListValue),
            DoubleArrayValue: (argument.Value as WorkerDoubleArrayValue),
            TransformValue: (argument.Value as WorkerTransformValue),
            WorldTransformValue: (argument.Value as WorkerWorldTransformValue),
            RgbColorValue: (argument.Value as WorkerRgbColorValue),
            FileReferenceValue: (argument.Value as WorkerFileReferenceValue),
            AngularUnitValue: ((argument.Value as WorkerAngularUnitChoice)?.Value),
            DistanceUnitValue: ((argument.Value as WorkerDistanceUnitChoice)?.Value),
            TemperatureUnitValue: ((argument.Value as WorkerTemperatureUnitChoice)?.Value),
            FontValue: (argument.Value as WorkerFontValue),
            SpecializedEnumValue: ToSdkSpecializedEnum(argument),
            AutoFilterProximitySettingsValue: ToSdkAutoFilter((argument.Value as WorkerAutoFilterProximitySettingsValue)),
            BSplineFitOptionsValue: ToSdkBSplineFit((argument.Value as WorkerBSplineFitOptionsValue)),
            CloudThinningOptionsValue: ToSdkCloudThinning((argument.Value as WorkerCloudThinningOptionsValue)),
            ColorizationOptionsValue: ToSdkColorization((argument.Value as WorkerColorizationOptionsValue)),
            FitConstraintScalarOptionsValue: (argument.Value as WorkerFitConstraintScalarOptionsValue),
            FitDegreeOfFreedomOptionsValue: ToSdkFitDegreeOfFreedom((argument.Value as WorkerFitDegreeOfFreedomOptionsValue)),
            ReportOutputOptionsValue: ToSdkReportOutput((argument.Value as WorkerReportOutputOptionsValue)),
            ReportViewOptionsValue: ToSdkReportView((argument.Value as WorkerReportViewOptionsValue)),
            ToleranceScalarOptionsValue: (argument.Value as WorkerToleranceScalarOptionsValue),
            ProjectionOptionsValue: ToSdkProjection((argument.Value as WorkerProjectionOptionsValue)),
            PointDeltaReportOptionsValue: ToSdkPointDeltaReport((argument.Value as WorkerPointDeltaReportOptionsValue)),
            UdpTransmitSettingsValue: (argument.Value as WorkerUdpTransmitSettingsValue) is { } udp
                ? new SdkUdpTransmitSettingsValue(udp.Enabled, udp.Broadcast, udp.IpAddress, udp.Port) : null,
            SdkBinding: argument.SdkBinding);


    private static ISdkSpecializedEnumValue? ToSdkSpecializedEnum(WorkerMpInputArgument argument) =>
        argument.Value is WorkerSpecializedEnumValue choice
            ? argument.Kind switch
            {
                WorkerMpValueKind.AsciiImportFileFormat => ToSdkEnum((SdkAsciiImportFileFormatValue)choice.Value),
                WorkerMpValueKind.AsciiFrameSetFormat => ToSdkEnum((SdkAsciiFrameSetFormatValue)choice.Value),
                WorkerMpValueKind.AxisIdentifier => ToSdkEnum((SdkAxisIdentifierValue)choice.Value),
                WorkerMpValueKind.WcfAxisIdentifier => ToSdkEnum((SdkWcfAxisIdentifierValue)choice.Value),
                WorkerMpValueKind.BaseColorType => ToSdkEnum((SdkBaseColorTypeValue)choice.Value),
                WorkerMpValueKind.BaseMidColorType => ToSdkEnum((SdkBaseMidColorTypeValue)choice.Value),
                WorkerMpValueKind.ChartType => ToSdkEnum((SdkChartTypeValue)choice.Value),
                WorkerMpValueKind.CollimationBaselineType => ToSdkEnum((SdkCollimationBaselineTypeValue)choice.Value),
                WorkerMpValueKind.CollimationType => ToSdkEnum((SdkCollimationTypeValue)choice.Value),
                WorkerMpValueKind.ColorRangeMethod => ToSdkEnum((SdkColorRangeMethodValue)choice.Value),
                WorkerMpValueKind.CoordinateSystemType => ToSdkEnum((SdkCoordinateSystemTypeValue)choice.Value),
                WorkerMpValueKind.VectorComponent => ToSdkEnum((SdkVectorComponentValue)choice.Value),
                WorkerMpValueKind.DynamicCircleMode => ToSdkEnum((SdkDynamicCircleModeValue)choice.Value),
                WorkerMpValueKind.DynamicEllipseMode => ToSdkEnum((SdkDynamicEllipseModeValue)choice.Value),
                WorkerMpValueKind.DynamicLineMode => ToSdkEnum((SdkDynamicLineModeValue)choice.Value),
                WorkerMpValueKind.DynamicPlaneMode => ToSdkEnum((SdkDynamicPlaneModeValue)choice.Value),
                WorkerMpValueKind.DynamicPointMode => ToSdkEnum((SdkDynamicPointModeValue)choice.Value),
                WorkerMpValueKind.EdgeMode => ToSdkEnum((SdkEdgeModeValue)choice.Value),
                WorkerMpValueKind.ExportDataDelimiterType => ToSdkEnum((SdkExportDataDelimiterTypeValue)choice.Value),
                WorkerMpValueKind.ExportTargetNameFormat => ToSdkEnum((SdkExportTargetNameFormatValue)choice.Value),
                WorkerMpValueKind.ExportVectorNameFormat => ToSdkEnum((SdkExportVectorNameFormatValue)choice.Value),
                WorkerMpValueKind.GeometryType => ToSdkEnum((SdkGeometryTypeValue)choice.Value),
                WorkerMpValueKind.GdtDistanceBetweenMode => ToSdkEnum((SdkGdtDistanceBetweenModeValue)choice.Value),
                WorkerMpValueKind.GdtEvaluationMethod => ToSdkEnum((SdkGdtEvaluationMethodValue)choice.Value),
                WorkerMpValueKind.InstrumentType => ToSdkEnum((SdkInstrumentTypeValue)choice.Value),
                WorkerMpValueKind.ObjectType => ToSdkEnum((WorkerObjectTypeValue)(choice.Value + 1)),
                WorkerMpValueKind.OffsetDirectionType => ToSdkEnum((SdkOffsetDirectionTypeValue)choice.Value),
                WorkerMpValueKind.PointFilterInputType => ToSdkEnum((SdkPointFilterInputTypeValue)choice.Value),
                WorkerMpValueKind.RelationshipWeightingMode => ToSdkEnum((SdkRelationshipWeightingModeValue)choice.Value),
                WorkerMpValueKind.RenderModeType => ToSdkEnum((SdkRenderModeTypeValue)choice.Value),
                WorkerMpValueKind.ReportPageOrientation => ToSdkEnum((SdkReportPageOrientationValue)choice.Value),
                WorkerMpValueKind.SaturationLimitType => ToSdkEnum((SdkSaturationLimitTypeValue)choice.Value),
                WorkerMpValueKind.ShowUsmnDialogType => ToSdkEnum((SdkShowUsmnDialogTypeValue)choice.Value),
                WorkerMpValueKind.SurfaceAnalysisMode => ToSdkEnum((SdkSurfaceAnalysisModeValue)choice.Value),
                WorkerMpValueKind.SurfaceDissectionModeType => ToSdkEnum((SdkSurfaceDissectionModeTypeValue)choice.Value),
                WorkerMpValueKind.TargetComputationMethod => ToSdkEnum((SdkTargetComputationMethodValue)choice.Value),
                WorkerMpValueKind.TranslucencyType => ToSdkEnum((SdkTranslucencyTypeValue)choice.Value),
                WorkerMpValueKind.CompTechnique => ToSdkEnum((SdkCompTechniqueValue)choice.Value),
                WorkerMpValueKind.DegreeOfFreedom => ToSdkEnum((SdkDegreeOfFreedomValue)choice.Value),
                WorkerMpValueKind.FitMethod => ToSdkEnum((SdkFitMethodValue)choice.Value),
                WorkerMpValueKind.MeasuredSideForPlanarOffset => ToSdkEnum((SdkMeasuredSideForPlanarOffsetValue)choice.Value),
                WorkerMpValueKind.MeasuredSideForRadialOffset => ToSdkEnum((SdkMeasuredSideForRadialOffsetValue)choice.Value),
                WorkerMpValueKind.MpDialogInteractionMode => ToSdkEnum((SdkMpDialogInteractionModeValue)choice.Value),
                WorkerMpValueKind.MpInteractionMode => ToSdkEnum((SdkMpInteractionModeValue)choice.Value),
                WorkerMpValueKind.NormalDirection => ToSdkEnum((SdkNormalDirectionValue)choice.Value),
                WorkerMpValueKind.SaInteractionMode => ToSdkEnum((SdkSaInteractionModeValue)choice.Value),
                WorkerMpValueKind.SlotType => ToSdkEnum((SdkSlotTypeValue)choice.Value),
                WorkerMpValueKind.SphereFitComputationMode => ToSdkEnum((SdkSphereFitComputationModeValue)choice.Value),
                WorkerMpValueKind.WindowState => ToSdkEnum((SdkWindowStateValue)choice.Value),
                WorkerMpValueKind.SystemString => ToSdkEnum((SdkSystemStringValue)choice.Value),
                _ => null
            }
            : null;

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
