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
                method.Contains("Arg", StringComparison.Ordinal))
            .OrderBy(method => method, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(workerSeam, registered);
        Assert.Equal(54, registered.Length);

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
                "angular_unit",
                "chart_name",
                "cloud_name",
                "collection_group_name_list",
                "collection_instrument_id",
                "collection_instrument_id_list",
                "collection_machine_id",
                "collection_name",
                "collection_object_name",
                "collection_object_name_list",
                "collection_vector_group_name",
                "collection_vector_group_name_list",
                "distance_unit",
                "double_array",
                "edit_text",
                "file_reference",
                "floating_point",
                "font",
                "frame_name",
                "logical",
                "point_name",
                "point_name_list",
                "rgb_color",
                "string",
                "string_list",
                "temperature_unit",
                "tolerance_vector_options",
                "transform",
                "vector3",
                "vector_group_name",
                "vector_name_list",
                "view_name",
                "whole_number",
                "world_transform"
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
