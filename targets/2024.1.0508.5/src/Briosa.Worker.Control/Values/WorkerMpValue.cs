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
[JsonDerivedType(typeof(WorkerSpecializedEnumValue), 32)]
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
        WorkerSpecializedEnumValue => kind is WorkerMpValueKind.AsciiImportFileFormat or
            WorkerMpValueKind.AsciiFrameSetFormat or
            WorkerMpValueKind.AxisIdentifier or
            WorkerMpValueKind.WcfAxisIdentifier or
            WorkerMpValueKind.BaseColorType or
            WorkerMpValueKind.BaseMidColorType or
            WorkerMpValueKind.ChartType or
            WorkerMpValueKind.CollimationBaselineType or
            WorkerMpValueKind.CollimationType or
            WorkerMpValueKind.ColorRangeMethod or
            WorkerMpValueKind.CoordinateSystemType or
            WorkerMpValueKind.VectorComponent or
            WorkerMpValueKind.DynamicCircleMode or
            WorkerMpValueKind.DynamicEllipseMode or
            WorkerMpValueKind.DynamicLineMode or
            WorkerMpValueKind.DynamicPlaneMode or
            WorkerMpValueKind.DynamicPointMode or
            WorkerMpValueKind.EdgeMode or
            WorkerMpValueKind.ExportDataDelimiterType or
            WorkerMpValueKind.ExportTargetNameFormat or
            WorkerMpValueKind.ExportVectorNameFormat or
            WorkerMpValueKind.GeometryType or
            WorkerMpValueKind.GdtDistanceBetweenMode or
            WorkerMpValueKind.GdtEvaluationMethod or
            WorkerMpValueKind.InstrumentType or
            WorkerMpValueKind.ObjectType or
            WorkerMpValueKind.OffsetDirectionType or
            WorkerMpValueKind.PointFilterInputType or
            WorkerMpValueKind.RelationshipWeightingMode or
            WorkerMpValueKind.RenderModeType or
            WorkerMpValueKind.ReportPageOrientation or
            WorkerMpValueKind.SaturationLimitType or
            WorkerMpValueKind.ShowUsmnDialogType or
            WorkerMpValueKind.SurfaceAnalysisMode or
            WorkerMpValueKind.SurfaceDissectionModeType or
            WorkerMpValueKind.TargetComputationMethod or
            WorkerMpValueKind.TranslucencyType or
            WorkerMpValueKind.CompTechnique or
            WorkerMpValueKind.DegreeOfFreedom or
            WorkerMpValueKind.FitMethod or
            WorkerMpValueKind.MeasuredSideForPlanarOffset or
            WorkerMpValueKind.MeasuredSideForRadialOffset or
            WorkerMpValueKind.MpDialogInteractionMode or
            WorkerMpValueKind.MpInteractionMode or
            WorkerMpValueKind.NormalDirection or
            WorkerMpValueKind.SaInteractionMode or
            WorkerMpValueKind.SlotType or
            WorkerMpValueKind.SphereFitComputationMode or
            WorkerMpValueKind.WindowState or
            WorkerMpValueKind.SystemString,
        _ => false
    };
}
