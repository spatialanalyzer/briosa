# Argument defaults migration

Contract major 3 changes what the server does when certain request fields are
absent or explicitly empty
([#243](https://github.com/spatialanalyzer/briosa/issues/243)). The reviewed
table, with evidence and per-target rows, is
[eng/classification/argument-defaults.md](../../eng/classification/argument-defaults.md).
This page lists what callers and client libraries change. Both exact-SA targets
behave the same.

## What changes for callers

A rejected request fails before dispatch with InvalidArgument, an
`OperationError` of kind Validation, diagnostic `request-validation-failed`, and
disposition NotStarted. Nothing reaches SpatialAnalyzer, so the request is safe
to correct and resend.

| Operation | Field | Contract 2.0 | Major 3 |
| --- | --- | --- | --- |
| `instrument_operations.locate_instruments_usmn` | `nominals_group` | Absent or empty name: rejected | Absent: setter omitted. Empty name: sent blank ("blank for none"). |
| `reporting_operations.create_chart_from_vector_group` | `template_chart_name` | Absent or empty: rejected | Absent: setter omitted. Empty: rejected. |
| `utility_operations.set_user_interface_profile` | `profile_file_name` | Absent or empty: rejected | Absent: setter omitted. Empty: rejected. |
| `construction_operations.construct_planes_bounding_point_group` | `resulting_high_plane_name`, `resulting_low_plane_name` | Absent or empty: rejected | Absent: setter omitted, and SpatialAnalyzer uses its default plane name. Empty: rejected. |
| `robot_operations.move_robot_machine_to_joint_pose_six_dof` | `joint_1` to `joint_6` | Absent: sent as 0 | Absent: rejected |
| `instrument_operations.set_instrument_weather_setting` | `set_automatically` | Absent: sent as false | Absent: rejected |
| `instrument_operations.set_instrument_weather_setting` | `temperature`, `pressure`, `relative_humidity` | Absent: sent as 0 | Absent: rejected when `set_automatically` is false; sent as 0 when it is true |
| `utility_operations.scale_objects` | `scale_factor` | Absent: sent as 0 | Absent: rejected |
| `instrument_operations.set_absolute_instrument_scale_factor`, `set_multiply_instrument_scale_factor` | `scale_factor` | Absent: sent as 0 | Absent: rejected |
| `robot_operations.set_robot_machine_model_link_parameters` | the 22 scalar parameters, `dh_alpha_component` through `segment_cg_mass_kg` | Absent: sent as 0 or false | Absent: rejected |
| `robot_operations.set_robot_machine_parameter` | `parameter_value` | Absent: sent as 0 | Absent: rejected |
| `instrument_operations.delete_measurement_observation`, `move_measurement_observation` | `observation_index` | Absent: sent as 0 | Absent: rejected |
| `vector_operations.delete_ith_vector_from_vector_group` | `vector_index` | Absent: sent as 0 | Absent: rejected |
| `utility_operations.delete_folder` | `folder_path` | Absent or empty: sent blank | Absent, empty or whitespace: rejected |
| `variables.delete_variable` | `name` | Absent or empty: sent blank | Absent, empty or whitespace: rejected |
| `variables.delete_variables_wildcard_match` | `variable_wildcard_criteria` | Absent or empty: sent blank | Absent, empty or whitespace: rejected |
| `vector_operations.delete_vector_by_name` | `vector_name` | Absent or empty: sent blank | Absent, empty or whitespace: rejected |
| `construction_operations.delete_collections_by_wildcard`, `delete_folders_by_wildcard` | `search_string` | Absent or empty: sent blank | Absent, empty or whitespace: rejected |
| `robot_operations.delete_robot_calibration` | `calibration_name` | Absent or empty: sent blank | Absent, empty or whitespace: rejected |
| `reporting_operations.remove_report_tag` | `tag_name` | Absent or empty: sent blank | Absent, empty or whitespace: rejected |
| `instrument_operations.measure_existing_single_point_manual_guide` | `measure_immediately` | Absent: sent as false | Absent: sent as true, as for the other measure operations |

An explicit 0 or false is still sent as given. A caller that relied on an old
zero, false or blank default sends that value explicitly.

## What does not change

- The relationship `tolerance` and `constraint` of the four `make_*_relationship`
  operations stay required. A caller that wants no limits passes an options
  value with every limit disabled.
- The fit seed geometry, Ask for Point Name's `initial_value` and Import SA
  File's `selected_collections` stay required until licensed probes settle them.
- Other optional strings that are sent blank when absent keep that behavior
  until a probe shows that omitting the setter is equivalent.

## Client libraries

All six first-party client products (dotnet, js and py, one package per exact-SA
target) change the same way:

- Make the template chart and the profile file optional, with no default value,
  and leave the field unset when the caller omits it. The USMN nominals group and
  the bounding plane names are already optional.
- Make the relationship tolerance and constraint required. A client may offer a
  named all-disabled value, for example `ToleranceVectorOptions.NoLimits`, that
  the caller passes explicitly. It is never an implicit default.
- Make every field marked "Absent: rejected" above required, with no default.
  The weather values stay optional but lose their 0 default. Remove facade
  overloads and value-type member defaults that fill these fields in. For
  example, the dotnet model-link overload without a configuration goes.
- Default the manual-guide `measure_immediately` to true.
- Never send an empty value, 0, or false for an argument the caller left unset,
  and do not pre-validate empty strings; the server rejects them.
