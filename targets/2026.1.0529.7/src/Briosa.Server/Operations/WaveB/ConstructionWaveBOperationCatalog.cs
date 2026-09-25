using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveB;

internal static partial class ConstructionWaveBOperationCatalog
{
    private const string Service = "briosa.ConstructionOperations";

    private static IReadOnlyList<MpOperationContract> StandardOperations { get; } =
    [
        Mutating(
            "construction_operations.add_surface_to_mesh_offset_along_reference_direction",
            "Add Surface To Mesh Offset Along Reference Direction",
            "AddSurfaceToMeshOffsetAlongReferenceDirection",
            [
                new("reference_frame_names", "Reference Frame Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("surface_for_offset_distance_computation", "Surface for Offset Distance Computation", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("surface_offset_range", "Surface Offset Range", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "10.000000", false),
                new("collection_for_result_frames", "Collection for Result Frames", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("object_providing_direction_reference", "Object Providing Direction Reference", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("bi_directional_projection", "Bi-directional projection?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("mesh_serving_as_projection_target", "Mesh Serving As Projection Target", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh)]),
        Mutating(
            "construction_operations.auto_arrange_callout_view",
            "Auto Arrange Callout View",
            "AutoArrangeCalloutView",
            [
                new("callout_view", "Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView)]),
        Mutating(
            "construction_operations.clear_hidden_point_bar_database",
            "Clear Hidden Point Bar Database",
            "ClearHiddenPointBarDatabase",
            []),
        Mutating(
            "construction_operations.construct_b_spline_from_intersection_of_plane_and_surface",
            "Construct B-Spline From Intersection of Plane and Surface",
            "ConstructBSplineFromIntersectionOfPlaneAndSurface",
            [
                new("resulting_b_spline_name", "Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("plane_name", "Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("surface_name", "Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("approximation_tolerance", "Approximation Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.0001", false)]),
        Mutating(
            "construction_operations.construct_b_spline_from_intersection_of_surfaces",
            "Construct B-Spline From Intersection of Surfaces",
            "ConstructBSplineFromIntersectionOfSurfaces",
            [
                new("resulting_b_spline_name", "Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("first_surface_name", "First Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("second_surface_name", "Second Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("approximation_tolerance", "Approximation Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.0001", false)]),
        Mutating(
            "construction_operations.construct_b_spline_from_point_set",
            "Construct B-Spline From Point Set",
            "ConstructBSplineFromPointSet",
            [
                new("resulting_b_spline_name", "Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("b_spline_fit_options", "B-Spline Fit Options", WorkerMpValueKind.BSplineFitOptions, "SetBSplineFitOptionsArg", "Message defaults", false),
                new("point_set_container", "Point Set Container", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointSet)]),
        Mutating(
            "construction_operations.construct_b_spline_from_points",
            "Construct B-Spline From Points",
            "ConstructBSplineFromPoints",
            [
                new("resulting_b_spline_name", "Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("b_spline_fit_options", "B-Spline Fit Options", WorkerMpValueKind.BSplineFitOptions, "SetBSplineFitOptionsArg", "Message defaults", false),
                new("point_list", "Point List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_b_spline_from_several_b_splines",
            "Construct B-Spline From Several B-Splines",
            "ConstructBSplineFromSeveralBSplines",
            [
                new("resulting_b_spline_name", "Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("close_resulting_b_spline", "Close Resulting B-Spline", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_b_splines_from_intersection_of_plane_and_mesh",
            "Construct B-Splines From Intersection of Plane and Mesh",
            "ConstructBSplinesFromIntersectionOfPlaneAndMesh",
            [
                new("resulting_b_spline_name", "Resulting B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("plane_name", "Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("mesh_name", "Mesh Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh),
                new("closed_line_segment_limit", "Delete closed lines whose number of segment is less than this value", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "3", false),
                new("unclosed_line_segment_limit", "Delete unclosed lines whose number of segment is less than this value", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "3", false),
                new("create_intersection_points", "Create Intersection Points?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)],
            [
                new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_b_splines_from_lines",
            "Construct B-Splines From Lines",
            "ConstructBSplinesFromLines",
            [
                new("resulting_b_spline_name_prefix", "Resulting B-Spline Name prefix (Optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true),
                new("line_list", "Line List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)],
            [
                new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_b_splines_from_surfaces",
            "Construct B-Splines From Surfaces",
            "ConstructBSplinesFromSurfaces",
            [
                new("resulting_b_spline_name_prefix", "Resulting B-Spline Name prefix (Optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true),
                new("surface_list", "Surface List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)],
            [
                new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_boundary_points_from_cloud",
            "Construct Boundary Points from Cloud",
            "ConstructBoundaryPointsFromCloud",
            [
                new("source_cloud_name", "Source Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("destination_cloud_name", "Destination Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud)]),
        Mutating(
            "construction_operations.construct_circle",
            "Construct Circle",
            "ConstructCircle",
            [
                new("circle_name", "Circle Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Circle),
                new("circle_center", "Circle Center (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("circle_normal", "Circle Normal (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("circle_radius", "Circle Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_circles_from_surface_faces_runtime_select",
            "Construct Circles From Surface Faces - Runtime Select",
            "ConstructCirclesFromSurfaceFacesRuntimeSelect",
            []),
        Mutating(
            "construction_operations.construct_circles_lines_from_surfaces",
            "Construct Circles (Lines) From Surfaces",
            "ConstructCirclesLinesFromSurfaces",
            [
                new("surfaces", "Surfaces", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("minimum_diameter", "Minimum Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_diameter", "Maximum Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("tolerance", "Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.020000", false),
                new("single_surface", "Single Surface?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("circle_line_mode", "Circle Line Mode", WorkerMpValueKind.Text, "SetStringArg", "Required", true, null, ["Circle", "Line"]),
                new("destination_collection_name", "Destination Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("base_name", "Base Name", WorkerMpValueKind.Text, "SetStringArg", "Geometry Object", false)],
            [
                new("geometry_objects", "Geometry Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_collection",
            "Construct Collection",
            "ConstructCollection",
            [
                new("collection_name", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("folder_path", "Folder Path", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("make_default_collection", "Make Default Collection?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_cone",
            "Construct Cone",
            "ConstructCone",
            [
                new("cone_name", "Cone Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cone),
                new("cone_end_point", "Cone End Point (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("cone_axis", "Cone Axis (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("cone_length", "Cone Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("cone_theta_start", "Cone Theta Start", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("cone_theta_span", "Cone Theta Span", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("cone_included_angle", "Cone Included Angle", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_cones_from_surface_faces_runtime_select",
            "Construct Cones From Surface Faces - Runtime Select",
            "ConstructConesFromSurfaceFacesRuntimeSelect",
            []),
        Mutating(
            "construction_operations.construct_cross_section_cloud",
            "Construct Cross Section Cloud",
            "ConstructCrossSectionCloud",
            [
                new("cross_section_cloud_name", "Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.CrossSectionCloud),
                new("cylindrical_cross_section_mode", "Cylindrical Cross Section Mode?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("start_distance", "Start Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("section_spacing", "Section Spacing", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("proximity_threshold", "Proximity Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_section_count", "Maximum Section Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("limit_cross_section_extent", "Limit Cross Section Extent", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("radius_limit", "Radius Limit", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("project_to_reference_surface", "Project to Reference Surface", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("reference_object", "Reference Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("input_clouds", "Input Clouds", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("cloud_thinning_settings", "Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions, "SetCloudThinningOptionsArg", "Message defaults", false),
                new("update_existing_cloud", "Update Existing Cloud", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_cross_section_cloud_user_select",
            "Construct Cross Section Cloud - User Select",
            "ConstructCrossSectionCloudUserSelect",
            [
                new("cross_section_cloud_name", "Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.CrossSectionCloud),
                new("proximity_threshold", "Proximity Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("limit_cross_section_extent", "Limit Cross Section Extent", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("radius_limit", "Radius Limit", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("project_to_reference_surface", "Project to Reference Surface", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("reference_planes", "Reference Planes", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("input_clouds", "Input Clouds", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("cloud_thinning_settings", "Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions, "SetCloudThinningOptionsArg", "Message defaults", false),
                new("update_existing_cloud", "Update Existing Cloud", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_cylinder",
            "Construct Cylinder",
            "ConstructCylinder",
            [
                new("cylinder_name", "Cylinder Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cylinder),
                new("cylinder_end_point", "Cylinder End Point (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("cylinder_axis", "Cylinder Axis (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("cylinder_diameter", "Cylinder Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("cylinder_length", "Cylinder Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_cylinder_from_end_points",
            "Construct Cylinder From End Points",
            "ConstructCylinderFromEndPoints",
            [
                new("cylinder_name", "Cylinder Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cylinder),
                new("cylinder_end_point_a", "Cylinder End Point A (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("cylinder_end_point_b", "Cylinder End Point B (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("cylinder_diameter", "Cylinder Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_cylinders_from_surface_faces_runtime_select",
            "Construct Cylinders From Surface Faces - Runtime Select",
            "ConstructCylindersFromSurfaceFacesRuntimeSelect",
            []),
        Mutating(
            "construction_operations.construct_ellipse",
            "Construct Ellipse",
            "ConstructEllipse",
            [
                new("ellipse_name", "Ellipse Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Ellipse),
                new("center_coordinate", "Center Coordinate", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("normal_direction", "Normal Direction", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("major_axis_radius", "Major Axis Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minor_axis_radius", "Minor Axis Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_ellipsoid",
            "Construct Ellipsoid",
            "ConstructEllipsoid",
            [
                new("ellipse_name", "Ellipse Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("x_axis_radius", "X-Axis Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "5.000000", false),
                new("y_axis_radius", "Y-Axis Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "4.000000", false),
                new("z_axis_radius", "Z-Axis Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "3.000000", false),
                new("magnification", "Magnification", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("uncertainty_ellipsoid", "Uncertainty Ellipsoid?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("transform_in_working_coordinates", "Transform in Working Coordinates", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true),
                new("ellipse_color", "Ellipse Color", WorkerMpValueKind.RgbColor, "SetColorArg", "Message defaults", false)]),
        Mutating(
            "construction_operations.construct_folders",
            "Construct Folder(s)",
            "ConstructFolders",
            [
                new("folder_path", "Folder Path", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "construction_operations.construct_frame",
            "Construct Frame",
            "ConstructFrame",
            [
                new("new_frame_name", "New Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("transform_in_working_coordinates", "Transform in Working Coordinates", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_frame_at_point_with_working_z_and_clocked_axis",
            "Construct Frame, at Point, with working Z, and clocked axis",
            "ConstructFrameAtPointWithWorkingZAndClockedAxis",
            [
                new("origin_point", "Origin Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("clocked_axis", "Clocked axis", WorkerMpValueKind.AxisIdentifier, "SetAxisNameArg", "Required", true),
                new("clocking_point", "Clocking Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.FrameName, "SetFrameNameArg", "Omitted", false, null, null, true)]),
        Mutating(
            "construction_operations.construct_frame_at_robot_link",
            "Construct Frame at Robot Link",
            "ConstructFrameAtRobotLink",
            [
                new("machine_id", "Machine ID", WorkerMpValueKind.CollectionMachineId, "SetColMachineIdArg", "Required", true),
                new("link_name", "Link Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("resulting_frame", "Resulting Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)]),
        Mutating(
            "construction_operations.construct_frame_average_of_other_object_frames",
            "Construct Frame - Average of Other Object Frames",
            "ConstructFrameAverageOfOtherObjectFrames",
            [
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Frame, null, true)]),
        Mutating(
            "construction_operations.construct_frame_copy_and_make_left_handed",
            "Construct Frame - Copy And Make Left Handed",
            "ConstructFrameCopyAndMakeLeftHanded",
            [
                new("reference_frame", "Reference Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, null, null, true),
                new("axis_to_reverse", "Axis to reverse", WorkerMpValueKind.AxisIdentifier, "SetAxisNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_frame_from_point_measurement_probing_frames",
            "Construct Frame From Point Measurement Probing Frames",
            "ConstructFrameFromPointMeasurementProbingFrames",
            [
                new("point_list", "Point List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("show_frame", "Show Frame? (Hide = FALSE)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_frame_from_transform_in_world",
            "Construct Frame From Transform In World",
            "ConstructFrameFromTransformInWorld",
            [
                new("new_frame_name", "New Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("transform_in_world_coordinates", "Transform in World Coordinates", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_frame_known_origin_object_direction_object_direction",
            "Construct Frame, Known Origin, Object Direction, Object Direction",
            "ConstructFrameKnownOriginObjectDirectionObjectDirection",
            [
                new("known_point", "Known Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("known_point_value_in_new_frame", "Known Point Value in New Frame", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("primary_axis_object", "Primary Axis Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("primary_axis_defines_which_axis", "Primary Axis Defines Which Axis", WorkerMpValueKind.AxisIdentifier, "SetAxisNameArg", "Required", true),
                new("secondary_axis_object", "Secondary Axis Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("secondary_axis_defines_which_axis", "Secondary Axis Defines Which Axis", WorkerMpValueKind.AxisIdentifier, "SetAxisNameArg", "Required", true),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Frame, null, true)]),
        Mutating(
            "construction_operations.construct_frame_on_instrument_base",
            "Construct Frame on Instrument Base",
            "ConstructFrameOnInstrumentBase",
            [
                new("instrument_id", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.FrameName, "SetFrameNameArg", "Omitted", false, null, null, true)]),
        Mutating(
            "construction_operations.construct_frame_on_object",
            "Construct Frame on Object",
            "ConstructFrameOnObject",
            [
                new("reference_object", "Reference Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Frame, null, true)]),
        Mutating(
            "construction_operations.construct_frame_pick_origin_and_point_on_x_axis_clock_z_along_working_z",
            "Construct Frame, Pick origin and point on X axis - clock Z along working Z",
            "ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZ",
            [
                new("origin_point", "Origin Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("point_on_x_axis", "Point on X-Axis", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.FrameName, "SetFrameNameArg", "Omitted", false, null, null, true)]),
        Mutating(
            "construction_operations.construct_frame_three_planes",
            "Construct Frame, 3 Planes",
            "ConstructFrameThreePlanes",
            [
                new("x_plane", "X Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("x_value_on_plane", "X Value on PLane", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("y_plane", "Y Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("y_value_on_plane", "Y Value on PLane", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("z_plane", "Z Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("z_value_on_plane", "Z Value on Plane", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Frame, null, true)]),
        Mutating(
            "construction_operations.construct_frame_three_points",
            "Construct Frame, 3 Points",
            "ConstructFrameThreePoints",
            [
                new("construction_method", "Construction Method", WorkerMpValueKind.Text, "SetStringArg", "Required", true, null, ["Origin X XY", "Origin X XZ", "Origin Y Yx", "Origin Y YZ", "Origin Z Zx", "Origin X Zy"]),
                new("origin_point", "Origin Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("primary_axis_point", "Primary Axis Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("secondary_axis_point", "Secondary Axis Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("frame_name", "Frame Name (Optional)", WorkerMpValueKind.FrameName, "SetFrameNameArg", "Omitted", false, null, null, true)]),
        Mutating(
            "construction_operations.construct_frame_with_wizard",
            "Construct Frame with Wizard",
            "ConstructFrameWithWizard",
            [
                new("new_frame_name", "New Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("wait_for_completion", "Wait for Completion", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "construction_operations.construct_frames_by_projecting_frames_on_mesh_along_frame_direction",
            "Construct Frames By Projecting Frames On Mesh Along Frame Direction",
            "ConstructFramesByProjectingFramesOnMeshAlongFrameDirection",
            [
                new("reference_frame_names", "Reference Frame Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("base_name_for_projected_frames", "Base Name For Projected Frames", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("bi_directional_projection", "Bi-directional projection?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("mesh_serving_as_projection_target", "Mesh Serving As Projection Target", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh)],
            [
                new("resultant_frame_name_list", "Resultant Frame Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_frames_by_projecting_frames_on_mesh_along_reference_direction",
            "Construct Frames By Projecting Frames On Mesh Along Reference Direction",
            "ConstructFramesByProjectingFramesOnMeshAlongReferenceDirection",
            [
                new("reference_frame_names", "Reference Frame Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("base_name_for_projected_frames", "Base Name For Projected Frames", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("object_providing_direction_reference", "Object Providing Direction Reference", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("bi_directional_projection", "Bi-directional projection?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("mesh_serving_as_projection_target", "Mesh Serving As Projection Target", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh)],
            [
                new("resultant_frame_name_list", "Resultant Frame Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_line_center_of_slot",
            "Construct Line Center of Slot",
            "ConstructLineCenterOfSlot",
            [
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("slot_name", "Slot Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Slot)]),
        Mutating(
            "construction_operations.construct_line_from_instrument_shot",
            "Construct Line From Instrument Shot",
            "ConstructLineFromInstrumentShot",
            [
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("observation_index", "Observation Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line)]),
        Mutating(
            "construction_operations.construct_line_normal_to_object",
            "Construct Line Normal to Object",
            "ConstructLineNormalToObject",
            [
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("line_length", "Line Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("object", "Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)]),
        Mutating(
            "construction_operations.construct_line_normal_to_object_through_point",
            "Construct Line - Normal to Object through Point",
            "ConstructLineNormalToObjectThroughPoint",
            [
                new("line_to_create", "Line To Create", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("object_name", "Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_line_project_line_to_object_reference_plane",
            "Construct Line - Project Line to Object Reference Plane",
            "ConstructLineProjectLineToObjectReferencePlane",
            [
                new("line_to_create", "Line To Create", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("line_to_project", "Line To Project", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("object_to_project_to", "Object to project to", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)]),
        Mutating(
            "construction_operations.construct_line_two_plane_intersection",
            "Construct Line 2 Plane Intersection",
            "ConstructLineTwoPlaneIntersection",
            [
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("first_plane", "First Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("second_plane", "Second Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane)]),
        Mutating(
            "construction_operations.construct_line_two_points",
            "Construct Line 2 Points",
            "ConstructLineTwoPoints",
            [
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("first_point", "First Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("second_point", "Second Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_line_two_points_vector_notation",
            "Construct Line 2 Points (Vector Notation)",
            "ConstructLineTwoPointsVectorNotation",
            [
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("first_vector", "First Vector", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("second_vector", "Second Vector", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_lines_from_surface_faces_runtime_select",
            "Construct Lines From Surface Faces - Runtime Select",
            "ConstructLinesFromSurfaceFacesRuntimeSelect",
            []),
        Mutating(
            "construction_operations.construct_mirror_cube_frame",
            "Construct Mirror Cube Frame",
            "ConstructMirrorCubeFrame",
            [
                new("mirror_cube_frame_name", "Mirror Cube Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("use_current_measurements_marked_as_mirror_shots", "Use Current Measurements Marked as Mirror Shots", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("nominal_cube_face_angle", "Nominal Cube Face Angle", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "90.000000", false)],
            [
                new("total_angular_error", "Total Angular Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.construct_perimeter_from_points",
            "Construct Perimeter From Points",
            "ConstructPerimeterFromPoints",
            [
                new("resulting_perimeter_name", "Resulting Perimeter Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Perimeter),
                new("point_list", "Point List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("open_perimeter", "Open Perimeter?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_plane",
            "Construct Plane",
            "ConstructPlane",
            [
                new("plane_name", "Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("plane_center", "Plane Center (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("plane_normal", "Plane Normal (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("plane_edge_dimension", "Plane Edge Dimension", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_plane_normal_to_object_through_point",
            "Construct Plane, Normal to Object, Through Point",
            "ConstructPlaneNormalToObjectThroughPoint",
            [
                new("resultant_plane_name", "Resultant Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("normal_to_object_name", "'Normal to' Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("through_point_name", "'Through' Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("plane_edge_dimension", "Plane Edge Dimension", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_planes_bisect_two_planes",
            "Construct Planes, Bisect 2 Planes",
            "ConstructPlanesBisectTwoPlanes",
            [
                new("resultant_plane_name", "Resultant Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("first_plane", "First Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("second_plane", "Second Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane)]),
        Mutating(
            "construction_operations.construct_planes_bounding_point_group",
            "Construct Planes, Bounding Point Group",
            "ConstructPlanesBoundingPointGroup",
            [
                new("reference_plane_name", "Reference Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("group_to_bound", "Group to bound", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("resulting_high_plane_name", "Resulting 'High' Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("resulting_low_plane_name", "Resulting 'Low' Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("override_target_point_offsets", "Override Target/Point Offsets", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("offset_value", "Offset Value", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_planes_from_surface_faces_runtime_select",
            "Construct Planes From Surface Faces - Runtime Select",
            "ConstructPlanesFromSurfaceFacesRuntimeSelect",
            []),
        Mutating(
            "construction_operations.construct_point_at_circle_center",
            "Construct a Point at Circle Center",
            "ConstructPointAtCircleCenter",
            [
                new("circle_name", "Circle Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Circle),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_at_intersection_of_b_spline_and_surfaces",
            "Construct Point at intersection of B-Spline and Surfaces",
            "ConstructPointAtIntersectionOfBSplineAndSurfaces",
            [
                new("b_spline_name", "B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("surface_list", "Surface List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("approximation_tolerance", "Approximation Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.001000", false),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_at_intersection_of_plane_and_line",
            "Construct Point at Intersection of Plane and Line",
            "ConstructPointAtIntersectionOfPlaneAndLine",
            [
                new("plane_name", "Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_at_intersection_of_planes",
            "Construct Point at Intersection of Planes",
            "ConstructPointAtIntersectionOfPlanes",
            [
                new("plane_1_name", "Plane 1 Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("plane_2_name", "Plane 2 Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("plane_3_name", "Plane 3 Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_at_intersection_of_two_b_splines",
            "Construct Point at Intersection of 2 B-Splines",
            "ConstructPointAtIntersectionOfTwoBSplines",
            [
                new("first_b_spline_name", "First B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("second_b_spline_name", "Second B-Spline Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_at_intersection_of_two_lines",
            "Construct Point at Intersection of Two Lines",
            "ConstructPointAtIntersectionOfTwoLines",
            [
                new("first_line_name", "First Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("second_line_name", "Second Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_at_line_midpoint",
            "Construct a Point at line MidPoint",
            "ConstructPointAtLineMidpoint",
            [
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_at_projection_of_point_onto_object",
            "Construct a Point at Projection of Point onto An Object",
            "ConstructPointAtProjectionOfPointOntoObject",
            [
                new("point_to_project", "Point to Project", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("object_name", "Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_cloud_from_existing_clouds",
            "Construct Point Cloud from Existing Clouds",
            "ConstructPointCloudFromExistingClouds",
            [
                new("existing_point_cloud_list", "Existing Point Cloud List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("new_cloud_name", "New Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("cloud_thinning_settings", "Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions, "SetCloudThinningOptionsArg", "Message defaults", false),
                new("hide_original_point_clouds", "Hide Original Point Clouds", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("set_cloud_point_rgb_from_voxels", "Set Cloud Point RGB from Voxels?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_point_cloud_from_visible_cloud_points",
            "Construct Point Cloud from Visible Cloud Points",
            "ConstructPointCloudFromVisibleCloudPoints",
            [
                new("source_clouds", "Source Clouds", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("destination_cloud_name", "Destination Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud)]),
        Mutating(
            "construction_operations.construct_point_cloud_limiting_probing_directions",
            "Construct Point Cloud Limiting Probing Directions",
            "ConstructPointCloudLimitingProbingDirections",
            [
                new("source_cloud_name", "Source Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("normal_to_object_name", "'Normal to' Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("acceptance_angle", "Acceptance Angle", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "30.000000", false),
                new("destination_cloud_name", "Destination Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("hide_source_cloud", "Hide Source Cloud", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_point_clouds_from_existing_cloud_points_runtime_select",
            "Construct Point Clouds from Existing Cloud Points - Runtime Select",
            "ConstructPointCloudsFromExistingCloudPointsRuntimeSelect",
            [
                new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud)]),
        Mutating(
            "construction_operations.construct_point_clouds_from_existing_clouds_uniform_spacing",
            "Construct Point Clouds from Existing Clouds - Uniform Spacing",
            "ConstructPointCloudsFromExistingCloudsUniformSpacing",
            [
                new("existing_point_cloud_list", "Existing Point Cloud List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("desired_point_spacing", "Desired Point Spacing", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.020000", false),
                new("minimum_points_per_output_point", "Minimum Points Per Output Point", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "3", false),
                new("new_cloud_name", "New Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("hide_original_point_clouds", "Hide Original Point Clouds", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "construction_operations.construct_point_clouds_from_existing_point_group",
            "Construct Point Clouds from Existing Point Group",
            "ConstructPointCloudsFromExistingPointGroup",
            [
                new("point_group_name", "Point Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud)]),
        Mutating(
            "construction_operations.construct_point_fit_to_points",
            "Construct Point (Fit to Points)",
            "ConstructPointFitToPoints",
            [
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_from_cloud_point_runtime_select",
            "Construct Point From Cloud Point - Runtime Select",
            "ConstructPointFromCloudPointRuntimeSelect",
            [
                new("selection_prompt", "Selection Prompt", WorkerMpValueKind.Text, "SetStringArg", "Select cloud point", false),
                new("construct_point", "Construct Point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("constructed_point_name", "Constructed Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)],
            [
                new("selection_cloud_point_coordinates", "Selection Cloud Point Coordinates", WorkerMpValueKind.Vector, "GetVectorArg", "—", false)]),
        Mutating(
            "construction_operations.construct_point_from_survey_target_center",
            "Construct Point From Survey Target Center",
            "ConstructPointFromSurveyTargetCenter",
            [
                new("cloud_containing_target", "Cloud Containing Target", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("reference_seed_point", "Reference Seed Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("survey_target_type", "Survey Target Type", WorkerMpValueKind.Text, "SetStringArg", "Triangle", false, null, ["Triangle", "Circle"]),
                new("search_diameter", "Search Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("result_center_point_name", "Result Center Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_point_group_from_point_cloud",
            "Construct Point Group from Point Cloud",
            "ConstructPointGroupFromPointCloud",
            [
                new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("point_group_name", "Point Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("point_prefix", "Point Prefix", WorkerMpValueKind.Text, "SetStringArg", "pt", false),
                new("starting_point_number", "Starting Point Number", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("point_offset", "Point Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("sub_sampling", "Sub-Sampling?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("sub_sampling_distance", "Sub-Sampling Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.500000", false),
                new("show_progress", "Show Progress?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_point_group_from_point_name_ref_list",
            "Construct Point Group from Point Name Ref List",
            "ConstructPointGroupFromPointNameRefList",
            [
                new("point_name_list", "Point Name List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_name", "Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "construction_operations.construct_point_groups_from_vector_groups",
            "Construct Point Groups from Vector Groups",
            "ConstructPointGroupsFromVectorGroups",
            [
                new("vector_groups", "Vector Groups", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("optional_group_name_suffix", "Optional Group Name Suffix", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true),
                new("make_vector_begin_points", "Make Vector Begin Points", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("make_vector_end_points", "Make Vector End Points", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("point_groups", "Point Groups", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_point_in_working_coordinates",
            "Construct a Point in Working Coordinates",
            "ConstructPointInWorkingCoordinates",
            [
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("working_coordinates", "Working Coordinates", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_points_at_intersection_of_circle_and_line",
            "Construct Points at Intersection of Circle and Line",
            "ConstructPointsAtIntersectionOfCircleAndLine",
            [
                new("circle_name", "Circle Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Circle),
                new("line_name", "Line Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Line),
                new("base_point_name_for_results", "Base Point Name for results", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_points_at_intersection_of_principal_object_axes_and_surfaces",
            "Construct Points at Intersection of Principle Object Axes and Surfaces",
            "ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfaces",
            [
                new("axis_object_list", "Axis Object List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("surface_list", "Surface List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("point_suffix", "Point Suffix (optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true),
                new("resultant_group_name", "Resultant Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "construction_operations.construct_points_at_projection_on_surfaces_parallel_to_wcf_axis",
            "Construct Points at Projection on Surfaces - Parallel to WCF Axis",
            "ConstructPointsAtProjectionOnSurfacesParallelToWcfAxis",
            [
                new("surface_list", "Surface List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_name_to_contain_new_points", "Group Name to Contain New Points", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_name_prefix", "Point Name Prefix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_name_suffix", "Point Name Suffix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("axis", "Axis", WorkerMpValueKind.AxisIdentifier, "SetAxisNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_points_at_projection_on_surfaces_radial_from_wcf_axis",
            "Construct Points at Projection on Surfaces - Radial from WCF Axis",
            "ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxis",
            [
                new("surface_list", "Surface List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_name_to_contain_new_points", "Group Name to Contain New Points", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_name_prefix", "Point Name Prefix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_name_suffix", "Point Name Suffix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("axis", "Axis", WorkerMpValueKind.AxisIdentifier, "SetAxisNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_points_at_projection_on_surfaces_spherical_from_wcf_origin",
            "Construct Points at Projection on Surfaces - Spherical from WCF Origin",
            "ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOrigin",
            [
                new("surface_list", "Surface List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_name_to_contain_new_points", "Group Name to Contain New Points", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_name_prefix", "Point Name Prefix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_name_suffix", "Point Name Suffix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "construction_operations.construct_points_auto_correspond_two_groups_inter_point_distance",
            "Construct Points Auto-Correspond 2 groups Inter-Point Distance",
            "ConstructPointsAutoCorrespondTwoGroupsInterPointDistance",
            [
                new("reference_group", "Reference group (known point names)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("group_to_be_copied", "Group to be copied (unknown point names)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("same_point_tolerance", "Auto-correspond same-point tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("group_to_contain_matched_points", "Group to contain matched points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "construction_operations.construct_points_auto_correspond_two_groups_proximity",
            "Construct Points Auto-Correspond 2 groups Proximity",
            "ConstructPointsAutoCorrespondTwoGroupsProximity",
            [
                new("reference_group", "Reference group (known point names)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("group_to_be_copied", "Group to be copied (unknown point names)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("same_point_tolerance", "Auto-correspond same-point tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.250000", false),
                new("group_to_contain_matched_points", "Group to contain matched points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "construction_operations.construct_points_by_projecting_points_on_mesh_along_direction",
            "Construct Points By Projecting Points On Mesh Along Direction",
            "ConstructPointsByProjectingPointsOnMeshAlongDirection",
            [
                new("reference_point_names", "Reference Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_name_for_projected_points", "Group Name For Projected Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("object_providing_direction_reference", "Object Providing Direction Reference", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("bi_directional_projection", "Bi-directional projection?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("mesh_serving_as_projection_target", "Mesh Serving As Projection Target", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh)],
            [
                new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_points_cylindrically_shifted",
            "Construct Points Cylindrically Shifted",
            "ConstructPointsCylindricallyShifted",
            [
                new("reference_object_name", "Reference Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("original_points", "Original Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_for_new_points", "Group for New Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("radial_shift", "Radial Shift", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("theta_shift", "Theta Shift (degrees)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("planar_shift", "Planar Shift", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_points_from_cylinder",
            "Construct Points from Cylinder",
            "ConstructPointsFromCylinder",
            [
                new("cylinder_name", "Cylinder Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cylinder),
                new("group_name", "Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "construction_operations.construct_points_from_surface_faces_runtime_select",
            "Construct Points From Surface Faces - Runtime Select",
            "ConstructPointsFromSurfaceFacesRuntimeSelect",
            []),
        Mutating(
            "construction_operations.construct_points_from_surfaces_on_uv_grid",
            "Construct Points From Surfaces On UV Grid",
            "ConstructPointsFromSurfacesOnUvGrid",
            [
                new("surface_list", "Surface List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("uv_point_group_base_name", "UV Point Group Base Name", WorkerMpValueKind.Text, "SetStringArg", "UV Points", false),
                new("make_each_line_separate_group", "Make Each Line Separate Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("number_of_u_grids", "Number of U Grids", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "5", false),
                new("number_of_v_grids", "Number of V Grids", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "5", false),
                new("edge_point_mode", "Edge Point Mode", WorkerMpValueKind.EdgeMode, "SetEdgeModeArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_points_layout_on_grid",
            "Construct Points Layout on Grid",
            "ConstructPointsLayoutOnGrid",
            [
                new("group_name", "Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("point_prefix", "Point Prefix", WorkerMpValueKind.Text, "SetStringArg", "p", false),
                new("x_min", "X Min", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("x_max", "X Max", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "100.000000", false),
                new("x_count", "X Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "10", false),
                new("y_min", "Y Min", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("y_max", "Y Max", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "50.000000", false),
                new("y_count", "Y Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "10", false),
                new("z_min", "Z Min", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("z_max", "Z Max", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("z_count", "Z Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1", false)]),
        Mutating(
            "construction_operations.construct_points_n_spaced_on_curves",
            "Construct Points N-Spaced on Curves",
            "ConstructPointsNSpacedOnCurves",
            [
                new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("number_of_evenly_spaced_points", "Number of Evenly Spaced Points", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "10", false),
                new("resultant_group_name", "Resultant Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("resultant_point_name_prefix", "Resultant Point Name Prefix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "construction_operations.construct_points_on_curves_using_max_chordal_deviation",
            "Construct Points on Curves Using Max Chordal Deviation",
            "ConstructPointsOnCurvesUsingMaxChordalDeviation",
            [
                new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("maximum_chordal_deviation", "Maximum Chordal Deviation", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.050000", false),
                new("maximum_trim_edge_angle", "Maximum Trim Edge Angle", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "15.000000", false),
                new("maximum_chord_length", "Maximum Chord Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("resultant_group_name", "Resultant Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("resultant_point_name_prefix", "Resultant Point Name Prefix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "construction_operations.construct_points_on_object_vertices",
            "Construct Points on Objects Vertices",
            "ConstructPointsOnObjectVertices",
            [
                new("object_name_list", "Object Name List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("resultant_group_name", "Resultant Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "construction_operations.construct_points_on_surfaces_by_clicking",
            "Construct Points on Surface(s) by Clicking",
            "ConstructPointsOnSurfacesByClicking",
            [
                new("group_name_for_points", "Group Name for Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("first_point_name", "First Point Name", WorkerMpValueKind.Text, "SetStringArg", "p0", false)]),
        Mutating(
            "construction_operations.construct_points_shifted_in_working_frame",
            "Construct Points Shifted in Working Frame",
            "ConstructPointsShiftedInWorkingFrame",
            [
                new("original_points", "Original Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_for_new_points", "Group for New Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("shift_vector", "Shift Vector", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_points_spaced_at_distance_on_curves",
            "Construct Points Spaced at a Distance on Curves",
            "ConstructPointsSpacedAtDistanceOnCurves",
            [
                new("b_spline_list", "B-Spline List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("distance_between_points", "Distance Between Points", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.500000", false),
                new("resultant_group_name", "Resultant Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("resultant_point_name_prefix", "Resultant Point Name Prefix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "construction_operations.construct_points_subset_with_greatest_spacing",
            "Construct Points Subset with greatest spacing",
            "ConstructPointsSubsetWithGreatestSpacing",
            [
                new("points_to_subsample", "Points to Subsample", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("subset_size", "Subset Size", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "10", false),
                new("group_for_subset", "Group for Subset", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "construction_operations.construct_points_wildcard_selection",
            "Construct Points WildCard Selection",
            "ConstructPointsWildcardSelection",
            [
                new("groups_to_select_from", "Groups to Select From", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("wildcard_selection_names", "WildCard Selection Names", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("group_for_new_points", "Group for New Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("include_prior_complete_name", "Include prior complete name", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_sphere",
            "Construct Sphere",
            "ConstructSphere",
            [
                new("sphere_name", "Sphere Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Sphere),
                new("sphere_center", "Sphere Center (in working coordinates)", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("sphere_radius", "Sphere Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.construct_spheres_from_surface_faces_runtime_select",
            "Construct Spheres From Surface Faces - Runtime Select",
            "ConstructSpheresFromSurfaceFacesRuntimeSelect",
            []),
        Mutating(
            "construction_operations.construct_surface_by_dissecting_surfaces",
            "Construct Surface by Dissecting Surface(s)",
            "ConstructSurfaceByDissectingSurfaces",
            [
                new("dissection_mode", "Dissection Mode", WorkerMpValueKind.SurfaceDissectionModeType, "SetSurfDissectModeTypeArg", "Required", true)],
            [
                new("resultant_surfaces_list", "Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_surface_by_offsetting_surface",
            "Construct surface by offsetting a surface",
            "ConstructSurfaceByOffsettingSurface",
            [
                new("reference_surface", "Reference Surface", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("surface_offset", "Surface offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("hide_original_surface", "Hide original surface?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "construction_operations.construct_surface_fit_from_nominal_surfaces_and_actual_data",
            "Construct Surface Fit From Nominal Surfaces and Actual Data",
            "ConstructSurfaceFitFromNominalSurfacesAndActualData",
            [
                new("nominal_surface", "Nominal Surface", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("actual_data_point_list", "Actual Data Point List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface)]),
        Mutating(
            "construction_operations.construct_surface_from_annotation_links",
            "Construct Surface From Annotation Links",
            "ConstructSurfaceFromAnnotationLinks",
            [
                new("annotation_list", "Annotation List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface)]),
        Mutating(
            "construction_operations.construct_surface_from_b_splines",
            "Construct Surface From BSplines",
            "ConstructSurfaceFromBSplines",
            [
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("b_spline_list", "BSpline List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_surface_from_collection_of_surfaces",
            "Construct Surface From a Collection of Surfaces",
            "ConstructSurfaceFromCollectionOfSurfaces",
            [
                new("surfaces_to_combine", "Surfaces to Combine", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("hide_original_surfaces", "Hide Original Surfaces?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("delete_original_surfaces", "Delete Original Surfaces?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("enable_sewing_tolerance", "Enable Sewing Tolerance?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("sewing_tolerance", "Sewing Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "-1.000000", false)]),
        Mutating(
            "construction_operations.construct_surface_from_cone",
            "Construct Surface From Cone",
            "ConstructSurfaceFromCone",
            [
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("cone_name", "Cone Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cone)]),
        Mutating(
            "construction_operations.construct_surface_from_cylinder",
            "Construct Surface From Cylinder",
            "ConstructSurfaceFromCylinder",
            [
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("cylinder_name", "Cylinder Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cylinder),
                new("internal_cylinder", "Internal Cylinder?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("use_theta_extent_mode", "Use Theta Extent Mode?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_surface_from_plane",
            "Construct Surface From Plane",
            "ConstructSurfaceFromPlane",
            [
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("plane_name", "Plane Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Plane)]),
        Mutating(
            "construction_operations.construct_surface_from_point_groups",
            "Construct Surface From Point Groups",
            "ConstructSurfaceFromPointGroups",
            [
                new("group_name_list", "Group Name List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("b_spline_fit_options", "B-Spline Fit Options", WorkerMpValueKind.BSplineFitOptions, "SetBSplineFitOptionsArg", "Message defaults", false),
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface)]),
        Mutating(
            "construction_operations.construct_surface_from_sphere",
            "Construct Surface From Sphere",
            "ConstructSurfaceFromSphere",
            [
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("sphere_name", "Sphere Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Sphere)]),
        Mutating(
            "construction_operations.construct_surfaces_by_dissecting_surfaces_from_ref_list",
            "Construct Surfaces by Dissecting Surfaces from Ref List",
            "ConstructSurfacesByDissectingSurfacesFromRefList",
            [
                new("surfaces_to_dissect", "Surfaces to Dissect", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)],
            [
                new("resultant_surfaces_list", "Resultant Surfaces List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.construct_surfaces_by_projecting_points",
            "Construct Surfaces By Projecting Points",
            "ConstructSurfacesByProjectingPoints",
            [
                new("projection_target_name_list", "Projection Target Name List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("point_list", "Point List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("resulting_surface_name", "Resulting Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface)]),
        Mutating(
            "construction_operations.construct_surfaces_from_objects",
            "Construct Surfaces From Objects",
            "ConstructSurfacesFromObjects",
            [
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_vector_group_area_profile_check",
            "Construct a Vector Group - Area Profile Check",
            "ConstructVectorGroupAreaProfileCheck",
            [
                new("reference_vectors", "Reference Vectors", WorkerMpValueKind.VectorNameList, "SetVectorNameRefListArg", "Required", true),
                new("vector_groups_to_check", "Vector Groups to Check", WorkerMpValueKind.CollectionVectorGroupNameList, "SetCollectionVectorGroupNameRefListArg", "Required", true),
                new("area_radius", "Area Radius", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("area_tolerance", "Area Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("resultant_vector_group_name", "Resultant Vector Group Name", WorkerMpValueKind.CollectionVectorGroupName, "SetColVectorGroupNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_vector_group_from_relationship",
            "Construct a Vector Group From a Relationship",
            "ConstructVectorGroupFromRelationship",
            [
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionVectorGroupName, "SetColVectorGroupNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_vector_group_from_vector_name_ref_list",
            "Construct a Vector Group From Vector Name Ref List",
            "ConstructVectorGroupFromVectorNameRefList",
            [
                new("vector_name_list", "Vector Name List", WorkerMpValueKind.VectorNameList, "SetVectorNameRefListArg", "Required", true),
                new("resultant_vector_group_name", "Resultant Vector Group Name", WorkerMpValueKind.CollectionVectorGroupName, "SetColVectorGroupNameArg", "Required", true)]),
        Mutating(
            "construction_operations.construct_vector_group_group_to_group_compare",
            "Construct a Vector Group - Group to Group Compare",
            "ConstructVectorGroupGroupToGroupCompare",
            [
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("group_a", "Group A", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("group_b", "Group B", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("rms_deviation_tolerance", "RMS Deviation Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("max_absolute_deviation_tolerance", "Max Absolute Deviation Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("average_deviation_tolerance", "Average Deviation Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("vector_count", "Vector Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("max_absolute_deviation", "Max Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("average_deviation", "Average Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.construct_vector_in_working_coordinates_begin_delta",
            "Construct a Vector in Working Coordinates(Begin/Delta)",
            "ConstructVectorInWorkingCoordinatesBeginDelta",
            [
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("new_vector_name", "New Vector Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("begin_in_working_coordinates", "'Begin' in Working Coordinates", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("delta_in_working_coordinates", "'Delta' in Working Coordinates", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("is_magnitude_negative", "Is Magnitude Negative", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.construct_vector_in_working_coordinates_begin_direction_magnitude",
            "Construct a Vector in Working Coordinates(Begin/Direction/Mag.)",
            "ConstructVectorInWorkingCoordinatesBeginDirectionMagnitude",
            [
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("new_vector_name", "New Vector Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("begin_in_working_coordinates", "'Begin' in Working Coordinates", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("direction_in_working_coordinates", "'Direction' in Working Coordinates", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("signed_magnitude", "Signed Magnitude", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.copy_groups_excluding_obscured_points",
            "Copy Groups Excluding Obscured Points",
            "CopyGroupsExcludingObscuredPoints",
            [
                new("instrument_id", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("group_names", "Group Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("new_collection_name", "New Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "construction_operations.copy_object",
            "Copy Object",
            "CopyObject",
            [
                new("source_object", "Source Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("new_object_name", "New Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("overwrite_if_exists", "Overwrite if exists?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.copy_objects_point_to_point_delta",
            "Copy Objects - Point to Point Delta",
            "CopyObjectsPointToPointDelta",
            [
                new("objects_to_copy", "Objects to Copy", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("first_delta_point", "First Delta Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("second_delta_point", "Second Delta Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("destination_collection_name", "Destination Collection Name (Optional)", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Omitted", false, null, null, true)]),
        Mutating(
            "construction_operations.copy_objects_to_a_collection",
            "Copy Objects to a collection",
            "CopyObjectsToACollection",
            [
                new("source_objects", "Source Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("destination_collection_name", "Destination Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "construction_operations.create_hidden_point",
            "Create Hidden Point",
            "CreateHiddenPoint",
            [
                new("end_a_point_name", "End A Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("end_b_point_name", "End B Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("hidden_point_rod_index", "Hidden Point Rod Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("overwrite_existing_point", "Overwrite existing point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("point_name_to_create", "Point Name To Create", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.create_hidden_point_rod",
            "Create Hidden Point Rod",
            "CreateHiddenPointRod",
            [
                new("hidden_point_rod_name", "Hidden Point Rod Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("a_to_b_distance", "A to B (Target to Target) Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("a_to_c_distance", "A to C (Target to Tip) Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("inter_point_tolerance", "A to B Inter-point Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("hidden_point_rod_index", "Hidden Point Rod Index", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        Mutating(
            "construction_operations.create_min_max_vector_group_callout",
            "Create Min/Max Vector Group Callout",
            "CreateMinMaxVectorGroupCallout",
            [
                new("destination_callout_view", "Destination Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("number_of_vectors_with_highest_mag", "Number of vectors with Highest Mag?", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1", false),
                new("number_of_vectors_with_lowest_mag", "Number of vectors with Lowest Mag?", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1", false),
                new("show_collection", "Show Collection?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_vector_group", "Show Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_vector_name", "Show Vector Name?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_dx", "Show dX?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_dy", "Show dY?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_dz", "Show dZ?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_d_mag", "Show dMag?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_tolerance_color", "Show Tolerance Color?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("tolerance_color_blue_green_red", "Tolerance Color Blue(+)/Green/Red(-)?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_out_of_tolerance_value", "Show Out of Tolerance Value?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_tolerance_range", "Show Tolerance Range?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_vector_color", "Show Vector Color?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_start_point", "Show Start Point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_end_point", "Show End Point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_units", "Show Units?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("attach_callout_to_end_point", "Attach Callout to End Point?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("use_default_placement", "Use default placement?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.create_picture_callout",
            "Create Picture Callout",
            "CreatePictureCallout",
            [
                new("destination_callout_view", "Destination Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("picture_name", "Picture Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Picture),
                new("view_x_position", "View X Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.400000", false),
                new("view_y_position", "View Y Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.600000", false),
                new("scale_image_percent", "Scale Image Percent (10-200)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "Omitted", false, null, null, true),
                new("object_for_callout_anchor_point", "Object for Callout Anchor Point", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Omitted", false, null, null, true, null, WorkerItemTypeValue.Any)]),
        Mutating(
            "construction_operations.create_point_callout",
            "Create Point Callout",
            "CreatePointCallout",
            [
                new("destination_callout_view", "Destination Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Any),
                new("point", "Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("view_x_position", "View X Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("view_y_position", "View Y Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("show_point_collection", "Show Point Collection?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_point_group", "Show Point Group?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_point_target", "Show Point Target?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_x", "Show X (R)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_y", "Show Y (Theta)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_z", "Show Z (Phi)?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_units", "Show Units?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_ux", "Show Ux (Ur)?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_uy", "Show Uy (Utheta)?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_uz", "Show Uz (Uphi)?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_umag", "Show Umag?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("desired_coordinate_system", "Desired Coordinate System", WorkerMpValueKind.CoordinateSystemType, "SetCoordinateSystemTypeArg", "Required", true),
                new("notes", "Notes (blank for none)", WorkerMpValueKind.EditText, "SetEditTextArg", "Empty", false),
                new("use_default_placement", "Use default placement?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.create_point_comparison_callout",
            "Create Point Comparison Callout",
            "CreatePointComparisonCallout",
            [
                new("destination_callout_view", "Destination Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Any),
                new("first_point", "First Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("second_point", "Second Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("view_x_position", "View X Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("view_y_position", "View Y Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("show_first_point_collection", "Show First Point Collection?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_first_point_group", "Show First Point Group?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_first_point_target", "Show First Point Target?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_first_point_coordinates", "Show First Point Coordinates?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_second_point_collection", "Show Second Point Collection?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_second_point_group", "Show Second Point Group?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_second_point_target", "Show Second Point Target?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_second_point_coordinates", "Show Second Point Coordinates?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_dx", "Show dX?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_dy", "Show dY?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_dz", "Show dZ?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_d_mag", "Show dMag?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("additional_x_comments", "Additional X Comments (blank for none)", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("additional_y_comments", "Additional Y Comments (blank for none)", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("additional_z_comments", "Additional Z Comments (blank for none)", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("additional_notes", "Additional Notes (blank for none)", WorkerMpValueKind.EditText, "SetEditTextArg", "Empty", false),
                new("use_default_placement", "Use default placement?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.create_relationship_callout",
            "Create Relationship Callout",
            "CreateRelationshipCallout",
            [
                new("destination_callout_view", "Destination Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("relationship_name", "Relationship Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Relationship),
                new("view_x_position", "View X Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("view_y_position", "View Y Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("additional_notes", "Additional Notes (blank for none)", WorkerMpValueKind.EditText, "SetEditTextArg", "Empty", false)]),
        Mutating(
            "construction_operations.create_text_callout",
            "Create Text Callout",
            "CreateTextCallout",
            [
                new("destination_callout_view", "Destination Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("text", "Text", WorkerMpValueKind.EditText, "SetEditTextArg", "Omitted", false, null, null, true),
                new("view_x_position", "View X Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.400000", false),
                new("view_y_position", "View Y Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.600000", false),
                new("callout_anchor_point", "Callout Anchor Point (Optional)", WorkerMpValueKind.PointName, "SetPointNameArg", "Omitted", false, null, null, true)]),
        Mutating(
            "construction_operations.create_vector_callout",
            "Create Vector Callout",
            "CreateVectorCallout",
            [
                new("destination_callout_view", "Destination Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("vector_name", "Vector Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("view_x_position", "View X Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("view_y_position", "View Y Position", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("show_collection", "Show Collection?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_vector_group", "Show Vector Group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_vector_name", "Show Vector Name?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_dx", "Show dX?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_dy", "Show dY?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_dz", "Show dZ?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_d_mag", "Show dMag?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_tolerance_color", "Show Tolerance Color?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("show_out_of_tolerance_value", "Show Out of Tolerance Value?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_tolerance_range", "Show Tolerance Range?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_vector_color", "Show Vector Color?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_start_point", "Show Start Point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_end_point", "Show End Point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_units", "Show Units?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("additional_notes", "Additional Notes (blank for none)", WorkerMpValueKind.EditText, "SetEditTextArg", "Empty", false),
                new("attach_callout_to_end_point", "Attach Callout to End Point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("use_default_placement", "Use default placement?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.decompose_transform_into_doubles_euler_xyz",
            "Decompose Transform into Doubles (Euler XYZ)",
            "DecomposeTransformIntoDoublesEulerXyz",
            [
                new("input_transform", "Input Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rx", "Euler Rx", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("ry", "Euler Ry", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rz", "Euler Rz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_transform_into_doubles_euler_zxz",
            "Decompose Transform into Doubles (Euler ZXZ)",
            "DecomposeTransformIntoDoublesEulerZxz",
            [
                new("input_transform", "Input Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("first_rz", "Euler Rz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rx", "Euler Rx", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("second_rz", "Euler Rz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_transform_into_doubles_euler_zyx",
            "Decompose Transform into Doubles (Euler ZYX)",
            "DecomposeTransformIntoDoublesEulerZyx",
            [
                new("input_transform", "Input Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rz", "Euler Rz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("ry", "Euler Ry", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rx", "Euler Rx", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_transform_into_doubles_euler_zyz",
            "Decompose Transform into Doubles (Euler ZYZ)",
            "DecomposeTransformIntoDoublesEulerZyz",
            [
                new("input_transform", "Input Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("first_rz", "Euler Rz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("ry", "Euler Ry", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("second_rz", "Euler Rz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_transform_into_doubles_fixed_xyz",
            "Decompose Transform into Doubles (Fixed XYZ)",
            "DecomposeTransformIntoDoublesFixedXyz",
            [
                new("input_transform", "Input Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rx", "Rx (Roll)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("ry", "Ry (Pitch)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rz", "Rz (Yaw)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_transform_into_vectors_fixed_xyz",
            "Decompose Transform into Vectors (Fixed XYZ)",
            "DecomposeTransformIntoVectorsFixedXyz",
            [
                new("input_transform", "Input Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("position_in_working", "Position in Working", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("orientation_in_working", "Orientation in Working", WorkerMpValueKind.Vector, "GetVectorArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_transform_into_vectors_origin_and_axes",
            "Decompose Transform into Vectors (Origin and Axes)",
            "DecomposeTransformIntoVectorsOriginAndAxes",
            [
                new("transform", "Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("origin", "Origin", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("x_axis", "X Axis", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("y_axis", "Y Axis", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("z_axis", "Z Axis", WorkerMpValueKind.Vector, "GetVectorArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_world_transform_operator_into_doubles_fixed_xyz_in_world",
            "Decompose World Transform Operator into Doubles (Fixed XYZ in World)",
            "DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorld",
            [
                new("input_world_transform_operator", "Input World Transform Operator", WorkerMpValueKind.WorldTransform, "SetWorldTransformArg", "Required", true)],
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rx", "Rx (Roll)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("ry", "Ry (Pitch)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rz", "Rz (Yaw)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("scale", "Scale", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.decompose_world_transform_operator_into_vectors_fixed_xyz_in_world",
            "Decompose World Transform Operator into Vectors (Fixed XYZ in World)",
            "DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorld",
            [
                new("input_world_transform_operator", "Input World Transform Operator", WorkerMpValueKind.WorldTransform, "SetWorldTransformArg", "Required", true)],
            [
                new("position_in_working", "Position in Working", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("orientation_in_working", "Orientation in Working", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("scale", "Scale", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "construction_operations.delete_callout_view",
            "Delete Callout View",
            "DeleteCalloutView",
            [
                new("callout_view", "Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView)], riskFlags: ["destructive"]),
        Mutating(
            "construction_operations.delete_collection",
            "Delete Collection",
            "DeleteCollection",
            [
                new("collection_name", "Name of Collection to Delete", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)], riskFlags: ["destructive"]),
        Mutating(
            "construction_operations.delete_collections_by_wildcard",
            "Delete Collections by Wildcard",
            "DeleteCollectionsByWildcard",
            [
                new("search_string", "Search String", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("case_sensitive_search", "Case Sensitive Search", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_deleting_all_collections", "Allow Deleting all Collections", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("num_deleted", "Num Deleted", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("num_failed", "Num Failed", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        Mutating(
            "construction_operations.delete_folders_by_wildcard",
            "Delete Folders by Wildcard",
            "DeleteFoldersByWildcard",
            [
                new("search_string", "Search String", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("case_sensitive_search", "Case Sensitive Search", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_deleting_all_folders", "Allow Deleting all Folders", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("num_deleted", "Num Deleted", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("num_failed", "Num Failed", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        Mutating(
            "construction_operations.delete_hidden_point_rod",
            "Delete Hidden Point Rod",
            "DeleteHiddenPointRod",
            [
                new("hidden_point_rod_index", "Hidden Point Rod Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)], riskFlags: ["destructive"]),
        Mutating(
            "construction_operations.delete_points",
            "Delete Points",
            "DeletePoints",
            [
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true)], riskFlags: ["destructive"]),
        Mutating(
            "construction_operations.delete_points_wildcard_selection",
            "Delete Points WildCard Selection",
            "DeletePointsWildcardSelection",
            [
                new("groups_to_delete_from", "Groups to Delete From", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("wildcard_selection_names", "WildCard Selection Names", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)], riskFlags: ["destructive"]),
        Mutating(
            "construction_operations.extract_sphere_centers_from_point_cloud",
            "Extract Sphere Centers from Point Cloud",
            "ExtractSphereCentersFromPointCloud",
            [
                new("cloud_name", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("desired_diameter", "Desired Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("extraction_tolerance", "Extraction Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minimum_point_count", "Minimum Point Count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "50", false),
                new("group_name_for_points", "Group Name for Points", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("perform_final_fit", "Perform Final Fit", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("final_fit_cone_angle", "Final Fit Cone Angle", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "120.000000", false)],
            [
                new("number_of_points_extracted", "Number of Points Extracted", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "construction_operations.get_collection_instrument_ref_list_variable",
            "Get Collection Instrument Ref List Variable",
            "GetCollectionInstrumentRefListVariable",
            [
                new("name", "Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("value", "Value", WorkerMpValueKind.CollectionInstrumentIdList, "GetColInstIdRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.get_hidden_point_rod_index_by_name",
            "Get Hidden Point Rod Index by Name",
            "GetHiddenPointRodIndexByName",
            [
                new("hidden_point_rod_name", "Hidden Point Rod Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("hidden_point_rod_index", "Hidden Point Rod Index", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "construction_operations.get_ith_callout_position_in_callout_view",
            "Get I-th Callout Position in Callout View",
            "GetIthCalloutPositionInCalloutView",
            [
                new("callout_view", "Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("callout_view_index", "Callout View Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)],
            [
                new("x_position", "X Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("y_position", "Y Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("x_anchor_position", "X Anchor Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("y_anchor_position", "Y Anchor Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("callout_width", "Callout Width", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("callout_height", "Callout Height", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "construction_operations.get_number_of_callouts_in_callout_view",
            "Get Number of Callouts in Callout View",
            "GetNumberOfCalloutsInCalloutView",
            [
                new("callout_view", "Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView)],
            [
                new("callouts_count", "Callouts Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "construction_operations.get_working_transform_of_object_fixed_xyz",
            "Get Working Transform of Object (Fixed XYZ)",
            "GetWorkingTransformOfObjectFixedXyz",
            [
                new("object_name", "Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any)],
            [
                new("transform", "Transform", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        Mutating(
            "construction_operations.invert_transform",
            "Invert Transform",
            "InvertTransform",
            [
                new("transform", "Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true)],
            [
                new("inverse_transform", "Inverse Transform", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_instrument_id_runtime_select",
            "Make a Collection Instrument ID - Runtime Select",
            "MakeCollectionInstrumentIdRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("instrument_id", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_instrument_ref_list_runtime_select",
            "Make a Collection Instrument Reference List- Runtime Select",
            "MakeCollectionInstrumentRefListRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("resultant_collection_instrument_ref_list", "Resultant Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList, "GetColInstIdRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_item_name_ref_list_wildcard_selection",
            "Make a Collection Item Name Reference List - WildCard Selection",
            "MakeCollectionItemNameRefListWildcardSelection",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("item_wildcard_criteria", "Item Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("item_type", "Item Type", WorkerMpValueKind.Text, "SetStringArg", "Required", true)],
            [
                new("resultant_collection_item_name_ref_list", "Resultant Collection Item Name Reference List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_name_runtime_select",
            "Make a Collection Name - Runtime Select",
            "MakeCollectionNameRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("resultant_collection_name", "Resultant Collection Name", WorkerMpValueKind.CollectionName, "GetCollectionNameArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_object_name_ref_list_by_type",
            "Make a Collection Object Name Ref List - By Type",
            "MakeCollectionObjectNameRefListByType",
            [
                new("collection", "Collection", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("object_type", "Object Type", WorkerMpValueKind.ObjectType, "SetObjectTypeArg", "Required", true)],
            [
                new("resultant_collection_object_name_list", "Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_object_name_ref_list_by_type_and_color",
            "Make a Collection Object Name Ref List - By Type and Color",
            "MakeCollectionObjectNameRefListByTypeAndColor",
            [
                new("collection", "Collection", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("object_type", "Object Type", WorkerMpValueKind.ObjectType, "SetObjectTypeArg", "Required", true),
                new("object_color", "Object Color", WorkerMpValueKind.RgbColor, "SetColorArg", "Message defaults", false)],
            [
                new("resultant_collection_object_name_list", "Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_object_name_ref_list_from_all_groups_in_collection",
            "Make a Collection Object Name Ref List from all Groups in a Collection",
            "MakeCollectionObjectNameRefListFromAllGroupsInCollection",
            [
                new("collection_name", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)],
            [
                new("collection_object_name_list", "Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_object_name_ref_list_runtime_select",
            "Make a Collection Object Name Reference List- Runtime Select",
            "MakeCollectionObjectNameRefListRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("object_type", "Object Type", WorkerMpValueKind.ObjectType, "SetObjectTypeArg", "Required", true)],
            [
                new("resultant_collection_object_name_ref_list", "Resultant Collection Object Name Reference List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_object_name_ref_list_wildcard_selection",
            "Make a Collection Object Name Reference List- WildCard Selection",
            "MakeCollectionObjectNameRefListWildcardSelection",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("object_wildcard_criteria", "Object Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("object_type", "Object Type", WorkerMpValueKind.ObjectType, "SetObjectTypeArg", "Required", true)],
            [
                new("resultant_collection_object_name_ref_list", "Resultant Collection Object Name Reference List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_object_name_runtime_select",
            "Make a Collection Object Name - Runtime Select",
            "MakeCollectionObjectNameRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("object_type", "Object Type", WorkerMpValueKind.ObjectType, "SetObjectTypeArg", "Required", true)],
            [
                new("resultant_collection_object_name", "Resultant Collection Object Name", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_collection_vector_group_name_ref_list_runtime_select",
            "Make a Collection Vector Group Name Ref List - Runtime Select",
            "MakeCollectionVectorGroupNameRefListRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("resultant_collection_vector_group_name_reference_list", "Resultant Collection Vector Group Name Reference List", WorkerMpValueKind.CollectionVectorGroupNameList, "GetCollectionVectorGroupNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_event_ref_list_wildcard_selection",
            "Make an Event Reference List- WildCard Selection",
            "MakeEventRefListWildcardSelection",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("event_wildcard_criteria", "Event Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false)],
            [
                new("resultant_event_ref_list", "Resultant Event Reference List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_picture_name_ref_list_runtime_select",
            "Make a Picture Name Ref List - Runtime Select",
            "MakePictureNameRefListRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("picture_name_list", "Picture Name List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_point_name_ref_list_from_group",
            "Make a Point Name Ref List From a Group",
            "MakePointNameRefListFromGroup",
            [
                new("group_name", "Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)],
            [
                new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_point_name_ref_list_runtime_select",
            "Make a Point Name Ref List - Runtime Select",
            "MakePointNameRefListRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_point_name_ref_list_wildcard_select",
            "Make a Point Name Ref List - Wildcard Select",
            "MakePointNameRefListWildcardSelect",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("group_name_wildcard_criteria", "Group Name Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("point_name_wildcard_criteria", "Point Name Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false)],
            [
                new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_point_name_runtime_select",
            "Make a Point Name - Runtime Select",
            "MakePointNameRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("resultant_point_name", "Resultant Point Name", WorkerMpValueKind.PointName, "GetPointNameArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_report_ref_list_from_collection",
            "Make a Report Ref List from a Collection",
            "MakeReportRefListFromCollection",
            [
                new("collection_name", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)],
            [
                new("report_list", "Report List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_report_ref_list_runtime_select",
            "Make a Report Ref List - Runtime Select",
            "MakeReportRefListRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("report_list", "Report List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_system_string",
            "Make a System String",
            "MakeSystemString",
            [
                new("string_content", "String Content", WorkerMpValueKind.SystemString, "SetSystemStringArg", "Required", true),
                new("format_string", "Format String (Optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true)],
            [
                new("resultant_string", "Resultant String", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_transform_from_doubles_euler_parameters",
            "Make a Transform from Doubles (Euler Parameters)",
            "MakeTransformFromDoublesEulerParameters",
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("e1", "e1", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("e2", "e2", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("e3", "e3", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("e4", "e4", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("resultant_transform", "Resultant Transform", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_transform_from_doubles_fixed_xyz",
            "Make a Transform from Doubles (Fixed XYZ)",
            "MakeTransformFromDoublesFixedXyz",
            [
                new("x", "X", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("y", "Y", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("z", "Z", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("rx", "Rx (Roll)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("ry", "Ry (Pitch)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("rz", "Rz (Yaw)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("resultant_transform", "Resultant Transform", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_vector_name_ref_list_from_vector_group",
            "Make a Vector Name Ref List From a Vector Group",
            "MakeVectorNameRefListFromVectorGroup",
            [
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup)],
            [
                new("resultant_vector_name_list", "Resultant Vector Name List", WorkerMpValueKind.VectorNameList, "GetVectorNameRefListArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_vector_name_ref_list_runtime_select",
            "Make a Vector Name Ref List - Runtime Select",
            "MakeVectorNameRefListRuntimeSelect",
            [
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", " Select Vectors (ENTER when done) ", false)],
            [
                new("resultant_vector_name_list", "Resultant Vector Name List", WorkerMpValueKind.VectorNameList, "GetVectorNameRefListArg", "—", false)]),
        Mutating(
            "construction_operations.make_vector_names_unique_in_vector_group",
            "Make Vector Names Unique in Vector Group",
            "MakeVectorNamesUniqueInVectorGroup",
            [
                new("vector_group_name", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup)]),
        Mutating(
            "construction_operations.mirror_objects",
            "Mirror Object(s)",
            "MirrorObjects",
            [
                new("objects", "Object(s)", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("frame_name", "Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("frame_plane_to_mirror_around", "Frame Plane to Mirror Around", WorkerMpValueKind.Text, "SetStringArg", "Required", true, null, ["XY", "XZ", "YZ"]),
                new("copy", "Copy? [FALSE = Move]", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "construction_operations.move_objects_point_to_point_delta",
            "Move Objects - Point to Point Delta",
            "MoveObjectsPointToPointDelta",
            [
                new("objects_to_move", "Objects to Move", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("first_delta_point", "First Delta Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("second_delta_point", "Second Delta Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "construction_operations.move_objects_to_a_collection",
            "Move Objects to a collection",
            "MoveObjectsToACollection",
            [
                new("source_objects", "Source Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("destination_collection_name", "Destination Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "construction_operations.rename_callout_view",
            "Rename Callout View",
            "RenameCalloutView",
            [
                new("original_callout_view_name", "Original Callout View Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("new_callout_view_name", "New Callout View Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("overwrite_if_exists", "Overwrite if exists?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.rename_collection",
            "Rename Collection",
            "RenameCollection",
            [
                new("original_collection_name", "Original Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("new_collection_name", "New Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "construction_operations.rename_item",
            "Rename Item",
            "RenameItem",
            [
                new("original_item_name", "Original Item Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Any),
                new("new_item_name", "New Item Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Any),
                new("overwrite_if_exists", "Overwrite if exists?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.rename_object",
            "Rename Object",
            "RenameObject",
            [
                new("original_object_name", "Original Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("new_object_name", "New Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("overwrite_if_exists", "Overwrite if exists?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.rename_point",
            "Rename Point",
            "RenamePoint",
            [
                new("original_point_name", "Original Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("new_point_name", "New Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("overwrite_if_exists", "Overwrite if exists?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "construction_operations.rename_points_with_name_pattern",
            "Rename Points with Name Pattern",
            "RenamePointsWithNamePattern",
            [
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("name_pattern", "Name Pattern", WorkerMpValueKind.Text, "SetStringArg", "NewName_%d", false),
                new("start_value", "Start Value", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1", false)]),
        Mutating(
            "construction_operations.set_collection_instrument_ref_list_variable",
            "Set Collection Instrument Ref List Variable",
            "SetCollectionInstrumentRefListVariable",
            [
                new("name", "Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("value", "Value", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true)]),
        Mutating(
            "construction_operations.set_ith_callout_position_in_callout_view",
            "Set I-th Callout Position in Callout View",
            "SetIthCalloutPositionInCalloutView",
            [
                new("callout_view", "Callout View", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.CalloutView),
                new("callout_view_index", "Callout View Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("x_position", "X Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("y_position", "Y Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "construction_operations.set_or_construct_default_collection",
            "Set (or construct) default collection",
            "SetOrConstructDefaultCollection",
            [
                new("collection_name", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "construction_operations.set_point_position_in_working_coordinates",
            "Set Point Position in Working Coordinates",
            "SetPointPositionInWorkingCoordinates",
            [
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("position_in_working_coordinates", "Position In Working Coordinates", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true)]),
        Mutating(
            "construction_operations.shift_plane",
            "Shift Plane",
            "ShiftPlane",
            [
                new("plane", "Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("shift_along_normal", "Shift Along Normal", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("grow_bounds_by_factor", "Grow Bounds by Factor", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "construction_operations.transform_points_by_delta_about_working_frame",
            "Transform Points by Delta (About Working Frame)",
            "TransformPointsByDeltaAboutWorkingFrame",
            [
                new("point_name_list", "Point Name List", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("delta_in_working_coordinates", "Delta In Working Coordinates", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true)]),
    ];

    private static MpOperationContract Mutating(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract>? outputs = null, IReadOnlyList<string>? riskFlags = null, Action<IMessage>? validateRequest = null) =>
        new(operationId, step, Service, rpc, "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, riskFlags ?? [], inputs, outputs ?? [], validateRequest);

    private static MpOperationContract ReadOnly(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract> outputs) =>
        new(operationId, step, Service, rpc, "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, [], inputs, outputs);
}
