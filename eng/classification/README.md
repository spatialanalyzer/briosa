# Operation classification seed

This directory records where the per-target D1 operation classification tables
came from and how to reproduce that seed. The D1 decision is on
[#242](https://github.com/spatialanalyzer/briosa/issues/242); the implementation
is [#293](https://github.com/spatialanalyzer/briosa/issues/293).

**The tables are the source of truth**, in each target's
`src/Briosa.Server/Security/OperationClassification.cs`. Each one holds one
hand-reviewed row per registered operation: risk flags, duration class,
validation status, and intended isolation class. Change a classification by
editing the table, and add a row with every new operation. The target's
`OperationClassificationTests` fail when a row is missing, extra, or incomplete.
The files here are review artifacts and are not read by any build or test.

At step 1 of #293 the tables are not consumed by `OperationPolicy`. The
registered descriptors still govern admission, scope, and replay safety.

## Files

| File | Purpose |
|---|---|
| `operation-classification-<target>.csv` | The reviewed seed, one row per operation. The tables were transcribed from these files. |
| `classify.py` | The rules used for the decision, plus the corrections made in review. It writes the CSVs from a descriptor dump. |
| `Export-OperationDescriptors.ps1` | Dumps a built target's compiled registry (`SpatialAnalyzerApi.Operations`) as `descriptors-<target>.json`, the input of `classify.py`. |
| `target-differences.json` | Reviewed shared operations whose rows may differ between targets. It is read by `eng/Test-OperationClassificationTwins.ps1`. |

The CSV columns are the operation id, its registered effect, a primary family
(a review aid only), the snake_case risk flags, the duration class, the
validation status, the intended isolation class, the legacy descriptor flags,
and admission under each D1 profile. Admission is computed from the proposal's
profile definitions as amended by the decision. A profile admits an operation
when all of its flags are in the profile's set and its effect filter passes.
`interactive_ui` is in no profile's set, and `exclusive_workflow` is never
admitted.

## Provenance

The decision classified the registries of `origin/main` at `5d2c2cb` (Server
0.9.1 line). `classify.py` uses those name, service and per-operation rules,
which were checked against the registry but not against licensed SA runs. The
#242 decision then changed two things, and both are applied here:

- The interactive operations (47 per target) are not exclusive. They carry
  `interactive_ui` and the `interactive` duration class, and their intended
  isolation is `admissible`.
- Only the multi-call session lifecycles are `exclusive_workflow`: 43 on 2026
  and 46 on 2024.

The existing descriptor flags map as decided. `fixture_validation_pending` and
the 2024 `at-risk-no-runtime-validation` become validation statuses
(`fixture_pending`, `at_risk_unvalidated`). The 2024 `long-running` becomes the
`long_running` duration class. The 2024 `instrument-control` becomes
`device_session`. `destructive` and `filesystem_metadata` stay flags.

These corrections were made while reviewing the seed for #293 step 1. They are
encoded in the review section of `classify.py`: `REVIEW_INTERACTIVE`,
`REVIEW_DEVICE_ACTION`, `REVIEW_DEVICE_CONFIG` and `REVIEW_MOTION` add an
operation to the matching decision rule, so its family, flags and duration
follow that rule; `REVIEW_ADD_FLAGS` and `REVIEW_REMOVE_FLAGS` change flags
only. Each correction applies to every target that registers the operation, and
`OperationClassificationEvidenceTests` in each target pins the corrected rows.

| Operation | Change | Reason |
|---|---|---|
| `relationship_operations.edit_geometry_relationship_point_list` | + `interactive_ui`, `quick` → `interactive` | It opens the selected point-list dialog and pauses the MP until the operator closes it. |
| `construction_operations.construct_surface_by_dissecting_surfaces` | + `interactive_ui`, `quick` → `interactive` | It is a runtime-select step that prompts the operator to pick the surfaces or faces to dissect. |
| `reporting_operations.set_report_options_for_object` | + `interactive_ui`, `quick` → `interactive` | It opens the report options dialog for the object. |
| `robot_operations.get_robot_machine_parameter` | + `device_session`, `quick` → `long_running` | It reads a parameter from the robot/machine, whose interface must be active. |
| `robot_operations.set_robot_machine_parameter` | + `device_session`, `device_config`, `quick` → `long_running` | It sets a machine-specific parameter on the robot/machine, whose interface must be active. |
| `instrument_operations.align_two_targets_with_axis_wcf_x` | + `device_session`, `quick` → `long_running` | It measures the two points with the instrument, whose interface must be running. |
| `instrument_operations.set_ladar_auto_meas_point`, `set_ladar_auto_meas_sphere`, `set_ladar_feature_meas_circle`, `set_ladar_feature_meas_cylinder`, `set_ladar_feature_meas_slot`, `set_ladar_feature_meas_sphere` (2026 only) | + `device_config` | Each adjusts an instrument measurement mode: sample time, sphere options, or scan line spacing and extra scan area. |
| `instrument_operations.measure_existing_single_point`, `measure_existing_single_point_and_compare` | + `physical_motion` | Each points the instrument at the existing point before it locks on and measures, like `point_at_target`. |
| `instrument_operations.measure_existing_single_point_manual_guide` | + `device_session`, `physical_motion` | It points and measures with a live instrument like the other `measure_existing_single_point` operations; the operator may steer the head instead. |
| `file_operations.load_html_form`, `file_operations.load_html_form_in_edge_browser` | + `filesystem_write` | The form writes its results to the caller-named output data-share file. |
| `file_operations.make_embedded_file_name_list` | − `filesystem_read` | `file_name_pattern` filters files embedded in the SA job; no filesystem path is read. |

The documentation-backed corrections were checked against each target's
installed MP command reference: for 2024.1.0508.5, `Documentation/MP Command
Reference.pdf` (Construct Surface by Dissecting Surface(s), p. 389; Edit
Geometry Relationship Point List, p. 731; Set Report Options for Object, p. 910;
Point At Target, p. 1022; Measure Existing Single Point and its Manual Guide and
Compare variants, pp. 1026–1028; Align Two Targets with Axis (WCF-X), p. 1156;
Set and Get Robot/Machine Parameter, pp. 1202–1203). For 2026.1.0529.7, the
matching topics under `Documentation/Content/Topics/Scripting/MPCommandListing`
(`ConstructionOperations/Surfaces/ConstructSurfaceByDissecting.htm`,
`RelationshipOperations/EditGeometryRelationshipPointList.htm`,
`ReportingOperations/SetReportOptionsForObject.htm`,
`RobotOperations/Get Robot Machine Parameter.htm`,
`RobotOperations/Set Robot Machine Parameter.htm`,
`InstrumentOperations/AlignTwoTargetsWithAxis.htm`,
`InstrumentOperations/MeasureExistingSinglePoint*.htm`,
`InstrumentOperations/PointAtTarget.htm` and `InstrumentOperations/APILadar/`).
The two releases document these operations the same way.

## Reviewed counts

| | 2026.1.0529.7 | 2024.1.0508.5 |
|---|---:|---:|
| Operations | 1,027 | 996 |
| `exclusive_workflow` | 43 | 46 |
| `interactive_ui` | 50 | 50 |
| `read-only` / `standard` / `device` / `full` | 184 / 819 / 924 / 934 | 180 / 799 / 890 / 900 |

The decision's counts, before the documentation-backed corrections, were 47
interactive operations per target and 185 / 831 / 927 / 937 (2026) and
181 / 805 / 893 / 903 (2024) admissions.

## Regenerate the seed

Run from the repository root with the .NET SDK from `global.json` and Python 3.12:

```powershell
foreach ($target in '2026.1.0529.7', '2024.1.0508.5') {
    dotnet build "targets/$target/src/Briosa.Server/Briosa.Server.csproj" -c Release
    ./eng/classification/Export-OperationDescriptors.ps1 -TargetDirectory "targets/$target" -OutputDirectory artifacts/classification
}
python eng/classification/classify.py --descriptors-dir artifacts/classification --output-dir artifacts/classification
```

Then compare the output with the committed CSVs and the tables. Once a table
has been edited, the CSV no longer has to match it. Regenerating is a review
aid for an SA-version migration or a broad reclassification, not a build step.
Do not generate table rows or operations from it.
