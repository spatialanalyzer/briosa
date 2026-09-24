using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;

namespace Briosa.Server.Operations.WaveB;

internal static partial class ConstructionWaveBOperationCatalog
{
    public static IReadOnlyList<MpOperationContract> Operations =>
    [
        .. StandardOperations,
        .. SpecialOperations
    ];

    private static IReadOnlyList<MpOperationContract> SpecialOperations { get; } =
    [
        Mutating(
            "construction_operations.add_collection_instruments_to_ref_list_wildcard_selection",
            "Add Collection Instruments to a Ref List - WildCard Selection",
            "AddCollectionInstrumentsToRefListWildcardSelection",
            [
                new("collection_instrument_ref_list", "Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true),
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("instrument_wildcard_criteria", "Instrument Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false)
            ],
            [new("collection_instrument_ref_list", "Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList, "GetColInstIdRefListArg", "—", false)]),
        Mutating(
            "construction_operations.average_set_of_groups",
            "Average a set of Groups",
            "AverageSetOfGroups",
            [
                new("group_names", "Group Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("resulting_group_name", "Resulting Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("rms_tolerance", "RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("maximum_absolute_tolerance", "Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("maximum_average_tolerance", "Maximum Average Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false)
            ],
            [
                new("statistics", "RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "rms_deviation"),
                new("statistics", "Max Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "max_absolute_deviation"),
                new("statistics", "Average Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "average_deviation")
            ]),
        Mutating(
            "construction_operations.construct_geometry_from_surfaces",
            "Construct Geometry From Surfaces",
            "ConstructGeometryFromSurfaces",
            [
                new("surfaces", "Surfaces", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Surface),
                new("minimum_diameter", "Minimum Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("maximum_diameter", "Maximum Diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("reference_frame", "Reference Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.Frame, OmitWhenAbsent: true),
                new("destination_collection_name", "Destination Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Omitted", false, OmitWhenAbsent: true),
                new("base_name", "Base Name", WorkerMpValueKind.Text, "SetStringArg", "Geometry Object", false)
            ],
            [new("geometry_objects", "Geometry Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, WorkerObjectTypeValue.Any)]),
        Mutating(
            "construction_operations.construct_objects_from_surface_faces_runtime_select",
            "Construct Objects From Surface Faces - Runtime Select",
            "ConstructObjectsFromSurfaceFacesRuntimeSelect",
            [
                new("construct_planes", "Construct Planes?", WorkerMpValueKind.Logical, "SetBoolArg", "Required", true),
                new("construct_cylinders", "Construct Cylinders?", WorkerMpValueKind.Logical, "SetBoolArg", "Required", true),
                new("construct_spheres", "Construct Spheres?", WorkerMpValueKind.Logical, "SetBoolArg", "Required", true),
                new("construct_cones", "Construct Cones?", WorkerMpValueKind.Logical, "SetBoolArg", "Required", true),
                new("construct_lines", "Construct Lines?", WorkerMpValueKind.Logical, "SetBoolArg", "Required", true),
                new("construct_points", "Construct Points?", WorkerMpValueKind.Logical, "SetBoolArg", "Required", true),
                new("construct_circles", "Construct Circles?", WorkerMpValueKind.Logical, "SetBoolArg", "Required", true)
            ]),
        Mutating(
            "construction_operations.construct_point_at_object_origin",
            "Construct Point at Object Origin",
            "ConstructPointAtObjectOrigin",
            [
                new("object_name", "Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("resultant_point_name", "Resultant Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)
            ],
            [
                new("origin", "Vector Representation", WorkerMpValueKind.Vector, "GetVectorArg", "—", false, NestedFieldName: "vector_representation"),
                new("origin", "X Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "x_value"),
                new("origin", "Y Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "y_value"),
                new("origin", "Z Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "z_value")
            ]),
        Gradient(
            "construction_operations.get_gradient_at_projected_point_on_surface",
            "Get Gradient At Projected Point On Surface",
            "GetGradientAtProjectedPointOnSurface",
            [
                new("point_to_project", "Point to Project", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("surface_name", "Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("generate_output_vector_lines", "Generate output vector lines?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)
            ]),
        Gradient(
            "construction_operations.get_gradient_at_projected_point_on_surface_edge",
            "Get Gradient At Projected Point On Surface Edge",
            "GetGradientAtProjectedPointOnSurfaceEdge",
            [
                new("point_to_project", "Point to Project", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("surface_edge", "Surface Edge (B-Spline)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.BSpline),
                new("surface_name", "Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("edge_offset_direction", "Edge Offset Direction", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("edge_offset_distance", "Edge Offset Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.01", false),
                new("generate_output_vector_lines", "Generate output vector lines?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)
            ]),
        ReadOnly(
            "construction_operations.make_callout_view_ref_list_wildcard_selection",
            "Make a Callout View Ref List - WildCard Selection",
            "MakeCalloutViewRefListWildcardSelection",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("callout_view_wildcard_criteria", "Callout View Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false)
            ],
            [new("callout_views", "Resultant Callout View List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, ItemTypeWhenOmitted: WorkerItemTypeValue.CalloutView)]),
        ReadOnly(
            "construction_operations.make_collection_object_name_ensure_unique",
            "Make a Collection Object Name - Ensure Unique",
            "MakeCollectionObjectNameEnsureUnique",
            [
                new("collection_object_name", "Collection Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("use_number_suffix", "Use Number Suffix?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)
            ],
            [new("collection_object_name", "Collection Object Name", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg", "—", false, WorkerObjectTypeValue.Any)]),
        ReadOnly(
            "construction_operations.make_point_name_ensure_unique",
            "Make a Point Name - Ensure Unique",
            "MakePointNameEnsureUnique",
            [
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("use_number_suffix", "Use Number Suffix?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)
            ],
            [new("resultant_point_name", "Point Name", WorkerMpValueKind.PointName, "GetPointNameArg", "—", false)]),
        ReadOnly(
            "construction_operations.make_relationship_ref_list_runtime_select",
            "Make a Relationship Reference List- Runtime Select",
            "MakeRelationshipRefListRuntimeSelect",
            [new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [new("resultant_relationship_ref_list", "Resultant Relationship Reference List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)]),
        ReadOnly(
            "construction_operations.make_relationship_ref_list_wildcard_selection",
            "Make a Relationship Reference List- WildCard Selection",
            "MakeRelationshipRefListWildcardSelection",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("relationship_wildcard_criteria", "Relationship Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false)
            ],
            [new("resultant_relationship_ref_list", "Resultant Relationship Reference List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, ItemTypeWhenOmitted: WorkerItemTypeValue.Relationship)]),
        SetCalloutProperties(
            "construction_operations.set_default_callout_view_properties",
            "Set Default Callout View Properties",
            "SetDefaultCalloutViewProperties",
            [new("default_callout_view_name", "Default Callout View Name", WorkerMpValueKind.Text, "SetStringArg", "Callout 1", false)]),
        SetCalloutProperties(
            "construction_operations.set_callout_view_properties",
            "Set Callout View Properties",
            "SetCalloutViewProperties",
            [new("callout_views", "Callout View List", WorkerMpValueKind.CollectionItemNameList, "SetCollectionObjectNameRefListArg", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.CalloutView)]),
        Mutating(
            "construction_operations.construct_polygonized_surface_from_point_clouds",
            "Construct Polygonized Surface from Point Clouds",
            "ConstructPolygonizedSurfaceFromPointClouds",
            [
                new("point_cloud_list", "Point Cloud List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true, WorkerObjectTypeValue.Cloud),
                new("mesh_orientation", "Mesh Orientation", WorkerMpValueKind.Text, "SetStringArg", "Required", true, EnumTextValues: ["Use Current Point of View", "Use Current Working Frame"]),
                new("grid_resolution", "Grid Resolution", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("polygonized_surface_name", "Polygonized Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.ScanStripeMesh)
            ]),
        Mutating(
            "construction_operations.construct_scale_bar",
            "Construct Scale Bar",
            "ConstructScaleBar",
            [
                new("scale_bar_name", "Scale Bar Name", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.ScaleBar),
                new("begin_target", "Begin Target", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("end_target", "End Target", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("length", "Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("uncertainty", "Uncertainty", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("use_relative_tolerances", "Use Relative Tolerances?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("use_high_tolerances", "Use High Tolerances?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("use_low_tolerances", "Use Low Tolerances?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("high_tolerance", "High Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false),
                new("low_tolerance", "Low Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0", false)
            ])
    ];

    private static MpOperationContract Gradient(
        string operationId,
        string step,
        string rpc,
        IReadOnlyList<MpArgumentContract> inputs) =>
        ReadOnly(
            operationId,
            step,
            rpc,
            inputs,
            [
                new("gradient", "Projected Point", WorkerMpValueKind.Vector, "GetVectorArg", "—", false, NestedFieldName: "projected_point"),
                new("gradient", "Normal Vector", WorkerMpValueKind.Vector, "GetVectorArg", "—", false, NestedFieldName: "normal_vector"),
                new("gradient", "U Direction", WorkerMpValueKind.Vector, "GetVectorArg", "—", false, NestedFieldName: "u_direction"),
                new("gradient", "V Direction", WorkerMpValueKind.Vector, "GetVectorArg", "—", false, NestedFieldName: "v_direction")
            ]);

    private static MpOperationContract SetCalloutProperties(
        string operationId,
        string step,
        string rpc,
        IReadOnlyList<MpArgumentContract> leadingInputs) =>
        Mutating(
            operationId,
            step,
            rpc,
            [
                .. leadingInputs,
                new("properties", "Lock View Point?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false, NestedFieldName: "lock_view_point"),
                new("properties", "Recall Working Frame?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false, NestedFieldName: "recall_working_frame"),
                new("properties", "Recall Visible Layer?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false, NestedFieldName: "recall_visible_layer"),
                new("properties", "Callout Leader Thickness", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "2", false, NestedFieldName: "callout_leader_thickness"),
                new("properties", "Callout Leader Color", WorkerMpValueKind.RgbColor, "SetColorArg", "128,128,128", false, NestedFieldName: "callout_leader_color"),
                new("properties", "Callout Border Thickness", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "2", false, NestedFieldName: "callout_border_thickness"),
                new("properties", "Callout Border Color", WorkerMpValueKind.RgbColor, "SetColorArg", "0,0,255", false, NestedFieldName: "callout_border_color"),
                new("properties", "Divide Text with Lines?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false, NestedFieldName: "divide_text_with_lines"),
                new("properties", "Font", WorkerMpValueKind.Font, "SetFontTypeArg", "Message defaults", false, NestedFieldName: "font")
            ]);
}
