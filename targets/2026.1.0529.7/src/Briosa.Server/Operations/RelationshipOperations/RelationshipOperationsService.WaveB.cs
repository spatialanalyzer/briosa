using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal sealed partial class RelationshipOperationsService
{
    [OperationImplementation("relationship_operations.set_relationship_associated_data")]
    public override Task<Api.SetRelationshipAssociatedDataResult> SetRelationshipAssociatedData(Api.SetRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipAssociatedDataRequest, Api.SetRelationshipAssociatedDataResult>(executor, request, context, "relationship_operations.set_relationship_associated_data");

    [OperationImplementation("relationship_operations.make_vector_group_to_vector_group_relationship")]
    public override Task<Api.MakeVectorGroupToVectorGroupRelationshipResult> MakeVectorGroupToVectorGroupRelationship(Api.MakeVectorGroupToVectorGroupRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeVectorGroupToVectorGroupRelationshipRequest, Api.MakeVectorGroupToVectorGroupRelationshipResult>(executor, request, context, "relationship_operations.make_vector_group_to_vector_group_relationship");

    [OperationImplementation("relationship_operations.filter_geometry_relationship_outlier_cloud_points")]
    public override Task<Api.FilterGeometryRelationshipOutlierCloudPointsResult> FilterGeometryRelationshipOutlierCloudPoints(Api.FilterGeometryRelationshipOutlierCloudPointsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FilterGeometryRelationshipOutlierCloudPointsRequest, Api.FilterGeometryRelationshipOutlierCloudPointsResult>(executor, request, context, "relationship_operations.filter_geometry_relationship_outlier_cloud_points");

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_cylindrical_zone")]
    public override Task<Api.SetVectorGroupToVectorGroupCylindricalZoneResult> SetVectorGroupToVectorGroupCylindricalZone(Api.SetVectorGroupToVectorGroupCylindricalZoneRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorGroupToVectorGroupCylindricalZoneRequest, Api.SetVectorGroupToVectorGroupCylindricalZoneResult>(executor, request, context, "relationship_operations.set_vector_group_to_vector_group_cylindrical_zone");

    [OperationImplementation("relationship_operations.do_relationship_fit")]
    public override Task<Api.DoRelationshipFitResult> DoRelationshipFit(Api.DoRelationshipFitRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DoRelationshipFitRequest, Api.DoRelationshipFitResult>(executor, request, context, "relationship_operations.do_relationship_fit");

    [OperationImplementation("relationship_operations.get_geom_relationship_criteria_name_list")]
    public override Task<Api.GetGeomRelationshipCriteriaNameListResult> GetGeomRelationshipCriteriaNameList(Api.GetGeomRelationshipCriteriaNameListRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipCriteriaNameListRequest, Api.GetGeomRelationshipCriteriaNameListResult>(executor, request, context, "relationship_operations.get_geom_relationship_criteria_name_list");

    [OperationImplementation("relationship_operations.set_group_to_nominal_group_view_zooming")]
    public override Task<Api.SetGroupToNominalGroupViewZoomingResult> SetGroupToNominalGroupViewZooming(Api.SetGroupToNominalGroupViewZoomingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGroupToNominalGroupViewZoomingRequest, Api.SetGroupToNominalGroupViewZoomingResult>(executor, request, context, "relationship_operations.set_group_to_nominal_group_view_zooming");

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_fit_weights")]
    public override Task<Api.SetVectorGroupToVectorGroupFitWeightsResult> SetVectorGroupToVectorGroupFitWeights(Api.SetVectorGroupToVectorGroupFitWeightsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorGroupToVectorGroupFitWeightsRequest, Api.SetVectorGroupToVectorGroupFitWeightsResult>(executor, request, context, "relationship_operations.set_vector_group_to_vector_group_fit_weights");

    [OperationImplementation("relationship_operations.make_dynamic_ellipse_relationship")]
    public override Task<Api.MakeDynamicEllipseRelationshipResult> MakeDynamicEllipseRelationship(Api.MakeDynamicEllipseRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeDynamicEllipseRelationshipRequest, Api.MakeDynamicEllipseRelationshipResult>(executor, request, context, "relationship_operations.make_dynamic_ellipse_relationship");

    [OperationImplementation("relationship_operations.set_object_to_object_direction_relationship_tolerances")]
    public override Task<Api.SetObjectToObjectDirectionRelationshipTolerancesResult> SetObjectToObjectDirectionRelationshipTolerances(Api.SetObjectToObjectDirectionRelationshipTolerancesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObjectToObjectDirectionRelationshipTolerancesRequest, Api.SetObjectToObjectDirectionRelationshipTolerancesResult>(executor, request, context, "relationship_operations.set_object_to_object_direction_relationship_tolerances");

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_fit_gradient_factor")]
    public override Task<Api.SetVectorGroupToVectorGroupFitGradientFactorResult> SetVectorGroupToVectorGroupFitGradientFactor(Api.SetVectorGroupToVectorGroupFitGradientFactorRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorGroupToVectorGroupFitGradientFactorRequest, Api.SetVectorGroupToVectorGroupFitGradientFactorResult>(executor, request, context, "relationship_operations.set_vector_group_to_vector_group_fit_gradient_factor");

    [OperationImplementation("relationship_operations.start_stop_relationship_trapping")]
    public override Task<Api.StartStopRelationshipTrappingResult> StartStopRelationshipTrapping(Api.StartStopRelationshipTrappingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartStopRelationshipTrappingRequest, Api.StartStopRelationshipTrappingResult>(executor, request, context, "relationship_operations.start_stop_relationship_trapping");

    [OperationImplementation("relationship_operations.make_dynamic_circle_relationship")]
    public override Task<Api.MakeDynamicCircleRelationshipResult> MakeDynamicCircleRelationship(Api.MakeDynamicCircleRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeDynamicCircleRelationshipRequest, Api.MakeDynamicCircleRelationshipResult>(executor, request, context, "relationship_operations.make_dynamic_circle_relationship");

    [OperationImplementation("relationship_operations.get_relationship_associated_data")]
    public override Task<Api.GetRelationshipAssociatedDataResult> GetRelationshipAssociatedData(Api.GetRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipAssociatedDataRequest, Api.GetRelationshipAssociatedDataResult>(executor, request, context, "relationship_operations.get_relationship_associated_data");

    [OperationImplementation("relationship_operations.make_dynamic_plane_relationship")]
    public override Task<Api.MakeDynamicPlaneRelationshipResult> MakeDynamicPlaneRelationship(Api.MakeDynamicPlaneRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeDynamicPlaneRelationshipRequest, Api.MakeDynamicPlaneRelationshipResult>(executor, request, context, "relationship_operations.make_dynamic_plane_relationship");

    [OperationImplementation("relationship_operations.make_point_to_point_relationship")]
    public override Task<Api.MakePointToPointRelationshipResult> MakePointToPointRelationship(Api.MakePointToPointRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakePointToPointRelationshipRequest, Api.MakePointToPointRelationshipResult>(executor, request, context, "relationship_operations.make_point_to_point_relationship");

    [OperationImplementation("relationship_operations.auto_filter_points_groups_clouds_to_surface_faces")]
    public override Task<Api.AutoFilterPointsGroupsCloudsToSurfaceFacesResult> AutoFilterPointsGroupsCloudsToSurfaceFaces(Api.AutoFilterPointsGroupsCloudsToSurfaceFacesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoFilterPointsGroupsCloudsToSurfaceFacesRequest, Api.AutoFilterPointsGroupsCloudsToSurfaceFacesResult>(executor, request, context, "relationship_operations.auto_filter_points_groups_clouds_to_surface_faces");

    [OperationImplementation("relationship_operations.make_geometry_fit_and_compare_to_nominal_relationship")]
    public override Task<Api.MakeGeometryFitAndCompareToNominalRelationshipResult> MakeGeometryFitAndCompareToNominalRelationship(Api.MakeGeometryFitAndCompareToNominalRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGeometryFitAndCompareToNominalRelationshipRequest, Api.MakeGeometryFitAndCompareToNominalRelationshipResult>(executor, request, context, "relationship_operations.make_geometry_fit_and_compare_to_nominal_relationship");

    [OperationImplementation("relationship_operations.create_points_to_objects_map")]
    public override Task<Api.CreatePointsToObjectsMapResult> CreatePointsToObjectsMap(Api.CreatePointsToObjectsMapRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreatePointsToObjectsMapRequest, Api.CreatePointsToObjectsMapResult>(executor, request, context, "relationship_operations.create_points_to_objects_map");

    [OperationImplementation("relationship_operations.make_cloud_to_swatch_relationship")]
    public override Task<Api.MakeCloudToSwatchRelationshipResult> MakeCloudToSwatchRelationship(Api.MakeCloudToSwatchRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeCloudToSwatchRelationshipRequest, Api.MakeCloudToSwatchRelationshipResult>(executor, request, context, "relationship_operations.make_cloud_to_swatch_relationship");

    [OperationImplementation("relationship_operations.make_geometry_compare_only_relationship")]
    public override Task<Api.MakeGeometryCompareOnlyRelationshipResult> MakeGeometryCompareOnlyRelationship(Api.MakeGeometryCompareOnlyRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGeometryCompareOnlyRelationshipRequest, Api.MakeGeometryCompareOnlyRelationshipResult>(executor, request, context, "relationship_operations.make_geometry_compare_only_relationship");

    [OperationImplementation("relationship_operations.make_dynamic_line_relationship")]
    public override Task<Api.MakeDynamicLineRelationshipResult> MakeDynamicLineRelationship(Api.MakeDynamicLineRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeDynamicLineRelationshipRequest, Api.MakeDynamicLineRelationshipResult>(executor, request, context, "relationship_operations.make_dynamic_line_relationship");

    [OperationImplementation("relationship_operations.auto_filter_clouds_to_nominal_geometry_2d")]
    public override Task<Api.AutoFilterCloudsToNominalGeometry2DResult> AutoFilterCloudsToNominalGeometry2D(Api.AutoFilterCloudsToNominalGeometry2DRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoFilterCloudsToNominalGeometry2DRequest, Api.AutoFilterCloudsToNominalGeometry2DResult>(executor, request, context, "relationship_operations.auto_filter_clouds_to_nominal_geometry_2d");

    [OperationImplementation("relationship_operations.make_dynamic_point_relationship")]
    public override Task<Api.MakeDynamicPointRelationshipResult> MakeDynamicPointRelationship(Api.MakeDynamicPointRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeDynamicPointRelationshipRequest, Api.MakeDynamicPointRelationshipResult>(executor, request, context, "relationship_operations.make_dynamic_point_relationship");

    [OperationImplementation("relationship_operations.make_object_to_object_direction_relationship")]
    public override Task<Api.MakeObjectToObjectDirectionRelationshipResult> MakeObjectToObjectDirectionRelationship(Api.MakeObjectToObjectDirectionRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeObjectToObjectDirectionRelationshipRequest, Api.MakeObjectToObjectDirectionRelationshipResult>(executor, request, context, "relationship_operations.make_object_to_object_direction_relationship");

    [OperationImplementation("relationship_operations.get_points_to_points_relationship_associated_data")]
    public override Task<Api.GetPointsToPointsRelationshipAssociatedDataResult> GetPointsToPointsRelationshipAssociatedData(Api.GetPointsToPointsRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointsToPointsRelationshipAssociatedDataRequest, Api.GetPointsToPointsRelationshipAssociatedDataResult>(executor, request, context, "relationship_operations.get_points_to_points_relationship_associated_data");

    [OperationImplementation("relationship_operations.set_points_to_points_relationship_associated_data")]
    public override Task<Api.SetPointsToPointsRelationshipAssociatedDataResult> SetPointsToPointsRelationshipAssociatedData(Api.SetPointsToPointsRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointsToPointsRelationshipAssociatedDataRequest, Api.SetPointsToPointsRelationshipAssociatedDataResult>(executor, request, context, "relationship_operations.set_points_to_points_relationship_associated_data");

    [OperationImplementation("relationship_operations.get_point_to_point_relationship_statistics")]
    public override Task<Api.GetPointToPointRelationshipStatisticsResult> GetPointToPointRelationshipStatistics(Api.GetPointToPointRelationshipStatisticsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointToPointRelationshipStatisticsRequest, Api.GetPointToPointRelationshipStatisticsResult>(executor, request, context, "relationship_operations.get_point_to_point_relationship_statistics");

    [OperationImplementation("relationship_operations.make_frame_to_frame_relationship")]
    public override Task<Api.MakeFrameToFrameRelationshipResult> MakeFrameToFrameRelationship(Api.MakeFrameToFrameRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeFrameToFrameRelationshipRequest, Api.MakeFrameToFrameRelationshipResult>(executor, request, context, "relationship_operations.make_frame_to_frame_relationship");

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_relative_polarity")]
    public override Task<Api.SetVectorGroupToVectorGroupRelativePolarityResult> SetVectorGroupToVectorGroupRelativePolarity(Api.SetVectorGroupToVectorGroupRelativePolarityRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorGroupToVectorGroupRelativePolarityRequest, Api.SetVectorGroupToVectorGroupRelativePolarityResult>(executor, request, context, "relationship_operations.set_vector_group_to_vector_group_relative_polarity");

    [OperationImplementation("relationship_operations.make_points_to_objects_relationship")]
    public override Task<Api.MakePointsToObjectsRelationshipResult> MakePointsToObjectsRelationship(Api.MakePointsToObjectsRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakePointsToObjectsRelationshipRequest, Api.MakePointsToObjectsRelationshipResult>(executor, request, context, "relationship_operations.make_points_to_objects_relationship");

    [OperationImplementation("relationship_operations.make_group_to_group_relationship")]
    public override Task<Api.MakeGroupToGroupRelationshipResult> MakeGroupToGroupRelationship(Api.MakeGroupToGroupRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGroupToGroupRelationshipRequest, Api.MakeGroupToGroupRelationshipResult>(executor, request, context, "relationship_operations.make_group_to_group_relationship");

    [OperationImplementation("relationship_operations.delete_relationship")]
    public override Task<Api.DeleteRelationshipResult> DeleteRelationship(Api.DeleteRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteRelationshipRequest, Api.DeleteRelationshipResult>(executor, request, context, "relationship_operations.delete_relationship");

    [OperationImplementation("relationship_operations.make_average_point_relationship")]
    public override Task<Api.MakeAveragePointRelationshipResult> MakeAveragePointRelationship(Api.MakeAveragePointRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeAveragePointRelationshipRequest, Api.MakeAveragePointRelationshipResult>(executor, request, context, "relationship_operations.make_average_point_relationship");

    [OperationImplementation("relationship_operations.set_optimization_perturbation_parameters")]
    public override Task<Api.SetOptimizationPerturbationParametersResult> SetOptimizationPerturbationParameters(Api.SetOptimizationPerturbationParametersRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetOptimizationPerturbationParametersRequest, Api.SetOptimizationPerturbationParametersResult>(executor, request, context, "relationship_operations.set_optimization_perturbation_parameters");

    [OperationImplementation("relationship_operations.get_objects_from_points_to_objects_map_point_list")]
    public override Task<Api.GetObjectsFromPointsToObjectsMapPointListResult> GetObjectsFromPointsToObjectsMapPointList(Api.GetObjectsFromPointsToObjectsMapPointListRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetObjectsFromPointsToObjectsMapPointListRequest, Api.GetObjectsFromPointsToObjectsMapPointListResult>(executor, request, context, "relationship_operations.get_objects_from_points_to_objects_map_point_list");

    [OperationImplementation("relationship_operations.set_optimization_search_options")]
    public override Task<Api.SetOptimizationSearchOptionsResult> SetOptimizationSearchOptions(Api.SetOptimizationSearchOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetOptimizationSearchOptionsRequest, Api.SetOptimizationSearchOptionsResult>(executor, request, context, "relationship_operations.set_optimization_search_options");

    [OperationImplementation("relationship_operations.get_relationship_sigmoidal_gap_fit_constraints")]
    public override Task<Api.GetRelationshipSigmoidalGapFitConstraintsResult> GetRelationshipSigmoidalGapFitConstraints(Api.GetRelationshipSigmoidalGapFitConstraintsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipSigmoidalGapFitConstraintsRequest, Api.GetRelationshipSigmoidalGapFitConstraintsResult>(executor, request, context, "relationship_operations.get_relationship_sigmoidal_gap_fit_constraints");

    [OperationImplementation("relationship_operations.relationship_watch_window_template")]
    public override Task<Api.RelationshipWatchWindowTemplateResult> RelationshipWatchWindowTemplate(Api.RelationshipWatchWindowTemplateRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RelationshipWatchWindowTemplateRequest, Api.RelationshipWatchWindowTemplateResult>(executor, request, context, "relationship_operations.relationship_watch_window_template");

    [OperationImplementation("relationship_operations.extract_geometry_from_point_clouds")]
    public override Task<Api.ExtractGeometryFromPointCloudsResult> ExtractGeometryFromPointClouds(Api.ExtractGeometryFromPointCloudsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExtractGeometryFromPointCloudsRequest, Api.ExtractGeometryFromPointCloudsResult>(executor, request, context, "relationship_operations.extract_geometry_from_point_clouds");

    [OperationImplementation("relationship_operations.make_groups_to_objects_relationship")]
    public override Task<Api.MakeGroupsToObjectsRelationshipResult> MakeGroupsToObjectsRelationship(Api.MakeGroupsToObjectsRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGroupsToObjectsRelationshipRequest, Api.MakeGroupsToObjectsRelationshipResult>(executor, request, context, "relationship_operations.make_groups_to_objects_relationship");

    [OperationImplementation("relationship_operations.compute_geometry_relationship_uncertainties")]
    public override Task<Api.ComputeGeometryRelationshipUncertaintiesResult> ComputeGeometryRelationshipUncertainties(Api.ComputeGeometryRelationshipUncertaintiesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ComputeGeometryRelationshipUncertaintiesRequest, Api.ComputeGeometryRelationshipUncertaintiesResult>(executor, request, context, "relationship_operations.compute_geometry_relationship_uncertainties");

    [OperationImplementation("relationship_operations.get_points_to_objects_relationship_statistics")]
    public override Task<Api.GetPointsToObjectsRelationshipStatisticsResult> GetPointsToObjectsRelationshipStatistics(Api.GetPointsToObjectsRelationshipStatisticsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointsToObjectsRelationshipStatisticsRequest, Api.GetPointsToObjectsRelationshipStatisticsResult>(executor, request, context, "relationship_operations.get_points_to_objects_relationship_statistics");

    [OperationImplementation("relationship_operations.get_relationship_status")]
    public override Task<Api.GetRelationshipStatusResult> GetRelationshipStatus(Api.GetRelationshipStatusRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipStatusRequest, Api.GetRelationshipStatusResult>(executor, request, context, "relationship_operations.get_relationship_status");

    [OperationImplementation("relationship_operations.auto_filter_points_to_nominal_geometry_3d")]
    public override Task<Api.AutoFilterPointsToNominalGeometry3DResult> AutoFilterPointsToNominalGeometry3D(Api.AutoFilterPointsToNominalGeometry3DRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoFilterPointsToNominalGeometry3DRequest, Api.AutoFilterPointsToNominalGeometry3DResult>(executor, request, context, "relationship_operations.auto_filter_points_to_nominal_geometry_3d");

    [OperationImplementation("relationship_operations.make_geometry_fit_only_relationship")]
    public override Task<Api.MakeGeometryFitOnlyRelationshipResult> MakeGeometryFitOnlyRelationship(Api.MakeGeometryFitOnlyRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGeometryFitOnlyRelationshipRequest, Api.MakeGeometryFitOnlyRelationshipResult>(executor, request, context, "relationship_operations.make_geometry_fit_only_relationship");

    [OperationImplementation("relationship_operations.make_point_clouds_to_objects_relationship")]
    public override Task<Api.MakePointCloudsToObjectsRelationshipResult> MakePointCloudsToObjectsRelationship(Api.MakePointCloudsToObjectsRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakePointCloudsToObjectsRelationshipRequest, Api.MakePointCloudsToObjectsRelationshipResult>(executor, request, context, "relationship_operations.make_point_clouds_to_objects_relationship");

    [OperationImplementation("relationship_operations.edit_geometry_relationship_point_list")]
    public override Task<Api.EditGeometryRelationshipPointListResult> EditGeometryRelationshipPointList(Api.EditGeometryRelationshipPointListRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EditGeometryRelationshipPointListRequest, Api.EditGeometryRelationshipPointListResult>(executor, request, context, "relationship_operations.edit_geometry_relationship_point_list");

    [OperationImplementation("relationship_operations.get_general_relationship_statistics")]
    public override Task<Api.GetGeneralRelationshipStatisticsResult> GetGeneralRelationshipStatistics(Api.GetGeneralRelationshipStatisticsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeneralRelationshipStatisticsRequest, Api.GetGeneralRelationshipStatisticsResult>(executor, request, context, "relationship_operations.get_general_relationship_statistics");

    [OperationImplementation("relationship_operations.make_group_to_nominal_group_relationship")]
    public override Task<Api.MakeGroupToNominalGroupRelationshipResult> MakeGroupToNominalGroupRelationship(Api.MakeGroupToNominalGroupRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGroupToNominalGroupRelationshipRequest, Api.MakeGroupToNominalGroupRelationshipResult>(executor, request, context, "relationship_operations.make_group_to_nominal_group_relationship");

    [OperationImplementation("relationship_operations.generate_geometry_relationship_summary")]
    public override Task<Api.GenerateGeometryRelationshipSummaryResult> GenerateGeometryRelationshipSummary(Api.GenerateGeometryRelationshipSummaryRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GenerateGeometryRelationshipSummaryRequest, Api.GenerateGeometryRelationshipSummaryResult>(executor, request, context, "relationship_operations.generate_geometry_relationship_summary");

    [OperationImplementation("relationship_operations.move_collections_by_minimizing_relationships")]
    public override Task<Api.MoveCollectionsByMinimizingRelationshipsResult> MoveCollectionsByMinimizingRelationships(Api.MoveCollectionsByMinimizingRelationshipsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MoveCollectionsByMinimizingRelationshipsRequest, Api.MoveCollectionsByMinimizingRelationshipsResult>(executor, request, context, "relationship_operations.move_collections_by_minimizing_relationships");

    [OperationImplementation("relationship_operations.make_points_to_points_relationship")]
    public override Task<Api.MakePointsToPointsRelationshipResult> MakePointsToPointsRelationship(Api.MakePointsToPointsRelationshipRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakePointsToPointsRelationshipRequest, Api.MakePointsToPointsRelationshipResult>(executor, request, context, "relationship_operations.make_points_to_points_relationship");

    [OperationImplementation("relationship_operations.auto_filter_clouds_to_nominal_geometry_3d")]
    public override Task<Api.AutoFilterCloudsToNominalGeometry3DResult> AutoFilterCloudsToNominalGeometry3D(Api.AutoFilterCloudsToNominalGeometry3DRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoFilterCloudsToNominalGeometry3DRequest, Api.AutoFilterCloudsToNominalGeometry3DResult>(executor, request, context, "relationship_operations.auto_filter_clouds_to_nominal_geometry_3d");

}
