"""Reproduce the reviewed D1 operation-classification seed (#242, #293).

This script is a review aid, not a generator. It applies the name, service and
explicit per-operation rules used for the D1 decision to a descriptor dump of
each target's compiled registry and writes one CSV per target. The per-target
OperationClassification.cs tables are the source of truth; the CSVs record the
seed they were transcribed from. See README.md for provenance and usage.
"""

import argparse
import collections
import csv
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
TARGETS = ['2026.1.0529.7', '2024.1.0508.5']
PROFILES = ('read-only', 'standard', 'device', 'full')


# ---------- proto parsing: which RPC requests carry file/path inputs ----------
def proto_file_rpcs(target):
    pdir = os.path.join(REPO, 'targets', target, 'proto', 'briosa')
    msgs, rpcs = {}, {}
    for fn in sorted(os.listdir(pdir)):
        txt = open(os.path.join(pdir, fn), encoding='utf-8').read()
        for m in re.finditer(r'service\s+(\w+)\s*\{(.*?)\n\}', txt, re.S):
            for r in re.finditer(r'rpc\s+(\w+)\((\w+)\)', m.group(2)):
                rpcs[(m.group(1), r.group(1))] = r.group(2)
        for m in re.finditer(r'message\s+(\w+)\s*\{(.*?)\n\}', txt, re.S):
            msgs[m.group(1)] = m.group(2)
    out = {}
    for (svc, rpc), req in rpcs.items():
        body = msgs.get(req, '')
        hit = bool(re.search(r'\bFileReference\b', body))
        for typ, fname in re.findall(r'(\w+)\s+(\w+)\s*=\s*\d+\s*;', body):
            if typ == 'string' and ({'file', 'directory'} & set(fname.split('_'))):
                hit = True
        out[(svc, rpc)] = hit
    return out


# ---------- classification rules (as used for the D1 decision) ----------
def R(*names):
    return set(names)


INTERACTIVE = re.compile(
    r'(^ask_for_|_runtime_select$|_user_select$|^notify_user_|^status_dialog$|_drag_graphically$|'
    r'_by_clicking$|^construct_frame_pick_|^construct_frame_with_wizard$|_manual_guide$|'
    r'^load_html_form|^html_display_board$|^close_html_display_board$|^pop_poly_bay_analysis_window$)')

CODE_EXEC = R('mp_subroutines.run_subroutine', 'file_operations.terminate_all_running_mps')

EXTERNAL_IO = R(
    'utility_operations.get_opc_da_tag_value_double', 'utility_operations.get_opc_da_tag_value_integer',
    'utility_operations.get_opc_da_tag_value_string', 'utility_operations.set_opc_da_tag_value_double',
    'utility_operations.set_opc_da_tag_value_integer', 'utility_operations.set_opc_da_tag_value_string',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address',
)

# Multi-call session lifecycles (start/stop, trapping, watch, guiding, connection): ExclusiveWorkflow.
SESSION = R(
    # instrument interface / measurement modes
    'instrument_operations.start_instrument_interface', 'instrument_operations.start_theodolite_interface',
    'instrument_operations.stop_instrument_interface', 'instrument_operations.multi_measurement_initiate',
    'instrument_operations.multi_measurement_stop', 'instrument_operations.initiate_servo_guide',
    'instrument_operations.stop_active_measurement_mode', 'instrument_operations.wait_for_trapping_to_complete',
    'instrument_operations.start_gdt_inspection', 'instrument_operations.start_gdt_inspection_design',
    'instrument_operations.start_gdt_inspection_rehearse',
    'instrument_operations.enable_disable_frame_set_scan_mode_all_instruments',
    'instrument_operations.enable_disable_frame_set_scan_mode_by_instrument',
    'instrument_operations.enable_disable_point_set_scan_mode',
    'instrument_operations.move_objects_in_6d_using_instrument_updates',
    'instrument_operations.guide_objects_in_6d_based_on_point_measurements',
    'instrument_operations.auto_correspond_closest_point', 'instrument_operations.auto_correspond_with_proximity_trigger',
    'instrument_operations.close_auto_correspond_closest_point_dialog',
    'instrument_operations.watch_closest_point', 'instrument_operations.watch_instrument',
    'instrument_operations.watch_point_to_edge', 'instrument_operations.watch_point_to_objects',
    'instrument_operations.watch_point_to_point', 'instrument_operations.watch_point_to_point_with_view_zooming',
    'instrument_operations.lr_hardware_connect', 'instrument_operations.lr_hardware_disconnect',
    'instrument_operations.project_objects', 'instrument_operations.stop_projection',
    'instrument_operations.run_crib_sheet',
    'view_control.set_point_of_view_from_instrument_updates',
    # trapping in other services
    'gdt_operations.start_stop_feature_check_trapping', 'relationship_operations.start_stop_relationship_trapping',
    'robot_operations.start_stop_robot_calibration_trapping', 'utility_operations.lock_unlock_trapping_control',
    'relationship_operations.relationship_watch_window_template', 'utility_operations.close_all_watch_windows',
    # robot interface and calibration runs
    'robot_operations.start_robot_machine_interface', 'robot_operations.stop_robot_machine_interface',
    'robot_operations.perform_robot_calibration', 'robot_operations.perform_robot_calibration_alternate',
    # calibration appliance node session control
    'robot_calibration_appliance_node_operations.connect_disconnect_calibration_appliance_node',
    'robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_trap_manager',
    'robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests',
    'robot_calibration_appliance_node_operations.skip_calibration_appliance_node_measurement',
    'robot_calibration_appliance_node_operations.enable_disable_calibration_appliance_node_instrument_auto_point',
)

MOTION = R(
    'instrument_operations.point_at_target', 'instrument_operations.jump_instrument_to_new_location',
    'instrument_operations.issue_instrument_actuator_command', 'instrument_operations.initiate_servo_guide',
    'robot_operations.move_robot_machine_through_path', 'robot_operations.move_robot_machine_to_frame',
    'robot_operations.move_robot_machine_to_joint_pose_six_dof', 'robot_operations.move_robot_machine_to_named_destination',
    'robot_operations.perform_robot_calibration', 'robot_operations.perform_robot_calibration_alternate',
    'instrument_operations.auto_measure_batch_of_features', 'instrument_operations.auto_measure_points',
    'instrument_operations.auto_measure_specified_geometry', 'instrument_operations.auto_measure_surface_vector_intersections',
    'instrument_operations.auto_measure_vectors', 'instrument_operations.scan_cad_faces',
    'instrument_operations.scan_within_perimeter', 'instrument_operations.align_laser_projector',
    'instrument_operations.project_objects',
)

# Single-RPC actions or live reads against a running device (device profile).
DEVICE_ACTION = R(
    'instrument_operations.measure', 'instrument_operations.measure_existing_single_point',
    'instrument_operations.measure_existing_single_point_and_compare', 'instrument_operations.measure_nominal_feature',
    'instrument_operations.measure_single_point_here', 'instrument_operations.configure_and_measure',
    'instrument_operations.auto_measure_batch_of_features', 'instrument_operations.auto_measure_points',
    'instrument_operations.auto_measure_specified_geometry', 'instrument_operations.auto_measure_surface_vector_intersections',
    'instrument_operations.auto_measure_vectors', 'instrument_operations.edge_scan_measurement',
    'instrument_operations.scan_cad_faces', 'instrument_operations.scan_within_perimeter',
    'instrument_operations.point_at_target', 'instrument_operations.jump_instrument_to_new_location',
    'instrument_operations.issue_instrument_actuator_command', 'instrument_operations.collimation',
    'instrument_operations.drift_check', 'instrument_operations.instrument_operational_check',
    'instrument_operations.quick_align', 'instrument_operations.align_laser_projector',
    'instrument_operations.set_alignment_projector', 'instrument_operations.synchronized_measurement_master_slave',
    'instrument_operations.track_tape_measurement', 'instrument_operations.lr_apdis_activate_mcm_calibration',
    'instrument_operations.lr_apdis_get_active_mcm_calibration', 'instrument_operations.lr_apdis_perform_mcm_calibration',
    'instrument_operations.lr_get_most_recent_snr_info', 'instrument_operations.lr_self_test',
    'instrument_operations.lr_self_test_flip_test', 'instrument_operations.lr_self_test_linearization',
    'instrument_operations.lr_self_test_lo_sep', 'instrument_operations.lr_set_red_laser_intensity',
    'instrument_operations.lr_verify_hardware_connection', 'instrument_operations.verify_instrument_connection',
    'instrument_operations.get_current_instrument_position_update', 'instrument_operations.get_instrument_target_status',
    'instrument_operations.get_current_trapping_status', 'instrument_operations.get_wrtl_channel_and_status',
    'instrument_operations.set_wrtl_channel', 'instrument_operations.get_instrument_part_temperature',
    'instrument_operations.send_cloud_to_sa', 'instrument_operations.clear_cloud_viewer',
    'instrument_operations.load_cloud_viewer_point_cloud_file', 'instrument_operations.save_cloud_viewer_point_cloud_file',
    'instrument_operations.set_cloud_viewer_filter', 'instrument_operations.activate_deactivate_instrument_toolbar',
    'instrument_operations.dock_instrument_interface', 'instrument_operations.get_estimated_scan_time',
    'robot_operations.move_robot_machine_through_path', 'robot_operations.move_robot_machine_to_frame',
    'robot_operations.move_robot_machine_to_joint_pose_six_dof', 'robot_operations.move_robot_machine_to_named_destination',
    'robot_calibration_appliance_node_operations.get_calibration_appliance_node_status',
    'robot_calibration_appliance_node_operations.update_calibration_appliance_node_display_robot_joints',
)

# Configuration consumed by a live device or appliance session; no hardware action itself (device profile).
DEVICE_CONFIG = R(
    'instrument_operations.set_instrument_interface_response_timeout', 'instrument_operations.get_instrument_interface_response_timeout',
    'instrument_operations.set_instrument_measurement_mode_profile', 'instrument_operations.get_instrument_measurement_mode_profile',
    'instrument_operations.get_instrument_targets_and_mode_profiles', 'instrument_operations.set_instrument_targeting',
    'instrument_operations.get_instrument_targeting', 'instrument_operations.set_instrument_group_and_target',
    'instrument_operations.get_instrument_group_and_target', 'instrument_operations.set_probe_offset_frame_online',
    'instrument_operations.edit_scan_perimeter_profile', 'instrument_operations.set_remeasure_failed_checks_only',
    'instrument_operations.set_inspection_verification_mode', 'instrument_operations.get_inspection_verification_mode',
    'instrument_operations.set_instrument_axes', 'instrument_operations.set_instrument_weather_setting',
    'instrument_operations.get_instrument_weather_setting', 'instrument_operations.load_instrument_configuration',
    'instrument_operations.save_instrument_configuration',
    'robot_operations.set_calibration_appliance_data', 'robot_operations.set_calibration_appliance_integer_value',
    'robot_operations.set_calibration_appliance_real_value', 'robot_operations.get_calibration_appliance_data',
    'robot_operations.get_calibration_appliance_integer_value', 'robot_operations.get_calibration_appliance_real_value',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_calibration_appliance_ip_address',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_trapping_node_id',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_data',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_display_robot',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_instrument_dwell_time',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_integer_value',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_frame',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_offset_transform',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_point_group',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_profile',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_measurement_target',
    'robot_calibration_appliance_node_operations.set_calibration_appliance_node_real_value',
    'robot_calibration_appliance_node_operations.get_calibration_appliance_node_data',
    'robot_calibration_appliance_node_operations.get_calibration_appliance_node_integer_value',
    'robot_calibration_appliance_node_operations.get_calibration_appliance_node_real_value',
)

LONG_RUNNING_EXTRA = R(
    'file_operations.import_e57_file', 'file_operations.import_polyworks_file', 'file_operations.open_sa_file',
    'file_operations.save', 'file_operations.save_as', 'cloud_and_mesh_operations.generate_general_mesh',
    'construction_operations.construct_polygonized_surface_from_point_clouds', 'relationship_operations.do_relationship_fit',
    'relationship_operations.move_collections_by_minimizing_relationships', 'mp_subroutines.run_subroutine',
    'robot_operations.simulate_robot_machine_path_output_csv_file', 'reporting_operations.output_sa_report_to_pdf',
    'reporting_operations.output_sa_report_to_excel', 'file_operations.backup_now',
)

FILE_PURPOSE = re.compile(r'(^export_|^import_|^save_|^load_|^output_sa_report_to_|^capture_screen_to_file|^generate_standard_html_report$|_to_jpeg_file$|^simulate_robot_machine_path_output_csv_file$|^set_overview_image$)')
FS_DELETE = R('file_operations.delete_general_file')
FS_METADATA = R('file_operations.get_working_directory', 'file_operations.find_files_in_directory',
                'file_operations.find_sub_directories_in_directory', 'file_operations.verify_general_file_exists',
                'file_operations.verify_mp_file_exists')
DESTRUCTIVE_EXTRA = R(
    'construction_operations.clear_hidden_point_bar_database', 'variables.clear_named_double_list_variable',
    'reporting_operations.clear_custom_table', 'reporting_operations.close_all_reports',
    'cloud_and_mesh_operations.clear_cloud_point_deviations', 'mp_task_overview.create_clear_task_overview_list',
    'file_operations.new_sa_file', 'file_operations.open_sa_file', 'file_operations.open_template_file',
    'file_operations.terminate_all_running_mps', 'utility_operations.trim_log_file',
    'file_operations.delete_general_file', 'instrument_operations.clear_cloud_viewer',
    'robot_calibration_appliance_node_operations.clear_calibration_appliance_node_trap_manager_requests',
)

WRITE_VERB = re.compile(r'(^export_|^save|_save_|^set_\w+_in_data_share_file$|^backup_now$|^output_sa_report_to_|'
                        r'^capture_screen_to_file|^save_current_view|^simulate_robot_machine_path_output_csv_file$|'
                        r'^trim_log_file$|^write_to_log$|^copy_general_file$|^rename_general_file$|'
                        r'^merge_measurements_into_xml_file$|^prepare_qdas_data_list$|^generate_standard_html_report$|^export|_to_jpeg_file$|'
                        r'^best_fit_transformation_group_to_group$|^locate_instrument_best_fit_|^new_raster_scan_edge_inspection$)')
READ_VERB = re.compile(r'(^import_|^open_|^load_|_from_data_share_file$|^copy_general_file$|^merge_measurements_into_xml_file$|'
                       r'^direct_cad_access$|^use_nrkxml_library$|^run_subroutine$|^get_qdas_catalog_entries$|'
                       r'^html_display_board$|^notify_user_html$|^set_overview_image$|^ask_for_user_decision_from_image$|'
                       r'^create_picture_callout$|^define_report_template$|^add_item_to_sa_report_at_location$)')

# ---------- corrections made while reviewing the seed for #293 step 1 ----------
# Each entry is applied after the decision rules above. Keep reasons in README.md.
REVIEW_ADD_FLAGS = {
    # The manual-guide measurement drives a live instrument, like the other measure_* device actions.
    'instrument_operations.measure_existing_single_point_manual_guide': {'device_session'},
    # The form writes its results to the caller-named output data-share file.
    'file_operations.load_html_form': {'filesystem_write'},
    'file_operations.load_html_form_in_edge_browser': {'filesystem_write'},
}
REVIEW_REMOVE_FLAGS = {
    # file_name_pattern filters files embedded in the SA job; no filesystem path is read.
    'file_operations.make_embedded_file_name_list': {'filesystem_read'},
}

FS_FLAGS = {'filesystem_write', 'filesystem_read', 'filesystem_metadata', 'filesystem_delete'}


def classify(o, file_rpcs):
    oid = o['id']
    svc, name = oid.split('.', 1)
    flags = set()
    has_file = file_rpcs.get((o['service'].split('.', 1)[1], o['rpc']), False)
    # family (one primary family per operation; a review aid only)
    if oid in CODE_EXEC:
        fam = 'code_execution'
    elif INTERACTIVE.search(name):
        fam = 'interactive_ui'
    elif oid in SESSION:
        fam = 'session_lifecycle'
    elif oid in DEVICE_ACTION:
        fam = 'device_action'
    elif oid in DEVICE_CONFIG:
        fam = 'device_config'
    elif svc == 'utility_operations' and 'opc_da' in name:
        fam = 'external_io'
    elif svc == 'file_operations' or (has_file and FILE_PURPOSE.search(name)):
        fam = 'file_io'
    elif name.startswith('delete_') or oid in DESTRUCTIVE_EXTRA:
        fam = 'destructive_model'
    elif o['effect'] == 'read_only':
        fam = 'pure_read'
    else:
        fam = 'model_mutation'
    # flags
    if oid in CODE_EXEC:
        flags.add('code_execution')
    if INTERACTIVE.search(name):
        flags.add('interactive_ui')
    if oid in SESSION or oid in DEVICE_ACTION:
        flags.add('device_session')
    if oid in DEVICE_CONFIG:
        flags.add('device_config')
    if oid in MOTION:
        flags.add('physical_motion')
    if oid in EXTERNAL_IO:
        flags.add('external_io')
    if oid in FS_METADATA:
        flags.add('filesystem_metadata')
    if oid in FS_DELETE:
        flags.add('filesystem_delete')
    if has_file or svc == 'file_operations':
        if WRITE_VERB.search(name):
            flags.add('filesystem_write')
        if READ_VERB.search(name):
            flags.add('filesystem_read')
        if has_file and not (FS_FLAGS & flags):
            flags.add('filesystem_read')  # conservative: any path input reads unless reviewed
    if svc == 'file_operations' and not (FS_FLAGS & flags):
        if name in ('save', 'save_as', 'save_as_read_only_template', 'backup_now'):
            flags.add('filesystem_write')
    if name.startswith('delete_') or oid in DESTRUCTIVE_EXTRA or 'destructive' in o['flags']:
        flags.add('destructive')
    flags |= REVIEW_ADD_FLAGS.get(oid, set())
    flags -= REVIEW_REMOVE_FLAGS.get(oid, set())
    # duration
    if 'interactive_ui' in flags:
        duration = 'interactive'
    elif oid in SESSION or oid in DEVICE_ACTION or oid in LONG_RUNNING_EXTRA or 'long-running' in o['flags'] \
            or 'physical_motion' in flags:
        duration = 'long_running'
    else:
        duration = 'quick'
    # Decision on #242: interactive operations are not exclusive; only multi-call session lifecycles are.
    isolation = 'exclusive_workflow' if oid in SESSION else 'admissible'
    # validation status (moved out of the risk flags)
    if 'at-risk-no-runtime-validation' in o['flags']:
        validation = 'at_risk_unvalidated'
    elif 'fixture_validation_pending' in o['flags']:
        validation = 'fixture_pending'
    else:
        validation = 'no_recorded_gap'
    return dict(id=oid, effect=o['effect'], family=fam, flags=sorted(flags), duration=duration,
                isolation=isolation, validation=validation, legacy_flags=list(o['flags']))


# ---------- profiles (D1 proposal section 3.3, as amended by the #242 decision) ----------
# A profile admits an operation when every flag is in its admitted set and its effect filter
# passes. interactive_ui is in no set (explicit opt-in only); exclusive_workflow is never admitted.
READ_ONLY_FLAGS = {'filesystem_metadata'}
STANDARD_FLAGS = READ_ONLY_FLAGS | {'filesystem_read', 'filesystem_write', 'destructive'}
DEVICE_FLAGS = STANDARD_FLAGS | {'device_session', 'device_config', 'physical_motion'}
FULL_FLAGS = DEVICE_FLAGS | {'external_io', 'code_execution', 'filesystem_delete'}


def profiles(c):
    f = set(c['flags'])
    if c['isolation'] != 'admissible':
        return {p: False for p in PROFILES}
    return {
        'read-only': c['effect'] == 'read_only' and f <= READ_ONLY_FLAGS,
        'standard': f <= STANDARD_FLAGS,
        'device': f <= DEVICE_FLAGS,
        'full': f <= FULL_FLAGS,
    }


def write_csv(path, rows):
    with open(path, 'w', encoding='utf-8', newline='\n') as handle:
        writer = csv.writer(handle, lineterminator='\n')
        writer.writerow(['operation_id', 'effect', 'family', 'risk_flags', 'duration_class',
                         'validation_status', 'isolation', 'legacy_descriptor_flags', *PROFILES])
        for c in sorted(rows, key=lambda r: r['id']):
            writer.writerow([c['id'], c['effect'], c['family'], ';'.join(c['flags']), c['duration'],
                             c['validation'], c['isolation'], ';'.join(c['legacy_flags']),
                             *[str(c['profiles'][p]).lower() for p in PROFILES]])


def summarize(target, rows):
    counter = collections.Counter
    print(f'===== {target} ({len(rows)} operations)')
    print('  flags     ', dict(sorted(counter(f for c in rows for f in c['flags']).items())))
    print('  duration  ', dict(sorted(counter(c['duration'] for c in rows).items())))
    print('  validation', dict(sorted(counter(c['validation'] for c in rows).items())))
    print('  isolation ', dict(sorted(counter(c['isolation'] for c in rows).items())))
    print('  profiles  ', {p: sum(c['profiles'][p] for c in rows) for p in PROFILES})


def main(argv):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--descriptors-dir', required=True,
                        help='directory holding descriptors-<target>.json from Export-OperationDescriptors.ps1')
    parser.add_argument('--output-dir', default=HERE, help='directory for operation-classification-<target>.csv')
    parser.add_argument('--target', action='append', choices=TARGETS, help='limit to one target (repeatable)')
    args = parser.parse_args(argv)
    for target in args.target or TARGETS:
        with open(os.path.join(args.descriptors_dir, f'descriptors-{target}.json'), encoding='utf-8-sig') as handle:
            descriptors = json.load(handle)
        file_rpcs = proto_file_rpcs(target)
        rows = [classify(o, file_rpcs) for o in descriptors]
        if len({c['id'] for c in rows}) != len(rows):
            raise SystemExit(f'{target}: the descriptor dump contains duplicate operation ids')
        for c in rows:
            c['profiles'] = profiles(c)
        write_csv(os.path.join(args.output_dir, f'operation-classification-{target}.csv'), rows)
        summarize(target, rows)


if __name__ == '__main__':
    main(sys.argv[1:])
