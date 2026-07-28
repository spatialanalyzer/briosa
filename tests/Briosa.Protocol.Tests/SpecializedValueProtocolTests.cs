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
        Assert.Equal(108, (int)TargetProtocol.InstrumentType.FaroVantage);
        Assert.Equal(191, Enum.GetValues<TargetProtocol.InstrumentType>().Length);
        Assert.Equal(8, (int)TargetProtocol.ObjectType.Cone);
        Assert.Equal(26, (int)TargetProtocol.ObjectType.VectorGroup);
        Assert.Equal(27, Enum.GetValues<TargetProtocol.ObjectType>().Length);
        Assert.Equal(3, (int)TargetProtocol.ItemType.Annotation);
        Assert.Equal(25, (int)TargetProtocol.ItemType.Picture);
        Assert.Equal(42, (int)TargetProtocol.ItemType.VectorGroup);
        Assert.Equal(43, Enum.GetValues<TargetProtocol.ItemType>().Length);
        Assert.Equal(45, Enum.GetValues<TargetProtocol.AsciiImportFileFormat>().Length);
        Assert.Equal(9, Enum.GetValues<TargetProtocol.AsciiFrameSetFormat>().Length);
        Assert.Equal(7, Enum.GetValues<TargetProtocol.AxisIdentifier>().Length);
        Assert.Equal(4, Enum.GetValues<TargetProtocol.WcfAxisIdentifier>().Length);
        Assert.Equal(5, Enum.GetValues<TargetProtocol.VectorComponent>().Length);
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
    public void ReportOutputDestinationPreservesExternalAndEmbeddedStructure()
    {
        var external = new TargetProtocol.ReportOutputOptions
        {
            OutputType = TargetProtocol.ReportOutputType.Pdf,
            ExternalPath = "report.pdf"
        };
        var embedded = new TargetProtocol.ReportOutputOptions
        {
            OutputType = TargetProtocol.ReportOutputType.SaReport,
            EmbeddedFile = new TargetProtocol.EmbeddedReportFile
            {
                CollectionName = "Collection",
                FileName = "Report"
            }
        };

        var externalRoundTrip = TargetProtocol.ReportOutputOptions.Parser.ParseFrom(
            external.ToByteArray());
        var embeddedRoundTrip = TargetProtocol.ReportOutputOptions.Parser.ParseFrom(
            embedded.ToByteArray());

        Assert.Equal(
            TargetProtocol.ReportOutputOptions.DestinationOneofCase.ExternalPath,
            externalRoundTrip.DestinationCase);
        Assert.Equal("report.pdf", externalRoundTrip.ExternalPath);
        Assert.Equal(
            TargetProtocol.ReportOutputOptions.DestinationOneofCase.EmbeddedFile,
            embeddedRoundTrip.DestinationCase);
        Assert.True(embeddedRoundTrip.EmbeddedFile.HasCollectionName);
        Assert.Equal("Report", embeddedRoundTrip.EmbeddedFile.FileName);
    }
    [Fact]
    public void UnresolvedSpecializedShapesAreNotPublished()
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
        Assert.DoesNotContain("ProjectionType", schema, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectionOptions", schema, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportDetailsFormat", schema, StringComparison.Ordinal);
        Assert.DoesNotContain("PointDeltaReportOptions", schema, StringComparison.Ordinal);
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
