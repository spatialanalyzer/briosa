using Briosa.Server.Operations.WaveA;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Server.Operations.WaveB;

internal static class InstrumentWaveBOperationCatalog
{
    private const string Service = "briosa.InstrumentOperations";

    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        Mutating(
            "instrument_operations.run_crib_sheet",
            "Run Crib Sheet",
            "RunCribSheet",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("crib_sheet_name", "Crib Sheet Name", WorkerMpValueKind.Text, "SetStringArg", "Required", true),
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)
            ],
            riskFlags: ["instrument-control", "long-running", "at-risk-no-runtime-validation"]),
        Mutating(
            "instrument_operations.project_objects",
            "Project Objects",
            "ProjectObjects",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("objects_to_project", "Objects To Project", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)
            ],
            riskFlags: ["instrument-control", "at-risk-no-runtime-validation"]),
        Mutating(
            "instrument_operations.stop_projection",
            "Stop Projection",
            "StopProjection",
            [new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            riskFlags: ["instrument-control", "at-risk-no-runtime-validation"]),
        Mutating(
            "instrument_operations.activate_deactivate_instrument_toolbar",
            "Activate/Deactivate Instrument Toolbar",
            "ActivateDeactivateInstrumentToolbar",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("deactivate_toolbar", "Deactivate Toolbar?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.add_new_instrument",
            "Add New Instrument",
            "AddNewInstrument",
            [
                new("instrument_type", "Instrument Type", WorkerMpValueKind.InstrumentTypeName, "SetInstTypeNameArg", "Required", true)],
            [
                new("instrument_added", "Instrument Added (result)", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg", "—", false)]),
        Mutating(
            "instrument_operations.add_nominal_point_to_tcp_fixture",
            "Add Nominal Point to TCP Fixture",
            "AddNominalPointToTcpFixture",
            [
                new("tcp_fixture", "TCP Fixture", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("nominal_point_name", "Nominal Point Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("nominal_point_location", "Nominal Point Location", WorkerMpValueKind.Vector, "SetVectorArg", "Required", true),
                new("var_xx", "Var XX", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("var_yy", "Var YY", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("var_zz", "Var ZZ", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("covar_xy", "CoVar XY", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("covar_xz", "CoVar XZ", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("covar_yz", "CoVar YZ", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "instrument_operations.align_cloud_to_cad",
            "Align Cloud to CAD",
            "AlignCloudToCad",
            [
                new("cloud", "Cloud Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Cloud),
                new("surfaces", "Surfaces", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("maximum_coarse_cad_mesh_edge_length", "Maximum Coarse CAD Mesh Edge Length", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("use_fine_cad_mesh", "Use Fine CAD Mesh (50% of Coarse)?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("execute_alignment", "Execute Alignment?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)],
            [
                new("alignment", "RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "rms_deviation"),
                new("alignment", "Average Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "average_deviation"),
                new("alignment", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "maximum_absolute_deviation"),
                new("alignment", "Resultant Transform in Working Frame", WorkerMpValueKind.Transform, "GetTransformArg", "—", false, null, null, false, "resultant_transform_in_working")]),
        Mutating(
            "instrument_operations.align_laser_projector",
            "Align Laser Projector",
            "AlignLaserProjector",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("group", "Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "instrument_operations.align_two_targets_with_axis_wcf_x",
            "Align Two Targets with Axis (WCF - X)",
            "AlignTwoTargetsWithAxisWcfX",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("first_point_on_axis", "First Point On Axis", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("second_point_on_axis", "Second Point On Axis", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("initial_measured_group", "Initial Measured Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("rotational_tolerance", "Rotational Tolerance - Optional", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.associate_objects_with_instrument",
            "Associate Objects with Instrument",
            "AssociateObjectsWithInstrument",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)]),
        Mutating(
            "instrument_operations.auto_correspond_closest_point",
            "Auto-Correspond Closest Point",
            "AutoCorrespondClosestPoint",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reference_group", "Reference Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("actuals_group", "Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("wait_for_completion", "Wait for Completion?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.auto_correspond_with_proximity_trigger",
            "Auto-Correspond with Proximity Trigger",
            "AutoCorrespondWithProximityTrigger",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("nominal_group", "Nominal Point Group or Vector Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("results_group", "Results Point Group for measurements", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("point_distance_threshold", "Point distance threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.500000", false),
                new("vector_axis_threshold", "Vector axis threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.250000", false),
                new("project_results_to_nominal_vector", "Project results to nominal vector", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("warbler_ramp_start_distance", "Warbler ramp start zone distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "12.000000", false),
                new("show_watch_window", "Show Watch window on startup", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("deviation_vector_group_name", "Vector Group to make while Measuring (blank means ignore)", WorkerMpValueKind.VectorGroupName, "SetVectorGroupNameArg", "Required", true),
                new("make_unmeasured_group", "Make unmeasured group when done", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("measure_each_point_only_once", "Measure each point only once", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.auto_measure_batch_of_features",
            "Auto-Measure Batch of Features",
            "AutoMeasureBatchOfFeatures",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("features", "Feature List", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("wait_for_complete", "Wait for Complete", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.auto_measure_points",
            "Auto Measure Points",
            "AutoMeasurePoints",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reference_group", "Reference Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("actuals_group", "Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("force_existing_group", "Force use of existing group?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_complete_dialog", "Show complete dialog?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("wait_for_completion", "Wait for Completion?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("auto_start", "Auto Start?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.auto_measure_specified_geometry",
            "Auto-Measure Specified Geometry",
            "AutoMeasureSpecifiedGeometry",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("geometry", "Object", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Any),
                new("mode_profile", "Mode/Profile", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("wait_for_complete", "Wait for Complete", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.auto_measure_surface_vector_intersections",
            "Auto-Measure Surface Vector Intersections",
            "AutoMeasureSurfaceVectorIntersections",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("vector_group", "Vector Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("resultant_group", "Resultant Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("wait_for_complete", "Wait for Complete", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.auto_measure_vectors",
            "Auto-Measure Vectors",
            "AutoMeasureVectors",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("vector_group", "Vector Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.VectorGroup),
                new("actuals_group", "Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("project_point_to_vector", "Project Point to Vector", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("angle_tolerance", "Angle Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("high_tolerance", "High Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("low_tolerance", "Low Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "instrument_operations.build_target",
            "'Build' Target",
            "BuildTarget",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("output_target_name", "Output Target Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("nominal_point", "Nominal Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("tolerance", "Tolerance", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Required", true),
                new("html_prompt_file", "HTML Prompt File (optional)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        ReadOnly(
            "instrument_operations.calculate_tcp_fixture_uncertainties",
            "Calculate TCP Fixture Uncertainties",
            "CalculateTcpFixtureUncertainties",
            [
                new("tcp_fixture", "TCP Fixture", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("tcp_in_working", "TCP In Working Frame", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true),
                new("tcp_measurements", "TCP Measurements", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true)],
            [
                new("uncertainties", "Solution Valid", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "solution_valid"),
                new("uncertainties", "Refined TCP In Working Frame", WorkerMpValueKind.Transform, "GetTransformArg", "—", false, null, null, false, "refined_tcp_in_working"),
                new("uncertainties", "Uncertainties in TCP Fixture Frame", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "uncertainties_in_tcp_fixture_frame"),
                new("uncertainties", "Uncertainties in Working Frame", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "uncertainties_in_working_frame"),
                new("uncertainties", "RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "rms_error"),
                new("uncertainties", "MAX Abs Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "maximum_absolute_error"),
                new("uncertainties", "Goodness Of Fit", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "goodness_of_fit"),
                new("uncertainties", "Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "robustness"),
                new("uncertainties", "Result Notes", WorkerMpValueKind.EditText, "GetEditTextArg", "—", false, null, null, false, "result_notes")]),
        Mutating(
            "instrument_operations.clear_cloud_viewer",
            "Clear Cloud Viewer",
            "ClearCloudViewer",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "instrument_operations.close_auto_correspond_closest_point_dialog",
            "Close Auto-Correspond Closest Point Dialog",
            "CloseAutoCorrespondClosestPointDialog",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "instrument_operations.collimation",
            "Collimation",
            "Collimation",
            [
                new("stationary_instrument", "Stationary Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("moving_instrument", "Moving Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("collimation_point", "Collimation Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("zero_moving_instrument", "Zero Moving Instrument", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("tilt_mode", "Collimation Tilt Mode", WorkerMpValueKind.CollimationType, "SetCollimationTypeArg", "Required", true),
                new("baseline_method", "Collimation Baseline Mode", WorkerMpValueKind.CollimationBaselineType, "SetCollimationBaselineTypeArg", "Required", true),
                new("baseline_distance", "Baseline Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("scale_point_1", "Scale Point 1", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("scale_point_2", "Scale Point 2", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("not_measured_by_moving_instrument", "Not Measured By Moving Instrument", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("as_measured_by_moving_instrument", "As Measured By Moving Instrument", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "instrument_operations.combine_point_groups",
            "Combine Point Groups",
            "CombinePointGroups",
            [
                new("groups_to_combine", "Groups to Combine", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("combined_point_group", "Combined Point Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup)]),
        Mutating(
            "instrument_operations.compute_cte_scale_factor",
            "Compute CTE Scale Factor",
            "ComputeCteScaleFactor",
            [
                new("material_cte", "Material CTE (1/Deg F)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("initial_temperature", "Initial Temperature (F)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("final_temperature", "Final Temperature (F)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("scale_factor", "Scale Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "instrument_operations.configure_and_measure",
            "Configure and Measure",
            "ConfigureAndMeasure",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("target", "Target Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("measurement_mode", "Measurement Mode", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("measure_immediately", "Measure Immediately", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("wait_for_completion", "Wait for Completion", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("timeout_seconds", "Timeout in Seconds", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "instrument_operations.construct_measured_point_uncertainty_ellipsoids",
            "Construct Measured Point Uncertainty Ellipsoids",
            "ConstructMeasuredPointUncertaintyEllipsoids",
            [
                new("measurements", "Measurements", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true)]),
        Mutating(
            "instrument_operations.construct_mirror_from_plane",
            "Construct Mirror from Plane",
            "ConstructMirrorFromPlane",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("mirror_name", "Mirror Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("plane", "Plane", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)]),
        Mutating(
            "instrument_operations.construct_mirror_from_two_points",
            "Construct Mirror from Two Points",
            "ConstructMirrorFromTwoPoints",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("mirror_name", "Mirror Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_measured_directly", "Point Measured Directly", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("point_measured_through_mirror", "Point Measured Through Mirror", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("send_mirror_to_instrument", "Send Mirror to Instrument?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)],
            [
                new("mirror_plane", "Mirror Plane", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg", "—", false)]),
        Mutating(
            "instrument_operations.construct_perimeters_from_surface_face_list",
            "Construct Perimeters from Surface Face List",
            "ConstructPerimetersFromSurfaceFaceList",
            [
                new("surface_faces", "Selected Surface Faces", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("perimeters", "Scan perimeter list", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "scan_perimeters"),
                new("perimeters", "Exclusion perimeter list", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false, null, null, false, "exclusion_perimeters")]),
        Mutating(
            "instrument_operations.construct_tcp_fixture",
            "Construct TCP Fixture",
            "ConstructTcpFixture",
            [
                new("requested_tcp_fixture", "Requested TCP Fixture", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("point_match_threshold", "Point Match Threshold", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("replace_existing_tcp_fixture", "Replace Existing TCP Fixture", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("resulting_tcp_fixture", "Resulting TCP Fixture", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg", "—", false)]),
        Mutating(
            "instrument_operations.create_new_dynamic_reference",
            "Create New Dynamic Reference",
            "CreateNewDynamicReference",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("points_defining_dynamic_reference", "Points defining Dynamic Reference", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("dynamic_reference_name", "Dynamic Reference Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.create_templated_instrument_usmn",
            "Create Templated Instrument (USMN)",
            "CreateTemplatedInstrumentUsmn",
            [
                new("instrument_template_name", "Instrument Template Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("overall_instrument_weight", "Overal Instrument Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("moving", "Moving", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_x", "Enable X", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_y", "Enable Y", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_z", "Enable Z", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_rx", "Enable Rx", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_ry", "Enable Ry", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_rz", "Enable Rz", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("enable_scale", "Enable Scale", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("enable_component_weights", "Enable Component Weights", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("component_1_weight", "Component 1 (Azimuth) Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("component_2_weight", "Component 2 (Elevation) Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("component_3_weight", "Component 3 (Distance) Weight", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false)]),
        Mutating(
            "instrument_operations.delete_instrument",
            "Delete Instrument",
            "DeleteInstrument",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("prompt_user_to_confirm", "Prompt user to confirm?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("keep_resulting_points", "Keep resulting points?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)], riskFlags: ["destructive"]),
        Mutating(
            "instrument_operations.delete_measurement_observation",
            "Delete Measurement Observation",
            "DeleteMeasurementObservation",
            [
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("observation_index", "Observation index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("delete_point_if_no_measurements_remain", "Delete point if no measurements remain?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)], riskFlags: ["destructive"]),
        Mutating(
            "instrument_operations.delete_measurements",
            "Delete Measurements",
            "DeleteMeasurements",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("point_name", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("delete_point_if_no_measurements_remain", "Delete point if no measurements remain?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)], riskFlags: ["destructive"]),
        Mutating(
            "instrument_operations.disassociate_objects_from_instrument",
            "Disassociate Objects from Instrument",
            "DisassociateObjectsFromInstrument",
            [
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true)]),
        Mutating(
            "instrument_operations.dissect_point_group",
            "Dissect Point Group",
            "DissectPointGroup",
            [
                new("group_to_dissect", "Group to Dissect", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("base_name_for_dissected_groups", "Base Name for Disected Groups", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.dock_instrument_interface",
            "Dock Instrument Interface",
            "DockInstrumentInterface",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("dock_interface", "Dock Interface?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.drift_check",
            "Drift Check",
            "DriftCheck",
            [
                new("instrument", "Instrument to check", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reference_group", "Reference Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("actuals_group", "Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("tolerance", "Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("minimum_point_count", "Minimum point count", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("use_closest_reference_point", "Use Closest Reference Point", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)],
            [
                new("maximum_error", "Max error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("rms_error", "RMS error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("instrument_added", "Instrument added?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("new_instrument", "New Instrument", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg", "—", false)]),
        Mutating(
            "instrument_operations.edge_scan_measurement",
            "Edge Scan Measurement",
            "EdgeScanMeasurement",
            [
                new("instrument", "Instrument to scan", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("point_near_edge", "Point near edge", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("edge_search_direction_point", "Point in edge search direction", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("parameter_set_name", "Parameter set name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_group", "Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("target_name", "Target Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.enable_disable_frame_set_scan_mode_all_instruments",
            "Enable/Disable Frame Set Scan Mode (All Instruments)",
            "EnableDisableFrameSetScanModeAllInstruments",
            [
                new("enable_frame_set_scan_mode", "Enable Frame Set Scan Mode", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.enable_disable_frame_set_scan_mode_by_instrument",
            "Enable/Disable Frame Set Scan Mode (By Instrument)",
            "EnableDisableFrameSetScanModeByInstrument",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("enable_frame_set_scan_mode", "Enable Frame Set Scan Mode", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.enable_disable_point_set_scan_mode",
            "Enable/Disable Point Set Scan Mode",
            "EnableDisablePointSetScanMode",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("enable_point_set_scan_mode", "Enable Point Set Scan Mode", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.export_instrument_history_to_xml_file",
            "Export Instrument History to XML File",
            "ExportInstrumentHistoryToXmlFile",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("file_path", "File Path", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.fabricate_observations",
            "Fabricate Observations",
            "FabricateObservations",
            [
                new("instrument", "Instrument to shoot", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("point_group", "Group name to shoot", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("introduce_instrument_error", "Introduce instrument error?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("limit_distance", "Limit Distance?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("minimum_distance", "Min Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_distance", "Max Distance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1000000.000000", false)]),
        ReadOnly(
            "instrument_operations.get_current_instrument_position_update",
            "Get Current Instrument Position Update",
            "GetCurrentInstrumentPositionUpdate",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reporting_frame", "Reporting Frame", WorkerMpValueKind.Text, "SetStringArg", "Instrument Base", false, null, ["Instrument Base", "World", "Working"]),
                new("polar_coordinates", "Polar Coordinates?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("x_or_r", "X / R", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y_or_theta", "Y / Theta (Degrees)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z_or_phi", "Z / Phi (Degrees)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("time_since_update", "Time Since Update (sec)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("timestamp", "Timestamp (Approximate)", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_current_trapping_status",
            "Get Current Trapping Status",
            "GetCurrentTrappingStatus",
            [],
            [
                new("status", "Trapping Active?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "active"),
                new("status", "Relationship / Feature Check Name", WorkerMpValueKind.CollectionItemName, "GetCollectionObjectNameArg", "—", false, null, null, false, "focused_item"),
                new("status", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg", "—", false, null, null, false, "instrument")]),
        ReadOnly(
            "instrument_operations.get_estimated_scan_time",
            "Get Estimated Scan Time",
            "GetEstimatedScanTime",
            [
                new("instrument", "Instrument", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("profile_name", "Profile name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("estimated_scan_time", "Estimated Scan Time", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world",
            "Get Instrument Base Uncertainty Covariance Matrix WRT WORLD",
            "GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("covariance_matrix", "Covar Row 1", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_1"),
                new("covariance_matrix", "Covar Row 2", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_2"),
                new("covariance_matrix", "Covar Row 3", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_3"),
                new("covariance_matrix", "Covar Row 4", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_4"),
                new("covariance_matrix", "Covar Row 5", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_5"),
                new("covariance_matrix", "Covar Row 6", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_6")]),
        ReadOnly(
            "instrument_operations.get_instrument_id_from_name",
            "Get Instrument ID from Name",
            "GetInstrumentIdFromName",
            [
                new("name", "Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)],
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_interface_response_timeout",
            "Get Instrument Interface Response Timeout",
            "GetInstrumentInterfaceResponseTimeout",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("timeout", "Resulting Timeout Value (secs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_model",
            "Get Instrument Model",
            "GetInstrumentModel",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("name", "Name", WorkerMpValueKind.Text, "GetStringArg", "—", false),
                new("model", "Model", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_part_temperature",
            "Get Instrument Part Temperature",
            "GetInstrumentPartTemperature",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("part_temperature", "Part Temperature (F)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_scale_factor",
            "Get Instrument Scale Factor",
            "GetInstrumentScaleFactor",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("scale_factor", "Scale Factor", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_target_status",
            "Get Instrument Target Status",
            "GetInstrumentTargetStatus",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("status", "Is Locked?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "is_locked"),
                new("status", "Name", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "name"),
                new("status", "Number of Faces", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "number_of_faces"),
                new("status", "Locked Face", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "locked_face")]),
        ReadOnly(
            "instrument_operations.get_instrument_targets_and_mode_profiles",
            "Get Instrument Targets and Mode/Profiles",
            "GetInstrumentTargetsAndModeProfiles",
            [
                new("instrument", "Instrument to set", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("mode_profiles", "Mode/Profile", WorkerMpValueKind.StringList, "GetStringRefListArg", "—", false),
                new("target_names", "Target Names", WorkerMpValueKind.StringList, "GetStringRefListArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_transform",
            "Get Instrument Transform",
            "GetInstrumentTransform",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reference_frame", "Reference Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)],
            [
                new("transform", "Transform", WorkerMpValueKind.Transform, "GetTransformArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instrument_weather_setting",
            "Get Instrument Weather Setting",
            "GetInstrumentWeatherSetting",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("temperature", "Temperature (F)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("pressure", "Pressure (mmHg)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("relative_humidity", "Humidity (%Rel)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("set_automatically", "Was Set Automatically? (using Inst or external sensor", WorkerMpValueKind.Logical, "GetBoolArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_instruments_with_observations_on_target",
            "Get Instruments with Observations on Target",
            "GetInstrumentsWithObservationsOnTarget",
            [
                new("point", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)],
            [
                new("instruments", "Resultant Collection Instrument Reference List", WorkerMpValueKind.CollectionInstrumentIdList, "GetColInstIdRefListArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_last_instrument_index",
            "Get Last Instrument Index",
            "GetLastInstrumentIndex",
            [],
            [
                new("instrument_index", "Instrument ID", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix",
            "Get Last Solved TCP Fixture Uncertainty Covariance Matrix",
            "GetLastSolvedTcpFixtureUncertaintyCovarianceMatrix",
            [
                new("tcp_fixture", "TCP Fixture", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true)],
            [
                new("covariance_matrix", "Covar Row 1", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_1"),
                new("covariance_matrix", "Covar Row 2", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_2"),
                new("covariance_matrix", "Covar Row 3", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_3"),
                new("covariance_matrix", "Covar Row 4", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_4"),
                new("covariance_matrix", "Covar Row 5", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_5"),
                new("covariance_matrix", "Covar Row 6", WorkerMpValueKind.DoubleArray, "GetDoubleArrayArg", "—", false, null, null, false, "row_6")]),
        ReadOnly(
            "instrument_operations.get_number_of_observations_on_target",
            "Get Number of Observations on Target",
            "GetNumberOfObservationsOnTarget",
            [
                new("point", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)],
            [
                new("observation_count", "Number of Shots", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_obscured_points_from_instrument",
            "Get Obscured Points from Instrument",
            "GetObscuredPointsFromInstrument",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("candidate_points", "Candidate Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("show_obscured_shots", "Show Obscured Shots", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)],
            [
                new("obscured_points", "Obscured Points", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_observation_info",
            "Get Observation Info",
            "GetObservationInfo",
            [
                new("point", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("observation_index", "Observation Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)],
            [
                new("observation", "Resulting Instrument", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg", "—", false, null, null, false, "instrument"),
                new("observation", "Resultant Vector", WorkerMpValueKind.Vector, "GetVectorArg", "—", false, null, null, false, "spherical_values"),
                new("observation", "Active?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "active"),
                new("observation", "Timestamp", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "timestamp"),
                new("observation", "RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "rms_error"),
                new("observation", "Temperature (deg F)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "temperature"),
                new("observation", "Pressure (in. Hg)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "pressure"),
                new("observation", "Humidity (% RH)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "relative_humidity"),
                new("observation", "Info Data", WorkerMpValueKind.Text, "GetStringArg", "—", false, null, null, false, "info_data")]),
        ReadOnly(
            "instrument_operations.get_pcmm_instrument_xyz_uncertainties",
            "Get PCMM Instrument XYZ Uncertainties",
            "GetPcmmInstrumentXyzUncertainties",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("x_uncertainty", "X Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y_uncertainty", "Y Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z_uncertainty", "Z Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_targets_measured_by_instrument",
            "Get Targets Measured by Instrument",
            "GetTargetsMeasuredByInstrument",
            [
                new("instrument", "Measuring Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("targets", "Points Measured by Instrument", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_tracker_edm_theodolite_uncertainties",
            "Get Tracker/EDM Theodolite Uncertainties",
            "GetTrackerEdmTheodoliteUncertainties",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("theta_dispersion", "Theta Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("theta_threshold", "Theta Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("phi_dispersion", "Phi Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("phi_threshold", "Phi Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("distance", "Distance (PPM)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("distance_threshold", "Distance Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        ReadOnly(
            "instrument_operations.get_wrtl_channel_and_status",
            "Get WRTL Channel and Status",
            "GetWrtlChannelAndStatus",
            [
                new("instrument", "Instrument", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("status", "Connection Status", WorkerMpValueKind.Logical, "GetBoolArg", "—", false, null, null, false, "connection_status"),
                new("status", "Active Channel", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "active_channel")]),
        ReadOnly(
            "instrument_operations.get_xyz_instrument_uncertainties",
            "Get XYZ Instrument Uncertainties",
            "GetXyzInstrumentUncertainties",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("x_uncertainty", "X Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y_uncertainty", "Y Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z_uncertainty", "Z Uncertainty", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "instrument_operations.guide_objects_in_6d_based_on_point_measurements",
            "Guide Objects in 6D based on Point Measurements",
            "GuideObjectsIn6dBasedOnPointMeasurements",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("destination_group", "Destination Group (goal)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("moving_reference_group", "Moving Reference Group (attached to objects)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("objects_to_move", "Objects to Move", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("initial_survey_group", "Initially surveyed Group (First Position Measurements - Optional)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Omitted", false, WorkerObjectTypeValue.PointGroup, null, true),
                new("positional_tolerance", "Positional Tolerance - Optional", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Omitted", false, null, null, true),
                new("rotational_tolerance", "Rotational Tolerance - Optional", WorkerMpValueKind.ToleranceVectorOptions, "SetToleranceVectorOptionsArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.initiate_servo_guide",
            "Initiate Servo-Guide",
            "InitiateServoGuide",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("nominal_points", "Nominal Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true),
                new("group_name_suffix", "Group name suffix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("target_name_suffix", "Target name suffix", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("tolerance", "Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "instrument_operations.instrument_operational_check",
            "Instrument Operational Check",
            "InstrumentOperationalCheck",
            [
                new("instrument", "Instrument to Check", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("check_type", "Check Type", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.issue_instrument_actuator_command",
            "Issue Instrument Actuator Command",
            "IssueInstrumentActuatorCommand",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("command", "Command", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.jump_instrument_to_new_location",
            "Jump Instrument To New Location",
            "JumpInstrumentToNewLocation",
            [
                new("live_instrument", "Live Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("hide_previous_instrument", "Hide the Previous Instrument?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.load_cloud_viewer_point_cloud_file",
            "Load Point Cloud File",
            "LoadCloudViewerPointCloudFile",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("file_path", "File Path", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.load_instrument_configuration",
            "Load Instrument Configuration",
            "LoadInstrumentConfiguration",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("configuration_file", "Configuration File", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.locate_instrument_best_fit_group_to_group",
            "Locate Instrument (Best Fit - Group to Group)",
            "LocateInstrumentBestFitGroupToGroup",
            [
                new("reference_group", "Reference Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("corresponding_group", "Corresponding Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("show_interface", "Show Interface", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("rms_tolerance", "RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_absolute_tolerance", "Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("allow_scale", "Allow Scale", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("allow_x", "Allow X", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_y", "Allow Y", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_z", "Allow Z", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_rx", "Allow Rx", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_ry", "Allow Ry", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_rz", "Allow Rz", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("lock_degrees_of_freedom", "Lock Degrees of Freedom", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("generate_event", "Generate Event", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("csv_report", "File Path for CSV Text Report (requires Show Interface = TRUE)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)],
            [
                new("transform_in_working", "Transform in Working", WorkerMpValueKind.Transform, "GetTransformArg", "—", false),
                new("optimum_transform", "Optimum Transform", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg", "—", false),
                new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("maximum_absolute_deviation", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("number_of_unknowns", "Number of Unknowns", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("number_of_equations", "Number of Equations", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("robustness", "Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "instrument_operations.locate_instrument_best_fit_nominal_geometry",
            "Locate Instrument (Best Fit - Nominal Geometry)",
            "LocateInstrumentBestFitNominalGeometry",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("geometry_relationships", "Geometry Relationships", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("show_interface", "Show Interface", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("rms_tolerance", "RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_absolute_tolerance", "Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("allow_scale", "Allow Scale", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("allow_x", "Allow X", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_y", "Allow Y", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_z", "Allow Z", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_rx", "Allow Rx", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_ry", "Allow Ry", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("allow_rz", "Allow Rz", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("lock_degrees_of_freedom", "Lock Degrees of Freedom", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("generate_event", "Generate Event", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("csv_report", "File Path for CSV Text Report (requires Show Interface = TRUE)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)],
            [
                new("transform_in_working", "Transform in Working", WorkerMpValueKind.Transform, "GetTransformArg", "—", false),
                new("optimum_transform", "Optimum Transform", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg", "—", false),
                new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("maximum_absolute_deviation", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("number_of_unknowns", "Number of Unknowns", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("number_of_equations", "Number of Equations", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("robustness", "Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "instrument_operations.locate_instrument_group_to_surface_quick_fit",
            "Locate Instrument (Group to Surface Quick Fit)",
            "LocateInstrumentGroupToSurfaceQuickFit",
            [
                new("instrument", "Instrument to Locate", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("measured_group", "Name of Measured Group", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("surface_points_group", "Name of Group containing Surface Pts", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("surface_to_fit", "Surface to fit", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Surface),
                new("other_objects_to_transform", "Other Objects to Transform", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("rms_tolerance", "RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_absolute_tolerance", "Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("rms_error", "RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("maximum_absolute_error", "Maximum Absolute Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "instrument_operations.locate_instrument_ref_tie_in",
            "Locate Instrument (Ref. Tie-In)",
            "LocateInstrumentRefTieIn",
            [
                new("instrument", "Instrument to Locate", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reference_group", "Reference Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("actuals_group", "Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("tolerance", "Tolerance", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("auto_survey", "Auto Survey", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.locate_instruments_usmn",
            "Locate Instruments (USMN)",
            "LocateInstrumentsUsmn",
            [
                new("instruments", "Instruments to Locate", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true),
                new("nominals_group", "Nominals Group Name (blank for none)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("output_group", "Output Group Name (to be established)", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("move_in_working_frame", "Move In Working Frame (TRUE) or Instrument Frame (FALSE)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("auto_reject_outliers_and_resolve", "AutoReject Outliers and Resolve", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("show_usmn_dialog", "Show USMN Dialog", WorkerMpValueKind.ShowUsmnDialogType, "SetShowUsmnDialogTypeArg", "Required", true),
                new("maximum_acceptable_rms_error", "Max Acceptable RMS Error Value (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("maximum_acceptable_error", "Max Acceptable Error Value (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("excluded_groups", "Groups to be Excluded", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("exclude_single_instrument_points", "Exclude Points Measured By Only One Instrument", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("run_uncertainty_field_analysis", "Run Uncertainty Field Analysis?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("analysis_samples", "Analysis Samples", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "300", false),
                new("analysis_time_limit", "Analysis Time Limit (Minutes - 0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "4.000000", false)],
            [
                new("rms_error", "RMS Error Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("maximum_error", "Max Error Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "instrument_operations.lr_apdis_activate_mcm_calibration",
            "LR APDIS Activate MCM Calibration",
            "LrApdisActivateMcmCalibration",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("calibration_name", "Calibration Name (Optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true),
                new("calibration_id", "Calibration ID (Optional)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "Omitted", false, null, null, true)],
            [
                new("active_mcm_name", "Active MCM Name", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        Mutating(
            "instrument_operations.lr_apdis_get_active_mcm_calibration",
            "LR APDIS Get Active MCM Calibration",
            "LrApdisGetActiveMcmCalibration",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("active_mcm_name", "Active MCM Name", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        Mutating(
            "instrument_operations.lr_apdis_perform_mcm_calibration",
            "LR APDIS Perform MCM Calibration",
            "LrApdisPerformMcmCalibration",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("nominal_group", "Nominal Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("use_matte_tooling_ball", "Use Matte Tooling Ball?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("new_calibration_name", "New Calibration Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.lr_get_most_recent_snr_info",
            "LR Get Most Recent SNR Info",
            "LrGetMostRecentSnrInfo",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("info", "SNR", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "snr"),
                new("info", "Size of Data Array", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "size_of_data_array"),
                new("info", "Peak Value Index", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "peak_value_index"),
                new("info", "Peak Value (dB)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "peak_value"),
                new("info", "Measured Range (m)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "measured_range")]),
        Mutating(
            "instrument_operations.lr_hardware_connect",
            "LR Hardware Connect",
            "LrHardwareConnect",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("host", "Host", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("port", "Port", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.lr_hardware_disconnect",
            "LR Hardware Disconnect",
            "LrHardwareDisconnect",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "instrument_operations.lr_self_test",
            "LR Self Test",
            "LrSelfTest",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("reference_arm_length", "Ref Arm Length (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("reference_arm_quality", "Ref Arm Quality", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("mirror_measurement_count", "Mirror Measurement Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false),
                new("mirror_measurement_range_mean", "Mirror Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("mirror_measurement_range_standard_deviation", "Mirror Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("mirror_measurement_quality_mean", "Mirror Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("mirror_measurement_quality_standard_deviation", "Mirror Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("passed_reference_arm_quality_threshold", "Passed Ref Arm Quality Threshold?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("passed_mirror_offset_delta_threshold", "Passed Mirror Offset Delta Threshold?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("passed_mirror_offset_standard_deviation_threshold", "Passed Mirror Offset StdDev Threshold?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("passed_mirror_mean_quality_threshold", "Passed Mirror Mean Quality Threshold?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false),
                new("passed_overall", "Passed Overall?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false)]),
        Mutating(
            "instrument_operations.lr_self_test_flip_test",
            "LR Self Test - Flip Test",
            "LrSelfTestFlipTest",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("result", "Front Measurement - Range (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "front_range"),
                new("result", "Front Measurement - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "front_azimuth"),
                new("result", "Front Measurement - Elevation (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "front_elevation"),
                new("result", "Front Measurement - Quality", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "front_quality"),
                new("result", "Back Measurement - Range (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "back_range"),
                new("result", "Back Measurement - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "back_azimuth"),
                new("result", "Back Measurement - Elevation (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "back_elevation"),
                new("result", "Back Measurement - Quality", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "back_quality"),
                new("result", "Front/Back Difference - Range (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "front_back_difference_range"),
                new("result", "Front/Back Difference - Azimuth (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "front_back_difference_azimuth"),
                new("result", "Front/Back Difference - Elevation (Degs)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "front_back_difference_elevation")]),
        Mutating(
            "instrument_operations.lr_self_test_linearization",
            "LR Self Test - Linearization",
            "LrSelfTestLinearization",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("linearity", "Linearity (kHz)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false)]),
        Mutating(
            "instrument_operations.lr_self_test_lo_sep",
            "LR Self Test - LO Sep",
            "LrSelfTestLoSep",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("region", "Region (1=Region12,2=Region23,3=Region34)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("num_range_measurements", "Num Range Measurements", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)],
            [
                new("result", "Primary LO (indexed from 1)", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "primary_lo"),
                new("result", "Secondary LO (indexed from 1)", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "secondary_lo"),
                new("result", "Primary LO Measurement Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "primary_lo_measurement_count"),
                new("result", "Primary LO Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "primary_lo_range_mean"),
                new("result", "Primary LO Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "primary_lo_range_standard_deviation"),
                new("result", "Primary LO Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "primary_lo_quality_mean"),
                new("result", "Primary LO Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "primary_lo_quality_standard_deviation"),
                new("result", "Secondary LO Measurement Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg", "—", false, null, null, false, "secondary_lo_measurement_count"),
                new("result", "Secondary LO Measurement Range - Mean (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "secondary_lo_range_mean"),
                new("result", "Secondary LO Measurement Range - StdDev (Inches)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "secondary_lo_range_standard_deviation"),
                new("result", "Secondary LO Measurement Quality - Mean", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "secondary_lo_quality_mean"),
                new("result", "Secondary LO Measurement Quality - StdDev", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false, null, null, false, "secondary_lo_quality_standard_deviation")]),
        Mutating(
            "instrument_operations.lr_set_red_laser_intensity",
            "LR Set Red Laser Intensity",
            "LrSetRedLaserIntensity",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("intensity", "Intensity (0-100)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.lr_verify_hardware_connection",
            "LR Verify Hardware Connection",
            "LrVerifyHardwareConnection",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("connected_to_hardware", "Connected to Hardware?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false)]),
        ReadOnly(
            "instrument_operations.make_collection_object_name_ref_list_from_objects_associated_with_instruments",
            "Make Collection Object Name Ref List from Objects associated with Instruments",
            "MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstruments",
            [
                new("instruments", "Instrument IDs", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true)],
            [
                new("objects", "Resultant Collection Object Name List", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg", "—", false)]),
        ReadOnly(
            "instrument_operations.make_surface_face_list_from_point_proximity",
            "Make Surface Face List from Point Proximity",
            "MakeSurfaceFaceListFromPointProximity",
            [
                new("measured_points", "Measured Points", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Required", true)],
            [
                new("surface_faces", "Selected Surface Faces", WorkerMpValueKind.Text, "GetStringArg", "—", false)]),
        Mutating(
            "instrument_operations.measure",
            "Measure",
            "Measure",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "instrument_operations.measure_existing_single_point",
            "Measure Existing Single Point",
            "MeasureExistingSinglePoint",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("existing_target_id", "Existing Target ID", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("group_name_for_new_point", "Group name for new point", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("measure_immediately", "Measure Immediately", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("html_prompt_file", "HTML Prompt File (optional)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)],
            [
                new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName, "GetPointNameArg", "—", false)]),
        Mutating(
            "instrument_operations.measure_existing_single_point_and_compare",
            "Measure Existing Single Point and Compare",
            "MeasureExistingSinglePointAndCompare",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("existing_target_id", "Existing Target ID", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("group_name_for_new_point", "Group name for new point", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("measure_immediately", "Measure Immediately", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("html_prompt_file", "HTML Prompt File (optional)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true),
                new("tolerance", "Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)],
            [
                new("vector_representation", "Vector Representation", WorkerMpValueKind.Vector, "GetVectorArg", "—", false),
                new("x_value", "X Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("y_value", "Y Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("z_value", "Z Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("magnitude", "Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg", "—", false),
                new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName, "GetPointNameArg", "—", false)]),
        Mutating(
            "instrument_operations.measure_existing_single_point_manual_guide",
            "Measure Existing Single Point (Manual Guide)",
            "MeasureExistingSinglePointManualGuide",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("existing_target_id", "Existing Target ID", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("group_name_for_new_point", "Group name for new point", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("measure_immediately", "Measure Immediately", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("html_prompt_file", "HTML Prompt File (optional)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)],
            [
                new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName, "GetPointNameArg", "—", false)]),
        Mutating(
            "instrument_operations.measure_nominal_feature",
            "Measure Nominal Feature",
            "MeasureNominalFeature",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("feature", "Feature Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("resulting_point", "Resulting Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "instrument_operations.measure_single_point_here",
            "Measure Single Point Here",
            "MeasureSinglePointHere",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("target_id", "Target ID", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("measure_immediately", "Measure Immediately", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("html_prompt_file", "HTML Prompt File (optional)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.move_instrument_to_another_collection",
            "Move Instrument to Another Collection",
            "MoveInstrumentToAnotherCollection",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("collection_name", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "instrument_operations.move_measurement_observation",
            "Move Measurement Observation",
            "MoveMeasurementObservation",
            [
                new("source_point_name", "Source Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("observation_index", "Observation index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("delete_point_if_no_measurements_remain", "Delete point if no measurements remain?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("destination_point_name", "Destination Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("force_observation_active", "Force observation to be active?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.move_objects_in_6d_using_instrument_updates",
            "Move Objects in 6D using Instrument Updates",
            "MoveObjectsIn6dUsingInstrumentUpdates",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("objects_to_move", "Objects to Move", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("measurement_mode", "Measurement Mode", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.multi_measurement_initiate",
            "Multi Measurement Initiate",
            "MultiMeasurementInitiate",
            [
                new("instruments", "Instruments", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true),
                new("measurement_mode", "Measurement Mode", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("wait_for_completion", "Wait for Completion", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.multi_measurement_stop",
            "Multi Measurement Stop",
            "MultiMeasurementStop",
            [
                new("instruments", "Instruments", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true)]),
        Mutating(
            "instrument_operations.point_at_target",
            "Point At Target",
            "PointAtTarget",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("target_id", "Target ID", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("html_prompt_file", "HTML Prompt File (optional)", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.quick_align",
            "Quick Align",
            "QuickAlign",
            [
                new("instruments", "Instrument IDs", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true),
                new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("nominal_points", "Nominal Points (optional)", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg", "Omitted", false, null, null, true),
                new("nominal_point_of_view_names", "Nominal Point of View Names (optional)", WorkerMpValueKind.StringList, "SetStringRefListArg", "Omitted", false, null, null, true),
                new("align_to_individual_faces_only", "Align to Individual Faces Only (not Entire Surface)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.rename_instrument",
            "Rename Instrument",
            "RenameInstrument",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("new_name", "New Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.save_cloud_viewer_point_cloud_file",
            "Save Point Cloud File",
            "SaveCloudViewerPointCloudFile",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("file_path", "File Path", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true),
                new("save_as_ascii", "Save as Ascii", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.save_instrument_configuration",
            "Save Instrument Configuration",
            "SaveInstrumentConfiguration",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("configuration_file", "Configuration File", WorkerMpValueKind.FileReference, "SetFilePathArg", "Omitted", false, null, null, true)]),
        Mutating(
            "instrument_operations.send_cloud_to_sa",
            "Send Cloud To SA",
            "SendCloudToSa",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("cloud_name", "Cloud Name", WorkerMpValueKind.CloudName, "SetCloudNameArg", "Required", true)]),
        Mutating(
            "instrument_operations.set_absolute_instrument_scale_factor",
            "Set (absolute) Instrument Scale Factor (CAUTION!)",
            "SetAbsoluteInstrumentScaleFactor",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("scale_factor", "Scale Factor", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "instrument_operations.set_alignment_projector",
            "Set Alignment Projector",
            "SetAlignmentProjector",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("projector_profile", "Projector Profile", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("user_prompt", "User Prompt", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.set_cloud_viewer_filter",
            "Set Filter",
            "SetCloudViewerFilter",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("filter_value", "Filter Value", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.set_instrument_axes",
            "Set Instrument Axes",
            "SetInstrumentAxes",
            [
                new("instrument_to_adjust", "Instrument to Adjust", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("axis_values", "Axis Values", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true),
                new("number_of_steps", "Number of Steps", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_base",
            "Set Instrument Base Uncertainty Covariance Matrix WRT Base",
            "SetInstrumentBaseUncertaintyCovarianceMatrixWrtBase",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("covariance_matrix", "Covar Row 1", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_1"),
                new("covariance_matrix", "Covar Row 2", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_2"),
                new("covariance_matrix", "Covar Row 3", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_3"),
                new("covariance_matrix", "Covar Row 4", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_4"),
                new("covariance_matrix", "Covar Row 5", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_5"),
                new("covariance_matrix", "Covar Row 6", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_6")]),
        Mutating(
            "instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_world",
            "Set Instrument Base Uncertainty Covariance Matrix WRT WORLD",
            "SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("covariance_matrix", "Covar Row 1", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_1"),
                new("covariance_matrix", "Covar Row 2", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_2"),
                new("covariance_matrix", "Covar Row 3", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_3"),
                new("covariance_matrix", "Covar Row 4", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_4"),
                new("covariance_matrix", "Covar Row 5", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_5"),
                new("covariance_matrix", "Covar Row 6", WorkerMpValueKind.DoubleArray, "SetDoubleArrayArg", "Required", true, null, null, false, "row_6")]),
        Mutating(
            "instrument_operations.set_instrument_group_and_target",
            "Set Instrument Group and Target",
            "SetInstrumentGroupAndTarget",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("point", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true)]),
        Mutating(
            "instrument_operations.set_instrument_interface_response_timeout",
            "Set Instrument Interface Response Timeout",
            "SetInstrumentInterfaceResponseTimeout",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("timeout", "Timeout (secs)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "instrument_operations.set_instrument_measurement_mode_profile",
            "Set Instrument Measurement Mode/Profile",
            "SetInstrumentMeasurementModeProfile",
            [
                new("instrument", "Instrument to set", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("mode_profile", "Mode/Profile", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.set_instrument_targeting",
            "Set Instrument Targeting",
            "SetInstrumentTargeting",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("targeting_name", "Targeting Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.set_instrument_transform",
            "Set Instrument Transform",
            "SetInstrumentTransform",
            [
                new("instrument", "Instrument to Move", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("destination_transform", "Destination Transform", WorkerMpValueKind.Transform, "SetTransformArg", "Required", true),
                new("reference_frame", "Reference Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("number_of_steps", "Number of Steps", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.set_instrument_weather_setting",
            "Set Instrument Weather Setting",
            "SetInstrumentWeatherSetting",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("temperature", "Temperature (F)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("pressure", "Pressure (mmHg)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("relative_humidity", "Humidity (%Rel)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false),
                new("set_automatically", "Set Automatically? (Ignore above values)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.set_multiply_instrument_scale_factor",
            "Set (multiply) Instrument Scale Factor (CAUTION!)",
            "SetMultiplyInstrumentScaleFactor",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("scale_factor", "Scale Factor", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000000", false)]),
        Mutating(
            "instrument_operations.set_observation_collimation_shot_options",
            "Set Observation Collimation Shot Options",
            "SetObservationCollimationShotOptions",
            [
                new("point", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("observation_index", "Observation Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("is_collimation_shot", "Is Collimation Shot? (FALSE = Normal", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("targeted_instrument", "Targeted Instrument", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "instrument_operations.set_observation_mirror_cube_shot_face",
            "Set Observation Mirror Cube Shot Face",
            "SetObservationMirrorCubeShotFace",
            [
                new("point", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("observation_index", "Observation Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("is_mirror_cube_shot", "Is Mirror Cube Shot? (FALSE = Normal)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("mirror_cube_shot_face", "Mirror Cube Shot Face (1 .. 6)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "1", false)]),
        Mutating(
            "instrument_operations.set_observation_status",
            "Set Observation Status",
            "SetObservationStatus",
            [
                new("point", "Point Name", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("observation_index", "Observation Index", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("active", "Active?", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.set_pcmm_instrument_xyz_uncertainties",
            "Set PCMM Instrument XYZ Uncertainties",
            "SetPcmmInstrumentXyzUncertainties",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("x_uncertainty", "X Uncertainty", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.001000", false),
                new("y_uncertainty", "Y Uncertainty)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.001000", false),
                new("z_uncertainty", "Z Uncertainty", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.001000", false)]),
        Mutating(
            "instrument_operations.set_probe_offset_frame_offline",
            "Set Probe Offset Frame Offline (Select Previously Measured Frame)",
            "SetProbeOffsetFrameOffline",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("probe_name", "Probe Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("face_id", "Face ID ", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("raw_measured_frame", "Raw Measured Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame),
                new("offset_frame", "Offset Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)]),
        Mutating(
            "instrument_operations.set_probe_offset_frame_online",
            "Set Probe Offset Frame Online (Measure Raw Frame)",
            "SetProbeOffsetFrameOnline",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("probe_name", "Probe Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("face_id", "Face ID ", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("measure_profile_name", "Measure Profile Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("timeout_seconds", "Timeout in Seconds", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "15.000000", false),
                new("offset_frame", "Offset Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)]),
        Mutating(
            "instrument_operations.set_remeasure_failed_checks_only",
            "Set Remeasure Failed Checks Only",
            "SetRemeasureFailedChecksOnly",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true)]),
        Mutating(
            "instrument_operations.set_target_computation_options",
            "Set Target Computation Options",
            "SetTargetComputationOptions",
            [
                new("computation_method", "Target Computation Method", WorkerMpValueKind.TargetComputationMethod, "SetTargetComputationMethodArg", "Required", true),
                new("ignore_distance_measurements", "Ignore Distance Measurements", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.set_tracker_edm_theodolite_uncertainties",
            "Set Tracker/EDM Theodolite Uncertainties",
            "SetTrackerEdmTheodoliteUncertainties",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("theta_dispersion", "Theta Dispersion (arcseconds)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("theta_threshold", "Theta Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.001000", false),
                new("phi_dispersion", "Phi Dispersion(arcseconds)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "1.000000", false),
                new("phi_threshold", "Phi Threshold(linear units)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.001000", false),
                new("distance", "Distance (PPM)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "2.500000", false),
                new("distance_threshold", "Distance Threshold (linear units)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000300", false)]),
        Mutating(
            "instrument_operations.set_wrtl_channel",
            "Set WRTL Channel",
            "SetWrtlChannel",
            [
                new("instrument", "Instrument", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("channel", "Channel", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.set_xyz_instrument_uncertainties",
            "Set XYZ Instrument Uncertainties",
            "SetXyzInstrumentUncertainties",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("x_uncertainty", "X Uncertainty", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000500", false),
                new("y_uncertainty", "Y Uncertainty", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000500", false),
                new("z_uncertainty", "Z Uncertainty)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg", "0.000500", false)]),
        Mutating(
            "instrument_operations.set_xyz_reference_frame_instrument_base_anchor_frame",
            "Set XYZ Reference Frame Instrument Base Anchor Frame",
            "SetXyzReferenceFrameInstrumentBaseAnchorFrame",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("anchor_frame", "Anchor Frame", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.Frame)]),
        Mutating(
            "instrument_operations.start_gdt_inspection",
            "Start GD&T Inspection",
            "StartGdtInspection",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("filter", "Filter (ALL/CHECKS/DATUMS)", WorkerMpValueKind.Text, "SetStringArg", "ALL", false, null, ["ALL", "CHECKS", "DATUMS"])]),
        Mutating(
            "instrument_operations.start_gdt_inspection_design",
            "Start GD&T Inspection Design",
            "StartGdtInspectionDesign",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("filter", "Filter (ALL/CHECKS/DATUMS)", WorkerMpValueKind.Text, "SetStringArg", "ALL", false, null, ["ALL", "CHECKS", "DATUMS"])]),
        Mutating(
            "instrument_operations.start_gdt_inspection_rehearse",
            "Start GD&T Inspection Rehearse",
            "StartGdtInspectionRehearse",
            [
                new("collection", "Collection Name", WorkerMpValueKind.CollectionName, "SetCollectionNameArg", "Required", true),
                new("filter", "Filter (ALL/CHECKS/DATUMS)", WorkerMpValueKind.Text, "SetStringArg", "ALL", false, null, ["ALL", "CHECKS", "DATUMS"])]),
        Mutating(
            "instrument_operations.start_instrument_interface",
            "Start Instrument Interface",
            "StartInstrumentInterface",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("initialize_at_startup", "Initialize at Startup", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("device_ip_address", "Device IP Address (optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true),
                new("interface_type", "Interface Type (0=default)", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("run_in_simulation", "Run in Simulation", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("allow_start_without_initialization_requirements", "Allow Start w/o Init Requirements", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.start_theodolite_interface",
            "Start Theodolite Interface",
            "StartTheodoliteInterface",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("theodolite_type", "Theodolite Type (Must match Theodolite Manager Add Instrument type)", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("comm_port", "Comm Port", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("device_ip_address", "Device IP Address (optional)", WorkerMpValueKind.Text, "SetStringArg", "Omitted", false, null, null, true),
                new("simulation", "Simulation", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.stop_active_measurement_mode",
            "Stop Active Measurement Mode",
            "StopActiveMeasurementMode",
            [
                new("instrument", "Instrument ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "instrument_operations.stop_instrument_interface",
            "Stop Instrument Interface",
            "StopInstrumentInterface",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)]),
        Mutating(
            "instrument_operations.synchronized_measurement_master_slave",
            "Synchronized Measurement (Master/Slave)",
            "SynchronizedMeasurementMasterSlave",
            [
                new("master_instrument", "Master Instrument", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("slave_instrument", "Slave Instrument", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("slave_group_suffix", "Slave Group Suffix", WorkerMpValueKind.Text, "SetStringArg", "_Slave", false),
                new("locate_one_of_the_instruments", "Locate One of the Instruments?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false),
                new("locate_master", "Locate Master (FALSE = Slave)", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("wait_for_completion", "Wait for Completion?", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
        Mutating(
            "instrument_operations.track_tape_measurement",
            "Track Tape Measurement",
            "TrackTapeMeasurement",
            [
                new("instrument", "Instrument to scan", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("point_on_tape", "Point on Tape", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("point_on_part", "Point on Part", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("direction_point", "Point for Direction", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("termination_point", "Point for Termination", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("parameter_set_name", "Parameter set name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("point_group", "Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true, WorkerObjectTypeValue.PointGroup),
                new("initial_target_name", "Initial Target Name", WorkerMpValueKind.Text, "SetStringArg", "Empty", false)]),
        Mutating(
            "instrument_operations.transform_instrument_by_delta",
            "Transform Instrument by Delta",
            "TransformInstrumentByDelta",
            [
                new("instrument", "Instrument to Transform", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("delta_transform", "Delta Transform", WorkerMpValueKind.WorldTransform, "SetWorldTransformArg", "Required", true),
                new("apply_scale_to_instrument", "Apply Scale from Transform to Instrument", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.transform_instrument_frame_to_frame",
            "Transform Instrument - Frame To Frame",
            "TransformInstrumentFrameToFrame",
            [
                new("instrument", "Instrument to move", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("initial_frame", "Initial Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("destination_frame", "Destination Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("number_of_steps", "Number of Steps", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.transform_multiple_instruments_by_delta",
            "Transform Multiple Instruments By Delta",
            "TransformMultipleInstrumentsByDelta",
            [
                new("instruments", "Instruments to Move", WorkerMpValueKind.CollectionInstrumentIdList, "SetColInstIdRefListArg", "Required", true),
                new("delta_transform", "Delta Transform", WorkerMpValueKind.WorldTransform, "SetWorldTransformArg", "Required", true),
                new("apply_scale_to_instruments", "Apply Scale from Transform to Instrument", WorkerMpValueKind.Logical, "SetBoolArg", "false", false)]),
        Mutating(
            "instrument_operations.verify_instrument_connection",
            "Verify Instrument Connection",
            "VerifyInstrumentConnection",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true)],
            [
                new("connected", "Connected?", WorkerMpValueKind.Logical, "GetBoolArg", "—", false)]),
        Mutating(
            "instrument_operations.wait_for_trapping_to_complete",
            "Wait For Trapping To Complete",
            "WaitForTrappingToComplete",
            []),
        Mutating(
            "instrument_operations.watch_closest_point",
            "Watch Closest Point",
            "WatchClosestPoint",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("groups_to_consider", "Groups to Consider", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("watch_window_properties", "3 DOF Watch Window Properties", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_mode", "Measurement Mode", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("pause_mp_until_closed", "Pause MP Until Closed", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("window_top_left_x", "Window Top Left X Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_top_left_y", "Window Top Left Y Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_width", "Window Width", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_height", "Window Height", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.watch_instrument",
            "Watch Instrument",
            "WatchInstrument",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("pause_mp_until_closed", "Pause MP Until Closed", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("watch_window_properties", "3 DOF Watch Window Properties", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("window_top_left_x", "Window Top Left X Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_top_left_y", "Window Top Left Y Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_width", "Window Width", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_height", "Window Height", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.watch_point_to_edge",
            "Watch Point To Edge",
            "WatchPointToEdge",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("projection_reference_objects", "Projection Reference Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("measurement_reference_objects", "Measurement Reference Objects ", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("projection_options", "Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg", "Required", true),
                new("watch_window_properties", "3 DOF Watch Window Properties", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_mode", "Measurement Mode", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("pause_mp_until_closed", "Pause MP Until Closed", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("window_top_left_x", "Window Top Left X Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_top_left_y", "Window Top Left Y Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_width", "Window Width", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_height", "Window Height", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.watch_point_to_objects",
            "Watch Point To Objects",
            "WatchPointToObjects",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("objects_to_consider", "Objects to Consider", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg", "Required", true),
                new("projection_options", "Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg", "Required", true),
                new("watch_window_properties", "3 DOF Watch Window Properties", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_mode", "Measurement Mode", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("pause_mp_until_closed", "Pause MP Until Closed", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("window_top_left_x", "Window Top Left X Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_top_left_y", "Window Top Left Y Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_width", "Window Width", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_height", "Window Height", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.watch_point_to_point",
            "Watch Point To Point",
            "WatchPointToPoint",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reference_point", "Reference Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("watch_window_properties", "3 DOF Watch Window Properties", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2", "Required", true),
                new("measurement_mode", "Measurement Mode", WorkerMpValueKind.Text, "SetStringArg", "Empty", false),
                new("pause_mp_until_closed", "Pause MP Until Closed", WorkerMpValueKind.Logical, "SetBoolArg", "false", false),
                new("window_top_left_x", "Window Top Left X Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_top_left_y", "Window Top Left Y Position", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_width", "Window Width", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false),
                new("window_height", "Window Height", WorkerMpValueKind.WholeNumber, "SetIntegerArg", "0", false)]),
        Mutating(
            "instrument_operations.watch_point_to_point_with_view_zooming",
            "Watch Point To Point With View Zooming",
            "WatchPointToPointWithViewZooming",
            [
                new("instrument", "Instrument's ID", WorkerMpValueKind.CollectionInstrumentId, "SetColInstIdArg", "Required", true),
                new("reference_point", "Reference Point", WorkerMpValueKind.PointName, "SetPointNameArg", "Required", true),
                new("update", "Update(TRUE),Close(FALSE)", WorkerMpValueKind.Logical, "SetBoolArg", "true", false)]),
    ];

    private static MpOperationContract Mutating(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract>? outputs = null, IReadOnlyList<string>? riskFlags = null, Action<IMessage>? validateRequest = null) =>
        new(operationId, step, Service, rpc, "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, riskFlags ?? [], inputs, outputs ?? [], validateRequest);

    private static MpOperationContract ReadOnly(string operationId, string step, string rpc, IReadOnlyList<MpArgumentContract> inputs, IReadOnlyList<MpArgumentContract> outputs) =>
        new(operationId, step, Service, rpc, "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, [], inputs, outputs);
}
