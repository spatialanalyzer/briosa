using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveB;

internal static class CloudAndMeshOperationCatalog
{
    private const string Service = "briosa.CloudAndMeshOperations";

    private static readonly IReadOnlyList<string> CloudBoxTypes =
    [
        "World Axis Aligned Box",
        "Work Axis Aligned Box",
        "Minimum Oriented Box - Unconditional",
        "Minimum Oriented Box - Verify Volume"
    ];

    private static readonly IReadOnlyList<string> PointOutputTypes =
        ["Points", "Cloud Points"];

    private static readonly IReadOnlyList<string> RgbFilterOperations =
        ["Incrementally Apply Filter", "Reset and Apply Filter", "Reset All Cloud Points Visible"];

    private static readonly IReadOnlyList<string> RgbColorChannels =
        ["Red", "Green", "Blue", "Intensity"];

    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        Mutating(
            "cloud_and_mesh_operations.cloud_display_control",
            "Cloud Display Control",
            "CloudDisplayControl",
            [new("thin", "Thin (Draw Increment)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1", false),
                new("point_size", "Point Size", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1", false)]),
        Mutating(
            "cloud_and_mesh_operations.reset_cloud_bounding_box",
            "Reset Cloud Bounding Box",
            "ResetCloudBoundingBox",
            [new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("cloud_box_type", "Cloud Box Type", WorkerMpValueKind.Text, "SetStringArg", "World Axis Aligned Box", false, EnumTextValues: CloudBoxTypes),
                new("show_bounding_box", "Show Bounding Box?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("use_all_points", "Use All Points?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("desired_point_count", "Desired Point Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1000", false)],
            [new("x_axis_dimension", "X-Axis Dimension", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y_axis_dimension", "Y-Axis Dimension", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z_axis_dimension", "Z-Axis Dimension", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("x_axis_in_world", "X-Axis (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("y_axis_in_world", "Y-Axis (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("z_axis_in_world", "Z-Axis (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("centroid_in_world", "Centroid (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("reference_transform_in_world", "Reference Transform (in WORLD)", WorkerMpValueKind.Transform, "GetTransformArg", "—", false),
                new("reference_transform_in_working", "Reference Transform (in WORKING)", WorkerMpValueKind.Transform, "GetTransformArg", "—", false),
                new("points_used_for_bounding_box", "Points Used for Bounding Box", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "cloud_and_mesh_operations.get_cloud_point_count",
            "Get Cloud Point Count",
            "GetCloudPointCount",
            [new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud)],
            [new("points_count", "Points Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("planar_offset", "Planar Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("radial_offset", "Radial Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("active_clipping_planes", "Active Clipping Planes", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        Mutating(
            "cloud_and_mesh_operations.set_cloud_default_clipping_plane",
            "Set Cloud Default Clipping Plane",
            "SetCloudDefaultClippingPlane",
            [new("enable_cloud_clipping", "Enable Cloud Clipping?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("reference_object", "Reference Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Any, OmitWhenAbsent: true)],
            validateRequest: ValidateCloudDefaultClippingPlane),
        Mutating(
            "cloud_and_mesh_operations.raster_scan_edge_inspection",
            "Raster Scan Edge Inspection",
            "RasterScanEdgeInspection",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("edge_surface_name", "Edge Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("b_spline_edge_list", "BSpline Edge List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.BSpline),
                new("prefix_for_output_groups", "Prefix for Output Groups", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("tolerance", "Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minimum_good_points_per_unit_length", "Minimum Number of \"Good\" Points per Unit Length", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("maximum_bad_points_percentage", "Maximum Percentage of \"Bad\" Points (0-100)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [new("summary_result", "Summary Result", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        Mutating(
            "cloud_and_mesh_operations.new_raster_scan_edge_inspection",
            "New Raster Scan Edge Inspection",
            "NewRasterScanEdgeInspection",
            [new("edge_cloud_names", "Edge Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("edge_surface_name", "Edge Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("edge_b_spline_name", "Edge BSpline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("output_prefix", "Output Prefix", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("inspection_increment", "Inspection Increment", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("proximity_filter_distance", "Proximity Filter Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("edge_bias_value", "Edge Bias Value", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("error_tolerance", "Error Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("use_cosine_projection_method", "Use Cosine Projection Method", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("minimum_edge_points_per_segment", "Minimum Number of Edge Points per segment", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("intermediate_calculation_results_file", "Intermediate Calculation Results File(optional)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, OmitWhenAbsent: true)],
            [new("summary_result", "Summary Result", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        Mutating(
            "cloud_and_mesh_operations.clear_cloud_point_deviations",
            "Clear Cloud Point Deviations",
            "ClearCloudPointDeviations",
            [new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud)]),
        Mutating(
            "cloud_and_mesh_operations.enable_all_cloud_cross_sections",
            "Enable All Cloud Cross Sections",
            "EnableAllCloudCrossSections",
            [new("cross_section_cloud_name", "Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.CrossSectionCloud)]),
        Mutating(
            "cloud_and_mesh_operations.enable_disable_cloud_cross_sections",
            "Enable/Disable Cloud Cross Sections",
            "EnableDisableCloudCrossSections",
            [new("cross_section_cloud_name", "Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.CrossSectionCloud),
                new("cross_section_id", "Cross Section ID", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("enable", "Enable (TRUE) / Disable (FALSE)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "cloud_and_mesh_operations.enable_single_cloud_cross_section",
            "Enable Single Cloud Cross Section",
            "EnableSingleCloudCrossSection",
            [new("cross_section_cloud_name", "Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.CrossSectionCloud),
                new("cross_section_id", "Cross Section ID", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        ReadOnly(
            "cloud_and_mesh_operations.get_number_of_cross_sections_in_cross_section_cloud",
            "Get Number of Cross Sections in Cross Section Cloud",
            "GetNumberOfCrossSectionsInCrossSectionCloud",
            [new("cross_section_cloud_name", "Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.CrossSectionCloud)],
            [new("cross_section_count", "Cross Section Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        Mutating(
            "cloud_and_mesh_operations.filter_clouds_to_plane",
            "Filter Clouds to Plane",
            "FilterCloudsToPlane",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("filter_plane_name", "Filter Plane's Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("output_group_name", "Output Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("proximity", "Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("allowable_offset_direction", "Allowable Offset Dir", WorkerMpValueKind.OffsetDirectionType, "SetOffsetDirectionTypeArg", "Both", false),
                EnumText("output_type", "Output Type", "Points", PointOutputTypes)]),
        Mutating(
            "cloud_and_mesh_operations.filter_clouds_to_group",
            "Filter Clouds to Group",
            "FilterCloudsToGroup",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("filter_group_name", "Filter Group's Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("output_group_name", "Output Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("proximity", "Proximity (0 for Closest Point only)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_number_of_points", "Maximum Number of Points (0 for Unlimited)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                EnumText("output_type", "Output Type", "Points", PointOutputTypes)]),
        Mutating(
            "cloud_and_mesh_operations.filter_clouds_to_surface",
            "Filter Clouds to Surface",
            "FilterCloudsToSurface",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("filter_surface_name", "Filter Surface's Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("output_group_name", "Output Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("low_proximity", "Low Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("high_proximity", "High Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("skip_factor", "Skip Factor", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                EnumText("output_type", "Output Type", "Points", PointOutputTypes)]),
        Mutating(
            "cloud_and_mesh_operations.filter_clouds_to_bsplines",
            "Filter Clouds to BSplines",
            "FilterCloudsToBSplines",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("filter_b_spline_names", "Filter BSpline Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.BSpline),
                new("output_group_name", "Output Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("minimum_proximity", "Minimum Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_proximity", "Maximum Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                EnumText("output_type", "Output Type", "Points", PointOutputTypes)]),
        Mutating(
            "cloud_and_mesh_operations.filter_clouds_to_line_segment",
            "Filter Clouds to Line Segment",
            "FilterCloudsToLineSegment",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("first_line_end_point", "First Line End Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("second_line_end_point", "Second Line End Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("output_group_name", "Output Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("minimum_proximity", "Minimum Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_proximity", "Maximum Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                EnumText("output_type", "Output Type", "Points", PointOutputTypes)]),
        Mutating(
            "cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_points",
            "Filter Clouds to Vector Groups - Resolve points",
            "FilterCloudsToVectorGroupsResolvePoints",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("vector_group_names", "Vector Group Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("output_group_name", "Output Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("minimum_proximity", "Minimum Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_proximity", "Maximum Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_distance_from_vector_begin", "Maximum Distance From Vector Begin", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minimum_number_of_required_points", "Minimum number of required points", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                EnumText("output_type", "Output Type", "Points", PointOutputTypes),
                new("include_proximity_points", "Include Proximity Points?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_clouds",
            "Filter Clouds to Vector Groups - Resolve Clouds",
            "FilterCloudsToVectorGroupsResolveClouds",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("vector_group_names", "Vector Group Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("radial_cutoff", "Radial Cutoff", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("lower_cutoff", "Lower Cutoff", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "-0.100000", false),
                new("upper_cutoff", "Upper Cutoff", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("output_collection_name", "Output Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)],
            [new("filtered_clouds", "Filtered Clouds", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, WorkerObjectTypeValue.Cloud)]),
        Mutating(
            "cloud_and_mesh_operations.rgb_cloud_point_filter",
            "RGB Cloud Point Filter",
            "RGBCloudPointFilter",
            [new("filter_name", "Filter Name", WorkerMpValueKind.Text, "SetStringArg", "Default Filter", false),
                new("clouds_to_be_filtered", "Clouds To Be Filtered", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("red_enabled", "Red Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("red_high_enabled", "Red High Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("red_high_threshold", "Red High Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "255", false),
                new("red_low_enabled", "Red Low Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("red_low_threshold", "Red Low Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("green_enabled", "Green Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("green_high_enabled", "Green High Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("green_high_threshold", "Green High Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "255", false),
                new("green_low_enabled", "Green Low Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("green_low_threshold", "Green Low Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("blue_enabled", "Blue Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("blue_high_enabled", "Blue High Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("blue_high_threshold", "Blue High Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "255", false),
                new("blue_low_enabled", "Blue Low Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("blue_low_threshold", "Blue Low Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("gray_scale_enabled", "Gray Scale Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("gray_scale_high_enabled", "Gray Scale High Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("gray_scale_high_threshold", "Gray Scale High Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "255", false),
                new("gray_scale_low_enabled", "Gray Scale Low Enabled", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("gray_scale_low_threshold", "Gray Scale Low Threshold", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                EnumText("rgb_filter_operation", "RGB Filter Operation", "Reset and Apply Filter", RgbFilterOperations)]),
        ReadOnly(
            "cloud_and_mesh_operations.get_cloud_rgb_values",
            "Get Cloud RGB Values",
            "GetCloudRGBValues",
            [new("source_cloud_name", "Source Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                EnumText("rgb_color_channel", "RGB Color Channel", "Intensity", RgbColorChannels)],
            RgbStatisticsOutputs()),
        ReadOnly(
            "cloud_and_mesh_operations.get_cloud_rgb_values_near_point",
            "Get Cloud RGB Values Near Point",
            "GetCloudRGBValuesNearPoint",
            [new("source_cloud_name", "Source Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("single_point", "Single Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("diameter", "Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "10.000000", false),
                EnumText("rgb_color_channel", "RGB Color Channel", "Intensity", RgbColorChannels)],
            RgbStatisticsOutputs()),
        Mutating(
            "cloud_and_mesh_operations.subdivide_cloud_by_point_spacing",
            "Subdivide Cloud by Point Spacing",
            "SubdivideCloudByPointSpacing",
            [new("source_cloud_name", "Source Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.EnhancedCloud),
                new("point_spacing", "Point Spacing", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minimum_points_per_group", "Minimum Points Per Group", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("new_cloud_name", "New Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.EnhancedCloud),
                new("keep_all_groups", "Keep All Groups?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "cloud_and_mesh_operations.delete_cloud_points_by_radial_distance_from_points",
            "Delete Cloud Points by Radial Distance from Points",
            "DeleteCloudPointsByRadialDistanceFromPoints",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("points", "Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("radius", "Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("delete_inside", "Delete Inside", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            riskFlags: ["destructive"]),
        Mutating(
            "cloud_and_mesh_operations.delete_cloud_points_by_xyz_range",
            "Delete Cloud Points by X Y Z Range",
            "DeleteCloudPointsByXYZRange",
            [new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                OmittedDouble("x_min", "X Min"),
                OmittedDouble("x_max", "X Max"),
                OmittedDouble("y_min", "Y Min"),
                OmittedDouble("y_max", "Y Max"),
                OmittedDouble("z_min", "Z Min"),
                OmittedDouble("z_max", "Z Max"),
                new("delete_inside", "Delete Inside", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            riskFlags: ["destructive"]),
        Mutating(
            "cloud_and_mesh_operations.generate_general_mesh",
            "Generate General Mesh",
            "GenerateGeneralMesh",
            [new("output_mesh_name", "Output Mesh Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh),
                new("clouds_to_mesh", "Clouds to Mesh", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("maximum_triangle_size", "Maximum Triangle Size", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.050000", false),
                new("smallest_hole_diameter", "Smallest Hole Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.250000", false),
                new("finalize", "Finalize", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("use_scan_direction_for_point_normal", "Use Scan Direction For Point Normal", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("json_file", "JSON File(optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, OmitWhenAbsent: true)]),
        Mutating(
            "cloud_and_mesh_operations.consolidate_mesh",
            "Consolidate Mesh",
            "ConsolidateMesh",
            [new("mesh", "Mesh", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh)]),
        ReadOnly(
            "cloud_and_mesh_operations.mesh_volume",
            "Mesh Volume",
            "MeshVolume",
            [new("mesh", "Mesh", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh),
                new("plane", "Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane)],
            [new("above", "Above", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("below", "Below", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "cloud_and_mesh_operations.mesh_fill_holes",
            "Mesh Fill Holes",
            "MeshFillHoles",
            [new("mesh", "Mesh", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh),
                new("maximum_triangle_length", "Maximum Triangle Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "-1.000000", false),
                new("tension", "Tension", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("unconditional_filling", "Unconditional Filling?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("fill_all_holes", "Fill All Holes?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)])
    ];

    private static MpArgumentContract EnumText(
        string field,
        string mpName,
        string defaultValue,
        IReadOnlyList<string> values) =>
        new(field, mpName, WorkerMpValueKind.Text, "SetStringArg", defaultValue, false,
            EnumTextValues: values);

    private static MpArgumentContract OmittedDouble(string field, string mpName) =>
        new(field, mpName, WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "Omitted", false,
            OmitWhenAbsent: true);

    private static IReadOnlyList<MpArgumentContract> RgbStatisticsOutputs() =>
    [
        new("low_value", "Low Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
        new("high_value", "High Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
        new("average_value", "Average Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
        new("standard_deviation", "Standard Deviation", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)
    ];

    private static MpOperationContract Mutating(
        string operationId,
        string step,
        string rpc,
        IReadOnlyList<MpArgumentContract> inputs,
        IReadOnlyList<MpArgumentContract>? outputs = null,
        IReadOnlyList<string>? riskFlags = null,
        Action<IMessage>? validateRequest = null) =>
        new(
            operationId,
            step,
            Service,
            rpc,
            "state_mutation",
            Api.OperationExecutionScope.GlobalStateMutation,
            Api.ReplaySafety.Unsafe,
            riskFlags ?? [],
            inputs,
            outputs ?? [],
            validateRequest);

    private static MpOperationContract ReadOnly(
        string operationId,
        string step,
        string rpc,
        IReadOnlyList<MpArgumentContract> inputs,
        IReadOnlyList<MpArgumentContract> outputs) =>
        new(
            operationId,
            step,
            Service,
            rpc,
            "read_only",
            Api.OperationExecutionScope.GlobalStateRead,
            Api.ReplaySafety.Safe,
            [],
            inputs,
            outputs);

    private static void ValidateCloudDefaultClippingPlane(IMessage request)
    {
        var typed = (Api.SetCloudDefaultClippingPlaneRequest)request;
        if (typed.HasEnableCloudClipping && typed.EnableCloudClipping &&
            typed.ReferenceObject is null)
        {
            throw new ArgumentException(
                "Request field 'reference_object' is required when cloud clipping is enabled.",
                nameof(request));
        }
    }
}
