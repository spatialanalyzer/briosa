using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal sealed class InstrumentOperationsService(OperationExecutor executor)
    : Api.InstrumentOperations.InstrumentOperationsBase
{
    [OperationImplementation("instrument_operations.get_instrument_group_and_target")]
    public override Task<Api.GetInstrumentGroupAndTargetResult> GetInstrumentGroupAndTarget(Api.GetInstrumentGroupAndTargetRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentGroupAndTargetOperation.Descriptor,
            GetInstrumentGroupAndTargetOperation.CreateCommand, GetInstrumentGroupAndTargetOperation.OutputContracts,
            GetInstrumentGroupAndTargetOperation.CreateResult);

    [OperationImplementation("instrument_operations.locate_instruments_usmn")]
    public override Task<Api.LocateInstrumentsUsmnResult> LocateInstrumentsUsmn(Api.LocateInstrumentsUsmnRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LocateInstrumentsUsmnOperation.Descriptor,
            LocateInstrumentsUsmnOperation.CreateCommand, LocateInstrumentsUsmnOperation.OutputContracts,
            LocateInstrumentsUsmnOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_pcmm_instrument_xyz_uncertainties")]
    public override Task<Api.SetPcmmInstrumentXyzUncertaintiesResult> SetPcmmInstrumentXyzUncertainties(Api.SetPcmmInstrumentXyzUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPcmmInstrumentXyzUncertaintiesOperation.Descriptor,
            SetPcmmInstrumentXyzUncertaintiesOperation.CreateCommand, SetPcmmInstrumentXyzUncertaintiesOperation.OutputContracts,
            SetPcmmInstrumentXyzUncertaintiesOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_wrtl_channel")]
    public override Task<Api.SetWrtlChannelResult> SetWrtlChannel(Api.SetWrtlChannelRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetWrtlChannelOperation.Descriptor,
            SetWrtlChannelOperation.CreateCommand, SetWrtlChannelOperation.OutputContracts,
            SetWrtlChannelOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_tracker_edm_theodolite_uncertainties")]
    public override Task<Api.GetTrackerEdmTheodoliteUncertaintiesResult> GetTrackerEdmTheodoliteUncertainties(Api.GetTrackerEdmTheodoliteUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTrackerEdmTheodoliteUncertaintiesOperation.Descriptor,
            GetTrackerEdmTheodoliteUncertaintiesOperation.CreateCommand, GetTrackerEdmTheodoliteUncertaintiesOperation.OutputContracts,
            GetTrackerEdmTheodoliteUncertaintiesOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_model")]
    public override Task<Api.GetInstrumentModelResult> GetInstrumentModel(Api.GetInstrumentModelRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentModelOperation.Descriptor,
            GetInstrumentModelOperation.CreateCommand, GetInstrumentModelOperation.OutputContracts,
            GetInstrumentModelOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_ladar_feature_meas_slot")]
    public override Task<Api.SetLadarFeatureMeasSlotResult> SetLadarFeatureMeasSlot(Api.SetLadarFeatureMeasSlotRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetLadarFeatureMeasSlotOperation.Descriptor,
            SetLadarFeatureMeasSlotOperation.CreateCommand, SetLadarFeatureMeasSlotOperation.OutputContracts,
            SetLadarFeatureMeasSlotOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_hardware_disconnect")]
    public override Task<Api.LrHardwareDisconnectResult> LrHardwareDisconnect(Api.LrHardwareDisconnectRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrHardwareDisconnectOperation.Descriptor,
            LrHardwareDisconnectOperation.CreateCommand, LrHardwareDisconnectOperation.OutputContracts,
            LrHardwareDisconnectOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_observation_mirror_cube_shot_face")]
    public override Task<Api.SetObservationMirrorCubeShotFaceResult> SetObservationMirrorCubeShotFace(Api.SetObservationMirrorCubeShotFaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObservationMirrorCubeShotFaceOperation.Descriptor,
            SetObservationMirrorCubeShotFaceOperation.CreateCommand, SetObservationMirrorCubeShotFaceOperation.OutputContracts,
            SetObservationMirrorCubeShotFaceOperation.CreateResult);

    [OperationImplementation("instrument_operations.construct_mirror_from_two_points")]
    public override Task<Api.ConstructMirrorFromTwoPointsResult> ConstructMirrorFromTwoPoints(Api.ConstructMirrorFromTwoPointsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConstructMirrorFromTwoPointsOperation.Descriptor,
            ConstructMirrorFromTwoPointsOperation.CreateCommand, ConstructMirrorFromTwoPointsOperation.OutputContracts, ConstructMirrorFromTwoPointsOperation.CreateResult);

    [OperationImplementation("instrument_operations.transform_instrument_frame_to_frame")]
    public override Task<Api.TransformInstrumentFrameToFrameResult> TransformInstrumentFrameToFrame(Api.TransformInstrumentFrameToFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TransformInstrumentFrameToFrameOperation.Descriptor,
            TransformInstrumentFrameToFrameOperation.CreateCommand, TransformInstrumentFrameToFrameOperation.OutputContracts,
            TransformInstrumentFrameToFrameOperation.CreateResult);

    [OperationImplementation("instrument_operations.add_nominal_point_to_tcp_fixture")]
    public override Task<Api.AddNominalPointToTcpFixtureResult> AddNominalPointToTcpFixture(Api.AddNominalPointToTcpFixtureRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddNominalPointToTcpFixtureOperation.Descriptor,
            AddNominalPointToTcpFixtureOperation.CreateCommand, AddNominalPointToTcpFixtureOperation.OutputContracts,
            AddNominalPointToTcpFixtureOperation.CreateResult);

    [OperationImplementation("instrument_operations.locate_instrument_best_fit_nominal_geometry")]
    public override Task<Api.LocateInstrumentBestFitNominalGeometryResult> LocateInstrumentBestFitNominalGeometry(Api.LocateInstrumentBestFitNominalGeometryRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LocateInstrumentBestFitNominalGeometryOperation.Descriptor,
            LocateInstrumentBestFitNominalGeometryOperation.CreateCommand, LocateInstrumentBestFitNominalGeometryOperation.OutputContracts,
            LocateInstrumentBestFitNominalGeometryOperation.CreateResult);

    [OperationImplementation("instrument_operations.multi_measurement_initiate")]
    public override Task<Api.MultiMeasurementInitiateResult> MultiMeasurementInitiate(Api.MultiMeasurementInitiateRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MultiMeasurementInitiateOperation.Descriptor,
            MultiMeasurementInitiateOperation.CreateCommand, MultiMeasurementInitiateOperation.OutputContracts,
            MultiMeasurementInitiateOperation.CreateResult);

    [OperationImplementation("instrument_operations.dock_instrument_interface")]
    public override Task<Api.DockInstrumentInterfaceResult> DockInstrumentInterface(Api.DockInstrumentInterfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DockInstrumentInterfaceOperation.Descriptor,
            DockInstrumentInterfaceOperation.CreateCommand, DockInstrumentInterfaceOperation.OutputContracts,
            DockInstrumentInterfaceOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_apdis_perform_mcm_calibration")]
    public override Task<Api.LrApdisPerformMcmCalibrationResult> LrApdisPerformMcmCalibration(Api.LrApdisPerformMcmCalibrationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrApdisPerformMcmCalibrationOperation.Descriptor,
            LrApdisPerformMcmCalibrationOperation.CreateCommand, LrApdisPerformMcmCalibrationOperation.OutputContracts,
            LrApdisPerformMcmCalibrationOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_wrtl_channel_and_status")]
    public override Task<Api.GetWrtlChannelAndStatusResult> GetWrtlChannelAndStatus(Api.GetWrtlChannelAndStatusRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetWrtlChannelAndStatusOperation.Descriptor,
            GetWrtlChannelAndStatusOperation.CreateCommand, GetWrtlChannelAndStatusOperation.OutputContracts,
            GetWrtlChannelAndStatusOperation.CreateResult);

    [OperationImplementation("instrument_operations.move_measurement_observation")]
    public override Task<Api.MoveMeasurementObservationResult> MoveMeasurementObservation(Api.MoveMeasurementObservationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveMeasurementObservationOperation.Descriptor,
            MoveMeasurementObservationOperation.CreateCommand, MoveMeasurementObservationOperation.OutputContracts,
            MoveMeasurementObservationOperation.CreateResult);

    [OperationImplementation("instrument_operations.synchronized_measurement_master_slave")]
    public override Task<Api.SynchronizedMeasurementMasterSlaveResult> SynchronizedMeasurementMasterSlave(Api.SynchronizedMeasurementMasterSlaveRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SynchronizedMeasurementMasterSlaveOperation.Descriptor,
            SynchronizedMeasurementMasterSlaveOperation.CreateCommand, SynchronizedMeasurementMasterSlaveOperation.OutputContracts,
            SynchronizedMeasurementMasterSlaveOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_apdis_activate_mcm_calibration")]
    public override Task<Api.LrApdisActivateMcmCalibrationResult> LrApdisActivateMcmCalibration(Api.LrApdisActivateMcmCalibrationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrApdisActivateMcmCalibrationOperation.Descriptor,
            LrApdisActivateMcmCalibrationOperation.CreateCommand, LrApdisActivateMcmCalibrationOperation.OutputContracts,
            LrApdisActivateMcmCalibrationOperation.CreateResult);

    [OperationImplementation("instrument_operations.delete_measurements")]
    public override Task<Api.DeleteMeasurementsResult> DeleteMeasurements(Api.DeleteMeasurementsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteMeasurementsOperation.Descriptor,
            DeleteMeasurementsOperation.CreateCommand, DeleteMeasurementsOperation.OutputContracts,
            DeleteMeasurementsOperation.CreateResult);

    [OperationImplementation("instrument_operations.watch_instrument")]
    public override Task<Api.WatchInstrumentResult> WatchInstrument(Api.WatchInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WatchInstrumentOperation.Descriptor,
            WatchInstrumentOperation.CreateCommand, WatchInstrumentOperation.OutputContracts,
            WatchInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_alignment_projector")]
    public override Task<Api.SetAlignmentProjectorResult> SetAlignmentProjector(Api.SetAlignmentProjectorRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetAlignmentProjectorOperation.Descriptor,
            SetAlignmentProjectorOperation.CreateCommand, SetAlignmentProjectorOperation.OutputContracts,
            SetAlignmentProjectorOperation.CreateResult);

    [OperationImplementation("instrument_operations.construct_tcp_fixture")]
    public override Task<Api.ConstructTcpFixtureResult> ConstructTcpFixture(Api.ConstructTcpFixtureRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConstructTcpFixtureOperation.Descriptor,
            ConstructTcpFixtureOperation.CreateCommand, ConstructTcpFixtureOperation.OutputContracts,
            ConstructTcpFixtureOperation.CreateResult);

    [OperationImplementation("instrument_operations.stop_instrument_interface")]
    public override Task<Api.StopInstrumentInterfaceResult> StopInstrumentInterface(Api.StopInstrumentInterfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StopInstrumentInterfaceOperation.Descriptor,
            StopInstrumentInterfaceOperation.CreateCommand, StopInstrumentInterfaceOperation.OutputContracts,
            StopInstrumentInterfaceOperation.CreateResult);

    [OperationImplementation("instrument_operations.rename_instrument")]
    public override Task<Api.RenameInstrumentResult> RenameInstrument(Api.RenameInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RenameInstrumentOperation.Descriptor,
            RenameInstrumentOperation.CreateCommand, RenameInstrumentOperation.OutputContracts,
            RenameInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.auto_measure_specified_geometry")]
    public override Task<Api.AutoMeasureSpecifiedGeometryResult> AutoMeasureSpecifiedGeometry(Api.AutoMeasureSpecifiedGeometryRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoMeasureSpecifiedGeometryOperation.Descriptor,
            AutoMeasureSpecifiedGeometryOperation.CreateCommand, AutoMeasureSpecifiedGeometryOperation.OutputContracts,
            AutoMeasureSpecifiedGeometryOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_group_and_target")]
    public override Task<Api.SetInstrumentGroupAndTargetResult> SetInstrumentGroupAndTarget(Api.SetInstrumentGroupAndTargetRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentGroupAndTargetOperation.Descriptor,
            SetInstrumentGroupAndTargetOperation.CreateCommand, SetInstrumentGroupAndTargetOperation.OutputContracts,
            SetInstrumentGroupAndTargetOperation.CreateResult);

    [OperationImplementation("instrument_operations.auto_measure_points")]
    public override Task<Api.AutoMeasurePointsResult> AutoMeasurePoints(Api.AutoMeasurePointsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoMeasurePointsOperation.Descriptor,
            AutoMeasurePointsOperation.CreateCommand, AutoMeasurePointsOperation.OutputContracts,
            AutoMeasurePointsOperation.CreateResult);

    [OperationImplementation("instrument_operations.start_gdt_inspection")]
    public override Task<Api.StartGdtInspectionResult> StartGdtInspection(Api.StartGdtInspectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartGdtInspectionOperation.Descriptor,
            StartGdtInspectionOperation.CreateCommand, StartGdtInspectionOperation.OutputContracts,
            StartGdtInspectionOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_targeting")]
    public override Task<Api.SetInstrumentTargetingResult> SetInstrumentTargeting(Api.SetInstrumentTargetingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentTargetingOperation.Descriptor,
            SetInstrumentTargetingOperation.CreateCommand, SetInstrumentTargetingOperation.OutputContracts,
            SetInstrumentTargetingOperation.CreateResult);

    [OperationImplementation("instrument_operations.compute_cte_scale_factor")]
    public override Task<Api.ComputeCteScaleFactorResult> ComputeCteScaleFactor(Api.ComputeCteScaleFactorRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ComputeCteScaleFactorOperation.Descriptor,
            ComputeCteScaleFactorOperation.CreateCommand, ComputeCteScaleFactorOperation.OutputContracts,
            ComputeCteScaleFactorOperation.CreateResult);

    [OperationImplementation("instrument_operations.watch_point_to_objects")]
    public override Task<Api.WatchPointToObjectsResult> WatchPointToObjects(Api.WatchPointToObjectsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WatchPointToObjectsOperation.Descriptor,
            WatchPointToObjectsOperation.CreateCommand, WatchPointToObjectsOperation.OutputContracts,
            WatchPointToObjectsOperation.CreateResult);

    [OperationImplementation("instrument_operations.enable_disable_frame_set_scan_mode_by_instrument")]
    public override Task<Api.EnableDisableFrameSetScanModeByInstrumentResult> EnableDisableFrameSetScanModeByInstrument(Api.EnableDisableFrameSetScanModeByInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisableFrameSetScanModeByInstrumentOperation.Descriptor,
            EnableDisableFrameSetScanModeByInstrumentOperation.CreateCommand, EnableDisableFrameSetScanModeByInstrumentOperation.OutputContracts,
            EnableDisableFrameSetScanModeByInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_world")]
    public override Task<Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult> SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld(Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.Descriptor,
            SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateCommand, SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.OutputContracts,
            SetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateResult);

    [OperationImplementation("instrument_operations.scan_cad_faces")]
    public override Task<Api.ScanCadFacesResult> ScanCadFaces(Api.ScanCadFacesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ScanCadFacesOperation.Descriptor,
            ScanCadFacesOperation.CreateCommand, ScanCadFacesOperation.OutputContracts,
            ScanCadFacesOperation.CreateResult);

    [OperationImplementation("instrument_operations.delete_instrument")]
    public override Task<Api.DeleteInstrumentResult> DeleteInstrument(Api.DeleteInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteInstrumentOperation.Descriptor,
            DeleteInstrumentOperation.CreateCommand, DeleteInstrumentOperation.OutputContracts,
            DeleteInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.combine_point_groups")]
    public override Task<Api.CombinePointGroupsResult> CombinePointGroups(Api.CombinePointGroupsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CombinePointGroupsOperation.Descriptor,
            CombinePointGroupsOperation.CreateCommand, CombinePointGroupsOperation.OutputContracts, CombinePointGroupsOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_cloud_viewer_filter")]
    public override Task<Api.SetCloudViewerFilterResult> SetCloudViewerFilter(Api.SetCloudViewerFilterRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCloudViewerFilterOperation.Descriptor,
            SetCloudViewerFilterOperation.CreateCommand, SetCloudViewerFilterOperation.OutputContracts,
            SetCloudViewerFilterOperation.CreateResult);

    [OperationImplementation("instrument_operations.transform_instrument_by_delta")]
    public override Task<Api.TransformInstrumentByDeltaResult> TransformInstrumentByDelta(Api.TransformInstrumentByDeltaRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TransformInstrumentByDeltaOperation.Descriptor,
            TransformInstrumentByDeltaOperation.CreateCommand, TransformInstrumentByDeltaOperation.OutputContracts,
            TransformInstrumentByDeltaOperation.CreateResult);

    [OperationImplementation("instrument_operations.make_surface_face_list_from_point_proximity")]
    public override Task<Api.MakeSurfaceFaceListFromPointProximityResult> MakeSurfaceFaceListFromPointProximity(Api.MakeSurfaceFaceListFromPointProximityRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeSurfaceFaceListFromPointProximityOperation.Descriptor,
            MakeSurfaceFaceListFromPointProximityOperation.CreateCommand, MakeSurfaceFaceListFromPointProximityOperation.OutputContracts,
            MakeSurfaceFaceListFromPointProximityOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_measurement_mode_profile")]
    public override Task<Api.GetInstrumentMeasurementModeProfileResult> GetInstrumentMeasurementModeProfile(Api.GetInstrumentMeasurementModeProfileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentMeasurementModeProfileOperation.Descriptor,
            GetInstrumentMeasurementModeProfileOperation.CreateCommand, GetInstrumentMeasurementModeProfileOperation.OutputContracts,
            GetInstrumentMeasurementModeProfileOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_observation_info")]
    public override Task<Api.GetObservationInfoResult> GetObservationInfo(Api.GetObservationInfoRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetObservationInfoOperation.Descriptor,
            GetObservationInfoOperation.CreateCommand, GetObservationInfoOperation.OutputContracts,
            GetObservationInfoOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_interface_response_timeout")]
    public override Task<Api.GetInstrumentInterfaceResponseTimeoutResult> GetInstrumentInterfaceResponseTimeout(Api.GetInstrumentInterfaceResponseTimeoutRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentInterfaceResponseTimeoutOperation.Descriptor,
            GetInstrumentInterfaceResponseTimeoutOperation.CreateCommand, GetInstrumentInterfaceResponseTimeoutOperation.OutputContracts,
            GetInstrumentInterfaceResponseTimeoutOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_axes")]
    public override Task<Api.SetInstrumentAxesResult> SetInstrumentAxes(Api.SetInstrumentAxesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentAxesOperation.Descriptor,
            SetInstrumentAxesOperation.CreateCommand, SetInstrumentAxesOperation.OutputContracts,
            SetInstrumentAxesOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_remeasure_failed_checks_only")]
    public override Task<Api.SetRemeasureFailedChecksOnlyResult> SetRemeasureFailedChecksOnly(Api.SetRemeasureFailedChecksOnlyRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRemeasureFailedChecksOnlyOperation.Descriptor,
            SetRemeasureFailedChecksOnlyOperation.CreateCommand, SetRemeasureFailedChecksOnlyOperation.OutputContracts,
            SetRemeasureFailedChecksOnlyOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_ladar_feature_meas_circle")]
    public override Task<Api.SetLadarFeatureMeasCircleResult> SetLadarFeatureMeasCircle(Api.SetLadarFeatureMeasCircleRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetLadarFeatureMeasCircleOperation.Descriptor,
            SetLadarFeatureMeasCircleOperation.CreateCommand, SetLadarFeatureMeasCircleOperation.OutputContracts,
            SetLadarFeatureMeasCircleOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_target_status")]
    public override Task<Api.GetInstrumentTargetStatusResult> GetInstrumentTargetStatus(Api.GetInstrumentTargetStatusRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentTargetStatusOperation.Descriptor,
            GetInstrumentTargetStatusOperation.CreateCommand, GetInstrumentTargetStatusOperation.OutputContracts,
            GetInstrumentTargetStatusOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_self_test_linearization")]
    public override Task<Api.LrSelfTestLinearizationResult> LrSelfTestLinearization(Api.LrSelfTestLinearizationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrSelfTestLinearizationOperation.Descriptor,
            LrSelfTestLinearizationOperation.CreateCommand, LrSelfTestLinearizationOperation.OutputContracts,
            LrSelfTestLinearizationOperation.CreateResult);

    [OperationImplementation("instrument_operations.align_cloud_to_cad")]
    public override Task<Api.AlignCloudToCadResult> AlignCloudToCad(Api.AlignCloudToCadRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AlignCloudToCadOperation.Descriptor,
            AlignCloudToCadOperation.CreateCommand, AlignCloudToCadOperation.OutputContracts,
            AlignCloudToCadOperation.CreateResult);

    [OperationImplementation("instrument_operations.watch_point_to_point")]
    public override Task<Api.WatchPointToPointResult> WatchPointToPoint(Api.WatchPointToPointRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WatchPointToPointOperation.Descriptor,
            WatchPointToPointOperation.CreateCommand, WatchPointToPointOperation.OutputContracts,
            WatchPointToPointOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_current_trapping_status")]
    public override Task<Api.GetCurrentTrappingStatusResult> GetCurrentTrappingStatus(Api.GetCurrentTrappingStatusRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCurrentTrappingStatusOperation.Descriptor,
            GetCurrentTrappingStatusOperation.CreateCommand, GetCurrentTrappingStatusOperation.OutputContracts,
            GetCurrentTrappingStatusOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_verify_hardware_connection")]
    public override Task<Api.LrVerifyHardwareConnectionResult> LrVerifyHardwareConnection(Api.LrVerifyHardwareConnectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrVerifyHardwareConnectionOperation.Descriptor,
            LrVerifyHardwareConnectionOperation.CreateCommand, LrVerifyHardwareConnectionOperation.OutputContracts,
            LrVerifyHardwareConnectionOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_interface_response_timeout")]
    public override Task<Api.SetInstrumentInterfaceResponseTimeoutResult> SetInstrumentInterfaceResponseTimeout(Api.SetInstrumentInterfaceResponseTimeoutRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentInterfaceResponseTimeoutOperation.Descriptor,
            SetInstrumentInterfaceResponseTimeoutOperation.CreateCommand, SetInstrumentInterfaceResponseTimeoutOperation.OutputContracts,
            SetInstrumentInterfaceResponseTimeoutOperation.CreateResult);

    [OperationImplementation("instrument_operations.load_instrument_configuration")]
    public override Task<Api.LoadInstrumentConfigurationResult> LoadInstrumentConfiguration(Api.LoadInstrumentConfigurationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LoadInstrumentConfigurationOperation.Descriptor,
            LoadInstrumentConfigurationOperation.CreateCommand, LoadInstrumentConfigurationOperation.OutputContracts,
            LoadInstrumentConfigurationOperation.CreateResult);

    [OperationImplementation("instrument_operations.point_at_target")]
    public override Task<Api.PointAtTargetResult> PointAtTarget(Api.PointAtTargetRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PointAtTargetOperation.Descriptor,
            PointAtTargetOperation.CreateCommand, PointAtTargetOperation.OutputContracts,
            PointAtTargetOperation.CreateResult);

    [OperationImplementation("instrument_operations.move_objects_in_6d_using_instrument_updates")]
    public override Task<Api.MoveObjectsIn6dUsingInstrumentUpdatesResult> MoveObjectsIn6dUsingInstrumentUpdates(Api.MoveObjectsIn6dUsingInstrumentUpdatesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveObjectsIn6dUsingInstrumentUpdatesOperation.Descriptor,
            MoveObjectsIn6dUsingInstrumentUpdatesOperation.CreateCommand, MoveObjectsIn6dUsingInstrumentUpdatesOperation.OutputContracts,
            MoveObjectsIn6dUsingInstrumentUpdatesOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world")]
    public override Task<Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldResult> GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld(Api.GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.Descriptor,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateCommand, GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.OutputContracts,
            GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorldOperation.CreateResult);

    [OperationImplementation("instrument_operations.auto_correspond_closest_point")]
    public override Task<Api.AutoCorrespondClosestPointResult> AutoCorrespondClosestPoint(Api.AutoCorrespondClosestPointRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoCorrespondClosestPointOperation.Descriptor,
            AutoCorrespondClosestPointOperation.CreateCommand, AutoCorrespondClosestPointOperation.OutputContracts,
            AutoCorrespondClosestPointOperation.CreateResult);

    [OperationImplementation("instrument_operations.fabricate_observations")]
    public override Task<Api.FabricateObservationsResult> FabricateObservations(Api.FabricateObservationsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FabricateObservationsOperation.Descriptor,
            FabricateObservationsOperation.CreateCommand, FabricateObservationsOperation.OutputContracts,
            FabricateObservationsOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_weather_setting")]
    public override Task<Api.GetInstrumentWeatherSettingResult> GetInstrumentWeatherSetting(Api.GetInstrumentWeatherSettingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentWeatherSettingOperation.Descriptor,
            GetInstrumentWeatherSettingOperation.CreateCommand, GetInstrumentWeatherSettingOperation.OutputContracts,
            GetInstrumentWeatherSettingOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_current_instrument_position_update")]
    public override Task<Api.GetCurrentInstrumentPositionUpdateResult> GetCurrentInstrumentPositionUpdate(Api.GetCurrentInstrumentPositionUpdateRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCurrentInstrumentPositionUpdateOperation.Descriptor,
            GetCurrentInstrumentPositionUpdateOperation.CreateCommand, GetCurrentInstrumentPositionUpdateOperation.OutputContracts,
            GetCurrentInstrumentPositionUpdateOperation.CreateResult);

    [OperationImplementation("instrument_operations.collimation")]
    public override Task<Api.CollimationResult> Collimation(Api.CollimationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CollimationOperation.Descriptor,
            CollimationOperation.CreateCommand, CollimationOperation.OutputContracts,
            CollimationOperation.CreateResult);

    [OperationImplementation("instrument_operations.align_two_targets_with_axis_wcf_x")]
    public override Task<Api.AlignTwoTargetsWithAxisWcfXResult> AlignTwoTargetsWithAxisWcfX(Api.AlignTwoTargetsWithAxisWcfXRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AlignTwoTargetsWithAxisWcfXOperation.Descriptor,
            AlignTwoTargetsWithAxisWcfXOperation.CreateCommand, AlignTwoTargetsWithAxisWcfXOperation.OutputContracts,
            AlignTwoTargetsWithAxisWcfXOperation.CreateResult);

    [OperationImplementation("instrument_operations.construct_measured_point_uncertainty_ellipsoids")]
    public override Task<Api.ConstructMeasuredPointUncertaintyEllipsoidsResult> ConstructMeasuredPointUncertaintyEllipsoids(Api.ConstructMeasuredPointUncertaintyEllipsoidsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConstructMeasuredPointUncertaintyEllipsoidsOperation.Descriptor,
            ConstructMeasuredPointUncertaintyEllipsoidsOperation.CreateCommand, ConstructMeasuredPointUncertaintyEllipsoidsOperation.OutputContracts,
            ConstructMeasuredPointUncertaintyEllipsoidsOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_probe_offset_frame_online")]
    public override Task<Api.SetProbeOffsetFrameOnlineResult> SetProbeOffsetFrameOnline(Api.SetProbeOffsetFrameOnlineRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetProbeOffsetFrameOnlineOperation.Descriptor,
            SetProbeOffsetFrameOnlineOperation.CreateCommand, SetProbeOffsetFrameOnlineOperation.OutputContracts,
            SetProbeOffsetFrameOnlineOperation.CreateResult);

    [OperationImplementation("instrument_operations.delete_measurement_observation")]
    public override Task<Api.DeleteMeasurementObservationResult> DeleteMeasurementObservation(Api.DeleteMeasurementObservationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteMeasurementObservationOperation.Descriptor,
            DeleteMeasurementObservationOperation.CreateCommand, DeleteMeasurementObservationOperation.OutputContracts,
            DeleteMeasurementObservationOperation.CreateResult);

    [OperationImplementation("instrument_operations.watch_point_to_point_with_view_zooming")]
    public override Task<Api.WatchPointToPointWithViewZoomingResult> WatchPointToPointWithViewZooming(Api.WatchPointToPointWithViewZoomingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WatchPointToPointWithViewZoomingOperation.Descriptor,
            WatchPointToPointWithViewZoomingOperation.CreateCommand, WatchPointToPointWithViewZoomingOperation.OutputContracts,
            WatchPointToPointWithViewZoomingOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_pcmm_instrument_xyz_uncertainties")]
    public override Task<Api.GetPcmmInstrumentXyzUncertaintiesResult> GetPcmmInstrumentXyzUncertainties(Api.GetPcmmInstrumentXyzUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPcmmInstrumentXyzUncertaintiesOperation.Descriptor,
            GetPcmmInstrumentXyzUncertaintiesOperation.CreateCommand, GetPcmmInstrumentXyzUncertaintiesOperation.OutputContracts,
            GetPcmmInstrumentXyzUncertaintiesOperation.CreateResult);

    [OperationImplementation("instrument_operations.measure_existing_single_point")]
    public override Task<Api.MeasureExistingSinglePointResult> MeasureExistingSinglePoint(Api.MeasureExistingSinglePointRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeasureExistingSinglePointOperation.Descriptor,
            MeasureExistingSinglePointOperation.CreateCommand, MeasureExistingSinglePointOperation.OutputContracts,
            MeasureExistingSinglePointOperation.CreateResult);

    [OperationImplementation("instrument_operations.verify_instrument_connection")]
    public override Task<Api.VerifyInstrumentConnectionResult> VerifyInstrumentConnection(Api.VerifyInstrumentConnectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, VerifyInstrumentConnectionOperation.Descriptor,
            VerifyInstrumentConnectionOperation.CreateCommand, VerifyInstrumentConnectionOperation.OutputContracts,
            VerifyInstrumentConnectionOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_base_uncertainty_covariance_matrix_wrt_base")]
    public override Task<Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseResult> SetInstrumentBaseUncertaintyCovarianceMatrixWrtBase(Api.SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseOperation.Descriptor,
            SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseOperation.CreateCommand, SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseOperation.OutputContracts,
            SetInstrumentBaseUncertaintyCovarianceMatrixWrtBaseOperation.CreateResult);

    [OperationImplementation("instrument_operations.construct_mirror_from_plane")]
    public override Task<Api.ConstructMirrorFromPlaneResult> ConstructMirrorFromPlane(Api.ConstructMirrorFromPlaneRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConstructMirrorFromPlaneOperation.Descriptor,
            ConstructMirrorFromPlaneOperation.CreateCommand, ConstructMirrorFromPlaneOperation.OutputContracts, ConstructMirrorFromPlaneOperation.CreateResult);

    [OperationImplementation("instrument_operations.quick_align")]
    public override Task<Api.QuickAlignResult> QuickAlign(Api.QuickAlignRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QuickAlignOperation.Descriptor,
            QuickAlignOperation.CreateCommand, QuickAlignOperation.OutputContracts, QuickAlignOperation.CreateResult);

    [OperationImplementation("instrument_operations.auto_measure_batch_of_features")]
    public override Task<Api.AutoMeasureBatchOfFeaturesResult> AutoMeasureBatchOfFeatures(Api.AutoMeasureBatchOfFeaturesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoMeasureBatchOfFeaturesOperation.Descriptor,
            AutoMeasureBatchOfFeaturesOperation.CreateCommand, AutoMeasureBatchOfFeaturesOperation.OutputContracts,
            AutoMeasureBatchOfFeaturesOperation.CreateResult);

    [OperationImplementation("instrument_operations.wait_for_trapping_to_complete")]
    public override Task<Api.WaitForTrappingToCompleteResult> WaitForTrappingToComplete(Api.WaitForTrappingToCompleteRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WaitForTrappingToCompleteOperation.Descriptor,
            WaitForTrappingToCompleteOperation.CreateCommand, WaitForTrappingToCompleteOperation.OutputContracts,
            WaitForTrappingToCompleteOperation.CreateResult);

    [OperationImplementation("instrument_operations.align_laser_projector")]
    public override Task<Api.AlignLaserProjectorResult> AlignLaserProjector(Api.AlignLaserProjectorRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AlignLaserProjectorOperation.Descriptor,
            AlignLaserProjectorOperation.CreateCommand, AlignLaserProjectorOperation.OutputContracts,
            AlignLaserProjectorOperation.CreateResult);

    [OperationImplementation("instrument_operations.create_new_dynamic_reference")]
    public override Task<Api.CreateNewDynamicReferenceResult> CreateNewDynamicReference(Api.CreateNewDynamicReferenceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreateNewDynamicReferenceOperation.Descriptor,
            CreateNewDynamicReferenceOperation.CreateCommand, CreateNewDynamicReferenceOperation.OutputContracts, CreateNewDynamicReferenceOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_id_from_name")]
    public override Task<Api.GetInstrumentIdFromNameResult> GetInstrumentIdFromName(Api.GetInstrumentIdFromNameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentIdFromNameOperation.Descriptor,
            GetInstrumentIdFromNameOperation.CreateCommand, GetInstrumentIdFromNameOperation.OutputContracts,
            GetInstrumentIdFromNameOperation.CreateResult);

    [OperationImplementation("instrument_operations.make_collection_object_name_ref_list_from_objects_associated_with_instruments")]
    public override Task<Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsResult> MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstruments(Api.MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsOperation.Descriptor,
            MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsOperation.CreateCommand, MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsOperation.OutputContracts,
            MakeCollectionObjectNameRefListFromObjectsAssociatedWithInstrumentsOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_hardware_connect")]
    public override Task<Api.LrHardwareConnectResult> LrHardwareConnect(Api.LrHardwareConnectRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrHardwareConnectOperation.Descriptor,
            LrHardwareConnectOperation.CreateCommand, LrHardwareConnectOperation.OutputContracts,
            LrHardwareConnectOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_scale_factor")]
    public override Task<Api.GetInstrumentScaleFactorResult> GetInstrumentScaleFactor(Api.GetInstrumentScaleFactorRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentScaleFactorOperation.Descriptor,
            GetInstrumentScaleFactorOperation.CreateCommand, GetInstrumentScaleFactorOperation.OutputContracts,
            GetInstrumentScaleFactorOperation.CreateResult);

    [OperationImplementation("instrument_operations.build_target")]
    public override Task<Api.BuildTargetResult> BuildTarget(Api.BuildTargetRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, BuildTargetOperation.Descriptor,
            BuildTargetOperation.CreateCommand, BuildTargetOperation.OutputContracts, BuildTargetOperation.CreateResult);

    [OperationImplementation("instrument_operations.disassociate_objects_from_instrument")]
    public override Task<Api.DisassociateObjectsFromInstrumentResult> DisassociateObjectsFromInstrument(Api.DisassociateObjectsFromInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DisassociateObjectsFromInstrumentOperation.Descriptor,
            DisassociateObjectsFromInstrumentOperation.CreateCommand, DisassociateObjectsFromInstrumentOperation.OutputContracts,
            DisassociateObjectsFromInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_ladar_feature_meas_sphere")]
    public override Task<Api.SetLadarFeatureMeasSphereResult> SetLadarFeatureMeasSphere(Api.SetLadarFeatureMeasSphereRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetLadarFeatureMeasSphereOperation.Descriptor,
            SetLadarFeatureMeasSphereOperation.CreateCommand, SetLadarFeatureMeasSphereOperation.OutputContracts,
            SetLadarFeatureMeasSphereOperation.CreateResult);

    [OperationImplementation("instrument_operations.load_cloud_viewer_point_cloud_file")]
    public override Task<Api.LoadCloudViewerPointCloudFileResult> LoadCloudViewerPointCloudFile(Api.LoadCloudViewerPointCloudFileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LoadCloudViewerPointCloudFileOperation.Descriptor,
            LoadCloudViewerPointCloudFileOperation.CreateCommand, LoadCloudViewerPointCloudFileOperation.OutputContracts,
            LoadCloudViewerPointCloudFileOperation.CreateResult);

    [OperationImplementation("instrument_operations.measure_nominal_feature")]
    public override Task<Api.MeasureNominalFeatureResult> MeasureNominalFeature(Api.MeasureNominalFeatureRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeasureNominalFeatureOperation.Descriptor,
            MeasureNominalFeatureOperation.CreateCommand, MeasureNominalFeatureOperation.OutputContracts,
            MeasureNominalFeatureOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_inspection_verification_mode")]
    public override Task<Api.GetInspectionVerificationModeResult> GetInspectionVerificationMode(Api.GetInspectionVerificationModeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInspectionVerificationModeOperation.Descriptor,
            GetInspectionVerificationModeOperation.CreateCommand, GetInspectionVerificationModeOperation.OutputContracts,
            GetInspectionVerificationModeOperation.CreateResult);

    [OperationImplementation("instrument_operations.auto_correspond_with_proximity_trigger")]
    public override Task<Api.AutoCorrespondWithProximityTriggerResult> AutoCorrespondWithProximityTrigger(Api.AutoCorrespondWithProximityTriggerRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoCorrespondWithProximityTriggerOperation.Descriptor,
            AutoCorrespondWithProximityTriggerOperation.CreateCommand, AutoCorrespondWithProximityTriggerOperation.OutputContracts,
            AutoCorrespondWithProximityTriggerOperation.CreateResult);

    [OperationImplementation("instrument_operations.edge_scan_measurement")]
    public override Task<Api.EdgeScanMeasurementResult> EdgeScanMeasurement(Api.EdgeScanMeasurementRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EdgeScanMeasurementOperation.Descriptor,
            EdgeScanMeasurementOperation.CreateCommand, EdgeScanMeasurementOperation.OutputContracts,
            EdgeScanMeasurementOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_set_red_laser_intensity")]
    public override Task<Api.LrSetRedLaserIntensityResult> LrSetRedLaserIntensity(Api.LrSetRedLaserIntensityRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrSetRedLaserIntensityOperation.Descriptor,
            LrSetRedLaserIntensityOperation.CreateCommand, LrSetRedLaserIntensityOperation.OutputContracts,
            LrSetRedLaserIntensityOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_tracker_edm_theodolite_uncertainties")]
    public override Task<Api.SetTrackerEdmTheodoliteUncertaintiesResult> SetTrackerEdmTheodoliteUncertainties(Api.SetTrackerEdmTheodoliteUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTrackerEdmTheodoliteUncertaintiesOperation.Descriptor,
            SetTrackerEdmTheodoliteUncertaintiesOperation.CreateCommand, SetTrackerEdmTheodoliteUncertaintiesOperation.OutputContracts,
            SetTrackerEdmTheodoliteUncertaintiesOperation.CreateResult);

    [OperationImplementation("instrument_operations.enable_disable_frame_set_scan_mode_all_instruments")]
    public override Task<Api.EnableDisableFrameSetScanModeAllInstrumentsResult> EnableDisableFrameSetScanModeAllInstruments(Api.EnableDisableFrameSetScanModeAllInstrumentsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisableFrameSetScanModeAllInstrumentsOperation.Descriptor,
            EnableDisableFrameSetScanModeAllInstrumentsOperation.CreateCommand, EnableDisableFrameSetScanModeAllInstrumentsOperation.OutputContracts,
            EnableDisableFrameSetScanModeAllInstrumentsOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_target_computation_options")]
    public override Task<Api.SetTargetComputationOptionsResult> SetTargetComputationOptions(Api.SetTargetComputationOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTargetComputationOptionsOperation.Descriptor,
            SetTargetComputationOptionsOperation.CreateCommand, SetTargetComputationOptionsOperation.OutputContracts,
            SetTargetComputationOptionsOperation.CreateResult);

    [OperationImplementation("instrument_operations.measure")]
    public override Task<Api.MeasureResult> Measure(Api.MeasureRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeasureOperation.Descriptor,
            MeasureOperation.CreateCommand, MeasureOperation.OutputContracts, MeasureOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_observation_collimation_shot_options")]
    public override Task<Api.SetObservationCollimationShotOptionsResult> SetObservationCollimationShotOptions(Api.SetObservationCollimationShotOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObservationCollimationShotOptionsOperation.Descriptor,
            SetObservationCollimationShotOptionsOperation.CreateCommand, SetObservationCollimationShotOptionsOperation.OutputContracts,
            SetObservationCollimationShotOptionsOperation.CreateResult);

    [OperationImplementation("instrument_operations.save_instrument_configuration")]
    public override Task<Api.SaveInstrumentConfigurationResult> SaveInstrumentConfiguration(Api.SaveInstrumentConfigurationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SaveInstrumentConfigurationOperation.Descriptor,
            SaveInstrumentConfigurationOperation.CreateCommand, SaveInstrumentConfigurationOperation.OutputContracts,
            SaveInstrumentConfigurationOperation.CreateResult);

    [OperationImplementation("instrument_operations.export_instrument_history_to_xml_file")]
    public override Task<Api.ExportInstrumentHistoryToXmlFileResult> ExportInstrumentHistoryToXmlFile(Api.ExportInstrumentHistoryToXmlFileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportInstrumentHistoryToXmlFileOperation.Descriptor,
            ExportInstrumentHistoryToXmlFileOperation.CreateCommand, ExportInstrumentHistoryToXmlFileOperation.OutputContracts,
            ExportInstrumentHistoryToXmlFileOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_ladar_feature_meas_cylinder")]
    public override Task<Api.SetLadarFeatureMeasCylinderResult> SetLadarFeatureMeasCylinder(Api.SetLadarFeatureMeasCylinderRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetLadarFeatureMeasCylinderOperation.Descriptor,
            SetLadarFeatureMeasCylinderOperation.CreateCommand, SetLadarFeatureMeasCylinderOperation.OutputContracts,
            SetLadarFeatureMeasCylinderOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_self_test_flip_test")]
    public override Task<Api.LrSelfTestFlipTestResult> LrSelfTestFlipTest(Api.LrSelfTestFlipTestRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrSelfTestFlipTestOperation.Descriptor,
            LrSelfTestFlipTestOperation.CreateCommand, LrSelfTestFlipTestOperation.OutputContracts,
            LrSelfTestFlipTestOperation.CreateResult);

    [OperationImplementation("instrument_operations.save_cloud_viewer_point_cloud_file")]
    public override Task<Api.SaveCloudViewerPointCloudFileResult> SaveCloudViewerPointCloudFile(Api.SaveCloudViewerPointCloudFileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SaveCloudViewerPointCloudFileOperation.Descriptor,
            SaveCloudViewerPointCloudFileOperation.CreateCommand, SaveCloudViewerPointCloudFileOperation.OutputContracts,
            SaveCloudViewerPointCloudFileOperation.CreateResult);

    [OperationImplementation("instrument_operations.clear_cloud_viewer")]
    public override Task<Api.ClearCloudViewerResult> ClearCloudViewer(Api.ClearCloudViewerRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ClearCloudViewerOperation.Descriptor,
            ClearCloudViewerOperation.CreateCommand, ClearCloudViewerOperation.OutputContracts,
            ClearCloudViewerOperation.CreateResult);

    [OperationImplementation("instrument_operations.track_tape_measurement")]
    public override Task<Api.TrackTapeMeasurementResult> TrackTapeMeasurement(Api.TrackTapeMeasurementRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TrackTapeMeasurementOperation.Descriptor,
            TrackTapeMeasurementOperation.CreateCommand, TrackTapeMeasurementOperation.OutputContracts,
            TrackTapeMeasurementOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_obscured_points_from_instrument")]
    public override Task<Api.GetObscuredPointsFromInstrumentResult> GetObscuredPointsFromInstrument(Api.GetObscuredPointsFromInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetObscuredPointsFromInstrumentOperation.Descriptor,
            GetObscuredPointsFromInstrumentOperation.CreateCommand, GetObscuredPointsFromInstrumentOperation.OutputContracts,
            GetObscuredPointsFromInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.associate_objects_with_instrument")]
    public override Task<Api.AssociateObjectsWithInstrumentResult> AssociateObjectsWithInstrument(Api.AssociateObjectsWithInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AssociateObjectsWithInstrumentOperation.Descriptor,
            AssociateObjectsWithInstrumentOperation.CreateCommand, AssociateObjectsWithInstrumentOperation.OutputContracts,
            AssociateObjectsWithInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_ladar_auto_meas_sphere")]
    public override Task<Api.SetLadarAutoMeasSphereResult> SetLadarAutoMeasSphere(Api.SetLadarAutoMeasSphereRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetLadarAutoMeasSphereOperation.Descriptor,
            SetLadarAutoMeasSphereOperation.CreateCommand, SetLadarAutoMeasSphereOperation.OutputContracts,
            SetLadarAutoMeasSphereOperation.CreateResult);

    [OperationImplementation("instrument_operations.activate_deactivate_instrument_toolbar")]
    public override Task<Api.ActivateDeactivateInstrumentToolbarResult> ActivateDeactivateInstrumentToolbar(Api.ActivateDeactivateInstrumentToolbarRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ActivateDeactivateInstrumentToolbarOperation.Descriptor,
            ActivateDeactivateInstrumentToolbarOperation.CreateCommand, ActivateDeactivateInstrumentToolbarOperation.OutputContracts,
            ActivateDeactivateInstrumentToolbarOperation.CreateResult);

    [OperationImplementation("instrument_operations.jump_instrument_to_new_location")]
    public override Task<Api.JumpInstrumentToNewLocationResult> JumpInstrumentToNewLocation(Api.JumpInstrumentToNewLocationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, JumpInstrumentToNewLocationOperation.Descriptor,
            JumpInstrumentToNewLocationOperation.CreateCommand, JumpInstrumentToNewLocationOperation.OutputContracts,
            JumpInstrumentToNewLocationOperation.CreateResult);

    [OperationImplementation("instrument_operations.auto_measure_surface_vector_intersections")]
    public override Task<Api.AutoMeasureSurfaceVectorIntersectionsResult> AutoMeasureSurfaceVectorIntersections(Api.AutoMeasureSurfaceVectorIntersectionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoMeasureSurfaceVectorIntersectionsOperation.Descriptor,
            AutoMeasureSurfaceVectorIntersectionsOperation.CreateCommand,
            AutoMeasureSurfaceVectorIntersectionsOperation.OutputContracts,
            AutoMeasureSurfaceVectorIntersectionsOperation.CreateResult);

    [OperationImplementation("instrument_operations.start_gdt_inspection_rehearse")]
    public override Task<Api.StartGdtInspectionRehearseResult> StartGdtInspectionRehearse(Api.StartGdtInspectionRehearseRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartGdtInspectionRehearseOperation.Descriptor,
            StartGdtInspectionRehearseOperation.CreateCommand, StartGdtInspectionRehearseOperation.OutputContracts,
            StartGdtInspectionRehearseOperation.CreateResult);

    [OperationImplementation("instrument_operations.configure_and_measure")]
    public override Task<Api.ConfigureAndMeasureResult> ConfigureAndMeasure(Api.ConfigureAndMeasureRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConfigureAndMeasureOperation.Descriptor,
            ConfigureAndMeasureOperation.CreateCommand, ConfigureAndMeasureOperation.OutputContracts,
            ConfigureAndMeasureOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_targets_and_mode_profiles")]
    public override Task<Api.GetInstrumentTargetsAndModeProfilesResult> GetInstrumentTargetsAndModeProfiles(Api.GetInstrumentTargetsAndModeProfilesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentTargetsAndModeProfilesOperation.Descriptor,
            GetInstrumentTargetsAndModeProfilesOperation.CreateCommand, GetInstrumentTargetsAndModeProfilesOperation.OutputContracts,
            GetInstrumentTargetsAndModeProfilesOperation.CreateResult);

    [OperationImplementation("instrument_operations.move_instrument_to_another_collection")]
    public override Task<Api.MoveInstrumentToAnotherCollectionResult> MoveInstrumentToAnotherCollection(Api.MoveInstrumentToAnotherCollectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveInstrumentToAnotherCollectionOperation.Descriptor,
            MoveInstrumentToAnotherCollectionOperation.CreateCommand, MoveInstrumentToAnotherCollectionOperation.OutputContracts,
            MoveInstrumentToAnotherCollectionOperation.CreateResult);

    [OperationImplementation("instrument_operations.measure_single_point_here")]
    public override Task<Api.MeasureSinglePointHereResult> MeasureSinglePointHere(Api.MeasureSinglePointHereRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeasureSinglePointHereOperation.Descriptor,
            MeasureSinglePointHereOperation.CreateCommand, MeasureSinglePointHereOperation.OutputContracts,
            MeasureSinglePointHereOperation.CreateResult);

    [OperationImplementation("instrument_operations.issue_instrument_actuator_command")]
    public override Task<Api.IssueInstrumentActuatorCommandResult> IssueInstrumentActuatorCommand(Api.IssueInstrumentActuatorCommandRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, IssueInstrumentActuatorCommandOperation.Descriptor,
            IssueInstrumentActuatorCommandOperation.CreateCommand, IssueInstrumentActuatorCommandOperation.OutputContracts,
            IssueInstrumentActuatorCommandOperation.CreateResult);

    [OperationImplementation("instrument_operations.add_new_instrument")]
    public override Task<Api.AddNewInstrumentResult> AddNewInstrument(Api.AddNewInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddNewInstrumentOperation.Descriptor,
            AddNewInstrumentOperation.CreateCommand, AddNewInstrumentOperation.OutputContracts,
            AddNewInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_inspection_verification_mode")]
    public override Task<Api.SetInspectionVerificationModeResult> SetInspectionVerificationMode(Api.SetInspectionVerificationModeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInspectionVerificationModeOperation.Descriptor,
            SetInspectionVerificationModeOperation.CreateCommand, SetInspectionVerificationModeOperation.OutputContracts,
            SetInspectionVerificationModeOperation.CreateResult);

    [OperationImplementation("instrument_operations.watch_closest_point")]
    public override Task<Api.WatchClosestPointResult> WatchClosestPoint(Api.WatchClosestPointRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WatchClosestPointOperation.Descriptor,
            WatchClosestPointOperation.CreateCommand, WatchClosestPointOperation.OutputContracts,
            WatchClosestPointOperation.CreateResult);

    [OperationImplementation("instrument_operations.transform_multiple_instruments_by_delta")]
    public override Task<Api.TransformMultipleInstrumentsByDeltaResult> TransformMultipleInstrumentsByDelta(Api.TransformMultipleInstrumentsByDeltaRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TransformMultipleInstrumentsByDeltaOperation.Descriptor,
            TransformMultipleInstrumentsByDeltaOperation.CreateCommand, TransformMultipleInstrumentsByDeltaOperation.OutputContracts,
            TransformMultipleInstrumentsByDeltaOperation.CreateResult);

    [OperationImplementation("instrument_operations.guide_objects_in_6d_based_on_point_measurements")]
    public override Task<Api.GuideObjectsIn6dBasedOnPointMeasurementsResult> GuideObjectsIn6dBasedOnPointMeasurements(Api.GuideObjectsIn6dBasedOnPointMeasurementsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GuideObjectsIn6dBasedOnPointMeasurementsOperation.Descriptor,
            GuideObjectsIn6dBasedOnPointMeasurementsOperation.CreateCommand,
            GuideObjectsIn6dBasedOnPointMeasurementsOperation.OutputContracts,
            GuideObjectsIn6dBasedOnPointMeasurementsOperation.CreateResult);

    [OperationImplementation("instrument_operations.locate_instrument_ref_tie_in")]
    public override Task<Api.LocateInstrumentRefTieInResult> LocateInstrumentRefTieIn(Api.LocateInstrumentRefTieInRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LocateInstrumentRefTieInOperation.Descriptor,
            LocateInstrumentRefTieInOperation.CreateCommand, LocateInstrumentRefTieInOperation.OutputContracts,
            LocateInstrumentRefTieInOperation.CreateResult);

    [OperationImplementation("instrument_operations.drift_check")]
    public override Task<Api.DriftCheckResult> DriftCheck(Api.DriftCheckRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DriftCheckOperation.Descriptor,
            DriftCheckOperation.CreateCommand, DriftCheckOperation.OutputContracts,
            DriftCheckOperation.CreateResult);

    [OperationImplementation("instrument_operations.construct_perimeters_from_surface_face_list")]
    public override Task<Api.ConstructPerimetersFromSurfaceFaceListResult> ConstructPerimetersFromSurfaceFaceList(Api.ConstructPerimetersFromSurfaceFaceListRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConstructPerimetersFromSurfaceFaceListOperation.Descriptor,
            ConstructPerimetersFromSurfaceFaceListOperation.CreateCommand, ConstructPerimetersFromSurfaceFaceListOperation.OutputContracts, ConstructPerimetersFromSurfaceFaceListOperation.CreateResult);

    [OperationImplementation("instrument_operations.dissect_point_group")]
    public override Task<Api.DissectPointGroupResult> DissectPointGroup(Api.DissectPointGroupRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DissectPointGroupOperation.Descriptor,
            DissectPointGroupOperation.CreateCommand, DissectPointGroupOperation.OutputContracts, DissectPointGroupOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_self_test")]
    public override Task<Api.LrSelfTestResult> LrSelfTest(Api.LrSelfTestRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrSelfTestOperation.Descriptor,
            LrSelfTestOperation.CreateCommand, LrSelfTestOperation.OutputContracts,
            LrSelfTestOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_last_instrument_index")]
    public override Task<Api.GetLastInstrumentIndexResult> GetLastInstrumentIndex(Api.GetLastInstrumentIndexRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetLastInstrumentIndexOperation.Descriptor,
            GetLastInstrumentIndexOperation.CreateCommand, GetLastInstrumentIndexOperation.OutputContracts,
            GetLastInstrumentIndexOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_absolute_instrument_scale_factor")]
    public override Task<Api.SetAbsoluteInstrumentScaleFactorResult> SetAbsoluteInstrumentScaleFactor(Api.SetAbsoluteInstrumentScaleFactorRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetAbsoluteInstrumentScaleFactorOperation.Descriptor,
            SetAbsoluteInstrumentScaleFactorOperation.CreateCommand, SetAbsoluteInstrumentScaleFactorOperation.OutputContracts,
            SetAbsoluteInstrumentScaleFactorOperation.CreateResult);

    [OperationImplementation("instrument_operations.send_cloud_to_sa")]
    public override Task<Api.SendCloudToSaResult> SendCloudToSa(Api.SendCloudToSaRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SendCloudToSaOperation.Descriptor,
            SendCloudToSaOperation.CreateCommand, SendCloudToSaOperation.OutputContracts,
            SendCloudToSaOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_estimated_scan_time")]
    public override Task<Api.GetEstimatedScanTimeResult> GetEstimatedScanTime(Api.GetEstimatedScanTimeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetEstimatedScanTimeOperation.Descriptor,
            GetEstimatedScanTimeOperation.CreateCommand, GetEstimatedScanTimeOperation.OutputContracts,
            GetEstimatedScanTimeOperation.CreateResult);

    [OperationImplementation("instrument_operations.locate_instrument_best_fit_group_to_group")]
    public override Task<Api.LocateInstrumentBestFitGroupToGroupResult> LocateInstrumentBestFitGroupToGroup(Api.LocateInstrumentBestFitGroupToGroupRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LocateInstrumentBestFitGroupToGroupOperation.Descriptor,
            LocateInstrumentBestFitGroupToGroupOperation.CreateCommand, LocateInstrumentBestFitGroupToGroupOperation.OutputContracts,
            LocateInstrumentBestFitGroupToGroupOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instruments_with_observations_on_target")]
    public override Task<Api.GetInstrumentsWithObservationsOnTargetResult> GetInstrumentsWithObservationsOnTarget(Api.GetInstrumentsWithObservationsOnTargetRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentsWithObservationsOnTargetOperation.Descriptor,
            GetInstrumentsWithObservationsOnTargetOperation.CreateCommand, GetInstrumentsWithObservationsOnTargetOperation.OutputContracts,
            GetInstrumentsWithObservationsOnTargetOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_measurement_mode_profile")]
    public override Task<Api.SetInstrumentMeasurementModeProfileResult> SetInstrumentMeasurementModeProfile(Api.SetInstrumentMeasurementModeProfileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentMeasurementModeProfileOperation.Descriptor,
            SetInstrumentMeasurementModeProfileOperation.CreateCommand, SetInstrumentMeasurementModeProfileOperation.OutputContracts,
            SetInstrumentMeasurementModeProfileOperation.CreateResult);

    [OperationImplementation("instrument_operations.watch_point_to_edge")]
    public override Task<Api.WatchPointToEdgeResult> WatchPointToEdge(Api.WatchPointToEdgeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, WatchPointToEdgeOperation.Descriptor,
            WatchPointToEdgeOperation.CreateCommand, WatchPointToEdgeOperation.OutputContracts,
            WatchPointToEdgeOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_number_of_observations_on_target")]
    public override Task<Api.GetNumberOfObservationsOnTargetResult> GetNumberOfObservationsOnTarget(Api.GetNumberOfObservationsOnTargetRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfObservationsOnTargetOperation.Descriptor,
            GetNumberOfObservationsOnTargetOperation.CreateCommand,
            GetNumberOfObservationsOnTargetOperation.OutputContracts,
            GetNumberOfObservationsOnTargetOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_multiply_instrument_scale_factor")]
    public override Task<Api.SetMultiplyInstrumentScaleFactorResult> SetMultiplyInstrumentScaleFactor(Api.SetMultiplyInstrumentScaleFactorRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetMultiplyInstrumentScaleFactorOperation.Descriptor,
            SetMultiplyInstrumentScaleFactorOperation.CreateCommand, SetMultiplyInstrumentScaleFactorOperation.OutputContracts,
            SetMultiplyInstrumentScaleFactorOperation.CreateResult);

    [OperationImplementation("instrument_operations.edit_scan_perimeter_profile")]
    public override Task<Api.EditScanPerimeterProfileResult> EditScanPerimeterProfile(Api.EditScanPerimeterProfileRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EditScanPerimeterProfileOperation.Descriptor,
            EditScanPerimeterProfileOperation.CreateCommand, EditScanPerimeterProfileOperation.OutputContracts,
            EditScanPerimeterProfileOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_transform")]
    public override Task<Api.GetInstrumentTransformResult> GetInstrumentTransform(Api.GetInstrumentTransformRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentTransformOperation.Descriptor,
            GetInstrumentTransformOperation.CreateCommand, GetInstrumentTransformOperation.OutputContracts,
            GetInstrumentTransformOperation.CreateResult);

    [OperationImplementation("instrument_operations.auto_measure_vectors")]
    public override Task<Api.AutoMeasureVectorsResult> AutoMeasureVectors(Api.AutoMeasureVectorsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoMeasureVectorsOperation.Descriptor,
            AutoMeasureVectorsOperation.CreateCommand, AutoMeasureVectorsOperation.OutputContracts,
            AutoMeasureVectorsOperation.CreateResult);

    [OperationImplementation("instrument_operations.multi_measurement_stop")]
    public override Task<Api.MultiMeasurementStopResult> MultiMeasurementStop(Api.MultiMeasurementStopRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MultiMeasurementStopOperation.Descriptor,
            MultiMeasurementStopOperation.CreateCommand, MultiMeasurementStopOperation.OutputContracts,
            MultiMeasurementStopOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_observation_status")]
    public override Task<Api.SetObservationStatusResult> SetObservationStatus(Api.SetObservationStatusRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObservationStatusOperation.Descriptor,
            SetObservationStatusOperation.CreateCommand, SetObservationStatusOperation.OutputContracts,
            SetObservationStatusOperation.CreateResult);

    [OperationImplementation("instrument_operations.start_gdt_inspection_design")]
    public override Task<Api.StartGdtInspectionDesignResult> StartGdtInspectionDesign(Api.StartGdtInspectionDesignRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartGdtInspectionDesignOperation.Descriptor,
            StartGdtInspectionDesignOperation.CreateCommand, StartGdtInspectionDesignOperation.OutputContracts,
            StartGdtInspectionDesignOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_part_temperature")]
    public override Task<Api.GetInstrumentPartTemperatureResult> GetInstrumentPartTemperature(Api.GetInstrumentPartTemperatureRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentPartTemperatureOperation.Descriptor,
            GetInstrumentPartTemperatureOperation.CreateCommand, GetInstrumentPartTemperatureOperation.OutputContracts,
            GetInstrumentPartTemperatureOperation.CreateResult);

    [OperationImplementation("instrument_operations.initiate_servo_guide")]
    public override Task<Api.InitiateServoGuideResult> InitiateServoGuide(Api.InitiateServoGuideRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, InitiateServoGuideOperation.Descriptor,
            InitiateServoGuideOperation.CreateCommand, InitiateServoGuideOperation.OutputContracts,
            InitiateServoGuideOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_targets_measured_by_instrument")]
    public override Task<Api.GetTargetsMeasuredByInstrumentResult> GetTargetsMeasuredByInstrument(Api.GetTargetsMeasuredByInstrumentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTargetsMeasuredByInstrumentOperation.Descriptor,
            GetTargetsMeasuredByInstrumentOperation.CreateCommand, GetTargetsMeasuredByInstrumentOperation.OutputContracts,
            GetTargetsMeasuredByInstrumentOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_instrument_targeting")]
    public override Task<Api.GetInstrumentTargetingResult> GetInstrumentTargeting(Api.GetInstrumentTargetingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetInstrumentTargetingOperation.Descriptor,
            GetInstrumentTargetingOperation.CreateCommand, GetInstrumentTargetingOperation.OutputContracts,
            GetInstrumentTargetingOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_xyz_instrument_uncertainties")]
    public override Task<Api.SetXyzInstrumentUncertaintiesResult> SetXyzInstrumentUncertainties(Api.SetXyzInstrumentUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetXyzInstrumentUncertaintiesOperation.Descriptor,
            SetXyzInstrumentUncertaintiesOperation.CreateCommand, SetXyzInstrumentUncertaintiesOperation.OutputContracts,
            SetXyzInstrumentUncertaintiesOperation.CreateResult);

    [OperationImplementation("instrument_operations.calculate_tcp_fixture_uncertainties")]
    public override Task<Api.CalculateTcpFixtureUncertaintiesResult> CalculateTcpFixtureUncertainties(Api.CalculateTcpFixtureUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CalculateTcpFixtureUncertaintiesOperation.Descriptor,
            CalculateTcpFixtureUncertaintiesOperation.CreateCommand, CalculateTcpFixtureUncertaintiesOperation.OutputContracts,
            CalculateTcpFixtureUncertaintiesOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_weather_setting")]
    public override Task<Api.SetInstrumentWeatherSettingResult> SetInstrumentWeatherSetting(Api.SetInstrumentWeatherSettingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentWeatherSettingOperation.Descriptor,
            SetInstrumentWeatherSettingOperation.CreateCommand, SetInstrumentWeatherSettingOperation.OutputContracts,
            SetInstrumentWeatherSettingOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_xyz_reference_frame_instrument_base_anchor_frame")]
    public override Task<Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameResult> SetXyzReferenceFrameInstrumentBaseAnchorFrame(Api.SetXyzReferenceFrameInstrumentBaseAnchorFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetXyzReferenceFrameInstrumentBaseAnchorFrameOperation.Descriptor,
            SetXyzReferenceFrameInstrumentBaseAnchorFrameOperation.CreateCommand, SetXyzReferenceFrameInstrumentBaseAnchorFrameOperation.OutputContracts,
            SetXyzReferenceFrameInstrumentBaseAnchorFrameOperation.CreateResult);

    [OperationImplementation("instrument_operations.scan_within_perimeter")]
    public override Task<Api.ScanWithinPerimeterResult> ScanWithinPerimeter(Api.ScanWithinPerimeterRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ScanWithinPerimeterOperation.Descriptor,
            ScanWithinPerimeterOperation.CreateCommand, ScanWithinPerimeterOperation.OutputContracts,
            ScanWithinPerimeterOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_apdis_get_active_mcm_calibration")]
    public override Task<Api.LrApdisGetActiveMcmCalibrationResult> LrApdisGetActiveMcmCalibration(Api.LrApdisGetActiveMcmCalibrationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrApdisGetActiveMcmCalibrationOperation.Descriptor,
            LrApdisGetActiveMcmCalibrationOperation.CreateCommand, LrApdisGetActiveMcmCalibrationOperation.OutputContracts,
            LrApdisGetActiveMcmCalibrationOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_ladar_auto_meas_point")]
    public override Task<Api.SetLadarAutoMeasPointResult> SetLadarAutoMeasPoint(Api.SetLadarAutoMeasPointRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetLadarAutoMeasPointOperation.Descriptor,
            SetLadarAutoMeasPointOperation.CreateCommand, SetLadarAutoMeasPointOperation.OutputContracts,
            SetLadarAutoMeasPointOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_xyz_instrument_uncertainties")]
    public override Task<Api.GetXyzInstrumentUncertaintiesResult> GetXyzInstrumentUncertainties(Api.GetXyzInstrumentUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetXyzInstrumentUncertaintiesOperation.Descriptor,
            GetXyzInstrumentUncertaintiesOperation.CreateCommand, GetXyzInstrumentUncertaintiesOperation.OutputContracts,
            GetXyzInstrumentUncertaintiesOperation.CreateResult);

    [OperationImplementation("instrument_operations.start_instrument_interface")]
    public override Task<Api.StartInstrumentInterfaceResult> StartInstrumentInterface(Api.StartInstrumentInterfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartInstrumentInterfaceOperation.Descriptor,
            StartInstrumentInterfaceOperation.CreateCommand, StartInstrumentInterfaceOperation.OutputContracts,
            StartInstrumentInterfaceOperation.CreateResult);

    [OperationImplementation("instrument_operations.start_theodolite_interface")]
    public override Task<Api.StartTheodoliteInterfaceResult> StartTheodoliteInterface(Api.StartTheodoliteInterfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartTheodoliteInterfaceOperation.Descriptor,
            StartTheodoliteInterfaceOperation.CreateCommand, StartTheodoliteInterfaceOperation.OutputContracts,
            StartTheodoliteInterfaceOperation.CreateResult);

    [OperationImplementation("instrument_operations.enable_disable_point_set_scan_mode")]
    public override Task<Api.EnableDisablePointSetScanModeResult> EnableDisablePointSetScanMode(Api.EnableDisablePointSetScanModeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisablePointSetScanModeOperation.Descriptor,
            EnableDisablePointSetScanModeOperation.CreateCommand, EnableDisablePointSetScanModeOperation.OutputContracts,
            EnableDisablePointSetScanModeOperation.CreateResult);

    [OperationImplementation("instrument_operations.create_templated_instrument_usmn")]
    public override Task<Api.CreateTemplatedInstrumentUsmnResult> CreateTemplatedInstrumentUsmn(Api.CreateTemplatedInstrumentUsmnRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreateTemplatedInstrumentUsmnOperation.Descriptor,
            CreateTemplatedInstrumentUsmnOperation.CreateCommand, CreateTemplatedInstrumentUsmnOperation.OutputContracts, CreateTemplatedInstrumentUsmnOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_instrument_transform")]
    public override Task<Api.SetInstrumentTransformResult> SetInstrumentTransform(Api.SetInstrumentTransformRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInstrumentTransformOperation.Descriptor,
            SetInstrumentTransformOperation.CreateCommand, SetInstrumentTransformOperation.OutputContracts,
            SetInstrumentTransformOperation.CreateResult);

    [OperationImplementation("instrument_operations.stop_active_measurement_mode")]
    public override Task<Api.StopActiveMeasurementModeResult> StopActiveMeasurementMode(Api.StopActiveMeasurementModeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StopActiveMeasurementModeOperation.Descriptor,
            StopActiveMeasurementModeOperation.CreateCommand, StopActiveMeasurementModeOperation.OutputContracts,
            StopActiveMeasurementModeOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_self_test_lo_sep")]
    public override Task<Api.LrSelfTestLoSepResult> LrSelfTestLoSep(Api.LrSelfTestLoSepRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrSelfTestLoSepOperation.Descriptor,
            LrSelfTestLoSepOperation.CreateCommand, LrSelfTestLoSepOperation.OutputContracts,
            LrSelfTestLoSepOperation.CreateResult);

    [OperationImplementation("instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix")]
    public override Task<Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixResult> GetLastSolvedTcpFixtureUncertaintyCovarianceMatrix(Api.GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.Descriptor,
            GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.CreateCommand,
            GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.OutputContracts,
            GetLastSolvedTcpFixtureUncertaintyCovarianceMatrixOperation.CreateResult);

    [OperationImplementation("instrument_operations.measure_existing_single_point_and_compare")]
    public override Task<Api.MeasureExistingSinglePointAndCompareResult> MeasureExistingSinglePointAndCompare(Api.MeasureExistingSinglePointAndCompareRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeasureExistingSinglePointAndCompareOperation.Descriptor,
            MeasureExistingSinglePointAndCompareOperation.CreateCommand,
            MeasureExistingSinglePointAndCompareOperation.OutputContracts,
            MeasureExistingSinglePointAndCompareOperation.CreateResult);

    [OperationImplementation("instrument_operations.set_probe_offset_frame_offline")]
    public override Task<Api.SetProbeOffsetFrameOfflineResult> SetProbeOffsetFrameOffline(Api.SetProbeOffsetFrameOfflineRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetProbeOffsetFrameOfflineOperation.Descriptor,
            SetProbeOffsetFrameOfflineOperation.CreateCommand, SetProbeOffsetFrameOfflineOperation.OutputContracts,
            SetProbeOffsetFrameOfflineOperation.CreateResult);

    [OperationImplementation("instrument_operations.locate_instrument_group_to_surface_quick_fit")]
    public override Task<Api.LocateInstrumentGroupToSurfaceQuickFitResult> LocateInstrumentGroupToSurfaceQuickFit(Api.LocateInstrumentGroupToSurfaceQuickFitRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LocateInstrumentGroupToSurfaceQuickFitOperation.Descriptor,
            LocateInstrumentGroupToSurfaceQuickFitOperation.CreateCommand, LocateInstrumentGroupToSurfaceQuickFitOperation.OutputContracts,
            LocateInstrumentGroupToSurfaceQuickFitOperation.CreateResult);

    [OperationImplementation("instrument_operations.lr_get_most_recent_snr_info")]
    public override Task<Api.LrGetMostRecentSnrInfoResult> LrGetMostRecentSnrInfo(Api.LrGetMostRecentSnrInfoRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LrGetMostRecentSnrInfoOperation.Descriptor,
            LrGetMostRecentSnrInfoOperation.CreateCommand, LrGetMostRecentSnrInfoOperation.OutputContracts,
            LrGetMostRecentSnrInfoOperation.CreateResult);

    [OperationImplementation("instrument_operations.measure_existing_single_point_manual_guide")]
    public override Task<Api.MeasureExistingSinglePointManualGuideResult> MeasureExistingSinglePointManualGuide(Api.MeasureExistingSinglePointManualGuideRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeasureExistingSinglePointManualGuideOperation.Descriptor,
            MeasureExistingSinglePointManualGuideOperation.CreateCommand,
            MeasureExistingSinglePointManualGuideOperation.OutputContracts,
            MeasureExistingSinglePointManualGuideOperation.CreateResult);

    [OperationImplementation("instrument_operations.instrument_operational_check")]
    public override Task<Api.InstrumentOperationalCheckResult> InstrumentOperationalCheck(Api.InstrumentOperationalCheckRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, InstrumentOperationalCheckOperation.Descriptor,
            InstrumentOperationalCheckOperation.CreateCommand, InstrumentOperationalCheckOperation.OutputContracts,
            InstrumentOperationalCheckOperation.CreateResult);

    [OperationImplementation("instrument_operations.close_auto_correspond_closest_point_dialog")]
    public override Task<Api.CloseAutoCorrespondClosestPointDialogResult> CloseAutoCorrespondClosestPointDialog(Api.CloseAutoCorrespondClosestPointDialogRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CloseAutoCorrespondClosestPointDialogOperation.Descriptor,
            CloseAutoCorrespondClosestPointDialogOperation.CreateCommand, CloseAutoCorrespondClosestPointDialogOperation.OutputContracts,
            CloseAutoCorrespondClosestPointDialogOperation.CreateResult);

}
