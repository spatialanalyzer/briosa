# Argument defaults (D2)

This is the reviewed defaults table that the D2 decision on
[#243](https://github.com/spatialanalyzer/briosa/issues/243#issuecomment-6107759671)
requires before any F6/F7 code: "A reviewed defaults table comes before code. It
records argument, rule and evidence link for every decided argument." It covers
the twelve F6 arguments, the relationship tolerance and constraint inverse gap,
the substitutions D2 point 5 removes, and Mesh Orientation. An appendix lists
the optional scalar request fields whose presence the server ignores (F7).

The files are review data. No build or test reads them yet; the F6/F7 change
adds the guard described in [How the code is checked](#how-the-code-is-checked).
The "current" columns describe `origin/main` at `0c7ff7a` (contract 2.0); the
"major3" columns are the decided behavior for the major-3 release.

| File | Purpose |
|---|---|
| `argument-defaults-<target>.csv` | The reviewed table, one row per argument, per exact-SA target. |
| `scalar-presence-<target>.csv` | F7 appendix: optional scalar request fields whose absent value is the protobuf zero value or an empty string. |
| `optional-inputs-<target>.csv` | Every optional MP input in the target's evidence, with the operation it matched and its absent behavior. See [Optional-input coverage](#optional-input-coverage). |
| `audit_scalar_presence.py` | The audit tool that writes the appendix and the coverage files. It reads source and writes CSVs; it generates no operations. |

Each target has its own files because each cites its own licensed evidence. The
two targets' operation mappings for every row are identical files today, and
the rows differ only in evidence wording.

## Rules for major 3

1. **An absent optional argument omits its setter.** It is never sent blank.
2. **Four arguments become optional** and are omitted when absent: the USMN
   nominals group, the `CreateChartFromVectorGroup` template chart, the
   `SetUserInterfaceProfile` profile file, and the
   `ConstructPlanesBoundingPointGroup` High and Low plane names. The High and
   Low names are separate fields, so the table has five rows for them.
3. **An explicit empty value is rejected before dispatch** with
   InvalidArgument and NotStarted (`request-validation-failed`), as missing
   required fields are today. The one exception is the USMN nominals group,
   which the exact-target evidence documents as "blank for none" and which
   probe 1 showed SA accepts blank; an explicit empty nominals group is sent
   blank. An explicit empty plane name is rejected, because SA rejects it with a
   blocking dialog.
4. **Unobserved arguments keep their current behavior** until a licensed probe
   settles them: the fit seed geometry (pending
   [#311](https://github.com/spatialanalyzer/briosa/issues/311)), the other F6
   arguments, and the optional scalars in the appendix. The relationship
   tolerance and constraint stay required; the clients change to match.
5. **Substitutions that the evidence shows don't work are removed** in major 3
   ([#308](https://github.com/spatialanalyzer/briosa/issues/308),
   [#310](https://github.com/spatialanalyzer/briosa/issues/310)). Mesh
   Orientation stays at risk
   ([#309](https://github.com/spatialanalyzer/briosa/issues/309)).

"Absent" means the request does not set the field. "Explicit empty" depends on
the field type:

| Type | Explicit empty |
|---|---|
| `CollectionObjectName`, `PointName`, `ChartName` | The message is set, but its name (`object_name`, `target_name`, `name`) is empty or whitespace. The current mappers already treat whitespace as empty. |
| `FileReference` | The message is set, but `path` is empty or whitespace. |
| `ToleranceVectorOptions` | The message is set with no limits; it is sent as all eight limits disabled. |
| `repeated` fields | An empty list. It is the absent value on the wire, so it behaves like absent. |
| enums | Not applicable; `UNSPECIFIED` is treated like absent. |

## Table columns

| Column | Meaning |
|---|---|
| `category` | `optional_omit` (rule 2), `required_clients_align` (rule 4, relationship inverse gap), `unchanged_pending_311` (fit seed), `unchanged_pending_probe` (other F6 arguments), `removed_in_major_3` (rule 5), `at_risk_309` (Mesh Orientation). |
| `operation_id`, `mp_step` | The public operation ID and the exact MP step text the server sends. |
| `mp_argument`, `direction`, `sdk_binding` | The exact SDK argument label, input or output, and the setter or getter the server calls. |
| `proto_field`, `proto_field_number`, `proto_type` | The request (or, for outputs, result) field, as `Message.field`, with its number and declared type. |
| `mp_evidence` | What the exact-target evidence says about presence. The 2026 inventory records presence; the 2024 curated facts do not, so 2024 rows cite the label. |
| `current_absent`, `current_explicit_empty`, `current_value` | What the server does today. |
| `major3_absent`, `major3_explicit_empty`, `major3_value` | What the server does in major 3. |
| `d2_point`, `probe`, `evidence`, `evidence_deviation`, `issues` | The D2 points that decide the row, the probe 277 step, the evidence document and anchor in the same target, the `EvidenceDeviations.json` entry (`command \| argument \| binding \| kind`), and the issues. |
| `client_impact` | What the six client products (dotnet, js and py, one package per target each) must do. |
| `notes` | The observation behind the rule, in this target only. |

Behavior values: `rejected` (InvalidArgument and NotStarted before dispatch),
`omitted` (the setter is not called), `sent`, `sends_blank` (the setter is
called with empty names), `sends_all_limits_disabled`, `not_applicable`,
`getter_returns_no_value`, and `removed` (the operation is not registered).

## Rows

Both targets have the same 24 rows.

| Category | Rows | Arguments | Change in major 3 |
|---|---:|---|---|
| `optional_omit` | 5 | USMN `Nominals Group Name (blank for none)`; `Template Chart Name (optional)`; `Profile File Name (optional)`; `Resulting 'High' Plane Name`; `Resulting 'Low' Plane Name` | Absent: rejected → omitted. Explicit empty: stays rejected, except the nominals group, which is sent blank. |
| `required_clients_align` | 8 | `Tolerance` and `Constraint` of `make_group_to_group_relationship`, `make_group_to_nominal_group_relationship`, `make_point_to_point_relationship` and `make_points_to_points_relationship` | None on the server. The clients make both required. |
| `unchanged_pending_311` | 3 | `Starting Condition Geometry (optional)` of the three fit operations | None until #311 re-runs probe 2. |
| `unchanged_pending_probe` | 2 | `ask_for_point_name` `Initial Value`; `import_sa_file` `Selected Collections (optional)` | None. Neither was probed. |
| `removed_in_major_3` | 5 | The callout view and instrument wildcard getters (#308); the callout view list, Circle Line Mode and mirror frame plane setters (#310) | The operation is removed, unless the #308/#310 change keeps it at risk because it finds an untried exact-target binding. That change is authoritative. |
| `at_risk_309` | 1 | `construct_polygonized_surface_from_point_clouds` `Mesh Orientation` | None; at risk until the isolated re-probe. |

The twelve F6 arguments are the 5 `optional_omit` rows, the 3 fit rows, the 2
`unchanged_pending_probe` rows, and the two `make_points_to_points_relationship`
rows. The coverage check below confirms that these twelve are the only
optional inputs rejected when absent.

## Optional-input coverage

`optional-inputs-<target>.csv` lists every optional MP input in the target's
evidence, with the registered operation and argument entry it matched and what
the server does when the field is absent. `audit_scalar_presence.py` writes it,
and `--check` compares it with the committed file.

| Status | 2026.1.0529.7 | 2024.1.0508.5 |
|---|---:|---:|
| Optional inputs in the evidence | 98 | 57 |
| Not implemented (no registered operation) | 15 | — |
| Implemented, but the SDK records no setter for the input | 1 | 0 |
| Rejected when absent | 12 | 7 |
| Omitted when absent | 39 | 41 |
| A default is sent when absent | 31 | 9 |
| Implemented in total | 83 | 57 |

Every input rejected when absent has a row in the defaults table. No new F6 row
was found.

**2026** reads the committed inventory: every input whose presence is
`optional`. An operation matches when its descriptor MP step is the same text
as the inventory `mp_step` or one of the entry's `sdk_evidence.mp_step` values.
The argument entry then matches on the exact SDK label and setter in the
operation's source, with no name normalization. That alone matches 75 inputs,
the maintainer's lower bound on #318. Seven more match through a reviewed alias,
because the inventory import recorded one identity twice without joining them,
and one more matches its operation but has no setter. Each alias is listed in
the script with both exact texts:

- **Three LADAR feature-measure inputs.** The documentation entries `Set LADAR
  Feature Measure Circle`, `Cylinder` and `Slot` carry no SDK evidence. The SDK
  steps were recorded as separate SDK-only entries (`Set LADAR FeatureMeas
  Circle` and so on), and the operations register those steps.
- **Four inputs whose documented row has no setter.** The same inventory
  command has a not-documented SDK row with the setter:
  - the bounding-plane `Resulting ‘High’ Plane Name (optional)` and `Low`, sent
    as `Resulting 'High' Plane Name` and `Low`;
  - `Additional Note (blank for none)`, sent as `Additional Notes (blank for
    none)`;
  - `Measurement Mode (optional)`, sent as `Measurement Mode`.

The input without a setter is `Report Name (optional)` of
`generate_quick_report_from_tab_order`. The operation is registered, but the
SDK records no setter for that documented input, and Briosa sends none.

The first version of this table reported 25 inputs not implemented and 61
omitted or defaulted. It matched only the documentation `mp_step`, so it missed
ten implemented inputs. Four belong to operations whose SDK step text differs:

- `Save As...`
- `Construct Frame on Object`
- `Align Two Targets with Axis (WCF - X)`
- `'Build' Target`

Three are the LADAR inputs above. The other three belong to two operations whose
descriptors are declared through a helper the first search did not read:
`point_at_target`, and the two `lr_apdis_activate_mcm_calibration` inputs. The
corrected 2026 figures are 83 implemented: 12 rejected, 70 omitted or defaulted,
and 1 without a setter.

**2024** has no committed inventory, so presence comes from the curated binding
facts: every input of a registered operation whose label says "optional" or
"blank for none". The curated facts cover only registered operations, so
nothing is "not implemented". Labels miss optional inputs that are not marked,
so these 57 are not comparable with the 2026 count. Examples are Ask for Point
Name's `Initial Value`, the Points to Points tolerance and constraint, and the
plane names, which the defaults table covers. The exporter's
`SetCollectionObjectNameArg` is read as Briosa's reviewed
`SetCollectionObjectNameArg2` adapter. The 54 inputs that both files list have
the same status on both targets.

**Status.** It comes from the argument entry in the operation source:

- `omitted_when_absent`: the entry is added only under an `if`, or inside a
  conditional expression.
- `rejected_when_absent`: an unconditional entry whose value goes through a
  `Required` or `RequiredList` mapper.
- `default_sent`: any other unconditional entry.

A throwaway test confirmed every implemented status on both targets (82 on
2026, 57 on 2024). For each operation, it filled a request and cleared each
field, then called the real `CreateCommand` and named the field that removes,
rejects or changes the argument. It also confirmed each descriptor step against
the compiled registry. The test is not committed.

## Evidence

- Each row cites only its own target's licensed record:
  `targets/<target>/docs/testing/evidence/probe-277-<target>-2026-10-07.md`.
  Nothing is transferred between targets. For example, the 2026 record shows a
  blocking "Template Chart Not found" dialog for a template name that matches
  no chart, which the 2024 session did not exercise.
- Probes 1, 3, 4, 5 and 6 behaved the same on both targets: omit, blank and
  value were accepted for the nominals group, template chart and profile file,
  and an omitted plane name made SA use its default name. A blank plane name
  was rejected only after a blocking dialog.
- No licensed observation covers the relationship tolerance and constraint,
  `Initial Value`, or `Selected Collections`.
- The `current_*` columns were confirmed against the code on both targets. A
  throwaway test built each input row's request with the field absent, empty
  and set, and called the operation's real `CreateCommand`. It also checked the
  label, SDK binding and field number. The test is not committed.

## Client impact

The client facts come from the clients' `origin/main` (0.4.0: briosa-dotnet
`8f80e50`, briosa-js `3f8c87c`, briosa-py `6289610`). For every row, the two
packages of each client are identical, and no client pre-validates empty
strings.

- **USMN nominals group, and the High and Low plane names.** All six clients
  already make these optional and leave the field unset. Today the server
  rejects that default call. Major 3 accepts it, so no client signature changes.
  The High and Low names are a third inverse gap, besides the two the addendum
  lists, and rule 2 closes it.
- **Template chart and profile file.** All six require them today. In major 3
  they become optional with no default value, and the field is left unset when
  omitted. Dotnet and py take the template chart as a positional parameter,
  which has to move behind the required parameters once it is optional.
- **Relationship tolerance and constraint.** All six make them optional today,
  and the server rejects the default call. In major 3 they become required, a
  breaking signature change. A caller that wants no limits passes options with
  every limit disabled. A client must not fill that in implicitly.
- **Removed operations.** All six remove the facade methods and regenerate.
- **Everything else.** No change; the clients already require these arguments,
  as the server does.
- **All clients.** Never send an empty value for an unset optional argument.
  The server, not the client, rejects explicit empty values.

briosa-examples calls none of these operations.

## How the code is checked

The F6/F7 change adds a guard in the pattern of `conditional-ui.json`
(#293):

1. Each target transcribes its table into a test table, one row per line.
2. A root script, like `eng/Test-OperationConditionalOptions.ps1`, requires the
   transcription and this CSV to agree in both directions on operation, label,
   binding, field and the three `major3_*` values.
3. A per-target test builds the real MP command for each row three ways, with
   the field absent, explicitly empty and set. As
   `OperationConditionalOptionTests` does, it requires the outcome in the
   `major3_*` columns: the setter omitted or present, the empty value rejected
   (InvalidArgument and NotStarted) or sent blank, and the value sent with the
   row's label and binding.
4. For `removed_in_major_3` rows, the test requires that the operation is not
   registered.

The throwaway test that confirmed the `current_*` columns is a prototype of
step 3. `audit_scalar_presence.py --check` reports any drift between the code
and the appendix. It is a review aid, not a CI gate.

## F7 appendix: presence-insensitive optional scalars

D2 point 4 keeps these unchanged until probes settle them. The appendix lists
them so that later changes are deliberate. It is not a hand audit, and a row is
not a defect.

| | 2026.1.0529.7 | 2024.1.0508.5 |
|---|---:|---:|
| `optional` scalar request fields | 1,600 | 1,528 |
| Presence ignored: absent sends the zero value (`ignored`) | 523 | 521 |
| Presence ignored but rejected later (`ignored_validated`) | 1 | 1 |
| Both presence-ignored rows by type: `bool` / `string` / `double` / `int32` | 155 / 155 / 127 / 87 | 154 / 155 / 127 / 86 |
| Operations with at least one such field | 288 | 287 |
| Presence checked, but absent still sends an empty string (`checked_sends_blank`) | 82 | 76 |

For `ignored` rows the server sends `false`, `0` or an empty string, and an
explicit zero cannot be told apart from an omitted field. The one
`ignored_validated` field, `do_relationship_fit`
`collection_containing_relationships`, is rejected when empty. The
`checked_sends_blank` rows are not presence-insensitive. They are listed
because rule 1 forbids sending blank for an absent optional argument; see open
question 3. In all, an absent optional string is sent as an empty string for
236 fields on 2026 and 230 on 2024.

Columns: `operation_id`, `request_message`, `field`, `field_number`,
`proto_type`, `presence`, `mp_argument` and `sdk_setter` (as the mapping sends
them), `sent_when_absent`, `narrowest_profile`, and `review_flag`.
`narrowest_profile` is the narrowest admission profile that admits the
operation today, taken from the target's `OperationClassification.cs`:
`read-only`, `standard`, `device` or `full`, plus "interactive opt-in" for
`interactive_ui` rows, or `never` for exclusive workflows. It matched the
classification seed CSVs for every row.

### Method and limits

`audit_scalar_presence.py` reads each target's `proto/briosa/*.proto`, finds
each request type's operation through `CreateCommand(Api.<Request> request)`,
and classifies every `optional` scalar field:

- **`ignored`.** The mapping never reads `request.Has<Field>` and passes
  `request.<Field>` straight to `WorkerBooleanValue`, `WorkerDoubleValue`,
  `WorkerIntegerValue` or `WorkerTextValue`. Every such use in both targets
  takes one of these four forms.
- **`ignored_validated`.** The field is also read by a check that can reject
  it.
- **`checked_sends_blank`.** A string field sent as
  `Has<Field> ? <Field> : string.Empty` or `: ""`.

Limits:

- The method matches source text; it does not compile the code. It relies on
  the handwritten mapping conventions: one operation class per request, with
  the request parameter used directly. Every scalar field in both targets
  resolved to a label and a setter.
- It covers request scalars only. Optional enums (135 on 2026, 122 on 2024),
  message fields and repeated fields are excluded, as are optional scalars
  nested in value messages such as `ToleranceLimit`.
- It reports what the server sends, not what SA does with it. No row has
  licensed evidence.
- A field counts as checked when the mapping reads `Has<Field>` anywhere. A
  separate pass looked for uses outside such a guard. It found only the seven
  2024 `construct_objects_from_surface_faces_runtime_select` choices, which an
  earlier check rejects when any one is absent.

### Risky defaults for the maintainer

Destructive Boolean options are not on the list. All 23 that ignore presence
on each target send false when absent, which is the non-destructive choice:
`Replace Existing?`, `Replace Existing Entries?`, `Overwrite if exists?`,
`Overwrite existing file?`, `Delete point if no measurements remain?`,
`Clear Existing?`, `Allow Deleting all …` and `Remove Clipping Plane?`.

These are shortlisted from the `ignored` rows, keeping only operations that
some profile admits. Each is a risk to review, not an observed failure: SA's
handling of the zero value is unobserved unless stated. The rows carry the ID in
`review_flag`.

| ID | Fields (both targets unless noted) | Profile | Why it is risky |
|---|---|---|---|
| R1 | `move_robot_machine_to_joint_pose_six_dof` `joint_1` to `joint_6` | device | Any joint the caller leaves out is sent as 0, so the robot is commanded toward a pose the caller did not ask for. |
| R2 | `set_instrument_weather_setting` `temperature`, `pressure`, `relative_humidity`, `set_automatically` | device | `Set Automatically?` defaults to false, and SA documents that the supplied values are then used. An empty request sets 0 °F, 0 mmHg and 0 % on the instrument, which skews its later measurements. |
| R3 | `scale_objects` `scale_factor` | standard | Documented by Briosa as 0. An omitted factor scales the objects by zero about the working frame. |
| R4 | `set_absolute_instrument_scale_factor`, `set_multiply_instrument_scale_factor` `scale_factor` | device | Sets the instrument's scale to 0, or multiplies it by 0. Both now carry `device_config` (question 7), so `standard` no longer admits them. |
| R5 | `set_robot_machine_model_link_parameters` (22 fields); `set_robot_machine_parameter` `parameter_value` | device | A partial request overwrites every DH, 6DOF, encoder and mass parameter it omits with 0 or false. The model-link operation now carries `device_config` (question 7). |
| R6 | `delete_measurement_observation` and `move_measurement_observation` `observation_index`; `delete_ith_vector_from_vector_group` `vector_index` | standard | A destructive operation whose omitted index targets index 0, so it either removes the first item or fails, depending on SA's unobserved indexing. |
| R7 | `delete_folder` `folder_path`; `delete_variable` `name`; `delete_variables_wildcard_match` `variable_wildcard_criteria`; `delete_vector_by_name` `vector_name`; `delete_collections_by_wildcard` and `delete_folders_by_wildcard` `search_string`; `delete_robot_calibration` `calibration_name`; `remove_report_tag` `tag_name` | standard | A destructive operation is sent an empty target name or pattern, and what SA does with it is unobserved. The two wildcard deletes also send `Allow Deleting all …` as false. |
| R8 | `auto_measure_vectors` angle, high and low tolerance; `scale_bar_check` temperature, bar length, CTE and tolerance; `locate_instrument_ref_tie_in` `tolerance`; `construct_vector_group_area_profile_check` `area_radius` and `area_tolerance`; `construct_tcp_fixture` `point_match_threshold` | standard / device | A zero tolerance or limit that, unlike the `(0.0 for none)` arguments, is not documented as "none". It either fails every check or disables it. |
| R9 | `construct_scale_bar` `length`; `number_of_steps` of `set_instrument_transform`, `transform_instrument_frame_to_frame` and `set_robot_machine_base_transform`; `lr_self_test_lo_sep` `region` and `num_range_measurements`; `set_ladar_auto_meas_point` `sample_time_milliseconds` (2026 only); `lr_set_red_laser_intensity` `intensity` | standard / device | A zero size, step count or setting. Some are outside the documented range: Region is 1 to 3, and Sample Time is 1 to 2000 ms. |
| R10 | `set_dimension_tolerance` `enable_nominal`, `enable_high`, `enable_low`; `set_logging_state`, `set_auto_event_creation` and `set_automatic_relationship_construction_state` `active`; `measure_existing_single_point_manual_guide` `measure_immediately` | standard / device | The Boolean is the reason for the call, so an omitted field silently switches the feature off. `measure_immediately` is false here, although the decided default for the other measure operations is true. |
| R11 | `ask_for_user_decision_from_strings` `button1_text` to `button3_text` | standard + interactive opt-in | Empty hides the button, so a request without buttons opens a decision dialog that has none. |

Requiring a field rejects the request before dispatch, so it sends nothing new
to SA and needs no licensed observation; see open question 6.

## Open questions

1. **Explicit empty for name messages.** Treat a set message whose name is
   empty or whitespace as explicit empty, ignoring `collection_name`.
   Recommended, because it matches the current mappers.
2. **USMN explicit empty.** Send blank, with empty collection and object names
   and the PointGroup type kept, as probe 1 did. Normalizing it to an omitted
   setter is the alternative. Recommended: send blank, the observed and
   documented form.
3. **Rule 1 and existing blank defaults.** Optional strings sent as empty when
   absent: 236 on 2026 and 230 on 2024 (`ignored` plus `checked_sends_blank`).
   Recommended: keep them until probed (point 4), apply rule 1 to every
   argument that major 3 changes, and record them in the appendix.
4. **Relationship "no limits".** Recommended: clients may offer a named
   all-disabled value that the caller passes explicitly, but never an implicit
   default.
5. **Guard form.** Recommended: a transcribed per-target table plus a root
   agreement script, the #293 pattern. Letting target tests read the root CSV
   is the alternative.
6. **R1 to R5.** Recommended: make these fields required in major 3 as a
   bounded exception to point 4, because rejecting before dispatch sends
   nothing to SA. Review R6 to R11 with the probes.
7. **Unflagged scale and robot-model setters.** R4 and the R5 model-link
   operation carried no risk flag, so `standard` admitted them. Decided
   (2026-10-13): they carry `device_config`, so only `device` and `full` admit
   them. Extended (2026-10-14) to the robot calibration setters and
   `set_probe_offset_frame_offline`; `set_robot_machine_base_transform` stays in
   `standard`.
8. **Appendix drift.** Recommended: keep `audit_scalar_presence.py --check` a
   review aid, not a CI gate. The F6/F7 change regenerates and reviews the
   appendix.

## Regenerate the generated files

Run from the repository root with Python 3.12 or later:

```powershell
python eng/classification/audit_scalar_presence.py          # rewrite the generated CSVs
python eng/classification/audit_scalar_presence.py --check  # compare only
```

The argument-defaults CSVs are hand-reviewed data; edit them directly.
