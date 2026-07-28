using System.Text.RegularExpressions;
using Briosa.Core.V1Alpha1;
using Google.Protobuf;
using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void GeneratedDescriptorsRetainIndependentPackageIdentities()
    {
        Assert.Equal(
            "briosa.core.v1alpha1",
            VersionCoordinates.Descriptor.File.Package);
        Assert.Equal(
            "briosa.sa.v2026_1_0529_7.v1alpha1",
            TargetProtocol.PointName.Descriptor.File.Package);
        Assert.NotEqual(
            typeof(VersionCoordinates).Namespace,
            typeof(TargetProtocol.PointName).Namespace);
    }

    [Fact]
    public void ExplicitPresenceDistinguishesDisabledAndZeroFromMissing()
    {
        var limit = new TargetProtocol.ToleranceLimit
        {
            Enabled = false,
            Value = 0
        };

        Assert.True(limit.HasEnabled);
        Assert.True(limit.HasValue);

        limit.ClearEnabled();
        limit.ClearValue();

        Assert.False(limit.HasEnabled);
        Assert.False(limit.HasValue);
    }

    [Fact]
    public void IdentityAndReferenceMessagesPreservePresenceOrderAndEmptyLists()
    {
        var objects = new TargetProtocol.CollectionObjectNameList();
        objects.Values.Add(new TargetProtocol.CollectionObjectName
        {
            CollectionName = "Collection",
            ObjectName = "Object",
            ExactObjectType = TargetProtocol.ObjectType.PointGroup
        });
        var points = new TargetProtocol.PointNameList();
        points.Values.Add(new TargetProtocol.PointName
        {
            CollectionName = "Collection",
            GroupName = "Group",
            TargetName = "Point A"
        });
        points.Values.Add(new TargetProtocol.PointName
        {
            CollectionName = "Collection",
            GroupName = "Group",
            TargetName = ""
        });

        var objectRoundTrip = TargetProtocol.CollectionObjectNameList.Parser.ParseFrom(
            objects.ToByteArray());
        var pointRoundTrip = TargetProtocol.PointNameList.Parser.ParseFrom(
            points.ToByteArray());
        var vectors = new TargetProtocol.VectorNameList();
        vectors.Values.Add(new TargetProtocol.VectorName
        {
            CollectionName = "Collection",
            GroupName = "Vectors",
            Name = "Vector A"
        });
        var vectorRoundTrip = TargetProtocol.VectorNameList.Parser.ParseFrom(
            vectors.ToByteArray());
        var emptyRoundTrip = TargetProtocol.StringList.Parser.ParseFrom(
            new TargetProtocol.StringList().ToByteArray());

        Assert.Equal(TargetProtocol.ObjectType.PointGroup, Assert.Single(objectRoundTrip.Values).ExactObjectType);
        Assert.Equal(["Point A", ""], pointRoundTrip.Values.Select(value => value.TargetName));
        Assert.Equal("Vector A", Assert.Single(vectorRoundTrip.Values).Name);
        Assert.Empty(emptyRoundTrip.Values);
    }

    [Fact]
    public void ContainerValuesRoundTripWithoutComShapes()
    {
        var world = new TargetProtocol.WorldTransform
        {
            Transform = new TargetProtocol.Transform
            {
                Values = { Enumerable.Range(0, 16).Select(value => (double)value) }
            },
            ScaleFactor = 0
        };
        var file = new TargetProtocol.FileReference
        {
            Path = "",
            EmbeddedFile = false
        };
        var font = new TargetProtocol.Font
        {
            FontName = "Segoe UI",
            Size = 255,
            Color = new TargetProtocol.RgbColor
            {
                Red = 0,
                Green = 127,
                Blue = 255
            }
        };

        var worldRoundTrip = TargetProtocol.WorldTransform.Parser.ParseFrom(
            world.ToByteArray());
        var fileRoundTrip = TargetProtocol.FileReference.Parser.ParseFrom(
            file.ToByteArray());
        var fontRoundTrip = TargetProtocol.Font.Parser.ParseFrom(
            font.ToByteArray());
        var emptyArrayRoundTrip = TargetProtocol.DoubleArray.Parser.ParseFrom(
            new TargetProtocol.DoubleArray().ToByteArray());

        Assert.Equal(16, worldRoundTrip.Transform.Values.Count);
        Assert.Equal(15d, worldRoundTrip.Transform.Values[15]);
        Assert.True(worldRoundTrip.HasScaleFactor);
        Assert.Equal(0d, worldRoundTrip.ScaleFactor);
        Assert.True(fileRoundTrip.HasPath);
        Assert.Equal("", fileRoundTrip.Path);
        Assert.True(fileRoundTrip.HasEmbeddedFile);
        Assert.False(fileRoundTrip.EmbeddedFile);
        Assert.True(fontRoundTrip.HasSize);
        Assert.Equal(255u, fontRoundTrip.Size);
        Assert.Equal(255u, fontRoundTrip.Color.Blue);
        Assert.Empty(emptyArrayRoundTrip.Values);
        Assert.Equal(2, (int)TargetProtocol.AngularUnit.DegreesMinutesSeconds);
        Assert.Equal(6, (int)TargetProtocol.DistanceUnit.UsSurveyFeet);
        Assert.Equal(2, (int)TargetProtocol.TemperatureUnit.Celsius);
    }

    [Fact]
    public void CollectionObjectPreservesAllRequiredComponents()
    {
        var value = new TargetProtocol.CollectionObjectName
        {
            CollectionName = "Collection",
            ObjectName = "Object",
            ExactObjectType = TargetProtocol.ObjectType.PointGroup
        };

        var roundTrip = TargetProtocol.CollectionObjectName.Parser.ParseFrom(value.ToByteArray());

        Assert.True(roundTrip.HasCollectionName);
        Assert.True(roundTrip.HasObjectName);
        Assert.True(roundTrip.HasExactObjectType);
        Assert.Equal(TargetProtocol.ObjectType.PointGroup, roundTrip.ExactObjectType);
        Assert.NotEqual(
            TargetProtocol.CollectionInstrumentId.Descriptor.FullName,
            TargetProtocol.CollectionMachineId.Descriptor.FullName);
    }
    [Fact]
    public void SchemaPackagesMatchTheirDirectories()
    {
        var protoRoot = FindRepositoryRoot().GetDirectories("proto").Single();

        foreach (var file in EnumerateSchemas(protoRoot))
        {
            var relativePath = NormalizePath(Path.GetRelativePath(protoRoot.FullName, file));
            var separator = relativePath.LastIndexOf('/');
            Assert.True(separator > 0, $"Schema path '{relativePath}' has no package directory.");

            var expectedPackage = relativePath[..separator].Replace('/', '.');
            var source = File.ReadAllText(file);
            var match = PackageDeclaration().Match(source);

            Assert.True(match.Success, $"Schema '{relativePath}' has no package declaration.");
            Assert.Equal(expectedPackage, match.Groups["package"].Value);
        }
    }

    [Fact]
    public void SchemaImportsNeverCrossExactSaTargets()
    {
        var protoRoot = FindRepositoryRoot().GetDirectories("proto").Single();

        foreach (var file in EnumerateSchemas(protoRoot))
        {
            var relativePath = NormalizePath(Path.GetRelativePath(protoRoot.FullName, file));
            var sourceSegments = relativePath.Split('/');
            var sourceIsCore = sourceSegments.Length > 1 &&
                sourceSegments[0] == "briosa" &&
                sourceSegments[1] == "core";
            var sourceTarget = sourceSegments.Length > 2 &&
                sourceSegments[0] == "briosa" &&
                sourceSegments[1] == "sa"
                    ? sourceSegments[2]
                    : null;

            foreach (Match import in ImportDeclaration().Matches(File.ReadAllText(file)))
            {
                var importPath = import.Groups["path"].Value;
                var importSegments = importPath.Split('/');
                var importsTarget = importSegments.Length > 2 &&
                    importSegments[0] == "briosa" &&
                    importSegments[1] == "sa";

                if (!importsTarget)
                {
                    continue;
                }

                Assert.False(
                    sourceIsCore,
                    $"Core schema '{relativePath}' imports target schema '{importPath}'.");
                Assert.NotNull(sourceTarget);
                Assert.Equal(sourceTarget, importSegments[2]);
            }
        }
    }

    private static IEnumerable<string> EnumerateSchemas(DirectoryInfo protoRoot) =>
        Directory
            .EnumerateFiles(protoRoot.FullName, "*.proto", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal);

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ??
            throw new DirectoryNotFoundException("Could not locate the Briosa repository root.");
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    [GeneratedRegex(@"(?m)^\s*package\s+(?<package>[A-Za-z0-9_.]+)\s*;")]
    private static partial Regex PackageDeclaration();

    [GeneratedRegex(@"(?m)^\s*import(?:\s+(?:public|weak))?\s+""(?<path>[^""]+)""\s*;")]
    private static partial Regex ImportDeclaration();
}
