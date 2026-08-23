using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal sealed class CloudAndMeshOperationsService(OperationExecutor executor)
    : Api.CloudAndMeshOperations.CloudAndMeshOperationsBase
{
    [OperationImplementation("cloud_and_mesh_operations.cloud_display_control")]
    public override Task<Api.CloudDisplayControlResult> CloudDisplayControl(Api.CloudDisplayControlRequest request, ServerCallContext context) =>
        Execute<Api.CloudDisplayControlRequest, Api.CloudDisplayControlResult>(request, context, "cloud_and_mesh_operations.cloud_display_control");

    [OperationImplementation("cloud_and_mesh_operations.reset_cloud_bounding_box")]
    public override Task<Api.ResetCloudBoundingBoxResult> ResetCloudBoundingBox(Api.ResetCloudBoundingBoxRequest request, ServerCallContext context) =>
        Execute<Api.ResetCloudBoundingBoxRequest, Api.ResetCloudBoundingBoxResult>(request, context, "cloud_and_mesh_operations.reset_cloud_bounding_box");

    [OperationImplementation("cloud_and_mesh_operations.get_cloud_point_count")]
    public override Task<Api.GetCloudPointCountResult> GetCloudPointCount(Api.GetCloudPointCountRequest request, ServerCallContext context) =>
        Execute<Api.GetCloudPointCountRequest, Api.GetCloudPointCountResult>(request, context, "cloud_and_mesh_operations.get_cloud_point_count");

    [OperationImplementation("cloud_and_mesh_operations.set_cloud_default_clipping_plane")]
    public override Task<Api.SetCloudDefaultClippingPlaneResult> SetCloudDefaultClippingPlane(Api.SetCloudDefaultClippingPlaneRequest request, ServerCallContext context) =>
        Execute<Api.SetCloudDefaultClippingPlaneRequest, Api.SetCloudDefaultClippingPlaneResult>(request, context, "cloud_and_mesh_operations.set_cloud_default_clipping_plane");

    [OperationImplementation("cloud_and_mesh_operations.raster_scan_edge_inspection")]
    public override Task<Api.RasterScanEdgeInspectionResult> RasterScanEdgeInspection(Api.RasterScanEdgeInspectionRequest request, ServerCallContext context) =>
        Execute<Api.RasterScanEdgeInspectionRequest, Api.RasterScanEdgeInspectionResult>(request, context, "cloud_and_mesh_operations.raster_scan_edge_inspection");

    [OperationImplementation("cloud_and_mesh_operations.new_raster_scan_edge_inspection")]
    public override Task<Api.NewRasterScanEdgeInspectionResult> NewRasterScanEdgeInspection(Api.NewRasterScanEdgeInspectionRequest request, ServerCallContext context) =>
        Execute<Api.NewRasterScanEdgeInspectionRequest, Api.NewRasterScanEdgeInspectionResult>(request, context, "cloud_and_mesh_operations.new_raster_scan_edge_inspection");

    [OperationImplementation("cloud_and_mesh_operations.clear_cloud_point_deviations")]
    public override Task<Api.ClearCloudPointDeviationsResult> ClearCloudPointDeviations(Api.ClearCloudPointDeviationsRequest request, ServerCallContext context) =>
        Execute<Api.ClearCloudPointDeviationsRequest, Api.ClearCloudPointDeviationsResult>(request, context, "cloud_and_mesh_operations.clear_cloud_point_deviations");

    [OperationImplementation("cloud_and_mesh_operations.enable_all_cloud_cross_sections")]
    public override Task<Api.EnableAllCloudCrossSectionsResult> EnableAllCloudCrossSections(Api.EnableAllCloudCrossSectionsRequest request, ServerCallContext context) =>
        Execute<Api.EnableAllCloudCrossSectionsRequest, Api.EnableAllCloudCrossSectionsResult>(request, context, "cloud_and_mesh_operations.enable_all_cloud_cross_sections");

    [OperationImplementation("cloud_and_mesh_operations.enable_disable_cloud_cross_sections")]
    public override Task<Api.EnableDisableCloudCrossSectionsResult> EnableDisableCloudCrossSections(Api.EnableDisableCloudCrossSectionsRequest request, ServerCallContext context) =>
        Execute<Api.EnableDisableCloudCrossSectionsRequest, Api.EnableDisableCloudCrossSectionsResult>(request, context, "cloud_and_mesh_operations.enable_disable_cloud_cross_sections");

    [OperationImplementation("cloud_and_mesh_operations.enable_single_cloud_cross_section")]
    public override Task<Api.EnableSingleCloudCrossSectionResult> EnableSingleCloudCrossSection(Api.EnableSingleCloudCrossSectionRequest request, ServerCallContext context) =>
        Execute<Api.EnableSingleCloudCrossSectionRequest, Api.EnableSingleCloudCrossSectionResult>(request, context, "cloud_and_mesh_operations.enable_single_cloud_cross_section");

    [OperationImplementation("cloud_and_mesh_operations.get_number_of_cross_sections_in_cross_section_cloud")]
    public override Task<Api.GetNumberOfCrossSectionsInCrossSectionCloudResult> GetNumberOfCrossSectionsInCrossSectionCloud(Api.GetNumberOfCrossSectionsInCrossSectionCloudRequest request, ServerCallContext context) =>
        Execute<Api.GetNumberOfCrossSectionsInCrossSectionCloudRequest, Api.GetNumberOfCrossSectionsInCrossSectionCloudResult>(request, context, "cloud_and_mesh_operations.get_number_of_cross_sections_in_cross_section_cloud");

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_plane")]
    public override Task<Api.FilterCloudsToPlaneResult> FilterCloudsToPlane(Api.FilterCloudsToPlaneRequest request, ServerCallContext context) =>
        Execute<Api.FilterCloudsToPlaneRequest, Api.FilterCloudsToPlaneResult>(request, context, "cloud_and_mesh_operations.filter_clouds_to_plane");

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_group")]
    public override Task<Api.FilterCloudsToGroupResult> FilterCloudsToGroup(Api.FilterCloudsToGroupRequest request, ServerCallContext context) =>
        Execute<Api.FilterCloudsToGroupRequest, Api.FilterCloudsToGroupResult>(request, context, "cloud_and_mesh_operations.filter_clouds_to_group");

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_surface")]
    public override Task<Api.FilterCloudsToSurfaceResult> FilterCloudsToSurface(Api.FilterCloudsToSurfaceRequest request, ServerCallContext context) =>
        Execute<Api.FilterCloudsToSurfaceRequest, Api.FilterCloudsToSurfaceResult>(request, context, "cloud_and_mesh_operations.filter_clouds_to_surface");

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_bsplines")]
    public override Task<Api.FilterCloudsToBSplinesResult> FilterCloudsToBSplines(Api.FilterCloudsToBSplinesRequest request, ServerCallContext context) =>
        Execute<Api.FilterCloudsToBSplinesRequest, Api.FilterCloudsToBSplinesResult>(request, context, "cloud_and_mesh_operations.filter_clouds_to_bsplines");

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_line_segment")]
    public override Task<Api.FilterCloudsToLineSegmentResult> FilterCloudsToLineSegment(Api.FilterCloudsToLineSegmentRequest request, ServerCallContext context) =>
        Execute<Api.FilterCloudsToLineSegmentRequest, Api.FilterCloudsToLineSegmentResult>(request, context, "cloud_and_mesh_operations.filter_clouds_to_line_segment");

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_points")]
    public override Task<Api.FilterCloudsToVectorGroupsResolvePointsResult> FilterCloudsToVectorGroupsResolvePoints(Api.FilterCloudsToVectorGroupsResolvePointsRequest request, ServerCallContext context) =>
        Execute<Api.FilterCloudsToVectorGroupsResolvePointsRequest, Api.FilterCloudsToVectorGroupsResolvePointsResult>(request, context, "cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_points");

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_clouds")]
    public override Task<Api.FilterCloudsToVectorGroupsResolveCloudsResult> FilterCloudsToVectorGroupsResolveClouds(Api.FilterCloudsToVectorGroupsResolveCloudsRequest request, ServerCallContext context) =>
        Execute<Api.FilterCloudsToVectorGroupsResolveCloudsRequest, Api.FilterCloudsToVectorGroupsResolveCloudsResult>(request, context, "cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_clouds");

    [OperationImplementation("cloud_and_mesh_operations.rgb_cloud_point_filter")]
    public override Task<Api.RGBCloudPointFilterResult> RGBCloudPointFilter(Api.RGBCloudPointFilterRequest request, ServerCallContext context) =>
        Execute<Api.RGBCloudPointFilterRequest, Api.RGBCloudPointFilterResult>(request, context, "cloud_and_mesh_operations.rgb_cloud_point_filter");

    [OperationImplementation("cloud_and_mesh_operations.get_cloud_rgb_values")]
    public override Task<Api.GetCloudRGBValuesResult> GetCloudRGBValues(Api.GetCloudRGBValuesRequest request, ServerCallContext context) =>
        Execute<Api.GetCloudRGBValuesRequest, Api.GetCloudRGBValuesResult>(request, context, "cloud_and_mesh_operations.get_cloud_rgb_values");

    [OperationImplementation("cloud_and_mesh_operations.get_cloud_rgb_values_near_point")]
    public override Task<Api.GetCloudRGBValuesNearPointResult> GetCloudRGBValuesNearPoint(Api.GetCloudRGBValuesNearPointRequest request, ServerCallContext context) =>
        Execute<Api.GetCloudRGBValuesNearPointRequest, Api.GetCloudRGBValuesNearPointResult>(request, context, "cloud_and_mesh_operations.get_cloud_rgb_values_near_point");

    [OperationImplementation("cloud_and_mesh_operations.subdivide_cloud_by_point_spacing")]
    public override Task<Api.SubdivideCloudByPointSpacingResult> SubdivideCloudByPointSpacing(Api.SubdivideCloudByPointSpacingRequest request, ServerCallContext context) =>
        Execute<Api.SubdivideCloudByPointSpacingRequest, Api.SubdivideCloudByPointSpacingResult>(request, context, "cloud_and_mesh_operations.subdivide_cloud_by_point_spacing");

    [OperationImplementation("cloud_and_mesh_operations.delete_cloud_points_by_radial_distance_from_points")]
    public override Task<Api.DeleteCloudPointsByRadialDistanceFromPointsResult> DeleteCloudPointsByRadialDistanceFromPoints(Api.DeleteCloudPointsByRadialDistanceFromPointsRequest request, ServerCallContext context) =>
        Execute<Api.DeleteCloudPointsByRadialDistanceFromPointsRequest, Api.DeleteCloudPointsByRadialDistanceFromPointsResult>(request, context, "cloud_and_mesh_operations.delete_cloud_points_by_radial_distance_from_points");

    [OperationImplementation("cloud_and_mesh_operations.delete_cloud_points_by_xyz_range")]
    public override Task<Api.DeleteCloudPointsByXYZRangeResult> DeleteCloudPointsByXYZRange(Api.DeleteCloudPointsByXYZRangeRequest request, ServerCallContext context) =>
        Execute<Api.DeleteCloudPointsByXYZRangeRequest, Api.DeleteCloudPointsByXYZRangeResult>(request, context, "cloud_and_mesh_operations.delete_cloud_points_by_xyz_range");

    [OperationImplementation("cloud_and_mesh_operations.generate_general_mesh")]
    public override Task<Api.GenerateGeneralMeshResult> GenerateGeneralMesh(Api.GenerateGeneralMeshRequest request, ServerCallContext context) =>
        Execute<Api.GenerateGeneralMeshRequest, Api.GenerateGeneralMeshResult>(request, context, "cloud_and_mesh_operations.generate_general_mesh");

    [OperationImplementation("cloud_and_mesh_operations.consolidate_mesh")]
    public override Task<Api.ConsolidateMeshResult> ConsolidateMesh(Api.ConsolidateMeshRequest request, ServerCallContext context) =>
        Execute<Api.ConsolidateMeshRequest, Api.ConsolidateMeshResult>(request, context, "cloud_and_mesh_operations.consolidate_mesh");

    [OperationImplementation("cloud_and_mesh_operations.mesh_volume")]
    public override Task<Api.MeshVolumeResult> MeshVolume(Api.MeshVolumeRequest request, ServerCallContext context) =>
        Execute<Api.MeshVolumeRequest, Api.MeshVolumeResult>(request, context, "cloud_and_mesh_operations.mesh_volume");

    [OperationImplementation("cloud_and_mesh_operations.mesh_fill_holes")]
    public override Task<Api.MeshFillHolesResult> MeshFillHoles(Api.MeshFillHolesRequest request, ServerCallContext context) =>
        Execute<Api.MeshFillHolesRequest, Api.MeshFillHolesResult>(request, context, "cloud_and_mesh_operations.mesh_fill_holes");

    private Task<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        string operationId)
        where TRequest : class, Google.Protobuf.IMessage<TRequest>
        where TResponse : class, Google.Protobuf.IMessage<TResponse>, new() =>
        MpOperationServiceExecutor.ExecuteAsync<TRequest, TResponse>(
            executor,
            request,
            context,
            operationId);
}
