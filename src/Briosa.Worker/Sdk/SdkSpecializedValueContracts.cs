namespace Briosa.Worker.Sdk;

internal interface ISdkSpecializedEnumValue;

internal sealed record SdkSpecializedEnumValue<T>(T Value) : ISdkSpecializedEnumValue
    where T : struct, Enum;

internal enum SdkAsciiFileFormatValue
{
    Xyz,
    XyzOffsetOffset2,
    XyzNotes,
    RadiusThetaPhi,
    RadiusThetaZ,
    PointNameXyz,
    PointNameXyzNotes,
    PointNameXyzOffsetOffset2,
    PointNameXyzUxUyUz,
    PointNameXyzTxTyTzTd,
    PointNameXyzWxWyWzWmag,
    PointNameXyzHighLowTolerance,
    PointNameXyzTxTyTzTdWxWyWz,
    PointNameXyzWxWyWzTxTyTzTd,
    PointNameXyzHighLowToleranceWxWyWz,
    PointNameXyzWxWyWzHighLowTolerance,
    PointNameRadiusThetaPhi,
    PointNameRadiusThetaZ,
    PointNameXyzGroupName,
    PointNameYxzGroupName,
    GroupNamePointNameXyz,
    GroupNamePointNameXyzOffsetOffset2,
    GroupNamePointNameXyzNotes,
    GroupNamePointNameXyzUxUyUz,
    GroupNamePointNameRadiusThetaPhi,
    GroupNamePointNameRadiusThetaZ,
    CollectionGroupPointXyz,
    CollectionGroupPointXyzNotes,
    CollectionGroupPointRadiusThetaPhi,
    CollectionGroupPointRadiusThetaZ,
    XyzIjk,
    VectorNameXyzIjk,
    VectorNameXyzDxDyDzSignedMagnitude,
    VectorGroupNameVectorNameXyzIjk,
    VectorGroupNameVectorNameXyzDxDyDzSignedMagnitude,
    FrameNameXyzRxRyRzTimestamp,
    FrameNameXyzEulerXyzTimestamp,
    FrameNameXyzEulerZyxTimestamp,
    FrameNameXyzEulerZyzTimestamp,
    FrameNameXyzEulerZxzTimestamp,
    FrameNameTransformationMatrixTimestamp,
    TransformationMatrixTimestamp,
    FrameNameXyzQuaternionTimestamp,
    PlaneNameXyzDxDyDzPlaneSize
}

internal enum SdkAxisIdentifierValue
{
    PositiveX,
    NegativeX,
    PositiveY,
    NegativeY,
    PositiveZ,
    NegativeZ,
    X,
    Y,
    Z
}

internal enum SdkBaseColorTypeValue { Red, Green, Blue }
internal enum SdkBaseMidColorTypeValue { Red, Green, Gray, Blue }
internal enum SdkChartTypeValue { RunChart, IndividualXMovingRange, BullseyeChart }
internal enum SdkCollimationBaselineTypeValue { DeterminedByValue, DeterminedFromScale, DeterminedFromKnownPoint }
internal enum SdkCollimationTypeValue { FullCollimation, NoTiltCollimation }
internal enum SdkColorRangeMethodValue { SingleColor, Continuous, TolerancedContinuous, TolerancedGoNoGo, TolerancedGoNoGoWithWarning, DiscreteColors }
internal enum SdkCoordinateSystemTypeValue { Cartesian, Cylindric, Polar }
internal enum SdkDatasetTypeValue { X, Y, Z, Magnitude }
internal enum SdkDynamicCircleModeValue { CylinderPlaneHoldPlaneNormal, CylinderPlaneHoldCylinderAxis, ConePlaneHoldPlaneNormal, ConePlaneHoldConeAxis, SpherePlaneIntersection, TwoConesIntersection, ConeCylinderIntersection }
internal enum SdkDynamicEllipseModeValue { CylinderPlaneIntersection, ConePlaneIntersection }
internal enum SdkDynamicLineModeValue { ConeAxis, CylinderAxis, IntersectionOfTwoPlanes, BisectTwoLines, SlotCenterlineAlongLength }
internal enum SdkDynamicPlaneModeValue { BisectTwoPlanes, TwoConesBestFitPlane, TwoConesFirstConeAxis, TwoConesSecondConeAxis, ConeCylinderBestFitPlane, ConeCylinderConeAxis, ConeCylinderCylinderAxis, OffsetPlaneFromPlane }
internal enum SdkDynamicPointModeValue { IntersectionLinePlane, IntersectionCylinderPlane, IntersectionConePlane, IntersectionThreePlanes, MidPointPerpendicularTwoLines }
internal enum SdkEdgeModeValue { IncludeEdges, ExcludeEdges, EdgesOnly }
internal enum SdkExportDataDelimiterTypeValue { Space, Comma, Tab }
internal enum SdkExportTargetNameFormatValue { CollectionGroupTarget, GroupTarget, Target, None }
internal enum SdkExportVectorNameFormatValue { CollectionGroupVector, GroupVector, Vector, None }
internal enum SdkGeometryTypeValue { Line, Plane, Circle, Sphere, Cylinder, Cone, Paraboloid, Ellipse, Slot, Torus }

internal enum SdkInstrumentTypeValue
{
    AiconDpa,
    AiconMoveInspect,
    ApiIlt,
    AssemblyGuidanceLaserProjector,
    CreaformVxElements,
    DigitalNetworkLevel,
    FaroArm15m6Dof,
    FaroArm25m6Dof,
    FaroArm25m7Dof,
    FaroArm2m6Dof,
    FaroArm2m7Dof,
    FaroArm35m6Dof,
    FaroArm35m7Dof,
    FaroArm3m6Dof,
    FaroArm3m7Dof,
    FaroArm4m6Dof,
    FaroArm4m7Dof,
    FaroScannerPhotonLsFocus3d,
    GenericAuxDevice,
    GenericAuxDevice2,
    GenericPhotogrammetrySystem,
    GenericPhotogrammetrySystem2,
    LapCadProLaserProjector,
    LeicaGeosystemsRtc360,
    LeicaGeosystemsScanStationPxx,
    LeicaT1200TotalStation,
    LeicaTm6100aTheodolite,
    LeicaTs09TotalStation,
    LeicaTs15TotalStation,
    LeicaTs16TotalStation,
    LeicaTs20TotalStation,
    LeicaTs30TotalStation,
    LptLaserProjector,
    SaOpenAuxiliaryInstrument,
    SaOpenInstrument,
    SaPipeline,
    Surphaser10Scanner,
    SurphaserScanner,
    ViconTracker,
    XyzReferenceFrame
}

internal enum SdkObjectTypeValue
{
    Any,
    BSpline,
    Circle,
    Cloud,
    CrossSectionCloud,
    Cylinder,
    Datum,
    Ellipse,
    EnhancedCloud,
    Frame,
    FrameSet,
    Line,
    Paraboloid,
    Perimeter,
    Plane,
    PointGroup,
    PointSet,
    PolySurface,
    ScanStripeCloud,
    ScanStripeMesh,
    Slot,
    Sphere,
    Surface,
    Torus,
    VectorGroup
}

internal enum SdkOffsetDirectionTypeValue { Both, PositiveOnly, NegativeOnly }
internal enum SdkPointFilterInputTypeValue { CardinalPoints, InputPoints, NominalCardinalPoints }
internal enum SdkRelationshipWeightingModeValue { NormalizeEquationCount, NormalizeEquationCountAndToleranceWidth, ResetAllWeights, NormalizeSquareRootEquationCount, NormalizeSquareRootAndToleranceWidth }
internal enum SdkRenderModeTypeValue { Wireframe, HiddenLineRemoved, SolidAndEdges, Solid }
internal enum SdkReportPageOrientationValue { Portrait, Landscape }
internal enum SdkSaturationLimitTypeValue { Deviation, SigmaRule, Custom }
internal enum SdkShowUsmnDialogTypeValue { No, Yes, OnToleranceViolation }
internal enum SdkSurfaceAnalysisModeValue { None, Relationship, Normals, Curvature, DeviationRms, DeviationMax, DeviationAverage, DeviationMin, DeviationMaxAbsolute, DeviationMaxDelta, PseudoSurface }
internal enum SdkSurfaceDissectionModeTypeValue { EntireSolid, SelectFaces }
internal enum SdkTargetComputationMethodValue { UseMostRecentShotFromEachFace, UseOnlyMostRecentShot, DoNotChangePriorMeasurements, ForceNewPointForEachMeasurement, RemoveAllPriorShots, DeactivateAllPriorShots }
internal enum SdkTranslucencyTypeValue { Solid, Translucent, Wireframe }

internal enum SdkCloudThinningModeValue { None, Random, NthPoint }
internal enum SdkProjectionTypeValue { TargetToOffsetObjectVectors, OffsetObjectToTargetVectors, ProbeToObjectVectors, ObjectToProbeVectors, PointsOnProbeSurface, PointsOnOffsetObject, PointsOnObject }
internal enum SdkReportDetailsFormatValue { None, Single, MultiHorizontal, MultiVertical }
internal enum SdkReportOutputTypeValue { None, SaReport, SaDocument, Pdf, Rtf }
internal enum SdkReportViewTypeValue { None, CurrentView, CalloutView }

internal sealed record SdkAutoFilterProximitySettingsValue(
    double SurfaceInclusionProximity,
    double EdgeExclusionProximity,
    double PlanarInclusionProximity,
    double PlanarExclusionProximity,
    double RadialInclusionProximity,
    double GeometryExtractionTolerance,
    SdkOffsetDirectionTypeValue SurfaceProximityMode,
    SdkOffsetDirectionTypeValue PlanarProximityMode,
    SdkOffsetDirectionTypeValue RadialProximityMode,
    bool ProjectToPlane,
    bool AssertPlaneBoundaries);

internal sealed record SdkCloudThinningOptionsValue(
    SdkCloudThinningModeValue Mode,
    int PointIncrement,
    int MinimumNumberOfPoints,
    int MaximumNumberOfPoints);

internal sealed record SdkColorizationOptionsValue(
    SdkColorRangeMethodValue ColorRangeMethod,
    SdkBaseColorTypeValue BaseHighColor,
    SdkBaseMidColorTypeValue BaseMidColor,
    SdkBaseColorTypeValue BaseLowColor,
    bool DrawTubes,
    bool DrawArrowheads,
    bool IndicateValues,
    double VectorMagnification,
    int VectorWidth,
    bool DrawBlotches,
    double BlotchSize,
    bool ShowOutOfToleranceOnly,
    bool ShowColorBarInView,
    bool ShowColorBarPercentages,
    bool ShowColorBarFractions,
    double HighSaturationLimit,
    double LowSaturationLimit,
    double HighTolerance,
    double LowTolerance);

internal sealed record SdkFitConstraintScalarOptionsValue(
    SdkToleranceLimit High,
    SdkToleranceLimit Low);

internal sealed record SdkFitDegreeOfFreedomOptionsValue(
    bool AllowX,
    bool AllowY,
    bool AllowZ,
    bool AllowRx,
    bool AllowRy,
    bool AllowRz,
    bool RotateAboutCentroid);

internal sealed record SdkPointDeltaReportOptionsValue(
    SdkCoordinateSystemTypeValue CoordinateSystem,
    SdkReportDetailsFormatValue DetailsFormat,
    bool ShowPointA,
    bool ShowPointB,
    bool ShowDelta,
    bool ShowMagnitude,
    bool ShowComponent1,
    bool ShowComponent2,
    bool ShowComponent3,
    bool SortPointNames,
    bool ShowToleranceFields,
    bool ColorizeInToleranceFields);

internal sealed record SdkProjectionOptionsValue(
    SdkProjectionTypeValue ProjectionType,
    bool IgnoreEdgeProjections,
    bool OverrideTargetOffsets,
    double OverrideTargetOffsetsValue,
    bool AddExtraMaterialThickness,
    double ExtraMaterialThicknessValue);

internal sealed record SdkReportOutputOptionsValue(
    SdkReportOutputTypeValue OutputType,
    string PathOrEmbeddedName);

internal sealed record SdkReportViewOptionsValue(
    SdkReportViewTypeValue ViewType,
    string CollectionName,
    string CalloutName);

internal sealed record SdkToleranceScalarOptionsValue(
    SdkToleranceLimit High,
    SdkToleranceLimit Low);
