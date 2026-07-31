using System.Text.RegularExpressions;
using global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void ProtobufDescriptorsUseOneStablePackageAndNamespace()
    {
        Assert.Equal("briosa", VersionCoordinates.Descriptor.File.Package);
        Assert.Equal("briosa", GetWorkingDirectoryRequest.Descriptor.File.Package);
        Assert.Equal(typeof(VersionCoordinates).Namespace, typeof(GetWorkingDirectoryRequest).Namespace);
        Assert.Equal("Briosa", typeof(GetWorkingDirectoryRequest).Namespace);
    }

    [Fact]
    public void MethodRetainsItsReviewedCategoryFileAndFieldIdentity()
    {
        var file = GetWorkingDirectoryRequest.Descriptor.File;
        var service = Assert.Single(file.Services);
        var method = Assert.Single(service.Methods);
        var directory = GetWorkingDirectoryResult.Descriptor.FindFieldByName("directory");
        var execution = GetWorkingDirectoryResult.Descriptor.FindFieldByName("execution");

        Assert.Equal(
            "briosa/file_operations.proto",
            file.Name);
        Assert.Equal("briosa.FileOperations", service.FullName);
        Assert.Equal("GetWorkingDirectory", method.Name);
        Assert.Equal(1, directory.FieldNumber);
        Assert.Equal(1000, execution.FieldNumber);
    }

    [Fact]
    public void FileLevelBreakingPolicyRemainsExplicit()
    {
        var repositoryRoot = FindRepositoryRoot();
        var bufConfiguration = File.ReadAllText(Path.Combine(repositoryRoot.FullName, "buf.yaml"));
        Assert.Matches(@"(?ms)^breaking:\s*\r?\n\s+use:\s*\r?\n\s+- FILE\s*$", bufConfiguration);
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
    public void SchemaImportsRemainInsideTheBriosaPackage()
    {
        var protoRoot = FindRepositoryRoot().GetDirectories("proto").Single();

        foreach (var file in EnumerateSchemas(protoRoot))
        {
            foreach (Match import in ImportDeclaration().Matches(File.ReadAllText(file)))
            {
                var importPath = import.Groups["path"].Value;
                Assert.StartsWith("briosa/", importPath, StringComparison.Ordinal);
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
