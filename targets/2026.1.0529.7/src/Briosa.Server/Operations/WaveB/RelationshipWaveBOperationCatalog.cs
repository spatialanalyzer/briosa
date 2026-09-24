using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveB;

internal static class RelationshipWaveBOperationCatalog
{
    private const string Service = "briosa.RelationshipOperations";

    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        Mutating(
            "relationship_operations.auto_filter_clouds_to_nominal_geometry_2d",
            "Auto Filter Clouds to Nominal Geometry 2D",
            "AutoFilterCloudsToNominalGeometry2D",
            [
                new("auto_filter_target_relationships", "Auto Filter Target Relationships", WorkerMpValueKind.CollectionItemNameList, "SetCollectionObjectNameRefListArg", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("clouds", "Clouds", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("cloud_thinning_settings", "Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions, "SetCloudThinningOptionsArg", "Message defaults", false),
                new("filter_proximity_settings_2d", "Filter Proximity Settings 2D", WorkerMpValueKind.AutoFilterProximitySettings, "SetAutoFilterProximitySettingsArg", "Required", true),
                new("geometry_extraction_tolerance", "Geometry Extraction Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.010000", false),
                new("use_feature_specific_filter_settings", "Use Feature Specific Filter Settings?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.auto_filter_clouds_to_nominal_geometry_3d",
            "Auto Filter Clouds to Nominal Geometry 3D",
            "AutoFilterCloudsToNominalGeometry3D",
            [
                new("auto_filter_target_relationships", "Auto Filter Target Relationships", WorkerMpValueKind.CollectionItemNameList, "SetCollectionObjectNameRefListArg", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("clouds", "Clouds", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("cloud_thinning_settings", "Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions, "SetCloudThinningOptionsArg", "Message defaults", false),
                new("filter_proximity_settings_3d", "Filter Proximity Settings 3D", WorkerMpValueKind.AutoFilterProximitySettings, "SetAutoFilterProximitySettingsArg", "Required", true),
                new("use_feature_specific_filter_settings", "Use Feature Specific Filter Settings?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.auto_filter_points_groups_clouds_to_surface_faces",
            "Auto Filter Points/Groups/Clouds to Surface Faces",
            "AutoFilterPointsGroupsCloudsToSurfaceFaces",
            [
                new("points", "Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("groups", "Groups", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("clouds", "Clouds", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("surface_offset", "Surface Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("edge_offset", "Edge Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("offset_direction", "Offset Direction", WorkerMpValueKind.OffsetDirectionType, "SetOffsetDirectionTypeArg", "Required", true),
                new("enforce_max_points_per_face_in_output", "Enforce Max Pts per Face in Output?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("max_points_per_face", "Max Pts per Face", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("surfaces", "Surfaces", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("cloud_thinning_settings", "Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions, "SetCloudThinningOptionsArg", "Message defaults", false),
                new("output_cloud_base_name", "Output Cloud Base Name", WorkerMpValueKind.Text, "SetStringArg", "InspAutoFilteredCloud", false),
                new("use_face_ids_for_suffix", "Use Face IDs for suffix", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "relationship_operations.auto_filter_points_to_nominal_geometry_3d",
            "Auto Filter Points to Nominal Geometry 3D",
            "AutoFilterPointsToNominalGeometry3D",
            [
                new("auto_filter_target_relationships", "Auto Filter Target Relationships", WorkerMpValueKind.CollectionItemNameList, "SetCollectionObjectNameRefListArg", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("points", "Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("filter_proximity_settings_3d", "Filter Proximity Settings 3D", WorkerMpValueKind.AutoFilterProximitySettings, "SetAutoFilterProximitySettingsArg", "Required", true)]),
        Mutating(
            "relationship_operations.compute_geometry_relationship_uncertainties",
            "Compute Geometry Relationship Uncertainties",
            "ComputeGeometryRelationshipUncertainties",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("display_results", "Display Results", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.create_points_to_objects_map",
            "Create Points to Objects Map",
            "CreatePointsToObjectsMap",
            [
                new("points", "Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("groups", "Groups", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("proximity_tolerance", "Proximity Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("points_to_objects_map_name", "Points to Objects Map Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "relationship_operations.delete_relationship",
            "Delete Relationship",
            "DeleteRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)], riskFlags: ["destructive"]),
        Mutating(
            "relationship_operations.do_relationship_fit",
            "Do Relationship Fit",
            "DoRelationshipFit",
            [
                new("collection_containing_relationships", "Collection Containing Relationships", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("objects_to_move", "Objects to Move", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("instruments_to_move", "Instruments to Move", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true),
                new("solver_mode", "Solver Mode", WorkerMpValueKind.Text, "SetStringArg", "Gauss-Newton", false, null, ["Gauss-Newton", "Levenberg-Marquardt", "Gauss-Newton /w Gradient Search", "Direct Search"]),
                new("motion_to_allow", "Motion to allow", WorkerMpValueKind.FitDegreeOfFreedomOptions, "SetFitDofOptionsArg", "Required", true),
                new("enable_randomized_start", "Enable Randomized Start", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("use_fit_dialog", "Use Fit Dialog", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("transform_in_reference", "Transform In Reference", WorkerMpValueKind.Transform, "GetTransformArg", "—", false),
                new("transform_in_working", "Transform In Working", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg", "—", false),
                new("transform_in_world", "Transform In World", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg", "—", false),
                new("fit_objective_value", "Fit Objective Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "relationship_operations.extract_geometry_from_point_clouds",
            "Extract Geometry From Point Clouds",
            "ExtractGeometryFromPointClouds",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("geometry_type", "Geometry Type", WorkerMpValueKind.GeometryType, "SetGeometryTypeArg", "Required", true),
                new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("bounding_points", "Bounding Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("seed_points", "Seed Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("tolerance", "Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("reverse_normal", "Reverse Normal", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("planar_point_count", "Planar Point Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1000", false)]),
        Mutating(
            "relationship_operations.filter_geometry_relationship_outlier_cloud_points",
            "Filter Geometry Relationship Outlier Cloud Points",
            "FilterGeometryRelationshipOutlierCloudPoints",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("sigma_threshold", "Sigma Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "3.000000", false),
                new("modify_existing_input_clouds", "Modify Existing Input Clouds", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("metrics", "First Pass RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "first_pass_rms_error"),
                new("metrics", "First Pass Maximum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "first_pass_maximum_error"),
                new("metrics", "First Pass Minimum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "first_pass_minimum_error"),
                new("metrics", "First Pass Average Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "first_pass_average_error"),
                new("metrics", "Final Pass RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "final_pass_rms_error"),
                new("metrics", "Final Pass Maximum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "final_pass_maximum_error"),
                new("metrics", "Final Pass Minimum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "final_pass_minimum_error"),
                new("metrics", "Final Pass Average Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "final_pass_average_error"),
                new("metrics", "Total Input Point Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "total_input_point_count"),
                new("metrics", "Exclude Point Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "exclude_point_count")]),
        Mutating(
            "relationship_operations.generate_geometry_relationship_summary",
            "Generate Geometry Relationship Summary",
            "GenerateGeometryRelationshipSummary",
            [
                new("relationship_ref_list", "Relationship Ref List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("summary_table_name", "Summary Table Name", WorkerMpValueKind.Text, "SetStringArg", "Geometry Relationship Summary", false)]),
        ReadOnly(
            "relationship_operations.get_general_relationship_statistics",
            "Get General Relationship Statistics",
            "GetGeneralRelationshipStatistics",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)],
            [
                new("absolute_max_deviation", "Absolute Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rms", "RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("has_signed_deviation", "Has Signed Deviation?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("signed_max_deviation", "Signed Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("signed_min_deviation", "Signed Min Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "relationship_operations.get_geom_relationship_criteria_name_list",
            "Get Geom Relationship Criteria Name List",
            "GetGeomRelationshipCriteriaNameList",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("include_all_criteria", "Include All Criteria?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("criteria_name_list", "Criteria Name List", WorkerMpValueKind.StringList, "GetStringRefListArg", "—", false)]),
        ReadOnly(
            "relationship_operations.get_objects_from_points_to_objects_map_point_list",
            "Get Objects From Points to Objects Map (Point List)",
            "GetObjectsFromPointsToObjectsMapPointList",
            [
                new("points_to_objects_map_name", "Points to Objects Map Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("points", "Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true)],
            [
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "relationship_operations.get_point_to_point_relationship_statistics",
            "Get Point to Point Relationship Statistics",
            "GetPointToPointRelationshipStatistics",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)],
            [
                new("delta_x", "Delta X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("delta_y", "Delta Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("delta_z", "Delta Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("delta_magnitude", "Delta Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("reference_frame", "Reference Frame", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg", "—", false)]),
        ReadOnly(
            "relationship_operations.get_points_to_objects_relationship_statistics",
            "Get Points to Objects Relationship Statistics",
            "GetPointsToObjectsRelationshipStatistics",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)],
            [
                new("absolute_max_deviation", "Absolute Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("max_deviation", "Max Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("min_deviation", "Min Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("avg_deviation", "Avg Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rms", "RMS", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("candidate_point_count", "# of Candidate Points", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("sampled_point_count", "# of Points Sampled", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("rejected_point_count", "# of Points Rejected", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("used_point_count", "# of Points Used", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("out_of_tolerance_point_count", "# of Points Out of Tolerance", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "relationship_operations.get_points_to_points_relationship_associated_data",
            "Get Points to Points Relationship Associated Data",
            "GetPointsToPointsRelationshipAssociatedData",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)],
            [
                new("associated_data", "Nominal Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false, null, null, false, "nominal_points"),
                new("associated_data", "Actual Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false, null, null, false, "actual_points")]),
        ReadOnly(
            "relationship_operations.get_relationship_associated_data",
            "Get Relationship Associated Data",
            "GetRelationshipAssociatedData",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)],
            [
                new("associated_data", "Relationship Type", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "relationship_type"),
                new("associated_data", "Individual Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false, null, null, false, "individual_points"),
                new("associated_data", "Point Groups", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "point_groups"),
                new("associated_data", "Point Clouds", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "point_clouds"),
                new("associated_data", "Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "objects")]),
        ReadOnly(
            "relationship_operations.get_relationship_status",
            "Get Relationship Status",
            "GetRelationshipStatus",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)],
            [
                new("status", "Dormant", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "dormant"),
                new("status", "Success", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "success"),
                new("status", "Measured", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "measured"),
                new("status", "Failed", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "failed"),
                new("status", "Unmeasured", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "unmeasured")]),
        Mutating(
            "relationship_operations.make_average_point_relationship",
            "Make Average Point Relationship",
            "MakeAveragePointRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("points_in_relationship", "Points in Relationship", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("average_point_name", "Average Point Name (Optional)", WorkerMpValueKind.PointName, "SetPointNameArg", "Omitted", false, null, null, true),
                new("nominal_point_name", "Nominal Point Name (Optional)", WorkerMpValueKind.PointName, "SetPointNameArg", "Omitted", false, null, null, true)]),
        Mutating(
            "relationship_operations.make_cloud_to_swatch_relationship",
            "Make Cloud to Swatch Relationship",
            "MakeCloudToSwatchRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("input_cloud_name", "Input Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("surface_face_list", "Surface Face List", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("reference_point", "Reference Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("maximum_radial_offset", "Maximum Radial Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.125000", false),
                new("minimum_axial_offset", "Minimum Axial Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "-0.125000", false),
                new("maximum_axial_offset", "Maximum Axial Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.125000", false),
                new("cardinal_point_group_name", "Cardinal Pt Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "relationship_operations.make_dynamic_circle_relationship",
            "Make Dynamic Circle Relationship",
            "MakeDynamicCircleRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("construction_mode", "Construction Mode", WorkerMpValueKind.DynamicCircleMode, "SetDynamicCircleModeArg", "Required", true),
                new("first_reference_geometry", "First Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("second_reference_geometry", "Second Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)]),
        Mutating(
            "relationship_operations.make_dynamic_ellipse_relationship",
            "Make Dynamic Ellipse Relationship",
            "MakeDynamicEllipseRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("construction_mode", "Construction Mode", WorkerMpValueKind.DynamicEllipseMode, "SetDynamicEllipseModeArg", "Required", true),
                new("first_reference_geometry", "First Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("second_reference_geometry", "Second Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)]),
        Mutating(
            "relationship_operations.make_dynamic_line_relationship",
            "Make Dynamic Line Relationship",
            "MakeDynamicLineRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("construction_mode", "Construction Mode", WorkerMpValueKind.DynamicLineMode, "SetDynamicLineModeArg", "Required", true),
                new("first_reference_geometry", "First Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("second_reference_geometry", "Second Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)]),
        Mutating(
            "relationship_operations.make_dynamic_plane_relationship",
            "Make Dynamic Plane Relationship",
            "MakeDynamicPlaneRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("construction_mode", "Construction Mode", WorkerMpValueKind.DynamicPlaneMode, "SetDynamicPlaneModeArg", "Required", true),
                new("first_reference_geometry", "First Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("second_reference_geometry", "Second Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("offset_plane_offset", "Offset Plane Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "relationship_operations.make_dynamic_point_relationship",
            "Make Dynamic Point Relationship",
            "MakeDynamicPointRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("construction_mode", "Construction Mode", WorkerMpValueKind.DynamicPointMode, "SetDynamicPointModeArg", "Required", true),
                new("first_reference_geometry", "First Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("second_reference_geometry", "Second Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("third_reference_geometry", "Third Reference Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)]),
        Mutating(
            "relationship_operations.make_frame_to_frame_relationship",
            "Make Frame to Frame Relationship",
            "MakeFrameToFrameRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("first_frame_name", "First Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("second_frame_name", "Second Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("orientation_tolerance", "Orientation Tolerance", WorkerMpValueKind.ToleranceScalarOptions, "SetToleranceScalarOptionsArg", "Required", true),
                new("position_tolerance", "Position Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true)]),
        Mutating(
            "relationship_operations.make_geometry_compare_only_relationship",
            "Make Geometry Compare Only Relationship",
            "MakeGeometryCompareOnlyRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("nominal_geometry", "Nominal Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("measured_geometry", "Measured Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)]),
        Mutating(
            "relationship_operations.make_geometry_fit_and_compare_to_nominal_relationship",
            "Make Geometry Fit and Compare to Nominal Relationship",
            "MakeGeometryFitAndCompareToNominalRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("nominal_geometry", "Nominal Geometry", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("point_groups_to_fit", "Point Groups to Fit", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("resulting_object_name", "Resulting Object Name (Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Any, null, true),
                new("fit_profile_name", "Fit Profile Name (Optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true)]),
        Mutating(
            "relationship_operations.make_geometry_fit_only_relationship",
            "Make Geometry Fit Only Relationship",
            "MakeGeometryFitOnlyRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("point_groups_to_fit", "Point Groups to Fit", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("geometry_type", "Geometry Type", WorkerMpValueKind.GeometryType, "SetGeometryTypeArg", "Required", true),
                new("resulting_object_name", "Resulting Object Name (Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Any, null, true),
                new("fit_profile_name", "Fit Profile Name (Optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true)]),
        Mutating(
            "relationship_operations.make_group_to_group_relationship",
            "Make Group to Group Relationship",
            "MakeGroupToGroupRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("first_group_name", "First Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("second_group_name", "Second Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("auto_update_a_vector_group", "Auto Update a Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("tolerance", "Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true),
                new("constraint", "Constraint", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true)]),
        Mutating(
            "relationship_operations.make_group_to_nominal_group_relationship",
            "Make Group to Nominal Group Relationship",
            "MakeGroupToNominalGroupRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("nominal_group_name", "Nominal Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("measured_group_name", "Measured Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("auto_update_a_vector_group", "Auto Update a Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("use_closest_point", "Use Closest Point?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("display_closest_point_watch_window", "Display Closest Point Watch Window?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("use_view_zooming_with_proximity", "Use View Zooming With Proximity?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("ignore_points_beyond_threshold", "Ignore Points Beyond Threshold?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("proximity_threshold", "Proximity Threshold?", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.010000", false),
                new("tolerance", "Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true),
                new("constraint", "Constraint", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true),
                new("fit_weight", "Fit Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false)]),
        Mutating(
            "relationship_operations.make_groups_to_objects_relationship",
            "Make Groups to Objects Relationship",
            "MakeGroupsToObjectsRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("point_groups_in_relationship", "Point Groups in Relationship", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("objects_in_relationship", "Objects in Relationship", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("projection_options", "Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg", "Required", true),
                new("auto_update_a_vector_group", "Auto Update a Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.make_object_to_object_direction_relationship",
            "Make Object to Object Direction Relationship",
            "MakeObjectToObjectDirectionRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("first_object_in_relationship", "First Object in Relationship", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("second_object_in_relationship", "Second Object in Relationship", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("nominal_angle", "Nominal Angle", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "relationship_operations.make_point_clouds_to_objects_relationship",
            "Make Point Clouds to Objects Relationship",
            "MakePointCloudsToObjectsRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("point_clouds_in_relationship", "Point Clouds in Relationship", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("objects_in_relationship", "Objects in Relationship", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("projection_options", "Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg", "Required", true),
                new("auto_update_a_vector_group", "Auto Update a Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.make_point_to_point_relationship",
            "Make Point to Point Relationship",
            "MakePointToPointRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("first_point_name", "First Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("second_point_name", "Second Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("tolerance", "Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true),
                new("constraint", "Constraint", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true)]),
        Mutating(
            "relationship_operations.make_points_to_objects_relationship",
            "Make Points to Objects Relationship",
            "MakePointsToObjectsRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("points_in_relationship", "Points in Relationship", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("objects_in_relationship", "Objects in Relationship", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("projection_options", "Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg", "Required", true),
                new("auto_update_a_vector_group", "Auto Update a Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.make_points_to_points_relationship",
            "Make Points to Points Relationship",
            "MakePointsToPointsRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("nominal_points", "Nominal Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("measured_points", "Measured Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("auto_update_a_vector_group", "Auto Update a Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("tolerance", "Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true),
                new("constraint", "Constraint", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true)]),
        Mutating(
            "relationship_operations.make_vector_group_to_vector_group_relationship",
            "Make Vector Group To Vector Group Relationship",
            "MakeVectorGroupToVectorGroupRelationship",
            [
                new("new_vg_to_vg_relationship", "New VG To VG Relationship", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("reference_vector_group", "Reference Vector Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("corresponding_vector_group", "Corresponding Vector Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("set_opposing_vector_group_polarity", "Set Opposing Vector Group Polarity", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "relationship_operations.move_collections_by_minimizing_relationships",
            "Move Collections by Minimizing Relationships",
            "MoveCollectionsByMinimizingRelationships",
            [
                new("collections_to_move", "Collections To Move", WorkerMpValueKind.StringList, "SetStringRefListArg", "Empty", false),
                new("relationships_to_minimize", "Relationships To Minimize", WorkerMpValueKind.CollectionItemNameList, "SetCollectionObjectNameRefListArg", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("solver_mode", "Solver Mode", WorkerMpValueKind.Text, "SetStringArg", "Gauss-Newton", false, null, ["Gauss-Newton", "Levenberg-Marquardt", "Gauss-Newton /w Gradient Search", "Direct Search"]),
                new("motion_to_allow", "Motion to allow", WorkerMpValueKind.FitDegreeOfFreedomOptions, "SetFitDofOptionsArg", "Required", true),
                new("use_fit_dialog", "Use Fit Dialog", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("convergence_threshold", "Convergence Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "relationship_operations.relationship_watch_window_template",
            "Relationship Watch Window Template",
            "RelationshipWatchWindowTemplate",
            [
                new("watch_window_template_name", "Watch Window Template Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("linear_precision", "Linear Precision", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "4", false),
                new("angular_precision", "Angular Precision", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "3", false),
                new("font", "Font", WorkerMpValueKind.Font, "SetFontTypeArg", "Message defaults", false),
                new("text_color", "Text Color", WorkerMpValueKind.RgbColor, "SetColorArg", "Message defaults", false),
                new("background_color", "Background Color", WorkerMpValueKind.RgbColor, "SetColorArg", "Message defaults", false),
                new("highlight_color", "Highlight Color", WorkerMpValueKind.RgbColor, "SetColorArg", "Message defaults", false),
                new("show_deviation_x", "Show Deviation X (Rx)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_deviation_y", "Show Deviation Y (Ry)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_deviation_z", "Show Deviation Z (Rz)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_deviation_magnitude", "Show Deviation Mag?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("udp_network_transmit_settings", "UDP Network Transmit Settings", WorkerMpValueKind.UdpTransmitSettings, "SetUdpTransmitSettingsArg", "Required", true),
                new("transparent_background", "Transparent Background?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("hide_units", "Hide Units?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.set_group_to_nominal_group_view_zooming",
            "Set Group To Nominal Group View Zooming",
            "SetGroupToNominalGroupViewZooming",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("use_closest_point", "Use Closest Point", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_closest_point_watch_window", "Show Closest Point Watch Window", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("use_view_zooming", "Use View Zooming", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("ignore_points_beyond_threshold", "Ignore Points Beyond Threshold", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("proximity_threshold", "Proximity Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.010000", false)]),
        Mutating(
            "relationship_operations.set_object_to_object_direction_relationship_tolerances",
            "Set Object to Object Direction Relationship Tolerances",
            "SetObjectToObjectDirectionRelationshipTolerances",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("angle_between_vectors_tolerances", "Angle Between Vectors Tolerances", WorkerMpValueKind.ToleranceScalarOptions, "SetToleranceScalarOptionsArg", "Required", true),
                new("mutual_perpendicular_length_tolerances", "Mutual Perpendicular Length Tolerances", WorkerMpValueKind.ToleranceScalarOptions, "SetToleranceScalarOptionsArg", "Required", true)]),
        Mutating(
            "relationship_operations.set_optimization_perturbation_parameters",
            "Set Optimization Perturbation Parameters",
            "SetOptimizationPerturbationParameters",
            [
                new("length_perturbation", "Length Perturbation", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000100", false),
                new("angular_perturbation", "Angular Perturbation", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000100", false),
                new("damping", "Damping ", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false)]),
        Mutating(
            "relationship_operations.set_optimization_search_options",
            "Set Optimization Search Options",
            "SetOptimizationSearchOptions",
            [
                new("max_number_of_step_size_reduction", "Max Number of Step Size Reduction", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "5", false)]),
        Mutating(
            "relationship_operations.set_points_to_points_relationship_associated_data",
            "Set Points to Points Relationship Associated Data",
            "SetPointsToPointsRelationshipAssociatedData",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("nominal_points", "Nominal Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("actual_points", "Actual Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("ignore_empty_arguments", "Ignore Empty Arguments?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "relationship_operations.set_relationship_associated_data",
            "Set Relationship Associated Data",
            "SetRelationshipAssociatedData",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("individual_points", "Individual Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("point_groups", "Point Groups", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("point_clouds", "Point Clouds", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("ignore_empty_arguments", "Ignore Empty Arguments?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "relationship_operations.set_vector_group_to_vector_group_cylindrical_zone",
            "Set Vector Group To Vector Group Cylindrical Zone",
            "SetVectorGroupToVectorGroupCylindricalZone",
            [
                new("vg_to_vg_relationship", "VG To VG Relationship", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("radial_offset", "Radial Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("minimum_axial_offset", "Minimum Axial Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "-10.000000", false),
                new("maximum_axial_offset", "Maximum Axial Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "10.000000", false)]),
        Mutating(
            "relationship_operations.set_vector_group_to_vector_group_fit_gradient_factor",
            "Set Vector Group To Vector Group Fit Gradient Factor",
            "SetVectorGroupToVectorGroupFitGradientFactor",
            [
                new("vg_to_vg_relationship", "VG To VG Relationship", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("fit_gradient_factor", "Fit Gradient Factor", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "50.000000", false)]),
        Mutating(
            "relationship_operations.set_vector_group_to_vector_group_fit_weights",
            "Set Vector Group To Vector Group Fit Weights",
            "SetVectorGroupToVectorGroupFitWeights",
            [
                new("vg_to_vg_relationship", "VG To VG Relationship", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("minimum_gap", "Minimum Gap", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minimum_gap_fit_weight", "Minimum Gap Fit Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "10.000000", false),
                new("maximum_gap", "Maximum Gap", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_gap_fit_weight", "Maximum Gap Fit Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "10.000000", false),
                new("nominal_gap", "Nominal Gap", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("nominal_gap_fit_weight", "Nominal Gap Fit Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false)]),
        Mutating(
            "relationship_operations.set_vector_group_to_vector_group_relative_polarity",
            "Set Vector Group To Vector Group Relative Polarity",
            "SetVectorGroupToVectorGroupRelativePolarity",
            [
                new("vg_to_vg_relationship", "VG To VG Relationship", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("set_opposing_vector_group_polarity", "Set Opposing Vector Group Polarity", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "relationship_operations.start_stop_relationship_trapping",
            "Start/Stop Relationship Trapping",
            "StartStopRelationshipTrapping",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("instrument_id", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("start_trapping", "Start Trapping (FALSE = Stop)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "relationship_operations.edit_geometry_relationship_point_list",
            "Edit Geometry Relationship Point List",
            "EditGeometryRelationshipPointList",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship),
                new("point_edit_mode", "Point Edit Mode", WorkerMpValueKind.Text, "SetStringArg", "Point List", false, EnumTextValues: ["Point List", "Point Graph", "Sub-Sampler Settings"])]),
        ReadOnly(
            "relationship_operations.get_relationship_sigmoidal_gap_fit_constraints",
            "Get Relationship Sigmoidal Gap Fit Constraints",
            "GetRelationshipSigmoidalGapFitConstraints",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)],
            [
                new("constraints", "Use Sigmoidal Gap Constraints", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "use_sigmoidal_gap_constraints"),
                new("constraints", "Minimum Gap Boundary", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "minimum_gap_boundary"),
                new("constraints", "Minimum Gap Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "minimum_gap_weight"),
                new("constraints", "Maximum Gap Boundary", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "maximum_gap_boundary"),
                new("constraints", "Maximum Gap Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "maximum_gap_weight"),
                new("constraints", "Nominal Gap", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "nominal_gap"),
                new("constraints", "Nominal Gap Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "nominal_gap_weight"),
                new("constraints", "Gradient Steepness Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "gradient_steepness_factor")]),
    ];

    private static MpOperationContract Mutating(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract>? outputs = null, IReadOnlyList<string>? riskFlags = null, Action<IMessage>? validateRequest = null) =>
        new(operationId, step, Service, rpc, "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, riskFlags ?? [], inputs, outputs ?? [], validateRequest);

    private static MpOperationContract ReadOnly(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract> outputs) =>
        new(operationId, step, Service, rpc, "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, [], inputs, outputs);
}
