using static Briosa.Server.Security.OperationOptionAbsence;
using static Briosa.Server.Security.OperationOptionCondition;
using static Briosa.Server.Security.OperationOptionEffect;
using static Briosa.Server.Security.OperationOptionTrigger;

namespace Briosa.Server.Security;

/// <summary>
/// The reviewed caller options of this exact-SA target whose request value changes
/// an operation's effective classification (#242, #293).
/// </summary>
/// <remarks>
/// Transcribed from the <c>caller_option</c> and <c>caller_option_default_flips</c>
/// entries of <c>eng/classification/conditional-ui.json</c> (operator UI) and
/// <c>eng/classification/conditional-background.json</c> (work left running). The
/// JSON files are the reviewed seed; <c>eng/Test-OperationConditionalOptions.ps1</c>
/// fails when an entry differs in either direction. Keep one entry per line in the
/// form below so that check can read it. Job- and file-state dialogs have no
/// request option and are documented in the JSON only.
/// <see cref="OperationRequestClassifier"/> reads these entries before dispatch.
/// </remarks>
internal static class OperationConditionalOptions
{
    public static IReadOnlyList<OperationConditionalOption> Entries { get; } =
        [
            new("analysis_operations.best_fit_transformation_group_to_group", "show_interface", "Show Interface", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.fit_geometry_to_point_group", "report_deviations", "Report Deviations", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.fit_geometry_to_point_group_projected_to_plane", "report_deviations", "Report Deviations", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.fit_geometry_to_points", "report_deviations", "Report Deviations", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.query_groups_to_objects", "show_results_dialog", "Show Results Dialog?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.query_points_to_objects", "show_results_dialog", "Show Results Dialog?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.query_points_to_single_point", "show_vector_properties", "Show Vector Properties?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.rename_points_based_on_inter_point_distance_to_reference_points", "verify_results", "Verify Results?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("analysis_operations.rename_points_based_on_proximity_to_reference_points", "verify_results", "Verify Results?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("file_operations.direct_cad_access", "prompt_on_missing_components", "Prompt on Missing Components", InteractiveUi, CallerOption, WhenTrue, SendsTrue),
            new("file_operations.direct_cad_access", "selective_import", "Selective Import", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("file_operations.import_sa_file", "allow_operator_selections", "Allow Operator Selections", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("file_operations.import_step_file", "display_entity_filters", "Display Entity Filters", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("file_operations.import_step_file", "display_residuals", "Display Residuals", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("file_operations.import_e57_file", "automatically_close_converter", "Automatically Close Converter", InteractiveUi, CallerOption, WhenFalse, SendsTrue),
            new("cloud_and_mesh_operations.mesh_fill_holes", "fill_all_holes", "Fill All Holes?", InteractiveUi, CallerOption, WhenFalse, SendsTrue),
            new("instrument_operations.delete_instrument", "prompt_user_to_confirm", "Prompt user to confirm?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.auto_correspond_with_proximity_trigger", "show_watch_window", "Show Watch window on startup", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.locate_instrument_best_fit_group_to_group", "show_interface", "Show Interface", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.locate_instrument_best_fit_nominal_geometry", "show_interface", "Show Interface", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.locate_instruments_usmn", "show_usmn_dialog", "Show USMN Dialog", InteractiveUi, CallerOption, WhenOneOf, Rejected, "SHOW_USMN_DIALOG_YES", "SHOW_USMN_DIALOG_ON_TOLERANCE_VIOLATION"),
            new("instrument_operations.measure_existing_single_point", "html_prompt_file", "HTML Prompt File (optional)", InteractiveUi, CallerOption, WhenPresent, SendsNothing),
            new("instrument_operations.measure_existing_single_point_and_compare", "html_prompt_file", "HTML Prompt File (optional)", InteractiveUi, CallerOption, WhenPresent, SendsNothing),
            new("instrument_operations.measure_single_point_here", "html_prompt_file", "HTML Prompt File (optional)", InteractiveUi, CallerOption, WhenPresent, SendsNothing),
            new("instrument_operations.point_at_target", "html_prompt_file", "HTML Prompt File (optional)", InteractiveUi, CallerOption, WhenPresent, SendsNothing),
            new("instrument_operations.set_alignment_projector", "user_prompt", "User Prompt", InteractiveUi, CallerOption, WhenNonEmpty, SendsNothing),
            new("instrument_operations.instrument_operational_check", "check_type", "Check Type", InteractiveUi, CallerOption, WhenNonEmpty, SendsNothing),
            new("instrument_operations.watch_closest_point", "pause_mp_until_closed", "Pause MP Until Closed", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.watch_instrument", "pause_mp_until_closed", "Pause MP Until Closed", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.watch_point_to_edge", "pause_mp_until_closed", "Pause MP Until Closed", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.watch_point_to_objects", "pause_mp_until_closed", "Pause MP Until Closed", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("instrument_operations.watch_point_to_point", "pause_mp_until_closed", "Pause MP Until Closed", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("relationship_operations.compute_geometry_relationship_uncertainties", "display_results", "Display Results", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("relationship_operations.do_relationship_fit", "use_fit_dialog", "Use Fit Dialog", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("relationship_operations.move_collections_by_minimizing_relationships", "use_fit_dialog", "Use Fit Dialog", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("relationship_operations.make_group_to_nominal_group_relationship", "display_closest_point_watch_window", "Display Closest Point Watch Window?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.add_custom_table_to_sa_report", "show_report", "Show Report?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.add_item_to_sa_report_at_location", "show_report", "Show Report?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.append_items_to_sa_report", "show_report", "Show Report?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.combine_sa_reports", "show_report", "Show Report?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.create_chart_from_vector_group", "show_interface", "Show Interface?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.define_report_template", "show_generated_report", "Show Generated Report?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.generate_quick_report_from_tab_order", "open_report", "Open Report?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.make_utility_chart", "show_chart_dialog", "Show Chart Dialog?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.output_sa_report_to_excel", "show_file", "Show File?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("reporting_operations.output_sa_report_to_pdf", "show_pdf", "Show PDF?", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("robot_operations.perform_robot_calibration", "show_interface", "Show Interface", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("robot_operations.perform_robot_calibration_alternate", "show_interface", "Show Interface", InteractiveUi, CallerOption, WhenTrue, SendsFalse),
            new("view_control.set_point_of_view_from_instrument_updates", "display_view_control", "Display View Control", InteractiveUi, CallerOption, WhenTrue, SendsTrue),
            new("utility_operations.set_interaction_mode", "sa_interaction_mode", "SA Interaction Mode", InteractiveUi, CallerOption, WhenOneOf, Rejected, "SA_INTERACTION_MODE_MANUAL", "SA_INTERACTION_MODE_AUTOMATIC"),
            new("instrument_operations.measure_single_point_here", "measure_immediately", "Measure Immediately", InteractiveUi, DefaultFlip, WhenFalse, SendsTrue),
            new("instrument_operations.configure_and_measure", "measure_immediately", "Measure Immediately", InteractiveUi, DefaultFlip, WhenFalse, SendsTrue),
            new("instrument_operations.measure_existing_single_point", "measure_immediately", "Measure Immediately", InteractiveUi, DefaultFlip, WhenFalse, SendsTrue),
            new("instrument_operations.measure_existing_single_point_and_compare", "measure_immediately", "Measure Immediately", InteractiveUi, DefaultFlip, WhenFalse, SendsTrue),
            new("instrument_operations.auto_measure_points", "auto_start", "Auto Start?", InteractiveUi, DefaultFlip, WhenFalse, SendsTrue),
            new("instrument_operations.auto_measure_points", "wait_for_completion", "Wait for Completion?", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("instrument_operations.auto_measure_batch_of_features", "wait_for_complete", "Wait for Complete", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("instrument_operations.auto_measure_surface_vector_intersections", "wait_for_complete", "Wait for Complete", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("instrument_operations.auto_measure_specified_geometry", "wait_for_complete", "Wait for Complete", BackgroundWork, DefaultFlip, WhenFalse, SendsTrue),
            new("instrument_operations.configure_and_measure", "wait_for_completion", "Wait for Completion", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("instrument_operations.synchronized_measurement_master_slave", "wait_for_completion", "Wait for Completion?", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("instrument_operations.scan_cad_faces", "wait_for_completion", "Wait for Completion", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("instrument_operations.scan_within_perimeter", "wait_for_completion", "Wait for Completion", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("construction_operations.construct_frame_with_wizard", "wait_for_completion", "Wait for Completion", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("robot_operations.move_robot_machine_through_path", "acknowledge_arrival", "Acknowledge Arrival", BackgroundWork, CallerOption, WhenFalse, SendsTrue),
            new("robot_operations.move_robot_machine_to_frame", "acknowledge_arrival", "Acknowledge Arrival", BackgroundWork, DefaultFlip, WhenFalse, SendsTrue),
            new("robot_operations.move_robot_machine_to_named_destination", "acknowledge_arrival", "Acknowledge Arrival", BackgroundWork, DefaultFlip, WhenFalse, SendsTrue)
        ];
}
