using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "valueType")]
[JsonDerivedType(typeof(WorkerBooleanValue), 1)]
[JsonDerivedType(typeof(WorkerIntegerValue), 2)]
[JsonDerivedType(typeof(WorkerDoubleValue), 3)]
[JsonDerivedType(typeof(WorkerTextValue), 4)]
[JsonDerivedType(typeof(WorkerPointNameValue), 5)]
[JsonDerivedType(typeof(WorkerVectorValue), 6)]
[JsonDerivedType(typeof(WorkerToleranceVectorOptionsValue), 7)]
[JsonDerivedType(typeof(WorkerCollectionInstrumentIdValue), 8)]
[JsonDerivedType(typeof(WorkerCollectionInstrumentIdListValue), 9)]
[JsonDerivedType(typeof(WorkerCollectionMachineIdValue), 10)]
[JsonDerivedType(typeof(WorkerCollectionItemNameValue), 11)]
[JsonDerivedType(typeof(WorkerCollectionItemNameListValue), 12)]
[JsonDerivedType(typeof(WorkerCollectionObjectNameValue), 13)]
[JsonDerivedType(typeof(WorkerCollectionObjectNameListValue), 14)]
[JsonDerivedType(typeof(WorkerCollectionGroupNameListValue), 15)]
[JsonDerivedType(typeof(WorkerCollectionVectorGroupNameValue), 16)]
[JsonDerivedType(typeof(WorkerCollectionVectorGroupNameListValue), 17)]
[JsonDerivedType(typeof(WorkerPointNameListValue), 18)]
[JsonDerivedType(typeof(WorkerStringListValue), 19)]
[JsonDerivedType(typeof(WorkerVectorNameListValue), 20)]
[JsonDerivedType(typeof(WorkerDoubleArrayValue), 21)]
[JsonDerivedType(typeof(WorkerTransformValue), 22)]
[JsonDerivedType(typeof(WorkerWorldTransformValue), 23)]
[JsonDerivedType(typeof(WorkerFileReferenceValue), 24)]
[JsonDerivedType(typeof(WorkerFitConstraintScalarOptionsValue), 25)]
[JsonDerivedType(typeof(WorkerToleranceScalarOptionsValue), 26)]
[JsonDerivedType(typeof(WorkerRgbColorValue), 27)]
[JsonDerivedType(typeof(WorkerAngularUnitChoice), 28)]
[JsonDerivedType(typeof(WorkerDistanceUnitChoice), 29)]
[JsonDerivedType(typeof(WorkerTemperatureUnitChoice), 30)]
[JsonDerivedType(typeof(WorkerFontValue), 31)]
[JsonDerivedType(typeof(WorkerAutoFilterProximitySettingsValue), 33)]
[JsonDerivedType(typeof(WorkerBSplineFitOptionsValue), 34)]
[JsonDerivedType(typeof(WorkerCloudThinningOptionsValue), 35)]
[JsonDerivedType(typeof(WorkerColorizationOptionsValue), 36)]
[JsonDerivedType(typeof(WorkerFitDegreeOfFreedomOptionsValue), 37)]
[JsonDerivedType(typeof(WorkerReportOutputOptionsValue), 38)]
[JsonDerivedType(typeof(WorkerReportViewOptionsValue), 39)]
[JsonDerivedType(typeof(WorkerProjectionOptionsValue), 40)]
[JsonDerivedType(typeof(WorkerPointDeltaReportOptionsValue), 41)]
[JsonDerivedType(typeof(WorkerUdpTransmitSettingsValue), 42)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerAsciiImportFileFormatValue>), 100)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerAsciiFrameSetFormatValue>), 101)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerAxisIdentifierValue>), 102)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerWcfAxisIdentifierValue>), 103)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerBaseColorTypeValue>), 104)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerBaseMidColorTypeValue>), 105)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerChartTypeValue>), 106)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerCollimationBaselineTypeValue>), 107)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerCollimationTypeValue>), 108)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerColorRangeMethodValue>), 109)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerCoordinateSystemTypeValue>), 110)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerVectorComponentValue>), 111)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerDynamicCircleModeValue>), 112)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerDynamicEllipseModeValue>), 113)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerDynamicLineModeValue>), 114)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerDynamicPlaneModeValue>), 115)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerDynamicPointModeValue>), 116)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerEdgeModeValue>), 117)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerExportDataDelimiterTypeValue>), 118)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerExportTargetNameFormatValue>), 119)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerExportVectorNameFormatValue>), 120)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerGeometryTypeValue>), 121)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerGdtDistanceBetweenModeValue>), 122)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerGdtEvaluationMethodValue>), 123)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerInstrumentTypeValue>), 124)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerObjectTypeValue>), 125)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerOffsetDirectionTypeValue>), 126)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerPointFilterInputTypeValue>), 127)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerRelationshipWeightingModeValue>), 128)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerRenderModeTypeValue>), 129)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerReportPageOrientationValue>), 130)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerSaturationLimitTypeValue>), 131)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerShowUsmnDialogTypeValue>), 132)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerSurfaceAnalysisModeValue>), 133)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerSurfaceDissectionModeTypeValue>), 134)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerTargetComputationMethodValue>), 135)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerTranslucencyTypeValue>), 136)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerCompTechniqueValue>), 137)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerDegreeOfFreedomValue>), 138)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerFitMethodValue>), 139)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerMeasuredSideForPlanarOffsetValue>), 140)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerMeasuredSideForRadialOffsetValue>), 141)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerMpDialogInteractionModeValue>), 142)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerMpInteractionModeValue>), 143)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerNormalDirectionValue>), 144)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerSaInteractionModeValue>), 145)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerSlotTypeValue>), 146)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerSphereFitComputationModeValue>), 147)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerWindowStateValue>), 148)]
[JsonDerivedType(typeof(WorkerChoiceValue<WorkerSystemStringValue>), 149)]
public abstract record WorkerMpValue
{
    private protected WorkerMpValue() { }

    internal bool MatchesOutputKind(WorkerMpValueKind kind) => this switch
    {
        WorkerBooleanValue => kind == WorkerMpValueKind.Logical,
        WorkerIntegerValue => kind == WorkerMpValueKind.WholeNumber,
        WorkerDoubleValue => kind == WorkerMpValueKind.FloatingPoint,
        WorkerTextValue => kind is WorkerMpValueKind.Text or WorkerMpValueKind.InstrumentTypeName or
            WorkerMpValueKind.ChartName or WorkerMpValueKind.CloudName or WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or WorkerMpValueKind.VectorGroupName or WorkerMpValueKind.ViewName,
        WorkerPointNameValue => kind == WorkerMpValueKind.PointName,
        WorkerVectorValue => kind == WorkerMpValueKind.Vector,
        WorkerToleranceVectorOptionsValue => kind == WorkerMpValueKind.ToleranceVectorOptions,
        WorkerCollectionInstrumentIdValue => kind == WorkerMpValueKind.CollectionInstrumentId,
        WorkerCollectionInstrumentIdListValue => kind == WorkerMpValueKind.CollectionInstrumentIdList,
        WorkerCollectionMachineIdValue => kind == WorkerMpValueKind.CollectionMachineId,
        WorkerCollectionItemNameValue => kind == WorkerMpValueKind.CollectionItemName,
        WorkerCollectionItemNameListValue => kind == WorkerMpValueKind.CollectionItemNameList,
        WorkerCollectionObjectNameValue => kind == WorkerMpValueKind.CollectionObjectName,
        WorkerCollectionObjectNameListValue => kind == WorkerMpValueKind.CollectionObjectNameList,
        WorkerCollectionGroupNameListValue => kind == WorkerMpValueKind.CollectionGroupNameList,
        WorkerCollectionVectorGroupNameValue => kind == WorkerMpValueKind.CollectionVectorGroupName,
        WorkerCollectionVectorGroupNameListValue => kind == WorkerMpValueKind.CollectionVectorGroupNameList,
        WorkerPointNameListValue => kind == WorkerMpValueKind.PointNameList,
        WorkerStringListValue => kind is WorkerMpValueKind.StringList or WorkerMpValueKind.EditText,
        WorkerVectorNameListValue => kind == WorkerMpValueKind.VectorNameList,
        WorkerDoubleArrayValue => kind == WorkerMpValueKind.DoubleArray,
        WorkerTransformValue => kind == WorkerMpValueKind.Transform,
        WorkerWorldTransformValue => kind == WorkerMpValueKind.WorldTransform,
        WorkerFileReferenceValue => kind == WorkerMpValueKind.FileReference,
        WorkerFitConstraintScalarOptionsValue => kind == WorkerMpValueKind.FitConstraintScalarOptions,
        WorkerToleranceScalarOptionsValue => kind == WorkerMpValueKind.ToleranceScalarOptions,
        _ => false
    };

    internal bool MatchesInputKind(WorkerMpValueKind kind) => MatchesOutputKind(kind) || this switch
    {
        WorkerRgbColorValue => kind == WorkerMpValueKind.RgbColor,
        WorkerAngularUnitChoice => kind == WorkerMpValueKind.AngularUnit,
        WorkerDistanceUnitChoice => kind == WorkerMpValueKind.DistanceUnit,
        WorkerTemperatureUnitChoice => kind == WorkerMpValueKind.TemperatureUnit,
        WorkerFontValue => kind == WorkerMpValueKind.Font,
        WorkerAutoFilterProximitySettingsValue => kind == WorkerMpValueKind.AutoFilterProximitySettings,
        WorkerBSplineFitOptionsValue => kind == WorkerMpValueKind.BSplineFitOptions,
        WorkerCloudThinningOptionsValue => kind == WorkerMpValueKind.CloudThinningOptions,
        WorkerColorizationOptionsValue => kind == WorkerMpValueKind.ColorizationOptions,
        WorkerFitDegreeOfFreedomOptionsValue => kind == WorkerMpValueKind.FitDegreeOfFreedomOptions,
        WorkerReportOutputOptionsValue => kind == WorkerMpValueKind.ReportOutputOptions,
        WorkerReportViewOptionsValue => kind == WorkerMpValueKind.ReportViewOptions,
        WorkerProjectionOptionsValue => kind == WorkerMpValueKind.ProjectionOptions,
        WorkerPointDeltaReportOptionsValue => kind == WorkerMpValueKind.PointDeltaReportOptions,
        WorkerUdpTransmitSettingsValue => kind == WorkerMpValueKind.UdpTransmitSettings,
        WorkerChoiceValue<WorkerAsciiImportFileFormatValue> => kind == WorkerMpValueKind.AsciiImportFileFormat,
        WorkerChoiceValue<WorkerAsciiFrameSetFormatValue> => kind == WorkerMpValueKind.AsciiFrameSetFormat,
        WorkerChoiceValue<WorkerAxisIdentifierValue> => kind == WorkerMpValueKind.AxisIdentifier,
        WorkerChoiceValue<WorkerWcfAxisIdentifierValue> => kind == WorkerMpValueKind.WcfAxisIdentifier,
        WorkerChoiceValue<WorkerBaseColorTypeValue> => kind == WorkerMpValueKind.BaseColorType,
        WorkerChoiceValue<WorkerBaseMidColorTypeValue> => kind == WorkerMpValueKind.BaseMidColorType,
        WorkerChoiceValue<WorkerChartTypeValue> => kind == WorkerMpValueKind.ChartType,
        WorkerChoiceValue<WorkerCollimationBaselineTypeValue> => kind == WorkerMpValueKind.CollimationBaselineType,
        WorkerChoiceValue<WorkerCollimationTypeValue> => kind == WorkerMpValueKind.CollimationType,
        WorkerChoiceValue<WorkerColorRangeMethodValue> => kind == WorkerMpValueKind.ColorRangeMethod,
        WorkerChoiceValue<WorkerCoordinateSystemTypeValue> => kind == WorkerMpValueKind.CoordinateSystemType,
        WorkerChoiceValue<WorkerVectorComponentValue> => kind == WorkerMpValueKind.VectorComponent,
        WorkerChoiceValue<WorkerDynamicCircleModeValue> => kind == WorkerMpValueKind.DynamicCircleMode,
        WorkerChoiceValue<WorkerDynamicEllipseModeValue> => kind == WorkerMpValueKind.DynamicEllipseMode,
        WorkerChoiceValue<WorkerDynamicLineModeValue> => kind == WorkerMpValueKind.DynamicLineMode,
        WorkerChoiceValue<WorkerDynamicPlaneModeValue> => kind == WorkerMpValueKind.DynamicPlaneMode,
        WorkerChoiceValue<WorkerDynamicPointModeValue> => kind == WorkerMpValueKind.DynamicPointMode,
        WorkerChoiceValue<WorkerEdgeModeValue> => kind == WorkerMpValueKind.EdgeMode,
        WorkerChoiceValue<WorkerExportDataDelimiterTypeValue> => kind == WorkerMpValueKind.ExportDataDelimiterType,
        WorkerChoiceValue<WorkerExportTargetNameFormatValue> => kind == WorkerMpValueKind.ExportTargetNameFormat,
        WorkerChoiceValue<WorkerExportVectorNameFormatValue> => kind == WorkerMpValueKind.ExportVectorNameFormat,
        WorkerChoiceValue<WorkerGeometryTypeValue> => kind == WorkerMpValueKind.GeometryType,
        WorkerChoiceValue<WorkerGdtDistanceBetweenModeValue> => kind == WorkerMpValueKind.GdtDistanceBetweenMode,
        WorkerChoiceValue<WorkerGdtEvaluationMethodValue> => kind == WorkerMpValueKind.GdtEvaluationMethod,
        WorkerChoiceValue<WorkerInstrumentTypeValue> => kind == WorkerMpValueKind.InstrumentType,
        WorkerChoiceValue<WorkerObjectTypeValue> => kind == WorkerMpValueKind.ObjectType,
        WorkerChoiceValue<WorkerOffsetDirectionTypeValue> => kind == WorkerMpValueKind.OffsetDirectionType,
        WorkerChoiceValue<WorkerPointFilterInputTypeValue> => kind == WorkerMpValueKind.PointFilterInputType,
        WorkerChoiceValue<WorkerRelationshipWeightingModeValue> => kind == WorkerMpValueKind.RelationshipWeightingMode,
        WorkerChoiceValue<WorkerRenderModeTypeValue> => kind == WorkerMpValueKind.RenderModeType,
        WorkerChoiceValue<WorkerReportPageOrientationValue> => kind == WorkerMpValueKind.ReportPageOrientation,
        WorkerChoiceValue<WorkerSaturationLimitTypeValue> => kind == WorkerMpValueKind.SaturationLimitType,
        WorkerChoiceValue<WorkerShowUsmnDialogTypeValue> => kind == WorkerMpValueKind.ShowUsmnDialogType,
        WorkerChoiceValue<WorkerSurfaceAnalysisModeValue> => kind == WorkerMpValueKind.SurfaceAnalysisMode,
        WorkerChoiceValue<WorkerSurfaceDissectionModeTypeValue> => kind == WorkerMpValueKind.SurfaceDissectionModeType,
        WorkerChoiceValue<WorkerTargetComputationMethodValue> => kind == WorkerMpValueKind.TargetComputationMethod,
        WorkerChoiceValue<WorkerTranslucencyTypeValue> => kind == WorkerMpValueKind.TranslucencyType,
        WorkerChoiceValue<WorkerCompTechniqueValue> => kind == WorkerMpValueKind.CompTechnique,
        WorkerChoiceValue<WorkerDegreeOfFreedomValue> => kind == WorkerMpValueKind.DegreeOfFreedom,
        WorkerChoiceValue<WorkerFitMethodValue> => kind == WorkerMpValueKind.FitMethod,
        WorkerChoiceValue<WorkerMeasuredSideForPlanarOffsetValue> => kind == WorkerMpValueKind.MeasuredSideForPlanarOffset,
        WorkerChoiceValue<WorkerMeasuredSideForRadialOffsetValue> => kind == WorkerMpValueKind.MeasuredSideForRadialOffset,
        WorkerChoiceValue<WorkerMpDialogInteractionModeValue> => kind == WorkerMpValueKind.MpDialogInteractionMode,
        WorkerChoiceValue<WorkerMpInteractionModeValue> => kind == WorkerMpValueKind.MpInteractionMode,
        WorkerChoiceValue<WorkerNormalDirectionValue> => kind == WorkerMpValueKind.NormalDirection,
        WorkerChoiceValue<WorkerSaInteractionModeValue> => kind == WorkerMpValueKind.SaInteractionMode,
        WorkerChoiceValue<WorkerSlotTypeValue> => kind == WorkerMpValueKind.SlotType,
        WorkerChoiceValue<WorkerSphereFitComputationModeValue> => kind == WorkerMpValueKind.SphereFitComputationMode,
        WorkerChoiceValue<WorkerWindowStateValue> => kind == WorkerMpValueKind.WindowState,
        WorkerChoiceValue<WorkerSystemStringValue> => kind == WorkerMpValueKind.SystemString,
        _ => false
    };
}
