using System.Reflection;
using System.Text.Json;
using Briosa.Server.Services;

namespace Briosa.Server.Tests;

public sealed class CatalogCompletenessTests
{
    [Fact]
    public void EveryCatalogOperationIsGeneratedImplementedAndTested()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifests = Directory.GetFiles(
            Path.Combine(repositoryRoot.FullName, "generated", "catalog", "sa"),
            "coverage.json",
            SearchOption.AllDirectories);
        Assert.NotEmpty(manifests);

        var cataloged = Directory.GetFiles(
                Path.Combine(repositoryRoot.FullName, "catalog", "sa"),
                "catalog.json",
                SearchOption.AllDirectories)
            .SelectMany(ReadCatalogOperations)
            .ToHashSet(StringComparer.Ordinal);
        var generated = manifests
            .SelectMany(ReadOperations)
            .ToDictionary(operation => operation.OperationId, StringComparer.Ordinal);
        var implemented = MarkedOperations<OperationImplementationAttribute>(
            typeof(OperationImplementationAttribute).Assembly,
            marker => marker.OperationId);
        var tested = MarkedOperations<OperationTestAttribute>(
            typeof(CatalogCompletenessTests).Assembly,
            marker => marker.OperationId);

        Assert.Equal(cataloged.Order(), generated.Keys.Order());
        Assert.Equal(cataloged.Order(), implemented.Order());
        Assert.Equal(cataloged.Order(), tested.Order());
        foreach (var operation in generated.Values)
        {
            Assert.True(operation.Protocol);
            Assert.True(operation.CommandAdapter);
            Assert.True(operation.ResultAdapter);
            Assert.True(operation.Documentation);
            Assert.All(operation.Inputs, input =>
            {
                Assert.False(string.IsNullOrWhiteSpace(input.ArgumentId));
                Assert.False(string.IsNullOrWhiteSpace(input.Setter));
            });
            Assert.All(operation.Outputs, output =>
            {
                Assert.False(string.IsNullOrWhiteSpace(output.ArgumentId));
                Assert.False(string.IsNullOrWhiteSpace(output.Getter));
            });
        }
    }

    private static IEnumerable<string> ReadCatalogOperations(string manifestPath)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var targetRoot = Path.GetDirectoryName(manifestPath)!;
        foreach (var relativePath in manifest.RootElement.GetProperty("operation_files").EnumerateArray())
        {
            var operationPath = Path.Combine(
                targetRoot,
                relativePath.GetString()!.Replace('/', Path.DirectorySeparatorChar));
            using var operation = JsonDocument.Parse(File.ReadAllBytes(operationPath));
            yield return operation.RootElement.GetProperty("operation_id").GetString()!;
        }
    }

    private static IReadOnlyList<CoverageOperation> ReadOperations(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        return [.. document.RootElement.GetProperty("operations").EnumerateArray()
            .Select(operation => new CoverageOperation(
                operation.GetProperty("operation_id").GetString()!,
                operation.GetProperty("generated").GetProperty("protocol").GetBoolean(),
                operation.GetProperty("generated").GetProperty("command_adapter").GetBoolean(),
                operation.GetProperty("generated").GetProperty("result_adapter").GetBoolean(),
                operation.GetProperty("generated").GetProperty("documentation").GetBoolean(),
                [.. operation.GetProperty("inputs").EnumerateArray().Select(input =>
                    new CoverageArgument(
                        input.GetProperty("argument_id").GetString()!,
                        input.GetProperty("setter").GetString()))],
                [.. operation.GetProperty("outputs").EnumerateArray().Select(output =>
                    new CoverageArgument(
                        output.GetProperty("argument_id").GetString()!,
                        output.GetProperty("getter").GetString()))]))];
    }

    private static HashSet<string> MarkedOperations<TAttribute>(
        Assembly assembly,
        Func<TAttribute, string> operationId)
        where TAttribute : Attribute =>
        [.. assembly.GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(method => method.GetCustomAttributes<TAttribute>())
            .Select(operationId)];

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

    private sealed record CoverageOperation(
        string OperationId,
        bool Protocol,
        bool CommandAdapter,
        bool ResultAdapter,
        bool Documentation,
        IReadOnlyList<CoverageArgument> Inputs,
        IReadOnlyList<CoverageArgument> Outputs);

    private sealed record CoverageArgument(string ArgumentId, string? Binding)
    {
        public string? Setter => Binding;

        public string? Getter => Binding;
    }
}
