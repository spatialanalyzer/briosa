namespace Briosa.Worker.Sdk;

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
