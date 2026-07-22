using Briosa.Generator;

namespace Briosa.Generator.Tests;

public sealed class CommandCatalogGeneratorTests
{
    [Fact]
    public void CommittedVerticalSliceMatchesDeterministicGeneration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            $"briosa-generator-tests-{Guid.NewGuid():N}");
        try
        {
            var result = CommandCatalogGenerator.Generate(
                Path.Combine(repositoryRoot.FullName, "catalog"),
                outputRoot);

            Assert.Equal(
                [
                    "proto/briosa/sa/v2026_1_0529_7/v1alpha1/operations.proto",
                    "src/Briosa.Server/Generated/Sa/V2026_1_0529_7/V1Alpha1/Operations.g.cs",
                    "docs/reference/generated/sa/2026.1.0529.7/operations.md",
                    "generated/catalog/sa/2026.1.0529.7/coverage.json"
                ],
                result.Files);

            foreach (var relativePath in result.Files)
            {
                Assert.Equal(
                    File.ReadAllBytes(Path.Combine(repositoryRoot.FullName, relativePath)),
                    File.ReadAllBytes(Path.Combine(outputRoot, relativePath)));
            }
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void VerticalSlicePreservesExactStepAndResultGetterNames()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            $"briosa-generator-tests-{Guid.NewGuid():N}");
        try
        {
            _ = CommandCatalogGenerator.Generate(
                Path.Combine(repositoryRoot.FullName, "catalog"),
                outputRoot);

            var binding = File.ReadAllText(Path.Combine(
                outputRoot,
                "src",
                "Briosa.Server",
                "Generated",
                "Sa",
                "V2026_1_0529_7",
                "V1Alpha1",
                "Operations.g.cs"));
            Assert.Contains("StepName = \"Get Working Directory\"", binding, StringComparison.Ordinal);
            Assert.Contains("DirectoryArgumentName = \"Directory\"", binding, StringComparison.Ordinal);
            Assert.Contains("DirectoryFieldName = \"directory\"", binding, StringComparison.Ordinal);
            Assert.Contains("WorkerMpValueKind.Text", binding, StringComparison.Ordinal);
            Assert.Contains("DirectoryGetter = \"GetStringArg\"", binding, StringComparison.Ordinal);
            Assert.Contains("TargetCatalogMetadata", binding, StringComparison.Ordinal);
            Assert.Contains("CatalogId = \"briosa.sa.2026.1.0529.7\"", binding, StringComparison.Ordinal);
            Assert.Contains("/briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory", binding, StringComparison.Ordinal);
            Assert.Contains("OutputContracts", binding, StringComparison.Ordinal);
            Assert.Contains("CreateResult(SuccessfulOperationExecution completed)", binding, StringComparison.Ordinal);

            var proto = File.ReadAllText(Path.Combine(
                outputRoot,
                "proto",
                "briosa",
                "sa",
                "v2026_1_0529_7",
                "v1alpha1",
                "operations.proto"));
            Assert.Contains(
                "rpc GetWorkingDirectory(GetWorkingDirectoryRequest) returns (GetWorkingDirectoryResult)",
                proto,
                StringComparison.Ordinal);
            Assert.Contains("optional string directory = 1", proto, StringComparison.Ordinal);
            Assert.Contains(
                "briosa.core.v1alpha1.MpExecutionDetails execution = 1000",
                proto,
                StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

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
}
