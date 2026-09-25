namespace Briosa.Worker.Control;

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
