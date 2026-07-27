using Google.Protobuf;
using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Protocol.Tests;

public sealed class SpecializedValueProtocolTests
{
    [Fact]
    public void SpecializedEnumsReserveZeroForUnspecified()
    {
        Assert.Equal(0, (int)TargetProtocol.RenderModeType.Unspecified);
        Assert.Equal(3, (int)TargetProtocol.RenderModeType.SolidAndEdges);
        Assert.Equal(5, (int)TargetProtocol.InstrumentType.CreaformVxElements);
        Assert.Equal(25, (int)TargetProtocol.ObjectType.VectorGroup);
    }

    [Fact]
    public void SpecializedStructuresPreserveExplicitZeroAndFalsePresence()
    {
        var value = new TargetProtocol.AutoFilterProximitySettings
        {
            SurfaceInclusionProximity = 0,
            EdgeExclusionProximity = 0,
            PlanarInclusionProximity = 0,
            PlanarExclusionProximity = 0,
            RadialInclusionProximity = 0,
            GeometryExtractionTolerance = 0,
            SurfaceProximityMode = TargetProtocol.OffsetDirectionType.Both,
            PlanarProximityMode = TargetProtocol.OffsetDirectionType.PositiveOnly,
            RadialProximityMode = TargetProtocol.OffsetDirectionType.NegativeOnly,
            ProjectToPlane = false,
            AssertPlaneBoundaries = false
        };

        var roundTrip = TargetProtocol.AutoFilterProximitySettings.Parser.ParseFrom(
            value.ToByteArray());

        Assert.True(roundTrip.HasSurfaceInclusionProximity);
        Assert.True(roundTrip.HasProjectToPlane);
        Assert.False(roundTrip.ProjectToPlane);
        Assert.Equal(TargetProtocol.OffsetDirectionType.NegativeOnly, roundTrip.RadialProximityMode);
    }

    [Fact]
    public void UnresolvedBSplineShapeIsNotPublished()
    {
        var root = FindRepositoryRoot();
        var schema = File.ReadAllText(Path.Combine(
            root,
            "proto",
            "briosa",
            "sa",
            "v2026_1_0529_7",
            "v1alpha1",
            "specialized_values.proto"));

        Assert.DoesNotContain("BSplineFitOptions", schema, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
            throw new DirectoryNotFoundException("Could not locate the Briosa repository root.");
    }
}
