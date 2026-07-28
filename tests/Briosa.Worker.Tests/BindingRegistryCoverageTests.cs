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
        Assert.Equal(97, registered.Length);

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
                "ascii_frame_set_format",
                "ascii_import_file_format",
                "auto_filter_proximity_settings",
                "axis_identifier",
                "base_color_type",
                "base_mid_color_type",
                "chart_name",
                "chart_type",
                "cloud_name",
                "cloud_thinning_options",
                "collection_group_name_list",
                "collection_instrument_id",
                "collection_instrument_id_list",
                "collection_item_name",
                "collection_item_name_list",
                "collection_machine_id",
                "collection_name",
                "collection_object_name",
                "collection_object_name_list",
                "collection_vector_group_name",
                "collection_vector_group_name_list",
                "collimation_baseline_type",
                "collimation_type",
                "color_range_method",
                "colorization_options",
                "coordinate_system_type",
                "distance_unit",
                "double_array",
                "dynamic_circle_mode",
                "dynamic_ellipse_mode",
                "dynamic_line_mode",
                "dynamic_plane_mode",
                "dynamic_point_mode",
                "edge_mode",
                "edit_text",
                "export_data_delimiter_type",
                "export_target_name_format",
                "export_vector_name_format",
                "file_reference",
                "fit_constraint_scalar_options",
                "fit_degree_of_freedom_options",
                "floating_point",
                "font",
                "frame_name",
                "geometry_type",
                "instrument_type",
                "logical",
                "object_type",
                "offset_direction_type",
                "point_filter_input_type",
                "point_name",
                "point_name_list",
                "relationship_weighting_mode",
                "render_mode_type",
                "report_output_options",
                "report_page_orientation",
                "report_view_options",
                "rgb_color",
                "saturation_limit_type",
                "show_usmn_dialog_type",
                "string",
                "string_list",
                "surface_analysis_mode",
                "surface_dissection_mode_type",
                "target_computation_method",
                "temperature_unit",
                "tolerance_scalar_options",
                "tolerance_vector_options",
                "transform",
                "translucency_type",
                "vector3",
                "vector_component",
                "vector_group_name",
                "vector_name_list",
                "view_name",
                "wcf_axis_identifier",
                "whole_number",
                "world_transform"
            ],
            implementedFamilies);
        Assert.Equal(implementedFamilies.Length, Enum.GetValues<SdkValueKind>().Length);
        Assert.Contains(SdkValueKind.CollectionItemName, Enum.GetValues<SdkValueKind>());
        Assert.Contains(SdkValueKind.CollectionItemNameList, Enum.GetValues<SdkValueKind>());
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
