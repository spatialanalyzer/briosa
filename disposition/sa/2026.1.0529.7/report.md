# SA 2026.1.0529.7 command disposition report

This deterministic report summarizes Briosa-authored disposition metadata. It does not republish installed vendor documentation or generated SDK source.

## Inventory

- Path: `../../../inventory/sa/2026.1.0529.7/inventory.json`
- SHA-256: `8a5e16b0fda8ebda70219b2c795af0c1b57004b0b048a32392d5b3253c97e502`
- Commands: 1412
- Disposition shards: 30

## Dispositions

| Disposition | Count |
| --- | ---: |
| `approved_candidate` | 673 |
| `blocked` | 56 |
| `intentional_exclusion` | 476 |
| `sdk_unavailable` | 207 |

## Review states

| Review state | Count |
| --- | ---: |
| `needs_re_review` | 0 |
| `reviewed` | 1412 |
| `unreviewed` | 0 |

## Command shape resolution

| Status | Commands |
| --- | ---: |
| `resolved` | 673 |
| `blocked` | 56 |
| `not_applicable` | 683 |

- Resolved arguments: 2508
- Required inputs: 1756
- Optional inputs: 250
- Omitted SDK setters: 64
- Reviewed catalog defaults: 186
- Proposed defaults needing review: 536
- A generated SA 2026 VB value remains inactive review evidence unless a matching ObjectiveSA prior-release default corroborates it without an exact-target conflict.

## Categories

| Category | Entries | Approved | Excluded | SDK unavailable | Blocked | Unresolved | Unreviewed | Needs re-review |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| AccumulatorMathOperations | 8 | 0 | 8 | 0 | 0 | 0 | 0 | 0 |
| AnalysisOperations | 189 | 103 | 40 | 31 | 15 | 15 | 0 | 0 |
| CloudAndMeshOperations | 1 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| CloudMeshOps | 28 | 19 | 0 | 8 | 1 | 1 | 0 | 0 |
| ConstructionOperations | 278 | 134 | 94 | 32 | 18 | 18 | 0 | 0 |
| DimensionOperations | 1 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| Dimensions | 19 | 2 | 4 | 13 | 0 | 0 | 0 | 0 |
| EventOperations | 1 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| Events | 5 | 4 | 0 | 1 | 0 | 0 | 0 | 0 |
| ExcelDirectConnect | 17 | 0 | 17 | 0 | 0 | 0 | 0 | 0 |
| FileOperations | 125 | 34 | 62 | 20 | 9 | 9 | 0 | 0 |
| GDT | 40 | 18 | 10 | 7 | 5 | 5 | 0 | 0 |
| GDTOperations | 3 | 0 | 0 | 0 | 3 | 3 | 0 | 0 |
| GoogleSheets | 18 | 0 | 18 | 0 | 0 | 0 | 0 | 0 |
| GoogleSheetsOperations | 1 | 0 | 1 | 0 | 0 | 0 | 0 | 0 |
| InstrumentOperations | 185 | 129 | 16 | 40 | 0 | 0 | 0 | 0 |
| MPSubroutines | 4 | 0 | 4 | 0 | 0 | 0 | 0 | 0 |
| MPTaskOverview | 11 | 0 | 11 | 0 | 0 | 0 | 0 | 0 |
| MSOfficeReportingOperations | 14 | 0 | 14 | 0 | 0 | 0 | 0 | 0 |
| ProcessFlowOperations | 25 | 1 | 21 | 2 | 1 | 1 | 0 | 0 |
| RelationshipOperations | 67 | 48 | 5 | 13 | 1 | 1 | 0 | 0 |
| ReportingOperations | 71 | 52 | 8 | 11 | 0 | 0 | 0 | 0 |
| RobotCalibrationApplianceNodeOperations | 25 | 23 | 0 | 2 | 0 | 0 | 0 | 0 |
| RobotOperations | 33 | 28 | 0 | 5 | 0 | 0 | 0 | 0 |
| ScalarMathOperations | 21 | 0 | 21 | 0 | 0 | 0 | 0 | 0 |
| ScaleBars | 3 | 3 | 0 | 0 | 0 | 0 | 0 | 0 |
| UtilityOperations | 105 | 33 | 60 | 12 | 0 | 0 | 0 | 0 |
| Variables | 41 | 0 | 41 | 0 | 0 | 0 | 0 | 0 |
| Vector Operations | 22 | 11 | 10 | 1 | 0 | 0 | 0 | 0 |
| ViewControl | 51 | 31 | 11 | 9 | 0 | 0 | 0 | 0 |

## Unresolved work by risk effect

| Value | Count |
| --- | ---: |
| `mutating` | 43 |
| `read_only` | 13 |

## Unresolved work by risk flag

| Value | Count |
| --- | ---: |
| `filesystem_read` | 3 |
| `filesystem_write` | 8 |
| `long_running` | 17 |

## Unresolved work by data classification

| Value | Count |
| --- | ---: |
| `geometry` | 42 |
| `measurement` | 18 |
| `non_sensitive` | 1 |
| `object_identifier` | 48 |
| `path` | 11 |
| `proprietary` | 5 |

## Unresolved work by value family

| Value | Count |
| --- | ---: |
| `b_spline_fit_options` | 1 |
| `cloud_thinning_mode` | 1 |
| `collection_group_name_list` | 1 |
| `collection_instrument_id_list` | 1 |
| `collection_object_name` | 39 |
| `collection_object_name_list` | 14 |
| `collection_object_name_ref_list` | 1 |
| `coordinate_system_type` | 2 |
| `export_data_delimiter_type` | 2 |
| `export_target_name_format` | 2 |
| `file_reference` | 10 |
| `fit_constraint_scalar_options` | 1 |
| `floating_point` | 19 |
| `gd_and_t_options_check_validator_type` | 1 |
| `gdt_check_validator_type` | 1 |
| `gdt_distance_between_mode` | 1 |
| `item_type` | 1 |
| `logical` | 27 |
| `mesh_orientation_type` | 1 |
| `none` | 1 |
| `point_filter_input_type` | 1 |
| `point_name` | 2 |
| `point_name_list` | 4 |
| `sigmoidal_gap_constraint_options` | 1 |
| `string` | 11 |
| `tolerance_vector_options` | 1 |
| `transform` | 2 |
| `vector3` | 7 |
| `vector_name_list` | 1 |
| `whole_number` | 13 |
| `world_transform` | 1 |

## Reason codes

| Value | Count |
| --- | ---: |
| `argument_semantics_unresolved` | 43 |
| `client_owned_external_integration` | 74 |
| `client_owned_office_integration` | 14 |
| `client_owned_serialization` | 22 |
| `client_owned_spreadsheet_integration` | 38 |
| `client_owned_state_and_control_flow` | 70 |
| `client_owned_user_experience` | 75 |
| `client_owned_value_computation` | 64 |
| `client_owned_value_construction` | 100 |
| `command_shape_resolved` | 673 |
| `file_semantics_unresolved` | 11 |
| `filesystem_operation` | 49 |
| `interactive_operation` | 40 |
| `long_running_operation` | 100 |
| `operator_ui_dependency` | 18 |
| `read_only_operation` | 153 |
| `sdk_binding_unavailable` | 111 |
| `sdk_binding_unresolved` | 5 |
| `sdk_command_not_observed` | 39 |
| `sdk_input_binding_unavailable` | 57 |
| `server_lifecycle_boundary` | 1 |
| `state_mutation` | 458 |

## Blockers

| Value | Count |
| --- | ---: |
| `https://github.com/spatialanalyzer/briosa/issues/79` | 45 |
| `https://github.com/spatialanalyzer/briosa/issues/80` | 11 |

## Delivery waves

| Value | Count |
| --- | ---: |
| `wave_1` | 101 |
| `wave_2` | 229 |
| `wave_3` | 52 |
| `wave_4` | 291 |

## Command-specific shape discrepancies

| Category path | MP step | Inventory key | Discrepancy | Owner | Dependency |
| --- | --- | --- | --- | --- | --- |
| AnalysisOperations | Get Cylinder Properties | documentation:AnalysisOperations/GetCylinderProperties.htm | `sdk_argument_not_documented` (arguments 7, 8, 9, 10, 11) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations / PipeRelationships | Pipe Relationship Force Cut to Frame | documentation:AnalysisOperations/PipeRelationships/PipeRelationshipForceCut.htm | `sdk_argument_not_documented` (arguments 5, 6) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Auto Vectors Nominal (AVN) | documentation:AnalysisOperations/RelationshipAttributes/SetGeomRelationshipAuto.htm | `sdk_argument_not_documented` (arguments 3, 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations / RelationshipAttributes | Set Relationship Auto Vectors Fit (AVF) | documentation:AnalysisOperations/RelationshipAttributes/SetRelationshipAutoVectors.htm | `sdk_argument_not_documented` (arguments 2, 3) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations / RelationshipAttributesScalarTypes | Get Relationship Fit Constraints (Scalar Type) | documentation:AnalysisOperations/RelationshipAttributesScalarTypes/GetRelationshipFitConstraints.htm | `sdk_argument_not_documented` (arguments 2, 3, 4, 5) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Point Properties | documentation:AnalysisOperations/SetPointProperties.htm | `sdk_argument_not_documented` (arguments 3, 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| CloudMeshOps / CloudFilters | Filter Clouds to Vector Groups - Resolve Points | documentation:CloudMeshOps/CloudFilters/FilterCloudsToVectorGroups.htm | `sdk_argument_not_documented` (arguments 8, 9) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / OtherMPTypes | Make a Collection Item Name Reference List - Wildcard Selection | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionItemNameReference.htm | `exact_interop_binding_missing` (arguments 2) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointsandGroups | Construct Point at Intersection of Planes | documentation:ConstructionOperations/PointsandGroups/ConstructPointAtIntersectionOfPlanes.htm | `missing_input_arguments_section` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointsandGroups | Construct Point at Intersection of Planes | documentation:ConstructionOperations/PointsandGroups/ConstructPointAtIntersectionOfPlanes.htm | `sdk_argument_not_documented` (arguments 1, 2, 3) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointsandGroups | Construct Point Groups from Vector Groups | documentation:ConstructionOperations/PointsandGroups/ConstructPointGroupsfromVectorGroups.htm | `missing_input_arguments_section` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointsandGroups | Construct Point Groups from Vector Groups | documentation:ConstructionOperations/PointsandGroups/ConstructPointGroupsfromVectorGroups.htm | `sdk_argument_not_documented` (arguments 1, 2, 3, 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PolygonizedSurfaces | Construct Polygonized Surface from Point Clouds | documentation:ConstructionOperations/PolygonizedSurfaces/ConstructPolygonizedSurfacefromPointClouds.htm | `exact_interop_binding_missing` (arguments 1) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Surfaces | Construct Surface From Cylinder | documentation:ConstructionOperations/Surfaces/ConstructSurfaceFromCylinder.htm | `sdk_argument_not_documented` (arguments 3) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / VectorGroups | Make a Vector Name Ref List From a Vector Group | documentation:ConstructionOperations/VectorGroups/MakeAVectorNameRefList.htm | `missing_input_arguments_section` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / VectorGroups | Make a Vector Name Ref List From a Vector Group | documentation:ConstructionOperations/VectorGroups/MakeAVectorNameRefList.htm | `sdk_argument_not_documented` (arguments 1) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / VectorGroups | Make Vector Names Unique in Vector Group | documentation:ConstructionOperations/VectorGroups/MakeVectorNamesUniqueIn.htm | `missing_return_arguments_section` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| FileOperations | Save | documentation:FileOperations/Save.htm | `file_behavior_unresolved` | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations | Save As | documentation:FileOperations/SaveAs.htm | `file_behavior_unresolved` (arguments 0, 1, 2) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations | Save As Read-Only Template | documentation:FileOperations/SaveAsReadOnlyTemplate.htm | `file_behavior_unresolved` (arguments 0) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations / XML | Import Nominals from XML File | documentation:FileOperations/XML/ImportNominalsFromXMLFile.htm | `file_behavior_unresolved` (arguments 0) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations / XML | Merge Measurements into XML File | documentation:FileOperations/XML/MergeMeasurementsintoXML.htm | `file_behavior_unresolved` (arguments 0, 1) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| GDT | Datum Alignment | documentation:GDT/DatumAlignment.htm | `sdk_argument_not_documented` (arguments 3) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDT | Enable/Disable Datum Alignment for Feature Check | documentation:GDT/EnableDisableDatumAlignment.htm | `sdk_argument_not_documented` (arguments 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDT | Evaluate Feature Check | documentation:GDT/EvaluateFeatureCheck.htm | `sdk_argument_not_documented` (arguments 21) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDT | Evaluate Feature Checks | documentation:GDT/EvaluateFeatureChecks.htm | `sdk_argument_not_documented` (arguments 4, 5) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDT | Set GD&T Options | documentation:GDT/SetGDTOptions.htm | `exact_interop_binding_missing` (arguments 1, 8) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ProcessFlowOperations | Output SA Report to PDF | documentation:ProcessFlowOperations/OutputSAReportToPDF.htm | `file_behavior_unresolved` (arguments 0, 1, 2) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| AnalysisOperations | Create Point Uncertainty Cloud Point Sets | sdk:AnalysisOperations.txt#19 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Create Point Uncertainty Cloud Point Sets | sdk:AnalysisOperations.txt#19 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6, 7) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Point Weights From Uncertainties | sdk:AnalysisOperations.txt#20 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Point Weights From Uncertainties | sdk:AnalysisOperations.txt#20 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Get Point Coordinate | sdk:AnalysisOperations.txt#21 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Get Point Coordinate | sdk:AnalysisOperations.txt#21 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Transform for i-th Frame in Frame Set | sdk:AnalysisOperations.txt#37 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Transform for i-th Frame in Frame Set | sdk:AnalysisOperations.txt#37 | `sdk_argument_not_documented` (arguments 1, 2) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Get Euler Parameters for i-th Frame in Frame Set | sdk:AnalysisOperations.txt#38 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Get Euler Parameters for i-th Frame in Frame Set | sdk:AnalysisOperations.txt#38 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6, 7, 8) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Get Euler Parameters for Frame | sdk:AnalysisOperations.txt#39 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Get Euler Parameters for Frame | sdk:AnalysisOperations.txt#39 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6, 7) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Line Properties | sdk:AnalysisOperations.txt#55 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Line Properties | sdk:AnalysisOperations.txt#55 | `sdk_argument_not_documented` (arguments 1, 2, 3) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Cylinder Properties | sdk:AnalysisOperations.txt#60 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Cylinder Properties | sdk:AnalysisOperations.txt#60 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6, 7, 8, 9) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Ellipse Properties | sdk:AnalysisOperations.txt#62 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| AnalysisOperations | Set Ellipse Properties | sdk:AnalysisOperations.txt#62 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| CloudAndMeshOperations | Clear Cloud Point Deviations | sdk:CloudAndMeshOperations.txt#7 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| CloudAndMeshOperations | Clear Cloud Point Deviations | sdk:CloudAndMeshOperations.txt#7 | `sdk_argument_not_documented` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / BSplines | Construct B-Spline From Point Set | sdk:ConstructionOperations_BSplines.txt#3 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / BSplines | Construct B-Spline From Point Set | sdk:ConstructionOperations_BSplines.txt#3 | `sdk_argument_not_documented` (arguments 1, 2) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / BSplines | Construct B-Splines From Intersection of Plane and Mesh | sdk:ConstructionOperations_BSplines.txt#9 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / BSplines | Construct B-Splines From Intersection of Plane and Mesh | sdk:ConstructionOperations_BSplines.txt#9 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Ellipses | Construct Ellipse | sdk:ConstructionOperations_Ellipses.txt#1 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Ellipses | Construct Ellipse | sdk:ConstructionOperations_Ellipses.txt#1 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Construct Frames By Projecting Frames On Mesh Along Frame Direction | sdk:ConstructionOperations_Frames.txt#16 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Construct Frames By Projecting Frames On Mesh Along Frame Direction | sdk:ConstructionOperations_Frames.txt#16 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Construct Frames By Projecting Frames On Mesh Along Reference Direction | sdk:ConstructionOperations_Frames.txt#17 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Construct Frames By Projecting Frames On Mesh Along Reference Direction | sdk:ConstructionOperations_Frames.txt#17 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Add Surface To Mesh Offset Along Reference Direction | sdk:ConstructionOperations_Frames.txt#18 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Add Surface To Mesh Offset Along Reference Direction | sdk:ConstructionOperations_Frames.txt#18 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Construct Frame From Transform In World | sdk:ConstructionOperations_Frames.txt#3 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Frames | Construct Frame From Transform In World | sdk:ConstructionOperations_Frames.txt#3 | `sdk_argument_not_documented` (arguments 1) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Planes | Construct Planes, Bisect 2 Planes | sdk:ConstructionOperations_Planes.txt#4 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / Planes | Construct Planes, Bisect 2 Planes | sdk:ConstructionOperations_Planes.txt#4 | `sdk_argument_not_documented` (arguments 1, 2) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointClouds | Create Cloud Thinning Settings | sdk:ConstructionOperations_PointClouds.txt#11 | `exact_interop_binding_missing` (arguments 0) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointClouds | Construct Point Cloud from Visible Cloud Points | sdk:ConstructionOperations_PointClouds.txt#5 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointClouds | Construct Point Cloud from Visible Cloud Points | sdk:ConstructionOperations_PointClouds.txt#5 | `sdk_argument_not_documented` (arguments 1) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointsAndGroups | Construct Points By Projecting Points On Mesh Along Direction | sdk:ConstructionOperations_PointsAndGroups.txt#26 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| ConstructionOperations / PointsAndGroups | Construct Points By Projecting Points On Mesh Along Direction | sdk:ConstructionOperations_PointsAndGroups.txt#26 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| DimensionOperations | Set Dimension Tolerance | sdk:DimensionOperations.txt#12 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| DimensionOperations | Set Dimension Tolerance | sdk:DimensionOperations.txt#12 | `sdk_argument_not_documented` (arguments 1, 2, 3, 4, 5, 6) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| EventOperations | Export Event Ref List | sdk:EventOperations.txt#4 | `file_behavior_unresolved` (arguments 0, 1, 2, 3) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations / FileExport | Export ASCII Points | sdk:FileOperations_FileExport.txt#1 | `file_behavior_unresolved` (arguments 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations / FileExport | Export ASCII Point Set | sdk:FileOperations_FileExport.txt#2 | `file_behavior_unresolved` (arguments 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations / FileImport | Import VSTARS Cameras | sdk:FileOperations_FileImport.txt#16 | `file_behavior_unresolved` (arguments 0) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| FileOperations / FileImport | Import Polyworks File | sdk:FileOperations_FileImport.txt#19 | `file_behavior_unresolved` (arguments 0, 1) | `briosa` | https://github.com/spatialanalyzer/briosa/issues/80 |
| GDTOperations / GDTAnalysis | Set Global Force Simultaneous Evaluation | sdk:GDTOperations_GDTAnalysis.txt#17 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDTOperations / GDTAnalysis | Set Global Force Simultaneous Evaluation | sdk:GDTOperations_GDTAnalysis.txt#17 | `sdk_argument_not_documented` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDTOperations / GDTAnalysis | Generate Feature Check Summary | sdk:GDTOperations_GDTAnalysis.txt#20 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDTOperations / GDTAnalysis | Generate Feature Check Summary | sdk:GDTOperations_GDTAnalysis.txt#20 | `sdk_argument_not_documented` (arguments 1) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDTOperations / GDTConstruction | Make Surface Face List From Surface | sdk:GDTOperations_GDTConstruction.txt#2 | `documentation_command_missing` | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| GDTOperations / GDTConstruction | Make Surface Face List From Surface | sdk:GDTOperations_GDTConstruction.txt#2 | `sdk_argument_not_documented` (arguments 1) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |
| RelationshipOperations / RelationshipAttributesScalarTypes | Get Relationship Sigmoidal Gap Fit Constraints | sdk:RelationshipOperations_RelationshipAttributesScalarTypes.txt#8 | `exact_interop_binding_missing` (arguments 9) | `hexagon` | https://github.com/spatialanalyzer/briosa/issues/79 |

## Proposed defaults requiring maintainer review

These values are evidence-backed proposals only. Their inputs continue to reject omission until a reviewed disposition explicitly activates a catalog default. Maintainer review is tracked by https://github.com/spatialanalyzer/briosa/issues/82.

| Category path | MP step | Argument | Candidate evidence |
| --- | --- | --- | --- |
| AnalysisOperations | Get i-th Collection Name | Collection Index | sa_2026_generated_vb=0 |
| AnalysisOperations | Get Timestamp for i-th Frame in Frame Set | Frame Set Index | sa_2026_generated_vb=0 |
| AnalysisOperations | Get Timestamp for i-th Point in Point Set | Point Set Index | sa_2026_generated_vb=0 |
| AnalysisOperations | Get Transform for i-th Frame in Frame Set | Frame Set Index | sa_2026_generated_vb=0 |
| AnalysisOperations | Is Object of Type | Object Type | sa_2026_generated_vb="Any" |
| AnalysisOperations | Mushroom Target Hole Inspection | Sphere Target Radius | sa_2026_generated_vb=0.0 |
| AnalysisOperations / PipeRelationships | Make pipe Relationship Cut | Pipe 1 - Make Cut | sa_2026_generated_vb=true |
| AnalysisOperations / PipeRelationships | Make pipe Relationship Cut | Pipe 1 - Create Frame | sa_2026_generated_vb=false |
| AnalysisOperations / PipeRelationships | Make pipe Relationship Cut | Pipe 2 - Make Cut | sa_2026_generated_vb=true |
| AnalysisOperations / PipeRelationships | Make pipe Relationship Cut | Pipe 2 - Create Frame | sa_2026_generated_vb=false |
| AnalysisOperations | Query Clouds to Objects | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| AnalysisOperations | Query Clouds to Surface | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| AnalysisOperations | Query Groups to Objects | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| AnalysisOperations | Query Groups to Objects | Show Results Dialog? | objectivesa_prior_release=false; sa_2026_generated_vb=true |
| AnalysisOperations | Query Points to Circle | Is Inside Measurement | sa_2026_generated_vb=true |
| AnalysisOperations | Query Points to Circle | Auto Scale Vectors to % of Radius | sa_2026_generated_vb=40 |
| AnalysisOperations | Query Points to Objects | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| AnalysisOperations | Query Points to Objects | Show Results Dialog? | objectivesa_prior_release=false; sa_2026_generated_vb=true |
| AnalysisOperations | Re-Compute Calculated Items | Refresh Filtered Cloud Data? | sa_2026_generated_vb=false |
| AnalysisOperations / RelationshipAttributes | Enable/Disable Relationships for Optimization | Enable? | sa_2026_generated_vb=false |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Criteria | Show in Report | sa_2026_generated_vb=true |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Criteria | Tolerance Options | sa_2026_generated_vb=[false,0.0,false,0.0] |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Criteria | Optimization: Delta Weight | sa_2026_generated_vb=0.0 |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Criteria | Optimization: Out of Tolerance Weight | sa_2026_generated_vb=0.0 |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Nominal Geometry | Compare To Nominal? | sa_2026_generated_vb=true |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Nominal Avg Point | Compare To Nominal? | sa_2026_generated_vb=true |
| AnalysisOperations / RelationshipAttributes | Set Geom Relationship Projection Plane | Project to Plane? | sa_2026_generated_vb=true |
| AnalysisOperations / RelationshipAttributes | Set Relationship Auto Vectors Group Default Prefix | Geom Rel AVN VG Default Prefix | sa_2026_generated_vb="GR-AVN-" |
| AnalysisOperations / RelationshipAttributes | Set Relationship Auto Vectors Group Default Prefix | Geom Rel AVF VG Default Prefix | sa_2026_generated_vb="GR-AVF-" |
| AnalysisOperations / RelationshipAttributes | Set Relationship Auto Vectors Group Default Prefix | Non-Geom Rel VG Default Prefix | sa_2026_generated_vb="Auto Vectors: " |
| AnalysisOperations / RelationshipAttributes | Set Relationship Orientation Fit Constraints (Vector Type) | Orientation Vector Constraint | sa_2026_generated_vb=[true,0.0,true,0.0,true,0.0,false,0.0,true,0.0,true,0.0,true,0.0,false,0.0] |
| AnalysisOperations / RelationshipAttributes | Set Relationship Position Fit Constraints (Vector Type) | Position Vector Constraint | sa_2026_generated_vb=[true,0.0,true,0.0,true,0.0,false,0.0,true,0.0,true,0.0,true,0.0,false,0.0] |
| AnalysisOperations / RelationshipAttributes | Set Relationship Projection Options | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| AnalysisOperations / RelationshipAttributes | Set Relationship Tolerance (Vector Type) | Vector Tolerance | sa_2026_generated_vb=[false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0] |
| AnalysisOperations / RelationshipAttributes | Set Relationship Weights Normalized | Pick Weighting Mode | sa_2026_generated_vb="Normalize on equation count" |
| AnalysisOperations / RelationshipAttributesScalarTypes | Set Object to Object Direction Relationship Fit Constraints | Angle Between Vectors Fit Constraints | sa_2026_generated_vb=[true,0.0,true,0.0] |
| AnalysisOperations / RelationshipAttributesScalarTypes | Set Object to Object Direction Relationship Fit Constraints | Mutual Perpendicular Length Fit Constraints | sa_2026_generated_vb=[true,0.0,true,0.0] |
| AnalysisOperations / RelationshipAttributesScalarTypes | Set Relationship Fit Constraints (Scalar Type) | Fit Constraint Options | sa_2026_generated_vb=[true,0.0,true,0.0] |
| AnalysisOperations / RelationshipAttributesScalarTypes | Set Relationship Tolerance (Scalar Type) | Tolerance Options | sa_2026_generated_vb=[false,0.0,false,0.0] |
| AnalysisOperations | Set Circle Properties | Center Coordinate | sa_2026_generated_vb=[0.0,0.0,0.0] |
| AnalysisOperations | Set Circle Properties | Normal Direction | sa_2026_generated_vb=[0.0,0.0,0.0] |
| AnalysisOperations | Set Circle Properties | Radius | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Set Cone Properties | Cone End Point (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| AnalysisOperations | Set Cone Properties | Cone Axis (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| AnalysisOperations | Set Cone Properties | Cone Length | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Set Cone Properties | Cone Theta Start | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Set Cone Properties | Cone Theta Span | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Set Cone Properties | Cone Included Angle | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Set Cone Properties | Cut Length from Apex | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Set Default Colorization Options | Colorization Options | sa_2026_generated_vb=["Continuous","Blue","Green","Red",false,true,false,100.0,1,false,0.1,false,false,true,false,0.5,-0.5,0.03,-0.03] |
| AnalysisOperations | Set Measurement Auxiliary Data | Value | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Sphere Axis Check | Sphere Target Radius | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Temperature Compensate a group | Material CTE (1/Deg F) | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Temperature Compensate a group | Initial Temperature (F) | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Temperature Compensate a group | Final Temperature (F) | sa_2026_generated_vb=0.0 |
| AnalysisOperations | Translate Objects by Delta | Delta Translation | sa_2026_generated_vb=[0.0,0.0,0.0] |
| CloudMeshOps / CloudFilters | Filter Clouds to BSplines | Minimum Proximity | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to BSplines | Maximum Proximity | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to BSplines | Output Type | sa_2026_generated_vb="Points" |
| CloudMeshOps / CloudFilters | Filter Clouds to Group | Proximity (0 for Closest Point only) | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Group | Maximum Number of Points (0 for Unlimited) | sa_2026_generated_vb=0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Group | Output Type | sa_2026_generated_vb="Points" |
| CloudMeshOps / CloudFilters | Filter Clouds to Line Segment | Minimum Proximity | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Line Segment | Maximum Proximity | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Line Segment | Output Type | sa_2026_generated_vb="Points" |
| CloudMeshOps / CloudFilters | Filter Clouds to Plane | Proximity | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Surface | Low Proximity | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Surface | High Proximity | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Surface | Skip Factor | sa_2026_generated_vb=0 |
| CloudMeshOps / CloudFilters | Filter Clouds to Surface | Output Type | sa_2026_generated_vb="Points" |
| CloudMeshOps / CloudFilters | Filter Clouds to Vector Groups - Resolve Clouds | Radial Cutoff | sa_2026_generated_vb=0.1 |
| CloudMeshOps / CloudFilters | Filter Clouds to Vector Groups - Resolve Clouds | Lower Cutoff | sa_2026_generated_vb=-0.1 |
| CloudMeshOps / CloudFilters | Filter Clouds to Vector Groups - Resolve Clouds | Upper Cutoff | sa_2026_generated_vb=0.1 |
| CloudMeshOps / CloudFilters | Get Cloud RGB Values | RGB Color Channel | sa_2026_generated_vb="Intensity" |
| CloudMeshOps / CloudFilters | Get Cloud RGB Values Near Point | Diameter | sa_2026_generated_vb=10.0 |
| CloudMeshOps / CloudFilters | Get Cloud RGB Values Near Point | RGB Color Channel | sa_2026_generated_vb="Intensity" |
| CloudMeshOps / CloudFilters | Subdivide Cloud by Point Spacing | Point Spacing | sa_2026_generated_vb=0.0 |
| CloudMeshOps / CloudFilters | Subdivide Cloud by Point Spacing | Minimum Points Per Group | sa_2026_generated_vb=0 |
| CloudMeshOps / CloudFilters | Subdivide Cloud by Point Spacing | Keep All Groups? | sa_2026_generated_vb=true |
| CloudMeshOps / CrossSections | Enable/Disable Cloud Cross Sections | Cross Section ID | sa_2026_generated_vb=0 |
| CloudMeshOps / CrossSections | Enable/Disable Cloud Cross Sections | Enable (TRUE) / Disable (FALSE)? | sa_2026_generated_vb=true |
| CloudMeshOps / CrossSections | Enable Single Cloud Cross Section | Cross Section ID | sa_2026_generated_vb=0 |
| CloudMeshOps / MeshOperations | Mesh Fill Holes | Maximum Triangle Length | sa_2026_generated_vb=-1.0 |
| CloudMeshOps / MeshOperations | Mesh Fill Holes | Tension | sa_2026_generated_vb=0.0 |
| CloudMeshOps / MeshOperations | Mesh Fill Holes | Unconditional Filling? | sa_2026_generated_vb=false |
| CloudMeshOps / MeshOperations | Mesh Fill Holes | Fill All Holes? | sa_2026_generated_vb=true |
| ConstructionOperations / Callouts | Create Picture Callout | Object for Callout Anchor Point | objectivesa_prior_release=null |
| ConstructionOperations / Callouts | Set I-th Callout Position in Callout View | Callout View Index | sa_2026_generated_vb=0 |
| ConstructionOperations / Callouts | Set I-th Callout Position in Callout View | X Position | sa_2026_generated_vb=0 |
| ConstructionOperations / Callouts | Set I-th Callout Position in Callout View | Y Position | sa_2026_generated_vb=0 |
| ConstructionOperations / Circles | Construct Circle | Circle Center (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Circles | Construct Circle | Circle Normal (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Circles | Construct Circle | Circle Radius | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Cones | Construct Cone | Cone End Point (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Cones | Construct Cone | Cone Axis (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Cones | Construct Cone | Cone Length | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Cones | Construct Cone | Cone Theta Start | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Cones | Construct Cone | Cone Theta Span | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Cones | Construct Cone | Cone Included Angle | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Cylinders | Construct Cylinder | Cylinder End Point (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Cylinders | Construct Cylinder | Cylinder Axis (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Cylinders | Construct Cylinder | Cylinder Diameter | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Cylinders | Construct Cylinder | Cylinder Length | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Cylinders | Construct Cylinder From End Points | Cylinder End Point A (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Cylinders | Construct Cylinder From End Points | Cylinder End Point B (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Cylinders | Construct Cylinder From End Points | Cylinder Diameter | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Folders | Delete Folders by Wildcard | Case Sensitive Search | sa_2026_generated_vb=true |
| ConstructionOperations / Folders | Delete Folders by Wildcard | Allow Deleting all Folders | sa_2026_generated_vb=false |
| ConstructionOperations / Frames | Construct Frame, Known Origin, Object Direction, Object Direction | Known Point Value in New Frame | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Frames | Construct Frame, 3 Planes | X Value on PLane | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Frames | Construct Frame, 3 Planes | Y Value on PLane | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Frames | Construct Frame, 3 Planes | Z Value on Plane | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Frames | Construct Mirror Cube Frame | Use Current Measurements Marked as Mirror Shots | sa_2026_generated_vb=true |
| ConstructionOperations / Frames | Construct Mirror Cube Frame | Nominal Cube Face Angle | sa_2026_generated_vb=90.0 |
| ConstructionOperations / Lines | Construct Line 2 Points (Vector Notation) | First Vector | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Lines | Construct Line 2 Points (Vector Notation) | Second Vector | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Lines | Construct Line From Instrument Shot | Observation Index | sa_2026_generated_vb=0 |
| ConstructionOperations / Lines | Construct Line Normal to Object | Line Length | sa_2026_generated_vb=1.0 |
| ConstructionOperations / OtherMPTypes | Add Collection Instruments to a Ref List - WildCard Selection | Collection Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Add Collection Instruments to a Ref List - WildCard Selection | Instrument Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name - Ensure Unique | Use Number Suffix? | sa_2026_generated_vb=false |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Ref List - By Type and Color | Object Type | sa_2026_generated_vb="Any" |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Ref List - By Type and Color | Object Color | sa_2026_generated_vb=[255,0,0] |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Ref List - By Type | Object Type | sa_2026_generated_vb="Any" |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Reference List - WildCard Selection | Collection Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Reference List - WildCard Selection | Object Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Reference List - WildCard Selection | Object Type | sa_2026_generated_vb="Any" |
| ConstructionOperations / OtherMPTypes | Make a Point Name - Ensure Unique | Use Number Suffix? | sa_2026_generated_vb=false |
| ConstructionOperations / OtherMPTypes | Make a Point Name Ref List - Wildcard Select | Collection Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Make a Point Name Ref List - Wildcard Select | Group Name Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Make a Point Name Ref List - Wildcard Select | Point Name Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Make an Event Reference List-Wildcard Selection | Collection Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / OtherMPTypes | Make an Event Reference List-Wildcard Selection | Event Wildcard Criteria | sa_2026_generated_vb="*" |
| ConstructionOperations / Perimeters | Construct Perimeter From Points | Open Perimeter? | sa_2026_generated_vb=false |
| ConstructionOperations / Planes | Construct Plane, Normal to Object, Through Point | Plane Edge Dimension | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Planes | Construct Plane | Plane Center (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Planes | Construct Plane | Plane Normal (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Planes | Construct Plane | Plane Edge Dimension | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Planes | Shift Plane | Shift Along Normal | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Planes | Shift Plane | Grow Bounds by Factor | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Cylindrical Cross Section Mode? | sa_2026_generated_vb=false |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Start Distance | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Section Spacing | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Proximity Threshold | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Maximum Section Count | sa_2026_generated_vb=0 |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Limit Cross Section Extent | sa_2026_generated_vb=false |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Radius Limit | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Project to Reference Surface | sa_2026_generated_vb=false |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Cloud Thinning Settings | sa_2026_generated_vb=["Nth Point",5,100,20000] |
| ConstructionOperations / PointClouds | Construct Cross Section Cloud | Update Existing Cloud | sa_2026_generated_vb=false |
| ConstructionOperations / PointClouds | Construct Point Cloud Limiting Probing Directions | Acceptance Angle | sa_2026_generated_vb=30.0 |
| ConstructionOperations / PointClouds | Construct Point Cloud Limiting Probing Directions | Hide Source Cloud | sa_2026_generated_vb=false |
| ConstructionOperations / PointClouds | Construct Point Clouds from Existing Clouds - Uniform Spacing | Desired Point Spacing | sa_2026_generated_vb=0.02 |
| ConstructionOperations / PointClouds | Construct Point Clouds from Existing Clouds - Uniform Spacing | Minimum Points Per Output Point | sa_2026_generated_vb=3 |
| ConstructionOperations / PointClouds | Extract Sphere Centers from Point Cloud | Desired Diameter | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointClouds | Extract Sphere Centers from Point Cloud | Extraction Tolerance | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointClouds | Extract Sphere Centers from Point Cloud | Minimum Point Count | sa_2026_generated_vb=50 |
| ConstructionOperations / PointsandGroups | Average a set of Groups | RMS Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Average a set of Groups | Maximum Absolute Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Average a set of Groups | Maximum Average Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct a Point in Working Coordinates | Working Coordinates | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / PointsandGroups | Construct Point at Intersection of B-Spline and Surfaces | Approximation Tolerance | sa_2026_generated_vb=0.001 |
| ConstructionOperations / PointsandGroups | Construct Point From Survey Target Center | Survey Target Type | sa_2026_generated_vb="Triangle" |
| ConstructionOperations / PointsandGroups | Construct Point From Survey Target Center | Search Diameter | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Point Group from Point Cloud | Point Prefix | sa_2026_generated_vb="pt" |
| ConstructionOperations / PointsandGroups | Construct Point Group from Point Cloud | Starting Point Number | sa_2026_generated_vb=0 |
| ConstructionOperations / PointsandGroups | Construct Point Group from Point Cloud | Point Offset | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Point Group from Point Cloud | Sub-Sampling? | sa_2026_generated_vb=false |
| ConstructionOperations / PointsandGroups | Construct Point Group from Point Cloud | Sub-Sampling Distance | sa_2026_generated_vb=0.5 |
| ConstructionOperations / PointsandGroups | Construct Point Group from Point Cloud | Show Progress? | sa_2026_generated_vb=false |
| ConstructionOperations / PointsandGroups | Construct Points Auto-Correspond 2 groups Inter-Point Distance | Auto-correspond same-point tolerance | sa_2026_generated_vb=0.1 |
| ConstructionOperations / PointsandGroups | Construct Points Auto-Correspond 2 groups Proximity | Auto-correspond same-point tolerance | sa_2026_generated_vb=0.25 |
| ConstructionOperations / PointsandGroups | Construct Points Cylindrically Shifted | Radial Shift | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Points Cylindrically Shifted | Theta Shift (degrees) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Points Cylindrically Shifted | Planar Shift | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Points From Surfaces On UV Grid | UV Point Group Base Name | sa_2026_generated_vb="UV Points" |
| ConstructionOperations / PointsandGroups | Construct Points From Surfaces On UV Grid | Make Each Line Separate Group? | sa_2026_generated_vb=false |
| ConstructionOperations / PointsandGroups | Construct Points From Surfaces On UV Grid | Number of U Grids | sa_2026_generated_vb=5 |
| ConstructionOperations / PointsandGroups | Construct Points From Surfaces On UV Grid | Number of V Grids | sa_2026_generated_vb=5 |
| ConstructionOperations / PointsandGroups | Construct Points From Surfaces On UV Grid | Edge Point Mode | sa_2026_generated_vb="Include Edges" |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | Point Prefix | sa_2026_generated_vb="p" |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | X Min | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | X Max | sa_2026_generated_vb=100.0 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | X Count | sa_2026_generated_vb=10 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | Y Min | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | Y Max | sa_2026_generated_vb=50.0 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | Y Count | sa_2026_generated_vb=10 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | Z Min | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | Z Max | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Construct Points Layout on Grid | Z Count | sa_2026_generated_vb=1 |
| ConstructionOperations / PointsandGroups | Construct Points N-Spaced on Curves | Number of Evenly Spaced Points | sa_2026_generated_vb=10 |
| ConstructionOperations / PointsandGroups | Construct Points Shifted in Working Frame | Shift Vector | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / PointsandGroups | Construct Points Spaced at a Distance on Curves | Distance Between Points | sa_2026_generated_vb=0.5 |
| ConstructionOperations / PointsandGroups | Construct Points Subset with Greatest Spacing | Subset Size | sa_2026_generated_vb=10 |
| ConstructionOperations / PointsandGroups | Construct Points WildCard Selection | Include prior complete name | sa_2026_generated_vb=false |
| ConstructionOperations / PointsandGroups | Create Hidden Point | Hidden Point Rod Index | sa_2026_generated_vb=0 |
| ConstructionOperations / PointsandGroups | Create Hidden Point | Overwrite existing point? | sa_2026_generated_vb=false |
| ConstructionOperations / PointsandGroups | Create Hidden Point Rod | A to B (Target to Target) Distance | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Create Hidden Point Rod | A to C (Target to Tip) Distance | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Create Hidden Point Rod | A to B Inter-point Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / PointsandGroups | Delete Hidden point Rod | Hidden Point Rod Index | sa_2026_generated_vb=0 |
| ConstructionOperations / PointsandGroups | Get Gradient At Projected Point On Surface | Generate output vector lines? | sa_2026_generated_vb=false |
| ConstructionOperations / PointsandGroups | Transform Points by Delta (About Working Frame) | Delta In Working Coordinates | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Spheres | Construct Sphere | Sphere Center (in working coordinates) | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / Spheres | Construct Sphere | Sphere Radius | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Surfaces | Construct Surface by offsetting a surface | Surface offset | sa_2026_generated_vb=0.0 |
| ConstructionOperations / Surfaces | Construct Surface by offsetting a surface | Hide original surface? | sa_2026_generated_vb=true |
| ConstructionOperations / Surfaces | Construct Surface From a Collection of Surfaces | Hide Original Surfaces? | sa_2026_generated_vb=true |
| ConstructionOperations / Surfaces | Construct Surface From a Collection of Surfaces | Delete Original Surfaces? | sa_2026_generated_vb=false |
| ConstructionOperations / Surfaces | Construct Surface From a Collection of Surfaces | Enable Sewing Tolerance? | sa_2026_generated_vb=false |
| ConstructionOperations / Surfaces | Construct Surface From a Collection of Surfaces | Sewing Tolerance | sa_2026_generated_vb=-1.0 |
| ConstructionOperations / VectorGroups | Construct a Vector Group - Group to Group Compare | RMS Deviation Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / VectorGroups | Construct a Vector Group - Group to Group Compare | Max Absolute Deviation Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / VectorGroups | Construct a Vector Group - Group to Group Compare | Average Deviation Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| ConstructionOperations / VectorGroups | Construct a Vector in Working Coordinates (Begin/Delta) | 'Begin' in Working Coordinates | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / VectorGroups | Construct a Vector in Working Coordinates (Begin/Delta) | 'Delta' in Working Coordinates | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / VectorGroups | Construct a Vector in Working Coordinates (Begin/Delta) | Is Magnitude Negative | sa_2026_generated_vb=false |
| ConstructionOperations / VectorGroups | Construct a Vector in Working Coordinates (Begin/Direction/Mag.) | 'Begin' in Working Coordinates | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / VectorGroups | Construct a Vector in Working Coordinates (Begin/Direction/Mag.) | 'Direction' in Working Coordinates | sa_2026_generated_vb=[0.0,0.0,0.0] |
| ConstructionOperations / VectorGroups | Construct a Vector in Working Coordinates (Begin/Direction/Mag.) | Signed Magnitude | sa_2026_generated_vb=0.0 |
| Events | Get i-th Event From Event Ref List | Event Index | sa_2026_generated_vb=0 |
| FileOperations / QDASFileExport | Get QDAS Catalog Entry Identifier | Font | sa_2026_generated_vb=["MS Shell Dlg",8,0,0,0] |
| GDT | Feature Inspection Auto Filter | Feature Check Name List | objectivesa_prior_release=null |
| GDT / GDTConstruct | Make a Feature Check Reference List - WildCard Selection | Collection Wildcard Criteria | sa_2026_generated_vb="*" |
| GDT / GDTConstruct | Make a Feature Check Reference List - WildCard Selection | Feature Check Wildcard Criteria | sa_2026_generated_vb="*" |
| GDT / GDTConstruct | Make GD&T Datum Annotation | Is Slot? | sa_2026_generated_vb=false |
| GDT / GDTConstruct | Make GD&T Datum Annotation | Force Surface Feature? | sa_2026_generated_vb=false |
| GDT | Set GD&T Extended Options | Use Extended Options | sa_2026_generated_vb=true |
| GDT | Set GD&T Extended Options | Circle Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Cone Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Cylinder Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Ellipse Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Line Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Open Slot Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Plane Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Slot Extended Options | sa_2026_generated_vb="Least Squares" |
| GDT | Set GD&T Extended Options | Sphere Extended Options | sa_2026_generated_vb="Least Squares" |
| InstrumentOperations / APILadar | Set LADAR Auto Meas Point | Sample Time MS (1-2000) | sa_2026_generated_vb=0 |
| InstrumentOperations / APILadar | Set LADAR Auto Meas Sphere | Sphere Radius | sa_2026_generated_vb=1.1875 |
| InstrumentOperations / APILadar | Set LADAR Auto Meas Sphere | Scan Line Spacing | sa_2026_generated_vb=0.05 |
| InstrumentOperations / APILadar | Set LADAR Auto Meas Sphere | Send Center Point? | sa_2026_generated_vb=true |
| InstrumentOperations / APILadar | Set LADAR Auto Meas Sphere | Send Sphere? | sa_2026_generated_vb=false |
| InstrumentOperations / APILadar | Set LADAR Auto Meas Sphere | Send Measured Cloud? | sa_2026_generated_vb=false |
| InstrumentOperations | Add Nominal Point to TCP Fixture | Nominal Point Location | sa_2026_generated_vb=[0.0,0.0,0.0] |
| InstrumentOperations | Add Nominal Point to TCP Fixture | Var XX | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Add Nominal Point to TCP Fixture | Var YY | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Add Nominal Point to TCP Fixture | Var ZZ | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Add Nominal Point to TCP Fixture | CoVar XY | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Add Nominal Point to TCP Fixture | CoVar XZ | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Add Nominal Point to TCP Fixture | CoVar YZ | sa_2026_generated_vb=0.0 |
| InstrumentOperations / AdvancedInstrumentOperations | Set Instrument Axes | Number of Steps | sa_2026_generated_vb=0 |
| InstrumentOperations | Auto-Correspond with Proximity Trigger | Point distance threshold | sa_2026_generated_vb=0.5 |
| InstrumentOperations | Auto-Correspond with Proximity Trigger | Vector axis threshold | sa_2026_generated_vb=0.25 |
| InstrumentOperations | Auto-Correspond with Proximity Trigger | Project results to nominal vector | sa_2026_generated_vb=false |
| InstrumentOperations | Auto-Correspond with Proximity Trigger | Warbler ramp start zone distance | sa_2026_generated_vb=12.0 |
| InstrumentOperations | Auto-Correspond with Proximity Trigger | Show Watch window on startup | sa_2026_generated_vb=false |
| InstrumentOperations | Auto-Correspond with Proximity Trigger | Make unmeasured group when done | sa_2026_generated_vb=false |
| InstrumentOperations | Auto-Correspond with Proximity Trigger | Measure each point only once | sa_2026_generated_vb=false |
| InstrumentOperations | Auto-Measure Batch of Features | Wait for Complete | sa_2026_generated_vb=true |
| InstrumentOperations | Auto Measure Points | Force use of existing group? | sa_2026_generated_vb=false |
| InstrumentOperations | Auto Measure Points | Show complete dialog? | sa_2026_generated_vb=false |
| InstrumentOperations | Auto Measure Points | Wait for Completion? | sa_2026_generated_vb=true |
| InstrumentOperations | Auto Measure Points | Auto Start? | sa_2026_generated_vb=false |
| InstrumentOperations | Auto-Measure Specified Geometry | Wait for Complete | sa_2026_generated_vb=false |
| InstrumentOperations | Auto-Measure Vectors | Project Point to Vector | sa_2026_generated_vb=false |
| InstrumentOperations | Auto-Measure Vectors | Angle Tolerance | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Auto-Measure Vectors | High Tolerance | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Auto-Measure Vectors | Low Tolerance | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Compute CTE Scale Factor | Material CTE (1/Deg F) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Compute CTE Scale Factor | Initial Temperature (F) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Compute CTE Scale Factor | Final Temperature (F) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Configure and Measure | Measure Immediately | sa_2026_generated_vb=false |
| InstrumentOperations | Configure and Measure | Wait for Completion | sa_2026_generated_vb=true |
| InstrumentOperations | Configure and Measure | Timeout in Seconds | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Delete Instrument | Prompt user to confirm? | sa_2026_generated_vb=true |
| InstrumentOperations | Delete Instrument | Keep resulting points? | sa_2026_generated_vb=true |
| InstrumentOperations | Delete Measurement Observation | Observation index | sa_2026_generated_vb=0 |
| InstrumentOperations | Delete Measurement Observation | Delete point if no measurements remain? | sa_2026_generated_vb=false |
| InstrumentOperations | Delete Measurements | Delete point if no measurements remain? | sa_2026_generated_vb=false |
| InstrumentOperations | Get Current Instrument Position Update | Reporting Frame | sa_2026_generated_vb="Instrument Base" |
| InstrumentOperations | Get Current Instrument Position Update | Polar Coordinates? | sa_2026_generated_vb=false |
| InstrumentOperations | Get Observation Info | Observation Index | sa_2026_generated_vb=0 |
| InstrumentOperations | Initiate Servo-Guide | Tolerance | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Jump Instrument to New Location | Hide the Previous Instrument? | sa_2026_generated_vb=false |
| InstrumentOperations | Locate Instrument (Group to Surface Quick Fit) | RMS Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Locate Instrument (Group to Surface Quick Fit) | Maximum Absolute Tolerance (0.0 for none) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Locate Instruments (USMN) | Move In Working Frame (TRUE) or Instrument Frame (FALSE) | sa_2026_generated_vb=false |
| InstrumentOperations | Locate Instruments (USMN) | AutoReject Outliers and Resolve | sa_2026_generated_vb=false |
| InstrumentOperations | Locate Instruments (USMN) | Max Acceptable RMS Error Value (0.0 for none) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Locate Instruments (USMN) | Max Acceptable Error Value (0.0 for none) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Locate Instruments (USMN) | Exclude Points Measured By Only One Instrument | sa_2026_generated_vb=false |
| InstrumentOperations | Move Measurement Observation | Observation index | sa_2026_generated_vb=0 |
| InstrumentOperations | Move Measurement Observation | Delete point if no measurements remain? | sa_2026_generated_vb=false |
| InstrumentOperations | Move Measurement Observation | Force observation to be active? | sa_2026_generated_vb=true |
| InstrumentOperations / NikonMetrologyLaserRadar / CloudViewerOperations | Set Filter | Filter Value | sa_2026_generated_vb=0 |
| InstrumentOperations / NikonMetrologyLaserRadar | LR APDIS Perform MCM Calibration | Use Matte Tooling Ball? | sa_2026_generated_vb=true |
| InstrumentOperations / NikonMetrologyLaserRadar | LR Hardware Connect | Port | sa_2026_generated_vb=0 |
| InstrumentOperations / NikonMetrologyLaserRadar | LR Self Test - LO Sep | Region (1=Region12,2=Region23,3=Region34) | sa_2026_generated_vb=0 |
| InstrumentOperations / NikonMetrologyLaserRadar | LR Self Test - LO Sep | Num Range Measurements | sa_2026_generated_vb=0 |
| InstrumentOperations / NikonMetrologyLaserRadar | LR Set Red Laser Intensity | Intensity (0-100) | sa_2026_generated_vb=0 |
| InstrumentOperations | Quick Align | Align to Individual Faces Only (not Entire Surface) | sa_2026_generated_vb=false |
| InstrumentOperations | Scan CAD Faces | Enable exclusions? | sa_2026_generated_vb=true |
| InstrumentOperations | Scan CAD Faces | Wait for Completion | sa_2026_generated_vb=true |
| InstrumentOperations | Scan within Perimeter | Wait for Completion | sa_2026_generated_vb=true |
| InstrumentOperations | Set (absolute) Instrument Scale Factor (CAUTION!) | Scale Factor | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Set Instrument Interface Response Timeout | Timeout (secs) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Set Instrument Transform | Number of Steps | sa_2026_generated_vb=0 |
| InstrumentOperations | Set Instrument Weather Setting | Temperature (F) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Set Instrument Weather Setting | Pressure (mmHg) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Set Instrument Weather Setting | Humidity (%Rel) | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Set Instrument Weather Setting | Set Automatically? (Ignore above values) | sa_2026_generated_vb=false |
| InstrumentOperations | Set Observation Status | Observation Index | sa_2026_generated_vb=0 |
| InstrumentOperations | Set Observation Status | Active? | sa_2026_generated_vb=false |
| InstrumentOperations | Set Probe Offset Frame Offline (Select Previously Measured Frame) | Face ID  | sa_2026_generated_vb=0 |
| InstrumentOperations | Set Target Computation Options | Target Computation Method | sa_2026_generated_vb="Use most recent shot from each face" |
| InstrumentOperations | Set Target Computation Options | Ignore Distance Measurements | sa_2026_generated_vb=false |
| InstrumentOperations | Set (multiply) Instrument Scale Factor (CAUTION!) | Scale Factor | sa_2026_generated_vb=0.0 |
| InstrumentOperations | Start Instrument Interface | Initialize at Startup | sa_2026_generated_vb=false |
| InstrumentOperations | Start Instrument Interface | Interface Type (0=default) | sa_2026_generated_vb=0 |
| InstrumentOperations | Start Instrument Interface | Run in Simulation | sa_2026_generated_vb=false |
| InstrumentOperations | Start Instrument Interface | Allow Start w/o Init Requirements | sa_2026_generated_vb=false |
| InstrumentOperations | Transform Instrument by Delta | Apply Scale from Transform to Instrument | sa_2026_generated_vb=false |
| InstrumentOperations | Transform Instrument - Frame To Frame | Number of Steps | sa_2026_generated_vb=0 |
| InstrumentOperations | Transform Multiple Instruments by Delta | Apply Scale from Transform to Instrument | sa_2026_generated_vb=false |
| RelationshipOperations | Auto Filter Clouds to Nominal Geometry 2D | Cloud Thinning Settings | sa_2026_generated_vb=["Nth Point",5,100,20000] |
| RelationshipOperations | Auto Filter Clouds to Nominal Geometry 2D | Geometry Extraction Tolerance | sa_2026_generated_vb=0.01 |
| RelationshipOperations | Auto Filter Clouds to Nominal Geometry 2D | Use Feature Specific Filter Settings? | sa_2026_generated_vb=false |
| RelationshipOperations | Auto Filter Clouds to Nominal Geometry 3D | Cloud Thinning Settings | sa_2026_generated_vb=["Nth Point",5,100,20000] |
| RelationshipOperations | Auto Filter Clouds to Nominal Geometry 3D | Use Feature Specific Filter Settings? | sa_2026_generated_vb=false |
| RelationshipOperations | Auto Filter Points/Groups/Clouds to Surface Faces | Cloud Thinning Settings | objectivesa_prior_release=null; sa_2026_generated_vb=["Nth Point",5,100,20000] |
| RelationshipOperations | Create Points to Objects Map | Proximity Tolerance | sa_2026_generated_vb=0.0 |
| RelationshipOperations | Edit Geometry Relationship Point List | Point Edit Mode | objectivesa_prior_release="Point List" |
| RelationshipOperations | Extract Geometry From Point Clouds | Geometry Type | sa_2026_generated_vb="Circle" |
| RelationshipOperations | Extract Geometry From Point Clouds | Tolerance | sa_2026_generated_vb=0.1 |
| RelationshipOperations | Extract Geometry From Point Clouds | Reverse Normal | sa_2026_generated_vb=false |
| RelationshipOperations | Extract Geometry From Point Clouds | Planar Point Count | sa_2026_generated_vb=1000 |
| RelationshipOperations | Filter Geometry Relationship Outlier Cloud Points | Sigma Threshold | sa_2026_generated_vb=3.0 |
| RelationshipOperations | Filter Geometry Relationship Outlier Cloud Points | Modify Existing Input Clouds | sa_2026_generated_vb=false |
| RelationshipOperations | Get i-th Relationship From Relationship Ref List | Relationship Index | sa_2026_generated_vb=0 |
| RelationshipOperations | Make Dynamic Circle Relationship | Construction Mode | sa_2026_generated_vb="Cylinder and Plane Intersection - Hold Plane Normal" |
| RelationshipOperations | Make Dynamic Ellipse Relationship | Construction Mode | sa_2026_generated_vb="Cylinder and Plane Intersection" |
| RelationshipOperations | Make Dynamic Line Relationship | Construction Mode | sa_2026_generated_vb="Intersection of Two Planes" |
| RelationshipOperations | Make Dynamic Plane Relationship | Construction Mode | sa_2026_generated_vb="Bisect Two Planes" |
| RelationshipOperations | Make Dynamic Point Relationship | Construction Mode | sa_2026_generated_vb="Intersection of Line and Plane" |
| RelationshipOperations | Make Frame to Frame Relationship | Orientation Tolerance | objectivesa_prior_release=null; sa_2026_generated_vb=[false,0.0,false,0.0] |
| RelationshipOperations | Make Frame to Frame Relationship | Position Tolerance | objectivesa_prior_release=null; sa_2026_generated_vb=[false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0] |
| RelationshipOperations | Make Group to Group Relationship | Tolerance | objectivesa_prior_release=null; sa_2026_generated_vb=[false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0] |
| RelationshipOperations | Make Group to Group Relationship | Constraint | objectivesa_prior_release=null; sa_2026_generated_vb=[true,0.0,true,0.0,true,0.0,false,0.0,true,0.0,true,0.0,true,0.0,false,0.0] |
| RelationshipOperations | Make Point Clouds to Objects Relationship | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| RelationshipOperations | Make Point to Point Relationship | Tolerance | objectivesa_prior_release=null; sa_2026_generated_vb=[false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0,false,0.0] |
| RelationshipOperations | Make Point to Point Relationship | Constraint | objectivesa_prior_release=null; sa_2026_generated_vb=[true,0.0,true,0.0,true,0.0,false,0.0,true,0.0,true,0.0,true,0.0,false,0.0] |
| RelationshipOperations | Make Points to Objects Relationship | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| ReportingOperations | Add Item to SA Report at Location | Page Number | sa_2026_generated_vb=0 |
| ReportingOperations | Add Item to SA Report at Location | Horizontal Location | sa_2026_generated_vb=1.0 |
| ReportingOperations | Add Item to SA Report at Location | Vertical Location | sa_2026_generated_vb=1.0 |
| ReportingOperations | Add Item to SA Report at Location | Show Report? | sa_2026_generated_vb=false |
| ReportingOperations | Append Items to SA Report | Show Report? | sa_2026_generated_vb=false |
| ReportingOperations | Append Items to SA Report | Begin On New Page? | sa_2026_generated_vb=false |
| ReportingOperations | Combine SA Reports | Show Report? | sa_2026_generated_vb=false |
| ReportingOperations | Create Chart from Vector Group | Show Interface? | sa_2026_generated_vb=false |
| ReportingOperations / CustomReportTables | Add Custom Table to SA Report | Show Report? | sa_2026_generated_vb=false |
| ReportingOperations / CustomReportTables | Get Custom Table Cell Double | Row | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Get Custom Table Cell Double | Column | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Get Custom Table Cell String | Row | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Get Custom Table Cell String | Column | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Make Custom Table | Decimal Precision | sa_2026_generated_vb=6 |
| ReportingOperations / CustomReportTables | Set Custom Table Cell Color | Row | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Set Custom Table Cell Color | Column | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Set Custom Table Cell Color | Foreground Color Name | sa_2026_generated_vb=[255,0,0] |
| ReportingOperations / CustomReportTables | Set Custom Table Cell Color | Background Color Name | sa_2026_generated_vb=[255,0,0] |
| ReportingOperations / CustomReportTables | Set Custom Table Cell Font | Row | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Set Custom Table Cell Font | Column | sa_2026_generated_vb=0 |
| ReportingOperations / CustomReportTables | Set Custom Table Cell Font | Font | sa_2026_generated_vb=["MS Shell Dlg",8,0,0,0] |
| ReportingOperations | Quick Report | Open Report? | sa_2026_generated_vb=true |
| ReportingOperations | Rename Picture | Overwrite if exists? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Add Charts to Report Bar | Clear Existing? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Add Custom Tables to Report Bar | Clear Existing? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Add Datums to Report Bar | Clear Existing? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Add Events to Report Bar | Clear Existing? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Add Feature Checks to Report Bar | Clear Existing? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Add Objects to Report Bar | Clear Existing? | sa_2026_generated_vb=true |
| ReportingOperations / ReportBar | Add Pictures to Report Bar | Clear Existing? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Add Relationships to Report Bar | Clear Existing? | sa_2026_generated_vb=false |
| ReportingOperations / ReportBar | Set Report Bar Visibility | Show Report Bar? | sa_2026_generated_vb=false |
| ReportingOperations | Set Point Group Report Options | Coordinate System | sa_2026_generated_vb="Cartesian" |
| ReportingOperations | Set Point Group Report Options | Show X Component | sa_2026_generated_vb=true |
| ReportingOperations | Set Point Group Report Options | Show Y Component | sa_2026_generated_vb=true |
| ReportingOperations | Set Point Group Report Options | Show Z Component | sa_2026_generated_vb=true |
| ReportingOperations | Set Point Group Report Options | Show Offsets | sa_2026_generated_vb=false |
| ReportingOperations | Set Point Group Report Options | Show Uncertainty | sa_2026_generated_vb=true |
| ReportingOperations | Set Point Group Report Options | Show Notes | sa_2026_generated_vb=false |
| ReportingOperations | Set Point Group Report Options | Show Measurements | sa_2026_generated_vb=false |
| ReportingOperations | Set Point Group Report Options | Show Measurement Details | sa_2026_generated_vb=false |
| ReportingOperations | Set Point Group Report Options | Show PointingError/Worst Angle | sa_2026_generated_vb=false |
| ReportingOperations | Set Point Group Report Options | Sort by Point Names | sa_2026_generated_vb=true |
| ReportingOperations | Set Point Group Report Options | Make Default | sa_2026_generated_vb=false |
| ReportingOperations | Set Point Group Report Options | Apply to All | sa_2026_generated_vb=false |
| ReportingOperations | Set Relationship Report Options | Report Options | sa_2026_generated_vb=["Cartesian","Single",true,true,true,true,true,true,true,false,true,true] |
| ReportingOperations | Set Report Tag Value From Double | Tag Value | sa_2026_generated_vb=0.0 |
| ReportingOperations | Set Report Tag Value From Integer | Tag Value | sa_2026_generated_vb=0 |
| ReportingOperations | Set Scale for Picture | Scale | sa_2026_generated_vb=100.0 |
| ReportingOperations | Set Vector Group Report Options | Report Options | sa_2026_generated_vb=["Cartesian","Single",true,true,true,true,true,true,true,false,true,true] |
| RobotOperations | Get Calibration Appliance Integer Value | Index Offset | sa_2026_generated_vb=0 |
| RobotOperations | Get Calibration Appliance Real Value | Index Offset | sa_2026_generated_vb=0 |
| RobotOperations | Move Robot/Machine through Path | Use SA Kinematics | sa_2026_generated_vb=true |
| RobotOperations | Move Robot/Machine through Path | Linear Segments | sa_2026_generated_vb=false |
| RobotOperations | Move Robot/Machine through Path | Acknowledge Arrival | sa_2026_generated_vb=true |
| RobotOperations | Move Robot/Machine to Frame | Use SA Kinematics | sa_2026_generated_vb=false |
| RobotOperations | Move Robot/Machine to Frame | Acknowledge Arrival | sa_2026_generated_vb=false |
| RobotOperations | Move Robot/Machine to Joint Pose (6DOF) | Joint 1 | sa_2026_generated_vb=0.0 |
| RobotOperations | Move Robot/Machine to Joint Pose (6DOF) | Joint 2 | sa_2026_generated_vb=0.0 |
| RobotOperations | Move Robot/Machine to Joint Pose (6DOF) | Joint 3 | sa_2026_generated_vb=0.0 |
| RobotOperations | Move Robot/Machine to Joint Pose (6DOF) | Joint 4 | sa_2026_generated_vb=0.0 |
| RobotOperations | Move Robot/Machine to Joint Pose (6DOF) | Joint 5 | sa_2026_generated_vb=0.0 |
| RobotOperations | Move Robot/Machine to Joint Pose (6DOF) | Joint 6 | sa_2026_generated_vb=0.0 |
| RobotOperations | Move Robot/Machine to Named Destination | Acknowledge Arrival | sa_2026_generated_vb=false |
| RobotOperations | Perform Robot Calibration (Alternate) | Set Current Base as Nominal? | sa_2026_generated_vb=false |
| RobotOperations | Perform Robot Calibration (Alternate) | Show Interface | sa_2026_generated_vb=false |
| RobotOperations | Perform Robot Calibration (Alternate) | Allowed Outlier Rejection Count | sa_2026_generated_vb=0 |
| RobotOperations | Perform Robot Calibration (Alternate) | Allowable Maximum Error | sa_2026_generated_vb=0.0 |
| RobotOperations | Perform Robot Calibration (Alternate) | Allowable Average Error | sa_2026_generated_vb=0.0 |
| RobotOperations | Set Calibration Appliance Integer Value | Index Offset | sa_2026_generated_vb=0 |
| RobotOperations | Set Calibration Appliance Integer Value | Integer Value | sa_2026_generated_vb=0 |
| RobotOperations | Set Robot/Machine Parameter | Parameter Value | sa_2026_generated_vb=0.0 |
| RobotOperations | Set Calibration Appliance Real Value | Index Offset | sa_2026_generated_vb=0 |
| RobotOperations | Set Calibration Appliance Real Value | Real Value | sa_2026_generated_vb=0.0 |
| RobotOperations | Start Robot/Machine Interface | Interface Type | sa_2026_generated_vb=0 |
| RobotOperations | Start Robot/Machine Interface | Run in Simulation | sa_2026_generated_vb=false |
| RobotOperations | Start/Stop Robot Calibration Trapping | Start Trapping (FALSE = Stop) | sa_2026_generated_vb=false |
| ScaleBars | Scale Bar Check | Current Temperature (F) | sa_2026_generated_vb=0.0 |
| ScaleBars | Scale Bar Check | Length of Bar at 68F | sa_2026_generated_vb=0.0 |
| ScaleBars | Scale Bar Check | Material CTE (PPM/F) | sa_2026_generated_vb=0.0 |
| ScaleBars | Scale Bar Check | Tolerance | sa_2026_generated_vb=0.0 |
| UtilityOperations / Folders | Get Folders by Wildcard | Case Sensitive Search | sa_2026_generated_vb=true |
| UtilityOperations / Folders | Set Folder Notes | Append? (FALSE = Overwrite) | sa_2026_generated_vb=true |
| UtilityOperations / Network | Set Wild Card Asterisk Mode | Auto Wrap Search String? | sa_2026_generated_vb=true |
| UtilityOperations / Notes | Set Collection Notes | Append? (FALSE = Overwrite) | sa_2026_generated_vb=true |
| UtilityOperations / Notes | Set Object Notes | Append? (FALSE = Overwrite) | sa_2026_generated_vb=true |
| UtilityOperations / Notes | Set Point Notes | Append? (FALSE = Overwrite) | sa_2026_generated_vb=true |
| UtilityOperations / Units | Lock Imported Items | Lock Items? | sa_2026_generated_vb=false |
| UtilityOperations / Units | Lock/Unlock Selected Items | Lock Items? | sa_2026_generated_vb=false |
| UtilityOperations / Units | Scale Objects | Scale Factor | sa_2026_generated_vb=0.0 |
| UtilityOperations / Units | Set Angular Representation | 0-360, (FALSE = +/-180) | sa_2026_generated_vb=false |
| UtilityOperations / Units | Set Auto Event Creation | Active? | sa_2026_generated_vb=false |
| UtilityOperations / Units | Set Automatic Backup State | Auto Job File Restore Points Active? | sa_2026_generated_vb=true |
| UtilityOperations / Units | Set Automatic Backup State | Auto Measurements Backup Active? | sa_2026_generated_vb=true |
| UtilityOperations / Units | Set Automatic Relationship Construction State | Active? | sa_2026_generated_vb=false |
| UtilityOperations / Units | Set Decimal Digits for Display | Length | sa_2026_generated_vb=0 |
| UtilityOperations / Units | Set Decimal Digits for Display | Angle | sa_2026_generated_vb=0 |
| UtilityOperations / Units | Set Decimal Digits for Display | Scale | sa_2026_generated_vb=0 |
| UtilityOperations / Units | Set Decimal Digits for Display | Unit Vector | sa_2026_generated_vb=0 |
| UtilityOperations / Units | Set Decimal Digits for Display | Weight | sa_2026_generated_vb=0 |
| UtilityOperations / Units | Set View Idle Update Frequency | Idle Count | sa_2026_generated_vb=0 |
| Vector Operations | Auto-Range and Set Vector Group Colorization (All) | Treat Individually? | sa_2026_generated_vb=false |
| Vector Operations | Auto-Range and Set Vector Group Colorization (All) | Colorization Options (Uses Mode Only) | sa_2026_generated_vb=["Continuous","Blue","Green","Red",false,true,false,100.0,1,false,0.1,false,false,true,false,0.5,-0.5,0.03,-0.03] |
| Vector Operations | Auto-Range and Set Vector Group Colorization (Selected) | Treat Individually? | sa_2026_generated_vb=false |
| Vector Operations | Auto-Range and Set Vector Group Colorization (Selected) | Colorization Options (Uses Mode Only) | sa_2026_generated_vb=["Continuous","Blue","Green","Red",false,true,false,100.0,1,false,0.1,false,false,true,false,0.5,-0.5,0.03,-0.03] |
| Vector Operations | Delete i-th Vector From Vector Group | Vector Index | sa_2026_generated_vb=0 |
| Vector Operations | Get i-th Vector From Vector Group | Vector Index | sa_2026_generated_vb=0 |
| Vector Operations | Set Vector Group Colorization Options (All) | Colorization Options | sa_2026_generated_vb=["Continuous","Blue","Green","Red",false,true,false,100.0,1,false,0.1,false,false,true,false,0.5,-0.5,0.03,-0.03] |
| Vector Operations | Set Vector Group Colorization Options (Selected) | Colorization Options | sa_2026_generated_vb=["Continuous","Blue","Green","Red",false,true,false,100.0,1,false,0.1,false,false,true,false,0.5,-0.5,0.03,-0.03] |
| ViewControl | Center Graphics About Object(s) | Object Type | sa_2026_generated_vb="Any" |
| ViewControl | Center Graphics About Object(s) | Collection Wildcard Criteria | sa_2026_generated_vb="*" |
| ViewControl | Center Graphics About Object(s) | Object Wildcard Criteria | sa_2026_generated_vb="*" |
| ViewControl / Colors | Set Object(s) Color | New Working Color Name | sa_2026_generated_vb=[255,0,0] |
| ViewControl / Colors | Set Object(s) Color | Auto Increment | sa_2026_generated_vb=false |
| ViewControl / Colors | Set Working Color | New Working Color Name | sa_2026_generated_vb=[255,0,0] |
| ViewControl / Colors | Set Working Color Auto Increment | Auto Increment | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Annotations for Datums | Show? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Annotations for Datums | Highlight? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Annotations for Datums | Set Inspection View? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Annotations for Feature Checks | Show? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Annotations for Feature Checks | Highlight? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Annotations for Feature Checks | Set Inspection View? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Callout View | Show Callout View? | sa_2026_generated_vb=true |
| ViewControl / HideShowOperations | Show/Hide Instrument Probe Tip | Show Instrument Probe Tip? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Instruments | Show Instruments? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Points | Show? (Hide = FALSE) | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide Relationship Report | Show Relationship Report | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide by Object Type | All Collections? | sa_2026_generated_vb=false |
| ViewControl / HideShowOperations | Show/Hide by Object Type | Object Type To Show / Hide | sa_2026_generated_vb="Any" |
| ViewControl / HideShowOperations | Show/Hide by Object Type | Hide? (Show = FALSE) | sa_2026_generated_vb=true |
| ViewControl / HideShowOperations | Show Items in Tree | Collapse all other Items? | sa_2026_generated_vb=true |
| ViewControl / HideShowOperations | Show by Object Type | All Collections? | sa_2026_generated_vb=false |
| ViewControl / HighlightOperations | Highlight Objects | HighLight Objects? | sa_2026_generated_vb=false |
| ViewControl / HighlightOperations | Highlight Point | Show Point? | sa_2026_generated_vb=false |
| ViewControl / PointofView | Define Point of View | Rotation (x) | sa_2026_generated_vb=0.0 |
| ViewControl / PointofView | Define Point of View | Rotation (y) | sa_2026_generated_vb=0.0 |
| ViewControl / PointofView | Define Point of View | Rotation (z) | sa_2026_generated_vb=0.0 |
| ViewControl / PointofView | Define Point of View | Restore Zoom Settings? | sa_2026_generated_vb=false |
| ViewControl / PointofView | Define Point of View | Scale Factor | sa_2026_generated_vb=1.0 |
| ViewControl / PointofView | Define Point of View | Origin (x) | sa_2026_generated_vb=0.0 |
| ViewControl / PointofView | Define Point of View | Origin (y) | sa_2026_generated_vb=0.0 |
| ViewControl / PointofView | Define Point of View | Restore Render Mode? | sa_2026_generated_vb=false |
| ViewControl / PointofView | Define Point of View | Rendering Mode | sa_2026_generated_vb="Wireframe" |
| ViewControl / PointofView | Save Point of View | Restore Zoom Settings? | sa_2026_generated_vb=true |
| ViewControl | Set Object(s) Translucency | Opacity Value | sa_2026_generated_vb=0.0 |
| ViewControl | Set Target Labels Use Full Names | Use Full Names? | sa_2026_generated_vb=false |
| ViewControl | Set View Clipping Plane | Remove Clipping Plane? | sa_2026_generated_vb=false |
| InstrumentOperations | Construct Mirror from Two Points | Send Mirror to Instrument? | sa_2026_generated_vb=true |
| InstrumentOperations | Set Inspection Verification Mode | Enable Verification? | sa_2026_generated_vb=false |
| InstrumentOperations | Set WRTL Channel | Channel | sa_2026_generated_vb=0 |
| InstrumentOperations | Enable/Disable Frame Set Scan Mode (By Instrument) | Enable Frame Set Scan Mode | sa_2026_generated_vb=true |
| InstrumentOperations | Multi Measurement Initiate | Wait for Completion | sa_2026_generated_vb=false |
| InstrumentOperations | Set XYZ Instrument Uncertainties | X Uncertainty | sa_2026_generated_vb=0.0005 |
| InstrumentOperations | Set XYZ Instrument Uncertainties | Y Uncertainty | sa_2026_generated_vb=0.0005 |
| InstrumentOperations | Set XYZ Instrument Uncertainties | Z Uncertainty) | sa_2026_generated_vb=0.0005 |
| InstrumentOperations / APILADAR | Set LADAR FeatureMeas Sphere | Scan Line Spacing | sa_2026_generated_vb=0.05 |
| InstrumentOperations / APILADAR | Set LADAR FeatureMeas Circle | Scan Line Spacing | sa_2026_generated_vb=0.05 |
| InstrumentOperations / APILADAR | Set LADAR FeatureMeas Circle | Width of Extra Area Around Scan | sa_2026_generated_vb=0.0 |
| InstrumentOperations / APILADAR | Set LADAR FeatureMeas Slot | Scan Line Spacing | sa_2026_generated_vb=0.05 |
| InstrumentOperations / APILADAR | Set LADAR FeatureMeas Slot | Width of Extra Area Around Scan | sa_2026_generated_vb=0.0 |
| InstrumentOperations / APILADAR | Set LADAR FeatureMeas Cylinder | Scan Line Spacing | sa_2026_generated_vb=0.05 |
| InstrumentOperations / APILADAR | Set LADAR FeatureMeas Cylinder | Width of Extra Area Around Scan | sa_2026_generated_vb=0.0 |
| RelationshipOperations | Make Groups to Objects Relationship | Projection Options | sa_2026_generated_vb=["Object To Probe Vectors",false,false,0.0,false,0.0] |
| RelationshipOperations | Make Cloud to Swatch Relationship | Maximum Radial Offset | sa_2026_generated_vb=0.125 |
| RelationshipOperations | Make Cloud to Swatch Relationship | Minimum Axial Offset | sa_2026_generated_vb=-0.125 |
| RelationshipOperations | Make Cloud to Swatch Relationship | Maximum Axial Offset | sa_2026_generated_vb=0.125 |
| RelationshipOperations / RelationshipAttributes | Get Geom Relationship Criteria Name List | Include All Criteria? | sa_2026_generated_vb=false |
| RelationshipOperations / RelationshipAttributesScalarTypes | Set Object to Object Direction Relationship Tolerances | Angle Between Vectors Tolerances | sa_2026_generated_vb=[false,0.0,false,0.0] |
| RelationshipOperations / RelationshipAttributesScalarTypes | Set Object to Object Direction Relationship Tolerances | Mutual Perpendicular Length Tolerances | sa_2026_generated_vb=[false,0.0,false,0.0] |
| RobotCalibrationApplianceNodeOperations | Set Calibration Appliance Node Calibration Appliance IP Address | Calibration Appliance IP Address | sa_2026_generated_vb="0.0.0.0" |
| RobotCalibrationApplianceNodeOperations | Set Calibration Appliance Node Trapping Node ID | Trapping Node ID | sa_2026_generated_vb=0 |
| RobotCalibrationApplianceNodeOperations | Enable/Disable Calibration Appliance Node Trap Manager | Enable(TRUE), Disable(FALSE)? | sa_2026_generated_vb=true |
| RobotCalibrationApplianceNodeOperations | Set Calibration Appliance Node Integer Value | Index Offset | sa_2026_generated_vb=0 |
| RobotCalibrationApplianceNodeOperations | Set Calibration Appliance Node Integer Value | Integer Value | sa_2026_generated_vb=0 |
| RobotCalibrationApplianceNodeOperations | Get Calibration Appliance Node Integer Value | Index Offset | sa_2026_generated_vb=0 |
| RobotCalibrationApplianceNodeOperations | Set Calibration Appliance Node Real Value | Index Offset | sa_2026_generated_vb=0 |
| RobotCalibrationApplianceNodeOperations | Set Calibration Appliance Node Real Value | Real Value | sa_2026_generated_vb=0.0 |
| RobotCalibrationApplianceNodeOperations | Get Calibration Appliance Node Real Value | Index Offset | sa_2026_generated_vb=0 |
| RobotCalibrationApplianceNodeOperations | Update Calibration Appliance Node Display Robot Joints | Enable Display Robot Joint Updates? | sa_2026_generated_vb=true |
| RobotCalibrationApplianceNodeOperations | Connect/Disconnect Calibration Appliance Node | Connect(TRUE) or Disconnect(FALSE)? | sa_2026_generated_vb=true |
| RobotCalibrationApplianceNodeOperations | Enable/Disable Calibration Appliance Node Instrument Auto Point | Enable Instrument Auto Point? | sa_2026_generated_vb=true |
| RobotCalibrationApplianceNodeOperations | Set Calibration Appliance Node Instrument Dwell Time | Measurement Dwell Time (Seconds) | sa_2026_generated_vb=0.0 |
| RobotOperations | Set Robot/Machine Base Transform | Number of Steps | sa_2026_generated_vb=0 |
| ViewControl / HideShowOperations | Show / Hide Dimension | Show Dimension? | sa_2026_generated_vb=true |

## Reviewed intentional exclusions

| Category path | MP step | Inventory key | Reason codes | Rationale | Decision |
| --- | --- | --- | --- | --- | --- |
| AccumulatorMathOperations | Accumulator Add | documentation:AccumulatorMathOperations/AccumulatorAdd.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AccumulatorMathOperations | Accumulator Change Sign | documentation:AccumulatorMathOperations/AccumulatorChangeSign.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AccumulatorMathOperations | Accumulator Clear | documentation:AccumulatorMathOperations/AccumulatorClear.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AccumulatorMathOperations | Accumulator Divide | documentation:AccumulatorMathOperations/AccumulatorDivide.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AccumulatorMathOperations | Accumulator Invert | documentation:AccumulatorMathOperations/AccumulatorInvert.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AccumulatorMathOperations | Accumulator Multiply | documentation:AccumulatorMathOperations/AccumulatorMultiply.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AccumulatorMathOperations | Accumulator Power | documentation:AccumulatorMathOperations/AccumulatorPower.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AccumulatorMathOperations | Accumulator Subtract | documentation:AccumulatorMathOperations/AccumulatorSubtract.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Add a Picture to Picture Name Ref List | documentation:AnalysisOperations/AddAPictureToPictureName.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Add a Report to Report Ref List | documentation:AnalysisOperations/AddAReportToReportRef.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Append to String Ref List | documentation:AnalysisOperations/AppendToStringRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Double List Max/Min | documentation:AnalysisOperations/GetDoubleListMaxMin.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Number of Instruments in Collection Instrument Ref List | documentation:AnalysisOperations/GetNumberOfInstruments.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Number of Objects in Collection Object Name Ref List | documentation:AnalysisOperations/GetNumberOfObjectsInCollection.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Number of Pictures in Picture Name Ref List | documentation:AnalysisOperations/GetNumberOfPicturesInPicture.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Number of Point Names in Point Name Ref List | documentation:AnalysisOperations/GetNumberOfPointNames.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Number of Reports in Report Ref List | documentation:AnalysisOperations/GetNumbeOfReportsInReport.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Number of Strings in String Ref List | documentation:AnalysisOperations/GetNumberOfStringsInString.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get Number of characters in a string | documentation:AnalysisOperations/GetNumberOfCharactersInAString.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Instrument From Collection Instrument Ref List | documentation:AnalysisOperations/GetI-thInstrumentFromCollection.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Object From Collection Object Name Ref List | documentation:AnalysisOperations/GetI-thObjectFromCollection.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Object From Collection Object Name Ref List (Iterator) | documentation:AnalysisOperations/GetI-thObjectFromCollectionIterator.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Picture From Picture Name Ref List | documentation:AnalysisOperations/GetI-thPictureFromPictureName.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Point Name From Point Name Ref List | documentation:AnalysisOperations/GetI-thPointNameFromPointNameRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Point Name From Point Name Ref List (Iterator) | documentation:AnalysisOperations/GetI-thPointNameFromPointNameIterator.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Report From Report Ref List | sdk:AnalysisOperations.txt#48 | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Report Item From Report Items Ref List | documentation:AnalysisOperations/GetI-thReportItemFromReportItem.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th Report Ref List | documentation:AnalysisOperations/GetI-thReportRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th String From String Ref List | documentation:AnalysisOperations/GetI-thStringFromStringRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Get i-th String From String Ref List (Iterator) | documentation:AnalysisOperations/GetI-thStringFromStringIterator.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Remove i-th Object From Collection Object Name Ref List | documentation:AnalysisOperations/RemoveI-thObjectFromCollection.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Remove i-th Point Name From Point Name Ref List | documentation:AnalysisOperations/RemoveI-thPointNameFromPoint.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations | Remove i-th String from String Ref List | documentation:AnalysisOperations/RemoveI-thStringFromString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Circle Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeCircleFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Cone Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeConeFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Cylinder Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeCylinderFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Ellipse Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeEllipseFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Line Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeLineFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Paraboloid Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeParaboloidFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Plane Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakePlaneFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Slot Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeSlotFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / GeometryFitProfiles | Make Sphere Fit Profile | documentation:AnalysisOperations/GeometryFitProfiles/MakeSphereFitProfile.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / RelationshipAttributes | Make Vector Fit Constraint | documentation:AnalysisOperations/RelationshipAttributes/MakeVectorFitConstraint.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / RelationshipAttributes | Make Vector Tolerance | documentation:AnalysisOperations/RelationshipAttributes/MakeVectorTolerance.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / RelationshipAttributesScalarTypes | Make Relationship Sigmoidal Gap Fit Constraints | documentation:AnalysisOperations/RelationshipAttributesScalarTypes/MakeRelSigmoidConstraints.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / RelationshipAttributesScalarTypes | Make Scalar Fit Constraint | documentation:AnalysisOperations/RelationshipAttributesScalarTypes/MakeScalarFitConstraint.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / RelationshipAttributesScalarTypes | Make Scalar Tolerance | documentation:AnalysisOperations/RelationshipAttributesScalarTypes/MakeScalarTolerance.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| AnalysisOperations / RelationshipAttributesScalarTypes | Make Symmetric Scalar Tolerance | documentation:AnalysisOperations/RelationshipAttributesScalarTypes/MakeSymmetricScalarTolerance.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations | Construct Objects From Surface Faces - Runtime Select | documentation:ConstructionOperations/ConstructObjectsFromSurface.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Callouts | Add a Callout View to Callout View Ref List | documentation:ConstructionOperations/Callouts/AddACalloutViewToCallout.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Callouts | Get Number of Callout Views in Callout View Ref List | documentation:ConstructionOperations/Callouts/GetNumberOfCalloutViews.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Callouts | Get i-th Callout View From Callout View Ref List | documentation:ConstructionOperations/Callouts/GetI-thCalloutViewFrom.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Callouts | Make a Callout View Ref List | documentation:ConstructionOperations/Callouts/MakeACalloutViewRefList.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Callouts | Sort Callout View Ref List | documentation:ConstructionOperations/Callouts/SortCalloutViewRefList.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Circles | Construct Circles From Surface Faces-Runtime Select | documentation:ConstructionOperations/Circles/ConstructCirclesFromSurfaceFaces-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Cones | Construct Cones From Surface Faces-Runtime Select | documentation:ConstructionOperations/Cones/ConstructConesFromSurfaceFaces-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Cylinders | Construct Cylinders From Surface Faces-Runtime Select | documentation:ConstructionOperations/Cylinders/ConstructCylindersFromSurfaceFaces-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Frames | Construct Frame with Wizard | documentation:ConstructionOperations/Frames/ConstructFrameWithWizard.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Lines | Construct Lines From Surface Faces-Runtime Select | documentation:ConstructionOperations/Lines/ConstructLinesFromSurfaceFaces-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Add Double to Double List | documentation:ConstructionOperations/OtherMPTypes/AddDoubleToDoubleList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Add a Collection Instrument to a Ref List | documentation:ConstructionOperations/OtherMPTypes/AddACollectionInstrument.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Add a Collection Object Name to a Ref List | documentation:ConstructionOperations/OtherMPTypes/AddACollectionObjectName.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Append Two Point Name Ref Lists | documentation:ConstructionOperations/OtherMPTypes/AppendTwoPointNameRefLists.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Append two Collection Object Name Ref Lists | documentation:ConstructionOperations/OtherMPTypes/AppendtwoCollectionObject.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Append two Relationship Ref Lists | documentation:ConstructionOperations/OtherMPTypes/AppendTwoRelationshipRef.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Concatenate Strings | documentation:ConstructionOperations/OtherMPTypes/ConcatenateStrings.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Convert to Euler Angles from Fixed Angles | documentation:ConstructionOperations/OtherMPTypes/ConvertToEulerAnglesFromFixedAngles.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Doubles (Euler XYZ) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoDoublesEulerXYZ.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Doubles (Euler ZXZ) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoDoublesEulerZXZ.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Doubles (Euler ZYX) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoDoublesEulerZYX.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Doubles (Euler ZYZ) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoDoublesEulerZYZ.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Doubles (Fixed XYZ) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoDoublesFixed.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Doubles (Matrix Elements) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoDoublesMatrix.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Vectors (Fixed XYZ) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoVectorsFixed.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Transform into Vectors (Origin and Axes) | documentation:ConstructionOperations/OtherMPTypes/DecomposeTransformIntoVectorsOrigin.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose Vector into Doubles | documentation:ConstructionOperations/OtherMPTypes/DecomposeVectorIntoDoubles.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose World Transform Operator into Doubles (Fixed XYZ in World) | documentation:ConstructionOperations/OtherMPTypes/DecomposeWorldTransformOperatorDoubles.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Decompose World Transform Operator into Vectors (Fixed XYZ in World) | documentation:ConstructionOperations/OtherMPTypes/DecomposeWorldTransformOperatorVectors.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Get Collection Instrument Ref List Variable | sdk:ConstructionOperations_OtherMPTypes.txt#37 | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Get Collection Instrument Reference List Variable | documentation:ConstructionOperations/OtherMPTypes/GetCollectionInstrument.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Get Collection Name and Index from Collection Instrument ID | documentation:ConstructionOperations/OtherMPTypes/GetCollectionNameAndIndex.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Invert Transform | documentation:ConstructionOperations/OtherMPTypes/InvertTransform.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make Axis Identifier from String | documentation:ConstructionOperations/OtherMPTypes/MakeAxisIdentifierFromString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make Projection Options | documentation:ConstructionOperations/OtherMPTypes/MakeProjectionOptions.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make String from Decimal Degrees Angular Value | documentation:ConstructionOperations/OtherMPTypes/MakeStringFromDecimalDegrees.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make String from Double | documentation:ConstructionOperations/OtherMPTypes/MakeStringFromDouble.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make String from Integer | documentation:ConstructionOperations/OtherMPTypes/MakeStringFromInteger.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make Strings from a Collection Object Name | documentation:ConstructionOperations/OtherMPTypes/MakeStringsFromACollection.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make Strings from a Point Name | documentation:ConstructionOperations/OtherMPTypes/MakeStringsFromAPoint.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make UDP Settings | documentation:ConstructionOperations/OtherMPTypes/MakeUDPSettings.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Boolean | documentation:ConstructionOperations/OtherMPTypes/MakeABoolean.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Boolean From an Integer | documentation:ConstructionOperations/OtherMPTypes/MakeABooleanFromAnInteger.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Instrument ID - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionInstrumentIDRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Instrument ID from a Collection and an Integer | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionInstrumentID.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Instrument Reference List | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionInstrument.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Instrument Reference List - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionInstrumentReferenceListRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Item Name from Strings | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionItemName.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Machine ID from a Collection and an Integer | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionMachineID.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Name - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionNameRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionObjectName.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Ref List | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionObjectNameRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name Reference List - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionObjectNameReferenceListRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Object Name from Strings | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionObjectNameFromStrings.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Collection Vector Group Name Ref List - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeACollectionVectorGroup.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Double | documentation:ConstructionOperations/OtherMPTypes/MakeADouble.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Double From an Integer | documentation:ConstructionOperations/OtherMPTypes/MakeADoubleFromAnInteger.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Double List | documentation:ConstructionOperations/OtherMPTypes/MakeADoubleList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Double from String | documentation:ConstructionOperations/OtherMPTypes/MakeADoubleFromString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Integer | documentation:ConstructionOperations/OtherMPTypes/MakeAInteger.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Integer From String | documentation:ConstructionOperations/OtherMPTypes/MakeAIntegerFromString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Normalized Vector | documentation:ConstructionOperations/OtherMPTypes/MakeANormalizedVector.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Picture Name Ref List | documentation:ConstructionOperations/OtherMPTypes/MakeAPictureNameRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Picture Name Ref List - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeAPictureNameRefListRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Point Name - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeAPointNameRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Point Name Ref List | documentation:ConstructionOperations/OtherMPTypes/MakeAPointNameRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Point Name Ref List - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeAPointNameRefListRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Point Name from Strings | documentation:ConstructionOperations/OtherMPTypes/MakeAPointNameFromStrings.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Relationship Reference List- Runtime Select | sdk:ConstructionOperations_OtherMPTypes.txt#43 | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Relationship Reference ListRuntime Selection | documentation:ConstructionOperations/OtherMPTypes/MakeARelationshipReferenceListRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Report Items Ref List | documentation:ConstructionOperations/OtherMPTypes/MakeAReportItemsRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Report Ref List - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeAReportRefListRuntime.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a String | documentation:ConstructionOperations/OtherMPTypes/MakeAString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a String From A String Ref List | documentation:ConstructionOperations/OtherMPTypes/MakeAStringFromAString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a String Ref List | documentation:ConstructionOperations/OtherMPTypes/MakeAStringRefList.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a System String | documentation:ConstructionOperations/OtherMPTypes/MakeASystemString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Transform from Doubles (Euler Parameters) | sdk:ConstructionOperations_OtherMPTypes.txt#55 | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Transform from Doubles (Fixed XYZ) | documentation:ConstructionOperations/OtherMPTypes/MakeATransformFromDoublesFixed.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Transform from Doubles (Matrix Elements) | documentation:ConstructionOperations/OtherMPTypes/MakeATransformFromDoublesMatrix.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a Vector Name Ref List - Runtime Select | documentation:ConstructionOperations/OtherMPTypes/MakeWorldTransformOperator.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make a World Transform Operator (from Transform and Scale) | documentation:ConstructionOperations/OtherMPTypes/MakeAWorldTransformOperator.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Make an Incremented String | documentation:ConstructionOperations/OtherMPTypes/MakeAnIncrementedString.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Set Collection Instrument Ref List Variable | sdk:ConstructionOperations_OtherMPTypes.txt#38 | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Set Collection Instrument Reference List Variable | documentation:ConstructionOperations/OtherMPTypes/SetCollectionInstrument.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Split String into Two Strings | documentation:ConstructionOperations/OtherMPTypes/SplitStringintoTwoStrings.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / OtherMPTypes | Subtract Two Point Name Ref Lists | documentation:ConstructionOperations/OtherMPTypes/SubtractTwoPointNameRef.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Planes | Construct Planes From Surface Faces-Runtime Select | documentation:ConstructionOperations/Planes/ConstructPlanesFromSurfaceFaces-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / PointClouds | Construct Point Clouds from Existing Cloud Points - Runtime Select | documentation:ConstructionOperations/PointClouds/ConstructPointCloudsfromExistingCloudPoints.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / PointsandGroups | Construct Point from Cloud Point - Runtime Select | documentation:ConstructionOperations/PointsandGroups/ConstructPointfromCloudPoint-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / PointsandGroups | Construct Points From Surface Faces - Runtime Select | documentation:ConstructionOperations/PointsandGroups/ConstructPointsFromSurfaceFaces.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / PointsandGroups | Construct Points on Surface(s) by Clicking | documentation:ConstructionOperations/PointsandGroups/ConstructPointsonSurfacesbyClicking.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / Spheres | Construct Spheres From Surface Faces-Runtime Select | documentation:ConstructionOperations/Spheres/ConstructSpheresFromSurfaceFaces-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ConstructionOperations / VectorGroups | Make a Vector from Doubles | documentation:ConstructionOperations/VectorGroups/MakeAVectorFromDoubles.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| Dimensions | Add a Dimension to Dimension Ref List | documentation:Dimensions/AddADimensionToDimension.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| Dimensions | Get Number of Dimensions in Dimension Ref List | documentation:Dimensions/GetNumberOfDimensionsIn.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| Dimensions | Get i-th Dimension From Dimension Ref List | documentation:Dimensions/GetI-thDimensionFromDimension.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| Dimensions | Get i-th Dimension From Dimension Ref List (Iterator) | documentation:Dimensions/GetI-thDimensionFromDimensionIterator.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Close | documentation:ExcelDirectConnect/Close.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Close All Workbooks | documentation:ExcelDirectConnect/CloseAllWorkbooks.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Get Workbook Address | documentation:ExcelDirectConnect/GetWorkbookAddress.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Open Workbook File | documentation:ExcelDirectConnect/OpenWorkbookFile.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Run Macro | documentation:ExcelDirectConnect/RunMacro.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Save | documentation:ExcelDirectConnect/Save.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Set Clear After Insert | documentation:ExcelDirectConnect/SetClearAfterInsert.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect | Set Workbook Address | documentation:ExcelDirectConnect/SetWorkbookAddress.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Read | Read Double | documentation:ExcelDirectConnect/Read/ReadDouble.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Read | Read Integer | documentation:ExcelDirectConnect/Read/ReadInteger.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Read | Read String | documentation:ExcelDirectConnect/Read/ReadString.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Read | Read Variables | documentation:ExcelDirectConnect/Read/ReadVariables.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Write | Write Double | documentation:ExcelDirectConnect/Write/WriteDouble.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Write | Write Integer | documentation:ExcelDirectConnect/Write/WriteInteger.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Write | Write Picture | documentation:ExcelDirectConnect/Write/WritePicture.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Write | Write String | documentation:ExcelDirectConnect/Write/WriteString.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ExcelDirectConnect / Write | Write Variables | documentation:ExcelDirectConnect/Write/WriteVariables.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations | Browse For Directory | documentation:FileOperations/BrowseForDirectory.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Browse For File | documentation:FileOperations/BrowseForFile.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Copy Directory | documentation:FileOperations/CopyDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Copy General File | documentation:FileOperations/CopyGeneralFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Delete Directory | documentation:FileOperations/DeleteDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Delete General File | documentation:FileOperations/DeleteGeneralFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Directory Existence | documentation:FileOperations/DirectoryExistence.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Exit Measurement Plan | documentation:FileOperations/ExitMeasurementPlan.htm | client_owned_state_and_control_flow | Programming-language control flow and MP runtime sequencing belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Find Files in Directory | documentation:FileOperations/FindFilesInDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Find Sub-Directories in Directory | documentation:FileOperations/FindSubDirectoriesInDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Get Directory and Filename from Path | documentation:FileOperations/GetDirectoryAndFilename.htm | client_owned_value_computation | This deterministic value or reference-list computation belongs in the client library and does not require SpatialAnalyzer automation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Make Directory | documentation:FileOperations/MakeDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Pop PolyBay Analysis Window | sdk:FileOperations.txt#34 | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Rename General File | documentation:FileOperations/RenameGeneralFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Run Another Program | documentation:FileOperations/RunAnotherProgram.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Run Powershell Script | documentation:FileOperations/RunPowershellScript.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Shut Down SA | documentation:FileOperations/ShutDownSA.htm | server_lifecycle_boundary | Briosa may observe SpatialAnalyzer availability but does not expose a public command that shuts down the separately installed application. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Terminate All Running MPs | sdk:FileOperations.txt#19 | client_owned_state_and_control_flow | Programming-language control flow and MP runtime sequencing belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Verify General File Exists | documentation:FileOperations/VerifyGeneralFileExists.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations | Verify MP File Exists | documentation:FileOperations/VerifyMPFileExists.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / ASIIDataFileOperations | Clear All ASCII Files | documentation:FileOperations/ASIIDataFileOperations/ClearAllASCIIFiles.htm | client_owned_serialization | Raw file parsing and serialization belong in the client application rather than the SpatialAnalyzer gRPC command surface. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / ASIIDataFileOperations | Close ASCII File | documentation:FileOperations/ASIIDataFileOperations/CloseASCIIFile.htm | client_owned_serialization | Raw file parsing and serialization belong in the client application rather than the SpatialAnalyzer gRPC command surface. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / ASIIDataFileOperations | Open ASCII File | documentation:FileOperations/ASIIDataFileOperations/OpenASCIIFile.htm | client_owned_serialization | Raw file parsing and serialization belong in the client application rather than the SpatialAnalyzer gRPC command surface. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / ASIIDataFileOperations | Read ASCII Line (Iterator) | documentation:FileOperations/ASIIDataFileOperations/ReadASCIILineIterator.htm | client_owned_serialization | Raw file parsing and serialization belong in the client application rather than the SpatialAnalyzer gRPC command surface. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / ASIIDataFileOperations | Write ASCII Line | documentation:FileOperations/ASIIDataFileOperations/WriteASCIILine.htm | client_owned_serialization | Raw file parsing and serialization belong in the client application rather than the SpatialAnalyzer gRPC command surface. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatabaseOperations | Delete from ODBC Database | documentation:FileOperations/DatabaseOperations/DeletefromODBCDatabase.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatabaseOperations | Get from ODBC Database | documentation:FileOperations/DatabaseOperations/GetfromODBCDatabase.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatabaseOperations | Put to ODBC Database | documentation:FileOperations/DatabaseOperations/PuttoODBCDatabase.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Get Boolean From DataShare File | documentation:FileOperations/DatashareOperations/GetBooleanFromDataShare.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Get Double From DataShare File | documentation:FileOperations/DatashareOperations/GetDoubleFromDataShare.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Get Integer From DataShare File | documentation:FileOperations/DatashareOperations/GetIntegerFromDataShare.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Get String From DataShare File | documentation:FileOperations/DatashareOperations/GetStringFromDataShare.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Get Transform From DataShare File | documentation:FileOperations/DatashareOperations/GetTransformFromDataShare.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Get Vector From DataShare File | documentation:FileOperations/DatashareOperations/GetVectorFromDataShare.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Load DataShare File | documentation:FileOperations/DatashareOperations/LoadDataShareFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Load HTML Form | documentation:FileOperations/DatashareOperations/LoadHTMLForm.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Load HTML Form in Edge Browser | documentation:FileOperations/DatashareOperations/LoadHTMLForminEdgeBrowser.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Save DataShare File | documentation:FileOperations/DatashareOperations/SaveDataShareFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Set Boolean In DataShare File | documentation:FileOperations/DatashareOperations/SetBooleanInDataShareFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Set Double In DataShare File | documentation:FileOperations/DatashareOperations/SetDoubleInDataShareFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Set Integer In DataShare File | documentation:FileOperations/DatashareOperations/SetIntegerInDataShareFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Set String In DataShare File | documentation:FileOperations/DatashareOperations/SetStringInDataShareFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Set Transform In DataShare File | documentation:FileOperations/DatashareOperations/SetTransformInDataShare.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / DatashareOperations | Set Vector In DataShare File | documentation:FileOperations/DatashareOperations/SetVectorInDataShareFile.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| FileOperations / FileExport | Export Vector Container to Excel File | documentation:FileOperations/FileExport/ExportVectorContainerToExcelFile.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Close JSON File | documentation:FileOperations/JSON/CloseJSONFile.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Get JSON Array Size | documentation:FileOperations/JSON/GetJSONArraySize.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Get JSON Double Value | documentation:FileOperations/JSON/GetJSONDoubleValue.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Get JSON Integer Value | documentation:FileOperations/JSON/GetJSONIntegerValue.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Get JSON Object Value | documentation:FileOperations/JSON/GetJSONObjectValue.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Get JSON String Value | documentation:FileOperations/JSON/GetJSONStringValue.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Get JSON Tree Pointer List | documentation:FileOperations/JSON/GetJSONTreePointerList.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / JSON | Open JSON File | documentation:FileOperations/JSON/OpenJSONFile.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Add XML Element | documentation:FileOperations/XML/AddXMLElement.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Close XML File | documentation:FileOperations/XML/CloseXMLFile.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Get XML Attribute | documentation:FileOperations/XML/GetXMLAttribute.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Get XML Element Text Content | documentation:FileOperations/XML/GetXMLElementTextContent.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Open XML File | documentation:FileOperations/XML/OpenXMLFile.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Remove XML Attribute | documentation:FileOperations/XML/RemoveXMLAttribute.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Remove XML Element | documentation:FileOperations/XML/RemoveXMLElement.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Set XML Attribute | documentation:FileOperations/XML/SetXMLAttribute.htm | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| FileOperations / XML | Use NRKXML Library | sdk:FileOperations_XML.txt#1 | client_owned_serialization | Generic JSON or XML parsing and mutation belongs in the client application or a serialization library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get Number of Datums in Datum Ref List | documentation:GDT/GetNumberOfDatumsInDatum.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get Number of Feature Checks in Feature Check Ref List | documentation:GDT/GetNumberOfFeatureChecks.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get i-th Annotation From Annotation Ref List | documentation:GDT/GetI-thAnnotationFromAnnotation.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get i-th Annotation From Annotation Ref List (Iterator) | documentation:GDT/GetI-thAnnotationFromAnnotationIterator.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get i-th Datum From Datum Ref List | documentation:GDT/GetI-thDatumFromDatum.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get i-th Datum From Datum Ref List (Iterator) | documentation:GDT/GetI-thDatumFromDatumIterator.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get i-th Feature Check From Feature Check Ref List | documentation:GDT/GetI-thFeatureCheckFromFeatureCheck.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Get i-th Feature Check From Feature Check Ref List (Iterator) | documentation:GDT/GetI-thFeatureCheckFromFeatureIterator.htm | client_owned_value_construction | Pure value construction, decomposition, or reference-list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT | Start/Stop Feature Check Trapping | documentation:GDT/StartStopFeatureCheckTrapping.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/49, https://github.com/spatialanalyzer/briosa/issues/52 |
| GDT / GDTConstruct | Make Surface Face List - Runtime Select | documentation:GDT/GDTConstruct/MakeSurfaceFaceList-RuntimeSelect.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Close All Google Sheets Spreadsheets | documentation:GoogleSheets/CloseAllGoogleSheetsSpreadsheets.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Close Google Sheets Spreadsheet | documentation:GoogleSheets/CloseGoogleSheetsSpreadsheet.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Get Google Sheets Spreadsheet Cell Address | documentation:GoogleSheets/GetGoogleSheetsCellAddress.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Insert Column | documentation:GoogleSheets/GoogleSheetsInsertColumn.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Insert Row | documentation:GoogleSheets/GoogleSheetsInsertRow.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Read Boolean | documentation:GoogleSheets/GoogleReadBoolean.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Read Double | documentation:GoogleSheets/GoogleReadDouble.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Read Integer | documentation:GoogleSheets/GoogleReadInteger.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Read String | documentation:GoogleSheets/GoogleReadString.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Read Variables | documentation:GoogleSheets/GoogleReadVariables.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Write Boolean | documentation:GoogleSheets/GoogleWriteBoolean.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Write Double | documentation:GoogleSheets/GoogleWriteDouble.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Write Image | documentation:GoogleSheets/GoogleWriteImage.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Write Integer | documentation:GoogleSheets/GoogleWriteInteger.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Write String | documentation:GoogleSheets/GoogleWriteString.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Google Sheets Write Variables | documentation:GoogleSheets/GoogleWriteVariables.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Open Google Sheets Spreadsheet | documentation:GoogleSheets/OpenGoogleSheetsSpreadsheet.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheets | Set Google Sheets Spreadsheet Cell Address | documentation:GoogleSheets/SetGoogleSheetsCellAddress.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| GoogleSheetsOperations | Google Sheets Run Script | sdk:GoogleSheetsOperations.txt#8 | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Activate/Deactivate Instrument Toolbar | documentation:InstrumentOperations/ActivateDeactivateInstrument.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Close Auto-Correspond Closest Point Dialog | documentation:InstrumentOperations/CloseAutoCorrespondClosest.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Dock Instrument Interface | documentation:InstrumentOperations/DockInstrumentInterface.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Measure Existing Single Point (Manual Guide) | documentation:InstrumentOperations/MeasureExistingSinglePointManual.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Show/Hide Instrument Interface | documentation:InstrumentOperations/ShowHideInstrumentInterface.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Start GD&T Inspection | documentation:InstrumentOperations/StartGDTInspection.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Start GD&T Inspection Design | documentation:InstrumentOperations/StartGDTInspectionDesign.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Start GD&T Inspection Rehearse | documentation:InstrumentOperations/StartGDTInspectionRehearse.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Watch Closest Point | documentation:InstrumentOperations/WatchClosestPoint.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Watch Instrument | documentation:InstrumentOperations/WatchInstrument.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Watch Point to Edge | documentation:InstrumentOperations/WatchPointToEdge.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Watch Point to Objects | documentation:InstrumentOperations/WatchPointToObjects.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Watch Point to Point | documentation:InstrumentOperations/WatchPointToPoint.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Watch Point to Point With View Zooming | documentation:InstrumentOperations/WatchPointToPointWith.htm | operator_ui_dependency | This command depends on operator-driven SpatialAnalyzer UI interaction, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations | Watch Window Template 3D | documentation:InstrumentOperations/WatchWindowTemplate3D.htm | client_owned_external_integration | This command combines operator-facing watch-window behavior with SA-hosted UDP integration, both of which are outside Briosa public gRPC scope. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| InstrumentOperations / CribSheetOperations | Run Crib Sheet | documentation:InstrumentOperations/CribSheetOperations/RunCribSheet.htm | client_owned_state_and_control_flow | Briosa clients own application sequencing and control flow; executing an SA crib-sheet program is outside the public gRPC command surface. | https://github.com/spatialanalyzer/briosa/issues/50, https://github.com/spatialanalyzer/briosa/issues/52 |
| MPSubroutines | Define Subroutine Input Values | documentation:MPSubroutines/DefineSubroutineInputValues.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPSubroutines | Define Subroutine Return Values | documentation:MPSubroutines/DefineSubroutineReturnValues.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPSubroutines | Return from Subroutine Now | documentation:MPSubroutines/ReturnFromSubroutineNow.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPSubroutines | Run Subroutine | documentation:MPSubroutines/RunSubroutine.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Add Task Overview Item | documentation:MPTaskOverview/AddTaskOverviewItem.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Create/Clear Task Overview List | documentation:MPTaskOverview/CreateClearTaskOverviewList.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Set Current Task | documentation:MPTaskOverview/SetCurrentTask.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Set Overview Image | documentation:MPTaskOverview/SetOverviewImage.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Set Overview Title | documentation:MPTaskOverview/SetOverviewTitle.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Set Task Item Comment | documentation:MPTaskOverview/SetTaskItemComment.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Set Task Item Completion Values | documentation:MPTaskOverview/SetTaskItemCompletionValues.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Set Task Item Name | documentation:MPTaskOverview/SetTaskItemName.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Set Task Item Status | documentation:MPTaskOverview/SetTaskItemStatus.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Show Progress for Task Item | documentation:MPTaskOverview/ShowProgressforTaskItem.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MPTaskOverview | Show Task Overview List | documentation:MPTaskOverview/ShowTaskOverviewList.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Add Graphics View to Report | documentation:MSOfficeReportingOperations/AddGraphicsViewToReport.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Add Objects to Report | documentation:MSOfficeReportingOperations/AddObjectsToReport.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Add Preset Notes to Report | documentation:MSOfficeReportingOperations/AddPresetNotesToReport.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Add SADoc From File (RTF) | documentation:MSOfficeReportingOperations/AddSADocFromFileRTF.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Add Section Heading to Report | documentation:MSOfficeReportingOperations/AddSectionHeadingToReport.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Add User Input Notes to Report | documentation:MSOfficeReportingOperations/AddUserInputNotesToReport.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Adds an image to an MS Office report. | documentation:MSOfficeReportingOperations/AddsAnImageToAnMSOffice.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Close Office Report | documentation:MSOfficeReportingOperations/CloseOfficeReport.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Initialize Office Report | documentation:MSOfficeReportingOperations/InitializeOfficeReport.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Insert Graphics from file | sdk:MSOfficeReportingOperations.txt#6 | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Insert Section Break | documentation:MSOfficeReportingOperations/InsertSectionBreak.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Make Report Table | documentation:MSOfficeReportingOperations/MakeReportTable.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Save Office Report as RTF | documentation:MSOfficeReportingOperations/SaveOfficeReportAsRTF.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| MSOfficeReportingOperations | Set Page Orientation | documentation:MSOfficeReportingOperations/SetPageOrientation.htm | client_owned_office_integration | Microsoft Office document generation belongs in the client application or a dedicated reporting library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for Double | documentation:ProcessFlowOperations/AskforDouble.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for Integer | documentation:ProcessFlowOperations/AskforInteger.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for Point Name | documentation:ProcessFlowOperations/AskforPointName.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for String | documentation:ProcessFlowOperations/AskforString.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for String (Pull-Down Version) | documentation:ProcessFlowOperations/AskforStringPullDownVersion.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for User Decision (Pull-Down Version) | documentation:ProcessFlowOperations/AskforUserDecisionPullDownVersion.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for User Decision Extended | documentation:ProcessFlowOperations/AskforUserDecisionExtended.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for User Decision from Image | documentation:ProcessFlowOperations/AskforUserDecisionfromImage.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for User Decision from Strings | documentation:ProcessFlowOperations/AskforUserDecisionfromStrings.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Ask for User Decision(HTML) | documentation:ProcessFlowOperations/AskforUserDecisionHTML.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Create Counter | documentation:ProcessFlowOperations/CreateCounter.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Decrement Counter | documentation:ProcessFlowOperations/DecrementCounter.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Go/No Go - Range Check Results | documentation:ProcessFlowOperations/GoNoGoRangeCheckResults.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Increment Counter | documentation:ProcessFlowOperations/IncrementCounter.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Jump Based on Ranged Status Test | documentation:ProcessFlowOperations/JumpBasedonRangedStatus.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Jump To Other Measurement Plan | documentation:ProcessFlowOperations/JumpToOtherMeasurement.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Jump To Step | documentation:ProcessFlowOperations/JumpToStep.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Output SA Report to Excel | documentation:ProcessFlowOperations/OutputSAReportToExcel.htm | client_owned_spreadsheet_integration | Spreadsheet integration belongs in the client application or a dedicated spreadsheet library. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Reset Counter | documentation:ProcessFlowOperations/ResetCounter.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Step Status Test | documentation:ProcessFlowOperations/StepStatusTest.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ProcessFlowOperations | Wait for Steps to Complete | documentation:ProcessFlowOperations/WaitforStepstoComplete.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| RelationshipOperations | Make Auto Filter Proximity Settings | documentation:RelationshipOperations/MakeAutoFilterProximity.htm | client_owned_value_construction | Constructing a transportable option value does not require SpatialAnalyzer state and belongs in the client library. | https://github.com/spatialanalyzer/briosa/issues/51 |
| RelationshipOperations | Relationship Watch Window Template | documentation:RelationshipOperations/RelationshipWatchWindowTemplate.htm | operator_ui_dependency | This command depends on operator-driven watch or trapping UI, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| RelationshipOperations | Show/Hide Relationship Watch | documentation:RelationshipOperations/ShowHideRelationshipWatch.htm | operator_ui_dependency | This command depends on operator-driven watch or trapping UI, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| RelationshipOperations | Sort Relationship Ref List | documentation:RelationshipOperations/SortRelationshipRefList.htm | client_owned_value_computation | This deterministic value or reference-list computation belongs in the client library and does not require SpatialAnalyzer automation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| RelationshipOperations | Start/Stop Relationship Trapping | documentation:RelationshipOperations/StartStopRelationshipTrapping.htm | operator_ui_dependency | This command depends on operator-driven watch or trapping UI, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | Close HTML Display Board | documentation:ReportingOperations/CloseHTMLDisplayBoard.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | HTML Display Board | documentation:ReportingOperations/HTMLDisplayBoard.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | Make Report Graphical View Options | documentation:ReportingOperations/MakeReportGraphicalView.htm | client_owned_value_construction | Constructing a transportable option value does not require SpatialAnalyzer state and belongs in the client library. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | Make Report Output Options | documentation:ReportingOperations/MakeReportOutputOptions.htm | client_owned_value_construction | Constructing a transportable option value does not require SpatialAnalyzer state and belongs in the client library. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | Notify User Double | documentation:ReportingOperations/NotifyUserDouble.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | Notify User HTML | documentation:ReportingOperations/NotifyUserHTML.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | Notify User Integer | documentation:ReportingOperations/NotifyUserInteger.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ReportingOperations | Notify User Text Array | documentation:ReportingOperations/NotifyUserTextArray.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ScalarMathOperations | Boolean Comparison | documentation:ScalarMathOperations/BooleanComparison.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Boolean Comparison (result) | documentation:ScalarMathOperations/BooleanComparisonResult.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Change String Case | documentation:ScalarMathOperations/ChangeStringCase.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Color Comparison | documentation:ScalarMathOperations/ColorComparison.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Color Comparison (result) | documentation:ScalarMathOperations/ColorComparisonResult.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Does String Contain Sub-String | documentation:ScalarMathOperations/DoesStringContainSub-String.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Double Absolute Value | documentation:ScalarMathOperations/DoubleAbsoluteValue.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Double Angle Conversion | documentation:ScalarMathOperations/DoubleAngleConversion.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Double Comparison | documentation:ScalarMathOperations/DoubleComparison.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Double Comparison (result) | documentation:ScalarMathOperations/DoubleComparisonResult.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Double Math Operation | documentation:ScalarMathOperations/DoubleMathOperation.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Double Square Root | documentation:ScalarMathOperations/DoubleSquareRoot.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Integer Absolute Value | documentation:ScalarMathOperations/IntegerAbsoluteValue.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Integer Comparison | documentation:ScalarMathOperations/IntegerComparison.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Integer Comparison (result) | documentation:ScalarMathOperations/IntegerComparisonResult.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Integer Math Operation | documentation:ScalarMathOperations/IntegerMathOperation.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Logarithmic Function | documentation:ScalarMathOperations/LogarithmicFunction.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Round Double | documentation:ScalarMathOperations/RoundDouble.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | String Comparison | documentation:ScalarMathOperations/StringComparison.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | String Comparison (result) | documentation:ScalarMathOperations/StringComparisonResult.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ScalarMathOperations | Trig Function | documentation:ScalarMathOperations/TrigFunction.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Network | Get Screen Resolution | documentation:UtilityOperations/Network/GetScreenResolution.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Network | HTTP GET Request | documentation:UtilityOperations/Network/HTTPGETRequest.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Network | HTTPS Enter User Credentials | documentation:UtilityOperations/Network/HTTPSEnterUserCredentials.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Network | HTTPS GET Request | sdk:UtilityOperations_Network.txt#4 | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Network | Lock/Unlock Trapping Control | documentation:UtilityOperations/Network/LockUnlockTrappingControl.htm | client_owned_state_and_control_flow | Programming-language control flow and MP runtime sequencing belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Network | UDP Receive String | documentation:UtilityOperations/Network/UDPReceiveString.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Network | UDP Send String | documentation:UtilityOperations/Network/UDPSendString.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-DAServer | Get OPC DA Tag Value Double | documentation:UtilityOperations/OPC-DAServer/GetOPCDATagValueDouble.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-DAServer | Get OPC DA Tag Value Integer | documentation:UtilityOperations/OPC-DAServer/GetOPCDATagValueInteger.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-DAServer | Get OPC DA Tag Value String | documentation:UtilityOperations/OPC-DAServer/GetOPCDATagValueString.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-DAServer | Set OPC DA Tag Value Double | documentation:UtilityOperations/OPC-DAServer/SetOPCDATagValueDouble.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-DAServer | Set OPC DA Tag Value Integer | documentation:UtilityOperations/OPC-DAServer/SetOPCDATagValueInteger.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-DAServer | Set OPC DA Tag Value String | documentation:UtilityOperations/OPC-DAServer/SetOPCDATagValueString.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Connect to OPC UA Server | documentation:UtilityOperations/OPC-UAClient/ConnectToOPCUAServer.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Disconnect from OPC UA Server | documentation:UtilityOperations/OPC-UAClient/DisconnectFromOPCUAServer.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Get OPC UA Node Value Double | documentation:UtilityOperations/OPC-UAClient/GetOPCUANodeValueDouble.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Get OPC UA Node Value Integer | documentation:UtilityOperations/OPC-UAClient/GetOPCUANodeValueInteger.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Get OPC UA Node Value String | documentation:UtilityOperations/OPC-UAClient/GetOPCUANodeValueString.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | OPC UA Show Diagnostic Display | documentation:UtilityOperations/OPC-UAClient/OPCUAShowDiagnosticDisplay.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Register OPC UA Node Notification Service (Boolean) | documentation:UtilityOperations/OPC-UAClient/RegisterOPCUANodeNotificationService(Boolean).htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Register OPC UA Node Notification Service (MP Heartbeat) | documentation:UtilityOperations/OPC-UAClient/RegisterOPCUANodeNotificationService(Heartbeat).htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Register OPC UA Node Notification Service (String) | documentation:UtilityOperations/OPC-UAClient/RegisterOPCUANodeNotificationService(String).htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Reset OPC UA Notification Timeout | documentation:UtilityOperations/OPC-UAClient/ResetOPCUANotificationTimeout.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Set OPC UA Node Value Double | documentation:UtilityOperations/OPC-UAClient/SetOPCUANodeValueDouble.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Set OPC UA Node Value Integer | documentation:UtilityOperations/OPC-UAClient/SetOPCUANodeValueInteger.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Set OPC UA Node Value String | documentation:UtilityOperations/OPC-UAClient/SetOPCUANodeValueString.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Set OPC UA Notification Service State | documentation:UtilityOperations/OPC-UAClient/SetOPCUANotificationServiceState.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Set OPC UA Notification Service String | documentation:UtilityOperations/OPC-UAClient/SetOPCUANotificationServiceString.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Start OPC UA Node Notification Services | documentation:UtilityOperations/OPC-UAClient/StartOPCUANodeNotificationServices.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Stop OPC UA Node Notification Services | documentation:UtilityOperations/OPC-UAClient/StopOPCUANodeNotificationServices.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Subscribe to OPC UA Node | documentation:UtilityOperations/OPC-UAClient/SubscribetoOPCUANode.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Unregister All OPC UA Node Notification Services | documentation:UtilityOperations/OPC-UAClient/UnregisterAllOPCUANodeNotificationServices.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Unregister OPC UA Node Notification Service | documentation:UtilityOperations/OPC-UAClient/UnregisterOPCUANodeNotificationService.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Unsubscribe From All OPC UA Nodes | documentation:UtilityOperations/OPC-UAClient/UnsubscribeFromAllOPCUANodesr.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPC-UAClient | Unsubscribe From OPC UA Node | documentation:UtilityOperations/OPC-UAClient/UnsubscribeFromOPCUANodes.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPCUAClient | Get OPC UA Node Named Coordinate Frame | sdk:UtilityOperations_OPCUAClient.txt#25 | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPCUAClient | OPC UA MP Configuration Auto Run Settings | sdk:UtilityOperations_OPCUAClient.txt#23 | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / OPCUAClient | Set OPC UA Node Named Coordinate Frame | sdk:UtilityOperations_OPCUAClient.txt#24 | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Close All Watch Windows | documentation:UtilityOperations/Units/CloseAllWatchWindows.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Copy Directory | documentation:UtilityOperations/Units/CopyDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Units | Delay for Specified Time | documentation:UtilityOperations/Units/DelayForSpecifiedTime.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Delete Directory | documentation:UtilityOperations/Units/DeleteDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Units | Directory Existence | documentation:UtilityOperations/Units/DirectoryExistence.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Units | Generate Random Number | documentation:UtilityOperations/Units/GenerateRandomNumber.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Get Tick Count | documentation:UtilityOperations/Units/GetTickCount.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Increment Point Name | documentation:UtilityOperations/Units/IncrementPointName.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Make Directory | documentation:UtilityOperations/Units/MakeDirectory.htm | client_owned_external_integration | Generic filesystem, database, or process integration belongs in the client application and would unnecessarily expand Briosa authority. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Units | Move Instruments Drag Graphically | documentation:UtilityOperations/Units/MoveInstrumentsDragGraphically.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Move Objects Drag Graphically | documentation:UtilityOperations/Units/MoveObjectsDragGraphically.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Remove Specified Characters From String | documentation:UtilityOperations/Units/RemoveSpecifiedCharacters.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Send MP Result to External Device | documentation:UtilityOperations/Units/SendMPResultToExternal.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Send MP Step’s Status to External Device | documentation:UtilityOperations/Units/SendMPStepsStatusToExternal.htm | client_owned_external_integration | Network and industrial-protocol integration belongs in the client application or a future Briosa-owned integration, not an SA-hosted MP wrapper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Set Interaction Mode | documentation:UtilityOperations/Units/SetInteractionMode.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Set MP Step Mode | documentation:UtilityOperations/Units/SetMPStepMode.htm | client_owned_state_and_control_flow | Programming-language control flow and MP runtime sequencing belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Units | Set Notification Cancel Override | documentation:UtilityOperations/Units/SetNotificationCancelOverride.htm | client_owned_state_and_control_flow | Programming-language control flow and MP runtime sequencing belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Units | Set Special MP Mode | documentation:UtilityOperations/Units/SetSpecialMPMode.htm | client_owned_state_and_control_flow | Programming-language control flow and MP runtime sequencing belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/51 |
| UtilityOperations / Units | Set User Interface Profile | documentation:UtilityOperations/Units/SetUserInterfaceProfile.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Speak To User | documentation:UtilityOperations/Units/SpeakToUser.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Status Dialog | documentation:UtilityOperations/Units/StatusDialog.htm | client_owned_user_experience | Operator prompts and interactive SpatialAnalyzer UI control belong in the client application. | https://github.com/spatialanalyzer/briosa/issues/52 |
| UtilityOperations / Units | Step Comment | documentation:UtilityOperations/Units/StepComment.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Add Double to Named Double List Variable | documentation:Variables/AddDoubleToNamedDouble.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Clear Double List | documentation:Variables/ClearDoubleList.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Clear Named Double List Variable | documentation:Variables/ClearNamedDoubleListVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Delete Variable | documentation:Variables/DeleteVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Delete Variables - Wildcard Match | documentation:Variables/DeleteVariablesWildcard.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Boolean Variable | sdk:Variables.txt#17 | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Collection Object Name Variable | documentation:Variables/GetCollectionObjectName.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Collection Object Ref List Variable | documentation:Variables/GetCollectionObjectRef.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Double Variable | documentation:Variables/GetDoubleVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Font Variable | documentation:Variables/GetFontVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Integer Variable | documentation:Variables/GetIntegerVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Named Double List Variable | documentation:Variables/GetNamedDoubleListVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Named Double List Variable Min/Max | documentation:Variables/GetNamedDoubleListVariableMinMax.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Point Name Ref List Variable | documentation:Variables/GetPointNameRefListVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Point Name Variable | documentation:Variables/GetPointNameVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Relationship Ref List Variable | documentation:Variables/GetRelationshipRefList.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Report Items Reference List Variable | documentation:Variables/GetReportItemsReference.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get String Ref List Variable | documentation:Variables/GetStringRefListVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get String Variable | documentation:Variables/GetStringVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Transform Variable | documentation:Variables/GetTransformVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Vector Name Ref List Variable | sdk:Variables.txt#35 | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get Vector Variable | documentation:Variables/GetVectorVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get i-th Double From List | documentation:Variables/GetI-thDoubleFromList.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Get number of Doubles in List | documentation:Variables/GetNumberOfDoublesInList.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Boolean Variable | documentation:Variables/SetBooleanVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Collection Object Name Variable | documentation:Variables/SetCollectionObjectName.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Collection Object Ref List Variable | documentation:Variables/SetCollectionObjectRef.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Double List Variable | sdk:Variables.txt#10 | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Double Variable | documentation:Variables/SetDoubleVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Font Variable | documentation:Variables/SetFontVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Integer Variable | documentation:Variables/SetIntegerVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Named Double List Variable | documentation:Variables/SetNamedDoubleListVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Point Name Ref List Variable | documentation:Variables/SetPointNameRefListVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Point Name Variable | documentation:Variables/SetPointNameVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Relationship Ref List Variable | documentation:Variables/SetRelationshipRefList.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Report Items Reference List Variable | documentation:Variables/SetReportItemsReference.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set String Ref List Variable | documentation:Variables/SetStringRefListVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set String Variable | documentation:Variables/SetStringVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Transform Variable | documentation:Variables/SetTransformVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Vector Name Ref List Variable | sdk:Variables.txt#34 | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Variables | Set Vector Variable | documentation:Variables/SetVectorVariable.htm | client_owned_state_and_control_flow | Client-language state and control flow replace this measurement-plan programming helper. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations | Add a Vector to Vector Name Ref List | documentation:Vector Operations/AddAVectorToVectorName.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations | Get Number of Vectors in Vector Name Ref List | documentation:Vector Operations/GetNumberOfVectorsInVectorName.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations | Get i-th Vector From Vector Name Ref List | documentation:Vector Operations/GetI-thVectorFromVectorName.htm | client_owned_value_construction | Pure value construction, conversion, decomposition, or list algebra belongs in Briosa client types and client-language code. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations / VectorMathOperations | Vector Addition | documentation:Vector Operations/VectorMathOperations/VectorAddition.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations / VectorMathOperations | Vector Cross Product | documentation:Vector Operations/VectorMathOperations/VectorCrossProduct.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations / VectorMathOperations | Vector Dot Product | documentation:Vector Operations/VectorMathOperations/VectorDotProduct.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations / VectorMathOperations | Vector Magnitude (Length) | documentation:Vector Operations/VectorMathOperations/VectorMagnitudeLength.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations / VectorMathOperations | Vector Normalize | documentation:Vector Operations/VectorMathOperations/VectorNormalize.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations / VectorMathOperations | Vector Scaling | documentation:Vector Operations/VectorMathOperations/VectorScaling.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| Vector Operations / VectorMathOperations | Vector Subtraction | documentation:Vector Operations/VectorMathOperations/VectorSubtraction.htm | client_owned_value_computation | Pure value computation belongs in the client application and does not require SpatialAnalyzer. | https://github.com/spatialanalyzer/briosa/issues/52 |
| ViewControl | Set MP’s Window State | documentation:ViewControl/SetMPsWindowState.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl | Set SA’s Window Pos | documentation:ViewControl/SetSAsWindowPos.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl | Set SA’s Window Size | documentation:ViewControl/SetSAsWindowSize.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl | Set SA’s Window State | documentation:ViewControl/SetSAsWindowState.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl / Colors | Convert Integer Values to RGB | documentation:ViewControl/Colors/ConvertIntegerValuestoRGB.htm | client_owned_value_computation | This deterministic value or reference-list computation belongs in the client library and does not require SpatialAnalyzer automation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl / Colors | Convert RGB Values to Integer | documentation:ViewControl/Colors/ConvertRGBValuestoInteger.htm | client_owned_value_computation | This deterministic value or reference-list computation belongs in the client library and does not require SpatialAnalyzer automation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl / HideShowOperations | Set Toolkit Visibility | documentation:ViewControl/HideShowOperations/SetToolkitVisibility.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl / HideShowOperations | Show/Hide Inspection Bar | sdk:ViewControl_HideShowOperations.txt#16 | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl / HideShowOperations | Show/Hide Relationship Watch | documentation:ViewControl/HideShowOperations/ShowHideRelationshipWatch.htm | operator_ui_dependency | This command depends on operator-driven watch or trapping UI, which is not suitable for a deterministic unattended gRPC operation. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl / RibbonBar | Load Ribbon Bar from XML File | documentation:ViewControl/RibbonBar/LoadRibbonBarfromXMLFile.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |
| ViewControl / RibbonBar | Reset Ribbon Bar to Default | documentation:ViewControl/RibbonBar/ResetRibbonBartoDefault.htm | client_owned_user_experience | Operator prompts and application-chrome control belong in the client application rather than an unattended gRPC API. | https://github.com/spatialanalyzer/briosa/issues/51 |

## Promotion policy

- Only `approved_candidate` entries with `reviewed` state can be promoted into the supported command catalog.
- `unreviewed` and `needs_re_review` entries fail closed and remain absent from runtime capabilities.
- `intentional_exclusion` and `sdk_unavailable` are final non-supported dispositions with Briosa-authored reasons.
- `blocked` identifies a named dependency and cannot silently become supported.
- A changed per-command inventory fingerprint requires re-review before promotion.
