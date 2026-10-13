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

It also writes optional-inputs-<target>.csv: every optional MP input in the
target's evidence (the 2026 inventory's `optional` presence; the 2024 curated
labels that say optional), matched to a registered operation by exact MP step
text and to an argument entry by exact SDK label and setter, with what the
server does when the field is absent.

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
import json
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


# Reviewed shortlist of risky absent defaults (#243 F7) that are still under
# review, limited to operations some profile admits. Keys are (operation id,
# request field); values are the shortlist IDs explained in argument-defaults.md.
# R1 to R7 and the R10 manual-guide default were decided on 2026-10-13: those
# fields are no longer presence-insensitive, and the defaults table records them.
# A key whose operation a target does not register is ignored; any other
# unmatched key fails.
REVIEW_FLAGS: dict[tuple[str, str], str] = {
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


# --- Optional MP input coverage (#243 F6 cross-check) ---------------------------------
#
# Each optional MP input recorded by a target's exact-target evidence is matched to
# a registered operation and a literal argument entry in that operation's source.
# No name normalization is applied. Where the evidence records the same command or
# argument under two identities that the import did not join, the reviewed aliases
# below link them; each names both exact texts.

OPTIONAL_INPUT_COLUMNS = (
    'evidence_command', 'evidence_argument', 'presence_evidence', 'operation_id',
    'descriptor_step', 'step_match', 'sdk_label', 'sdk_setter', 'label_match', 'status',
    'defaults_table',
)

# Documentation entries in the 2026 inventory whose SDK step was recorded as a
# separate SDK-only inventory entry (no sdk_evidence on the documentation entry).
REVIEWED_STEP_ALIASES = {
    'Set LADAR Feature Measure Circle': 'Set LADAR FeatureMeas Circle',
    'Set LADAR Feature Measure Cylinder': 'Set LADAR FeatureMeas Cylinder',
    'Set LADAR Feature Measure Slot': 'Set LADAR FeatureMeas Slot',
}

# Documented optional inputs whose inventory row has no SDK setter, keyed by
# (inventory mp_step, documented mp_name), mapped to the not_documented SDK row of
# the same inventory command that carries the setter: (SDK label, setter).
REVIEWED_LABEL_ALIASES = {
    ('Construct Planes, Bounding Point Group',
     'Resulting \u2018High\u2019 Plane Name (optional)'): ("Resulting 'High' Plane Name", 'SetCollectionObjectNameArg2'),
    ('Construct Planes, Bounding Point Group',
     'Resulting \u2018Low\u2019 Plane Name (optional)'): ("Resulting 'Low' Plane Name", 'SetCollectionObjectNameArg2'),
    ('Create Vector Callout', 'Additional Note (blank for none)'):
        ('Additional Notes (blank for none)', 'SetEditTextArg'),
    ('Set Geom Relationship Auto Measure Nominal Feature', 'Measurement Mode (optional)'):
        ('Measurement Mode', 'SetStringArg'),
}

# 2024 has no committed inventory, so presence comes from the curated label.
OPTIONAL_LABEL = re.compile(r'optional|blank for none', re.IGNORECASE)
ADAPTER_SETTERS = {'SetCollectionObjectNameArg': 'SetCollectionObjectNameArg2'}


def _unescape(literal: str) -> str:
    return literal.replace('\\"', '"').replace('\\\\', '\\')


def _csharp_literal(text: str) -> str:
    return '"' + text.replace('\\', '\\\\').replace('"', '\\"') + '"'


def descriptor_steps(target: Path) -> dict[str, tuple[str, Path]]:
    """Registered operation id -> (descriptor MP step, declaring source file)."""
    registered = set(parse_classification(target))
    found: dict[str, set[tuple[str, Path]]] = {}
    for source in sorted((target / 'src' / 'Briosa.Server' / 'Operations').rglob('*.cs')):
        text = source.read_text(encoding='utf-8')
        pairs = [(m.group(1), _unescape(m.group(2))) for m in re.finditer(
            r'"([a-z_]+\.[a-z0-9_]+)"\s*,\s*"((?:[^"\\]|\\.)*)"', text)]
        operation = re.search(r'const string OperationId =\s*"([a-z_]+\.[a-z0-9_]+)"', text)
        step = re.search(r'const string StepName =\s*"((?:[^"\\]|\\.)*)"', text)
        if operation and step:
            pairs.append((operation.group(1), _unescape(step.group(1))))
        for operation_id, mp_step in pairs:
            if operation_id in registered:
                found.setdefault(operation_id, set()).add((mp_step, source))
    if set(found) != registered or any(len(value) != 1 for value in found.values()):
        raise SystemExit(f'{target.name}: could not read exactly one descriptor step per operation')
    return {operation_id: next(iter(value)) for operation_id, value in found.items()}


def _open_before(text: str, position: int) -> int:
    depth = 0
    for index in range(position - 1, -1, -1):
        if text[index] in ')]}':
            depth += 1
        elif text[index] in '([{':
            if depth == 0:
                return index
            depth -= 1
    return -1


def _close_after(text: str, position: int) -> int:
    depth = 0
    for index in range(position, len(text)):
        if text[index] in '([{':
            depth += 1
        elif text[index] in ')]}':
            depth -= 1
            if depth == 0:
                return index
    return -1


def _previous(text: str, index: int) -> int:
    index -= 1
    while index >= 0 and text[index].isspace():
        index -= 1
    return index


def _guarded_by_if(text: str, close_paren: int) -> bool:
    depth = 0
    for index in range(close_paren, -1, -1):
        if text[index] in ')]}':
            depth += 1
        elif text[index] in '([{':
            depth -= 1
            if depth == 0:
                return re.search(r'\bif\s*$', text[:index]) is not None
    return False


def _guard(text: str, entry: int) -> bool:
    """True when the argument entry is added only under a condition."""
    outer = _open_before(text, entry)
    while outer >= 0:
        head = text[:outer]
        if text[outer] == '(' and re.search(r'\.Add(Range)?\s*$', head):
            receiver = re.search(r'[\w.]+\s*$', head)
            before = _previous(text, receiver.start())
            if before >= 0 and text[before] == '{':
                before = _previous(text, before)
            return before >= 0 and text[before] == ')' and _guarded_by_if(text, before)
        if text[outer] in '[{' and re.search(
                r'\?\s*(new\s*[\w<>]*\s*\[\s*\]\s*)?$', text[:_previous(text, outer) + 1]):
            return True
        outer = _open_before(text, outer)
    return False


def input_status(text: str, label: str, setter: str) -> str:
    """Absent behaviour of the argument entry whose first argument is the label."""
    constants = {name: _unescape(value) for name, value in
                 re.findall(r'const string (\w+) =\s*"((?:[^"\\]|\\.)*)"', text)}
    patterns = [re.escape(_csharp_literal(label))] + [
        rf'\b{name}\b' for name, value in constants.items() if value == label]
    entries = []
    for pattern in patterns:
        for match in re.finditer(pattern, text):
            open_paren = _open_before(text, match.start())
            if open_paren >= 0 and text[open_paren] == '(' and not text[open_paren + 1:match.start()].strip():
                entries.append(open_paren)
    setter_names = [_csharp_literal(setter)] + [
        name for name, value in constants.items() if value == setter]
    entries = [entry for entry in entries
               if any(name in text[entry:_close_after(text, entry) + 1] for name in setter_names)]
    if len(entries) != 1:
        return 'not_sent'
    entry = entries[0]
    if _guard(text, entry):
        return 'omitted_when_absent'
    if re.search(r'\.Required(List)?\(', text[entry:_close_after(text, entry) + 1]):
        return 'rejected_when_absent'
    return 'default_sent'


def _defaults_table(root: Path, target_name: str) -> dict[tuple[str, str], str]:
    path = root / 'eng' / 'classification' / f'argument-defaults-{target_name}.csv'
    with path.open(encoding='utf-8', newline='') as handle:
        return {(row['operation_id'], row['mp_argument']): row['category']
                for row in csv.DictReader(handle)}


def optional_inputs(root: Path, target_name: str) -> list[dict[str, str]]:
    target = root / 'targets' / target_name
    steps = descriptor_steps(target)
    by_step: dict[str, list[str]] = {}
    for operation_id, (mp_step, _) in steps.items():
        by_step.setdefault(mp_step, []).append(operation_id)
    defaults = _defaults_table(root, target_name)
    rows = []

    def add(command, argument, presence, operation_id, step_match, label, setter, label_match):
        row = {'evidence_command': command, 'evidence_argument': argument,
               'presence_evidence': presence, 'operation_id': operation_id or '',
               'descriptor_step': steps[operation_id][0] if operation_id else '',
               'step_match': step_match, 'sdk_label': label or '', 'sdk_setter': setter or '',
               'label_match': label_match}
        if not operation_id:
            row['status'] = 'not_implemented'
        elif not setter:
            row['status'] = 'no_sdk_setter'
        else:
            text = steps[operation_id][1].read_text(encoding='utf-8')
            row['status'] = input_status(text, label, setter)
        row['defaults_table'] = defaults.get((operation_id, label), '') if operation_id else ''
        rows.append(row)

    inventory_path = target / 'inventory' / 'sa' / target_name / 'inventory.json'
    if inventory_path.exists():
        inventory = json.loads(inventory_path.read_text(encoding='utf-8'))
        sdk_only_arguments = {
            command['mp_step']: {argument['mp_name']: argument['sdk_binding']['setter']
                                 for argument in command['arguments']
                                 if argument['direction'] != 'output'
                                 and argument['sdk_binding']['setter']['status'] == 'available'}
            for command in inventory['commands']
            if command['inventory_key'].startswith('sdk:')
            and command['mp_step'] in REVIEWED_STEP_ALIASES.values()}
        for command in inventory['commands']:
            identities = [(command['mp_step'], 'inventory_mp_step')]
            identities += [(evidence['mp_step'], 'sdk_evidence_mp_step')
                           for evidence in command.get('sdk_evidence', []) if evidence.get('mp_step')]
            if command['mp_step'] in REVIEWED_STEP_ALIASES:
                identities.append((REVIEWED_STEP_ALIASES[command['mp_step']], 'reviewed_step_alias'))
            matched = []
            for mp_step, how in identities:
                for operation_id in by_step.get(mp_step, []):
                    if operation_id not in [m[0] for m in matched]:
                        matched.append((operation_id, how))
            for argument in command['arguments']:
                if argument['presence'] != 'optional' or argument['direction'] == 'output':
                    continue
                setter = argument['sdk_binding']['setter']
                label, method, label_match = setter['argument_name'], setter['method'], 'exact'
                alias = REVIEWED_LABEL_ALIASES.get((command['mp_step'], argument['mp_name']))
                sdk_only = sdk_only_arguments.get(REVIEWED_STEP_ALIASES.get(command['mp_step']), {})
                if alias:
                    (label, method), label_match = alias, 'reviewed_label_alias'
                elif setter['status'] != 'available' and argument['mp_name'] in sdk_only:
                    # The same argument name in the SDK-only entry of an aliased step.
                    sdk_setter = sdk_only[argument['mp_name']]
                    label, method, label_match = sdk_setter['argument_name'], sdk_setter['method'], 'sdk_only_entry'
                elif setter['status'] != 'available':
                    method = None
                for operation_id, how in matched or [(None, 'none')]:
                    add(command['mp_step'], argument['mp_name'], 'inventory presence optional',
                        operation_id, how, label, method, label_match)
    else:
        facts = json.loads((target / 'evidence' / 'sa' / target_name / 'bindings.json')
                           .read_text(encoding='utf-8'))
        for operation in facts['operations']:
            if operation['operation_id'] not in steps:
                continue
            exact_step = steps[operation['operation_id']][0] == operation['mp_step']
            for argument in operation['arguments']:
                if argument['direction'] == 'output' or not OPTIONAL_LABEL.search(argument['sdk_name']):
                    continue
                method = argument['setter']['method'] if argument['setter']['status'] == 'available' else None
                # The reviewed 2024 adapter family: the exporter's three-argument
                # SetCollectionObjectNameArg is called as SetCollectionObjectNameArg2.
                method = ADAPTER_SETTERS.get(method, method)
                add(operation['mp_step'], argument['sdk_name'], 'label marks it optional',
                    operation['operation_id'], 'curated_mp_step' if exact_step else 'curated_operation_id',
                    argument['sdk_name'], method, 'exact')
    rows.sort(key=lambda row: (row['evidence_command'], row['evidence_argument'], row['operation_id']))
    return rows


def render(rows: list[dict[str, str]], columns: tuple[str, ...] = COLUMNS) -> str:
    buffer = io.StringIO()
    writer = csv.DictWriter(buffer, fieldnames=columns, lineterminator='\n')
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
        outputs = (
            (f'scalar-presence-{target}.csv', render(rows), rows, 'presence'),
            (f'optional-inputs-{target}.csv', None, optional_inputs(args.repository_root, target), 'status'),
        )
        for name, content, output_rows, key in outputs:
            content = content or render(output_rows, OPTIONAL_INPUT_COLUMNS)
            path = args.output_dir / name
            counts: dict[str, int] = {}
            for row in output_rows:
                counts[row[key]] = counts.get(row[key], 0) + 1
            summary = ', '.join(f'{k} {v}' for k, v in sorted(counts.items()))
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
