"""Audit which optional scalar request fields ignore presence (F7, #243).

This is a review aid, like classify.py. It reads each exact-SA target's
handwritten protobuf contracts (proto/briosa/*.proto), operation mappings
(src/Briosa.Server/Operations/**) and classification table
(src/Briosa.Server/Security/OperationClassification.cs), and writes one CSV per
target, scalar-presence-<target>.csv, listing:

- every `optional` scalar request field whose presence the mapping never
  checks, so an omitted field is sent as the protobuf zero value
  (presence `ignored`), or is rejected by a later check
  (presence `ignored_validated`); and
- every `optional` string request field whose presence is checked but whose
  absent value is still sent as an empty string (presence
  `checked_sends_blank`), which D2 rule 1 (#243) does not allow for changed
  arguments.

It does not generate, register, select or change operations, request fields,
mappings or tests (AGENTS.md invariant 6), and no build or test reads its
output. The method is static text matching on the handwritten C#; its limits
are described in argument-defaults.md. REVIEW_FLAGS records the risky defaults
shortlisted for the maintainer; the reasons are in argument-defaults.md.

Run from the repository root with Python 3.12 or later:

    python eng/classification/audit_scalar_presence.py
    python eng/classification/audit_scalar_presence.py --check
"""

from __future__ import annotations

import argparse
import csv
import io
import re
import sys
from pathlib import Path

TARGETS = ('2026.1.0529.7', '2024.1.0508.5')

SCALAR_TYPES = {
    'bool', 'double', 'float', 'int32', 'int64', 'uint32', 'uint64', 'sint32',
    'sint64', 'fixed32', 'fixed64', 'sfixed32', 'sfixed64', 'string', 'bytes',
}

# The worker value constructors that pass a request scalar through unchanged,
# and the protobuf zero value each one then sends for an omitted field.
ZERO_VALUE_WRAPPERS = {
    'WorkerBooleanValue': 'false',
    'WorkerDoubleValue': '0',
    'WorkerIntegerValue': '0',
    'WorkerTextValue': 'empty string',
}

# OperationAdmissionProfile in each target: the risks each profile admits.
PROFILE_RISKS = (
    ('read-only', {'FilesystemMetadata'}),
    ('standard', {'FilesystemMetadata', 'FilesystemRead', 'FilesystemWrite', 'Destructive'}),
    ('device', {'FilesystemMetadata', 'FilesystemRead', 'FilesystemWrite', 'Destructive',
                'DeviceSession', 'DeviceConfig', 'PhysicalMotion'}),
    ('full', {'FilesystemMetadata', 'FilesystemRead', 'FilesystemWrite', 'Destructive',
              'DeviceSession', 'DeviceConfig', 'PhysicalMotion', 'ExternalIo',
              'CodeExecution', 'FilesystemDelete'}),
)


def _flag(flag: str, operation_id: str, *fields: str) -> dict[tuple[str, str], str]:
    return {(operation_id, field): flag for field in fields}


# Reviewed shortlist of risky absent defaults (#243 F7), limited to operations
# some profile admits. Keys are (operation id, request field); values are the
# shortlist IDs explained in argument-defaults.md. A key whose operation a
# target does not register is ignored; any other unmatched key fails.
REVIEW_FLAGS: dict[tuple[str, str], str] = {
    **_flag('R1', 'robot_operations.move_robot_machine_to_joint_pose_six_dof',
            'joint_1', 'joint_2', 'joint_3', 'joint_4', 'joint_5', 'joint_6'),
    **_flag('R2', 'instrument_operations.set_instrument_weather_setting',
            'temperature', 'pressure', 'relative_humidity', 'set_automatically'),
    **_flag('R3', 'utility_operations.scale_objects', 'scale_factor'),
    **_flag('R4', 'instrument_operations.set_absolute_instrument_scale_factor', 'scale_factor'),
    **_flag('R4', 'instrument_operations.set_multiply_instrument_scale_factor', 'scale_factor'),
    **_flag('R5', 'robot_operations.set_robot_machine_model_link_parameters',
            'dh_alpha_component', 'dh_a_component', 'dh_d_component', 'dh_theta_component',
            'dh_x_axis_deflection_factor', 'dh_y_axis_deflection_factor',
            'dh_z_axis_deflection_factor', 'six_dof_x_component', 'six_dof_y_component',
            'six_dof_z_component', 'six_dof_rx_component', 'six_dof_ry_component',
            'six_dof_rz_component', 'encoder_offset_value', 'minimum_encoder_limit',
            'maximum_encoder_limit', 'additional_encoder_index_offset',
            'segment_origin_mass_kg', 'segment_cg_mass_kg', 'encoder_sense_negative',
            'include_additional_encoder', 'additional_encoder_sense_negative'),
    **_flag('R5', 'robot_operations.set_robot_machine_parameter', 'parameter_value'),
    **_flag('R6', 'instrument_operations.delete_measurement_observation', 'observation_index'),
    **_flag('R6', 'instrument_operations.move_measurement_observation', 'observation_index'),
    **_flag('R6', 'vector_operations.delete_ith_vector_from_vector_group', 'vector_index'),
    **_flag('R7', 'utility_operations.delete_folder', 'folder_path'),
    **_flag('R7', 'variables.delete_variable', 'name'),
    **_flag('R7', 'variables.delete_variables_wildcard_match', 'variable_wildcard_criteria'),
    **_flag('R7', 'vector_operations.delete_vector_by_name', 'vector_name'),
    **_flag('R7', 'construction_operations.delete_collections_by_wildcard', 'search_string'),
    **_flag('R7', 'construction_operations.delete_folders_by_wildcard', 'search_string'),
    **_flag('R7', 'robot_operations.delete_robot_calibration', 'calibration_name'),
    **_flag('R7', 'reporting_operations.remove_report_tag', 'tag_name'),
    **_flag('R8', 'instrument_operations.auto_measure_vectors',
            'angle_tolerance', 'high_tolerance', 'low_tolerance'),
    **_flag('R8', 'scale_bar_operations.scale_bar_check',
            'current_temperature', 'length_of_bar_at_68f', 'material_cte', 'tolerance'),
    **_flag('R8', 'instrument_operations.locate_instrument_ref_tie_in', 'tolerance'),
    **_flag('R8', 'construction_operations.construct_vector_group_area_profile_check',
            'area_radius', 'area_tolerance'),
    **_flag('R8', 'instrument_operations.construct_tcp_fixture', 'point_match_threshold'),
    **_flag('R9', 'construction_operations.construct_scale_bar', 'length'),
    **_flag('R9', 'instrument_operations.set_instrument_transform', 'number_of_steps'),
    **_flag('R9', 'instrument_operations.transform_instrument_frame_to_frame', 'number_of_steps'),
    **_flag('R9', 'robot_operations.set_robot_machine_base_transform', 'number_of_steps'),
    **_flag('R9', 'instrument_operations.lr_self_test_lo_sep', 'region', 'num_range_measurements'),
    **_flag('R9', 'instrument_operations.set_ladar_auto_meas_point', 'sample_time_milliseconds'),
    **_flag('R9', 'instrument_operations.lr_set_red_laser_intensity', 'intensity'),
    **_flag('R10', 'instrument_operations.measure_existing_single_point_manual_guide',
            'measure_immediately'),
    **_flag('R10', 'utility_operations.set_logging_state', 'active'),
    **_flag('R10', 'utility_operations.set_auto_event_creation', 'active'),
    **_flag('R10', 'utility_operations.set_automatic_relationship_construction_state', 'active'),
    **_flag('R10', 'dimension_operations.set_dimension_tolerance',
            'enable_nominal', 'enable_high', 'enable_low'),
    **_flag('R11', 'process_flow_operations.ask_for_user_decision_from_strings',
            'button1_text', 'button2_text', 'button3_text'),
}

COLUMNS = (
    'operation_id', 'request_message', 'field', 'field_number', 'proto_type', 'presence',
    'mp_argument', 'sdk_setter', 'sent_when_absent', 'narrowest_profile', 'review_flag',
)

STRING_LITERAL = re.compile(r'"((?:[^"\\]|\\.)*)"')


def pascal(name: str) -> str:
    """The C# property name protoc gives a snake_case field."""
    out = ''.join(part[0].upper() + part[1:] for part in name.split('_') if part)
    return re.sub(r'(\d)([a-z])', lambda m: m.group(1) + m.group(2).upper(), out)


def parse_requests(target: Path) -> dict[str, list[tuple[str, str, str, int]]]:
    messages: dict[str, list[tuple[str, str, str, int]]] = {}
    for proto in sorted((target / 'proto' / 'briosa').glob('*.proto')):
        text = proto.read_text(encoding='utf-8')
        for match in re.finditer(r'^message (\w+) \{(?:\}|(.*?)^\})', text, re.M | re.S):
            fields = []
            for field in re.finditer(
                    r'^\s*(optional|repeated)?\s*([\w.]+)\s+(\w+)\s*=\s*(\d+)',
                    match.group(2) or '', re.M):
                label, kind, name, number = field.groups()
                fields.append((label or 'implicit', kind, name, int(number)))
            messages[match.group(1)] = fields
    return {name: fields for name, fields in messages.items() if name.endswith('Request')}


def parse_classification(target: Path) -> dict[str, tuple[set[str], str]]:
    path = target / 'src' / 'Briosa.Server' / 'Security' / 'OperationClassification.cs'
    rows = {}
    pattern = re.compile(r'^\s*new\("([a-z0-9_]+\.[a-z0-9_]+)",\s*([^,]+),\s*\w+,\s*\w+,\s*(\w+)\)')
    for line in path.read_text(encoding='utf-8').splitlines():
        match = pattern.match(line)
        if match:
            flags = {flag.strip() for flag in match.group(2).split('|')} - {'None'}
            rows[match.group(1)] = (flags, match.group(3))
    return rows


def narrowest_profile(flags: set[str], isolation: str, read_only_effect: bool) -> str:
    if isolation == 'ExclusiveWorkflow':
        return 'never (exclusive_workflow)'
    risks = flags - {'InteractiveUi'}
    suffix = ' + interactive opt-in' if 'InteractiveUi' in flags else ''
    for name, admitted in PROFILE_RISKS:
        if name == 'read-only' and not read_only_effect:
            continue
        if risks <= admitted:
            return name + suffix
    return 'none'


def enclosing_open_paren(text: str, position: int) -> int:
    depth = 0
    for index in range(position - 1, -1, -1):
        char = text[index]
        if char == ')':
            depth += 1
        elif char == '(':
            if depth == 0:
                return index
            depth -= 1
        elif char == ';' and depth == 0:
            return -1
    return -1


def matching_close_paren(text: str, position: int) -> int:
    depth = 0
    for index in range(position, len(text)):
        if text[index] == '(':
            depth += 1
        elif text[index] == ')':
            depth -= 1
            if depth == 0:
                return index
    return -1


def uses(text: str, variable: str, prop: str) -> list[tuple[str, str, str, str]]:
    """(wrapper, MP label, SDK setter, statement) for each use of variable.prop."""
    constants = re.findall(r'const string (\w+) = "((?:[^"\\]|\\.)*)"', text)
    found = []
    for match in re.finditer(rf'\b{variable}\.{prop}\b', text):
        open_paren = enclosing_open_paren(text, match.start())
        wrapper = ''
        if open_paren >= 0:
            callee = re.search(r'([\w.<>]+)\s*$', text[:open_paren])
            wrapper = callee.group(1) if callee else ''
        start = max(text.rfind(';', 0, match.start()), text.rfind('{', 0, match.start()),
                    text.rfind('}', 0, match.start()))
        statement = text[start + 1:text.find(';', match.end())]
        label = setter = ''
        outer = enclosing_open_paren(text, open_paren) if open_paren >= 0 else -1
        while outer >= 0:
            arguments = text[outer + 1:matching_close_paren(text, outer)]
            for name, value in constants:
                arguments = re.sub(rf'\b{name}\b', '"' + value + '"', arguments)
            first = re.match(r'\s*"((?:[^"\\]|\\.)*)"', arguments)
            if first:
                label = first.group(1).replace('\\"', '"')
                setters = [s for s in STRING_LITERAL.findall(arguments) if re.fullmatch(r'Set\w+', s)]
                setter = setters[-1] if setters else ''
                break
            outer = enclosing_open_paren(text, outer)
        found.append((wrapper, label, setter, statement))
    return found


def audit(root: Path, target_name: str) -> list[dict[str, str]]:
    target = root / 'targets' / target_name
    requests = parse_requests(target)
    classification = parse_classification(target)
    operations = {}
    for source in sorted((target / 'src' / 'Briosa.Server' / 'Operations').rglob('*.cs')):
        text = source.read_text(encoding='utf-8')
        for match in re.finditer(r'CreateCommand\(\s*Api\.(\w+Request)\s+(\w+)\s*\)', text):
            operations[match.group(1)] = (text, match.group(2))
    rows = []
    for message, fields in sorted(requests.items()):
        if message not in operations:
            continue
        text, variable = operations[message]
        operation_id = re.search(r'"([a-z_]+\.[a-z0-9_]+)"', text).group(1)
        flags, isolation = classification[operation_id]
        read_only = '"read_only"' in text or 'ReadOnlyDescriptor(' in text
        for label, kind, name, number in fields:
            if label != 'optional' or kind not in SCALAR_TYPES:
                continue
            prop = pascal(name)
            found = uses(text, variable, prop)
            checked = re.search(rf'\b{variable}\.Has{prop}\b', text) is not None
            if checked:
                blank = [u for u in found if u[0] == 'WorkerTextValue' and re.search(
                    rf'\b{variable}\.Has{prop}\s*\?\s*{variable}\.{prop}\s*:\s*(string\.Empty|"")',
                    u[3])]
                if kind != 'string' or not blank:
                    continue
                presence, sent, labelled = 'checked_sends_blank', 'empty string', blank
            elif not found:
                continue
            else:
                passthrough = [u for u in found if u[0] in ZERO_VALUE_WRAPPERS]
                validated = len(passthrough) != len(found)
                presence = 'ignored_validated' if validated else 'ignored'
                sent = ('rejected before dispatch' if validated
                        else ZERO_VALUE_WRAPPERS[passthrough[0][0]])
                labelled = passthrough
            labels = sorted({u[1] for u in labelled if u[1]})
            setters = sorted({u[2] for u in labelled if u[2]})
            rows.append({
                'operation_id': operation_id,
                'request_message': message,
                'field': name,
                'field_number': str(number),
                'proto_type': kind,
                'presence': presence,
                'mp_argument': ' | '.join(labels),
                'sdk_setter': ' | '.join(setters),
                'sent_when_absent': sent,
                'narrowest_profile': narrowest_profile(flags, isolation, read_only),
                'review_flag': REVIEW_FLAGS.get((operation_id, name), ''),
            })
    rows.sort(key=lambda row: (row['operation_id'], int(row['field_number'])))
    return rows


def render(rows: list[dict[str, str]]) -> str:
    buffer = io.StringIO()
    writer = csv.DictWriter(buffer, fieldnames=COLUMNS, lineterminator='\n')
    writer.writeheader()
    writer.writerows(rows)
    return buffer.getvalue()


def main() -> int:
    here = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--repository-root', type=Path, default=here.parent.parent)
    parser.add_argument('--output-dir', type=Path, default=here)
    parser.add_argument('--check', action='store_true',
                        help='Compare with the committed CSVs instead of writing them.')
    args = parser.parse_args()
    differences = 0
    for target in TARGETS:
        rows = audit(args.repository_root, target)
        unmatched = [k for k in REVIEW_FLAGS if k[0] in {r['operation_id'] for r in rows}
                     and k not in {(r['operation_id'], r['field']) for r in rows}]
        for operation_id, field in unmatched:
            print(f'{target}: review flag {operation_id}.{field} matches no row', file=sys.stderr)
            differences += 1
        content = render(rows)
        path = args.output_dir / f'scalar-presence-{target}.csv'
        counts = {}
        for row in rows:
            counts[row['presence']] = counts.get(row['presence'], 0) + 1
        summary = ', '.join(f'{key} {value}' for key, value in sorted(counts.items()))
        if args.check:
            committed = path.read_text(encoding='utf-8') if path.exists() else ''
            if committed != content:
                print(f'{path.name} is out of date ({summary})', file=sys.stderr)
                differences += 1
            else:
                print(f'{path.name} is current ({summary})')
        else:
            path.write_text(content, encoding='utf-8', newline='\n')
            print(f'Wrote {path.name} ({summary})')
    return 1 if differences else 0


if __name__ == '__main__':
    sys.exit(main())
