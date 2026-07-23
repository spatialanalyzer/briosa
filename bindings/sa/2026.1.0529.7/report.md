# SA 2026.1.0529.7 SDK binding registry

This deterministic report reconciles extracted View SDK Code evidence with the committed exact-target interop API. A usable binding does not approve any MP operation by itself.

## Coverage

- Binding methods: 151
- Semantic value families: 111
- Inventory-observed setters: 105
- Inventory-observed getters: 29
- Interop-exposed setters: 106
- Interop-exposed getters: 39

## Source status

| Value | Count |
| --- | ---: |
| `interop_only` | 17 |
| `inventory_and_interop` | 128 |
| `inventory_only` | 6 |

## Registry status

| Value | Count |
| --- | ---: |
| `blocked_missing_interop` | 6 |
| `excluded_only` | 14 |
| `unobserved_interop` | 17 |
| `usable` | 114 |

## Protocol coverage

| Value | Count |
| --- | ---: |
| `blocked` | 6 |
| `implemented` | 14 |
| `not_required` | 31 |
| `planned` | 100 |

## Worker coverage

| Value | Count |
| --- | ---: |
| `blocked` | 6 |
| `implemented` | 14 |
| `not_required` | 31 |
| `planned` | 100 |

## Adapter coverage

| Value | Count |
| --- | ---: |
| `blocked` | 6 |
| `implemented` | 14 |
| `not_required` | 31 |
| `planned` | 100 |

## Fake coverage

| Value | Count |
| --- | ---: |
| `blocked` | 6 |
| `implemented` | 14 |
| `not_required` | 31 |
| `planned` | 100 |

## Generator coverage

| Value | Count |
| --- | ---: |
| `blocked` | 6 |
| `implemented` | 14 |
| `not_required` | 31 |
| `planned` | 100 |

## Value-family implementation status

| Value | Count |
| --- | ---: |
| `blocked` | 6 |
| `implemented` | 7 |
| `not_required` | 17 |
| `planned` | 81 |

## Semantic value families

| Family | Shape | Directions | Public target | Worker target | Implementation | Bindings |
| --- | --- | --- | --- | --- | --- | --- |
| `angular_unit` | `enum` | setter | `AngularUnit` | `SdkAngularUnitValue` | `planned` | SetAngularUnitsArg |
| `ascii_file_format` | `enum` | setter | `AsciiFileFormat` | `SdkAsciiFileFormatValue` | `planned` | SetAsciiFileFormatArg |
| `auto_filter_proximity_settings` | `structured` | getter, setter | `AutoFilterProximitySettings` | `SdkAutoFilterProximitySettingsValue` | `planned` | GetAutoFilterProximitySettingsArg, SetAutoFilterProximitySettingsArg |
| `axis_identifier` | `enum` | setter | `AxisIdentifier` | `SdkAxisIdentifierValue` | `planned` | SetAxisNameArg |
| `b_spline_fit_options` | `structured` | getter, setter | `BSplineFitOptions` | `SdkBSplineFitOptionsValue` | `planned` | GetBSPlineFitOptionsArg, SetBSplineFitOptionsArg |
| `base_color_type` | `enum` | setter | `BaseColorType` | `SdkBaseColorTypeValue` | `planned` | SetBaseColorTypeArg |
| `base_mid_color_type` | `enum` | setter | `BaseMidColorType` | `SdkBaseMidColorTypeValue` | `planned` | SetBaseMidColorTypeArg |
| `chart_name` | `identifier` | getter, setter | `string` | `string` | `planned` | GetChartNameArg, SetChartNameArg |
| `chart_type` | `enum` | setter | `ChartType` | `SdkChartTypeValue` | `planned` | SetChartTypeArg |
| `cloud_name` | `identifier` | getter, setter | `string` | `string` | `planned` | GetCloudNameArg, SetCloudNameArg |
| `cloud_thinning_mode` | `enum` | setter | `CloudThinningMode` | `SdkCloudThinningModeValue` | `blocked` | SetCloudThinningModeTypeArg |
| `cloud_thinning_options` | `structured` | setter | `CloudThinningOptions` | `SdkCloudThinningOptionsValue` | `planned` | SetCloudThinningOptionsArg |
| `collection_group_name_list` | `reference_list` | getter, setter | `CollectionGroupNameList` | `SdkCollectionGroupNameListValue` | `planned` | GetCollectionGroupNameRefListArg, SetCollectionGroupNameRefListArg |
| `collection_instrument_id` | `identifier` | getter, setter | `CollectionInstrumentId` | `SdkCollectionInstrumentIdValue` | `planned` | GetColInstIdArg, SetColInstIdArg |
| `collection_instrument_id_list` | `reference_list` | getter, setter | `CollectionInstrumentIdList` | `SdkCollectionInstrumentIdListValue` | `planned` | GetColInstIdRefListArg, SetColInstIdRefListArg |
| `collection_machine_id` | `identifier` | getter, setter | `CollectionMachineId` | `SdkCollectionMachineIdValue` | `planned` | GetColMachineIdArg, SetColMachineIdArg |
| `collection_name` | `identifier` | getter, setter | `string` | `string` | `planned` | GetCollectionNameArg, SetCollectionNameArg |
| `collection_object_name` | `identifier` | getter, setter | `CollectionObjectName` | `SdkCollectionObjectNameValue` | `planned` | GetCollectionObjectNameArg, SetCollectionObjectNameArg, SetCollectionObjectNameArg2 |
| `collection_object_name_list` | `reference_list` | getter, setter | `CollectionObjectNameList` | `SdkCollectionObjectNameListValue` | `planned` | GetCollectionObjectNameRefListArg, SetCollectionObjectNameRefListArg |
| `collection_vector_group_name` | `identifier` | getter, setter | `CollectionVectorGroupName` | `SdkCollectionVectorGroupNameValue` | `planned` | GetColVectorGroupNameArg, SetColVectorGroupNameArg |
| `collection_vector_group_name_list` | `reference_list` | getter, setter | `CollectionVectorGroupNameList` | `SdkCollectionVectorGroupNameListValue` | `planned` | GetCollectionVectorGroupNameRefListArg, SetCollectionVectorGroupNameRefListArg |
| `collimation_baseline_type` | `enum` | setter | `CollimationBaselineType` | `SdkCollimationBaselineTypeValue` | `planned` | SetCollimationBaselineTypeArg |
| `collimation_type` | `enum` | setter | `CollimationType` | `SdkCollimationTypeValue` | `planned` | SetCollimationTypeArg |
| `color_range_method` | `enum` | setter | `ColorRangeMethod` | `SdkColorRangeMethodValue` | `planned` | SetColorRangeMethodArg |
| `colorization_options` | `structured` | setter | `ColorizationOptions` | `SdkColorizationOptionsValue` | `planned` | SetColorizationOptionsArg |
| `computation_technique` | `enum` | setter | `ComputationTechnique` | `SdkComputationTechniqueValue` | `planned` | SetCompTechniqueArg |
| `coordinate_system_type` | `enum` | setter | `CoordinateSystemType` | `SdkCoordinateSystemTypeValue` | `planned` | SetCoordinateSystemTypeArg |
| `dataset_type` | `enum` | setter | `DatasetType` | `SdkDatasetTypeValue` | `planned` | SetDatasetTypeArg |
| `degree_of_freedom` | `enum` | setter | `DegreeOfFreedom` | `SdkDegreeOfFreedomValue` | `planned` | SetDegreeOfFreedomArg |
| `distance_unit` | `enum` | setter | `DistanceUnit` | `SdkDistanceUnitValue` | `planned` | SetDistanceUnitsArg |
| `double_array` | `array` | getter, setter | `DoubleArray` | `SdkDoubleArrayValue` | `planned` | GetDoubleArrayArg, SetDoubleArrayArg |
| `dynamic_circle_mode` | `enum` | setter | `DynamicCircleMode` | `SdkDynamicCircleModeValue` | `planned` | SetDynamicCircleModeArg |
| `dynamic_ellipse_mode` | `enum` | setter | `DynamicEllipseMode` | `SdkDynamicEllipseModeValue` | `planned` | SetDynamicEllipseModeArg |
| `dynamic_line_mode` | `enum` | setter | `DynamicLineMode` | `SdkDynamicLineModeValue` | `planned` | SetDynamicLineModeArg |
| `dynamic_plane_mode` | `enum` | setter | `DynamicPlaneMode` | `SdkDynamicPlaneModeValue` | `planned` | SetDynamicPlaneModeArg |
| `dynamic_point_mode` | `enum` | setter | `DynamicPointMode` | `SdkDynamicPointModeValue` | `planned` | SetDynamicPointModeArg |
| `edge_mode` | `enum` | setter | `EdgeMode` | `SdkEdgeModeValue` | `planned` | SetEdgeModeArg |
| `edit_text` | `array` | getter, setter | `StringList` | `SdkStringListValue` | `planned` | GetEditTextArg, SetEditTextArg |
| `export_data_delimiter_type` | `enum` | setter | `ExportDataDelimiterType` | `SdkExportDataDelimiterTypeValue` | `planned` | SetExportDataDelimeterTypeArg |
| `export_target_name_format` | `enum` | setter | `ExportTargetNameFormat` | `SdkExportTargetNameFormatValue` | `planned` | SetExportTargetNameFormatArg |
| `export_vector_name_format` | `enum` | setter | `ExportVectorNameFormat` | `SdkExportVectorNameFormatValue` | `planned` | SetExportVectorNameFormatArg |
| `file_reference` | `path` | getter, setter | `FileReference` | `SdkFileReferenceValue` | `planned` | GetFilePathArg, SetFilePathArg |
| `fit_constraint_scalar_options` | `structured` | getter, setter | `FitConstraintScalarOptions` | `SdkFitConstraintScalarOptionsValue` | `planned` | GetFitConstraintScalarOptionsArg, SetFitConstraintScalarOptionsArg |
| `fit_degree_of_freedom_options` | `structured` | setter | `FitDegreeOfFreedomOptions` | `SdkFitDegreeOfFreedomOptionsValue` | `planned` | SetFitDofOptionsArg |
| `fit_method` | `enum` | setter | `FitMethod` | `SdkFitMethodValue` | `planned` | SetFitMethodArg |
| `floating_point` | `scalar` | getter, setter | `double` | `double` | `implemented` | GetDoubleArg, SetDoubleArg |
| `font` | `structured` | setter | `Font` | `SdkFontValue` | `planned` | SetFontTypeArg |
| `frame_name` | `identifier` | getter, setter | `string` | `string` | `planned` | GetFrameNameArg, SetFrameNameArg |
| `gdt_check_validator_type` | `enum` | setter | `GdtCheckValidatorType` | `SdkGdtCheckValidatorTypeValue` | `blocked` | SetMPGDTOptionsCheckValidatorTypeArg |
| `gdt_distance_between_mode` | `enum` | setter | `GdtDistanceBetweenMode` | `SdkGdtDistanceBetweenModeValue` | `blocked` | SetMPGDTOptionsDistanceBetweenModeArg |
| `geometry_type` | `enum` | setter | `GeometryType` | `SdkGeometryTypeValue` | `planned` | SetGeometryTypeArg |
| `instrument_id` | `identifier` | getter, setter | `int32` | `int` | `not_required` | GetInstIdArg, SetInstIdArg |
| `instrument_type` | `enum` | setter | `InstrumentType` | `SdkInstrumentTypeValue` | `planned` | SetInstTypeNameArg |
| `item_type` | `enum` | setter | `ItemType` | `SdkItemTypeValue` | `blocked` | SetItemTypeArg |
| `logarithmic_function` | `enum` | setter | `LogarithmicFunction` | `SdkLogarithmicFunctionValue` | `not_required` | SetLogarithmicFunctionArg |
| `logical` | `scalar` | getter, setter | `bool` | `bool` | `implemented` | GetBoolArg, SetBoolArg |
| `math_operation` | `enum` | setter | `MathOperation` | `SdkMathOperationValue` | `not_required` | SetMathOperationArg |
| `measured_side_for_planar_offset` | `enum` | setter | `MeasuredSideForPlanarOffset` | `SdkMeasuredSideForPlanarOffsetValue` | `planned` | SetMeasuredSideForPlanarOffsetArg |
| `measured_side_for_radial_offset` | `enum` | setter | `MeasuredSideForRadialOffset` | `SdkMeasuredSideForRadialOffsetValue` | `planned` | SetMeasuredSideForRadialOffsetArg |
| `mesh_orientation_type` | `enum` | setter | `MeshOrientationType` | `SdkMeshOrientationTypeValue` | `blocked` | SetMeshOrientationTypeArg |
| `move_direction_type` | `enum` | setter | `MoveDirectionType` | `SdkMoveDirectionTypeValue` | `not_required` | SetMoveDirectionTypeArg |
| `mp_dialog_interaction_mode` | `enum` | setter | `MpDialogInteractionMode` | `SdkMpDialogInteractionModeValue` | `not_required` | SetMPDialogInteractionModeArg |
| `mp_interaction_mode` | `enum` | setter | `MpInteractionMode` | `SdkMpInteractionModeValue` | `not_required` | SetMPInteractionModeArg |
| `normal_direction` | `enum` | setter | `NormalDirection` | `SdkNormalDirectionValue` | `planned` | SetNormalDirectionArg |
| `numeric_comparison_type` | `enum` | setter | `NumericComparisonType` | `SdkNumericComparisonTypeValue` | `not_required` | SetNumComparisonTypeArg |
| `object_name` | `identifier` | getter, setter | `string` | `string` | `not_required` | GetObjectNameArg, SetObjectNameArg |
| `object_type` | `enum` | setter | `ObjectType` | `SdkObjectTypeValue` | `planned` | SetObjectTypeArg |
| `offset_direction_type` | `enum` | setter | `OffsetDirectionType` | `SdkOffsetDirectionTypeValue` | `planned` | SetOffsetDirectionTypeArg |
| `perimeter_name` | `identifier` | getter, setter | `string` | `string` | `not_required` | GetPerimeterNameArg, SetPerimeterNameArg |
| `point_delta_report_options` | `structured` | setter | `PointDeltaReportOptions` | `SdkPointDeltaReportOptionsValue` | `planned` | SetPointDeltaReportOptionsArg |
| `point_filter_input_type` | `enum` | setter | `PointFilterInputType` | `SdkPointFilterInputTypeValue` | `planned` | SetPointFilterInputTypeArg |
| `point_name` | `identifier` | getter, setter | `PointName` | `SdkPointNameValue` | `implemented` | GetPointNameArg, SetPointNameArg |
| `point_name_list` | `reference_list` | getter, setter | `PointNameList` | `SdkPointNameListValue` | `planned` | GetPointNameRefListArg, SetPointNameRefListArg |
| `projection_options` | `structured` | setter | `ProjectionOptions` | `SdkProjectionOptionsValue` | `planned` | SetProjectionOptionsArg |
| `relationship_weighting_mode` | `enum` | setter | `RelationshipWeightingMode` | `SdkRelationshipWeightingModeValue` | `planned` | SetRelWeightingModeArg |
| `render_mode_type` | `enum` | setter | `RenderModeType` | `SdkRenderModeTypeValue` | `planned` | SetRenderModeTypeArg |
| `report_output_options` | `structured` | getter, setter | `ReportOutputOptions` | `SdkReportOutputOptionsValue` | `planned` | GetReportOutputOptionsArg, SetReportOutputOptionsArg |
| `report_page_orientation` | `enum` | setter | `ReportPageOrientation` | `SdkReportPageOrientationValue` | `planned` | SetReportPageSettingsArg |
| `report_type` | `enum` | setter | `ReportType` | `SdkReportTypeValue` | `not_required` | SetReportTypeArg |
| `report_view_options` | `structured` | getter, setter | `ReportViewOptions` | `SdkReportViewOptionsValue` | `planned` | GetReportViewOptionsArg, SetReportViewOptionsArg |
| `result_object_name` | `identifier` | getter, setter | `string` | `string` | `not_required` | GetResultArg, SetResultArg |
| `rgb_color` | `structured` | setter | `RgbColor` | `SdkRgbColorValue` | `planned` | SetColorArg |
| `sa_interaction_mode` | `enum` | setter | `SaInteractionMode` | `SdkSaInteractionModeValue` | `not_required` | SetSAInteractionModeArg |
| `saturation_limit_type` | `enum` | setter | `SaturationLimitType` | `SdkSaturationLimitTypeValue` | `planned` | SetSaturationLimitTypeArg |
| `show_usmn_dialog_type` | `enum` | setter | `ShowUsmnDialogType` | `SdkShowUsmnDialogTypeValue` | `planned` | SetShowUsmnDialogTypeArg |
| `sigmoidal_gap_constraint_options` | `structured` | getter | `SigmoidalGapConstraintOptions` | `SdkSigmoidalGapConstraintOptionsValue` | `blocked` | GetSigmoidalGapConstraintOptionsArg |
| `slot_type` | `enum` | setter | `SlotType` | `SdkSlotTypeValue` | `planned` | SetSlotTypeArg |
| `sphere_fit_computation_mode` | `enum` | setter | `SphereFitComputationMode` | `SdkSphereFitComputationModeValue` | `planned` | SetSphereFitComputationModeArg |
| `string` | `scalar` | getter, setter | `string` | `string` | `implemented` | GetStringArg, SetStringArg |
| `string_list` | `reference_list` | getter, setter | `StringList` | `SdkStringListValue` | `planned` | GetStringRefListArg, SetStringRefListArg |
| `surface_analysis_mode` | `enum` | setter | `SurfaceAnalysisMode` | `SdkSurfaceAnalysisModeValue` | `planned` | SetSurfaceAnalysisModeArg |
| `surface_dissection_mode_type` | `enum` | setter | `SurfaceDissectionModeType` | `SdkSurfaceDissectionModeTypeValue` | `planned` | SetSurfDissectModeTypeArg |
| `system_string` | `enum` | setter | `SystemString` | `SdkSystemStringValue` | `not_required` | SetSystemStringArg |
| `target_computation_method` | `enum` | setter | `TargetComputationMethod` | `SdkTargetComputationMethodValue` | `planned` | SetTargetComputationMethodArg |
| `temperature_unit` | `enum` | setter | `TemperatureUnit` | `SdkTemperatureUnitValue` | `planned` | SetTemperatureUnitsArg |
| `tolerance_scalar_options` | `structured` | getter, setter | `ToleranceScalarOptions` | `SdkToleranceScalarOptionsValue` | `planned` | GetToleranceScalarOptionsArg, SetToleranceScalarOptionsArg |
| `tolerance_vector_options` | `structured` | getter, setter | `ToleranceVectorOptions` | `SdkToleranceVectorOptionsValue` | `implemented` | GetToleranceVectorOptionsArg, SetToleranceVectorOptionsArg |
| `transform` | `transform` | getter, setter | `Transform` | `SdkTransformValue` | `planned` | GetTransformArg, SetTransformArg |
| `translucency_type` | `enum` | setter | `TranslucencyType` | `SdkTranslucencyTypeValue` | `planned` | SetTranslucencyTypeArg |
| `trigonometric_function` | `enum` | setter | `TrigonometricFunction` | `SdkTrigonometricFunctionValue` | `not_required` | SetTrigFunctionArg |
| `udp_transmit_settings` | `structured` | setter | `UdpTransmitSettings` | `SdkUdpTransmitSettingsValue` | `planned` | SetUdpTransmitSettingsArg |
| `user_summary_info_files` | `structured` | setter | `UserSummaryInfoFiles` | `SdkUserSummaryInfoFilesValue` | `not_required` | SetUserSummaryInfoFilesArg |
| `vector3` | `structured` | getter, setter | `Vector3` | `SdkVectorValue` | `implemented` | GetVectorArg, SetVectorArg |
| `vector_group_name` | `identifier` | getter, setter | `string` | `string` | `planned` | GetVectorGroupNameArg, SetVectorGroupNameArg |
| `vector_name_list` | `reference_list` | getter, setter | `VectorNameList` | `SdkVectorNameListValue` | `planned` | GetVectorNameRefListArg, SetVectorNameRefListArg |
| `view_name` | `identifier` | getter, setter | `string` | `string` | `planned` | GetViewNameArg, SetViewNameArg |
| `whole_number` | `scalar` | getter, setter | `int32` | `int` | `implemented` | GetIntegerArg, SetIntegerArg |
| `window_state` | `enum` | setter | `WindowState` | `SdkWindowStateValue` | `planned` | SetWindowStateArg |
| `workbook_address_mode_type` | `enum` | setter | `WorkbookAddressModeType` | `SdkWorkbookAddressModeTypeValue` | `not_required` | SetWorkbookAddressModeTypeArg |
| `world_transform` | `transform` | getter, setter | `WorldTransform` | `SdkWorldTransformValue` | `planned` | GetWorldTransformArg, SetWorldTransformArg |
| `write_mode_type` | `enum` | setter | `WriteModeType` | `SdkWriteModeTypeValue` | `not_required` | SetWriteModeTypeArg |

## Exact binding methods

| Method | Direction | Sources | Status | Family | Protocol | Worker | Adapter | Fake | Generator | Commands | Excluded | Remaining | Signature | Blocker |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | --- | --- |
| `GetAutoFilterProximitySettingsArg` | `getter` | `inventory_and_interop` | `usable` | `auto_filter_proximity_settings` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, ref double sip, ref double eep, ref double pip, ref double pep, ref double rip, ref double gt, ref int32 spm, ref int32 ppm, ref int32 rpm, ref boolean pp, ref boolean apb) |  |
| `GetBSPlineFitOptionsArg` | `getter` | `inventory_and_interop` | `usable` | `b_spline_fit_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, ref boolean bFit, ref boolean bOpen, ref int32 sortMethod, ref int32 terminateMethod, ref int32 degree, ref double terminateLength, ref double terminateAvgMultiplier, ref int32 numFitCPs, ref boolean uniqueCheck, ref double uniqueThreshold, ref double extension, ref boolean useGlobalTessellationOptions, ref double maxChordalDeviation, ref double maxTrimEdgeAngle) |  |
| `GetBoolArg` | `getter` | `inventory_and_interop` | `usable` | `logical` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 56 | 10 | 46 | boolean (value string argName, ref boolean value) |  |
| `GetChartNameArg` | `getter` | `interop_only` | `unobserved_interop` | `chart_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string chartName) |  |
| `GetCloudNameArg` | `getter` | `interop_only` | `unobserved_interop` | `cloud_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string cloudName) |  |
| `GetColInstIdArg` | `getter` | `inventory_and_interop` | `usable` | `collection_instrument_id` | `planned` | `planned` | `planned` | `planned` | `planned` | 8 | 3 | 5 | boolean (value string argName, ref string collectionName, ref int32 instId) |  |
| `GetColInstIdRefListArg` | `getter` | `inventory_and_interop` | `usable` | `collection_instrument_id_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 2 | 1 | boolean (value string argName, ref object colInstIdList) |  |
| `GetColMachineIdArg` | `getter` | `inventory_and_interop` | `excluded_only` | `collection_machine_id` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, ref string collectionName, ref int32 machineId) |  |
| `GetColVectorGroupNameArg` | `getter` | `interop_only` | `unobserved_interop` | `collection_vector_group_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string collectionName, ref string vectorGroupName) |  |
| `GetCollectionGroupNameRefListArg` | `getter` | `interop_only` | `unobserved_interop` | `collection_group_name_list` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref object colGroupNameList) |  |
| `GetCollectionNameArg` | `getter` | `inventory_and_interop` | `usable` | `collection_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 2 | 1 | boolean (value string argName, ref string collectionName) |  |
| `GetCollectionObjectNameArg` | `getter` | `inventory_and_interop` | `usable` | `collection_object_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 37 | 10 | 27 | boolean (value string argName, ref string collectionName, ref string objectName) |  |
| `GetCollectionObjectNameRefListArg` | `getter` | `inventory_and_interop` | `usable` | `collection_object_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 44 | 10 | 34 | boolean (value string argName, ref object objectNameList) |  |
| `GetCollectionVectorGroupNameRefListArg` | `getter` | `inventory_and_interop` | `excluded_only` | `collection_vector_group_name_list` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, ref object colVectorGrpNameList) |  |
| `GetDoubleArg` | `getter` | `inventory_and_interop` | `usable` | `floating_point` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 147 | 41 | 106 | boolean (value string argName, ref double value) |  |
| `GetDoubleArrayArg` | `getter` | `inventory_and_interop` | `usable` | `double_array` | `planned` | `planned` | `planned` | `planned` | `planned` | 7 | 1 | 6 | boolean (value string argName, ref int32 arraySize, ref object arrayIn) |  |
| `GetEditTextArg` | `getter` | `inventory_and_interop` | `usable` | `edit_text` | `planned` | `planned` | `planned` | `planned` | `planned` | 5 | 0 | 5 | boolean (value string argName, ref object stringList) |  |
| `GetFilePathArg` | `getter` | `inventory_and_interop` | `usable` | `file_reference` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, ref string path, ref boolean embeddedFile) |  |
| `GetFitConstraintScalarOptionsArg` | `getter` | `inventory_and_interop` | `usable` | `fit_constraint_scalar_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, ref boolean bUseHigh, ref double highTol, ref boolean bUseLow, ref double lowTol) |  |
| `GetFrameNameArg` | `getter` | `interop_only` | `unobserved_interop` | `frame_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string frameName) |  |
| `GetInstIdArg` | `getter` | `interop_only` | `unobserved_interop` | `instrument_id` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref int32 instId) |  |
| `GetIntegerArg` | `getter` | `inventory_and_interop` | `usable` | `whole_number` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 88 | 36 | 52 | boolean (value string argName, ref int32 value) |  |
| `GetObjectNameArg` | `getter` | `interop_only` | `unobserved_interop` | `object_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string objectName) |  |
| `GetPerimeterNameArg` | `getter` | `interop_only` | `unobserved_interop` | `perimeter_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string perimeterName) |  |
| `GetPointNameArg` | `getter` | `inventory_and_interop` | `usable` | `point_name` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 14 | 7 | 7 | boolean (value string argName, ref string collectionName, ref string groupName, ref string targetName) |  |
| `GetPointNameRefListArg` | `getter` | `inventory_and_interop` | `usable` | `point_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 16 | 4 | 12 | boolean (value string argName, ref object pointNameList) |  |
| `GetReportOutputOptionsArg` | `getter` | `inventory_and_interop` | `usable` | `report_output_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, ref string outputType, ref string pathOrEmbeddedName) |  |
| `GetReportViewOptionsArg` | `getter` | `inventory_and_interop` | `usable` | `report_view_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, ref string viewType, ref string collectionName, ref string calloutName) |  |
| `GetResultArg` | `getter` | `interop_only` | `unobserved_interop` | `result_object_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string objectName) |  |
| `GetSigmoidalGapConstraintOptionsArg` | `getter` | `inventory_only` | `blocked_missing_interop` | `sigmoidal_gap_constraint_options` | `blocked` | `blocked` | `blocked` | `blocked` | `blocked` | 2 | 0 | 2 | not present in interop | https://github.com/spatialanalyzer/briosa/issues/53 |
| `GetStringArg` | `getter` | `inventory_and_interop` | `usable` | `string` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 76 | 38 | 38 | boolean (value string argName, ref string value) |  |
| `GetStringRefListArg` | `getter` | `inventory_and_interop` | `usable` | `string_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 14 | 4 | 10 | boolean (value string argName, ref object stringList) |  |
| `GetToleranceScalarOptionsArg` | `getter` | `inventory_and_interop` | `usable` | `tolerance_scalar_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 0 | 3 | boolean (value string argName, ref boolean bUseHigh, ref double highTol, ref boolean bUseLow, ref double lowTol) |  |
| `GetToleranceVectorOptionsArg` | `getter` | `inventory_and_interop` | `usable` | `tolerance_vector_options` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 5 | 0 | 5 | boolean (value string argName, ref boolean bUseHighX, ref double highTolX, ref boolean bUseHighY, ref double highTolY, ref boolean bUseHighZ, ref double highTolZ, ref boolean bUseHighM, ref double highTolM, ref boolean bUseLowX, ref double lowTolX, ref boolean bUseLowY, ref double lowTolY, ref boolean bUseLowZ, ref double lowTolZ, ref boolean bUseLowM, ref double lowTolM) |  |
| `GetTransformArg` | `getter` | `inventory_and_interop` | `usable` | `transform` | `planned` | `planned` | `planned` | `planned` | `planned` | 22 | 7 | 15 | boolean (value string argName, ref object transform) |  |
| `GetVectorArg` | `getter` | `inventory_and_interop` | `usable` | `vector3` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 42 | 14 | 28 | boolean (value string argName, ref double XValue, ref double yValue, ref double zValue) |  |
| `GetVectorGroupNameArg` | `getter` | `interop_only` | `unobserved_interop` | `vector_group_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string vectorGrpName) |  |
| `GetVectorNameRefListArg` | `getter` | `inventory_and_interop` | `usable` | `vector_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 4 | 2 | 2 | boolean (value string argName, ref object vectorNameList) |  |
| `GetViewNameArg` | `getter` | `interop_only` | `unobserved_interop` | `view_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, ref string viewName) |  |
| `GetWorldTransformArg` | `getter` | `inventory_and_interop` | `usable` | `world_transform` | `planned` | `planned` | `planned` | `planned` | `planned` | 7 | 1 | 6 | boolean (value string argName, ref object transform, ref double scaleFactor) |  |
| `SetAngularUnitsArg` | `setter` | `inventory_and_interop` | `usable` | `angular_unit` | `planned` | `planned` | `planned` | `planned` | `planned` | 7 | 4 | 3 | boolean (value string argName, value string angularUnits) |  |
| `SetAsciiFileFormatArg` | `setter` | `inventory_and_interop` | `usable` | `ascii_file_format` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 0 | 3 | boolean (value string argName, value string asciiFileFormat) |  |
| `SetAutoFilterProximitySettingsArg` | `setter` | `inventory_and_interop` | `usable` | `auto_filter_proximity_settings` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 0 | 3 | boolean (value string argName, value double sip, value double eep, value double pip, value double pep, value double rip, value double gt, value int32 spm, value int32 ppm, value int32 rpm, value boolean pp, value boolean apb) |  |
| `SetAxisNameArg` | `setter` | `inventory_and_interop` | `usable` | `axis_identifier` | `planned` | `planned` | `planned` | `planned` | `planned` | 5 | 0 | 5 | boolean (value string argName, value string axisName) |  |
| `SetBSplineFitOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `b_spline_fit_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 0 | 3 | boolean (value string argName, value boolean bFit, value boolean bOpen, value int32 sortMethod, value int32 terminateMethod, value int32 degree, value double terminateLength, value double terminateAvgMultiplier, value int32 numFitCPs, value boolean uniqueCheck, value double uniqueThreshold, value double extension, value boolean useGlobalTessellationOptions, value double maxChordalDeviation, value double maxTrimEdgeAngle) |  |
| `SetBaseColorTypeArg` | `setter` | `inventory_and_interop` | `usable` | `base_color_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string baseColorType) |  |
| `SetBaseMidColorTypeArg` | `setter` | `inventory_and_interop` | `usable` | `base_mid_color_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string baseMidColorType) |  |
| `SetBoolArg` | `setter` | `inventory_and_interop` | `usable` | `logical` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 378 | 40 | 338 | boolean (value string argName, value boolean value) |  |
| `SetChartNameArg` | `setter` | `inventory_and_interop` | `usable` | `chart_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value string chartName) |  |
| `SetChartTypeArg` | `setter` | `inventory_and_interop` | `usable` | `chart_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string chartType) |  |
| `SetCloudNameArg` | `setter` | `inventory_and_interop` | `usable` | `cloud_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string cloudName) |  |
| `SetCloudThinningModeTypeArg` | `setter` | `inventory_only` | `blocked_missing_interop` | `cloud_thinning_mode` | `blocked` | `blocked` | `blocked` | `blocked` | `blocked` | 1 | 0 | 1 | not present in interop | https://github.com/spatialanalyzer/briosa/issues/53 |
| `SetCloudThinningOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `cloud_thinning_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 6 | 0 | 6 | boolean (value string argName, value string thinMode, value int32 pointIncrement, value int32 minNumPts, value int32 maxNumPts) |  |
| `SetColInstIdArg` | `setter` | `inventory_and_interop` | `usable` | `collection_instrument_id` | `planned` | `planned` | `planned` | `planned` | `planned` | 149 | 3 | 146 | boolean (value string argName, value string collectionName, value int32 instId) |  |
| `SetColInstIdRefListArg` | `setter` | `inventory_and_interop` | `usable` | `collection_instrument_id_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 18 | 6 | 12 | boolean (value string argName, ref object colInstIdList) |  |
| `SetColMachineIdArg` | `setter` | `inventory_and_interop` | `usable` | `collection_machine_id` | `planned` | `planned` | `planned` | `planned` | `planned` | 23 | 0 | 23 | boolean (value string argName, value string collectionName, value int32 machineId) |  |
| `SetColVectorGroupNameArg` | `setter` | `inventory_and_interop` | `usable` | `collection_vector_group_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 0 | 3 | boolean (value string argName, value string collectionName, value string vectorGroupName) |  |
| `SetCollectionGroupNameRefListArg` | `setter` | `inventory_and_interop` | `usable` | `collection_group_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, ref object groupNameList) |  |
| `SetCollectionNameArg` | `setter` | `inventory_and_interop` | `usable` | `collection_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 36 | 2 | 34 | boolean (value string argName, value string collectionName) |  |
| `SetCollectionObjectNameArg` | `setter` | `interop_only` | `unobserved_interop` | `collection_object_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, value string collectionName, value string objectName) |  |
| `SetCollectionObjectNameArg2` | `setter` | `inventory_and_interop` | `usable` | `collection_object_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 480 | 9 | 471 | boolean (value string argName, value string collectionName, value string objectName, value string objectType) |  |
| `SetCollectionObjectNameRefListArg` | `setter` | `inventory_and_interop` | `usable` | `collection_object_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 187 | 21 | 166 | boolean (value string argName, ref object objectNameList) |  |
| `SetCollectionVectorGroupNameRefListArg` | `setter` | `inventory_and_interop` | `usable` | `collection_vector_group_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 5 | 1 | 4 | boolean (value string argName, ref object colVectorGrpNameList) |  |
| `SetCollimationBaselineTypeArg` | `setter` | `inventory_and_interop` | `usable` | `collimation_baseline_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string mode) |  |
| `SetCollimationTypeArg` | `setter` | `inventory_and_interop` | `usable` | `collimation_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string mode) |  |
| `SetColorArg` | `setter` | `inventory_and_interop` | `usable` | `rgb_color` | `planned` | `planned` | `planned` | `planned` | `planned` | 13 | 2 | 11 | boolean (value string argName, value byte redColorVal, value byte greenColorVal, value byte blueColorVal) |  |
| `SetColorRangeMethodArg` | `setter` | `inventory_and_interop` | `usable` | `color_range_method` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string colorRangeMethod) |  |
| `SetColorizationOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `colorization_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 7 | 0 | 7 | boolean (value string argName, value string colorRangeMethod, value string baseHighColor, value string baseMidColor, value string baseLowColor, value boolean bDrawTubes, value boolean bDrawArrowheads, value boolean bIndicateValues, value double vectorMagnification, value int32 vectorWidth, value boolean bDrawBlotches, value double blotchSize, value boolean bShowOutOfToleranceOnly, value boolean bShowColorBarInView, value boolean bShowColorBarPercentages, value boolean bShowColorBarFractions, value double highSaturationLimit, value double lowSaturationLimit, value double highTolerance, value double lowTolerance) |  |
| `SetCompTechniqueArg` | `setter` | `inventory_and_interop` | `usable` | `computation_technique` | `planned` | `planned` | `planned` | `planned` | `planned` | 3 | 0 | 3 | boolean (value string argName, value string compTech) |  |
| `SetCoordinateSystemTypeArg` | `setter` | `inventory_and_interop` | `usable` | `coordinate_system_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 7 | 0 | 7 | boolean (value string argName, value string coordSystemType) |  |
| `SetDatasetTypeArg` | `setter` | `inventory_and_interop` | `usable` | `dataset_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string datasetType) |  |
| `SetDegreeOfFreedomArg` | `setter` | `inventory_and_interop` | `usable` | `degree_of_freedom` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string degOfFreedom) |  |
| `SetDistanceUnitsArg` | `setter` | `inventory_and_interop` | `usable` | `distance_unit` | `planned` | `planned` | `planned` | `planned` | `planned` | 6 | 1 | 5 | boolean (value string argName, value string distanceUnits) |  |
| `SetDoubleArg` | `setter` | `inventory_and_interop` | `usable` | `floating_point` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 226 | 34 | 192 | boolean (value string argName, value double value) |  |
| `SetDoubleArrayArg` | `setter` | `inventory_and_interop` | `usable` | `double_array` | `planned` | `planned` | `planned` | `planned` | `planned` | 14 | 8 | 6 | boolean (value string argName, value int32 arraySize, ref object arrayOut) |  |
| `SetDynamicCircleModeArg` | `setter` | `inventory_and_interop` | `usable` | `dynamic_circle_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string mode) |  |
| `SetDynamicEllipseModeArg` | `setter` | `inventory_and_interop` | `usable` | `dynamic_ellipse_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string mode) |  |
| `SetDynamicLineModeArg` | `setter` | `inventory_and_interop` | `usable` | `dynamic_line_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string mode) |  |
| `SetDynamicPlaneModeArg` | `setter` | `inventory_and_interop` | `usable` | `dynamic_plane_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string mode) |  |
| `SetDynamicPointModeArg` | `setter` | `inventory_and_interop` | `usable` | `dynamic_point_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string mode) |  |
| `SetEdgeModeArg` | `setter` | `inventory_and_interop` | `usable` | `edge_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string edgeMode) |  |
| `SetEditTextArg` | `setter` | `inventory_and_interop` | `usable` | `edit_text` | `planned` | `planned` | `planned` | `planned` | `planned` | 14 | 4 | 10 | boolean (value string argName, ref object stringList) |  |
| `SetExportDataDelimeterTypeArg` | `setter` | `inventory_and_interop` | `usable` | `export_data_delimiter_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 5 | 0 | 5 | boolean (value string argName, value string delimeterType) |  |
| `SetExportTargetNameFormatArg` | `setter` | `inventory_and_interop` | `usable` | `export_target_name_format` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value string targetNameFormat) |  |
| `SetExportVectorNameFormatArg` | `setter` | `inventory_and_interop` | `usable` | `export_vector_name_format` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 1 | 1 | boolean (value string argName, value string vectorNameFormat) |  |
| `SetFilePathArg` | `setter` | `inventory_and_interop` | `usable` | `file_reference` | `planned` | `planned` | `planned` | `planned` | `planned` | 120 | 16 | 104 | boolean (value string argName, value string path, value boolean embeddedFile) |  |
| `SetFitConstraintScalarOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `fit_constraint_scalar_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value boolean bUseHigh, value double highTol, value boolean bUseLow, value double lowTol) |  |
| `SetFitDofOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `fit_degree_of_freedom_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value boolean bAllowX, value boolean bAllowY, value boolean bAllowZ, value boolean bAllowRx, value boolean bAllowRy, value boolean bAllowRz, value boolean bRotateAboutCentroid) |  |
| `SetFitMethodArg` | `setter` | `inventory_and_interop` | `usable` | `fit_method` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string fitMethod) |  |
| `SetFontTypeArg` | `setter` | `inventory_and_interop` | `usable` | `font` | `planned` | `planned` | `planned` | `planned` | `planned` | 21 | 11 | 10 | boolean (value string argName, value string fontName, value byte fontSize, value byte redColorVal, value byte greenColorVal, value byte blueColorVal) |  |
| `SetFrameNameArg` | `setter` | `inventory_and_interop` | `usable` | `frame_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 5 | 0 | 5 | boolean (value string argName, value string frameName) |  |
| `SetGeometryTypeArg` | `setter` | `inventory_and_interop` | `usable` | `geometry_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 6 | 0 | 6 | boolean (value string argName, value string geometryType) |  |
| `SetInstIdArg` | `setter` | `interop_only` | `unobserved_interop` | `instrument_id` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, value int32 instId) |  |
| `SetInstTypeNameArg` | `setter` | `inventory_and_interop` | `usable` | `instrument_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string instType) |  |
| `SetIntegerArg` | `setter` | `inventory_and_interop` | `usable` | `whole_number` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 243 | 91 | 152 | boolean (value string argName, value int32 value) |  |
| `SetItemTypeArg` | `setter` | `inventory_only` | `blocked_missing_interop` | `item_type` | `blocked` | `blocked` | `blocked` | `blocked` | `blocked` | 2 | 1 | 1 | not present in interop | https://github.com/spatialanalyzer/briosa/issues/53 |
| `SetLogarithmicFunctionArg` | `setter` | `inventory_and_interop` | `excluded_only` | `logarithmic_function` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, value string logarithmicFunction) |  |
| `SetMPDialogInteractionModeArg` | `setter` | `inventory_and_interop` | `excluded_only` | `mp_dialog_interaction_mode` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, value string mpDialogInteractionMode) |  |
| `SetMPGDTOptionsCheckValidatorTypeArg` | `setter` | `inventory_only` | `blocked_missing_interop` | `gdt_check_validator_type` | `blocked` | `blocked` | `blocked` | `blocked` | `blocked` | 1 | 0 | 1 | not present in interop | https://github.com/spatialanalyzer/briosa/issues/53 |
| `SetMPGDTOptionsDistanceBetweenModeArg` | `setter` | `inventory_only` | `blocked_missing_interop` | `gdt_distance_between_mode` | `blocked` | `blocked` | `blocked` | `blocked` | `blocked` | 1 | 0 | 1 | not present in interop | https://github.com/spatialanalyzer/briosa/issues/53 |
| `SetMPInteractionModeArg` | `setter` | `inventory_and_interop` | `excluded_only` | `mp_interaction_mode` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, value string mpInteractionMode) |  |
| `SetMathOperationArg` | `setter` | `inventory_and_interop` | `excluded_only` | `math_operation` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 2 | 2 | 0 | boolean (value string argName, value string mathOperation) |  |
| `SetMeasuredSideForPlanarOffsetArg` | `setter` | `inventory_and_interop` | `usable` | `measured_side_for_planar_offset` | `planned` | `planned` | `planned` | `planned` | `planned` | 4 | 0 | 4 | boolean (value string argName, value string measSide) |  |
| `SetMeasuredSideForRadialOffsetArg` | `setter` | `inventory_and_interop` | `usable` | `measured_side_for_radial_offset` | `planned` | `planned` | `planned` | `planned` | `planned` | 7 | 0 | 7 | boolean (value string argName, value string measSide) |  |
| `SetMeshOrientationTypeArg` | `setter` | `inventory_only` | `blocked_missing_interop` | `mesh_orientation_type` | `blocked` | `blocked` | `blocked` | `blocked` | `blocked` | 1 | 0 | 1 | not present in interop | https://github.com/spatialanalyzer/briosa/issues/53 |
| `SetMoveDirectionTypeArg` | `setter` | `inventory_and_interop` | `excluded_only` | `move_direction_type` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 2 | 2 | 0 | boolean (value string argName, value string moveDirection) |  |
| `SetNormalDirectionArg` | `setter` | `inventory_and_interop` | `usable` | `normal_direction` | `planned` | `planned` | `planned` | `planned` | `planned` | 4 | 0 | 4 | boolean (value string argName, value string normDir) |  |
| `SetNumComparisonTypeArg` | `setter` | `inventory_and_interop` | `excluded_only` | `numeric_comparison_type` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 6 | 6 | 0 | boolean (value string argName, value string compType) |  |
| `SetObjectNameArg` | `setter` | `interop_only` | `unobserved_interop` | `object_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, value string objectName) |  |
| `SetObjectTypeArg` | `setter` | `inventory_and_interop` | `usable` | `object_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 9 | 3 | 6 | boolean (value string argName, value string objectType) |  |
| `SetOffsetDirectionTypeArg` | `setter` | `inventory_and_interop` | `usable` | `offset_direction_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 4 | 0 | 4 | boolean (value string argName, value string offsetDirType) |  |
| `SetPerimeterNameArg` | `setter` | `interop_only` | `unobserved_interop` | `perimeter_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, value string perimeterName) |  |
| `SetPointDeltaReportOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `point_delta_report_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value string coordSys, value string detailsFormat, value boolean bShowA, value boolean bShowB, value boolean bShowDelta, value boolean bShowMag, value boolean bShowComponent1, value boolean bShowComponent2, value boolean bShowComponent3, value boolean bSortPointNames, value boolean bShowToleranceFields, value boolean bColorizeInToleranceFields) |  |
| `SetPointFilterInputTypeArg` | `setter` | `inventory_and_interop` | `usable` | `point_filter_input_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string pointsInputType) |  |
| `SetPointNameArg` | `setter` | `inventory_and_interop` | `usable` | `point_name` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 97 | 5 | 92 | boolean (value string argName, value string collectionName, value string groupName, value string targetName) |  |
| `SetPointNameRefListArg` | `setter` | `inventory_and_interop` | `usable` | `point_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 57 | 8 | 49 | boolean (value string argName, ref object pointNameList) |  |
| `SetProjectionOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `projection_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 10 | 0 | 10 | boolean (value string argName, value string projectionType, value boolean bIgnoreEdgeProjections, value boolean bOverrideTargetOffsets, value double overrideTargetOffsetsValue, value boolean bAddExtraMaterialThickness, value double extraMaterialThicknessValue) |  |
| `SetRelWeightingModeArg` | `setter` | `inventory_and_interop` | `usable` | `relationship_weighting_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string type) |  |
| `SetRenderModeTypeArg` | `setter` | `inventory_and_interop` | `usable` | `render_mode_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value string renderModeType) |  |
| `SetReportOutputOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `report_output_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value string outputType, value string pathOrEmbeddedName) |  |
| `SetReportPageSettingsArg` | `setter` | `inventory_and_interop` | `usable` | `report_page_orientation` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string pageOrientation) |  |
| `SetReportTypeArg` | `setter` | `interop_only` | `unobserved_interop` | `report_type` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, value string reportType) |  |
| `SetReportViewOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `report_view_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string viewType, value string collectionName, value string calloutName) |  |
| `SetResultArg` | `setter` | `inventory_and_interop` | `excluded_only` | `result_object_name` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 3 | 3 | 0 | boolean (value string argName, value string objectName) |  |
| `SetSAInteractionModeArg` | `setter` | `inventory_and_interop` | `excluded_only` | `sa_interaction_mode` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, value string saInteractionMode) |  |
| `SetSaturationLimitTypeArg` | `setter` | `inventory_and_interop` | `usable` | `saturation_limit_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string satLimitType) |  |
| `SetShowUsmnDialogTypeArg` | `setter` | `inventory_and_interop` | `usable` | `show_usmn_dialog_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value string showType) |  |
| `SetSlotTypeArg` | `setter` | `inventory_and_interop` | `usable` | `slot_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string slotType) |  |
| `SetSphereFitComputationModeArg` | `setter` | `inventory_and_interop` | `usable` | `sphere_fit_computation_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string fitMode) |  |
| `SetStringArg` | `setter` | `inventory_and_interop` | `usable` | `string` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 367 | 152 | 215 | boolean (value string argName, value string value) |  |
| `SetStringRefListArg` | `setter` | `inventory_and_interop` | `usable` | `string_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 16 | 10 | 6 | boolean (value string argName, ref object stringList) |  |
| `SetSurfDissectModeTypeArg` | `setter` | `inventory_and_interop` | `usable` | `surface_dissection_mode_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string dissectModeType) |  |
| `SetSurfaceAnalysisModeArg` | `setter` | `inventory_and_interop` | `usable` | `surface_analysis_mode` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string surfAnalysisMode) |  |
| `SetSystemStringArg` | `setter` | `inventory_and_interop` | `excluded_only` | `system_string` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, value string systemStrName) |  |
| `SetTargetComputationMethodArg` | `setter` | `inventory_and_interop` | `usable` | `target_computation_method` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string compMethod) |  |
| `SetTemperatureUnitsArg` | `setter` | `inventory_and_interop` | `usable` | `temperature_unit` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string tempUnits) |  |
| `SetToleranceScalarOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `tolerance_scalar_options` | `planned` | `planned` | `planned` | `planned` | `planned` | 4 | 0 | 4 | boolean (value string argName, value boolean bUseHigh, value double highTol, value boolean bUseLow, value double lowTol) |  |
| `SetToleranceVectorOptionsArg` | `setter` | `inventory_and_interop` | `usable` | `tolerance_vector_options` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 13 | 0 | 13 | boolean (value string argName, value boolean bUseHighX, value double highTolX, value boolean bUseHighY, value double highTolY, value boolean bUseHighZ, value double highTolZ, value boolean bUseHighM, value double highTolM, value boolean bUseLowX, value double lowTolX, value boolean bUseLowY, value double lowTolY, value boolean bUseLowZ, value double lowTolZ, value boolean bUseLowM, value double lowTolM) |  |
| `SetTransformArg` | `setter` | `inventory_and_interop` | `usable` | `transform` | `planned` | `planned` | `planned` | `planned` | `planned` | 25 | 13 | 12 | boolean (value string argName, ref object transform) |  |
| `SetTranslucencyTypeArg` | `setter` | `inventory_and_interop` | `usable` | `translucency_type` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string type) |  |
| `SetTrigFunctionArg` | `setter` | `inventory_and_interop` | `excluded_only` | `trigonometric_function` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 1 | 1 | 0 | boolean (value string argName, value string trigFunction) |  |
| `SetUdpTransmitSettingsArg` | `setter` | `inventory_and_interop` | `usable` | `udp_transmit_settings` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value boolean bEnabled, value boolean bBroadcast, value string compName, value int32 port) |  |
| `SetUserSummaryInfoFilesArg` | `setter` | `interop_only` | `unobserved_interop` | `user_summary_info_files` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 0 | 0 | 0 | boolean (value string argName, value string leftSummaryFilePath, value string rightSummaryFilePath) |  |
| `SetVectorArg` | `setter` | `inventory_and_interop` | `usable` | `vector3` | `implemented` | `implemented` | `implemented` | `implemented` | `implemented` | 36 | 10 | 26 | boolean (value string argName, value double XValue, value double yValue, value double zValue) |  |
| `SetVectorGroupNameArg` | `setter` | `inventory_and_interop` | `usable` | `vector_group_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 1 | 0 | 1 | boolean (value string argName, value string vectorGrpName) |  |
| `SetVectorNameRefListArg` | `setter` | `inventory_and_interop` | `usable` | `vector_name_list` | `planned` | `planned` | `planned` | `planned` | `planned` | 8 | 4 | 4 | boolean (value string argName, ref object vectorNameList) |  |
| `SetViewNameArg` | `setter` | `inventory_and_interop` | `usable` | `view_name` | `planned` | `planned` | `planned` | `planned` | `planned` | 4 | 0 | 4 | boolean (value string argName, value string viewName) |  |
| `SetWindowStateArg` | `setter` | `inventory_and_interop` | `usable` | `window_state` | `planned` | `planned` | `planned` | `planned` | `planned` | 2 | 0 | 2 | boolean (value string argName, value string windowState) |  |
| `SetWorkbookAddressModeTypeArg` | `setter` | `inventory_and_interop` | `excluded_only` | `workbook_address_mode_type` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 2 | 2 | 0 | boolean (value string argName, value string workbookAddressMode) |  |
| `SetWorldTransformArg` | `setter` | `inventory_and_interop` | `usable` | `world_transform` | `planned` | `planned` | `planned` | `planned` | `planned` | 5 | 2 | 3 | boolean (value string argName, ref object transform, value double scaleFactor) |  |
| `SetWriteModeTypeArg` | `setter` | `inventory_and_interop` | `excluded_only` | `write_mode_type` | `not_required` | `not_required` | `not_required` | `not_required` | `not_required` | 2 | 2 | 0 | boolean (value string argName, value string writeMode) |  |

## Interpretation

- `usable` means only that Briosa has a reviewed exact SDK call shape and semantic family. The command disposition and supported catalog remain the public allowlist.
- `blocked_missing_interop` is not callable: generated sample code named a method that the exact-target interop API does not expose.
- `unobserved_interop` methods are preserved for drift accounting but are not implementation candidates without command evidence.
- `excluded_only` methods require no adapter solely for the current reviewed product scope; mixed-use methods remain usable for their in-scope commands.
