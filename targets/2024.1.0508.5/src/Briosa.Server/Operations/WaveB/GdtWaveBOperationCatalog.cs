using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveB;

internal static class GdtWaveBOperationCatalog
{
    private const string Service = "briosa.GdtOperations";

    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        Mutating(
            "gdt_operations.datum_alignment",
            "Datum Alignment",
            "DatumAlignment",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true),
                new("objects_to_move", "Objects to Move", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("instruments_to_move", "Instruments to Move", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true),
                new("apply_feature_check_transform", "Apply Feature Check Transform?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "gdt_operations.delete_feature_checks",
            "Delete Feature Checks",
            "DeleteFeatureChecks",
            [
                new("feature_checks", "Feature Check Name List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)], riskFlags: ["destructive"]),
        Mutating(
            "gdt_operations.enable_disable_datum_alignment_for_feature_check",
            "Enable/Disable Datum Alignment for Feature Check",
            "EnableDisableDatumAlignmentForFeatureCheck",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.FeatureCheck),
                new("enable_datum_alignment", "Enable Datum Alignment?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_custom_initial_alignment", "Enable Custom Initial Alignment?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("enable_initial_datum_alignment", "Enable Initial Datum Alignment?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("alignment", "Alignment", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Alignment)]),
        Mutating(
            "gdt_operations.evaluate_feature_check",
            "Evaluate Feature Check",
            "EvaluateFeatureCheck",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.FeatureCheck),
                new("perform_evaluation", "Perform Evaluation?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("simultaneous_evaluation", "Simultaneous Evaluation?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("check_evaluated", "Check Evaluated?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("check_result", "Check Result", WorkerMpValueKind.Text, "GetStringArg", "—", false),
                new("non_unique_result", "Non-unique Result?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("measured_deviation_upper", "Measured Deviation (Upper)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("distance_out_of_tolerance_upper", "Distance Out of Tolerance (Upper)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("eval_delta_transform_upper", "Eval Delta Transform (Upper)", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg", "—", false),
                new("measured_deviation_lower", "Measured Deviation (Lower)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("distance_out_of_tolerance_lower", "Distance Out of Tolerance (Lower)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("eval_delta_transform_lower", "Eval Delta Transform (Lower)", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg", "—", false),
                new("check_type", "Check Type", WorkerMpValueKind.Text, "GetStringArg", "—", false),
                new("tolerance_type", "Tolerance Type", WorkerMpValueKind.Text, "GetStringArg", "—", false),
                new("tolerance_simple", "Tolerance, Simple", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("tolerance_composite_upper", "Tolerance, Composite (Upper)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("tolerance_composite_lower", "Tolerance, Composite (Lower)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("tolerance_range_min", "Tolerance, Range (Min)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("tolerance_range_max", "Tolerance, Range (Max)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("tolerance_nominal_plus_minus_nominal", "Tolerance, NominalPlusMinus (Nominal)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("tolerance_nominal_plus_minus_minus", "Tolerance, NominalPlusMinus (Minus)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("tolerance_nominal_plus_minus_plus", "Tolerance, NominalPlusMinus (Plus)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "gdt_operations.evaluate_feature_checks",
            "Evaluate Feature Checks",
            "EvaluateFeatureChecks",
            [
                new("feature_check_list", "Feature Check List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("simultaneous_evaluation", "Simultaneous Evaluation?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("restrict_evaluations_to_listed_checks", "Restrict Evaluations To Listed Checks?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("total_passed", "Total Passed", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("total_failed", "Total Failed", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("total_incomplete", "Total Incomplete", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        Mutating(
            "gdt_operations.feature_inspection_auto_filter",
            "Feature Inspection Auto Filter",
            "FeatureInspectionAutoFilter",
            [
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_names", "Group Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("surface_offset", "Surface Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("edge_offset", "Edge Offset", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("offset_direction", "Offset Direction", WorkerMpValueKind.OffsetDirectionType, "SetOffsetDirectionTypeArg", "Required", true),
                new("include_points_within_cylinder_axis_proximity", "Include Pts within Cylinder Axis Proximity?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("enforce_max_points_per_face_in_output", "Enforce Max Pts per Face in Output?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("max_points_per_face", "Max Pts per Face", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("feature_check_name_list", "Feature Check Name List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("include_datums", "Include Datums?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("create_cloud_for_each_datum_or_check", "Create Cloud for Each Datum/Check", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "gdt_operations.generate_feature_check_summary",
            "Generate Feature Check Summary",
            "GenerateFeatureCheckSummary",
            [
                new("feature_check_list", "Feature Check List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("summary_table_name", "Summary Table Name", WorkerMpValueKind.Text, "SetStringArg", "GDT Feature Check Summary", false)]),
        ReadOnly(
            "gdt_operations.get_datum_measurements",
            "Get Datum Measurements",
            "GetDatumMeasurements",
            [
                new("datum", "Datum", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true)],
            [
                new("measurements", "Point Names", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false, null, null, false, "point_names"),
                new("measurements", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "cloud_names")]),
        ReadOnly(
            "gdt_operations.get_feature_check_cylinder_eval_options",
            "Get Feature Check Cylinder Eval Options",
            "GetFeatureCheckCylinderEvalOptions",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true)],
            [
                new("options", "Enable Actual Diameter Override", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "enable_actual_diameter_override"),
                new("options", "Actual Diameter Override", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "actual_diameter_override")]),
        ReadOnly(
            "gdt_operations.get_feature_check_datum_references",
            "Get Feature Check Datum References",
            "GetFeatureCheckDatumReferences",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.FeatureCheck)],
            [
                new("datum_1", "Datum 1 Reference String", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "reference_string"),
                new("datum_1", "Datum 1 CAD Faces", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "cad_faces"),
                new("datum_1", "Datum 1 SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "sa_objects"),
                new("datum_1", "Datum 1 Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "auxiliary_sa_objects"),
                new("datum_1", "Datum 1 Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "geometry_relationships"),
                new("datum_1", "Datum 1 Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "auxiliary_geometry_relationships"),
                new("datum_2", "Datum 2 Reference String", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "reference_string"),
                new("datum_2", "Datum 2 CAD Faces", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "cad_faces"),
                new("datum_2", "Datum 2 SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "sa_objects"),
                new("datum_2", "Datum 2 Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "auxiliary_sa_objects"),
                new("datum_2", "Datum 2 Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "geometry_relationships"),
                new("datum_2", "Datum 2 Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "auxiliary_geometry_relationships"),
                new("datum_3", "Datum 3 Reference String", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "reference_string"),
                new("datum_3", "Datum 3 CAD Faces", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "cad_faces"),
                new("datum_3", "Datum 3 SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "sa_objects"),
                new("datum_3", "Datum 3 Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "auxiliary_sa_objects"),
                new("datum_3", "Datum 3 Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "geometry_relationships"),
                new("datum_3", "Datum 3 Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "auxiliary_geometry_relationships")]),
        ReadOnly(
            "gdt_operations.get_feature_check_measurements",
            "Get Feature Check Measurements",
            "GetFeatureCheckMeasurements",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true)],
            [
                new("measurements", "Point Names", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false, null, null, false, "point_names"),
                new("measurements", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "cloud_names")]),
        ReadOnly(
            "gdt_operations.get_feature_check_reporting_frame",
            "Get Feature Check Reporting Frame",
            "GetFeatureCheckReportingFrame",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.FeatureCheck)],
            [
                new("reporting_frame", "Reporting Frame", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg", "—", false)]),
        ReadOnly(
            "gdt_operations.make_annotation_ref_list_from_collection",
            "Make Annotation Ref List from a Collection",
            "MakeAnnotationRefListFromCollection",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)],
            [
                new("annotations", "Resultant Annotation Reference List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "gdt_operations.make_annotation_ref_list_wildcard_selection",
            "Make Annotation Ref List- WildCard Selection",
            "MakeAnnotationRefListWildcardSelection",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("annotation_wildcard_criteria", "Annotation Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false)],
            [
                new("annotations", "Resultant Annotation Reference List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "gdt_operations.make_datum_ref_list_from_collection",
            "Make a Datum Ref List from a Collection",
            "MakeDatumRefListFromCollection",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)],
            [
                new("datums", "Datum Ref List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "gdt_operations.make_feature_check_ref_list_from_collection",
            "Make a Feature Check Ref List from a Collection",
            "MakeFeatureCheckRefListFromCollection",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)],
            [
                new("feature_checks", "Feature Check Ref List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "gdt_operations.make_feature_check_reference_list_wildcard_selection",
            "Make a Feature Check Reference List- WildCard Selection",
            "MakeFeatureCheckReferenceListWildcardSelection",
            [
                new("collection_wildcard_criteria", "Collection Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false),
                new("feature_check_wildcard_criteria", "Feature Check Wildcard Criteria", WorkerMpValueKind.Text, "SetStringArg", "*", false)],
            [
                new("feature_checks", "Resultant Feature Check Reference List", WorkerMpValueKind.CollectionItemNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        Mutating(
            "gdt_operations.make_feature_checks",
            "Make Feature Checks",
            "MakeFeatureChecks",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "gdt_operations.make_gdt_datum_annotation",
            "Make GD&T Datum Annotation",
            "MakeGdtDatumAnnotation",
            [
                new("datum_name", "Datum Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("geometry_relationships", "Geometry Relationships", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("surface_faces", "Surface Faces", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("auxiliary_object", "Auxiliary Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("auxiliary_geometry_relationship", "Auxiliary Geometry Relationship", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Relationship),
                new("is_slot", "Is Slot?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("force_surface_feature", "Force Surface Feature?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "gdt_operations.make_gdt_feature_check_annotation",
            "Make GD&T Feature Check Annotation",
            "MakeGdtFeatureCheckAnnotation",
            [
                new("feature_annotation_name", "Feature Annotation Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("feature_type", "Feature Type", WorkerMpValueKind.Text, "SetStringArg", "True Position", false, null, ["Diameter", "Radius", "Distance Between", "Width", "Length", "Angle Between", "Angularity", "Perpendicularity", "Parallelism", "Circularity", "Concentricity", "Cylindricity", "Straightness", "Surface Profile", "Line Profile", "Composite Surface Profile", "Flatness", "True Position", "Composite True Position", "Circular Runout", "Total Runout"]),
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("geometry_relationships", "Geometry Relationships", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("surface_faces", "Surface Faces", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("decompose_multiple_features", "Decompose Multiple Features?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("auto_create_diameter_checks", "Auto Create Diameter Checks?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("auto_create_slot_width_checks", "Auto Create Slot Width Checks?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("auto_create_slot_length_checks", "Auto Create Slot Length Checks?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("datum_references", "Datum References", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("tolerance", "Tolerance", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("is_slot", "Is Slot?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("per_unit_length_or_area", "Per unit length/area", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("circular_area", "Circular area? (Rectangular default)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("per_unit_area_length_distance", "Per unit (area) length distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("per_unit_area_length_step_over_percent", "Per unit (area) length step over %", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "50.000000", false),
                new("per_unit_area_width_distance", "Per unit area width distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("per_unit_area_width_step_over_percent", "Per unit area width step over %", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "50.000000", false),
                new("per_unit_area_circle_diameter", "Per unit area circle diameter", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("per_unit_area_diameter_step_over", "Per unit area diameter step over", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "50.000000", false),
                new("auxiliary_object", "Auxiliary Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("auxiliary_geometry_relationship", "Auxiliary Geometry Relationship", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.Relationship),
                new("use_nominal_for_dimension_tolerance", "Use Nominal for Dimension Tolerance", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("use_reference_object_for_nominal", "Use Reference Object for Nominal", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("nominal_dimension_tolerance", "Nominal Dimension Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("low_dimension_tolerance", "Low Dimension Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "-0.100000", false),
                new("high_dimension_tolerance", "High Dimension Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.100000", false),
                new("tolerance_zone_type", "Tolerance Zone Type", WorkerMpValueKind.Text, "SetStringArg", "None", false, null, ["None", "Cylindrical", "Planar", "Spherical", "Radial Arc", "Radial Planar", "Boundary", "Planar Median", "Surface"]),
                new("use_projected_tolerance_zone", "Use Projected Tolerance Zone?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("projected_tolerance_zone", "Projected Tolerance Zone", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        ReadOnly(
            "gdt_operations.make_surface_face_list_from_surface",
            "Make Surface Face List From Surface",
            "MakeSurfaceFaceListFromSurface",
            [
                new("surface", "Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface)],
            [
                new("surface_faces", "Selected Surface Faces", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        ReadOnly(
            "gdt_operations.make_surface_face_list_runtime_select",
            "Make Surface Face List - Runtime Select",
            "MakeSurfaceFaceListRuntimeSelect",
            [],
            [
                new("surface_faces", "Selected Surface Faces", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        Mutating(
            "gdt_operations.refresh_datums_feature_checks_from_annotations",
            "Refresh Datums/Feature Checks from Annotations",
            "RefreshDatumsFeatureChecksFromAnnotations",
            [
                new("collection", "Collection", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "gdt_operations.set_datum_measurements",
            "Set Datum Measurements",
            "SetDatumMeasurements",
            [
                new("datum", "Datum", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true),
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("replace_existing_measurements", "Replace Existing Measurements?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "gdt_operations.set_feature_check_cylinder_eval_options",
            "Set Feature Check Cylinder Eval Options",
            "SetFeatureCheckCylinderEvalOptions",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true),
                new("enable_actual_diameter_override", "Enable Actual Diameter Override", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("actual_diameter_override", "Actual Diameter Override", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "gdt_operations.set_feature_check_measurements",
            "Set Feature Check Measurements",
            "SetFeatureCheckMeasurements",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true),
                new("point_names", "Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("replace_existing_measurements", "Replace Existing Measurements?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "gdt_operations.set_feature_check_reporting_frame",
            "Set Feature Check Reporting Frame",
            "SetFeatureCheckReportingFrame",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, null, null, false, null, WorkerItemTypeValue.FeatureCheck),
                new("reporting_frame", "Reporting Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)]),
        Mutating(
            "gdt_operations.set_gdt_options",
            "Set GD&T Options",
            "SetGdtOptions",
            [
                new("use_high_points", "Use High Points", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("extrapolate_axial_extent", "Extrapolate Axial Extent", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("exclude_from_auto_evaluation", "Exclude From Auto Evaluation", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("distance_between_mode", "Distance Between Mode", WorkerMpValueKind.GdtDistanceBetweenMode, "SetMPGDTOptionsDistanceBetweenModeArg", "Required", true),
                new("evaluation_method", "Evaluation Method", WorkerMpValueKind.GdtEvaluationMethod, "SetMPGDTOptionsCheckValidatorTypeArg", "Required", true),
                new("create_actual_features", "Create Actual Features", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("create_solved_points", "Create Solved Points", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("cross_section_criteria", "Cross Section Criteria", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.039370", false),
                new("enable_auto_feature_detection", "Enable Auto Feature Detection?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "gdt_operations.set_global_force_simultaneous_evaluation",
            "Set Global Force Simultaneous Evaluation",
            "SetGlobalForceSimultaneousEvaluation",
            [
                new("global_simultaneous_evaluation", "Global Simultaneous Evaluation?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "gdt_operations.start_stop_feature_check_trapping",
            "Start/Stop Feature Check Trapping",
            "StartStopFeatureCheckTrapping",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true),
                new("instrument_id", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("start_trapping", "Start Trapping (FALSE = Stop)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        ReadOnly(
            "gdt_operations.get_feature_check_reporting_options",
            "Get Feature Check Reporting Options",
            "GetFeatureCheckReportingOptions",
            [new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.FeatureCheck)],
            [
                new("options", "Show Feature Control Frame Summary?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "show_feature_control_frame_summary"),
                new("options", "Include Title?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "include_title"),
                new("options", "Show Datum and Tolerance Summary?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "show_datum_and_tolerance_summary"),
                new("options", "Show Feature Summary?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "show_feature_summary"),
                new("options", "Show Point Details Table?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "show_point_details"),
                new("options", "Show Lower Tier Tables?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "show_lower_tier_tables")
            ]),
        ReadOnly(
            "gdt_operations.get_gdt_options",
            "Get GD&T Options",
            "GetGdtOptions",
            [],
            [
                new("options", "Use High Points", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "use_high_points"),
                new("options", "Extrapolate Axial Extent", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "extrapolate_axial_extent"),
                new("options", "Exclude From Auto Evaluation", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "exclude_from_auto_evaluation"),
                new("options", "Create Actual Features", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "create_actual_features"),
                new("options", "Create Solved Points", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "create_solved_points"),
                new("options", "Cross Section Criteria", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, NestedFieldName: "cross_section_criteria"),
                new("options", "Enable Auto Feature Detection?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, NestedFieldName: "enable_auto_feature_detection")
            ]),
        Mutating(
            "gdt_operations.set_feature_check_reporting_options",
            "Set Feature Check Reporting Options",
            "SetFeatureCheckReportingOptions",
            [
                new("feature_check", "Feature Check", WorkerMpValueKind.CollectionItemName, "SetCollectionObjectNameArg2", "Required", true, ItemTypeWhenOmitted: WorkerItemTypeValue.FeatureCheck),
                new("show_feature_control_frame_summary", "Show Feature Control Frame Summary?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("include_title", "Include Title?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_datum_and_tolerance_summary", "Show Datum and Tolerance Summary?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_feature_summary", "Show Feature Summary?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_point_details_summary", "Show Point Details Summary?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_lower_tier_tables", "Show Lower Tier Tables?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)
            ]),
    ];

    private static MpOperationContract Mutating(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract>? outputs = null, IReadOnlyList<string>? riskFlags = null, Action<IMessage>? validateRequest = null) =>
        new(operationId, step, Service, rpc, "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, riskFlags ?? [], inputs, outputs ?? [], validateRequest);

    private static MpOperationContract ReadOnly(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract> outputs) =>
        new(operationId, step, Service, rpc, "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, [], inputs, outputs);
}
