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
| `conditional-ui.json` | Reviewed caller-reachable operator UI that does not make an operation `interactive_ui`, for the planned argument guard. See [Conditional behavior](#conditional-behavior). `eng/Test-OperationClassificationTwins.ps1` checks its operation IDs. |

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

- The interactive operations (47 per target at the decision) are not exclusive. They carry
  `interactive_ui` and the `interactive` duration class, and their intended
  isolation is `admissible`.
- Only the multi-call session lifecycles are `exclusive_workflow`: 43 on 2026
  and 46 on 2024. Review later found four of them to be single calls (see
  below), leaving 39 and 42.

The existing descriptor flags map as decided. `fixture_validation_pending` and
the 2024 `at-risk-no-runtime-validation` become validation statuses
(`fixture_pending`, `at_risk_unvalidated`). The 2024 `long-running` becomes the
`long_running` duration class. The 2024 `instrument-control` becomes
`device_session`. `destructive` and `filesystem_metadata` stay flags.

These corrections were made while reviewing the seed for #293 step 1, most of
them against each target's installed MP command reference. They are encoded in
the review section of `classify.py`. The `REVIEW_*` rule sets add an operation
to, or remove it from, the matching decision rule, so its family, flags,
duration and isolation follow that rule. `REVIEW_ADD_FLAGS` and
`REVIEW_REMOVE_FLAGS` change flags only. Each correction applies to every target
that registers the operation, and `OperationClassificationEvidenceTests` in each
target pins the corrected rows, including the absence of removed flags.

A corrected operation that requires a live device session also takes the
`long_running` duration, like every other `device_session` row, unless it is
interactive. Three session lifecycles that are also operator-guided
(`auto_correspond_closest_point`, `initiate_servo_guide` and
`guide_objects_in_6d_based_on_point_measurements`) carry `interactive_ui` and
stay `exclusive_workflow`: exclusivity follows from the multi-call session, not
from the operator UI.

The Doc column gives the page of the 2024.1.0508.5 `Documentation/MP Command
Reference.pdf` (printed page numbers) and the 2026.1.0529.7 topic under
`Documentation/Content/Topics/Scripting/MPCommandListing`. Unless a row says
otherwise, both releases document the operation the same way.

| Operation | Change | Reason | Doc (2024 page; 2026 topic) |
|---|---|---|---|
| `relationship_operations.edit_geometry_relationship_point_list` | + `interactive_ui`, → `interactive` | Opens the selected point-list dialog and pauses the MP until the operator closes it. | p. 731; `RelationshipOperations/EditGeometryRelationshipPointList.htm` |
| `construction_operations.construct_surface_by_dissecting_surfaces` | + `interactive_ui`, → `interactive` | A runtime-select step that prompts the operator to pick surfaces or faces. | p. 389; `ConstructionOperations/Surfaces/ConstructSurfaceByDissecting.htm` |
| `reporting_operations.set_report_options_for_object` | + `interactive_ui`, → `interactive` | Opens the report options dialog for the object. | p. 910; `ReportingOperations/SetReportOptionsForObject.htm` |
| `instrument_operations.align_two_targets_with_axis_wcf_x` | + `device_session`, `interactive_ui`, → `interactive` | Guides the operator through aligning two points; the instrument interface must be running. | p. 1156; `InstrumentOperations/AlignTwoTargetsWithAxis.htm` |
| `instrument_operations.drift_check` | + `interactive_ui`, → `interactive` | Always measures through the Drift Check dialog, which the operator finishes or cancels. | p. 1141; `InstrumentOperations/DriftCheck.htm` |
| `instrument_operations.build_target` | + `device_session`, `interactive_ui`, → `interactive` | Guides the operator through measuring a point while showing live deviations. | p. 1025; `InstrumentOperations/BuildTarget.htm` |
| `instrument_operations.locate_instrument_ref_tie_in` | + `device_session`, `interactive_ui`, → `interactive` | Guides the operator through measuring the reference points. Auto Survey adds `physical_motion`; see [Conditional behavior](#conditional-behavior). | p. 1059; `InstrumentOperations/LocateInstrumentRefTie.htm` |
| `instrument_operations.auto_correspond_closest_point` | + `interactive_ui`, → `interactive` (stays `exclusive_workflow`) | Measures inside the Auto-Correspond dialog. | p. 1136; `InstrumentOperations/AutoCorrespondClosestPoint.htm` |
| `instrument_operations.initiate_servo_guide` | + `interactive_ui`, → `interactive` (stays `exclusive_workflow`) | Guides the operator through the points; the operator starts each measurement. | p. 1040; `InstrumentOperations/InitiateServoGuide.htm` |
| `instrument_operations.auto_measure_points` | + `interactive_ui`, → `interactive` | Always shows the full or the abbreviated auto-measure dialog. | p. 1131; `InstrumentOperations/AutoMeasurePoints.htm` |
| `instrument_operations.guide_objects_in_6d_based_on_point_measurements` | + `interactive_ui`, → `interactive` (stays `exclusive_workflow`) | Starts the operator-guided 6D build routine with a running instrument. | p. 1153; `InstrumentOperations/GuideObjectsIn6Dbased.htm` |
| `instrument_operations.synchronized_measurement_master_slave` | + `interactive_ui`, → `interactive` | Its documented success status is that the measurement window opens, so it always opens operator UI. | p. 1173; `InstrumentOperations/SynchronizedMeasurementMaster.htm` |
| `robot_operations.get_robot_machine_parameter` | + `device_session`, → `long_running` | The robot/machine interface must be active. | p. 1203; `RobotOperations/Get Robot Machine Parameter.htm` |
| `robot_operations.set_robot_machine_parameter` | + `device_session`, `device_config`, → `long_running` | Sets a machine-specific parameter; the robot/machine interface must be active. | p. 1202; `RobotOperations/Set Robot Machine Parameter.htm` |
| `instrument_operations.set_probe_offset_frame_online` | + `device_session`, → `long_running` | Triggers a raw-frame measurement and waits up to a timeout for it. | p. 1030; `InstrumentOperations/SetProbeOffsetFrameOnline.htm` |
| `instrument_operations.lr_set_red_laser_intensity` | + `device_config` | Sets the intensity of the laser radar's red laser. | p. 1186; `InstrumentOperations/NikonMetrologyLaserRadar/LRSetRedLaserIntensity.htm` |
| `instrument_operations.configure_and_measure` | + `device_config` | Sets the instrument's target name and measurement mode, then measures. | p. 1053; `InstrumentOperations/ConfigureAndMeasure.htm` |
| `instrument_operations.instrument_operational_check` | + `device_config`, `physical_motion` | Check strings set target, auto-measure, AutoLock and beam-break modes, and include Home, Jog, Change Face, Motors On/Off and ADM Drive. | p. 1091 ff.; `InstrumentOperations/InstrOpCheck-LaserTrackers.htm` |
| `instrument_operations.set_instrument_axes` | + `device_session`, → `long_running` | Sets joint values; the instrument's interface must be active. The documentation describes animating the model and does not say the live robot moves, so `physical_motion` is not added. | p. 1181; `InstrumentOperations/AdvancedInstrumentOperations/SetInstrumentAxes.htm` |
| `instrument_operations.set_remeasure_failed_checks_only` | + `device_session`, `destructive`, → `long_running` | Clears the points of failed feature checks and starts trapping. | p. 1167; `InstrumentOperations/SetRemeasureFailedChecks.htm` |
| `robot_operations.compute_robot_machine_adjusted_goal_frame` | + `device_session`, → `long_running` | The robot/machine interface must be active. | p. 1209; `RobotOperations/Compute Robot Machine Adjusted.htm` |
| `instrument_operations.construct_mirror_from_plane` | + `device_session`, `device_config`, → `long_running` | Creates the mirror in the laser radar interface. | p. 1139; `InstrumentOperations/ConstructMirrorFromPlane.htm` |
| `instrument_operations.construct_mirror_from_two_points` | + `device_session`, `device_config`, → `long_running` | Sends the mirror plane to the instrument when Send Mirror to Instrument? is true, which is Briosa's default. | p. 1140; no 2026 topic |
| `instrument_operations.create_new_dynamic_reference` | + `device_session`, `device_config`, → `long_running` | Passes the dynamic reference frame to the AICON MoveInspect system. | p. 1143; `InstrumentOperations/CreateNewDynamicReference.htm` |
| `view_control.show_hide_instrument_interface` | + `device_session`, → `long_running` | Fails when the instrument is not connected to an interface, like `dock_instrument_interface`. | p. 1057; `InstrumentOperations/ShowHideInstrumentInterface.htm` |
| `robot_operations.get_calibration_appliance_data`, `get_calibration_appliance_integer_value`, `get_calibration_appliance_real_value`, `set_calibration_appliance_data`, `set_calibration_appliance_integer_value`, `set_calibration_appliance_real_value` | + `device_session`, `external_io`, → `long_running` | Each exchanges packets with the appliance and fails on an invalid host or port, like the appliance IP-address setter. | pp. 1223–1228; `RobotOperations/` appliance topics |
| `relationship_operations.set_geom_relationship_auto_measure_nominal_feature` (2026 only) | + `device_config` | Sets the instrument and measurement mode in the relationship's trapping control settings. | `AnalysisOperations/RelationshipAttributes/SetGeomRelationshipAutoMeasureNom.htm` |
| `instrument_operations.lr_hardware_connect` | + `external_io` | Connects to the caller-supplied host and port. The 2024 PDF has no topic; the committed 2024 binding evidence (`targets/2024.1.0508.5/evidence/sa/2024.1.0508.5/bindings.json`) records the same Host and Port inputs. | 2024 bindings; `InstrumentOperations/NikonMetrologyLaserRadar/LRHardwareConnect.htm` |
| `instrument_operations.set_ladar_auto_meas_point`, `set_ladar_auto_meas_sphere`, `set_ladar_feature_meas_circle`, `set_ladar_feature_meas_cylinder`, `set_ladar_feature_meas_slot`, `set_ladar_feature_meas_sphere` (2026 only) | + `device_config` | Each adjusts an instrument measurement mode: sample time, sphere options, or scan settings. | `InstrumentOperations/APILadar/` |
| `instrument_operations.measure_existing_single_point`, `measure_existing_single_point_and_compare` | + `physical_motion` | Each points the instrument at the existing point before it locks on and measures, like `point_at_target`. | pp. 1026, 1028; `InstrumentOperations/MeasureExistingSinglePoint*.htm` |
| `instrument_operations.measure_existing_single_point_manual_guide` | + `device_session`, `physical_motion` | Points and measures with a live instrument; the operator may steer the head instead. | p. 1027; `InstrumentOperations/MeasureExistingSinglePointManual.htm` |
| `instrument_operations.measure_nominal_feature` | + `physical_motion` | Points the laser radar at the nominal feature and measures it. | p. 1152; `InstrumentOperations/MeasureNominalFeature.htm` |
| `instrument_operations.track_tape_measurement` | + `physical_motion` | Scans the instrument along a line of targets. | p. 1130; `InstrumentOperations/TrackTapeMeasurement.htm` |
| `instrument_operations.edge_scan_measurement` | + `physical_motion` | Scans the laser radar from a seed point toward a direction point to find an edge. | p. 1129; `InstrumentOperations/EdgeScanMeasurement.htm` |
| `instrument_operations.lr_apdis_perform_mcm_calibration` | + `physical_motion` | The laser radar measures the nominal points from both faces. | p. 1192; `InstrumentOperations/NikonMetrologyLaserRadar/LRAPDISPerformMCMCalibration.htm` |
| `instrument_operations.lr_self_test_flip_test` | + `physical_motion` | Measures front and back faces of the laser radar. | p. 1190; `InstrumentOperations/NikonMetrologyLaserRadar/LRSelfTestFlipTest.htm` |
| `instrument_operations.set_alignment_projector` | + `physical_motion` | Starts the projector's alignment routine, like `align_laser_projector`. | p. 1182; `InstrumentOperations/AdvancedInstrumentOperations/SetAlignmentProjector.htm` |
| `file_operations.rename_general_file` | + `filesystem_read`, `filesystem_delete` | The source file's content moves to the destination and the source no longer exists, like `copy_general_file`'s read. | p. 34; `FileOperations/RenameGeneralFile.htm` |
| `file_operations.prepare_qdas_data_list` | − `filesystem_write` | Builds the QDAS data list in memory; it takes no file argument. | p. 91; `FileOperations/QDASFileExport/PrepareQDASDataList.htm` |
| `file_operations.get_qdas_catalog_entries` | − `filesystem_read` | Reads the catalog already loaded by Import QDAS Catalog File; it takes no path. | p. 88; `FileOperations/QDASFileExport/GetQDASCatalogEntries.htm` |
| `file_operations.load_html_form`, `file_operations.load_html_form_in_edge_browser` | + `filesystem_write` | The form writes its results to the caller-named output data-share file. | — |
| `file_operations.make_embedded_file_name_list` | − `filesystem_read` | `file_name_pattern` filters files embedded in the SA job; no filesystem path is read. | — |
| `relationship_operations.auto_filter_points_groups_clouds_to_surface_faces` | → `long_running` | Documented as able to take some time to process. | p. 785; `RelationshipOperations/AutoFilterPointsGroupsClouds.htm` |
| `instrument_operations.locate_instruments_usmn` | → `long_running` | Its uncertainty analysis runs up to a time limit given in minutes. | p. 1064; `InstrumentOperations/LocateInstrumentsUSMN.htm` |
| `relationship_operations.relationship_watch_window_template` | − `device_session`, → `quick`, `exclusive_workflow` → `admissible` | Only creates a watch-window template and always succeeds; it is not a session lifecycle. | p. 740; `RelationshipOperations/RelationshipWatchWindowTemplate.htm` |
| `instrument_operations.enable_disable_point_set_scan_mode`, `enable_disable_frame_set_scan_mode_all_instruments`, `enable_disable_frame_set_scan_mode_by_instrument` | `device_session` → `device_config`, → `quick`, `exclusive_workflow` → `admissible` | Each only sets how SA stores scan data; none is a session lifecycle. | pp. 1032–1034; `InstrumentOperations/EnableDisable*SetScan*.htm` |
| `reporting_operations.close_all_reports` | − `destructive` | Only closes report windows. | p. 920; `ReportingOperations/CloseAllReports.htm` |

## Reviewed counts

| | 2026.1.0529.7 | 2024.1.0508.5 |
|---|---:|---:|
| Operations | 1,027 | 996 |
| `exclusive_workflow` | 39 | 42 |
| `interactive_ui` | 59 | 59 |
| `read-only` / `standard` / `device` / `full` | 185 / 811 / 915 / 932 | 181 / 792 / 881 / 898 |

Before the review corrections, the decision's counts were 43 (2026) and 46
(2024) exclusive workflows, 47 interactive operations per target, and
185 / 831 / 927 / 937 (2026) and 181 / 805 / 893 / 903 (2024) admissions.

## Conditional behavior

Many operations change what they do according to request options. The
maintainer approved three rules for classifying them:

1. **Safety flags follow the worst reachable behavior.** `destructive`, the
   `filesystem_*` flags, `device_session`, `device_config`, `physical_motion`,
   `external_io` and `code_execution` are classified by the worst behavior a
   caller can reach through the request. Any option the caller can turn on
   counts, but only when Briosa passes it through; an option Briosa fixes off is
   not reachable.
2. **`interactive_ui` needs UI that always opens, or opens with the current
   defaults.** An operation is `interactive_ui` only when it always opens
   operator UI, or opens it with Briosa's current request defaults. UI that
   appears only when the caller turns an option on (Show Interface, Show Results
   Dialog, Report Deviations, Verify Results, Use Fit Dialog, Pause MP Until
   Closed, an HTML prompt file, a user prompt, and so on) does not set
   `interactive_ui`. A later #293 step adds a server-side argument guard that
   rejects those options unless the caller opted into interactive operations.
3. **`measure_immediately` and `auto_start` are classified by their planned
   defaults.** The planned breaking release flips both Briosa defaults to true,
   so `measure_single_point_here`, `configure_and_measure`,
   `measure_existing_single_point`, `measure_existing_single_point_and_compare`
   and the Auto Start option of `auto_measure_points` are classified by the
   post-flip behavior: that option alone does not make them `interactive_ui`.
   This change classifies them only; request defaults and runtime code are
   unchanged. `auto_measure_points` stays `interactive_ui` because its
   auto-measure dialog always opens.
   `measure_existing_single_point_manual_guide` also stays `interactive_ui`:
   manual guiding, where the operator releases the motors and steers the head,
   is operator-driven whatever its Measure Immediately option says.

Rule 1 changed these rows in both targets. Each option was checked in both
releases' documentation, and in each target's operation mapping, which passes
it through:

| Operation | Change | Reachable behavior | Doc (2024 page; 2026 topic) |
|---|---|---|---|
| `construction_operations.construct_surface_from_collection_of_surfaces` | + `destructive` | Delete Original Surfaces? deletes the source surfaces. | p. 387; `ConstructionOperations/Surfaces/ConstructSurfaceFromACollection.htm` |
| `relationship_operations.filter_geometry_relationship_outlier_cloud_points` | + `destructive` | Modify Existing Input Clouds deletes points from the associated clouds. | p. 732; `RelationshipOperations/FilterGeometryRelationshipOutlierCloudPoints.htm` |
| `instrument_operations.move_measurement_observation` | + `destructive` | Delete point if no measurements remain? deletes the source point. | p. 1039; `InstrumentOperations/MoveMeasurementObservation.htm` |
| `file_operations.import_hidden_point_bar_xml_file` | + `destructive` | Replace Existing Entries? replaces the hidden point bar definitions in the User Options. | p. 65; `FileOperations/FileImport/ImportHiddenPointBarXMLFile.htm` |
| `construction_operations.rename_object`, `rename_item`, `rename_point`, `rename_callout_view`, `copy_object`, `create_hidden_point`; `reporting_operations.rename_picture`; `event_operations.rename_event` | + `destructive` | Overwrite if exists? (Overwrite existing point? for `create_hidden_point`) replaces the existing object, item, point, callout view, picture or event. | pp. 261, 262, 258, 461, 253, 320, 916, 860; matching `RenameObject.htm`, `RenameItem.htm`, `RenamePoint.htm`, `Callouts/RenameCalloutView.htm`, `CopyObject.htm`, `PointsandGroups/CreateHiddenPoint.htm`, `ReportingOperations/RenamePicture.htm`, `Events/RenameEvent.htm` |
| `file_operations.import_file_as_embedded_file`, `import_mp_file_as_embedded_mp`, `import_file_as_picture` | + `destructive` | Replace Existing? replaces the embedded file, embedded MP or picture with the same name. | pp. 54–56; `FileOperations/FileImport/ImportFileAsEmbedded File.htm`, `ImportMPFileAsEmbeddedMP.htm`, `ImportFileAsPicture.htm` |
| `instrument_operations.locate_instrument_ref_tie_in` | + `physical_motion` | Auto Survey makes the instrument point at and measure each nominal point. | p. 1059; `InstrumentOperations/LocateInstrumentRefTie.htm` |
| `file_operations.import_e57_file` | + `filesystem_write` | Saved Converted File saves the file after conversion. | p. 48; `FileOperations/FileImport/ImportE57File.htm` |
| `instrument_operations.start_instrument_interface` | + `external_io` | The optional Device IP Address names the instrument host. Initialize at Startup runs the instrument's initialization routine, but neither release says that it moves the instrument, so `physical_motion` is not added. | p. 1049; `InstrumentOperations/StartInstrumentInterface.htm` |
| `instrument_operations.instrument_operational_check` | + `filesystem_read`, `filesystem_write` | Check strings take file paths: SaveVideoFrame and the iVision Teach XML path write files; Set Auto Align File and Send Scan to SA read them. | p. 1091 ff.; `InstrumentOperations/InstrOpCheck-*.htm` |

`conditional-ui.json` lists every caller-reachable operator UI that rule 2
leaves unflagged, one entry per operation and request field: the value that
opens the UI, Briosa's current default, whether that default opens it, and the
documentation in each target. The step-2 guard is to use the `caller_option`
entries. The `caller_option_default_flips` entries are the rule 3 fields. The
`job_or_file_state` entries have no enabling field and are listed for the
maintainer: `file_operations.save` opens Save As when the job has never been
named, `export_vector_container_to_ascii_file` asks before overwriting an
existing file, and `configure_and_measure` asks about a missing measurement
profile unless the SA interaction mode is Silent. Operations that are already
`interactive_ui` are not listed, except for the rule 3 fields. Progress bars,
callout and report content, graphics display options, and operations whose
purpose is to show or hide a window are not operator UI and are not listed.
`eng/Test-OperationClassificationTwins.ps1` checks that each entry names exactly
the targets that register the operation.

Two caller options open UI with Briosa's current defaults but are not
`interactive_ui`. Both are candidates for a default flip, because the step-2
guard would otherwise reject a default request:

- `file_operations.direct_cad_access` `prompt_on_missing_components` (default
  true) notifies the operator only when the CAD model has missing components, so
  the UI depends on the file as well as the option.
- `view_control.set_point_of_view_from_instrument_updates`
  `display_view_control` (default true) displays the view Control dialog;
  neither release says the dialog waits for the operator.

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
