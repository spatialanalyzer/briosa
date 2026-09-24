# MP Argument Name Migration

The next server and client releases remove parenthetical MP-label qualifiers from public argument and result names. For example, `angle_tolerance_0_0_for_none` becomes `angle_tolerance` (`angleTolerance` in .NET and JavaScript). **0.0 still disables angle tolerance.** Units, defaults, presence, omission behavior, and SDK bindings do not change.

This is a source and JSON naming change. Update named arguments, result properties, object keys, keyword arguments, JSON/TextFormat payloads, and reflection-based lookups when adopting the new packages. Regenerate custom transport clients from the matching new protocol artifact. Binary protobuf field numbers, types, and presence remain unchanged. Runtime admission continues to use the existing exact-SA-target and behavioral compatibility rules independently of package versions.

Keep all existing names for **Evaluate Feature Check**, **Reset Cloud Bounding Box**, **Define point of view**, and **Get point of view parameters** because removing qualifiers causes collisions. Their entire signatures remain unchanged. The same method-level rule applies to future collisions.

Applications keep control of their package dependencies and server selection. Existing released packages and documentation snapshots retain their original names. Upgrade a consuming application by updating its client package for the same exact SA target, applying the table below, and running its tests. A new installer/server installation does not upgrade the application's client dependency. Previous immutable packages remain available for rollback.

The 2024 product has 168 renamed members in 83 commands. The 2026 product has 172 in 86 commands; its additions are Set Cone Properties, Set Line Properties, and Set Geom Relationship Auto Measure Nominal Feature. Shared result values follow their declaring type. The table lists every occurrence in the 2026 product; entries marked 2026 only are absent from 2024.

Names preserve established abbreviations and identifier conventions unrelated to parentheses. For example, `value_0_360` retains its necessary identifier prefix, and percent outside parentheses remains part of `per_unit_length_step_over_percent`.

## Rename Table

| Command | Direction | Previous Member | New Member | Meaning | Availability |
| --- | --- | --- | --- | --- | --- |
| Angle Between Line and Plane | input | `angle_tolerance_0_0_for_none` | `angle_tolerance` | 0.0 disables this tolerance. | Both targets |
| Angle Between Two Lines | input | `angle_tolerance_0_0_for_none` | `angle_tolerance` | 0.0 disables this tolerance. | Both targets |
| Angle Between Two Planes' normals | input | `angle_tolerance_0_0_for_none` | `angle_tolerance` | 0.0 disables this tolerance. | Both targets |
| Best Fit Transformation - Group to Group | input | `rms_tolerance_0_0_for_none` | `rms_tolerance` | 0.0 disables this tolerance. | Both targets |
| Best Fit Transformation - Group to Group | input | `maximum_absolute_tolerance_0_0_for_none` | `maximum_absolute_tolerance` | 0.0 disables this tolerance. | Both targets |
| Best Fit Transformation - Group to Group | input | `file_path_for_csv_text_report_requires_show_interface_true` | `file_path_for_csv_text_report` | Requires Show Interface to be true. | Both targets |
| Fit Geometry to Point Group | input | `fit_interface_tolerance_1_0_use_profile` | `fit_interface_tolerance` | -1.0 uses the profile tolerance. | Both targets |
| Fit Geometry to Point Group | input | `starting_condition_geometry_optional` | `starting_condition_geometry` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Fit Geometry to Point Group Projected to Plane | input | `fit_interface_tolerance_1_0_use_profile` | `fit_interface_tolerance` | -1.0 uses the profile tolerance. | Both targets |
| Fit Geometry to Point Group Projected to Plane | input | `starting_condition_geometry_optional` | `starting_condition_geometry` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Fit Geometry to Points | input | `fit_interface_tolerance_1_0_use_profile` | `fit_interface_tolerance` | -1.0 uses the profile tolerance. | Both targets |
| Fit Geometry to Points | input | `starting_condition_geometry_optional` | `starting_condition_geometry` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Get Cone Properties | output | `cone_end_point_in_working_coordinates` | `cone_end_point` | Expressed in working coordinates. | Both targets |
| Get Cone Properties | output | `cone_axis_in_working_coordinates` | `cone_axis` | Expressed in working coordinates. | Both targets |
| Get Measurement Weather Data | output | `temperature_deg_f` | `temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Get Measurement Weather Data | output | `pressure_in_hg` | `pressure` | Pressure in inches of mercury. | Both targets |
| Get Measurement Weather Data | output | `humidity_rh` | `humidity` | Relative humidity in percent. | Both targets |
| Get Slot Properties | output | `slot_transform_in_working_coordinates` | `slot_transform` | Expressed in working coordinates. | Both targets |
| Get Slot Properties | output | `center_in_working_coordinates` | `center` | Expressed in working coordinates. | Both targets |
| Get Slot Properties | output | `normal_direction_in_working_coordinates` | `normal_direction` | Expressed in working coordinates. | Both targets |
| Get Slot Properties | output | `centerline_pt_1_in_working_coordinates` | `centerline_pt_1` | Expressed in working coordinates. | Both targets |
| Get Slot Properties | output | `centerline_pt_2_in_working_coordinates` | `centerline_pt_2` | Expressed in working coordinates. | Both targets |
| Group To Surface Fit | input | `rms_tolerance_0_0_for_none` | `rms_tolerance` | 0.0 disables this tolerance. | Both targets |
| Group To Surface Fit | input | `maximum_absolute_tolerance_0_0_for_none` | `maximum_absolute_tolerance` | 0.0 disables this tolerance. | Both targets |
| Make Circle Fit Profile | input | `override_radial_offset_1_0_use_current` | `override_radial_offset` | -1.0 uses the current offset. | Both targets |
| Make Circle Fit Profile | input | `override_planar_offset_1_0_use_current` | `override_planar_offset` | -1.0 uses the current offset. | Both targets |
| Make Circle Fit Profile | input | `lock_radius_1_0_do_not_lock` | `lock_radius` | -1.0 leaves this dimension unlocked. | Both targets |
| Make Cone Fit Profile | input | `override_radial_offset_1_0_use_current` | `override_radial_offset` | -1.0 uses the current offset. | Both targets |
| Make Cone Fit Profile | input | `lock_angle_in_degrees_1_0_do_not_lock` | `lock_angle_in_degrees` | -1.0 leaves this dimension unlocked. | Both targets |
| Make Cylinder Fit Profile | input | `override_radial_offset_1_0_use_current` | `override_radial_offset` | -1.0 uses the current offset. | Both targets |
| Make Cylinder Fit Profile | input | `lock_radius_1_0_do_not_lock` | `lock_radius` | -1.0 leaves this dimension unlocked. | Both targets |
| Make Ellipse Fit Profile | input | `override_radial_offset_1_0_use_current` | `override_radial_offset` | -1.0 uses the current offset. | Both targets |
| Make Ellipse Fit Profile | input | `override_planar_offset_1_0_use_current` | `override_planar_offset` | -1.0 uses the current offset. | Both targets |
| Make Paraboloid Fit Profile | input | `override_radial_offset_1_0_use_current` | `override_radial_offset` | -1.0 uses the current offset. | Both targets |
| Make Paraboloid Fit Profile | input | `lock_focal_length_1_0_do_not_lock` | `lock_focal_length` | -1.0 leaves this dimension unlocked. | Both targets |
| Make Plane Fit Profile | input | `override_planar_offset_1_0_use_current` | `override_planar_offset` | -1.0 uses the current offset. | Both targets |
| Make Slot Fit Profile | input | `override_radial_offset_1_0_use_current` | `override_radial_offset` | -1.0 uses the current offset. | Both targets |
| Make Slot Fit Profile | input | `override_planar_offset_1_0_use_current` | `override_planar_offset` | -1.0 uses the current offset. | Both targets |
| Make Sphere Fit Profile | input | `override_radial_offset_1_0_use_current` | `override_radial_offset` | -1.0 uses the current offset. | Both targets |
| Make Sphere Fit Profile | input | `lock_radius_1_0_do_not_lock` | `lock_radius` | -1.0 leaves this dimension unlocked. | Both targets |
| Query Clouds to Objects | input | `rms_tolerance_0_0_for_none` | `rms_tolerance` | 0.0 disables this tolerance. | Both targets |
| Query Clouds to Objects | input | `maximum_absolute_tolerance_0_0_for_none` | `maximum_absolute_tolerance` | 0.0 disables this tolerance. | Both targets |
| Query Clouds to Surface | input | `rms_tolerance_0_0_for_none` | `rms_tolerance` | 0.0 disables this tolerance. | Both targets |
| Query Clouds to Surface | input | `maximum_absolute_tolerance_0_0_for_none` | `maximum_absolute_tolerance` | 0.0 disables this tolerance. | Both targets |
| Query Frame to Frame | output | `rx_roll` | `rx` | MP qualifier: Roll. | Both targets |
| Query Frame to Frame | output | `ry_pitch` | `ry` | MP qualifier: Pitch. | Both targets |
| Query Frame to Frame | output | `rz_yaw` | `rz` | MP qualifier: Yaw. | Both targets |
| Query Groups to Objects | input | `group_name_list_groups_to_project` | `group_name_list` | MP qualifier: Groups to Project. | Both targets |
| Query Groups to Objects | input | `object_name_list_objects_to_project_to` | `object_name_list` | MP qualifier: Objects to Project to. | Both targets |
| Query Groups to Objects | input | `rms_tolerance_0_0_for_none` | `rms_tolerance` | 0.0 disables this tolerance. | Both targets |
| Query Groups to Objects | input | `maximum_absolute_tolerance_0_0_for_none` | `maximum_absolute_tolerance` | 0.0 disables this tolerance. | Both targets |
| Query Points to Objects | input | `object_name_list_objects_to_project_to` | `object_name_list` | MP qualifier: Objects to Project to. | Both targets |
| Query Points to Objects | input | `rms_tolerance_0_0_for_none` | `rms_tolerance` | 0.0 disables this tolerance. | Both targets |
| Query Points to Objects | input | `maximum_absolute_tolerance_0_0_for_none` | `maximum_absolute_tolerance` | 0.0 disables this tolerance. | Both targets |
| Set Cone Properties | input | `cone_end_point_in_working_coordinates` | `cone_end_point` | Expressed in working coordinates. | 2026 only |
| Set Cone Properties | input | `cone_axis_in_working_coordinates` | `cone_axis` | Expressed in working coordinates. | 2026 only |
| Set Line Properties | input | `length_optional` | `length` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | 2026 only |
| Temperature Compensate a group | input | `scaling_origin_coordinate_frame` | `scaling_origin` | MP qualifier: coordinate frame. | Both targets |
| Temperature Compensate a group | input | `material_cte_1_deg_f` | `material_cte` | Coefficient per degree Fahrenheit. | Both targets |
| Temperature Compensate a group | input | `initial_temperature_f` | `initial_temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Temperature Compensate a group | input | `final_temperature_f` | `final_temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Cloud Display Control | input | `thin_draw_increment` | `thin` | MP qualifier: Draw Increment. | Both targets |
| Create Point Callout | input | `show_x_r` | `show_x` | MP qualifier: R. | Both targets |
| Create Point Callout | input | `show_y_theta` | `show_y` | MP qualifier: Theta. | Both targets |
| Create Point Callout | input | `show_z_phi` | `show_z` | MP qualifier: Phi. | Both targets |
| Create Point Callout | input | `show_ux_ur` | `show_ux` | MP qualifier: Ur. | Both targets |
| Create Point Callout | input | `show_uy_utheta` | `show_uy` | MP qualifier: Utheta. | Both targets |
| Create Point Callout | input | `show_uz_uphi` | `show_uz` | MP qualifier: Uphi. | Both targets |
| Get Gradient At Projected Point On Surface Edge | input | `surface_edge_b_spline` | `surface_edge` | MP qualifier: B-Spline. | Both targets |
| Construct Points Cylindrically Shifted | input | `theta_shift_degrees` | `theta_shift` | Angle in degrees. | Both targets |
| Create Hidden Point Rod | input | `target_to_target_distance` | `a_to_b_distance` | MP qualifier: Target to Target. | Both targets |
| Create Hidden Point Rod | input | `target_to_tip_distance` | `a_to_c_distance` | MP qualifier: Target to Tip. | Both targets |
| Construct Sphere | input | `sphere_center_in_working_coordinates` | `sphere_center` | Expressed in working coordinates. | Both targets |
| Direct CAD Access | input | `surface_normals_mode_1_or_2` | `surface_normals_mode` | MP qualifier: 1 or 2. | Both targets |
| Direct CAD Access | input | `cad_file_units_leave_blank_to_use_the_units_specified_in_the_file` | `cad_file_units` | An empty value uses the units specified in the file. | Both targets |
| Export ASCII Point Set | input | `maximum_precision_scientific_notation` | `maximum_precision` | MP qualifier: Scientific Notation. | Both targets |
| Export ASCII Points | input | `maximum_precision_scientific_notation` | `maximum_precision` | MP qualifier: Scientific Notation. | Both targets |
| Export Vector Container to ASCII File | input | `overwrite_existing_file_false_append` | `overwrite_existing_file` | False appends to the existing file. | Both targets |
| Export Vector Container to ASCII File | input | `use_full_precision_scientific_notation` | `use_full_precision` | MP qualifier: Scientific Notation. | Both targets |
| Import SA File | input | `selected_collections_optional` | `selected_collections` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Make GD&T Feature Check Annotation | input | `per_unit_area_length_distance` | `per_unit_length_distance` | MP qualifier: area. | Both targets |
| Make GD&T Feature Check Annotation | input | `per_unit_area_length_step_over_percent` | `per_unit_length_step_over_percent` | MP qualifier: area. | Both targets |
| LR Self Test | output | `reference_arm_length_inches` | `reference_arm_length` | Length in inches. | Both targets |
| LR Self Test | output | `mirror_measurement_range_mean_inches` | `mirror_measurement_range_mean` | Length in inches. | Both targets |
| LR Self Test | output | `mirror_measurement_range_standard_deviation_inches` | `mirror_measurement_range_standard_deviation` | Length in inches. | Both targets |
| LR Self Test - Linearization | output | `linearity_khz` | `linearity` | Frequency in kilohertz. | Both targets |
| Get Current Instrument Position Update | output | `y_or_theta_degrees` | `y_or_theta` | Angle in degrees. | Both targets |
| Get Current Instrument Position Update | output | `z_or_phi_degrees` | `z_or_phi` | Angle in degrees. | Both targets |
| Get Current Instrument Position Update | output | `time_since_update_seconds` | `time_since_update` | Time in seconds. | Both targets |
| Get Current Instrument Position Update | output | `timestamp_approximate` | `timestamp` | MP qualifier: Approximate. | Both targets |
| Locate Instruments (USMN) | input | `analysis_time_limit_minutes` | `analysis_time_limit` | Time in minutes; 0 disables the time limit. | Both targets |
| Create Templated Instrument (USMN) | input | `azimuth_weight` | `component_1_weight` | MP qualifier: Azimuth. | Both targets |
| Create Templated Instrument (USMN) | input | `elevation_weight` | `component_2_weight` | MP qualifier: Elevation. | Both targets |
| Create Templated Instrument (USMN) | input | `distance_weight` | `component_3_weight` | MP qualifier: Distance. | Both targets |
| Get Tracker/EDM Theodolite Uncertainties | output | `theta_dispersion_arcseconds` | `theta_dispersion` | Angle in arcseconds. | Both targets |
| Get Tracker/EDM Theodolite Uncertainties | output | `phi_dispersion_arcseconds` | `phi_dispersion` | Angle in arcseconds. | Both targets |
| Get Tracker/EDM Theodolite Uncertainties | output | `distance_ppm` | `distance` | Value in parts per million. | Both targets |
| Set Tracker/EDM Theodolite Uncertainties | input | `theta_dispersion_arcseconds` | `theta_dispersion` | Angle in arcseconds. | Both targets |
| Set Tracker/EDM Theodolite Uncertainties | input | `phi_dispersion_arcseconds` | `phi_dispersion` | Angle in arcseconds. | Both targets |
| Set Tracker/EDM Theodolite Uncertainties | input | `distance_ppm` | `distance` | Value in parts per million. | Both targets |
| Get Instrument Weather Setting | output | `temperature_fahrenheit` | `temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Get Instrument Weather Setting | output | `pressure_mmhg` | `pressure` | Pressure in millimeters of mercury. | Both targets |
| Get Instrument Weather Setting | output | `relative_humidity_percent` | `relative_humidity` | Relative humidity in percent. | Both targets |
| Set Instrument Weather Setting | input | `temperature_fahrenheit` | `temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Set Instrument Weather Setting | input | `pressure_mmhg` | `pressure` | Pressure in millimeters of mercury. | Both targets |
| Set Instrument Weather Setting | input | `relative_humidity_percent` | `relative_humidity` | Relative humidity in percent. | Both targets |
| Get Instrument Part Temperature | output | `part_temperature_fahrenheit` | `part_temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Compute CTE Scale Factor | input | `material_cte_per_degree_fahrenheit` | `material_cte` | Coefficient per degree Fahrenheit. | Both targets |
| Compute CTE Scale Factor | input | `initial_temperature_fahrenheit` | `initial_temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Compute CTE Scale Factor | input | `final_temperature_fahrenheit` | `final_temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Get Instrument Interface Response Timeout | output | `timeout_seconds` | `timeout` | Time in seconds. | Both targets |
| Set Instrument Interface Response Timeout | input | `timeout_seconds` | `timeout` | Time in seconds. | Both targets |
| Ask for User Decision from Image | input | `window_width_0_default` | `window_width` | 0 uses the default window dimension. | Both targets |
| Ask for User Decision from Image | input | `window_height_0_default` | `window_height` | 0 uses the default window dimension. | Both targets |
| Ask for User Decision from Strings | input | `button1_text_empty_to_hide_button` | `button1_text` | An empty string hides the button. | Both targets |
| Ask for User Decision from Strings | input | `button2_text_empty_to_hide_button` | `button2_text` | An empty string hides the button. | Both targets |
| Ask for User Decision from Strings | input | `button3_text_empty_to_hide_button` | `button3_text` | An empty string hides the button. | Both targets |
| Get Geom Relationship Auto Vectors | output | `auto_vectors_nominal_avn_enabled` | `auto_vectors_nominal_enabled` | MP qualifier: AVN. | Both targets |
| Get Geom Relationship Auto Vectors | output | `auto_vectors_nominal_avn_name` | `auto_vectors_nominal_name` | MP qualifier: AVN. | Both targets |
| Get Geom Relationship Auto Vectors | output | `auto_vectors_fit_avf_enabled` | `auto_vectors_fit_enabled` | MP qualifier: AVF. | Both targets |
| Get Geom Relationship Auto Vectors | output | `auto_vectors_fit_avf_name` | `auto_vectors_fit_name` | MP qualifier: AVF. | Both targets |
| Set Geom Relationship Auto Measure Nominal Feature | input | `trap_clouds_false_geometry` | `trap_clouds` | False selects geometry. | 2026 only |
| Set Relationship Voxel Cloud Display | input | `voxel_size_1_0_autodetect` | `voxel_size` | -1.0 selects automatic detection. | Both targets |
| Set Relationship Voxel Cloud Display | input | `voxel_rendering_diameter_1_0_fast` | `voxel_rendering_diameter` | -1.0 selects fast rendering. | Both targets |
| Relationship Watch Window Template | input | `show_deviation_x_rx` | `show_deviation_x` | MP qualifier: Rx. | Both targets |
| Relationship Watch Window Template | input | `show_deviation_y_ry` | `show_deviation_y` | MP qualifier: Ry. | Both targets |
| Relationship Watch Window Template | input | `show_deviation_z_rz` | `show_deviation_z` | MP qualifier: Rz. | Both targets |
| Create Chart from Vector Group | input | `template_chart_name_optional` | `template_chart_name` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Define Report Template | input | `report_page_settings_sa_report_only` | `report_page_settings` | MP qualifier: SA Report only. | Both targets |
| Make New SA Report | input | `sa_report_template_optional` | `sa_report_template` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Quick Report | input | `report_name_optional` | `report_name` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Save Current View (BMP/JPG/PNG/GIF/TIFF) | input | `render_scale_factor_1_0_uses_window_size` | `render_scale_factor` | 1.0 uses the window size. | Both targets |
| Set Calibration Appliance Node Instrument Dwell Time | input | `measurement_dwell_time_seconds` | `measurement_dwell_time` | Time in seconds. | Both targets |
| Set Robot Calibration Tool Frame | input | `tool_frame_relative_to_flange` | `tool_frame` | MP qualifier: relative to flange. | Both targets |
| Set Robot Calibration Measurement Offset In Tool Frame | input | `measurement_frame_relative_to_tool` | `measurement_frame` | MP qualifier: relative to tool. | Both targets |
| Scale Bar Check | input | `current_temperature_f` | `current_temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Scale Bar Check | input | `material_cte_ppm_f` | `material_cte` | Coefficient in parts per million per degree Fahrenheit. | Both targets |
| Get Angular Representation | output | `value_0_360_false_180` | `value_0_360` | False selects the +/-180 angular representation. | Both targets |
| Get Screen Resolution | input | `display_1_primary` | `display` | -1 selects the primary display. | Both targets |
| Set Angular Representation | input | `value_0_360_false_180` | `value_0_360` | False selects the +/-180 angular representation. | Both targets |
| Set Collection Notes | input | `append_false_overwrite` | `append` | False overwrites existing notes. | Both targets |
| Set Folder Notes | input | `append_false_overwrite` | `append` | False overwrites existing notes. | Both targets |
| Set Object Notes | input | `append_false_overwrite` | `append` | False overwrites existing notes. | Both targets |
| Set Point Notes | input | `append_false_overwrite` | `append` | False overwrites existing notes. | Both targets |
| Set User Interface Profile | input | `profile_file_name_optional` | `profile_file_name` | Optional in the MP editor; the existing API presence and omission behavior is unchanged. | Both targets |
| Auto-Range and Set Vector Group Colorization (All) | input | `colorization_options_uses_mode_only` | `colorization_options` | MP qualifier: Uses Mode Only. | Both targets |
| Auto-Range and Set Vector Group Colorization (Selected) | input | `colorization_options_uses_mode_only` | `colorization_options` | MP qualifier: Uses Mode Only. | Both targets |
| Highlight Objects | input | `object_names_empty_to_clear_all` | `object_names` | An empty selection clears all highlights. | Both targets |
| Highlight Point | input | `point_name_empty_to_clear_all` | `point_name` | An empty selection clears all highlights. | Both targets |
| Highlight Relationships | input | `relationships_empty_to_clear_all` | `relationships` | An empty selection clears all highlights. | Both targets |
| Show / Hide by Object Type | input | `hide_show_false` | `hide` | False shows the objects. | Both targets |
| Show / Hide Points | input | `show_hide_false` | `show` | False hides the points. | Both targets |
| Get Observation Info | output | `temperature_fahrenheit` | `temperature` | Temperature in degrees Fahrenheit. | Both targets |
| Get Observation Info | output | `pressure_in_hg` | `pressure` | Pressure in inches of mercury. | Both targets |
| Get Observation Info | output | `relative_humidity_percent` | `relative_humidity` | Relative humidity in percent. | Both targets |
| LR Get Most Recent SNR Info | output | `peak_value_db` | `peak_value` | Value in decibels. | Both targets |
| LR Get Most Recent SNR Info | output | `measured_range_meters` | `measured_range` | Range in meters. | Both targets |
| LR Self Test - Flip Test | output | `front_range_inches` | `front_range` | Length in inches. | Both targets |
| LR Self Test - Flip Test | output | `front_azimuth_degrees` | `front_azimuth` | Angle in degrees. | Both targets |
| LR Self Test - Flip Test | output | `front_elevation_degrees` | `front_elevation` | Angle in degrees. | Both targets |
| LR Self Test - Flip Test | output | `back_range_inches` | `back_range` | Length in inches. | Both targets |
| LR Self Test - Flip Test | output | `back_azimuth_degrees` | `back_azimuth` | Angle in degrees. | Both targets |
| LR Self Test - Flip Test | output | `back_elevation_degrees` | `back_elevation` | Angle in degrees. | Both targets |
| LR Self Test - Flip Test | output | `front_back_difference_range_inches` | `front_back_difference_range` | Length in inches. | Both targets |
| LR Self Test - Flip Test | output | `front_back_difference_azimuth_degrees` | `front_back_difference_azimuth` | Angle in degrees. | Both targets |
| LR Self Test - Flip Test | output | `front_back_difference_elevation_degrees` | `front_back_difference_elevation` | Angle in degrees. | Both targets |
| LR Self Test - LO Sep | output | `primary_lo_index` | `primary_lo` | Indexing starts at 1. | Both targets |
| LR Self Test - LO Sep | output | `secondary_lo_index` | `secondary_lo` | Indexing starts at 1. | Both targets |
| LR Self Test - LO Sep | output | `primary_lo_range_mean_inches` | `primary_lo_range_mean` | Length in inches. | Both targets |
| LR Self Test - LO Sep | output | `primary_lo_range_standard_deviation_inches` | `primary_lo_range_standard_deviation` | Length in inches. | Both targets |
| LR Self Test - LO Sep | output | `secondary_lo_range_mean_inches` | `secondary_lo_range_mean` | Length in inches. | Both targets |
| LR Self Test - LO Sep | output | `secondary_lo_range_standard_deviation_inches` | `secondary_lo_range_standard_deviation` | Length in inches. | Both targets |

## Validation and Release

Target-owned `mp-argument-name-migration.json` files record each message, field number, old/new name, and exact MP label. They are review evidence for issue #217, not an operation generator or a supported-command inventory.

`eng/Verify-Protocol.ps1 -AgainstRef <base>` preserves raw Buf diagnostics, checks WIRE compatibility, and applies the normal FILE rules after restoring only the reviewed names in an ephemeral descriptor. The exception activates only for the pinned old descriptor fingerprint. Every other baseline receives normal strict checks. Optional synthetic oneof names may change with a field name; field presence cannot change. Historical snapshots and raw vendor evidence must not be rewritten.

Before publication, run both target builds/tests, regenerate all six clients using verified candidate protocol artifacts, test actual packages across supported server generations, validate installer package ingestion, and build the documentation site. Publish server artifacts before dependent clients and catalog entries. Cut new documentation snapshots only after their packages are published. Candidate source-commit artifacts must not be labeled GitHub releases.
