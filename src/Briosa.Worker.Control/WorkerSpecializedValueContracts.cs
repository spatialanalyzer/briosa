namespace Briosa.Worker.Control;

public sealed record WorkerSpecializedEnumValue(int Value);

public sealed record WorkerAutoFilterProximitySettingsValue(
    double SurfaceInclusionProximity,
    double EdgeExclusionProximity,
    double PlanarInclusionProximity,
    double PlanarExclusionProximity,
    double RadialInclusionProximity,
    double GeometryExtractionTolerance,
    int SurfaceProximityMode,
    int PlanarProximityMode,
    int RadialProximityMode,
    bool ProjectToPlane,
    bool AssertPlaneBoundaries);

public sealed record WorkerCloudThinningOptionsValue(
    int Mode,
    int PointIncrement,
    int MinimumNumberOfPoints,
    int MaximumNumberOfPoints);

public sealed record WorkerColorizationOptionsValue(
    int ColorRangeMethod,
    int BaseHighColor,
    int BaseMidColor,
    int BaseLowColor,
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

public sealed record WorkerScalarToleranceLimit(bool Enabled, double Value);

public sealed record WorkerFitConstraintScalarOptionsValue(
    WorkerScalarToleranceLimit High,
    WorkerScalarToleranceLimit Low);

public sealed record WorkerFitDegreeOfFreedomOptionsValue(
    bool AllowX,
    bool AllowY,
    bool AllowZ,
    bool AllowRx,
    bool AllowRy,
    bool AllowRz,
    bool RotateAboutCentroid);

public sealed record WorkerPointDeltaReportOptionsValue(
    int CoordinateSystem,
    int DetailsFormat,
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

public sealed record WorkerProjectionOptionsValue(
    int ProjectionType,
    bool IgnoreEdgeProjections,
    bool OverrideTargetOffsets,
    double OverrideTargetOffsetsValue,
    bool AddExtraMaterialThickness,
    double ExtraMaterialThicknessValue);

public sealed record WorkerReportOutputOptionsValue(
    int OutputType,
    string PathOrEmbeddedName);

public sealed record WorkerReportViewOptionsValue(
    int ViewType,
    string CollectionName,
    string CalloutName);

public sealed record WorkerToleranceScalarOptionsValue(
    WorkerScalarToleranceLimit High,
    WorkerScalarToleranceLimit Low);
