using System.Text.Json;

namespace Briosa.Generator;

internal static class SpecializedValueMappings
{
    private static readonly HashSet<string> EnumTypes =
    [
        "ascii_frame_set_format",
        "ascii_import_file_format",
        "axis_identifier",
        "wcf_axis_identifier",
        "base_color_type",
        "base_mid_color_type",
        "chart_type",
        "collimation_baseline_type",
        "collimation_type",
        "color_range_method",
        "coordinate_system_type",
        "vector_component",
        "dynamic_circle_mode",
        "dynamic_ellipse_mode",
        "dynamic_line_mode",
        "dynamic_plane_mode",
        "dynamic_point_mode",
        "edge_mode",
        "export_data_delimiter_type",
        "export_target_name_format",
        "export_vector_name_format",
        "geometry_type",
        "instrument_type",
        "object_type",
        "offset_direction_type",
        "point_filter_input_type",
        "relationship_weighting_mode",
        "render_mode_type",
        "report_page_orientation",
        "saturation_limit_type",
        "show_usmn_dialog_type",
        "surface_analysis_mode",
        "surface_dissection_mode_type",
        "target_computation_method",
        "translucency_type"
    ];

    private static readonly HashSet<string> StructuredTypes =
    [
        "auto_filter_proximity_settings",
        "cloud_thinning_options",
        "colorization_options",
        "fit_constraint_scalar_options",
        "fit_degree_of_freedom_options",

        "report_output_options",
        "report_view_options",
        "tolerance_scalar_options"
    ];

    public static bool IsEnum(string semanticType) => EnumTypes.Contains(semanticType);

    public static bool IsStructured(string semanticType) => StructuredTypes.Contains(semanticType);

    public static bool IsSupported(string semanticType) =>
        IsEnum(semanticType) || IsStructured(semanticType);

    public static string ToTypeName(string semanticType)
    {
        if (!IsSupported(semanticType))
        {
            throw Unsupported(semanticType);
        }

        return string.Concat(semanticType
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));
    }

    public static string CreateInputExpression(
        string semanticType,
        string prefix,
        string value)
    {
        if (IsEnum(semanticType))
        {
            return prefix + $"SpecializedEnumValue: new((int){value} - 1))";
        }

        return semanticType switch
        {
            "auto_filter_proximity_settings" => prefix +
                $"AutoFilterProximitySettingsValue: new({value}.SurfaceInclusionProximity, {value}.EdgeExclusionProximity, {value}.PlanarInclusionProximity, {value}.PlanarExclusionProximity, {value}.RadialInclusionProximity, {value}.GeometryExtractionTolerance, (int){value}.SurfaceProximityMode - 1, (int){value}.PlanarProximityMode - 1, (int){value}.RadialProximityMode - 1, {value}.ProjectToPlane, {value}.AssertPlaneBoundaries))",
            "cloud_thinning_options" => prefix +
                $"CloudThinningOptionsValue: new((int){value}.Mode - 1, {value}.PointIncrement, {value}.MinimumNumberOfPoints, {value}.MaximumNumberOfPoints))",
            "colorization_options" => prefix +
                $"ColorizationOptionsValue: new((int){value}.ColorRangeMethod - 1, (int){value}.BaseHighColor - 1, (int){value}.BaseMidColor - 1, (int){value}.BaseLowColor - 1, {value}.DrawTubes, {value}.DrawArrowheads, {value}.IndicateValues, {value}.VectorMagnification, {value}.VectorWidth, {value}.DrawBlotches, {value}.BlotchSize, {value}.ShowOutOfToleranceOnly, {value}.ShowColorBarInView, {value}.ShowColorBarPercentages, {value}.ShowColorBarFractions, {value}.HighSaturationLimit, {value}.LowSaturationLimit, {value}.HighTolerance, {value}.LowTolerance))",
            "fit_constraint_scalar_options" => prefix +
                $"FitConstraintScalarOptionsValue: new(new({value}.High.Enabled, {value}.High.Value), new({value}.Low.Enabled, {value}.Low.Value)))",
            "fit_degree_of_freedom_options" => prefix +
                $"FitDegreeOfFreedomOptionsValue: new({value}.AllowX, {value}.AllowY, {value}.AllowZ, {value}.AllowRx, {value}.AllowRy, {value}.AllowRz, {value}.RotateAboutCentroid))",
            "report_output_options" => prefix +
                $"ReportOutputOptionsValue: new((int){value}.OutputType - 1, {value}.DestinationCase == TargetProtocol.ReportOutputOptions.DestinationOneofCase.ExternalPath ? {value}.ExternalPath : null, {value}.DestinationCase == TargetProtocol.ReportOutputOptions.DestinationOneofCase.EmbeddedFile ? new({value}.EmbeddedFile.CollectionName, {value}.EmbeddedFile.FileName) : null))",
            "report_view_options" => prefix +
                $"ReportViewOptionsValue: new((int){value}.ViewType - 1, {value}.CollectionName, {value}.CalloutName))",
            "tolerance_scalar_options" => prefix +
                $"ToleranceScalarOptionsValue: new(new({value}.High.Enabled, {value}.High.Value), new({value}.Low.Enabled, {value}.Low.Value)))",
            _ => throw Unsupported(semanticType)
        };
    }

    public static string ValidationCondition(string semanticType, string value) =>
        semanticType switch
        {
            var type when IsEnum(type) =>
                $"{value} == 0 || !Enum.IsDefined({value})",
            "auto_filter_proximity_settings" => Missing(value,
                "SurfaceInclusionProximity", "EdgeExclusionProximity", "PlanarInclusionProximity",
                "PlanarExclusionProximity", "RadialInclusionProximity", "GeometryExtractionTolerance",
                "SurfaceProximityMode", "PlanarProximityMode", "RadialProximityMode",
                "ProjectToPlane", "AssertPlaneBoundaries") + " || " + InvalidEnums(value,
                    "SurfaceProximityMode", "PlanarProximityMode", "RadialProximityMode"),
            "cloud_thinning_options" => Missing(value,
                "Mode", "PointIncrement", "MinimumNumberOfPoints", "MaximumNumberOfPoints") +
                " || " + InvalidEnums(value, "Mode"),
            "colorization_options" => Missing(value,
                "ColorRangeMethod", "BaseHighColor", "BaseMidColor", "BaseLowColor",
                "DrawTubes", "DrawArrowheads", "IndicateValues", "VectorMagnification", "VectorWidth",
                "DrawBlotches", "BlotchSize", "ShowOutOfToleranceOnly", "ShowColorBarInView",
                "ShowColorBarPercentages", "ShowColorBarFractions", "HighSaturationLimit",
                "LowSaturationLimit", "HighTolerance", "LowTolerance") + " || " + InvalidEnums(value,
                    "ColorRangeMethod", "BaseHighColor", "BaseMidColor", "BaseLowColor"),
            "fit_constraint_scalar_options" => MissingScalarLimits(value),
            "fit_degree_of_freedom_options" => Missing(value,
                "AllowX", "AllowY", "AllowZ", "AllowRx", "AllowRy", "AllowRz", "RotateAboutCentroid"),
            "report_output_options" => Missing(value, "OutputType") +
                " || " + InvalidEnums(value, "OutputType") +
                $" || {value}.DestinationCase == TargetProtocol.ReportOutputOptions.DestinationOneofCase.None" +
                $" || ({value}.DestinationCase == TargetProtocol.ReportOutputOptions.DestinationOneofCase.EmbeddedFile" +
                $" && ({value}.EmbeddedFile is null || !{value}.EmbeddedFile.HasCollectionName" +
                $" || !{value}.EmbeddedFile.HasFileName))",
            "report_view_options" => Missing(value, "ViewType", "CollectionName", "CalloutName") +
                " || " + InvalidEnums(value, "ViewType"),
            "tolerance_scalar_options" => MissingScalarLimits(value),
            _ => throw Unsupported(semanticType)
        };

    public static string ResultValueExpression(string semanticType, string variable) =>
        semanticType switch
        {
            "fit_constraint_scalar_options" => ScalarOptionsResult(
                "FitConstraintScalarOptions", "FitConstraintScalarOptionsValue", variable),
            "tolerance_scalar_options" => ScalarOptionsResult(
                "ToleranceScalarOptions", "ToleranceScalarOptionsValue", variable),
            _ => throw Unsupported(semanticType)
        };

    public static string DefaultExpression(string semanticType, JsonElement value)
    {
        if (!IsEnum(semanticType))
        {
            throw Unsupported(semanticType);
        }

        var number = value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : throw new NotSupportedException(
                $"Reviewed enum defaults for '{semanticType}' must use exact protobuf numeric values.");
        return $"(TargetProtocol.{ToTypeName(semanticType)}){number}";
    }

    private static string Missing(string value, params string[] properties) =>
        string.Join(" || ", properties.Select(property => $"!{value}.Has{property}"));

    private static string InvalidEnums(string value, params string[] properties) =>
        string.Join(" || ", properties.Select(property =>
            $"{value}.{property} == 0 || !Enum.IsDefined({value}.{property})"));

    private static string MissingScalarLimits(string value) =>
        $"{value}.High is null || !{value}.High.HasEnabled || !{value}.High.HasValue || " +
        $"{value}.Low is null || !{value}.Low.HasEnabled || !{value}.Low.HasValue";

    private static string ScalarOptionsResult(
        string protocolType,
        string workerProperty,
        string variable) =>
        $"new TargetProtocol.{protocolType} {{ High = new TargetProtocol.ScalarToleranceLimit {{ Enabled = {variable}.{workerProperty}!.High.Enabled, Value = {variable}.{workerProperty}.High.Value }}, Low = new TargetProtocol.ScalarToleranceLimit {{ Enabled = {variable}.{workerProperty}.Low.Enabled, Value = {variable}.{workerProperty}.Low.Value }} }}";

    private static NotSupportedException Unsupported(string semanticType) =>
        new($"Semantic type '{semanticType}' is not an approved specialized value family.");
}
