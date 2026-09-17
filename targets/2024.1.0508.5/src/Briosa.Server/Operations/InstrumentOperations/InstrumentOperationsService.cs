using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal sealed class InstrumentOperationsService(OperationExecutor executor)
    : Api.InstrumentOperations.InstrumentOperationsBase
{
    [OperationImplementation("instrument_operations.run_crib_sheet")]
    public override Task<Api.RunCribSheetResult> RunCribSheet(Api.RunCribSheetRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RunCribSheetRequest, Api.RunCribSheetResult>(executor, request, context, "instrument_operations.run_crib_sheet");

    [OperationImplementation("instrument_operations.project_objects")]
    public override Task<Api.ProjectObjectsResult> ProjectObjects(Api.ProjectObjectsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ProjectObjectsRequest, Api.ProjectObjectsResult>(executor, request, context, "instrument_operations.project_objects");

    [OperationImplementation("instrument_operations.stop_projection")]
    public override Task<Api.StopProjectionResult> StopProjection(Api.StopProjectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StopProjectionRequest, Api.StopProjectionResult>(executor, request, context, "instrument_operations.stop_projection");

    [OperationImplementation("instrument_operations.locate_instruments_usmn")]
    public override Task<Api.LocateInstrumentsUsmnResult> LocateInstrumentsUsmn(Api.LocateInstrumentsUsmnRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LocateInstrumentsUsmnRequest, Api.LocateInstrumentsUsmnResult>(executor, request, context, "instrument_operations.locate_instruments_usmn");

    [OperationImplementation("instrument_operations.set_pcmm_instrument_xyz_uncertainties")]
    public override Task<Api.SetPcmmInstrumentXyzUncertaintiesResult> SetPcmmInstrumentXyzUncertainties(Api.SetPcmmInstrumentXyzUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPcmmInstrumentXyzUncertaintiesRequest, Api.SetPcmmInstrumentXyzUncertaintiesResult>(executor, request, context, "instrument_operations.set_pcmm_instrument_xyz_uncertainties");

    [OperationImplementation("instrument_operations.set_wrtl_channel")]
    public override Task<Api.SetWrtlChannelResult> SetWrtlChannel(Api.SetWrtlChannelRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetWrtlChannelRequest, Api.SetWrtlChannelResult>(executor, request, context, "instrument_operations.set_wrtl_channel");

    [OperationImplementation("instrument_operations.get_tracker_edm_theodolite_uncertainties")]
    public override Task<Api.GetTrackerEdmTheodoliteUncertaintiesResult> GetTrackerEdmTheodoliteUncertainties(Api.GetTrackerEdmTheodoliteUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTrackerEdmTheodoliteUncertaintiesRequest, Api.GetTrackerEdmTheodoliteUncertaintiesResult>(executor, request, context, "instrument_operations.get_tracker_edm_theodolite_uncertainties");

    [OperationImplementation("instrument_operations.get_instrument_model")]
    public override Task<Api.GetInstrumentModelResult> GetInstrumentModel(Api.GetInstrumentModelRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentModelRequest, Api.GetInstrumentModelResult>(executor, request, context, "instrument_operations.get_instrument_model");

    [OperationImplementation("instrument_operations.lr_hardware_disconnect")]
    public override Task<Api.LrHardwareDisconnectResult> LrHardwareDisconnect(Api.LrHardwareDisconnectRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrHardwareDisconnectRequest, Api.LrHardwareDisconnectResult>(executor, request, context, "instrument_operations.lr_hardware_disconnect");

    [OperationImplementation("instrument_operations.set_observation_mirror_cube_shot_face")]
    public override Task<Api.SetObservationMirrorCubeShotFaceResult> SetObservationMirrorCubeShotFace(Api.SetObservationMirrorCubeShotFaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObservationMirrorCubeShotFaceRequest, Api.SetObservationMirrorCubeShotFaceResult>(executor, request, context, "instrument_operations.set_observation_mirror_cube_shot_face");

    [OperationImplementation("instrument_operations.construct_mirror_from_two_points")]
    public override Task<Api.ConstructMirrorFromTwoPointsResult> ConstructMirrorFromTwoPoints(Api.ConstructMirrorFromTwoPointsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ConstructMirrorFromTwoPointsRequest, Api.ConstructMirrorFromTwoPointsResult>(executor, request, context, "instrument_operations.construct_mirror_from_two_points");

    [OperationImplementation("instrument_operations.transform_instrument_frame_to_frame")]
    public override Task<Api.TransformInstrumentFrameToFrameResult> TransformInstrumentFrameToFrame(Api.TransformInstrumentFrameToFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TransformInstrumentFrameToFrameRequest, Api.TransformInstrumentFrameToFrameResult>(executor, request, context, "instrument_operations.transform_instrument_frame_to_frame");

    [OperationImplementation("instrument_operations.add_nominal_point_to_tcp_fixture")]
    public override Task<Api.AddNominalPointToTcpFixtureResult> AddNominalPointToTcpFixture(Api.AddNominalPointToTcpFixtureRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddNominalPointToTcpFixtureRequest, Api.AddNominalPointToTcpFixtureResult>(executor, request, context, "instrument_operations.add_nominal_point_to_tcp_fixture");

    [OperationImplementation("instrument_operations.locate_instrument_best_fit_nominal_geometry")]
    public override Task<Api.LocateInstrumentBestFitNominalGeometryResult> LocateInstrumentBestFitNominalGeometry(Api.LocateInstrumentBestFitNominalGeometryRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LocateInstrumentBestFitNominalGeometryRequest, Api.LocateInstrumentBestFitNominalGeometryResult>(executor, request, context, "instrument_operations.locate_instrument_best_fit_nominal_geometry");

    [OperationImplementation("instrument_operations.multi_measurement_initiate")]
    public override Task<Api.MultiMeasurementInitiateResult> MultiMeasurementInitiate(Api.MultiMeasurementInitiateRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MultiMeasurementInitiateRequest, Api.MultiMeasurementInitiateResult>(executor, request, context, "instrument_operations.multi_measurement_initiate");

    [OperationImplementation("instrument_operations.dock_instrument_interface")]
    public override Task<Api.DockInstrumentInterfaceResult> DockInstrumentInterface(Api.DockInstrumentInterfaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DockInstrumentInterfaceRequest, Api.DockInstrumentInterfaceResult>(executor, request, context, "instrument_operations.dock_instrument_interface");

    [OperationImplementation("instrument_operations.lr_apdis_perform_mcm_calibration")]
    public override Task<Api.LrApdisPerformMcmCalibrationResult> LrApdisPerformMcmCalibration(Api.LrApdisPerformMcmCalibrationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrApdisPerformMcmCalibrationRequest, Api.LrApdisPerformMcmCalibrationResult>(executor, request, context, "instrument_operations.lr_apdis_perform_mcm_calibration");

    [OperationImplementation("instrument_operations.get_wrtl_channel_and_status")]
    public override Task<Api.GetWrtlChannelAndStatusResult> GetWrtlChannelAndStatus(Api.GetWrtlChannelAndStatusRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetWrtlChannelAndStatusRequest, Api.GetWrtlChannelAndStatusResult>(executor, request, context, "instrument_operations.get_wrtl_channel_and_status");

    [OperationImplementation("instrument_operations.move_measurement_observation")]
    public override Task<Api.MoveMeasurementObservationResult> MoveMeasurementObservation(Api.MoveMeasurementObservationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveMeasurementObservationRequest, Api.MoveMeasurementObservationResult>(executor, request, context, "instrument_operations.move_measurement_observation");

    [OperationImplementation("instrument_operations.synchronized_measurement_master_slave")]
    public override Task<Api.SynchronizedMeasurementMasterSlaveResult> SynchronizedMeasurementMasterSlave(Api.SynchronizedMeasurementMasterSlaveRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SynchronizedMeasurementMasterSlaveRequest, Api.SynchronizedMeasurementMasterSlaveResult>(executor, request, context, "instrument_operations.synchronized_measurement_master_slave");

    [OperationImplementation("instrument_operations.lr_apdis_activate_mcm_calibration")]
    public override Task<Api.LrApdisActivateMcmCalibrationResult> LrApdisActivateMcmCalibration(Api.LrApdisActivateMcmCalibrationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrApdisActivateMcmCalibrationRequest, Api.LrApdisActivateMcmCalibrationResult>(executor, request, context, "instrument_operations.lr_apdis_activate_mcm_calibration");

    [OperationImplementation("instrument_operations.delete_measurements")]
    public override Task<Api.DeleteMeasurementsResult> DeleteMeasurements(Api.DeleteMeasurementsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteMeasurementsRequest, Api.DeleteMeasurementsResult>(executor, request, context, "instrument_operations.delete_measurements");

    [OperationImplementation("instrument_operations.watch_instrument")]
    public override Task<Api.WatchInstrumentResult> WatchInstrument(Api.WatchInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WatchInstrumentRequest, Api.WatchInstrumentResult>(executor, request, context, "instrument_operations.watch_instrument");

    [OperationImplementation("instrument_operations.set_alignment_projector")]
    public override Task<Api.SetAlignmentProjectorResult> SetAlignmentProjector(Api.SetAlignmentProjectorRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetAlignmentProjectorRequest, Api.SetAlignmentProjectorResult>(executor, request, context, "instrument_operations.set_alignment_projector");

    [OperationImplementation("instrument_operations.construct_tcp_fixture")]
    public override Task<Api.ConstructTcpFixtureResult> ConstructTcpFixture(Api.ConstructTcpFixtureRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ConstructTcpFixtureRequest, Api.ConstructTcpFixtureResult>(executor, request, context, "instrument_operations.construct_tcp_fixture");

    [OperationImplementation("instrument_operations.stop_instrument_interface")]
    public override Task<Api.StopInstrumentInterfaceResult> StopInstrumentInterface(Api.StopInstrumentInterfaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StopInstrumentInterfaceRequest, Api.StopInstrumentInterfaceResult>(executor, request, context, "instrument_operations.stop_instrument_interface");

    [OperationImplementation("instrument_operations.rename_instrument")]
    public override Task<Api.RenameInstrumentResult> RenameInstrument(Api.RenameInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RenameInstrumentRequest, Api.RenameInstrumentResult>(executor, request, context, "instrument_operations.rename_instrument");

    [OperationImplementation("instrument_operations.auto_measure_specified_geometry")]
    public override Task<Api.AutoMeasureSpecifiedGeometryResult> AutoMeasureSpecifiedGeometry(Api.AutoMeasureSpecifiedGeometryRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoMeasureSpecifiedGeometryRequest, Api.AutoMeasureSpecifiedGeometryResult>(executor, request, context, "instrument_operations.auto_measure_specified_geometry");

    [OperationImplementation("instrument_operations.set_instrument_group_and_target")]
    public override Task<Api.SetInstrumentGroupAndTargetResult> SetInstrumentGroupAndTarget(Api.SetInstrumentGroupAndTargetRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentGroupAndTargetRequest, Api.SetInstrumentGroupAndTargetResult>(executor, request, context, "instrument_operations.set_instrument_group_and_target");

    [OperationImplementation("instrument_operations.auto_measure_points")]
    public override Task<Api.AutoMeasurePointsResult> AutoMeasurePoints(Api.AutoMeasurePointsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoMeasurePointsRequest, Api.AutoMeasurePointsResult>(executor, request, context, "instrument_operations.auto_measure_points");

    [OperationImplementation("instrument_operations.start_gdt_inspection")]
    public override Task<Api.StartGdtInspectionResult> StartGdtInspection(Api.StartGdtInspectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartGdtInspectionRequest, Api.StartGdtInspectionResult>(executor, request, context, "instrument_operations.start_gdt_inspection");

    [OperationImplementation("instrument_operations.set_instrument_targeting")]
    public override Task<Api.SetInstrumentTargetingResult> SetInstrumentTargeting(Api.SetInstrumentTargetingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentTargetingRequest, Api.SetInstrumentTargetingResult>(executor, request, context, "instrument_operations.set_instrument_targeting");

    [OperationImplementation("instrument_operations.compute_cte_scale_factor")]
    public override Task<Api.ComputeCteScaleFactorResult> ComputeCteScaleFactor(Api.ComputeCteScaleFactorRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ComputeCteScaleFactorRequest, Api.ComputeCteScaleFactorResult>(executor, request, context, "instrument_operations.compute_cte_scale_factor");

    [OperationImplementation("instrument_operations.watch_point_to_objects")]
    public override Task<Api.WatchPointToObjectsResult> WatchPointToObjects(Api.WatchPointToObjectsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WatchPointToObjectsRequest, Api.WatchPointToObjectsResult>(executor, request, context, "instrument_operations.watch_point_to_objects");

    [OperationImplementation("instrument_operations.enable_disable_frame_set_scan_mode_by_instrument")]
    public override Task<Api.EnableDisableFrameSetScanModeByInstrumentResult> EnableDisableFrameSetScanModeByInstrument(Api.EnableDisableFrameSetScanModeByInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EnableDisableFrameSetScanModeByInstrumentRequest, Api.EnableDisableFrameSetScanModeByInstrumentResult>(executor, request, context, "instrument_operations.enable_disable_frame_set_scan_mode_by_instrument");

    [OperationImplementation("instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_world")]
    public override Task<Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult> SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld(Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest, Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult>(executor, request, context, "instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_world");

    [OperationImplementation("instrument_operations.delete_instrument")]
    public override Task<Api.DeleteInstrumentResult> DeleteInstrument(Api.DeleteInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteInstrumentRequest, Api.DeleteInstrumentResult>(executor, request, context, "instrument_operations.delete_instrument");

    [OperationImplementation("instrument_operations.combine_point_groups")]
    public override Task<Api.CombinePointGroupsResult> CombinePointGroups(Api.CombinePointGroupsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CombinePointGroupsRequest, Api.CombinePointGroupsResult>(executor, request, context, "instrument_operations.combine_point_groups");

    [OperationImplementation("instrument_operations.set_cloud_viewer_filter")]
    public override Task<Api.SetCloudViewerFilterResult> SetCloudViewerFilter(Api.SetCloudViewerFilterRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCloudViewerFilterRequest, Api.SetCloudViewerFilterResult>(executor, request, context, "instrument_operations.set_cloud_viewer_filter");

    [OperationImplementation("instrument_operations.transform_instrument_by_delta")]
    public override Task<Api.TransformInstrumentByDeltaResult> TransformInstrumentByDelta(Api.TransformInstrumentByDeltaRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TransformInstrumentByDeltaRequest, Api.TransformInstrumentByDeltaResult>(executor, request, context, "instrument_operations.transform_instrument_by_delta");

    [OperationImplementation("instrument_operations.make_surface_face_list_from_point_proximity")]
    public override Task<Api.MakeSurfaceFaceListFromPointProximityResult> MakeSurfaceFaceListFromPointProximity(Api.MakeSurfaceFaceListFromPointProximityRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeSurfaceFaceListFromPointProximityRequest, Api.MakeSurfaceFaceListFromPointProximityResult>(executor, request, context, "instrument_operations.make_surface_face_list_from_point_proximity");

    [OperationImplementation("instrument_operations.get_observation_info")]
    public override Task<Api.GetObservationInfoResult> GetObservationInfo(Api.GetObservationInfoRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetObservationInfoRequest, Api.GetObservationInfoResult>(executor, request, context, "instrument_operations.get_observation_info");

    [OperationImplementation("instrument_operations.get_instrument_interface_response_timeout")]
    public override Task<Api.GetInstrumentInterfaceResponseTimeoutResult> GetInstrumentInterfaceResponseTimeout(Api.GetInstrumentInterfaceResponseTimeoutRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentInterfaceResponseTimeoutRequest, Api.GetInstrumentInterfaceResponseTimeoutResult>(executor, request, context, "instrument_operations.get_instrument_interface_response_timeout");

    [OperationImplementation("instrument_operations.set_instrument_axes")]
    public override Task<Api.SetInstrumentAxesResult> SetInstrumentAxes(Api.SetInstrumentAxesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentAxesRequest, Api.SetInstrumentAxesResult>(executor, request, context, "instrument_operations.set_instrument_axes");

    [OperationImplementation("instrument_operations.set_remeasure_failed_checks_only")]
    public override Task<Api.SetRemeasureFailedChecksOnlyResult> SetRemeasureFailedChecksOnly(Api.SetRemeasureFailedChecksOnlyRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRemeasureFailedChecksOnlyRequest, Api.SetRemeasureFailedChecksOnlyResult>(executor, request, context, "instrument_operations.set_remeasure_failed_checks_only");

    [OperationImplementation("instrument_operations.get_instrument_target_status")]
    public override Task<Api.GetInstrumentTargetStatusResult> GetInstrumentTargetStatus(Api.GetInstrumentTargetStatusRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentTargetStatusRequest, Api.GetInstrumentTargetStatusResult>(executor, request, context, "instrument_operations.get_instrument_target_status");

    [OperationImplementation("instrument_operations.lr_self_test_linearization")]
    public override Task<Api.LrSelfTestLinearizationResult> LrSelfTestLinearization(Api.LrSelfTestLinearizationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrSelfTestLinearizationRequest, Api.LrSelfTestLinearizationResult>(executor, request, context, "instrument_operations.lr_self_test_linearization");

    [OperationImplementation("instrument_operations.align_cloud_to_cad")]
    public override Task<Api.AlignCloudToCadResult> AlignCloudToCad(Api.AlignCloudToCadRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AlignCloudToCadRequest, Api.AlignCloudToCadResult>(executor, request, context, "instrument_operations.align_cloud_to_cad");

    [OperationImplementation("instrument_operations.watch_point_to_point")]
    public override Task<Api.WatchPointToPointResult> WatchPointToPoint(Api.WatchPointToPointRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WatchPointToPointRequest, Api.WatchPointToPointResult>(executor, request, context, "instrument_operations.watch_point_to_point");

    [OperationImplementation("instrument_operations.get_current_trapping_status")]
    public override Task<Api.GetCurrentTrappingStatusResult> GetCurrentTrappingStatus(Api.GetCurrentTrappingStatusRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCurrentTrappingStatusRequest, Api.GetCurrentTrappingStatusResult>(executor, request, context, "instrument_operations.get_current_trapping_status");

    [OperationImplementation("instrument_operations.lr_verify_hardware_connection")]
    public override Task<Api.LrVerifyHardwareConnectionResult> LrVerifyHardwareConnection(Api.LrVerifyHardwareConnectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrVerifyHardwareConnectionRequest, Api.LrVerifyHardwareConnectionResult>(executor, request, context, "instrument_operations.lr_verify_hardware_connection");

    [OperationImplementation("instrument_operations.set_instrument_interface_response_timeout")]
    public override Task<Api.SetInstrumentInterfaceResponseTimeoutResult> SetInstrumentInterfaceResponseTimeout(Api.SetInstrumentInterfaceResponseTimeoutRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentInterfaceResponseTimeoutRequest, Api.SetInstrumentInterfaceResponseTimeoutResult>(executor, request, context, "instrument_operations.set_instrument_interface_response_timeout");

    [OperationImplementation("instrument_operations.load_instrument_configuration")]
    public override Task<Api.LoadInstrumentConfigurationResult> LoadInstrumentConfiguration(Api.LoadInstrumentConfigurationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LoadInstrumentConfigurationRequest, Api.LoadInstrumentConfigurationResult>(executor, request, context, "instrument_operations.load_instrument_configuration");

    [OperationImplementation("instrument_operations.point_at_target")]
    public override Task<Api.PointAtTargetResult> PointAtTarget(Api.PointAtTargetRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PointAtTargetRequest, Api.PointAtTargetResult>(executor, request, context, "instrument_operations.point_at_target");

    [OperationImplementation("instrument_operations.move_objects_in_6d_using_instrument_updates")]
    public override Task<Api.MoveObjectsIn6dUsingInstrumentUpdatesResult> MoveObjectsIn6dUsingInstrumentUpdates(Api.MoveObjectsIn6dUsingInstrumentUpdatesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveObjectsIn6dUsingInstrumentUpdatesRequest, Api.MoveObjectsIn6dUsingInstrumentUpdatesResult>(executor, request, context, "instrument_operations.move_objects_in_6d_using_instrument_updates");

    [OperationImplementation("instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world")]
    public override Task<Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult> GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld(Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest, Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult>(executor, request, context, "instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world");

    [OperationImplementation("instrument_operations.auto_correspond_closest_point")]
    public override Task<Api.AutoCorrespondClosestPointResult> AutoCorrespondClosestPoint(Api.AutoCorrespondClosestPointRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoCorrespondClosestPointRequest, Api.AutoCorrespondClosestPointResult>(executor, request, context, "instrument_operations.auto_correspond_closest_point");

    [OperationImplementation("instrument_operations.fabricate_observations")]
    public override Task<Api.FabricateObservationsResult> FabricateObservations(Api.FabricateObservationsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FabricateObservationsRequest, Api.FabricateObservationsResult>(executor, request, context, "instrument_operations.fabricate_observations");

    [OperationImplementation("instrument_operations.get_instrument_weather_setting")]
    public override Task<Api.GetInstrumentWeatherSettingResult> GetInstrumentWeatherSetting(Api.GetInstrumentWeatherSettingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentWeatherSettingRequest, Api.GetInstrumentWeatherSettingResult>(executor, request, context, "instrument_operations.get_instrument_weather_setting");

    [OperationImplementation("instrument_operations.get_current_instrument_position_update")]
    public override Task<Api.GetCurrentInstrumentPositionUpdateResult> GetCurrentInstrumentPositionUpdate(Api.GetCurrentInstrumentPositionUpdateRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCurrentInstrumentPositionUpdateRequest, Api.GetCurrentInstrumentPositionUpdateResult>(executor, request, context, "instrument_operations.get_current_instrument_position_update");

    [OperationImplementation("instrument_operations.collimation")]
    public override Task<Api.CollimationResult> Collimation(Api.CollimationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CollimationRequest, Api.CollimationResult>(executor, request, context, "instrument_operations.collimation");

    [OperationImplementation("instrument_operations.align_two_targets_with_axis_wcf_x")]
    public override Task<Api.AlignTwoTargetsWithAxisWcfXResult> AlignTwoTargetsWithAxisWcfX(Api.AlignTwoTargetsWithAxisWcfXRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AlignTwoTargetsWithAxisWcfXRequest, Api.AlignTwoTargetsWithAxisWcfXResult>(executor, request, context, "instrument_operations.align_two_targets_with_axis_wcf_x");

    [OperationImplementation("instrument_operations.construct_measured_point_uncertainty_ellipsoids")]
    public override Task<Api.ConstructMeasuredPointUncertaintyEllipsoidsResult> ConstructMeasuredPointUncertaintyEllipsoids(Api.ConstructMeasuredPointUncertaintyEllipsoidsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ConstructMeasuredPointUncertaintyEllipsoidsRequest, Api.ConstructMeasuredPointUncertaintyEllipsoidsResult>(executor, request, context, "instrument_operations.construct_measured_point_uncertainty_ellipsoids");

    [OperationImplementation("instrument_operations.set_probe_offset_frame_online")]
    public override Task<Api.SetProbeOffsetFrameOnlineResult> SetProbeOffsetFrameOnline(Api.SetProbeOffsetFrameOnlineRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetProbeOffsetFrameOnlineRequest, Api.SetProbeOffsetFrameOnlineResult>(executor, request, context, "instrument_operations.set_probe_offset_frame_online");

    [OperationImplementation("instrument_operations.delete_measurement_observation")]
    public override Task<Api.DeleteMeasurementObservationResult> DeleteMeasurementObservation(Api.DeleteMeasurementObservationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteMeasurementObservationRequest, Api.DeleteMeasurementObservationResult>(executor, request, context, "instrument_operations.delete_measurement_observation");

    [OperationImplementation("instrument_operations.watch_point_to_point_with_view_zooming")]
    public override Task<Api.WatchPointToPointWithViewZoomingResult> WatchPointToPointWithViewZooming(Api.WatchPointToPointWithViewZoomingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WatchPointToPointWithViewZoomingRequest, Api.WatchPointToPointWithViewZoomingResult>(executor, request, context, "instrument_operations.watch_point_to_point_with_view_zooming");

    [OperationImplementation("instrument_operations.get_pcmm_instrument_xyz_uncertainties")]
    public override Task<Api.GetPcmmInstrumentXyzUncertaintiesResult> GetPcmmInstrumentXyzUncertainties(Api.GetPcmmInstrumentXyzUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPcmmInstrumentXyzUncertaintiesRequest, Api.GetPcmmInstrumentXyzUncertaintiesResult>(executor, request, context, "instrument_operations.get_pcmm_instrument_xyz_uncertainties");

    [OperationImplementation("instrument_operations.measure_existing_single_point")]
    public override Task<Api.MeasureExistingSinglePointResult> MeasureExistingSinglePoint(Api.MeasureExistingSinglePointRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MeasureExistingSinglePointRequest, Api.MeasureExistingSinglePointResult>(executor, request, context, "instrument_operations.measure_existing_single_point");

    [OperationImplementation("instrument_operations.verify_instrument_connection")]
    public override Task<Api.VerifyInstrumentConnectionResult> VerifyInstrumentConnection(Api.VerifyInstrumentConnectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.VerifyInstrumentConnectionRequest, Api.VerifyInstrumentConnectionResult>(executor, request, context, "instrument_operations.verify_instrument_connection");

    [OperationImplementation("instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_base")]
    public override Task<Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseResult> SetInstrumentBaseUncertaintyCovarianceMatrixWrtBase(Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseRequest, Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseResult>(executor, request, context, "instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_base");

    [OperationImplementation("instrument_operations.construct_mirror_from_plane")]
    public override Task<Api.ConstructMirrorFromPlaneResult> ConstructMirrorFromPlane(Api.ConstructMirrorFromPlaneRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ConstructMirrorFromPlaneRequest, Api.ConstructMirrorFromPlaneResult>(executor, request, context, "instrument_operations.construct_mirror_from_plane");

    [OperationImplementation("instrument_operations.quick_align")]
    public override Task<Api.QuickAlignResult> QuickAlign(Api.QuickAlignRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QuickAlignRequest, Api.QuickAlignResult>(executor, request, context, "instrument_operations.quick_align");

    [OperationImplementation("instrument_operations.auto_measure_batch_of_features")]
    public override Task<Api.AutoMeasureBatchOfFeaturesResult> AutoMeasureBatchOfFeatures(Api.AutoMeasureBatchOfFeaturesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoMeasureBatchOfFeaturesRequest, Api.AutoMeasureBatchOfFeaturesResult>(executor, request, context, "instrument_operations.auto_measure_batch_of_features");

    [OperationImplementation("instrument_operations.wait_for_trapping_to_complete")]
    public override Task<Api.WaitForTrappingToCompleteResult> WaitForTrappingToComplete(Api.WaitForTrappingToCompleteRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WaitForTrappingToCompleteRequest, Api.WaitForTrappingToCompleteResult>(executor, request, context, "instrument_operations.wait_for_trapping_to_complete");

    [OperationImplementation("instrument_operations.align_laser_projector")]
    public override Task<Api.AlignLaserProjectorResult> AlignLaserProjector(Api.AlignLaserProjectorRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AlignLaserProjectorRequest, Api.AlignLaserProjectorResult>(executor, request, context, "instrument_operations.align_laser_projector");

    [OperationImplementation("instrument_operations.create_new_dynamic_reference")]
    public override Task<Api.CreateNewDynamicReferenceResult> CreateNewDynamicReference(Api.CreateNewDynamicReferenceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreateNewDynamicReferenceRequest, Api.CreateNewDynamicReferenceResult>(executor, request, context, "instrument_operations.create_new_dynamic_reference");

    [OperationImplementation("instrument_operations.get_instrument_id_from_name")]
    public override Task<Api.GetInstrumentIdFromNameResult> GetInstrumentIdFromName(Api.GetInstrumentIdFromNameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentIdFromNameRequest, Api.GetInstrumentIdFromNameResult>(executor, request, context, "instrument_operations.get_instrument_id_from_name");

    [OperationImplementation("instrument_operations.make_collection_object_name_ref_list_from_objects_associated_with_instruments")]
    public override Task<Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsResult> MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstruments(Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsRequest, Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsResult>(executor, request, context, "instrument_operations.make_collection_object_name_ref_list_from_objects_associated_with_instruments");

    [OperationImplementation("instrument_operations.lr_hardware_connect")]
    public override Task<Api.LrHardwareConnectResult> LrHardwareConnect(Api.LrHardwareConnectRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrHardwareConnectRequest, Api.LrHardwareConnectResult>(executor, request, context, "instrument_operations.lr_hardware_connect");

    [OperationImplementation("instrument_operations.get_instrument_scale_factor")]
    public override Task<Api.GetInstrumentScaleFactorResult> GetInstrumentScaleFactor(Api.GetInstrumentScaleFactorRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentScaleFactorRequest, Api.GetInstrumentScaleFactorResult>(executor, request, context, "instrument_operations.get_instrument_scale_factor");

    [OperationImplementation("instrument_operations.build_target")]
    public override Task<Api.BuildTargetResult> BuildTarget(Api.BuildTargetRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.BuildTargetRequest, Api.BuildTargetResult>(executor, request, context, "instrument_operations.build_target");

    [OperationImplementation("instrument_operations.disassociate_objects_from_instrument")]
    public override Task<Api.DisassociateObjectsFromInstrumentResult> DisassociateObjectsFromInstrument(Api.DisassociateObjectsFromInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DisassociateObjectsFromInstrumentRequest, Api.DisassociateObjectsFromInstrumentResult>(executor, request, context, "instrument_operations.disassociate_objects_from_instrument");

    [OperationImplementation("instrument_operations.load_cloud_viewer_point_cloud_file")]
    public override Task<Api.LoadCloudViewerPointCloudFileResult> LoadCloudViewerPointCloudFile(Api.LoadCloudViewerPointCloudFileRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LoadCloudViewerPointCloudFileRequest, Api.LoadCloudViewerPointCloudFileResult>(executor, request, context, "instrument_operations.load_cloud_viewer_point_cloud_file");

    [OperationImplementation("instrument_operations.measure_nominal_feature")]
    public override Task<Api.MeasureNominalFeatureResult> MeasureNominalFeature(Api.MeasureNominalFeatureRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MeasureNominalFeatureRequest, Api.MeasureNominalFeatureResult>(executor, request, context, "instrument_operations.measure_nominal_feature");

    [OperationImplementation("instrument_operations.auto_correspond_with_proximity_trigger")]
    public override Task<Api.AutoCorrespondWithProximityTriggerResult> AutoCorrespondWithProximityTrigger(Api.AutoCorrespondWithProximityTriggerRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoCorrespondWithProximityTriggerRequest, Api.AutoCorrespondWithProximityTriggerResult>(executor, request, context, "instrument_operations.auto_correspond_with_proximity_trigger");

    [OperationImplementation("instrument_operations.edge_scan_measurement")]
    public override Task<Api.EdgeScanMeasurementResult> EdgeScanMeasurement(Api.EdgeScanMeasurementRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EdgeScanMeasurementRequest, Api.EdgeScanMeasurementResult>(executor, request, context, "instrument_operations.edge_scan_measurement");

    [OperationImplementation("instrument_operations.lr_set_red_laser_intensity")]
    public override Task<Api.LrSetRedLaserIntensityResult> LrSetRedLaserIntensity(Api.LrSetRedLaserIntensityRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrSetRedLaserIntensityRequest, Api.LrSetRedLaserIntensityResult>(executor, request, context, "instrument_operations.lr_set_red_laser_intensity");

    [OperationImplementation("instrument_operations.set_tracker_edm_theodolite_uncertainties")]
    public override Task<Api.SetTrackerEdmTheodoliteUncertaintiesResult> SetTrackerEdmTheodoliteUncertainties(Api.SetTrackerEdmTheodoliteUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTrackerEdmTheodoliteUncertaintiesRequest, Api.SetTrackerEdmTheodoliteUncertaintiesResult>(executor, request, context, "instrument_operations.set_tracker_edm_theodolite_uncertainties");

    [OperationImplementation("instrument_operations.enable_disable_frame_set_scan_mode_all_instruments")]
    public override Task<Api.EnableDisableFrameSetScanModeAllInstrumentsResult> EnableDisableFrameSetScanModeAllInstruments(Api.EnableDisableFrameSetScanModeAllInstrumentsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EnableDisableFrameSetScanModeAllInstrumentsRequest, Api.EnableDisableFrameSetScanModeAllInstrumentsResult>(executor, request, context, "instrument_operations.enable_disable_frame_set_scan_mode_all_instruments");

    [OperationImplementation("instrument_operations.set_target_computation_options")]
    public override Task<Api.SetTargetComputationOptionsResult> SetTargetComputationOptions(Api.SetTargetComputationOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTargetComputationOptionsRequest, Api.SetTargetComputationOptionsResult>(executor, request, context, "instrument_operations.set_target_computation_options");

    [OperationImplementation("instrument_operations.measure")]
    public override Task<Api.MeasureResult> Measure(Api.MeasureRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MeasureRequest, Api.MeasureResult>(executor, request, context, "instrument_operations.measure");

    [OperationImplementation("instrument_operations.set_observation_collimation_shot_options")]
    public override Task<Api.SetObservationCollimationShotOptionsResult> SetObservationCollimationShotOptions(Api.SetObservationCollimationShotOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObservationCollimationShotOptionsRequest, Api.SetObservationCollimationShotOptionsResult>(executor, request, context, "instrument_operations.set_observation_collimation_shot_options");

    [OperationImplementation("instrument_operations.save_instrument_configuration")]
    public override Task<Api.SaveInstrumentConfigurationResult> SaveInstrumentConfiguration(Api.SaveInstrumentConfigurationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SaveInstrumentConfigurationRequest, Api.SaveInstrumentConfigurationResult>(executor, request, context, "instrument_operations.save_instrument_configuration");

    [OperationImplementation("instrument_operations.export_instrument_history_to_xml_file")]
    public override Task<Api.ExportInstrumentHistoryToXmlFileResult> ExportInstrumentHistoryToXmlFile(Api.ExportInstrumentHistoryToXmlFileRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportInstrumentHistoryToXmlFileRequest, Api.ExportInstrumentHistoryToXmlFileResult>(executor, request, context, "instrument_operations.export_instrument_history_to_xml_file");

    [OperationImplementation("instrument_operations.lr_self_test_flip_test")]
    public override Task<Api.LrSelfTestFlipTestResult> LrSelfTestFlipTest(Api.LrSelfTestFlipTestRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrSelfTestFlipTestRequest, Api.LrSelfTestFlipTestResult>(executor, request, context, "instrument_operations.lr_self_test_flip_test");

    [OperationImplementation("instrument_operations.save_cloud_viewer_point_cloud_file")]
    public override Task<Api.SaveCloudViewerPointCloudFileResult> SaveCloudViewerPointCloudFile(Api.SaveCloudViewerPointCloudFileRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SaveCloudViewerPointCloudFileRequest, Api.SaveCloudViewerPointCloudFileResult>(executor, request, context, "instrument_operations.save_cloud_viewer_point_cloud_file");

    [OperationImplementation("instrument_operations.clear_cloud_viewer")]
    public override Task<Api.ClearCloudViewerResult> ClearCloudViewer(Api.ClearCloudViewerRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ClearCloudViewerRequest, Api.ClearCloudViewerResult>(executor, request, context, "instrument_operations.clear_cloud_viewer");

    [OperationImplementation("instrument_operations.track_tape_measurement")]
    public override Task<Api.TrackTapeMeasurementResult> TrackTapeMeasurement(Api.TrackTapeMeasurementRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TrackTapeMeasurementRequest, Api.TrackTapeMeasurementResult>(executor, request, context, "instrument_operations.track_tape_measurement");

    [OperationImplementation("instrument_operations.get_obscured_points_from_instrument")]
    public override Task<Api.GetObscuredPointsFromInstrumentResult> GetObscuredPointsFromInstrument(Api.GetObscuredPointsFromInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetObscuredPointsFromInstrumentRequest, Api.GetObscuredPointsFromInstrumentResult>(executor, request, context, "instrument_operations.get_obscured_points_from_instrument");

    [OperationImplementation("instrument_operations.associate_objects_with_instrument")]
    public override Task<Api.AssociateObjectsWithInstrumentResult> AssociateObjectsWithInstrument(Api.AssociateObjectsWithInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AssociateObjectsWithInstrumentRequest, Api.AssociateObjectsWithInstrumentResult>(executor, request, context, "instrument_operations.associate_objects_with_instrument");

    [OperationImplementation("instrument_operations.activate_deactivate_instrument_toolbar")]
    public override Task<Api.ActivateDeactivateInstrumentToolbarResult> ActivateDeactivateInstrumentToolbar(Api.ActivateDeactivateInstrumentToolbarRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ActivateDeactivateInstrumentToolbarRequest, Api.ActivateDeactivateInstrumentToolbarResult>(executor, request, context, "instrument_operations.activate_deactivate_instrument_toolbar");

    [OperationImplementation("instrument_operations.jump_instrument_to_new_location")]
    public override Task<Api.JumpInstrumentToNewLocationResult> JumpInstrumentToNewLocation(Api.JumpInstrumentToNewLocationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.JumpInstrumentToNewLocationRequest, Api.JumpInstrumentToNewLocationResult>(executor, request, context, "instrument_operations.jump_instrument_to_new_location");

    [OperationImplementation("instrument_operations.auto_measure_surface_vector_intersections")]
    public override Task<Api.AutoMeasureSurfaceVectorIntersectionsResult> AutoMeasureSurfaceVectorIntersections(Api.AutoMeasureSurfaceVectorIntersectionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoMeasureSurfaceVectorIntersectionsRequest, Api.AutoMeasureSurfaceVectorIntersectionsResult>(executor, request, context, "instrument_operations.auto_measure_surface_vector_intersections");

    [OperationImplementation("instrument_operations.start_gdt_inspection_rehearse")]
    public override Task<Api.StartGdtInspectionRehearseResult> StartGdtInspectionRehearse(Api.StartGdtInspectionRehearseRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartGdtInspectionRehearseRequest, Api.StartGdtInspectionRehearseResult>(executor, request, context, "instrument_operations.start_gdt_inspection_rehearse");

    [OperationImplementation("instrument_operations.configure_and_measure")]
    public override Task<Api.ConfigureAndMeasureResult> ConfigureAndMeasure(Api.ConfigureAndMeasureRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ConfigureAndMeasureRequest, Api.ConfigureAndMeasureResult>(executor, request, context, "instrument_operations.configure_and_measure");

    [OperationImplementation("instrument_operations.get_instrument_targets_and_mode_profiles")]
    public override Task<Api.GetInstrumentTargetsAndModeProfilesResult> GetInstrumentTargetsAndModeProfiles(Api.GetInstrumentTargetsAndModeProfilesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentTargetsAndModeProfilesRequest, Api.GetInstrumentTargetsAndModeProfilesResult>(executor, request, context, "instrument_operations.get_instrument_targets_and_mode_profiles");

    [OperationImplementation("instrument_operations.move_instrument_to_another_collection")]
    public override Task<Api.MoveInstrumentToAnotherCollectionResult> MoveInstrumentToAnotherCollection(Api.MoveInstrumentToAnotherCollectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveInstrumentToAnotherCollectionRequest, Api.MoveInstrumentToAnotherCollectionResult>(executor, request, context, "instrument_operations.move_instrument_to_another_collection");

    [OperationImplementation("instrument_operations.measure_single_point_here")]
    public override Task<Api.MeasureSinglePointHereResult> MeasureSinglePointHere(Api.MeasureSinglePointHereRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MeasureSinglePointHereRequest, Api.MeasureSinglePointHereResult>(executor, request, context, "instrument_operations.measure_single_point_here");

    [OperationImplementation("instrument_operations.issue_instrument_actuator_command")]
    public override Task<Api.IssueInstrumentActuatorCommandResult> IssueInstrumentActuatorCommand(Api.IssueInstrumentActuatorCommandRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.IssueInstrumentActuatorCommandRequest, Api.IssueInstrumentActuatorCommandResult>(executor, request, context, "instrument_operations.issue_instrument_actuator_command");

    [OperationImplementation("instrument_operations.add_new_instrument")]
    public override Task<Api.AddNewInstrumentResult> AddNewInstrument(Api.AddNewInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddNewInstrumentRequest, Api.AddNewInstrumentResult>(executor, request, context, "instrument_operations.add_new_instrument");

    [OperationImplementation("instrument_operations.watch_closest_point")]
    public override Task<Api.WatchClosestPointResult> WatchClosestPoint(Api.WatchClosestPointRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WatchClosestPointRequest, Api.WatchClosestPointResult>(executor, request, context, "instrument_operations.watch_closest_point");

    [OperationImplementation("instrument_operations.transform_multiple_instruments_by_delta")]
    public override Task<Api.TransformMultipleInstrumentsByDeltaResult> TransformMultipleInstrumentsByDelta(Api.TransformMultipleInstrumentsByDeltaRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TransformMultipleInstrumentsByDeltaRequest, Api.TransformMultipleInstrumentsByDeltaResult>(executor, request, context, "instrument_operations.transform_multiple_instruments_by_delta");

    [OperationImplementation("instrument_operations.guide_objects_in_6d_based_on_point_measurements")]
    public override Task<Api.GuideObjectsIn6dBasedOnPointMeasurementsResult> GuideObjectsIn6dBasedOnPointMeasurements(Api.GuideObjectsIn6dBasedOnPointMeasurementsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GuideObjectsIn6dBasedOnPointMeasurementsRequest, Api.GuideObjectsIn6dBasedOnPointMeasurementsResult>(executor, request, context, "instrument_operations.guide_objects_in_6d_based_on_point_measurements");

    [OperationImplementation("instrument_operations.locate_instrument_ref_tie_in")]
    public override Task<Api.LocateInstrumentRefTieInResult> LocateInstrumentRefTieIn(Api.LocateInstrumentRefTieInRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LocateInstrumentRefTieInRequest, Api.LocateInstrumentRefTieInResult>(executor, request, context, "instrument_operations.locate_instrument_ref_tie_in");

    [OperationImplementation("instrument_operations.drift_check")]
    public override Task<Api.DriftCheckResult> DriftCheck(Api.DriftCheckRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DriftCheckRequest, Api.DriftCheckResult>(executor, request, context, "instrument_operations.drift_check");

    [OperationImplementation("instrument_operations.construct_perimeters_from_surface_face_list")]
    public override Task<Api.ConstructPerimetersFromSurfaceFaceListResult> ConstructPerimetersFromSurfaceFaceList(Api.ConstructPerimetersFromSurfaceFaceListRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ConstructPerimetersFromSurfaceFaceListRequest, Api.ConstructPerimetersFromSurfaceFaceListResult>(executor, request, context, "instrument_operations.construct_perimeters_from_surface_face_list");

    [OperationImplementation("instrument_operations.dissect_point_group")]
    public override Task<Api.DissectPointGroupResult> DissectPointGroup(Api.DissectPointGroupRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DissectPointGroupRequest, Api.DissectPointGroupResult>(executor, request, context, "instrument_operations.dissect_point_group");

    [OperationImplementation("instrument_operations.lr_self_test")]
    public override Task<Api.LrSelfTestResult> LrSelfTest(Api.LrSelfTestRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrSelfTestRequest, Api.LrSelfTestResult>(executor, request, context, "instrument_operations.lr_self_test");

    [OperationImplementation("instrument_operations.get_last_instrument_index")]
    public override Task<Api.GetLastInstrumentIndexResult> GetLastInstrumentIndex(Api.GetLastInstrumentIndexRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetLastInstrumentIndexRequest, Api.GetLastInstrumentIndexResult>(executor, request, context, "instrument_operations.get_last_instrument_index");

    [OperationImplementation("instrument_operations.set_absolute_instrument_scale_factor")]
    public override Task<Api.SetAbsoluteInstrumentScaleFactorResult> SetAbsoluteInstrumentScaleFactor(Api.SetAbsoluteInstrumentScaleFactorRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetAbsoluteInstrumentScaleFactorRequest, Api.SetAbsoluteInstrumentScaleFactorResult>(executor, request, context, "instrument_operations.set_absolute_instrument_scale_factor");

    [OperationImplementation("instrument_operations.send_cloud_to_sa")]
    public override Task<Api.SendCloudToSaResult> SendCloudToSa(Api.SendCloudToSaRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SendCloudToSaRequest, Api.SendCloudToSaResult>(executor, request, context, "instrument_operations.send_cloud_to_sa");

    [OperationImplementation("instrument_operations.get_estimated_scan_time")]
    public override Task<Api.GetEstimatedScanTimeResult> GetEstimatedScanTime(Api.GetEstimatedScanTimeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetEstimatedScanTimeRequest, Api.GetEstimatedScanTimeResult>(executor, request, context, "instrument_operations.get_estimated_scan_time");

    [OperationImplementation("instrument_operations.locate_instrument_best_fit_group_to_group")]
    public override Task<Api.LocateInstrumentBestFitGroupToGroupResult> LocateInstrumentBestFitGroupToGroup(Api.LocateInstrumentBestFitGroupToGroupRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LocateInstrumentBestFitGroupToGroupRequest, Api.LocateInstrumentBestFitGroupToGroupResult>(executor, request, context, "instrument_operations.locate_instrument_best_fit_group_to_group");

    [OperationImplementation("instrument_operations.get_instruments_with_observations_on_target")]
    public override Task<Api.GetInstrumentsWithObservationsOnTargetResult> GetInstrumentsWithObservationsOnTarget(Api.GetInstrumentsWithObservationsOnTargetRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentsWithObservationsOnTargetRequest, Api.GetInstrumentsWithObservationsOnTargetResult>(executor, request, context, "instrument_operations.get_instruments_with_observations_on_target");

    [OperationImplementation("instrument_operations.set_instrument_measurement_mode_profile")]
    public override Task<Api.SetInstrumentMeasurementModeProfileResult> SetInstrumentMeasurementModeProfile(Api.SetInstrumentMeasurementModeProfileRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentMeasurementModeProfileRequest, Api.SetInstrumentMeasurementModeProfileResult>(executor, request, context, "instrument_operations.set_instrument_measurement_mode_profile");

    [OperationImplementation("instrument_operations.watch_point_to_edge")]
    public override Task<Api.WatchPointToEdgeResult> WatchPointToEdge(Api.WatchPointToEdgeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.WatchPointToEdgeRequest, Api.WatchPointToEdgeResult>(executor, request, context, "instrument_operations.watch_point_to_edge");

    [OperationImplementation("instrument_operations.get_number_of_observations_on_target")]
    public override Task<Api.GetNumberOfObservationsOnTargetResult> GetNumberOfObservationsOnTarget(Api.GetNumberOfObservationsOnTargetRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfObservationsOnTargetRequest, Api.GetNumberOfObservationsOnTargetResult>(executor, request, context, "instrument_operations.get_number_of_observations_on_target");

    [OperationImplementation("instrument_operations.set_multiply_instrument_scale_factor")]
    public override Task<Api.SetMultiplyInstrumentScaleFactorResult> SetMultiplyInstrumentScaleFactor(Api.SetMultiplyInstrumentScaleFactorRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetMultiplyInstrumentScaleFactorRequest, Api.SetMultiplyInstrumentScaleFactorResult>(executor, request, context, "instrument_operations.set_multiply_instrument_scale_factor");

    [OperationImplementation("instrument_operations.get_instrument_transform")]
    public override Task<Api.GetInstrumentTransformResult> GetInstrumentTransform(Api.GetInstrumentTransformRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentTransformRequest, Api.GetInstrumentTransformResult>(executor, request, context, "instrument_operations.get_instrument_transform");

    [OperationImplementation("instrument_operations.auto_measure_vectors")]
    public override Task<Api.AutoMeasureVectorsResult> AutoMeasureVectors(Api.AutoMeasureVectorsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoMeasureVectorsRequest, Api.AutoMeasureVectorsResult>(executor, request, context, "instrument_operations.auto_measure_vectors");

    [OperationImplementation("instrument_operations.multi_measurement_stop")]
    public override Task<Api.MultiMeasurementStopResult> MultiMeasurementStop(Api.MultiMeasurementStopRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MultiMeasurementStopRequest, Api.MultiMeasurementStopResult>(executor, request, context, "instrument_operations.multi_measurement_stop");

    [OperationImplementation("instrument_operations.set_observation_status")]
    public override Task<Api.SetObservationStatusResult> SetObservationStatus(Api.SetObservationStatusRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObservationStatusRequest, Api.SetObservationStatusResult>(executor, request, context, "instrument_operations.set_observation_status");

    [OperationImplementation("instrument_operations.start_gdt_inspection_design")]
    public override Task<Api.StartGdtInspectionDesignResult> StartGdtInspectionDesign(Api.StartGdtInspectionDesignRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartGdtInspectionDesignRequest, Api.StartGdtInspectionDesignResult>(executor, request, context, "instrument_operations.start_gdt_inspection_design");

    [OperationImplementation("instrument_operations.get_instrument_part_temperature")]
    public override Task<Api.GetInstrumentPartTemperatureResult> GetInstrumentPartTemperature(Api.GetInstrumentPartTemperatureRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetInstrumentPartTemperatureRequest, Api.GetInstrumentPartTemperatureResult>(executor, request, context, "instrument_operations.get_instrument_part_temperature");

    [OperationImplementation("instrument_operations.initiate_servo_guide")]
    public override Task<Api.InitiateServoGuideResult> InitiateServoGuide(Api.InitiateServoGuideRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.InitiateServoGuideRequest, Api.InitiateServoGuideResult>(executor, request, context, "instrument_operations.initiate_servo_guide");

    [OperationImplementation("instrument_operations.get_targets_measured_by_instrument")]
    public override Task<Api.GetTargetsMeasuredByInstrumentResult> GetTargetsMeasuredByInstrument(Api.GetTargetsMeasuredByInstrumentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTargetsMeasuredByInstrumentRequest, Api.GetTargetsMeasuredByInstrumentResult>(executor, request, context, "instrument_operations.get_targets_measured_by_instrument");

    [OperationImplementation("instrument_operations.set_xyz_instrument_uncertainties")]
    public override Task<Api.SetXyzInstrumentUncertaintiesResult> SetXyzInstrumentUncertainties(Api.SetXyzInstrumentUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetXyzInstrumentUncertaintiesRequest, Api.SetXyzInstrumentUncertaintiesResult>(executor, request, context, "instrument_operations.set_xyz_instrument_uncertainties");

    [OperationImplementation("instrument_operations.calculate_tcp_fixture_uncertainties")]
    public override Task<Api.CalculateTcpFixtureUncertaintiesResult> CalculateTcpFixtureUncertainties(Api.CalculateTcpFixtureUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CalculateTcpFixtureUncertaintiesRequest, Api.CalculateTcpFixtureUncertaintiesResult>(executor, request, context, "instrument_operations.calculate_tcp_fixture_uncertainties");

    [OperationImplementation("instrument_operations.set_instrument_weather_setting")]
    public override Task<Api.SetInstrumentWeatherSettingResult> SetInstrumentWeatherSetting(Api.SetInstrumentWeatherSettingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentWeatherSettingRequest, Api.SetInstrumentWeatherSettingResult>(executor, request, context, "instrument_operations.set_instrument_weather_setting");

    [OperationImplementation("instrument_operations.set_xyz_reference_frame_instrument_base_anchor_frame")]
    public override Task<Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameResult> SetXyzReferenceFrameInstrumentBaseAnchorFrame(Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameRequest, Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameResult>(executor, request, context, "instrument_operations.set_xyz_reference_frame_instrument_base_anchor_frame");

    [OperationImplementation("instrument_operations.lr_apdis_get_active_mcm_calibration")]
    public override Task<Api.LrApdisGetActiveMcmCalibrationResult> LrApdisGetActiveMcmCalibration(Api.LrApdisGetActiveMcmCalibrationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrApdisGetActiveMcmCalibrationRequest, Api.LrApdisGetActiveMcmCalibrationResult>(executor, request, context, "instrument_operations.lr_apdis_get_active_mcm_calibration");

    [OperationImplementation("instrument_operations.get_xyz_instrument_uncertainties")]
    public override Task<Api.GetXyzInstrumentUncertaintiesResult> GetXyzInstrumentUncertainties(Api.GetXyzInstrumentUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetXyzInstrumentUncertaintiesRequest, Api.GetXyzInstrumentUncertaintiesResult>(executor, request, context, "instrument_operations.get_xyz_instrument_uncertainties");

    [OperationImplementation("instrument_operations.start_instrument_interface")]
    public override Task<Api.StartInstrumentInterfaceResult> StartInstrumentInterface(Api.StartInstrumentInterfaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartInstrumentInterfaceRequest, Api.StartInstrumentInterfaceResult>(executor, request, context, "instrument_operations.start_instrument_interface");

    [OperationImplementation("instrument_operations.start_theodolite_interface")]
    public override Task<Api.StartTheodoliteInterfaceResult> StartTheodoliteInterface(Api.StartTheodoliteInterfaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartTheodoliteInterfaceRequest, Api.StartTheodoliteInterfaceResult>(executor, request, context, "instrument_operations.start_theodolite_interface");

    [OperationImplementation("instrument_operations.enable_disable_point_set_scan_mode")]
    public override Task<Api.EnableDisablePointSetScanModeResult> EnableDisablePointSetScanMode(Api.EnableDisablePointSetScanModeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EnableDisablePointSetScanModeRequest, Api.EnableDisablePointSetScanModeResult>(executor, request, context, "instrument_operations.enable_disable_point_set_scan_mode");

    [OperationImplementation("instrument_operations.create_templated_instrument_usmn")]
    public override Task<Api.CreateTemplatedInstrumentUsmnResult> CreateTemplatedInstrumentUsmn(Api.CreateTemplatedInstrumentUsmnRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreateTemplatedInstrumentUsmnRequest, Api.CreateTemplatedInstrumentUsmnResult>(executor, request, context, "instrument_operations.create_templated_instrument_usmn");

    [OperationImplementation("instrument_operations.set_instrument_transform")]
    public override Task<Api.SetInstrumentTransformResult> SetInstrumentTransform(Api.SetInstrumentTransformRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInstrumentTransformRequest, Api.SetInstrumentTransformResult>(executor, request, context, "instrument_operations.set_instrument_transform");

    [OperationImplementation("instrument_operations.stop_active_measurement_mode")]
    public override Task<Api.StopActiveMeasurementModeResult> StopActiveMeasurementMode(Api.StopActiveMeasurementModeRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StopActiveMeasurementModeRequest, Api.StopActiveMeasurementModeResult>(executor, request, context, "instrument_operations.stop_active_measurement_mode");

    [OperationImplementation("instrument_operations.lr_self_test_lo_sep")]
    public override Task<Api.LrSelfTestLoSepResult> LrSelfTestLoSep(Api.LrSelfTestLoSepRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrSelfTestLoSepRequest, Api.LrSelfTestLoSepResult>(executor, request, context, "instrument_operations.lr_self_test_lo_sep");

    [OperationImplementation("instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix")]
    public override Task<Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixResult> GetLastSolvedTcpFixtureUncertaintyCovarianceMatrix(Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixRequest, Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixResult>(executor, request, context, "instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix");

    [OperationImplementation("instrument_operations.measure_existing_single_point_and_compare")]
    public override Task<Api.MeasureExistingSinglePointAndCompareResult> MeasureExistingSinglePointAndCompare(Api.MeasureExistingSinglePointAndCompareRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MeasureExistingSinglePointAndCompareRequest, Api.MeasureExistingSinglePointAndCompareResult>(executor, request, context, "instrument_operations.measure_existing_single_point_and_compare");

    [OperationImplementation("instrument_operations.set_probe_offset_frame_offline")]
    public override Task<Api.SetProbeOffsetFrameOfflineResult> SetProbeOffsetFrameOffline(Api.SetProbeOffsetFrameOfflineRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetProbeOffsetFrameOfflineRequest, Api.SetProbeOffsetFrameOfflineResult>(executor, request, context, "instrument_operations.set_probe_offset_frame_offline");

    [OperationImplementation("instrument_operations.locate_instrument_group_to_surface_quick_fit")]
    public override Task<Api.LocateInstrumentGroupToSurfaceQuickFitResult> LocateInstrumentGroupToSurfaceQuickFit(Api.LocateInstrumentGroupToSurfaceQuickFitRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LocateInstrumentGroupToSurfaceQuickFitRequest, Api.LocateInstrumentGroupToSurfaceQuickFitResult>(executor, request, context, "instrument_operations.locate_instrument_group_to_surface_quick_fit");

    [OperationImplementation("instrument_operations.lr_get_most_recent_snr_info")]
    public override Task<Api.LrGetMostRecentSnrInfoResult> LrGetMostRecentSnrInfo(Api.LrGetMostRecentSnrInfoRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LrGetMostRecentSnrInfoRequest, Api.LrGetMostRecentSnrInfoResult>(executor, request, context, "instrument_operations.lr_get_most_recent_snr_info");

    [OperationImplementation("instrument_operations.measure_existing_single_point_manual_guide")]
    public override Task<Api.MeasureExistingSinglePointManualGuideResult> MeasureExistingSinglePointManualGuide(Api.MeasureExistingSinglePointManualGuideRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MeasureExistingSinglePointManualGuideRequest, Api.MeasureExistingSinglePointManualGuideResult>(executor, request, context, "instrument_operations.measure_existing_single_point_manual_guide");

    [OperationImplementation("instrument_operations.instrument_operational_check")]
    public override Task<Api.InstrumentOperationalCheckResult> InstrumentOperationalCheck(Api.InstrumentOperationalCheckRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.InstrumentOperationalCheckRequest, Api.InstrumentOperationalCheckResult>(executor, request, context, "instrument_operations.instrument_operational_check");

    [OperationImplementation("instrument_operations.close_auto_correspond_closest_point_dialog")]
    public override Task<Api.CloseAutoCorrespondClosestPointDialogResult> CloseAutoCorrespondClosestPointDialog(Api.CloseAutoCorrespondClosestPointDialogRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CloseAutoCorrespondClosestPointDialogRequest, Api.CloseAutoCorrespondClosestPointDialogResult>(executor, request, context, "instrument_operations.close_auto_correspond_closest_point_dialog");

}
