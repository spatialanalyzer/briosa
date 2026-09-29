using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal sealed partial class RelationshipOperationsService
{
    [OperationImplementation("relationship_operations.set_relationship_associated_data")]
    public override Task<Api.SetRelationshipAssociatedDataResult> SetRelationshipAssociatedData(Api.SetRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipAssociatedDataOperation.Descriptor,
            SetRelationshipAssociatedDataOperation.CreateCommand,
            SetRelationshipAssociatedDataOperation.OutputContracts,
            SetRelationshipAssociatedDataOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_vector_group_to_vector_group_relationship")]
    public override Task<Api.MakeVectorGroupToVectorGroupRelationshipResult> MakeVectorGroupToVectorGroupRelationship(Api.MakeVectorGroupToVectorGroupRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeVectorGroupToVectorGroupRelationshipOperation.Descriptor,
            MakeVectorGroupToVectorGroupRelationshipOperation.CreateCommand, MakeVectorGroupToVectorGroupRelationshipOperation.OutputContracts,
            MakeVectorGroupToVectorGroupRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.filter_geometry_relationship_outlier_cloud_points")]
    public override Task<Api.FilterGeometryRelationshipOutlierCloudPointsResult> FilterGeometryRelationshipOutlierCloudPoints(Api.FilterGeometryRelationshipOutlierCloudPointsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterGeometryRelationshipOutlierCloudPointsOperation.Descriptor,
            FilterGeometryRelationshipOutlierCloudPointsOperation.CreateCommand, FilterGeometryRelationshipOutlierCloudPointsOperation.OutputContracts,
            FilterGeometryRelationshipOutlierCloudPointsOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_cylindrical_zone")]
    public override Task<Api.SetVectorGroupToVectorGroupCylindricalZoneResult> SetVectorGroupToVectorGroupCylindricalZone(Api.SetVectorGroupToVectorGroupCylindricalZoneRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorGroupToVectorGroupCylindricalZoneOperation.Descriptor,
            SetVectorGroupToVectorGroupCylindricalZoneOperation.CreateCommand,
            SetVectorGroupToVectorGroupCylindricalZoneOperation.OutputContracts,
            SetVectorGroupToVectorGroupCylindricalZoneOperation.CreateResult);

    [OperationImplementation("relationship_operations.do_relationship_fit")]
    public override Task<Api.DoRelationshipFitResult> DoRelationshipFit(Api.DoRelationshipFitRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DoRelationshipFitOperation.Descriptor,
            DoRelationshipFitOperation.CreateCommand, DoRelationshipFitOperation.OutputContracts,
            DoRelationshipFitOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_criteria_name_list")]
    public override Task<Api.GetGeomRelationshipCriteriaNameListResult> GetGeomRelationshipCriteriaNameList(Api.GetGeomRelationshipCriteriaNameListRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipCriteriaNameListOperation.Descriptor,
            GetGeomRelationshipCriteriaNameListOperation.CreateCommand,
            GetGeomRelationshipCriteriaNameListOperation.OutputContracts,
            GetGeomRelationshipCriteriaNameListOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_group_to_nominal_group_view_zooming")]
    public override Task<Api.SetGroupToNominalGroupViewZoomingResult> SetGroupToNominalGroupViewZooming(Api.SetGroupToNominalGroupViewZoomingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGroupToNominalGroupViewZoomingOperation.Descriptor,
            SetGroupToNominalGroupViewZoomingOperation.CreateCommand,
            SetGroupToNominalGroupViewZoomingOperation.OutputContracts,
            SetGroupToNominalGroupViewZoomingOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_fit_weights")]
    public override Task<Api.SetVectorGroupToVectorGroupFitWeightsResult> SetVectorGroupToVectorGroupFitWeights(Api.SetVectorGroupToVectorGroupFitWeightsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorGroupToVectorGroupFitWeightsOperation.Descriptor,
            SetVectorGroupToVectorGroupFitWeightsOperation.CreateCommand,
            SetVectorGroupToVectorGroupFitWeightsOperation.OutputContracts,
            SetVectorGroupToVectorGroupFitWeightsOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_dynamic_ellipse_relationship")]
    public override Task<Api.MakeDynamicEllipseRelationshipResult> MakeDynamicEllipseRelationship(Api.MakeDynamicEllipseRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeDynamicEllipseRelationshipOperation.Descriptor,
            MakeDynamicEllipseRelationshipOperation.CreateCommand, MakeDynamicEllipseRelationshipOperation.OutputContracts,
            MakeDynamicEllipseRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_object_to_object_direction_relationship_tolerances")]
    public override Task<Api.SetObjectToObjectDirectionRelationshipTolerancesResult> SetObjectToObjectDirectionRelationshipTolerances(Api.SetObjectToObjectDirectionRelationshipTolerancesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObjectToObjectDirectionRelationshipTolerancesOperation.Descriptor,
            SetObjectToObjectDirectionRelationshipTolerancesOperation.CreateCommand, SetObjectToObjectDirectionRelationshipTolerancesOperation.OutputContracts,
            SetObjectToObjectDirectionRelationshipTolerancesOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_fit_gradient_factor")]
    public override Task<Api.SetVectorGroupToVectorGroupFitGradientFactorResult> SetVectorGroupToVectorGroupFitGradientFactor(Api.SetVectorGroupToVectorGroupFitGradientFactorRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorGroupToVectorGroupFitGradientFactorOperation.Descriptor,
            SetVectorGroupToVectorGroupFitGradientFactorOperation.CreateCommand,
            SetVectorGroupToVectorGroupFitGradientFactorOperation.OutputContracts,
            SetVectorGroupToVectorGroupFitGradientFactorOperation.CreateResult);

    [OperationImplementation("relationship_operations.start_stop_relationship_trapping")]
    public override Task<Api.StartStopRelationshipTrappingResult> StartStopRelationshipTrapping(Api.StartStopRelationshipTrappingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartStopRelationshipTrappingOperation.Descriptor,
            StartStopRelationshipTrappingOperation.CreateCommand,
            StartStopRelationshipTrappingOperation.OutputContracts,
            StartStopRelationshipTrappingOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_dynamic_circle_relationship")]
    public override Task<Api.MakeDynamicCircleRelationshipResult> MakeDynamicCircleRelationship(Api.MakeDynamicCircleRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeDynamicCircleRelationshipOperation.Descriptor,
            MakeDynamicCircleRelationshipOperation.CreateCommand, MakeDynamicCircleRelationshipOperation.OutputContracts,
            MakeDynamicCircleRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_associated_data")]
    public override Task<Api.GetRelationshipAssociatedDataResult> GetRelationshipAssociatedData(Api.GetRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipAssociatedDataOperation.Descriptor,
            GetRelationshipAssociatedDataOperation.CreateCommand,
            GetRelationshipAssociatedDataOperation.OutputContracts,
            GetRelationshipAssociatedDataOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_dynamic_plane_relationship")]
    public override Task<Api.MakeDynamicPlaneRelationshipResult> MakeDynamicPlaneRelationship(Api.MakeDynamicPlaneRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeDynamicPlaneRelationshipOperation.Descriptor,
            MakeDynamicPlaneRelationshipOperation.CreateCommand, MakeDynamicPlaneRelationshipOperation.OutputContracts,
            MakeDynamicPlaneRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_point_to_point_relationship")]
    public override Task<Api.MakePointToPointRelationshipResult> MakePointToPointRelationship(Api.MakePointToPointRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakePointToPointRelationshipOperation.Descriptor,
            MakePointToPointRelationshipOperation.CreateCommand,
            MakePointToPointRelationshipOperation.OutputContracts,
            MakePointToPointRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.auto_filter_points_groups_clouds_to_surface_faces")]
    public override Task<Api.AutoFilterPointsGroupsCloudsToSurfaceFacesResult> AutoFilterPointsGroupsCloudsToSurfaceFaces(Api.AutoFilterPointsGroupsCloudsToSurfaceFacesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoFilterPointsGroupsCloudsToSurfaceFacesOperation.Descriptor,
            AutoFilterPointsGroupsCloudsToSurfaceFacesOperation.CreateCommand, AutoFilterPointsGroupsCloudsToSurfaceFacesOperation.OutputContracts,
            AutoFilterPointsGroupsCloudsToSurfaceFacesOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_geometry_fit_and_compare_to_nominal_relationship")]
    public override Task<Api.MakeGeometryFitAndCompareToNominalRelationshipResult> MakeGeometryFitAndCompareToNominalRelationship(Api.MakeGeometryFitAndCompareToNominalRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGeometryFitAndCompareToNominalRelationshipOperation.Descriptor,
            MakeGeometryFitAndCompareToNominalRelationshipOperation.CreateCommand, MakeGeometryFitAndCompareToNominalRelationshipOperation.OutputContracts,
            MakeGeometryFitAndCompareToNominalRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.create_points_to_objects_map")]
    public override Task<Api.CreatePointsToObjectsMapResult> CreatePointsToObjectsMap(Api.CreatePointsToObjectsMapRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreatePointsToObjectsMapOperation.Descriptor,
            CreatePointsToObjectsMapOperation.CreateCommand,
            CreatePointsToObjectsMapOperation.OutputContracts,
            CreatePointsToObjectsMapOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_cloud_to_swatch_relationship")]
    public override Task<Api.MakeCloudToSwatchRelationshipResult> MakeCloudToSwatchRelationship(Api.MakeCloudToSwatchRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeCloudToSwatchRelationshipOperation.Descriptor,
            MakeCloudToSwatchRelationshipOperation.CreateCommand, MakeCloudToSwatchRelationshipOperation.OutputContracts,
            MakeCloudToSwatchRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_geometry_compare_only_relationship")]
    public override Task<Api.MakeGeometryCompareOnlyRelationshipResult> MakeGeometryCompareOnlyRelationship(Api.MakeGeometryCompareOnlyRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGeometryCompareOnlyRelationshipOperation.Descriptor,
            MakeGeometryCompareOnlyRelationshipOperation.CreateCommand, MakeGeometryCompareOnlyRelationshipOperation.OutputContracts,
            MakeGeometryCompareOnlyRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_dynamic_line_relationship")]
    public override Task<Api.MakeDynamicLineRelationshipResult> MakeDynamicLineRelationship(Api.MakeDynamicLineRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeDynamicLineRelationshipOperation.Descriptor,
            MakeDynamicLineRelationshipOperation.CreateCommand, MakeDynamicLineRelationshipOperation.OutputContracts,
            MakeDynamicLineRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.auto_filter_clouds_to_nominal_geometry_2d")]
    public override Task<Api.AutoFilterCloudsToNominalGeometry2DResult> AutoFilterCloudsToNominalGeometry2D(Api.AutoFilterCloudsToNominalGeometry2DRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoFilterCloudsToNominalGeometry2DOperation.Descriptor,
            AutoFilterCloudsToNominalGeometry2DOperation.CreateCommand, AutoFilterCloudsToNominalGeometry2DOperation.OutputContracts,
            AutoFilterCloudsToNominalGeometry2DOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_dynamic_point_relationship")]
    public override Task<Api.MakeDynamicPointRelationshipResult> MakeDynamicPointRelationship(Api.MakeDynamicPointRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeDynamicPointRelationshipOperation.Descriptor,
            MakeDynamicPointRelationshipOperation.CreateCommand, MakeDynamicPointRelationshipOperation.OutputContracts,
            MakeDynamicPointRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_object_to_object_direction_relationship")]
    public override Task<Api.MakeObjectToObjectDirectionRelationshipResult> MakeObjectToObjectDirectionRelationship(Api.MakeObjectToObjectDirectionRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeObjectToObjectDirectionRelationshipOperation.Descriptor,
            MakeObjectToObjectDirectionRelationshipOperation.CreateCommand, MakeObjectToObjectDirectionRelationshipOperation.OutputContracts,
            MakeObjectToObjectDirectionRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_points_to_points_relationship_associated_data")]
    public override Task<Api.GetPointsToPointsRelationshipAssociatedDataResult> GetPointsToPointsRelationshipAssociatedData(Api.GetPointsToPointsRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointsToPointsRelationshipAssociatedDataOperation.Descriptor,
            GetPointsToPointsRelationshipAssociatedDataOperation.CreateCommand,
            GetPointsToPointsRelationshipAssociatedDataOperation.OutputContracts,
            GetPointsToPointsRelationshipAssociatedDataOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_points_to_points_relationship_associated_data")]
    public override Task<Api.SetPointsToPointsRelationshipAssociatedDataResult> SetPointsToPointsRelationshipAssociatedData(Api.SetPointsToPointsRelationshipAssociatedDataRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointsToPointsRelationshipAssociatedDataOperation.Descriptor,
            SetPointsToPointsRelationshipAssociatedDataOperation.CreateCommand,
            SetPointsToPointsRelationshipAssociatedDataOperation.OutputContracts,
            SetPointsToPointsRelationshipAssociatedDataOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_point_to_point_relationship_statistics")]
    public override Task<Api.GetPointToPointRelationshipStatisticsResult> GetPointToPointRelationshipStatistics(Api.GetPointToPointRelationshipStatisticsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointToPointRelationshipStatisticsOperation.Descriptor,
            GetPointToPointRelationshipStatisticsOperation.CreateCommand,
            GetPointToPointRelationshipStatisticsOperation.OutputContracts,
            GetPointToPointRelationshipStatisticsOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_frame_to_frame_relationship")]
    public override Task<Api.MakeFrameToFrameRelationshipResult> MakeFrameToFrameRelationship(Api.MakeFrameToFrameRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeFrameToFrameRelationshipOperation.Descriptor,
            MakeFrameToFrameRelationshipOperation.CreateCommand, MakeFrameToFrameRelationshipOperation.OutputContracts,
            MakeFrameToFrameRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_vector_group_to_vector_group_relative_polarity")]
    public override Task<Api.SetVectorGroupToVectorGroupRelativePolarityResult> SetVectorGroupToVectorGroupRelativePolarity(Api.SetVectorGroupToVectorGroupRelativePolarityRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorGroupToVectorGroupRelativePolarityOperation.Descriptor,
            SetVectorGroupToVectorGroupRelativePolarityOperation.CreateCommand,
            SetVectorGroupToVectorGroupRelativePolarityOperation.OutputContracts,
            SetVectorGroupToVectorGroupRelativePolarityOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_points_to_objects_relationship")]
    public override Task<Api.MakePointsToObjectsRelationshipResult> MakePointsToObjectsRelationship(Api.MakePointsToObjectsRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakePointsToObjectsRelationshipOperation.Descriptor,
            MakePointsToObjectsRelationshipOperation.CreateCommand, MakePointsToObjectsRelationshipOperation.OutputContracts,
            MakePointsToObjectsRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_group_to_group_relationship")]
    public override Task<Api.MakeGroupToGroupRelationshipResult> MakeGroupToGroupRelationship(Api.MakeGroupToGroupRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGroupToGroupRelationshipOperation.Descriptor,
            MakeGroupToGroupRelationshipOperation.CreateCommand, MakeGroupToGroupRelationshipOperation.OutputContracts,
            MakeGroupToGroupRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.delete_relationship")]
    public override Task<Api.DeleteRelationshipResult> DeleteRelationship(Api.DeleteRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteRelationshipOperation.Descriptor,
            DeleteRelationshipOperation.CreateCommand, DeleteRelationshipOperation.OutputContracts,
            DeleteRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_average_point_relationship")]
    public override Task<Api.MakeAveragePointRelationshipResult> MakeAveragePointRelationship(Api.MakeAveragePointRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeAveragePointRelationshipOperation.Descriptor,
            MakeAveragePointRelationshipOperation.CreateCommand,
            MakeAveragePointRelationshipOperation.OutputContracts,
            MakeAveragePointRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_optimization_perturbation_parameters")]
    public override Task<Api.SetOptimizationPerturbationParametersResult> SetOptimizationPerturbationParameters(
        Api.SetOptimizationPerturbationParametersRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetOptimizationPerturbationParametersOperation.Descriptor,
            SetOptimizationPerturbationParametersOperation.CreateCommand,
            SetOptimizationPerturbationParametersOperation.OutputContracts,
            SetOptimizationPerturbationParametersOperation.CreateResult);
    [OperationImplementation("relationship_operations.get_objects_from_points_to_objects_map_point_list")]
    public override Task<Api.GetObjectsFromPointsToObjectsMapPointListResult> GetObjectsFromPointsToObjectsMapPointList(
        Api.GetObjectsFromPointsToObjectsMapPointListRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetObjectsFromPointsToObjectsMapPointListOperation.Descriptor,
            GetObjectsFromPointsToObjectsMapPointListOperation.CreateCommand,
            GetObjectsFromPointsToObjectsMapPointListOperation.OutputContracts,
            GetObjectsFromPointsToObjectsMapPointListOperation.CreateResult);
    [OperationImplementation("relationship_operations.set_optimization_search_options")]
    public override Task<Api.SetOptimizationSearchOptionsResult> SetOptimizationSearchOptions(
        Api.SetOptimizationSearchOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetOptimizationSearchOptionsOperation.Descriptor,
            SetOptimizationSearchOptionsOperation.CreateCommand,
            SetOptimizationSearchOptionsOperation.OutputContracts,
            SetOptimizationSearchOptionsOperation.CreateResult);
    [OperationImplementation("relationship_operations.get_relationship_sigmoidal_gap_fit_constraints")]
    public override Task<Api.GetRelationshipSigmoidalGapFitConstraintsResult> GetRelationshipSigmoidalGapFitConstraints(Api.GetRelationshipSigmoidalGapFitConstraintsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipSigmoidalGapFitConstraintsOperation.Descriptor,
            GetRelationshipSigmoidalGapFitConstraintsOperation.CreateCommand,
            GetRelationshipSigmoidalGapFitConstraintsOperation.OutputContracts,
            GetRelationshipSigmoidalGapFitConstraintsOperation.CreateResult);

    [OperationImplementation("relationship_operations.relationship_watch_window_template")]
    public override Task<Api.RelationshipWatchWindowTemplateResult> RelationshipWatchWindowTemplate(Api.RelationshipWatchWindowTemplateRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RelationshipWatchWindowTemplateOperation.Descriptor,
            RelationshipWatchWindowTemplateOperation.CreateCommand, RelationshipWatchWindowTemplateOperation.OutputContracts,
            RelationshipWatchWindowTemplateOperation.CreateResult);

    [OperationImplementation("relationship_operations.extract_geometry_from_point_clouds")]
    public override Task<Api.ExtractGeometryFromPointCloudsResult> ExtractGeometryFromPointClouds(Api.ExtractGeometryFromPointCloudsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExtractGeometryFromPointCloudsOperation.Descriptor,
            ExtractGeometryFromPointCloudsOperation.CreateCommand, ExtractGeometryFromPointCloudsOperation.OutputContracts,
            ExtractGeometryFromPointCloudsOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_groups_to_objects_relationship")]
    public override Task<Api.MakeGroupsToObjectsRelationshipResult> MakeGroupsToObjectsRelationship(Api.MakeGroupsToObjectsRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGroupsToObjectsRelationshipOperation.Descriptor,
            MakeGroupsToObjectsRelationshipOperation.CreateCommand, MakeGroupsToObjectsRelationshipOperation.OutputContracts,
            MakeGroupsToObjectsRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.compute_geometry_relationship_uncertainties")]
    public override Task<Api.ComputeGeometryRelationshipUncertaintiesResult> ComputeGeometryRelationshipUncertainties(Api.ComputeGeometryRelationshipUncertaintiesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ComputeGeometryRelationshipUncertaintiesOperation.Descriptor,
            ComputeGeometryRelationshipUncertaintiesOperation.CreateCommand, ComputeGeometryRelationshipUncertaintiesOperation.OutputContracts,
            ComputeGeometryRelationshipUncertaintiesOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_points_to_objects_relationship_statistics")]
    public override Task<Api.GetPointsToObjectsRelationshipStatisticsResult> GetPointsToObjectsRelationshipStatistics(Api.GetPointsToObjectsRelationshipStatisticsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointsToObjectsRelationshipStatisticsOperation.Descriptor,
            GetPointsToObjectsRelationshipStatisticsOperation.CreateCommand,
            GetPointsToObjectsRelationshipStatisticsOperation.OutputContracts,
            GetPointsToObjectsRelationshipStatisticsOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_status")]
    public override Task<Api.GetRelationshipStatusResult> GetRelationshipStatus(Api.GetRelationshipStatusRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipStatusOperation.Descriptor,
            GetRelationshipStatusOperation.CreateCommand, GetRelationshipStatusOperation.OutputContracts,
            GetRelationshipStatusOperation.CreateResult);

    [OperationImplementation("relationship_operations.auto_filter_points_to_nominal_geometry_3d")]
    public override Task<Api.AutoFilterPointsToNominalGeometry3DResult> AutoFilterPointsToNominalGeometry3D(Api.AutoFilterPointsToNominalGeometry3DRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoFilterPointsToNominalGeometry3DOperation.Descriptor,
            AutoFilterPointsToNominalGeometry3DOperation.CreateCommand, AutoFilterPointsToNominalGeometry3DOperation.OutputContracts,
            AutoFilterPointsToNominalGeometry3DOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_geometry_fit_only_relationship")]
    public override Task<Api.MakeGeometryFitOnlyRelationshipResult> MakeGeometryFitOnlyRelationship(Api.MakeGeometryFitOnlyRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGeometryFitOnlyRelationshipOperation.Descriptor,
            MakeGeometryFitOnlyRelationshipOperation.CreateCommand, MakeGeometryFitOnlyRelationshipOperation.OutputContracts,
            MakeGeometryFitOnlyRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_point_clouds_to_objects_relationship")]
    public override Task<Api.MakePointCloudsToObjectsRelationshipResult> MakePointCloudsToObjectsRelationship(Api.MakePointCloudsToObjectsRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakePointCloudsToObjectsRelationshipOperation.Descriptor,
            MakePointCloudsToObjectsRelationshipOperation.CreateCommand, MakePointCloudsToObjectsRelationshipOperation.OutputContracts,
            MakePointCloudsToObjectsRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.edit_geometry_relationship_point_list")]
    public override Task<Api.EditGeometryRelationshipPointListResult> EditGeometryRelationshipPointList(Api.EditGeometryRelationshipPointListRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EditGeometryRelationshipPointListOperation.Descriptor,
            EditGeometryRelationshipPointListOperation.CreateCommand,
            EditGeometryRelationshipPointListOperation.OutputContracts,
            EditGeometryRelationshipPointListOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_general_relationship_statistics")]
    public override Task<Api.GetGeneralRelationshipStatisticsResult> GetGeneralRelationshipStatistics(Api.GetGeneralRelationshipStatisticsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeneralRelationshipStatisticsOperation.Descriptor,
            GetGeneralRelationshipStatisticsOperation.CreateCommand,
            GetGeneralRelationshipStatisticsOperation.OutputContracts,
            GetGeneralRelationshipStatisticsOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_group_to_nominal_group_relationship")]
    public override Task<Api.MakeGroupToNominalGroupRelationshipResult> MakeGroupToNominalGroupRelationship(Api.MakeGroupToNominalGroupRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGroupToNominalGroupRelationshipOperation.Descriptor,
            MakeGroupToNominalGroupRelationshipOperation.CreateCommand, MakeGroupToNominalGroupRelationshipOperation.OutputContracts,
            MakeGroupToNominalGroupRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.generate_geometry_relationship_summary")]
    public override Task<Api.GenerateGeometryRelationshipSummaryResult> GenerateGeometryRelationshipSummary(Api.GenerateGeometryRelationshipSummaryRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GenerateGeometryRelationshipSummaryOperation.Descriptor,
            GenerateGeometryRelationshipSummaryOperation.CreateCommand, GenerateGeometryRelationshipSummaryOperation.OutputContracts,
            GenerateGeometryRelationshipSummaryOperation.CreateResult);

    [OperationImplementation("relationship_operations.move_collections_by_minimizing_relationships")]
    public override Task<Api.MoveCollectionsByMinimizingRelationshipsResult> MoveCollectionsByMinimizingRelationships(Api.MoveCollectionsByMinimizingRelationshipsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MoveCollectionsByMinimizingRelationshipsOperation.Descriptor,
            MoveCollectionsByMinimizingRelationshipsOperation.CreateCommand, MoveCollectionsByMinimizingRelationshipsOperation.OutputContracts,
            MoveCollectionsByMinimizingRelationshipsOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_points_to_points_relationship")]
    public override Task<Api.MakePointsToPointsRelationshipResult> MakePointsToPointsRelationship(Api.MakePointsToPointsRelationshipRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakePointsToPointsRelationshipOperation.Descriptor,
            MakePointsToPointsRelationshipOperation.CreateCommand, MakePointsToPointsRelationshipOperation.OutputContracts,
            MakePointsToPointsRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.auto_filter_clouds_to_nominal_geometry_3d")]
    public override Task<Api.AutoFilterCloudsToNominalGeometry3DResult> AutoFilterCloudsToNominalGeometry3D(Api.AutoFilterCloudsToNominalGeometry3DRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoFilterCloudsToNominalGeometry3DOperation.Descriptor,
            AutoFilterCloudsToNominalGeometry3DOperation.CreateCommand, AutoFilterCloudsToNominalGeometry3DOperation.OutputContracts,
            AutoFilterCloudsToNominalGeometry3DOperation.CreateResult);

}
