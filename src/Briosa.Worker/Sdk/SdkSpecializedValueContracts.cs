namespace Briosa.Worker.Sdk;

internal interface ISdkSpecializedEnumValue;

internal sealed record SdkSpecializedEnumValue<T>(T Value) : ISdkSpecializedEnumValue
    where T : struct, Enum;

internal enum SdkAsciiImportFileFormatValue
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

internal enum SdkAsciiFrameSetFormatValue
{
    FrameNameXyzRxRyRzTimestamp,
    FrameNameXyzEulerXyzTimestamp,
    FrameNameXyzEulerZyxTimestamp,
    FrameNameXyzEulerZyzTimestamp,
    FrameNameXyzEulerZxzTimestamp,
    FrameNameTransformationMatrixTimestamp,
    TransformationMatrixTimestamp,
    FrameNameXyzQuaternionTimestamp
}

internal enum SdkAxisIdentifierValue
{
    PositiveX,
    NegativeX,
    PositiveY,
    NegativeY,
    PositiveZ,
    NegativeZ
}

internal enum SdkWcfAxisIdentifierValue { X, Y, Z }

internal enum SdkBaseColorTypeValue { Red, Green, Blue }
internal enum SdkBaseMidColorTypeValue { Red, Green, Gray, Blue }
internal enum SdkChartTypeValue { RunChart, IndividualXMovingRange, BullseyeChart }
internal enum SdkCollimationBaselineTypeValue { DeterminedByValue, DeterminedFromScale, DeterminedFromKnownPoint }
internal enum SdkCollimationTypeValue { FullCollimation, NoTiltCollimation }
internal enum SdkColorRangeMethodValue { SingleColor, Continuous, TolerancedContinuous, TolerancedGoNoGo, TolerancedGoNoGoWithWarning, DiscreteColors }
internal enum SdkCoordinateSystemTypeValue { Cartesian, Cylindric, Polar }
internal enum SdkVectorComponentValue { X, Y, Z, Magnitude }
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
    XyzReferenceFrame,
    AiconProCam3DProbe,
    ApiLadar,
    ApiLaserRail,
    ApiOmniTrac,
    ApiOmniTrac2,
    ApiRadian,
    ApiRadianPlusCore,
    ApiRadianPro,
    ApiTrackerDeviceInterface,
    ApiTrackerIi,
    ApiTrackerIii,
    Axxis6100Arm26m6Dof,
    Axxis6200Arm32m6Dof,
    Axxis7100ArmProbe26m7Dof,
    Axxis7100ArmScanner26m7Dof,
    CimCoreArm1024,
    CimCoreArm1028,
    CimCoreArm1030,
    CimCoreArm2200,
    CimCoreArm2500,
    CimCoreArm6Dof3012i501212m,
    CimCoreArm6Dof3018i501818m,
    CimCoreArm6Dof3024i502424m,
    CimCoreArm6Dof3028i502828m,
    CimCoreArm6Dof3036i503636m,
    CimCoreArm6Dof511212m,
    CimCoreArm6Dof511818m,
    CimCoreArm6Dof512424m,
    CimCoreArm6Dof512828m,
    CimCoreArm6Dof513030m,
    CimCoreArm6Dof513636m,
    CimCoreArm7Dof5012Sc301212m,
    CimCoreArm7Dof5018Sc301818m,
    CimCoreArm7Dof5024Sc302424m,
    CimCoreArm7Dof5028Sc302828m,
    CimCoreArm7Dof5030Sc303030m,
    CimCoreArm7Dof5036Sc303636m,
    CimCoreArm7Dof5112Sc12m,
    CimCoreArm7Dof5118Sc18m,
    CimCoreArm7Dof5124Sc24m,
    CimCoreArm7Dof5128Sc28m,
    CimCoreArm7Dof5130Sc30m,
    CimCoreArm7Dof5136Sc36m,
    CubicKitTheodolite,
    DavisPerceptionIiWeatherStation,
    FaroArm,
    FaroArmG04,
    FaroArmG04057Dof,
    FaroArmG08,
    FaroArmG08057Dof,
    FaroArmG12,
    FaroArmG12057Dof,
    FaroArmS08,
    FaroArmS12,
    FaroArmUsb10FtQuantumFusionPrimePlatinum,
    FaroArmUsb10Ft7DofQuantumFusionPrimePlatinum,
    FaroArmUsb12FtQuantumFusionPrimePlatinum,
    FaroArmUsb12Ft7DofEdgeQuantumFusionPrimePlatinum,
    FaroArmUsb4FtQuantumPrimePlatinum,
    FaroArmUsb4Ft7DofQuantumPrimePlatinum,
    FaroArmUsb6FtQuantumFusionPrimePlatinum,
    FaroArmUsb6Ft7DofEdgeQuantumFusionPrimePlatinum,
    FaroArmUsb8FtQuantumFusionPrimePlatinum,
    FaroArmUsb8Ft7DofQuantumFusionPrimePlatinum,
    FaroArmUsb9Ft7DofEdge,
    FaroIonTracker,
    FaroTracker,
    FaroVantage,
    GsiVStarsPhotogrammetrySystem,
    HexagonAbsolute86Dof12mCompact,
    HexagonAbsolute86Dof25m,
    HexagonAbsolute86Dof2m,
    HexagonAbsolute86Dof35m,
    HexagonAbsolute86Dof3m,
    HexagonAbsolute86Dof45m,
    HexagonAbsolute86Dof4m,
    HexagonAbsolute87Dof25m,
    HexagonAbsolute87Dof2m,
    HexagonAbsolute87Dof35m,
    HexagonAbsolute87Dof3m,
    HexagonAbsolute87Dof45m,
    HexagonAbsolute87Dof4m,
    HexagonHandheld3DScanner,
    ImportedMeasurementsWithUncertainty,
    KernE2Theodolite,
    KreonApiAce620,
    KreonApiAce625,
    KreonApiAce630,
    KreonApiAce635,
    KreonApiAce640,
    KreonApiAce645,
    KreonApiAce720,
    KreonApiAce725,
    KreonApiAce730,
    KreonApiAce735,
    KreonApiAce740,
    KreonApiAce745,
    LeicaAt500,
    LeicaAt960930,
    LeicaAts600,
    LeicaAts800,
    LeicaEmSconAbsoluteTrackerAt901Series,
    LeicaEmSconAt401,
    LeicaEmSconAt402,
    LeicaEmSconAt403,
    LeicaEmSconTrackerLt500800Series,
    LeicaNovaMs50TotalStation,
    LeicaNovaMs60TotalStation,
    LeicaTda5005TotalStationGeoCOM,
    LeicaTdra6000TotalStation,
    LeicaTotalStationTc2000Tc2002,
    LeicaTpsTheodolite1800,
    LeicaTpsTheodolite5100,
    LeicaTpsTotalStation200350005005,
    LeicaTrackerTpLink,
    LeicaWildTheodolitesT2000T2002T3000,
    MetronorPortableMeasurementSystem,
    MitutoyoSpaceTracA,
    MitutoyoSpaceTracAi,
    MitutoyoSpaceTracAp,
    NikonMetrologyApdisMv400,
    NikonMetrologyLaserRadarMv200,
    NikonMetrologyLaserRadarMv300,
    NikonMetrologySurveyorV2,
    Nivel20TwoAxisLevel,
    OnTrakLaserLineSystemOt4040Ot6000,
    RomerAbsolute7315,
    RomerAbsolute7x20,
    RomerAbsolute7x20SiSe,
    RomerAbsolute7x25,
    RomerAbsolute7x25SiSe,
    RomerAbsolute7x30,
    RomerAbsolute7x30SiSe,
    RomerAbsolute7x35,
    RomerAbsolute7x35SiSe,
    RomerAbsolute7x40,
    RomerAbsolute7x40SiSe,
    RomerAbsolute7x45,
    RomerAbsolute7x45SiSe,
    RomerMultiGage,
    SokkiaNet1TotalStation,
    SokkiaNet2TotalStation,
    SokkiaNet05AXTotalStation,
    SokkiaNet05XTotalStation,
    SokkiaSetxTotalStation,
    ThommenHm30WeatherStation,
    TopconMsAxSeriesTotalStation,
    UltrasonicThicknessGaugeCl400,
    VirtekLaserProjector,
    ZeissETh2Theodolite
}

internal enum SdkObjectTypeValue
{
    Any,
    BSpline,
    Circle,
    Cloud,
    EnhancedCloud,
    ScanStripeCloud,
    CrossSectionCloud,
    Cone,
    Cylinder,
    Datum,
    Ellipse,
    Frame,
    FrameSet,
    Line,
    Paraboloid,
    Perimeter,
    Plane,
    PointGroup,
    PointSet,
    PolySurface,
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


internal sealed record SdkEmbeddedReportFileValue(
    string CollectionName,
    string FileName);

internal sealed record SdkReportOutputOptionsValue(
    SdkReportOutputTypeValue OutputType,
    string? ExternalPath,
    SdkEmbeddedReportFileValue? EmbeddedFile);

internal sealed record SdkReportViewOptionsValue(
    SdkReportViewTypeValue ViewType,
    string CollectionName,
    string CalloutName);

internal sealed record SdkToleranceScalarOptionsValue(
    SdkToleranceLimit High,
    SdkToleranceLimit Low);
