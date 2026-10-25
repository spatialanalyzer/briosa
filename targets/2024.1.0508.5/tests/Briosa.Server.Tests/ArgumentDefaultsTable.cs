namespace Briosa.Server.Tests;

/// <summary>
/// The reviewed D2 argument defaults (#243), transcribed from
/// <c>eng/classification/argument-defaults-&lt;target&gt;.csv</c>: for each argument, what
/// the server does in major 3 when the request field is absent, explicitly empty,
/// or set. <see cref="ArgumentDefaultsTests"/> builds the real MP command for each
/// row three ways, and <c>eng/Test-ArgumentDefaults.ps1</c> requires this table and
/// the CSV to agree in both directions. Keep one row per line in the form below so
/// that script can read it.
/// </summary>
internal static class ArgumentDefaultsTable
{
    public static IReadOnlyList<ArgumentDefaultRow> Rows { get; } =
        [
            new("instrument_operations.locate_instruments_usmn", "LocateInstrumentsUsmnRequest.nominals_group", "Nominals Group Name (blank for none)", "SetCollectionObjectNameArg2", "omitted", "sends_blank", "sent"),
            new("reporting_operations.create_chart_from_vector_group", "CreateChartFromVectorGroupRequest.template_chart_name", "Template Chart Name (optional)", "SetChartNameArg", "omitted", "rejected", "sent"),
            new("utility_operations.set_user_interface_profile", "SetUserInterfaceProfileRequest.profile_file_name", "Profile File Name (optional)", "SetFilePathArg", "omitted", "rejected", "sent"),
            new("construction_operations.construct_planes_bounding_point_group", "ConstructPlanesBoundingPointGroupRequest.resulting_high_plane_name", "Resulting 'High' Plane Name", "SetCollectionObjectNameArg2", "omitted", "rejected", "sent"),
            new("construction_operations.construct_planes_bounding_point_group", "ConstructPlanesBoundingPointGroupRequest.resulting_low_plane_name", "Resulting 'Low' Plane Name", "SetCollectionObjectNameArg2", "omitted", "rejected", "sent"),
            new("relationship_operations.make_group_to_group_relationship", "MakeGroupToGroupRelationshipRequest.tolerance", "Tolerance", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("relationship_operations.make_group_to_group_relationship", "MakeGroupToGroupRelationshipRequest.constraint", "Constraint", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("relationship_operations.make_group_to_nominal_group_relationship", "MakeGroupToNominalGroupRelationshipRequest.tolerance", "Tolerance", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("relationship_operations.make_group_to_nominal_group_relationship", "MakeGroupToNominalGroupRelationshipRequest.constraint", "Constraint", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("relationship_operations.make_point_to_point_relationship", "MakePointToPointRelationshipRequest.tolerance", "Tolerance", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("relationship_operations.make_point_to_point_relationship", "MakePointToPointRelationshipRequest.constraint", "Constraint", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("relationship_operations.make_points_to_points_relationship", "MakePointsToPointsRelationshipRequest.tolerance", "Tolerance", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("relationship_operations.make_points_to_points_relationship", "MakePointsToPointsRelationshipRequest.constraint", "Constraint", "SetToleranceVectorOptionsArg", "rejected", "sends_all_limits_disabled", "sent"),
            new("analysis_operations.fit_geometry_to_point_group", "FitGeometryToPointGroupRequest.starting_condition_geometry", "Starting Condition Geometry (optional)", "SetCollectionObjectNameArg2", "rejected", "rejected", "sent"),
            new("analysis_operations.fit_geometry_to_points", "FitGeometryToPointsRequest.starting_condition_geometry", "Starting Condition Geometry (optional)", "SetCollectionObjectNameArg2", "rejected", "rejected", "sent"),
            new("analysis_operations.fit_geometry_to_point_group_projected_to_plane", "FitGeometryToPointGroupProjectedToPlaneRequest.starting_condition_geometry", "Starting Condition Geometry (optional)", "SetCollectionObjectNameArg2", "rejected", "rejected", "sent"),
            new("process_flow_operations.ask_for_point_name", "AskForPointNameRequest.initial_value", "Initial Value", "SetPointNameArg", "rejected", "rejected", "sent"),
            new("file_operations.import_sa_file", "ImportSaFileRequest.selected_collections", "Selected Collections (optional)", "SetStringRefListArg", "rejected", "rejected", "sent"),
            new("construction_operations.make_callout_view_ref_list_wildcard_selection", "MakeCalloutViewRefListWildcardSelectionResult.callout_views", "Resultant Callout View List", "GetCollectionObjectNameRefListArg", "removed", "removed", "removed"),
            new("construction_operations.add_collection_instruments_to_ref_list_wildcard_selection", "AddCollectionInstrumentsToRefListWildcardSelectionResult.collection_instrument_ref_list", "Collection Instrument Reference List", "GetColInstIdRefListArg", "removed", "removed", "removed"),
            new("construction_operations.set_callout_view_properties", "SetCalloutViewPropertiesRequest.callout_views", "Callout View List", "SetCollectionObjectNameRefListArg", "removed", "removed", "removed"),
            new("construction_operations.construct_circles_lines_from_surfaces", "ConstructCirclesLinesFromSurfacesRequest.circle_line_mode", "Circle Line Mode", "SetStringArg", "removed", "removed", "removed"),
            new("construction_operations.mirror_objects", "MirrorObjectsRequest.frame_plane_to_mirror_around", "Frame Plane to Mirror Around", "SetStringArg", "removed", "removed", "removed"),
            new("construction_operations.construct_polygonized_surface_from_point_clouds", "ConstructPolygonizedSurfaceFromPointCloudsRequest.mesh_orientation", "Mesh Orientation", "SetStringArg", "removed", "removed", "removed"),
            new("construction_operations.delete_collections_by_wildcard", "DeleteCollectionsByWildcardRequest.search_string", "Search String", "SetStringArg", "rejected", "rejected", "sent"),
            new("construction_operations.delete_folders_by_wildcard", "DeleteFoldersByWildcardRequest.search_string", "Search String", "SetStringArg", "rejected", "rejected", "sent"),
            new("instrument_operations.delete_measurement_observation", "DeleteMeasurementObservationRequest.observation_index", "Observation index", "SetIntegerArg", "rejected", "not_applicable", "sent"),
            new("instrument_operations.move_measurement_observation", "MoveMeasurementObservationRequest.observation_index", "Observation index", "SetIntegerArg", "rejected", "not_applicable", "sent"),
            new("instrument_operations.set_absolute_instrument_scale_factor", "SetAbsoluteInstrumentScaleFactorRequest.scale_factor", "Scale Factor", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("instrument_operations.set_instrument_weather_setting", "SetInstrumentWeatherSettingRequest.temperature", "Temperature (F)", "SetDoubleArg", "rejected_unless:set_automatically", "not_applicable", "sent"),
            new("instrument_operations.set_instrument_weather_setting", "SetInstrumentWeatherSettingRequest.pressure", "Pressure (mmHg)", "SetDoubleArg", "rejected_unless:set_automatically", "not_applicable", "sent"),
            new("instrument_operations.set_instrument_weather_setting", "SetInstrumentWeatherSettingRequest.relative_humidity", "Humidity (%Rel)", "SetDoubleArg", "rejected_unless:set_automatically", "not_applicable", "sent"),
            new("instrument_operations.set_instrument_weather_setting", "SetInstrumentWeatherSettingRequest.set_automatically", "Set Automatically? (Ignore above values)", "SetBoolArg", "rejected", "not_applicable", "sent"),
            new("instrument_operations.set_multiply_instrument_scale_factor", "SetMultiplyInstrumentScaleFactorRequest.scale_factor", "Scale Factor", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("reporting_operations.remove_report_tag", "RemoveReportTagRequest.tag_name", "Tag Name", "SetStringArg", "rejected", "rejected", "sent"),
            new("robot_operations.delete_robot_calibration", "DeleteRobotCalibrationRequest.calibration_name", "Calibration Name", "SetStringArg", "rejected", "rejected", "sent"),
            new("robot_operations.move_robot_machine_to_joint_pose_six_dof", "MoveRobotMachineToJointPoseSixDofRequest.joint_1", "Joint 1", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.move_robot_machine_to_joint_pose_six_dof", "MoveRobotMachineToJointPoseSixDofRequest.joint_2", "Joint 2", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.move_robot_machine_to_joint_pose_six_dof", "MoveRobotMachineToJointPoseSixDofRequest.joint_3", "Joint 3", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.move_robot_machine_to_joint_pose_six_dof", "MoveRobotMachineToJointPoseSixDofRequest.joint_4", "Joint 4", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.move_robot_machine_to_joint_pose_six_dof", "MoveRobotMachineToJointPoseSixDofRequest.joint_5", "Joint 5", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.move_robot_machine_to_joint_pose_six_dof", "MoveRobotMachineToJointPoseSixDofRequest.joint_6", "Joint 6", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.dh_alpha_component", "DH ALPHA Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.dh_a_component", "DH A Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.dh_d_component", "DH D Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.dh_theta_component", "DH THETA Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.dh_x_axis_deflection_factor", "DH X-Axis Deflection Factor", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.dh_y_axis_deflection_factor", "DH Y-Axis Deflection Factor", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.dh_z_axis_deflection_factor", "DH Z-Axis Deflection Factor", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.six_dof_x_component", "6DOF X Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.six_dof_y_component", "6DOF Y Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.six_dof_z_component", "6DPF Z Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.six_dof_rx_component", "6DOF RX Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.six_dof_ry_component", "6DOF RY Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.six_dof_rz_component", "6DOF RZ Component", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.encoder_offset_value", "Encoder Offset Value", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.minimum_encoder_limit", "Minimum Encoder Limit", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.maximum_encoder_limit", "Maximum Encoder Limit", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.encoder_sense_negative", "Encoder Sense Negative", "SetBoolArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.include_additional_encoder", "Include Additional Encoder", "SetBoolArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.additional_encoder_index_offset", "Additional Encoder Index Offset", "SetIntegerArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.additional_encoder_sense_negative", "Additional Encoder Sense Negative", "SetBoolArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.segment_origin_mass_kg", "Segment Origin Mass in Kg", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_model_link_parameters", "SetRobotMachineModelLinkParametersRequest.segment_cg_mass_kg", "Segment CG Mass in Kg", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("robot_operations.set_robot_machine_parameter", "SetRobotMachineParameterRequest.parameter_value", "Parameter Value", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("utility_operations.delete_folder", "DeleteFolderRequest.folder_path", "Folder Path", "SetStringArg", "rejected", "rejected", "sent"),
            new("utility_operations.scale_objects", "ScaleObjectsRequest.scale_factor", "Scale Factor", "SetDoubleArg", "rejected", "not_applicable", "sent"),
            new("variables.delete_variable", "DeleteVariableRequest.name", "Name", "SetStringArg", "rejected", "rejected", "sent"),
            new("variables.delete_variables_wildcard_match", "DeleteVariablesWildcardMatchRequest.variable_wildcard_criteria", "Variable Wildcard Criteria", "SetStringArg", "rejected", "rejected", "sent"),
            new("vector_operations.delete_ith_vector_from_vector_group", "DeleteIthVectorFromVectorGroupRequest.vector_index", "Vector Index", "SetIntegerArg", "rejected", "not_applicable", "sent"),
            new("vector_operations.delete_vector_by_name", "DeleteVectorByNameRequest.vector_name", "Vector Name", "SetStringArg", "rejected", "rejected", "sent"),
            new("instrument_operations.measure_existing_single_point_manual_guide", "MeasureExistingSinglePointManualGuideRequest.measure_immediately", "Measure Immediately", "SetBoolArg", "sends_default:true", "not_applicable", "sent")
        ];
}

/// <summary>One reviewed argument: the operation, the protobuf field as
/// <c>Message.field</c>, the exact MP argument label and SDK binding, and the major-3
/// outcome when the field is absent, explicitly empty, or set.</summary>
internal sealed record ArgumentDefaultRow(
    string OperationId,
    string ProtoField,
    string MpArgument,
    string SdkBinding,
    string Absent,
    string ExplicitEmpty,
    string Value);
