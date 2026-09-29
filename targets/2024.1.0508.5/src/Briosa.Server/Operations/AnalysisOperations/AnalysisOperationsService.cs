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
        executor.ExecuteAsync(request, context, AngleBetweenLineAndPlaneOperation.Descriptor,
            AngleBetweenLineAndPlaneOperation.CreateCommand, AngleBetweenLineAndPlaneOperation.OutputContracts,
            AngleBetweenLineAndPlaneOperation.CreateResult);

    [OperationImplementation("analysis_operations.angle_between_two_lines")]
    public override Task<Api.AngleBetweenTwoLinesResult> AngleBetweenTwoLines(
        Api.AngleBetweenTwoLinesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AngleBetweenTwoLinesOperation.Descriptor,
            AngleBetweenTwoLinesOperation.CreateCommand, AngleBetweenTwoLinesOperation.OutputContracts,
            AngleBetweenTwoLinesOperation.CreateResult);

    [OperationImplementation("analysis_operations.angle_between_two_planes_normals")]
    public override Task<Api.AngleBetweenTwoPlanesNormalsResult> AngleBetweenTwoPlanesNormals(
        Api.AngleBetweenTwoPlanesNormalsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AngleBetweenTwoPlanesNormalsOperation.Descriptor,
            AngleBetweenTwoPlanesNormalsOperation.CreateCommand, AngleBetweenTwoPlanesNormalsOperation.OutputContracts,
            AngleBetweenTwoPlanesNormalsOperation.CreateResult);

    [OperationImplementation("analysis_operations.best_fit_transformation_group_to_group")]
    public override Task<Api.BestFitTransformationGroupToGroupResult> BestFitTransformationGroupToGroup(
        Api.BestFitTransformationGroupToGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, BestFitTransformationGroupToGroupOperation.Descriptor,
            BestFitTransformationGroupToGroupOperation.CreateCommand, BestFitTransformationGroupToGroupOperation.OutputContracts,
            BestFitTransformationGroupToGroupOperation.CreateResult);

    [OperationImplementation("analysis_operations.compute_group_to_group_orientation_rx_ry_rz")]
    public override Task<Api.ComputeGroupToGroupOrientationRxRyRzResult> ComputeGroupToGroupOrientationRxRyRz(
        Api.ComputeGroupToGroupOrientationRxRyRzRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ComputeGroupToGroupOrientationRxRyRzOperation.Descriptor,
            ComputeGroupToGroupOrientationRxRyRzOperation.CreateCommand,
            ComputeGroupToGroupOrientationRxRyRzOperation.OutputContracts,
            ComputeGroupToGroupOrientationRxRyRzOperation.CreateResult);

    [OperationImplementation("analysis_operations.create_point_uncertainty_cloud_point_sets")]
    public override Task<Api.CreatePointUncertaintyCloudPointSetsResult> CreatePointUncertaintyCloudPointSets(
        Api.CreatePointUncertaintyCloudPointSetsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreatePointUncertaintyCloudPointSetsOperation.Descriptor,
            CreatePointUncertaintyCloudPointSetsOperation.CreateCommand,
            CreatePointUncertaintyCloudPointSetsOperation.OutputContracts,
            CreatePointUncertaintyCloudPointSetsOperation.CreateResult);

    [OperationImplementation("analysis_operations.create_point_uncertainty_fields")]
    public override Task<Api.CreatePointUncertaintyFieldsResult> CreatePointUncertaintyFields(
        Api.CreatePointUncertaintyFieldsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreatePointUncertaintyFieldsOperation.Descriptor,
            CreatePointUncertaintyFieldsOperation.CreateCommand, CreatePointUncertaintyFieldsOperation.OutputContracts,
            CreatePointUncertaintyFieldsOperation.CreateResult);

    [OperationImplementation("analysis_operations.fit_geometry_to_point_group")]
    public override Task<Api.FitGeometryToPointGroupResult> FitGeometryToPointGroup(
        Api.FitGeometryToPointGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FitGeometryToPointGroupOperation.Descriptor,
            FitGeometryToPointGroupOperation.CreateCommand, FitGeometryToPointGroupOperation.OutputContracts,
            FitGeometryToPointGroupOperation.CreateResult);

    [OperationImplementation("analysis_operations.fit_geometry_to_point_group_projected_to_plane")]
    public override Task<Api.FitGeometryToPointGroupProjectedToPlaneResult> FitGeometryToPointGroupProjectedToPlane(
        Api.FitGeometryToPointGroupProjectedToPlaneRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FitGeometryToPointGroupProjectedToPlaneOperation.Descriptor,
            FitGeometryToPointGroupProjectedToPlaneOperation.CreateCommand,
            FitGeometryToPointGroupProjectedToPlaneOperation.OutputContracts,
            FitGeometryToPointGroupProjectedToPlaneOperation.CreateResult);

    [OperationImplementation("analysis_operations.fit_geometry_to_points")]
    public override Task<Api.FitGeometryToPointsResult> FitGeometryToPoints(
        Api.FitGeometryToPointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FitGeometryToPointsOperation.Descriptor,
            FitGeometryToPointsOperation.CreateCommand, FitGeometryToPointsOperation.OutputContracts,
            FitGeometryToPointsOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_bspline_properties")]
    public override Task<Api.GetBSplinePropertiesResult> GetBSplineProperties(
        Api.GetBSplinePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetBSplinePropertiesOperation.Descriptor,
            GetBSplinePropertiesOperation.CreateCommand, GetBSplinePropertiesOperation.OutputContracts,
            GetBSplinePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_circle_properties")]
    public override Task<Api.GetCirclePropertiesResult> GetCircleProperties(
        Api.GetCirclePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCirclePropertiesOperation.Descriptor,
            GetCirclePropertiesOperation.CreateCommand, GetCirclePropertiesOperation.OutputContracts,
            GetCirclePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_cone_properties")]
    public override Task<Api.GetConePropertiesResult> GetConeProperties(
        Api.GetConePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetConePropertiesOperation.Descriptor,
            GetConePropertiesOperation.CreateCommand, GetConePropertiesOperation.OutputContracts,
            GetConePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_coordinate_for_ith_point_in_point_set")]
    public override Task<Api.GetCoordinateForIthPointInPointSetResult> GetCoordinateForIthPointInPointSet(
        Api.GetCoordinateForIthPointInPointSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCoordinateForIthPointInPointSetOperation.Descriptor,
            GetCoordinateForIthPointInPointSetOperation.CreateCommand, GetCoordinateForIthPointInPointSetOperation.OutputContracts,
            GetCoordinateForIthPointInPointSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_cylinder_properties")]
    public override Task<Api.GetCylinderPropertiesResult> GetCylinderProperties(
        Api.GetCylinderPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCylinderPropertiesOperation.Descriptor,
            GetCylinderPropertiesOperation.CreateCommand, GetCylinderPropertiesOperation.OutputContracts,
            GetCylinderPropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_ellipse_properties")]
    public override Task<Api.GetEllipsePropertiesResult> GetEllipseProperties(
        Api.GetEllipsePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetEllipsePropertiesOperation.Descriptor,
            GetEllipsePropertiesOperation.CreateCommand, GetEllipsePropertiesOperation.OutputContracts,
            GetEllipsePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_euler_parameters_for_frame")]
    public override Task<Api.GetEulerParametersForFrameResult> GetEulerParametersForFrame(
        Api.GetEulerParametersForFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetEulerParametersForFrameOperation.Descriptor,
            GetEulerParametersForFrameOperation.CreateCommand, GetEulerParametersForFrameOperation.OutputContracts,
            GetEulerParametersForFrameOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_euler_parameters_for_ith_frame_in_frame_set")]
    public override Task<Api.GetEulerParametersForIthFrameInFrameSetResult> GetEulerParametersForIthFrameInFrameSet(
        Api.GetEulerParametersForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetEulerParametersForIthFrameInFrameSetOperation.Descriptor,
            GetEulerParametersForIthFrameInFrameSetOperation.CreateCommand, GetEulerParametersForIthFrameInFrameSetOperation.OutputContracts,
            GetEulerParametersForIthFrameInFrameSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_ith_collection_name")]
    public override Task<Api.GetIthCollectionNameResult> GetIthCollectionName(
        Api.GetIthCollectionNameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetIthCollectionNameOperation.Descriptor,
            GetIthCollectionNameOperation.CreateCommand, GetIthCollectionNameOperation.OutputContracts,
            GetIthCollectionNameOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_ith_point_from_group")]
    public override Task<Api.GetIthPointFromGroupResult> GetIthPointFromGroup(
        Api.GetIthPointFromGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetIthPointFromGroupOperation.Descriptor,
            GetIthPointFromGroupOperation.CreateCommand, GetIthPointFromGroupOperation.OutputContracts,
            GetIthPointFromGroupOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_line_properties")]
    public override Task<Api.GetLinePropertiesResult> GetLineProperties(
        Api.GetLinePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetLinePropertiesOperation.Descriptor,
            GetLinePropertiesOperation.CreateCommand, GetLinePropertiesOperation.OutputContracts,
            GetLinePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_measurement_auxiliary_data")]
    public override Task<Api.GetMeasurementAuxiliaryDataResult> GetMeasurementAuxiliaryData(
        Api.GetMeasurementAuxiliaryDataRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetMeasurementAuxiliaryDataOperation.Descriptor,
            GetMeasurementAuxiliaryDataOperation.CreateCommand, GetMeasurementAuxiliaryDataOperation.OutputContracts,
            GetMeasurementAuxiliaryDataOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_measurement_info_data")]
    public override Task<Api.GetMeasurementInfoDataResult> GetMeasurementInfoData(
        Api.GetMeasurementInfoDataRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetMeasurementInfoDataOperation.Descriptor,
            GetMeasurementInfoDataOperation.CreateCommand, GetMeasurementInfoDataOperation.OutputContracts,
            GetMeasurementInfoDataOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_measurement_weather_data")]
    public override Task<Api.GetMeasurementWeatherDataResult> GetMeasurementWeatherData(
        Api.GetMeasurementWeatherDataRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetMeasurementWeatherDataOperation.Descriptor,
            GetMeasurementWeatherDataOperation.CreateCommand, GetMeasurementWeatherDataOperation.OutputContracts,
            GetMeasurementWeatherDataOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_number_of_collections")]
    public override Task<Api.GetNumberOfCollectionsResult> GetNumberOfCollections(
        Api.GetNumberOfCollectionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfCollectionsOperation.Descriptor,
            GetNumberOfCollectionsOperation.CreateCommand, GetNumberOfCollectionsOperation.OutputContracts,
            GetNumberOfCollectionsOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_number_of_frames_in_frame_set")]
    public override Task<Api.GetNumberOfFramesInFrameSetResult> GetNumberOfFramesInFrameSet(
        Api.GetNumberOfFramesInFrameSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfFramesInFrameSetOperation.Descriptor,
            GetNumberOfFramesInFrameSetOperation.CreateCommand, GetNumberOfFramesInFrameSetOperation.OutputContracts,
            GetNumberOfFramesInFrameSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_number_of_points_in_group")]
    public override Task<Api.GetNumberOfPointsInGroupResult> GetNumberOfPointsInGroup(
        Api.GetNumberOfPointsInGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfPointsInGroupOperation.Descriptor,
            GetNumberOfPointsInGroupOperation.CreateCommand, GetNumberOfPointsInGroupOperation.OutputContracts,
            GetNumberOfPointsInGroupOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_number_of_points_in_point_set")]
    public override Task<Api.GetNumberOfPointsInPointSetResult> GetNumberOfPointsInPointSet(
        Api.GetNumberOfPointsInPointSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfPointsInPointSetOperation.Descriptor,
            GetNumberOfPointsInPointSetOperation.CreateCommand, GetNumberOfPointsInPointSetOperation.OutputContracts,
            GetNumberOfPointsInPointSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_object_reporting_frame")]
    public override Task<Api.GetObjectReportingFrameResult> GetObjectReportingFrame(
        Api.GetObjectReportingFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetObjectReportingFrameOperation.Descriptor,
            GetObjectReportingFrameOperation.CreateCommand, GetObjectReportingFrameOperation.OutputContracts,
            GetObjectReportingFrameOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_plane_properties")]
    public override Task<Api.GetPlanePropertiesResult> GetPlaneProperties(
        Api.GetPlanePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPlanePropertiesOperation.Descriptor,
            GetPlanePropertiesOperation.CreateCommand, GetPlanePropertiesOperation.OutputContracts,
            GetPlanePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_point_coordinate")]
    public override Task<Api.GetPointCoordinateResult> GetPointCoordinate(
        Api.GetPointCoordinateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointCoordinateOperation.Descriptor,
            GetPointCoordinateOperation.CreateCommand, GetPointCoordinateOperation.OutputContracts,
            GetPointCoordinateOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_point_coordinate_cylindrical")]
    public override Task<Api.GetPointCoordinateCylindricalResult> GetPointCoordinateCylindrical(
        Api.GetPointCoordinateCylindricalRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointCoordinateCylindricalOperation.Descriptor,
            GetPointCoordinateCylindricalOperation.CreateCommand, GetPointCoordinateCylindricalOperation.OutputContracts,
            GetPointCoordinateCylindricalOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_point_coordinate_polar")]
    public override Task<Api.GetPointCoordinatePolarResult> GetPointCoordinatePolar(
        Api.GetPointCoordinatePolarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointCoordinatePolarOperation.Descriptor,
            GetPointCoordinatePolarOperation.CreateCommand, GetPointCoordinatePolarOperation.OutputContracts,
            GetPointCoordinatePolarOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_point_properties")]
    public override Task<Api.GetPointPropertiesResult> GetPointProperties(
        Api.GetPointPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointPropertiesOperation.Descriptor,
            GetPointPropertiesOperation.CreateCommand, GetPointPropertiesOperation.OutputContracts,
            GetPointPropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_point_to_line_distance")]
    public override Task<Api.GetPointToLineDistanceResult> GetPointToLineDistance(
        Api.GetPointToLineDistanceRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointToLineDistanceOperation.Descriptor,
            GetPointToLineDistanceOperation.CreateCommand, GetPointToLineDistanceOperation.OutputContracts,
            GetPointToLineDistanceOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_point_to_point_distance")]
    public override Task<Api.GetPointToPointDistanceResult> GetPointToPointDistance(
        Api.GetPointToPointDistanceRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointToPointDistanceOperation.Descriptor,
            GetPointToPointDistanceOperation.CreateCommand, GetPointToPointDistanceOperation.OutputContracts,
            GetPointToPointDistanceOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_point_tolerance")]
    public override Task<Api.GetPointToleranceResult> GetPointTolerance(
        Api.GetPointToleranceRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointToleranceOperation.Descriptor,
            GetPointToleranceOperation.CreateCommand, GetPointToleranceOperation.OutputContracts,
            GetPointToleranceOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_slot_properties")]
    public override Task<Api.GetSlotPropertiesResult> GetSlotProperties(
        Api.GetSlotPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetSlotPropertiesOperation.Descriptor,
            GetSlotPropertiesOperation.CreateCommand, GetSlotPropertiesOperation.OutputContracts,
            GetSlotPropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_sphere_properties")]
    public override Task<Api.GetSpherePropertiesResult> GetSphereProperties(
        Api.GetSpherePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetSpherePropertiesOperation.Descriptor,
            GetSpherePropertiesOperation.CreateCommand, GetSpherePropertiesOperation.OutputContracts,
            GetSpherePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_surface_physical_stats")]
    public override Task<Api.GetSurfacePhysicalStatsResult> GetSurfacePhysicalStats(
        Api.GetSurfacePhysicalStatsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetSurfacePhysicalStatsOperation.Descriptor,
            GetSurfacePhysicalStatsOperation.CreateCommand, GetSurfacePhysicalStatsOperation.OutputContracts,
            GetSurfacePhysicalStatsOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_timestamp_for_ith_frame_in_frame_set")]
    public override Task<Api.GetTimestampForIthFrameInFrameSetResult> GetTimestampForIthFrameInFrameSet(
        Api.GetTimestampForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTimestampForIthFrameInFrameSetOperation.Descriptor,
            GetTimestampForIthFrameInFrameSetOperation.CreateCommand, GetTimestampForIthFrameInFrameSetOperation.OutputContracts,
            GetTimestampForIthFrameInFrameSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_timestamp_for_ith_point_in_point_set")]
    public override Task<Api.GetTimestampForIthPointInPointSetResult> GetTimestampForIthPointInPointSet(
        Api.GetTimestampForIthPointInPointSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTimestampForIthPointInPointSetOperation.Descriptor,
            GetTimestampForIthPointInPointSetOperation.CreateCommand, GetTimestampForIthPointInPointSetOperation.OutputContracts,
            GetTimestampForIthPointInPointSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_torus_properties")]
    public override Task<Api.GetTorusPropertiesResult> GetTorusProperties(
        Api.GetTorusPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTorusPropertiesOperation.Descriptor,
            GetTorusPropertiesOperation.CreateCommand, GetTorusPropertiesOperation.OutputContracts,
            GetTorusPropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.get_transform_for_ith_frame_in_frame_set")]
    public override Task<Api.GetTransformForIthFrameInFrameSetResult> GetTransformForIthFrameInFrameSet(
        Api.GetTransformForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTransformForIthFrameInFrameSetOperation.Descriptor,
            GetTransformForIthFrameInFrameSetOperation.CreateCommand, GetTransformForIthFrameInFrameSetOperation.OutputContracts,
            GetTransformForIthFrameInFrameSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.group_to_surface_fit")]
    public override Task<Api.GroupToSurfaceFitResult> GroupToSurfaceFit(
        Api.GroupToSurfaceFitRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GroupToSurfaceFitOperation.Descriptor,
            GroupToSurfaceFitOperation.CreateCommand, GroupToSurfaceFitOperation.OutputContracts,
            GroupToSurfaceFitOperation.CreateResult);

    [OperationImplementation("analysis_operations.import_geometry_fit_profiles")]
    public override Task<Api.ImportGeometryFitProfilesResult> ImportGeometryFitProfiles(
        Api.ImportGeometryFitProfilesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportGeometryFitProfilesOperation.Descriptor,
            ImportGeometryFitProfilesOperation.CreateCommand, ImportGeometryFitProfilesOperation.OutputContracts,
            ImportGeometryFitProfilesOperation.CreateResult);

    [OperationImplementation("analysis_operations.is_object_of_type")]
    public override Task<Api.IsObjectOfTypeResult> IsObjectOfType(
        Api.IsObjectOfTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, IsObjectOfTypeOperation.Descriptor,
            IsObjectOfTypeOperation.CreateCommand, IsObjectOfTypeOperation.OutputContracts,
            IsObjectOfTypeOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_circle_fit_profile")]
    public override Task<Api.MakeCircleFitProfileResult> MakeCircleFitProfile(
        Api.MakeCircleFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeCircleFitProfileOperation.Descriptor,
            MakeCircleFitProfileOperation.CreateCommand, MakeCircleFitProfileOperation.OutputContracts,
            MakeCircleFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_cone_fit_profile")]
    public override Task<Api.MakeConeFitProfileResult> MakeConeFitProfile(
        Api.MakeConeFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeConeFitProfileOperation.Descriptor,
            MakeConeFitProfileOperation.CreateCommand, MakeConeFitProfileOperation.OutputContracts,
            MakeConeFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_cylinder_fit_profile")]
    public override Task<Api.MakeCylinderFitProfileResult> MakeCylinderFitProfile(
        Api.MakeCylinderFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeCylinderFitProfileOperation.Descriptor,
            MakeCylinderFitProfileOperation.CreateCommand, MakeCylinderFitProfileOperation.OutputContracts,
            MakeCylinderFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_ellipse_fit_profile")]
    public override Task<Api.MakeEllipseFitProfileResult> MakeEllipseFitProfile(
        Api.MakeEllipseFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeEllipseFitProfileOperation.Descriptor,
            MakeEllipseFitProfileOperation.CreateCommand, MakeEllipseFitProfileOperation.OutputContracts,
            MakeEllipseFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_line_fit_profile")]
    public override Task<Api.MakeLineFitProfileResult> MakeLineFitProfile(
        Api.MakeLineFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeLineFitProfileOperation.Descriptor,
            MakeLineFitProfileOperation.CreateCommand, MakeLineFitProfileOperation.OutputContracts,
            MakeLineFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_paraboloid_fit_profile")]
    public override Task<Api.MakeParaboloidFitProfileResult> MakeParaboloidFitProfile(
        Api.MakeParaboloidFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeParaboloidFitProfileOperation.Descriptor,
            MakeParaboloidFitProfileOperation.CreateCommand, MakeParaboloidFitProfileOperation.OutputContracts,
            MakeParaboloidFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_plane_fit_profile")]
    public override Task<Api.MakePlaneFitProfileResult> MakePlaneFitProfile(
        Api.MakePlaneFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakePlaneFitProfileOperation.Descriptor,
            MakePlaneFitProfileOperation.CreateCommand, MakePlaneFitProfileOperation.OutputContracts,
            MakePlaneFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_slot_fit_profile")]
    public override Task<Api.MakeSlotFitProfileResult> MakeSlotFitProfile(
        Api.MakeSlotFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeSlotFitProfileOperation.Descriptor,
            MakeSlotFitProfileOperation.CreateCommand, MakeSlotFitProfileOperation.OutputContracts,
            MakeSlotFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.make_sphere_fit_profile")]
    public override Task<Api.MakeSphereFitProfileResult> MakeSphereFitProfile(
        Api.MakeSphereFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeSphereFitProfileOperation.Descriptor,
            MakeSphereFitProfileOperation.CreateCommand, MakeSphereFitProfileOperation.OutputContracts,
            MakeSphereFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.mushroom_target_hole_inspection")]
    public override Task<Api.MushroomTargetHoleInspectionResult> MushroomTargetHoleInspection(
        Api.MushroomTargetHoleInspectionRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MushroomTargetHoleInspectionOperation.Descriptor,
            MushroomTargetHoleInspectionOperation.CreateCommand, MushroomTargetHoleInspectionOperation.OutputContracts,
            MushroomTargetHoleInspectionOperation.CreateResult);

    [OperationImplementation("analysis_operations.patch_normal_shift_hole_pin")]
    public override Task<Api.PatchNormalShiftHolePinResult> PatchNormalShiftHolePin(
        Api.PatchNormalShiftHolePinRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PatchNormalShiftHolePinOperation.Descriptor,
            PatchNormalShiftHolePinOperation.CreateCommand, PatchNormalShiftHolePinOperation.OutputContracts,
            PatchNormalShiftHolePinOperation.CreateResult);

    [OperationImplementation("analysis_operations.patch_normal_shift_point")]
    public override Task<Api.PatchNormalShiftPointResult> PatchNormalShiftPoint(
        Api.PatchNormalShiftPointRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PatchNormalShiftPointOperation.Descriptor,
            PatchNormalShiftPointOperation.CreateCommand, PatchNormalShiftPointOperation.OutputContracts,
            PatchNormalShiftPointOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_clouds_to_objects")]
    public override Task<Api.QueryCloudsToObjectsResult> QueryCloudsToObjects(
        Api.QueryCloudsToObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryCloudsToObjectsOperation.Descriptor,
            QueryCloudsToObjectsOperation.CreateCommand, QueryCloudsToObjectsOperation.OutputContracts,
            QueryCloudsToObjectsOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_clouds_to_surface")]
    public override Task<Api.QueryCloudsToSurfaceResult> QueryCloudsToSurface(
        Api.QueryCloudsToSurfaceRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryCloudsToSurfaceOperation.Descriptor,
            QueryCloudsToSurfaceOperation.CreateCommand, QueryCloudsToSurfaceOperation.OutputContracts,
            QueryCloudsToSurfaceOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_frame_to_frame")]
    public override Task<Api.QueryFrameToFrameResult> QueryFrameToFrame(
        Api.QueryFrameToFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryFrameToFrameOperation.Descriptor,
            QueryFrameToFrameOperation.CreateCommand, QueryFrameToFrameOperation.OutputContracts,
            QueryFrameToFrameOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_groups_to_objects")]
    public override Task<Api.QueryGroupsToObjectsResult> QueryGroupsToObjects(
        Api.QueryGroupsToObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryGroupsToObjectsOperation.Descriptor,
            QueryGroupsToObjectsOperation.CreateCommand, QueryGroupsToObjectsOperation.OutputContracts,
            QueryGroupsToObjectsOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_point_to_objects")]
    public override Task<Api.QueryPointToObjectsResult> QueryPointToObjects(
        Api.QueryPointToObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryPointToObjectsOperation.Descriptor,
            QueryPointToObjectsOperation.CreateCommand, QueryPointToObjectsOperation.OutputContracts,
            QueryPointToObjectsOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_point_to_point_along_curve")]
    public override Task<Api.QueryPointToPointAlongCurveResult> QueryPointToPointAlongCurve(
        Api.QueryPointToPointAlongCurveRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryPointToPointAlongCurveOperation.Descriptor,
            QueryPointToPointAlongCurveOperation.CreateCommand, QueryPointToPointAlongCurveOperation.OutputContracts,
            QueryPointToPointAlongCurveOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_points_to_circle")]
    public override Task<Api.QueryPointsToCircleResult> QueryPointsToCircle(
        Api.QueryPointsToCircleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryPointsToCircleOperation.Descriptor,
            QueryPointsToCircleOperation.CreateCommand, QueryPointsToCircleOperation.OutputContracts,
            QueryPointsToCircleOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_points_to_objects")]
    public override Task<Api.QueryPointsToObjectsResult> QueryPointsToObjects(
        Api.QueryPointsToObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryPointsToObjectsOperation.Descriptor,
            QueryPointsToObjectsOperation.CreateCommand, QueryPointsToObjectsOperation.OutputContracts,
            QueryPointsToObjectsOperation.CreateResult);

    [OperationImplementation("analysis_operations.query_points_to_single_point")]
    public override Task<Api.QueryPointsToSinglePointResult> QueryPointsToSinglePoint(
        Api.QueryPointsToSinglePointRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QueryPointsToSinglePointOperation.Descriptor,
            QueryPointsToSinglePointOperation.CreateCommand, QueryPointsToSinglePointOperation.OutputContracts,
            QueryPointsToSinglePointOperation.CreateResult);

    [OperationImplementation("analysis_operations.re_compute_calculated_items")]
    public override Task<Api.ReComputeCalculatedItemsResult> ReComputeCalculatedItems(
        Api.ReComputeCalculatedItemsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ReComputeCalculatedItemsOperation.Descriptor,
            ReComputeCalculatedItemsOperation.CreateCommand, ReComputeCalculatedItemsOperation.OutputContracts,
            ReComputeCalculatedItemsOperation.CreateResult);

    [OperationImplementation("analysis_operations.rename_points_based_on_inter_point_distance_to_reference_points")]
    public override Task<Api.RenamePointsBasedOnInterPointDistanceToReferencePointsResult> RenamePointsBasedOnInterPointDistanceToReferencePoints(
        Api.RenamePointsBasedOnInterPointDistanceToReferencePointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context,
            RenamePointsBasedOnInterPointDistanceToReferencePointsOperation.Descriptor,
            RenamePointsBasedOnInterPointDistanceToReferencePointsOperation.CreateCommand,
            RenamePointsBasedOnInterPointDistanceToReferencePointsOperation.OutputContracts,
            RenamePointsBasedOnInterPointDistanceToReferencePointsOperation.CreateResult);

    [OperationImplementation("analysis_operations.rename_points_based_on_proximity_to_reference_points")]
    public override Task<Api.RenamePointsBasedOnProximityToReferencePointsResult> RenamePointsBasedOnProximityToReferencePoints(
        Api.RenamePointsBasedOnProximityToReferencePointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context,
            RenamePointsBasedOnProximityToReferencePointsOperation.Descriptor,
            RenamePointsBasedOnProximityToReferencePointsOperation.CreateCommand,
            RenamePointsBasedOnProximityToReferencePointsOperation.OutputContracts,
            RenamePointsBasedOnProximityToReferencePointsOperation.CreateResult);

    [OperationImplementation("analysis_operations.reverse_bsplines")]
    public override Task<Api.ReverseBSplinesResult> ReverseBSplines(
        Api.ReverseBSplinesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ReverseBSplinesOperation.Descriptor,
            ReverseBSplinesOperation.CreateCommand, ReverseBSplinesOperation.OutputContracts,
            ReverseBSplinesOperation.CreateResult);

    [OperationImplementation("analysis_operations.reverse_plane_normals")]
    public override Task<Api.ReversePlaneNormalsResult> ReversePlaneNormals(
        Api.ReversePlaneNormalsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ReversePlaneNormalsOperation.Descriptor,
            ReversePlaneNormalsOperation.CreateCommand, ReversePlaneNormalsOperation.OutputContracts,
            ReversePlaneNormalsOperation.CreateResult);

    [OperationImplementation("analysis_operations.reverse_surface_normals")]
    public override Task<Api.ReverseSurfaceNormalsResult> ReverseSurfaceNormals(
        Api.ReverseSurfaceNormalsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ReverseSurfaceNormalsOperation.Descriptor,
            ReverseSurfaceNormalsOperation.CreateCommand, ReverseSurfaceNormalsOperation.OutputContracts,
            ReverseSurfaceNormalsOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_circle_properties")]
    public override Task<Api.SetCirclePropertiesResult> SetCircleProperties(
        Api.SetCirclePropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCirclePropertiesOperation.Descriptor,
            SetCirclePropertiesOperation.CreateCommand, SetCirclePropertiesOperation.OutputContracts,
            SetCirclePropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_cylinder_properties")]
    public override Task<Api.SetCylinderPropertiesResult> SetCylinderProperties(
        Api.SetCylinderPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCylinderPropertiesOperation.Descriptor,
            SetCylinderPropertiesOperation.CreateCommand, SetCylinderPropertiesOperation.OutputContracts,
            SetCylinderPropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_default_colorization_options")]
    public override Task<Api.SetDefaultColorizationOptionsResult> SetDefaultColorizationOptions(
        Api.SetDefaultColorizationOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetDefaultColorizationOptionsOperation.Descriptor,
            SetDefaultColorizationOptionsOperation.CreateCommand, SetDefaultColorizationOptionsOperation.OutputContracts,
            SetDefaultColorizationOptionsOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_geometry_relationship_fit_profile")]
    public override Task<Api.SetGeometryRelationshipFitProfileResult> SetGeometryRelationshipFitProfile(
        Api.SetGeometryRelationshipFitProfileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeometryRelationshipFitProfileOperation.Descriptor,
            SetGeometryRelationshipFitProfileOperation.CreateCommand, SetGeometryRelationshipFitProfileOperation.OutputContracts,
            SetGeometryRelationshipFitProfileOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_measurement_auxiliary_data")]
    public override Task<Api.SetMeasurementAuxiliaryDataResult> SetMeasurementAuxiliaryData(
        Api.SetMeasurementAuxiliaryDataRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetMeasurementAuxiliaryDataOperation.Descriptor,
            SetMeasurementAuxiliaryDataOperation.CreateCommand, SetMeasurementAuxiliaryDataOperation.OutputContracts,
            SetMeasurementAuxiliaryDataOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_object_reporting_frame")]
    public override Task<Api.SetObjectReportingFrameResult> SetObjectReportingFrame(
        Api.SetObjectReportingFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObjectReportingFrameOperation.Descriptor,
            SetObjectReportingFrameOperation.CreateCommand, SetObjectReportingFrameOperation.OutputContracts,
            SetObjectReportingFrameOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_point_properties")]
    public override Task<Api.SetPointPropertiesResult> SetPointProperties(
        Api.SetPointPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointPropertiesOperation.Descriptor,
            SetPointPropertiesOperation.CreateCommand, SetPointPropertiesOperation.OutputContracts,
            SetPointPropertiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_point_weights_from_uncertainties")]
    public override Task<Api.SetPointWeightsFromUncertaintiesResult> SetPointWeightsFromUncertainties(
        Api.SetPointWeightsFromUncertaintiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointWeightsFromUncertaintiesOperation.Descriptor,
            SetPointWeightsFromUncertaintiesOperation.CreateCommand,
            SetPointWeightsFromUncertaintiesOperation.OutputContracts,
            SetPointWeightsFromUncertaintiesOperation.CreateResult);

    [OperationImplementation("analysis_operations.set_transform_for_ith_frame_in_frame_set")]
    public override Task<Api.SetTransformForIthFrameInFrameSetResult> SetTransformForIthFrameInFrameSet(
        Api.SetTransformForIthFrameInFrameSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTransformForIthFrameInFrameSetOperation.Descriptor,
            SetTransformForIthFrameInFrameSetOperation.CreateCommand,
            SetTransformForIthFrameInFrameSetOperation.OutputContracts,
            SetTransformForIthFrameInFrameSetOperation.CreateResult);

    [OperationImplementation("analysis_operations.sphere_axis_check")]
    public override Task<Api.SphereAxisCheckResult> SphereAxisCheck(
        Api.SphereAxisCheckRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SphereAxisCheckOperation.Descriptor,
            SphereAxisCheckOperation.CreateCommand, SphereAxisCheckOperation.OutputContracts,
            SphereAxisCheckOperation.CreateResult);

    [OperationImplementation("analysis_operations.temperature_compensate_a_group")]
    public override Task<Api.TemperatureCompensateAGroupResult> TemperatureCompensateAGroup(
        Api.TemperatureCompensateAGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TemperatureCompensateAGroupOperation.Descriptor,
            TemperatureCompensateAGroupOperation.CreateCommand, TemperatureCompensateAGroupOperation.OutputContracts,
            TemperatureCompensateAGroupOperation.CreateResult);

    [OperationImplementation("analysis_operations.transform_objects_frame_to_frame")]
    public override Task<Api.TransformObjectsFrameToFrameResult> TransformObjectsFrameToFrame(
        Api.TransformObjectsFrameToFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TransformObjectsFrameToFrameOperation.Descriptor,
            TransformObjectsFrameToFrameOperation.CreateCommand, TransformObjectsFrameToFrameOperation.OutputContracts,
            TransformObjectsFrameToFrameOperation.CreateResult);

    [OperationImplementation("analysis_operations.transform_objects_by_delta_about_working_frame")]
    public override Task<Api.TransformObjectsByDeltaAboutWorkingFrameResult> TransformObjectsByDeltaAboutWorkingFrame(
        Api.TransformObjectsByDeltaAboutWorkingFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TransformObjectsByDeltaAboutWorkingFrameOperation.Descriptor,
            TransformObjectsByDeltaAboutWorkingFrameOperation.CreateCommand,
            TransformObjectsByDeltaAboutWorkingFrameOperation.OutputContracts,
            TransformObjectsByDeltaAboutWorkingFrameOperation.CreateResult);

    [OperationImplementation("analysis_operations.transform_objects_by_delta_world_transform_operator")]
    public override Task<Api.TransformObjectsByDeltaWorldTransformOperatorResult> TransformObjectsByDeltaWorldTransformOperator(
        Api.TransformObjectsByDeltaWorldTransformOperatorRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TransformObjectsByDeltaWorldTransformOperatorOperation.Descriptor,
            TransformObjectsByDeltaWorldTransformOperatorOperation.CreateCommand,
            TransformObjectsByDeltaWorldTransformOperatorOperation.OutputContracts,
            TransformObjectsByDeltaWorldTransformOperatorOperation.CreateResult);

    [OperationImplementation("analysis_operations.translate_objects_by_delta")]
    public override Task<Api.TranslateObjectsByDeltaResult> TranslateObjectsByDelta(
        Api.TranslateObjectsByDeltaRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TranslateObjectsByDeltaOperation.Descriptor,
            TranslateObjectsByDeltaOperation.CreateCommand, TranslateObjectsByDeltaOperation.OutputContracts,
            TranslateObjectsByDeltaOperation.CreateResult);

}
