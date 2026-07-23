using System.Reflection;
using System.Text.Json;
using Briosa.Worker.Sdk;

namespace Briosa.Worker.Tests;

public sealed class BindingRegistryCoverageTests
{
    [Fact]
    public void RegistryImplementedBindingsMatchWorkerSdkCallSeam()
    {
        var root = FindRepositoryRoot().FullName;
        using var review = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "bindings",
            "sa",
            "2026.1.0529.7",
            "review.json")));
        var registered = review.RootElement
            .GetProperty("implemented_coverage")
            .GetProperty("adapter")
            .EnumerateArray()
            .Select(element => element.GetString()!)
            .OrderBy(method => method, StringComparer.Ordinal)
            .ToArray();
        var workerSeam = typeof(ISpatialAnalyzerSdkCalls)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.Name)
            .Where(method =>
                (method.StartsWith("Get", StringComparison.Ordinal) ||
                 method.StartsWith("Set", StringComparison.Ordinal)) &&
                method.EndsWith("Arg", StringComparison.Ordinal))
            .OrderBy(method => method, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(workerSeam, registered);
        Assert.Equal(14, registered.Length);

        using var registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "bindings",
            "sa",
            "2026.1.0529.7",
            "registry.json")));
        var implementedFamilies = registry.RootElement
            .GetProperty("value_families")
            .EnumerateArray()
            .Where(element =>
                element.GetProperty("implementation_status").GetString() == "implemented")
            .Select(element => element.GetProperty("family_id").GetString()!)
            .OrderBy(family => family, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "floating_point",
                "logical",
                "point_name",
                "string",
                "tolerance_vector_options",
                "vector3",
                "whole_number"
            ],
            implementedFamilies);
        Assert.Equal(implementedFamilies.Length, Enum.GetValues<SdkValueKind>().Length);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Briosa.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException(
            "Could not find the repository root.");
    }
}
