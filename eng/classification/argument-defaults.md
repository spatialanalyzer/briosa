# Argument defaults (D2)

This is the reviewed defaults table for the D2 decision on
[#243](https://github.com/spatialanalyzer/briosa/issues/243#issuecomment-6107759671),
completed by the maintainer's
[2026-10-13 decision on its open questions](https://github.com/spatialanalyzer/briosa/issues/243#issuecomment-6147618560)
(Q1 to Q8 below). For each argument it records what the server does when the
request field is absent, explicitly empty, or set, with the rule, evidence, and
client impact. It covers the twelve F6 arguments, the relationship tolerance and
constraint inverse gap, the substitutions D2 point 5 removed, Mesh Orientation,
and the risky F7 defaults that Q6 makes required. An appendix lists the optional
scalar request fields whose presence the server still ignores.

The `major3_*` columns are the behavior the code implements for contract
major 3, and a guard keeps the code and the table in step (see
[How the code is checked](#how-the-code-is-checked)). The `current_*` columns
record the contract 2.0 behavior before the change, at `0c7ff7a`.

| File | Purpose |
|---|---|
| `argument-defaults-<target>.csv` | The reviewed table, one row per argument, per exact-SA target. |
| `scalar-presence-<target>.csv` | F7 appendix: optional scalar request fields whose absent value is the protobuf zero value or an empty string. |
| `optional-inputs-<target>.csv` | Every optional MP input in the target's evidence, with the operation it matched and its absent behavior. See [Optional-input coverage](#optional-input-coverage). |
| `audit_scalar_presence.py` | The audit tool that writes the appendix and the coverage files. It reads source and writes CSVs; it generates no operations. `--check` runs in CI. |

Each target has its own files because each cites its own licensed evidence. The
two targets' operation mappings for every row are identical files, and the rows
differ only in evidence wording. For callers, the
[argument defaults migration](../../docs/development/argument-defaults-migration.md)
summarizes what changes.

## Rules for major 3

1. **An absent optional argument omits its setter.** It is never sent blank.
   Rule 1 covers the arguments this table lists, the ones major 3 changes. It
   does not yet cover every optional string: the 228 (2026) and 222 (2024)
   optional strings that are still sent blank when absent keep that behavior
   until a licensed probe shows that omitting the setter is equivalent (Q3). The
   appendix records them.
2. **Four arguments become optional** and are omitted when absent: the USMN
   nominals group, the `CreateChartFromVectorGroup` template chart, the
   `SetUserInterfaceProfile` profile file, and the
   `ConstructPlanesBoundingPointGroup` High and Low plane names. The High and
   Low names are separate fields, so the table has five rows for them.
3. **An explicit empty value is rejected before dispatch** with
   InvalidArgument and NotStarted (`request-validation-failed`), as a missing
   required field is. The one exception is the USMN nominals group, which the
   exact-target evidence documents as "blank for none" and which probe 1 showed
   SA accepts blank. An explicit empty nominals group is sent blank, with empty
   collection and object names, as probe 1 sent it (Q2). An explicit empty plane
   name is rejected, because SA rejects it with a blocking dialog.
4. **Unobserved arguments keep their behavior** until a licensed probe settles
   them: the fit seed geometry (pending
   [#311](https://github.com/spatialanalyzer/briosa/issues/311)), the other F6
   arguments, and the optional scalars in the appendix. The relationship
   tolerance and constraint stay required, and the clients change to match. A
   client may offer a named all-disabled value that the caller passes
   explicitly, but never an implicit default (Q4).
5. **Substitutions that the evidence shows don't work are removed** in major 3
   ([#308](https://github.com/spatialanalyzer/briosa/issues/308),
   [#310](https://github.com/spatialanalyzer/briosa/issues/310)). Mesh
   Orientation stays at risk
   ([#309](https://github.com/spatialanalyzer/briosa/issues/309)).
6. **Risky defaults R1 to R7 become required** (Q6). An absent field is rejected
   before dispatch, so nothing new reaches SA:
   - R1: the robot joint-pose joints;
   - R2: the instrument weather setting's `set_automatically`, and its
     temperature, pressure and humidity when `set_automatically` is false. When
     it is true, SA ignores the three values, and an omitted value is still
     sent as 0.
   - R3 and R4: the `ScaleObjects` and instrument scale factors;
   - R5: all 22 robot model-link parameters and the robot parameter value;
   - R6: the destructive operations' indexes;
   - R7: the destructive operations' target names and patterns. An explicit
     empty or whitespace-only value is rejected too.
   The manual-guide `measure_immediately` now defaults to true, like the other
   measure operations (#297). R8 to R11 stay as they are until the probes.

"Absent" means the request does not set the field. "Explicit empty" depends on
the field type (Q1):

| Type | Explicit empty |
|---|---|
| `CollectionObjectName`, `PointName`, `ChartName` | The message is set, but its name (`object_name`, `target_name`, `name`) is empty or whitespace, whatever `collection_name` says. |
| `FileReference` | The message is set, but `path` is empty or whitespace. |
| `string` | Empty or whitespace. |
| `ToleranceVectorOptions` | The message is set with no limits; it is sent as all eight limits disabled. |
| `repeated` fields | An empty list. It is the absent value on the wire, so it behaves like absent. |
| scalars and enums | Not applicable. An explicit 0 or false is a value; `UNSPECIFIED` is treated like absent. |

## Table columns

| Column | Meaning |
|---|---|
| `category` | `optional_omit` (rule 2), `required_clients_align` (rule 4, relationship inverse gap), `unchanged_pending_311` (fit seed), `unchanged_pending_probe` (other F6 arguments), `removed_in_major_3` (rule 5), `at_risk_309` (Mesh Orientation), `required_risky_default` (rule 6, R1 to R7), `default_flip` (rule 6, the manual-guide default). |
| `operation_id`, `mp_step` | The public operation ID and the exact MP step text the server sends. |
| `mp_argument`, `direction`, `sdk_binding` | The exact SDK argument label, input or output, and the setter or getter the server calls. |
| `proto_field`, `proto_field_number`, `proto_type` | The request (or, for outputs, result) field, as `Message.field`, with its number and declared type. |
| `mp_evidence` | What the exact-target evidence says about presence. The 2026 inventory records presence; the 2024 curated facts do not, so 2024 rows cite the label. |
| `current_absent`, `current_explicit_empty`, `current_value` | The contract 2.0 behavior before major 3. |
| `major3_absent`, `major3_explicit_empty`, `major3_value` | The major-3 behavior, which the guard checks. |
| `d2_point`, `probe`, `evidence`, `evidence_deviation`, `issues` | The decision points (D2 points 1 to 6, and `Qn` for the 2026-10-13 answers), the probe 277 step, the evidence document and anchor in the same target, the `EvidenceDeviations.json` entry (`command \| argument \| binding \| kind`), and the issues. |
| `client_impact` | What the six client products (dotnet, js and py, one package per target each) must do. |
| `notes` | The observation or reason behind the rule, in this target only. |

Behavior values:

| Value | Meaning |
|---|---|
| `rejected` | InvalidArgument and NotStarted before dispatch. |
| `rejected_unless:<field>` | Rejected unless the named Boolean field is true; then the zero value is sent. |
| `omitted` | The setter is not called. |
| `sent` | The setter is called with the value. |
| `sends_blank` | The setter is called with empty names. |
| `sends_default:<value>` | The setter is called with that value. |
| `sends_all_limits_disabled` | The tolerance setter is called with every limit disabled. |
| `not_applicable` | The field type has no empty form, or the row is an output. |
| `getter_returns_no_value` | Before removal, the getter returned nothing. |
| `removed` | The operation is not registered. |

## Rows

Both targets have the same 72 rows.

| Category | Rows | Arguments | Change in major 3 |
|---|---:|---|---|
| `optional_omit` | 5 | USMN `Nominals Group Name (blank for none)`; `Template Chart Name (optional)`; `Profile File Name (optional)`; `Resulting 'High' Plane Name`; `Resulting 'Low' Plane Name` | Absent: rejected → omitted. Explicit empty: stays rejected, except the nominals group, which is sent blank. |
| `required_clients_align` | 8 | `Tolerance` and `Constraint` of `make_group_to_group_relationship`, `make_group_to_nominal_group_relationship`, `make_point_to_point_relationship` and `make_points_to_points_relationship` | None on the server. The clients make both required. |
| `unchanged_pending_311` | 3 | `Starting Condition Geometry (optional)` of the three fit operations | None until #311 re-runs probe 2. |
| `unchanged_pending_probe` | 2 | `ask_for_point_name` `Initial Value`; `import_sa_file` `Selected Collections (optional)` | None. Neither was probed. |
| `removed_in_major_3` | 5 | The callout view and instrument wildcard getters (#308); the callout view list, Circle Line Mode and mirror frame plane setters (#310) | Removed by #320. |
| `at_risk_309` | 1 | `construct_polygonized_surface_from_point_clouds` `Mesh Orientation` | None; at risk until the isolated re-probe. |
| `required_risky_default` | 47 | R1: 6 joints. R2: 4 weather fields. R3: 1 and R4: 2 scale factors. R5: 22 model-link parameters and the robot parameter value. R6: 3 indexes. R7: 8 names and patterns. | Absent: sent as 0, false or blank → rejected (R2 values only when `set_automatically` is false). R7 explicit empty: sent blank → rejected. |
| `default_flip` | 1 | `measure_existing_single_point_manual_guide` `Measure Immediately` | Absent: false → true. |

The twelve F6 arguments are the 5 `optional_omit` rows, the 3 fit rows, the 2
`unchanged_pending_probe` rows, and the two `make_points_to_points_relationship`
rows.

## Optional-input coverage

`optional-inputs-<target>.csv` lists every optional MP input in the target's
evidence, with the registered operation and argument entry it matched and what
the server does when the field is absent.

| Status | 2026.1.0529.7 | 2024.1.0508.5 |
|---|---:|---:|
| Optional inputs in the evidence | 98 | 57 |
| Not implemented (no registered operation) | 15 | — |
| Implemented, but the SDK records no setter for the input | 1 | 0 |
| Rejected when absent | 7 | 4 |
| Omitted when absent | 44 | 44 |
| A default is sent when absent | 31 | 9 |
| Implemented in total | 83 | 57 |

Major 3 moved the five `optional_omit` inputs from rejected to omitted (three on
2024, whose label-based list does not include the plane names). Every input
still rejected when absent has a defaults-table row.

**2026** reads the committed inventory: every input whose presence is
`optional`. An operation matches when its descriptor MP step is the same text
as the inventory `mp_step` or one of the entry's `sdk_evidence.mp_step` values.
The argument entry then matches on the exact SDK label and setter in the
operation's source, with no name normalization. That alone matches 75 inputs.
Seven more match through a reviewed alias, because the inventory import
recorded one identity twice without joining them, and one more matches its
operation but has no setter. Each alias is listed in the script with both exact
texts:

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

**2024** has no committed inventory, so presence comes from the curated binding
facts: every input of a registered operation whose label says "optional" or
"blank for none". The curated facts cover only registered operations, so
nothing is "not implemented". Labels miss optional inputs that are not marked,
so these 57 are not comparable with the 2026 count. Examples are Ask for Point
Name's `Initial Value`, the Points to Points tolerance and constraint, and the
plane names, which the defaults table covers. The exporter's
`SetCollectionObjectNameArg` is read as Briosa's reviewed
`SetCollectionObjectNameArg2` adapter. The inputs that both files list have the
same status on both targets.

**Status.** It comes from the argument entry in the operation source:

- `omitted_when_absent`: the entry is added only under an `if`, or inside a
  conditional expression.
- `rejected_when_absent`: an unconditional entry whose value goes through a
  `Required` or `RequiredList` mapper.
- `default_sent`: any other unconditional entry.

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
  `Initial Value`, `Selected Collections`, or the R1 to R7 fields. Requiring the
  R1 to R7 fields needs none, because rejecting before dispatch sends nothing
  new to SA.

## Client impact

The client facts come from the clients' `origin/main` (0.4.0: briosa-dotnet
`8f80e50`, briosa-js `3f8c87c` for the F6 rows and `6d861a7` for the R1 to R7
rows, briosa-py `6289610`). For every
row, the two packages of each client are identical, and no client pre-validates
empty strings.

- **USMN nominals group, and the High and Low plane names.** All six clients
  already make these optional and leave the field unset. Contract 2.0 rejected
  that default call; major 3 accepts it, so no client signature changes. The
  High and Low names were a third inverse gap, and rule 2 closes it.
- **Template chart and profile file.** All six require them today. In major 3
  they become optional with no default value, and the field is left unset when
  omitted. Dotnet and py take the template chart as a positional parameter,
  which has to move behind the required parameters once it is optional.
- **Relationship tolerance and constraint.** All six make them optional today,
  and the server rejects the default call. In major 3 they become required, a
  breaking signature change. A caller that wants no limits passes options with
  every limit disabled. A client may offer a named all-disabled value, for
  example `ToleranceVectorOptions.NoLimits`, that the caller passes explicitly;
  it is never an implicit default (Q4).
- **R1 to R7.** All six make these optional today with a 0, false or empty
  default (dotnet and py signature defaults, js runtime fallbacks). In major 3
  they become required with no default, a breaking signature change:
  - R2's three weather values lose their 0 default and stay optional; the
    server rejects a request that omits one while `set_automatically` is false.
  - R5: dotnet's model-link overload without a configuration, which sends every
    parameter as 0 or false, goes, and the configuration types lose their
    member defaults in all three clients.
  - R7: dotnet and py already require the wildcard `search_string`; js drops
    its runtime fallback to `''`.
- **Manual-guide `measure_immediately`.** All six default it to false; major 3
  changes the default to true, as for the other measure operations.
- **Removed operations.** All six remove the facade methods and regenerate.
- **Everything else.** No change; the clients already require these arguments,
  as the server does.
- **All clients.** Never send an empty value or a 0 for an unset argument. The
  server, not the client, rejects explicit empty values.

briosa-examples calls none of these operations.

## How the code is checked

The guard follows the `conditional-ui.json` pattern (#293, Q5):

1. Each target transcribes its table into
   `tests/Briosa.Server.Tests/ArgumentDefaultsTable.cs`, one row per line.
2. `eng/Test-ArgumentDefaults.ps1` requires the transcription and this CSV to
   agree in both directions. It compares operation, field, label, binding and the
   three `major3_*` values, and checks the vocabulary. It runs in CI.
3. `ArgumentDefaultsTests` in each target builds the real MP command for every
   row three ways (absent, explicitly empty, and set) and requires the
   `major3_*` outcome:
   - the setter omitted or present;
   - a rejection, which `OperationExecutor` maps to InvalidArgument and
     NotStarted;
   - a blank or default value;
   - the value sent with the row's label and binding.

   Explicit empty is tried both as an empty and as a whitespace string, with the
   collection name set. A `rejected_unless` row is tried with the guarding field
   both false and true. A removed row requires the operation to be unregistered.
   One test also follows a rejection through the public gRPC seam: it checks the
   InvalidArgument status, the `request-validation-failed` diagnostic, NotStarted,
   and that nothing reaches the worker.
4. `audit_scalar_presence.py --check` runs in CI (Q8), so the appendix and the
   coverage files fail the build when the mappings change without them.
   `eng/Verify-CiWorkflow.ps1` requires the CI workflow to keep running these
   gates.

With the contract 2.0 mappings, 53 of the guard's 73 row and vocabulary tests
failed on each target; the public-seam test was added with the mapper change.

## F7 appendix: presence-insensitive optional scalars

These stay unchanged until probes settle them (D2 point 4), except the R1 to R7
fields and the manual-guide default, which Q6 decided. The appendix lists them so
that later changes are deliberate. It is not a hand audit, and a row is not a
defect.

| | 2026.1.0529.7 | 2024.1.0508.5 |
|---|---:|---:|
| `optional` scalar request fields | 1,600 | 1,528 |
| Presence ignored: absent sends the zero value (`ignored`) | 475 | 473 |
| Presence ignored but rejected later (`ignored_validated`) | 1 | 1 |
| Both presence-ignored rows by type: `bool` / `string` / `double` / `int32` | 150 / 147 / 96 / 83 | 149 / 147 / 96 / 82 |
| Operations with at least one such field | 275 | 274 |
| Presence checked, but absent still sends an empty string (`checked_sends_blank`) | 82 | 76 |

Before major 3 the presence-ignored counts were 524 and 522; the 48 decided
fields per target now check presence.

For `ignored` rows the server sends `false`, `0` or an empty string, and an
explicit zero cannot be told apart from an omitted field. The one
`ignored_validated` field, `do_relationship_fit`
`collection_containing_relationships`, is rejected when empty. The
`checked_sends_blank` rows are not presence-insensitive. They are listed
because rule 1 forbids sending blank for an absent optional argument it covers.
In all, an absent optional string is still sent as an empty string for 228
fields on 2026 and 222 on 2024 (Q3).

Columns: `operation_id`, `request_message`, `field`, `field_number`,
`proto_type`, `presence`, `mp_argument` and `sdk_setter` (as the mapping sends
them), `sent_when_absent`, `narrowest_profile`, and `review_flag`.
`narrowest_profile` is the narrowest admission profile that admits the
operation, taken from the target's `OperationClassification.cs`: `read-only`,
`standard`, `device` or `full`, plus "interactive opt-in" for `interactive_ui`
rows, or `never` for exclusive workflows.

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
- A field counts as checked when the mapping reads `Has<Field>` anywhere.

### Risky defaults

The shortlist came from the `ignored` rows, keeping only operations that some
profile admits. The rows still under review carry their ID in `review_flag`.

| ID | Fields (both targets unless noted) | Status |
|---|---|---|
| R1 | `move_robot_machine_to_joint_pose_six_dof` `joint_1` to `joint_6` | Required in major 3 (Q6). |
| R2 | `set_instrument_weather_setting` `temperature`, `pressure`, `relative_humidity`, `set_automatically` | Required in major 3; the three values only when `set_automatically` is false (Q6). |
| R3 | `scale_objects` `scale_factor` | Required in major 3 (Q6). |
| R4 | `set_absolute_instrument_scale_factor`, `set_multiply_instrument_scale_factor` `scale_factor` | Required in major 3 (Q6). Their `device_config` flag (Q7) is a separate change. |
| R5 | `set_robot_machine_model_link_parameters` (22 fields); `set_robot_machine_parameter` `parameter_value` | Required in major 3 (Q6). The model-link `device_config` flag (Q7) is a separate change. |
| R6 | `delete_measurement_observation` and `move_measurement_observation` `observation_index`; `delete_ith_vector_from_vector_group` `vector_index` | Required in major 3 (Q6). |
| R7 | `delete_folder` `folder_path`; `delete_variable` `name`; `delete_variables_wildcard_match` `variable_wildcard_criteria`; `delete_vector_by_name` `vector_name`; `delete_collections_by_wildcard` and `delete_folders_by_wildcard` `search_string`; `delete_robot_calibration` `calibration_name`; `remove_report_tag` `tag_name` | Required and not empty in major 3 (Q6). |
| R8 | `auto_measure_vectors` angle, high and low tolerance; `scale_bar_check` temperature, bar length, CTE and tolerance; `locate_instrument_ref_tie_in` `tolerance`; `construct_vector_group_area_profile_check` `area_radius` and `area_tolerance`; `construct_tcp_fixture` `point_match_threshold` | Under review with the probes. A zero tolerance or limit that, unlike the `(0.0 for none)` arguments, is not documented as "none" either fails every check or disables it. |
| R9 | `construct_scale_bar` `length`; `number_of_steps` of `set_instrument_transform`, `transform_instrument_frame_to_frame` and `set_robot_machine_base_transform`; `lr_self_test_lo_sep` `region` and `num_range_measurements`; `set_ladar_auto_meas_point` `sample_time_milliseconds` (2026 only); `lr_set_red_laser_intensity` `intensity` | Under review. A zero size, step count or setting; some are outside the documented range (Region 1 to 3, Sample Time 1 to 2000 ms). |
| R10 | `set_dimension_tolerance` `enable_nominal`, `enable_high`, `enable_low`; `set_logging_state`, `set_auto_event_creation` and `set_automatic_relationship_construction_state` `active` | Under review. The Boolean is the reason for the call, so an omitted field switches the feature off. The manual-guide `measure_immediately`, once in R10, defaults to true in major 3 (Q6). |
| R11 | `ask_for_user_decision_from_strings` `button1_text` to `button3_text` | Under review. Empty hides the button, so a request without buttons opens a decision dialog that has none. |

Destructive Boolean options are not on the list. All 23 that ignore presence
on each target send false when absent, which is the non-destructive choice:
`Replace Existing?`, `Replace Existing Entries?`, `Overwrite if exists?`,
`Overwrite existing file?`, `Delete point if no measurements remain?`,
`Clear Existing?`, `Allow Deleting all …` and `Remove Clipping Plane?`.

## Decisions on the open questions (2026-10-13)

| Q | Decision | Where |
|---|---|---|
| Q1 | A set name message with an empty or whitespace name is explicit empty, whatever its collection says. | Rule 3; the guard tries both. |
| Q2 | An explicit empty USMN nominals group is sent blank; an absent one omits the setter. | `optional_omit` row. |
| Q3 | Rule 1 covers the arguments major 3 changes. The other optional strings keep sending blank until probed. | Rule 1; the appendix. |
| Q4 | Tolerance and constraint stay required; clients may offer an explicit all-disabled value. | Client impact. |
| Q5 | Transcribed per-target test tables plus a root agreement script. | How the code is checked. |
| Q6 | R1 to R7 are required (R2 conditional, R7 also rejects empty); the manual-guide `measure_immediately` defaults to true. | `required_risky_default` and `default_flip` rows. |
| Q7 | The R4 and R5 model-link operations get `device_config`. | A separate change. The appendix's `narrowest_profile` column follows it; whichever change merges second regenerates the appendix. |
| Q8 | `audit_scalar_presence.py --check` is a CI gate. | `.github/workflows/ci.yml`. |

Still open: the fit seed (#311), the R8 to R11 defaults, and the optional strings
still sent blank, each pending licensed probes.

## Regenerate the generated files

Run from the repository root with Python 3.12 or later:

```powershell
python eng/classification/audit_scalar_presence.py          # rewrite the generated CSVs
python eng/classification/audit_scalar_presence.py --check  # compare only (CI)
```

The argument-defaults CSVs are hand-reviewed data; edit them directly, then
update each target's `ArgumentDefaultsTable.cs` and run
`eng/Test-ArgumentDefaults.ps1`.
