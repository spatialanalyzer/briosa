using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal sealed class AnalysisOperationsService(OperationExecutor executor)
    : Api.AnalysisOperations.AnalysisOperationsBase
{
    [OperationImplementation("analysis_operations.angle_between_line_and_plane")]
    public override Task<Api.AngleBetweenLineAndPlaneResult> AngleBetweenLineAndPlane(
        Api.AngleBetweenLineAndPlaneRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AngleBetweenLineAndPlaneRequest, Api.AngleBetweenLineAndPlaneResult>(
            executor,
            request,
            context,
            "analysis_operations.angle_between_line_and_plane");

    [OperationImplementation("analysis_operations.angle_between_two_lines")]
    public override Task<Api.AngleBetweenTwoLinesResult> AngleBetweenTwoLines(
        Api.AngleBetweenTwoLinesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AngleBetweenTwoLinesRequest, Api.AngleBetweenTwoLinesResult>(
            executor,
            request,
            context,
            "analysis_operations.angle_between_two_lines");

    [OperationImplementation("analysis_operations.angle_between_two_planes_normals")]
    public override Task<Api.AngleBetweenTwoPlanesNormalsResult> AngleBetweenTwoPlanesNormals(
        Api.AngleBetweenTwoPlanesNormalsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AngleBetweenTwoPlanesNormalsRequest, Api.AngleBetweenTwoPlanesNormalsResult>(
            executor,
            request,
            context,
            "analysis_operations.angle_between_two_planes_normals");

    [OperationImplementation("analysis_operations.best_fit_transformation_group_to_group")]
    public override Task<Api.BestFitTransformationGroupToGroupResult> BestFitTransformationGroupToGroup(
        Api.BestFitTransformationGroupToGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.BestFitTransformationGroupToGroupRequest, Api.BestFitTransformationGroupToGroupResult>(
            executor,
            request,
            context,
            "analysis_operations.best_fit_transformation_group_to_group");

    [OperationImplementation("analysis_operations.compute_group_to_group_orientation_rx_ry_rz")]
    public override Task<Api.ComputeGroupToGroupOrientationRxRyRzResult> ComputeGroupToGroupOrientationRxRyRz(
        Api.ComputeGroupToGroupOrientationRxRyRzRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ComputeGroupToGroupOrientationRxRyRzRequest, Api.ComputeGroupToGroupOrientationRxRyRzResult>(
            executor,
            request,
            context,
            "analysis_operations.compute_group_to_group_orientation_rx_ry_rz");

    [OperationImplementation("analysis_operations.create_point_uncertainty_cloud_point_sets")]
    public override Task<Api.CreatePointUncertaintyCloudPointSetsResult> CreatePointUncertaintyCloudPointSets(
        Api.CreatePointUncertaintyCloudPointSetsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreatePointUncertaintyCloudPointSetsRequest, Api.CreatePointUncertaintyCloudPointSetsResult>(
            executor,
            request,
            context,
            "analysis_operations.create_point_uncertainty_cloud_point_sets");

    [OperationImplementation("analysis_operations.create_point_uncertainty_fields")]
    public override Task<Api.CreatePointUncertaintyFieldsResult> CreatePointUncertaintyFields(
        Api.CreatePointUncertaintyFieldsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreatePointUncertaintyFieldsRequest, Api.CreatePointUncertaintyFieldsResult>(
            executor,
            request,
            context,
            "analysis_operations.create_point_uncertainty_fields");

    [OperationImplementation("analysis_operations.fit_geometry_to_point_group")]
    public override Task<Api.FitGeometryToPointGroupResult> FitGeometryToPointGroup(
        Api.FitGeometryToPointGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FitGeometryToPointGroupRequest, Api.FitGeometryToPointGroupResult>(
            executor,
            request,
            context,
            "analysis_operations.fit_geometry_to_point_group");

    [OperationImplementation("analysis_operations.fit_geometry_to_point_group_projected_to_plane")]
    public override Task<Api.FitGeometryToPointGroupProjectedToPlaneResult> FitGeometryToPointGroupProjectedToPlane(
        Api.FitGeometryToPointGroupProjectedToPlaneRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FitGeometryToPointGroupProjectedToPlaneRequest, Api.FitGeometryToPointGroupProjectedToPlaneResult>(
            executor,
            request,
            context,
            "analysis_operations.fit_geometry_to_point_group_projected_to_plane");

    [OperationImplementation("analysis_operations.fit_geometry_to_points")]
    public override Task<Api.FitGeometryToPointsResult> FitGeometryToPoints(
        Api.FitGeometryToPointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FitGeometryToPointsRequest, Api.FitGeometryToPointsResult>(
            executor,
            request,
            context,
            "analysis_operations.fit_geometry_to_points");

    [OperationImplementation("analysis_operations.get_bspline_properties")]
    public override Task<Api.GetBSplinePropertiesResult> GetBSplineProperties(
        Api.GetBSplinePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetBSplinePropertiesRequest, Api.GetBSplinePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_bspline_properties");

    [OperationImplementation("analysis_operations.get_circle_properties")]
    public override Task<Api.GetCirclePropertiesResult> GetCircleProperties(
        Api.GetCirclePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCirclePropertiesRequest, Api.GetCirclePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_circle_properties");

    [OperationImplementation("analysis_operations.get_cone_properties")]
    public override Task<Api.GetConePropertiesResult> GetConeProperties(
        Api.GetConePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetConePropertiesRequest, Api.GetConePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_cone_properties");

    [OperationImplementation("analysis_operations.get_coordinate_for_ith_point_in_point_set")]
    public override Task<Api.GetCoordinateForIthPointInPointSetResult> GetCoordinateForIthPointInPointSet(
        Api.GetCoordinateForIthPointInPointSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCoordinateForIthPointInPointSetRequest, Api.GetCoordinateForIthPointInPointSetResult>(
            executor,
            request,
            context,
            "analysis_operations.get_coordinate_for_ith_point_in_point_set");

    [OperationImplementation("analysis_operations.get_cylinder_properties")]
    public override Task<Api.GetCylinderPropertiesResult> GetCylinderProperties(
        Api.GetCylinderPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCylinderPropertiesRequest, Api.GetCylinderPropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_cylinder_properties");

    [OperationImplementation("analysis_operations.get_ellipse_properties")]
    public override Task<Api.GetEllipsePropertiesResult> GetEllipseProperties(
        Api.GetEllipsePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetEllipsePropertiesRequest, Api.GetEllipsePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_ellipse_properties");

    [OperationImplementation("analysis_operations.get_euler_parameters_for_frame")]
    public override Task<Api.GetEulerParametersForFrameResult> GetEulerParametersForFrame(
        Api.GetEulerParametersForFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetEulerParametersForFrameRequest, Api.GetEulerParametersForFrameResult>(
            executor,
            request,
            context,
            "analysis_operations.get_euler_parameters_for_frame");

    [OperationImplementation("analysis_operations.get_euler_parameters_for_ith_frame_in_frame_set")]
    public override Task<Api.GetEulerParametersForIthFrameInFrameSetResult> GetEulerParametersForIthFrameInFrameSet(
        Api.GetEulerParametersForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetEulerParametersForIthFrameInFrameSetRequest, Api.GetEulerParametersForIthFrameInFrameSetResult>(
            executor,
            request,
            context,
            "analysis_operations.get_euler_parameters_for_ith_frame_in_frame_set");

    [OperationImplementation("analysis_operations.get_ith_collection_name")]
    public override Task<Api.GetIthCollectionNameResult> GetIthCollectionName(
        Api.GetIthCollectionNameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetIthCollectionNameRequest, Api.GetIthCollectionNameResult>(
            executor,
            request,
            context,
            "analysis_operations.get_ith_collection_name");

    [OperationImplementation("analysis_operations.get_ith_point_from_group")]
    public override Task<Api.GetIthPointFromGroupResult> GetIthPointFromGroup(
        Api.GetIthPointFromGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetIthPointFromGroupRequest, Api.GetIthPointFromGroupResult>(
            executor,
            request,
            context,
            "analysis_operations.get_ith_point_from_group");

    [OperationImplementation("analysis_operations.get_line_properties")]
    public override Task<Api.GetLinePropertiesResult> GetLineProperties(
        Api.GetLinePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetLinePropertiesRequest, Api.GetLinePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_line_properties");

    [OperationImplementation("analysis_operations.get_measurement_auxiliary_data")]
    public override Task<Api.GetMeasurementAuxiliaryDataResult> GetMeasurementAuxiliaryData(
        Api.GetMeasurementAuxiliaryDataRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetMeasurementAuxiliaryDataRequest, Api.GetMeasurementAuxiliaryDataResult>(
            executor,
            request,
            context,
            "analysis_operations.get_measurement_auxiliary_data");

    [OperationImplementation("analysis_operations.get_measurement_info_data")]
    public override Task<Api.GetMeasurementInfoDataResult> GetMeasurementInfoData(
        Api.GetMeasurementInfoDataRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetMeasurementInfoDataRequest, Api.GetMeasurementInfoDataResult>(
            executor,
            request,
            context,
            "analysis_operations.get_measurement_info_data");

    [OperationImplementation("analysis_operations.get_measurement_weather_data")]
    public override Task<Api.GetMeasurementWeatherDataResult> GetMeasurementWeatherData(
        Api.GetMeasurementWeatherDataRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetMeasurementWeatherDataRequest, Api.GetMeasurementWeatherDataResult>(
            executor,
            request,
            context,
            "analysis_operations.get_measurement_weather_data");

    [OperationImplementation("analysis_operations.get_number_of_collections")]
    public override Task<Api.GetNumberOfCollectionsResult> GetNumberOfCollections(
        Api.GetNumberOfCollectionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfCollectionsRequest, Api.GetNumberOfCollectionsResult>(
            executor,
            request,
            context,
            "analysis_operations.get_number_of_collections");

    [OperationImplementation("analysis_operations.get_number_of_frames_in_frame_set")]
    public override Task<Api.GetNumberOfFramesInFrameSetResult> GetNumberOfFramesInFrameSet(
        Api.GetNumberOfFramesInFrameSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfFramesInFrameSetRequest, Api.GetNumberOfFramesInFrameSetResult>(
            executor,
            request,
            context,
            "analysis_operations.get_number_of_frames_in_frame_set");

    [OperationImplementation("analysis_operations.get_number_of_points_in_group")]
    public override Task<Api.GetNumberOfPointsInGroupResult> GetNumberOfPointsInGroup(
        Api.GetNumberOfPointsInGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfPointsInGroupRequest, Api.GetNumberOfPointsInGroupResult>(
            executor,
            request,
            context,
            "analysis_operations.get_number_of_points_in_group");

    [OperationImplementation("analysis_operations.get_number_of_points_in_point_set")]
    public override Task<Api.GetNumberOfPointsInPointSetResult> GetNumberOfPointsInPointSet(
        Api.GetNumberOfPointsInPointSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfPointsInPointSetRequest, Api.GetNumberOfPointsInPointSetResult>(
            executor,
            request,
            context,
            "analysis_operations.get_number_of_points_in_point_set");

    [OperationImplementation("analysis_operations.get_object_reporting_frame")]
    public override Task<Api.GetObjectReportingFrameResult> GetObjectReportingFrame(
        Api.GetObjectReportingFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetObjectReportingFrameRequest, Api.GetObjectReportingFrameResult>(
            executor,
            request,
            context,
            "analysis_operations.get_object_reporting_frame");

    [OperationImplementation("analysis_operations.get_plane_properties")]
    public override Task<Api.GetPlanePropertiesResult> GetPlaneProperties(
        Api.GetPlanePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPlanePropertiesRequest, Api.GetPlanePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_plane_properties");

    [OperationImplementation("analysis_operations.get_point_coordinate")]
    public override Task<Api.GetPointCoordinateResult> GetPointCoordinate(
        Api.GetPointCoordinateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointCoordinateRequest, Api.GetPointCoordinateResult>(
            executor,
            request,
            context,
            "analysis_operations.get_point_coordinate");

    [OperationImplementation("analysis_operations.get_point_coordinate_cylindrical")]
    public override Task<Api.GetPointCoordinateCylindricalResult> GetPointCoordinateCylindrical(
        Api.GetPointCoordinateCylindricalRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointCoordinateCylindricalRequest, Api.GetPointCoordinateCylindricalResult>(
            executor,
            request,
            context,
            "analysis_operations.get_point_coordinate_cylindrical");

    [OperationImplementation("analysis_operations.get_point_coordinate_polar")]
    public override Task<Api.GetPointCoordinatePolarResult> GetPointCoordinatePolar(
        Api.GetPointCoordinatePolarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointCoordinatePolarRequest, Api.GetPointCoordinatePolarResult>(
            executor,
            request,
            context,
            "analysis_operations.get_point_coordinate_polar");

    [OperationImplementation("analysis_operations.get_point_properties")]
    public override Task<Api.GetPointPropertiesResult> GetPointProperties(
        Api.GetPointPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointPropertiesRequest, Api.GetPointPropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_point_properties");

    [OperationImplementation("analysis_operations.get_point_to_line_distance")]
    public override Task<Api.GetPointToLineDistanceResult> GetPointToLineDistance(
        Api.GetPointToLineDistanceRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointToLineDistanceRequest, Api.GetPointToLineDistanceResult>(
            executor,
            request,
            context,
            "analysis_operations.get_point_to_line_distance");

    [OperationImplementation("analysis_operations.get_point_to_point_distance")]
    public override Task<Api.GetPointToPointDistanceResult> GetPointToPointDistance(
        Api.GetPointToPointDistanceRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointToPointDistanceRequest, Api.GetPointToPointDistanceResult>(
            executor,
            request,
            context,
            "analysis_operations.get_point_to_point_distance");

    [OperationImplementation("analysis_operations.get_point_tolerance")]
    public override Task<Api.GetPointToleranceResult> GetPointTolerance(
        Api.GetPointToleranceRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointToleranceRequest, Api.GetPointToleranceResult>(
            executor,
            request,
            context,
            "analysis_operations.get_point_tolerance");

    [OperationImplementation("analysis_operations.get_slot_properties")]
    public override Task<Api.GetSlotPropertiesResult> GetSlotProperties(
        Api.GetSlotPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetSlotPropertiesRequest, Api.GetSlotPropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_slot_properties");

    [OperationImplementation("analysis_operations.get_sphere_properties")]
    public override Task<Api.GetSpherePropertiesResult> GetSphereProperties(
        Api.GetSpherePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetSpherePropertiesRequest, Api.GetSpherePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_sphere_properties");

    [OperationImplementation("analysis_operations.get_surface_physical_stats")]
    public override Task<Api.GetSurfacePhysicalStatsResult> GetSurfacePhysicalStats(
        Api.GetSurfacePhysicalStatsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetSurfacePhysicalStatsRequest, Api.GetSurfacePhysicalStatsResult>(
            executor,
            request,
            context,
            "analysis_operations.get_surface_physical_stats");

    [OperationImplementation("analysis_operations.get_timestamp_for_ith_frame_in_frame_set")]
    public override Task<Api.GetTimestampForIthFrameInFrameSetResult> GetTimestampForIthFrameInFrameSet(
        Api.GetTimestampForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTimestampForIthFrameInFrameSetRequest, Api.GetTimestampForIthFrameInFrameSetResult>(
            executor,
            request,
            context,
            "analysis_operations.get_timestamp_for_ith_frame_in_frame_set");

    [OperationImplementation("analysis_operations.get_timestamp_for_ith_point_in_point_set")]
    public override Task<Api.GetTimestampForIthPointInPointSetResult> GetTimestampForIthPointInPointSet(
        Api.GetTimestampForIthPointInPointSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTimestampForIthPointInPointSetRequest, Api.GetTimestampForIthPointInPointSetResult>(
            executor,
            request,
            context,
            "analysis_operations.get_timestamp_for_ith_point_in_point_set");

    [OperationImplementation("analysis_operations.get_torus_properties")]
    public override Task<Api.GetTorusPropertiesResult> GetTorusProperties(
        Api.GetTorusPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTorusPropertiesRequest, Api.GetTorusPropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.get_torus_properties");

    [OperationImplementation("analysis_operations.get_transform_for_ith_frame_in_frame_set")]
    public override Task<Api.GetTransformForIthFrameInFrameSetResult> GetTransformForIthFrameInFrameSet(
        Api.GetTransformForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTransformForIthFrameInFrameSetRequest, Api.GetTransformForIthFrameInFrameSetResult>(
            executor,
            request,
            context,
            "analysis_operations.get_transform_for_ith_frame_in_frame_set");

    [OperationImplementation("analysis_operations.group_to_surface_fit")]
    public override Task<Api.GroupToSurfaceFitResult> GroupToSurfaceFit(
        Api.GroupToSurfaceFitRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GroupToSurfaceFitRequest, Api.GroupToSurfaceFitResult>(
            executor,
            request,
            context,
            "analysis_operations.group_to_surface_fit");

    [OperationImplementation("analysis_operations.import_geometry_fit_profiles")]
    public override Task<Api.ImportGeometryFitProfilesResult> ImportGeometryFitProfiles(
        Api.ImportGeometryFitProfilesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportGeometryFitProfilesRequest, Api.ImportGeometryFitProfilesResult>(
            executor,
            request,
            context,
            "analysis_operations.import_geometry_fit_profiles");

    [OperationImplementation("analysis_operations.is_object_of_type")]
    public override Task<Api.IsObjectOfTypeResult> IsObjectOfType(
        Api.IsObjectOfTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.IsObjectOfTypeRequest, Api.IsObjectOfTypeResult>(
            executor,
            request,
            context,
            "analysis_operations.is_object_of_type");

    [OperationImplementation("analysis_operations.make_circle_fit_profile")]
    public override Task<Api.MakeCircleFitProfileResult> MakeCircleFitProfile(
        Api.MakeCircleFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeCircleFitProfileRequest, Api.MakeCircleFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_circle_fit_profile");

    [OperationImplementation("analysis_operations.make_cone_fit_profile")]
    public override Task<Api.MakeConeFitProfileResult> MakeConeFitProfile(
        Api.MakeConeFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeConeFitProfileRequest, Api.MakeConeFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_cone_fit_profile");

    [OperationImplementation("analysis_operations.make_cylinder_fit_profile")]
    public override Task<Api.MakeCylinderFitProfileResult> MakeCylinderFitProfile(
        Api.MakeCylinderFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeCylinderFitProfileRequest, Api.MakeCylinderFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_cylinder_fit_profile");

    [OperationImplementation("analysis_operations.make_ellipse_fit_profile")]
    public override Task<Api.MakeEllipseFitProfileResult> MakeEllipseFitProfile(
        Api.MakeEllipseFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeEllipseFitProfileRequest, Api.MakeEllipseFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_ellipse_fit_profile");

    [OperationImplementation("analysis_operations.make_line_fit_profile")]
    public override Task<Api.MakeLineFitProfileResult> MakeLineFitProfile(
        Api.MakeLineFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeLineFitProfileRequest, Api.MakeLineFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_line_fit_profile");

    [OperationImplementation("analysis_operations.make_paraboloid_fit_profile")]
    public override Task<Api.MakeParaboloidFitProfileResult> MakeParaboloidFitProfile(
        Api.MakeParaboloidFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeParaboloidFitProfileRequest, Api.MakeParaboloidFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_paraboloid_fit_profile");

    [OperationImplementation("analysis_operations.make_plane_fit_profile")]
    public override Task<Api.MakePlaneFitProfileResult> MakePlaneFitProfile(
        Api.MakePlaneFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakePlaneFitProfileRequest, Api.MakePlaneFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_plane_fit_profile");

    [OperationImplementation("analysis_operations.make_slot_fit_profile")]
    public override Task<Api.MakeSlotFitProfileResult> MakeSlotFitProfile(
        Api.MakeSlotFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeSlotFitProfileRequest, Api.MakeSlotFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_slot_fit_profile");

    [OperationImplementation("analysis_operations.make_sphere_fit_profile")]
    public override Task<Api.MakeSphereFitProfileResult> MakeSphereFitProfile(
        Api.MakeSphereFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeSphereFitProfileRequest, Api.MakeSphereFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.make_sphere_fit_profile");

    [OperationImplementation("analysis_operations.mushroom_target_hole_inspection")]
    public override Task<Api.MushroomTargetHoleInspectionResult> MushroomTargetHoleInspection(
        Api.MushroomTargetHoleInspectionRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MushroomTargetHoleInspectionRequest, Api.MushroomTargetHoleInspectionResult>(
            executor,
            request,
            context,
            "analysis_operations.mushroom_target_hole_inspection");

    [OperationImplementation("analysis_operations.patch_normal_shift_hole_pin")]
    public override Task<Api.PatchNormalShiftHolePinResult> PatchNormalShiftHolePin(
        Api.PatchNormalShiftHolePinRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PatchNormalShiftHolePinRequest, Api.PatchNormalShiftHolePinResult>(
            executor,
            request,
            context,
            "analysis_operations.patch_normal_shift_hole_pin");

    [OperationImplementation("analysis_operations.patch_normal_shift_point")]
    public override Task<Api.PatchNormalShiftPointResult> PatchNormalShiftPoint(
        Api.PatchNormalShiftPointRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PatchNormalShiftPointRequest, Api.PatchNormalShiftPointResult>(
            executor,
            request,
            context,
            "analysis_operations.patch_normal_shift_point");

    [OperationImplementation("analysis_operations.query_clouds_to_objects")]
    public override Task<Api.QueryCloudsToObjectsResult> QueryCloudsToObjects(
        Api.QueryCloudsToObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryCloudsToObjectsRequest, Api.QueryCloudsToObjectsResult>(
            executor,
            request,
            context,
            "analysis_operations.query_clouds_to_objects");

    [OperationImplementation("analysis_operations.query_clouds_to_surface")]
    public override Task<Api.QueryCloudsToSurfaceResult> QueryCloudsToSurface(
        Api.QueryCloudsToSurfaceRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryCloudsToSurfaceRequest, Api.QueryCloudsToSurfaceResult>(
            executor,
            request,
            context,
            "analysis_operations.query_clouds_to_surface");

    [OperationImplementation("analysis_operations.query_frame_to_frame")]
    public override Task<Api.QueryFrameToFrameResult> QueryFrameToFrame(
        Api.QueryFrameToFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryFrameToFrameRequest, Api.QueryFrameToFrameResult>(
            executor,
            request,
            context,
            "analysis_operations.query_frame_to_frame");

    [OperationImplementation("analysis_operations.query_groups_to_objects")]
    public override Task<Api.QueryGroupsToObjectsResult> QueryGroupsToObjects(
        Api.QueryGroupsToObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryGroupsToObjectsRequest, Api.QueryGroupsToObjectsResult>(
            executor,
            request,
            context,
            "analysis_operations.query_groups_to_objects");

    [OperationImplementation("analysis_operations.query_point_to_objects")]
    public override Task<Api.QueryPointToObjectsResult> QueryPointToObjects(
        Api.QueryPointToObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryPointToObjectsRequest, Api.QueryPointToObjectsResult>(
            executor,
            request,
            context,
            "analysis_operations.query_point_to_objects");

    [OperationImplementation("analysis_operations.query_point_to_point_along_curve")]
    public override Task<Api.QueryPointToPointAlongCurveResult> QueryPointToPointAlongCurve(
        Api.QueryPointToPointAlongCurveRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryPointToPointAlongCurveRequest, Api.QueryPointToPointAlongCurveResult>(
            executor,
            request,
            context,
            "analysis_operations.query_point_to_point_along_curve");

    [OperationImplementation("analysis_operations.query_points_to_circle")]
    public override Task<Api.QueryPointsToCircleResult> QueryPointsToCircle(
        Api.QueryPointsToCircleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryPointsToCircleRequest, Api.QueryPointsToCircleResult>(
            executor,
            request,
            context,
            "analysis_operations.query_points_to_circle");

    [OperationImplementation("analysis_operations.query_points_to_objects")]
    public override Task<Api.QueryPointsToObjectsResult> QueryPointsToObjects(
        Api.QueryPointsToObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryPointsToObjectsRequest, Api.QueryPointsToObjectsResult>(
            executor,
            request,
            context,
            "analysis_operations.query_points_to_objects");

    [OperationImplementation("analysis_operations.query_points_to_single_point")]
    public override Task<Api.QueryPointsToSinglePointResult> QueryPointsToSinglePoint(
        Api.QueryPointsToSinglePointRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QueryPointsToSinglePointRequest, Api.QueryPointsToSinglePointResult>(
            executor,
            request,
            context,
            "analysis_operations.query_points_to_single_point");

    [OperationImplementation("analysis_operations.re_compute_calculated_items")]
    public override Task<Api.ReComputeCalculatedItemsResult> ReComputeCalculatedItems(
        Api.ReComputeCalculatedItemsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ReComputeCalculatedItemsRequest, Api.ReComputeCalculatedItemsResult>(
            executor,
            request,
            context,
            "analysis_operations.re_compute_calculated_items");

    [OperationImplementation("analysis_operations.rename_points_based_on_inter_point_distance_to_reference_points")]
    public override Task<Api.RenamePointsBasedOnInterPointDistanceToReferencePointsResult> RenamePointsBasedOnInterPointDistanceToReferencePoints(
        Api.RenamePointsBasedOnInterPointDistanceToReferencePointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RenamePointsBasedOnInterPointDistanceToReferencePointsRequest, Api.RenamePointsBasedOnInterPointDistanceToReferencePointsResult>(
            executor,
            request,
            context,
            "analysis_operations.rename_points_based_on_inter_point_distance_to_reference_points");

    [OperationImplementation("analysis_operations.rename_points_based_on_proximity_to_reference_points")]
    public override Task<Api.RenamePointsBasedOnProximityToReferencePointsResult> RenamePointsBasedOnProximityToReferencePoints(
        Api.RenamePointsBasedOnProximityToReferencePointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RenamePointsBasedOnProximityToReferencePointsRequest, Api.RenamePointsBasedOnProximityToReferencePointsResult>(
            executor,
            request,
            context,
            "analysis_operations.rename_points_based_on_proximity_to_reference_points");

    [OperationImplementation("analysis_operations.reverse_bsplines")]
    public override Task<Api.ReverseBSplinesResult> ReverseBSplines(
        Api.ReverseBSplinesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ReverseBSplinesRequest, Api.ReverseBSplinesResult>(
            executor,
            request,
            context,
            "analysis_operations.reverse_bsplines");

    [OperationImplementation("analysis_operations.reverse_plane_normals")]
    public override Task<Api.ReversePlaneNormalsResult> ReversePlaneNormals(
        Api.ReversePlaneNormalsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ReversePlaneNormalsRequest, Api.ReversePlaneNormalsResult>(
            executor,
            request,
            context,
            "analysis_operations.reverse_plane_normals");

    [OperationImplementation("analysis_operations.reverse_surface_normals")]
    public override Task<Api.ReverseSurfaceNormalsResult> ReverseSurfaceNormals(
        Api.ReverseSurfaceNormalsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ReverseSurfaceNormalsRequest, Api.ReverseSurfaceNormalsResult>(
            executor,
            request,
            context,
            "analysis_operations.reverse_surface_normals");

    [OperationImplementation("analysis_operations.set_circle_properties")]
    public override Task<Api.SetCirclePropertiesResult> SetCircleProperties(
        Api.SetCirclePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCirclePropertiesRequest, Api.SetCirclePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.set_circle_properties");

    [OperationImplementation("analysis_operations.set_cone_properties")]
    public override Task<Api.SetConePropertiesResult> SetConeProperties(
        Api.SetConePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetConePropertiesRequest, Api.SetConePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.set_cone_properties");

    [OperationImplementation("analysis_operations.set_cylinder_properties")]
    public override Task<Api.SetCylinderPropertiesResult> SetCylinderProperties(
        Api.SetCylinderPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCylinderPropertiesRequest, Api.SetCylinderPropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.set_cylinder_properties");

    [OperationImplementation("analysis_operations.set_default_colorization_options")]
    public override Task<Api.SetDefaultColorizationOptionsResult> SetDefaultColorizationOptions(
        Api.SetDefaultColorizationOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetDefaultColorizationOptionsRequest, Api.SetDefaultColorizationOptionsResult>(
            executor,
            request,
            context,
            "analysis_operations.set_default_colorization_options");

    [OperationImplementation("analysis_operations.set_ellipse_properties")]
    public override Task<Api.SetEllipsePropertiesResult> SetEllipseProperties(
        Api.SetEllipsePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetEllipsePropertiesRequest, Api.SetEllipsePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.set_ellipse_properties");

    [OperationImplementation("analysis_operations.set_geometry_relationship_fit_profile")]
    public override Task<Api.SetGeometryRelationshipFitProfileResult> SetGeometryRelationshipFitProfile(
        Api.SetGeometryRelationshipFitProfileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeometryRelationshipFitProfileRequest, Api.SetGeometryRelationshipFitProfileResult>(
            executor,
            request,
            context,
            "analysis_operations.set_geometry_relationship_fit_profile");

    [OperationImplementation("analysis_operations.set_line_properties")]
    public override Task<Api.SetLinePropertiesResult> SetLineProperties(
        Api.SetLinePropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetLinePropertiesRequest, Api.SetLinePropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.set_line_properties");

    [OperationImplementation("analysis_operations.set_measurement_auxiliary_data")]
    public override Task<Api.SetMeasurementAuxiliaryDataResult> SetMeasurementAuxiliaryData(
        Api.SetMeasurementAuxiliaryDataRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetMeasurementAuxiliaryDataRequest, Api.SetMeasurementAuxiliaryDataResult>(
            executor,
            request,
            context,
            "analysis_operations.set_measurement_auxiliary_data");

    [OperationImplementation("analysis_operations.set_object_reporting_frame")]
    public override Task<Api.SetObjectReportingFrameResult> SetObjectReportingFrame(
        Api.SetObjectReportingFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObjectReportingFrameRequest, Api.SetObjectReportingFrameResult>(
            executor,
            request,
            context,
            "analysis_operations.set_object_reporting_frame");

    [OperationImplementation("analysis_operations.set_point_properties")]
    public override Task<Api.SetPointPropertiesResult> SetPointProperties(
        Api.SetPointPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointPropertiesRequest, Api.SetPointPropertiesResult>(
            executor,
            request,
            context,
            "analysis_operations.set_point_properties");

    [OperationImplementation("analysis_operations.set_point_weights_from_uncertainties")]
    public override Task<Api.SetPointWeightsFromUncertaintiesResult> SetPointWeightsFromUncertainties(
        Api.SetPointWeightsFromUncertaintiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointWeightsFromUncertaintiesRequest, Api.SetPointWeightsFromUncertaintiesResult>(
            executor,
            request,
            context,
            "analysis_operations.set_point_weights_from_uncertainties");

    [OperationImplementation("analysis_operations.set_transform_for_ith_frame_in_frame_set")]
    public override Task<Api.SetTransformForIthFrameInFrameSetResult> SetTransformForIthFrameInFrameSet(
        Api.SetTransformForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTransformForIthFrameInFrameSetRequest, Api.SetTransformForIthFrameInFrameSetResult>(
            executor,
            request,
            context,
            "analysis_operations.set_transform_for_ith_frame_in_frame_set");

    [OperationImplementation("analysis_operations.sphere_axis_check")]
    public override Task<Api.SphereAxisCheckResult> SphereAxisCheck(
        Api.SphereAxisCheckRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SphereAxisCheckRequest, Api.SphereAxisCheckResult>(
            executor,
            request,
            context,
            "analysis_operations.sphere_axis_check");

    [OperationImplementation("analysis_operations.temperature_compensate_a_group")]
    public override Task<Api.TemperatureCompensateAGroupResult> TemperatureCompensateAGroup(
        Api.TemperatureCompensateAGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TemperatureCompensateAGroupRequest, Api.TemperatureCompensateAGroupResult>(
            executor,
            request,
            context,
            "analysis_operations.temperature_compensate_a_group");

    [OperationImplementation("analysis_operations.transform_objects_frame_to_frame")]
    public override Task<Api.TransformObjectsFrameToFrameResult> TransformObjectsFrameToFrame(
        Api.TransformObjectsFrameToFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TransformObjectsFrameToFrameRequest, Api.TransformObjectsFrameToFrameResult>(
            executor,
            request,
            context,
            "analysis_operations.transform_objects_frame_to_frame");

    [OperationImplementation("analysis_operations.transform_objects_by_delta_about_working_frame")]
    public override Task<Api.TransformObjectsByDeltaAboutWorkingFrameResult> TransformObjectsByDeltaAboutWorkingFrame(
        Api.TransformObjectsByDeltaAboutWorkingFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TransformObjectsByDeltaAboutWorkingFrameRequest, Api.TransformObjectsByDeltaAboutWorkingFrameResult>(
            executor,
            request,
            context,
            "analysis_operations.transform_objects_by_delta_about_working_frame");

    [OperationImplementation("analysis_operations.transform_objects_by_delta_world_transform_operator")]
    public override Task<Api.TransformObjectsByDeltaWorldTransformOperatorResult> TransformObjectsByDeltaWorldTransformOperator(
        Api.TransformObjectsByDeltaWorldTransformOperatorRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TransformObjectsByDeltaWorldTransformOperatorRequest, Api.TransformObjectsByDeltaWorldTransformOperatorResult>(
            executor,
            request,
            context,
            "analysis_operations.transform_objects_by_delta_world_transform_operator");

    [OperationImplementation("analysis_operations.translate_objects_by_delta")]
    public override Task<Api.TranslateObjectsByDeltaResult> TranslateObjectsByDelta(
        Api.TranslateObjectsByDeltaRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TranslateObjectsByDeltaRequest, Api.TranslateObjectsByDeltaResult>(
            executor,
            request,
            context,
            "analysis_operations.translate_objects_by_delta");

}
