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
        executor.ExecuteAsync(request, context, CloudDisplayControlOperation.Descriptor,
            CloudDisplayControlOperation.CreateCommand, CloudDisplayControlOperation.OutputContracts, CloudDisplayControlOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.reset_cloud_bounding_box")]
    public override Task<Api.ResetCloudBoundingBoxResult> ResetCloudBoundingBox(Api.ResetCloudBoundingBoxRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ResetCloudBoundingBoxOperation.Descriptor,
            ResetCloudBoundingBoxOperation.CreateCommand, ResetCloudBoundingBoxOperation.OutputContracts, ResetCloudBoundingBoxOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.get_cloud_point_count")]
    public override Task<Api.GetCloudPointCountResult> GetCloudPointCount(Api.GetCloudPointCountRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCloudPointCountOperation.Descriptor,
            GetCloudPointCountOperation.CreateCommand, GetCloudPointCountOperation.OutputContracts, GetCloudPointCountOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.set_cloud_default_clipping_plane")]
    public override Task<Api.SetCloudDefaultClippingPlaneResult> SetCloudDefaultClippingPlane(Api.SetCloudDefaultClippingPlaneRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCloudDefaultClippingPlaneOperation.Descriptor,
            SetCloudDefaultClippingPlaneOperation.CreateCommand, SetCloudDefaultClippingPlaneOperation.OutputContracts, SetCloudDefaultClippingPlaneOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.raster_scan_edge_inspection")]
    public override Task<Api.RasterScanEdgeInspectionResult> RasterScanEdgeInspection(Api.RasterScanEdgeInspectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RasterScanEdgeInspectionOperation.Descriptor,
            RasterScanEdgeInspectionOperation.CreateCommand, RasterScanEdgeInspectionOperation.OutputContracts, RasterScanEdgeInspectionOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.new_raster_scan_edge_inspection")]
    public override Task<Api.NewRasterScanEdgeInspectionResult> NewRasterScanEdgeInspection(Api.NewRasterScanEdgeInspectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, NewRasterScanEdgeInspectionOperation.Descriptor,
            NewRasterScanEdgeInspectionOperation.CreateCommand, NewRasterScanEdgeInspectionOperation.OutputContracts, NewRasterScanEdgeInspectionOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.clear_cloud_point_deviations")]
    public override Task<Api.ClearCloudPointDeviationsResult> ClearCloudPointDeviations(Api.ClearCloudPointDeviationsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ClearCloudPointDeviationsOperation.Descriptor,
            ClearCloudPointDeviationsOperation.CreateCommand, ClearCloudPointDeviationsOperation.OutputContracts, ClearCloudPointDeviationsOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.enable_all_cloud_cross_sections")]
    public override Task<Api.EnableAllCloudCrossSectionsResult> EnableAllCloudCrossSections(Api.EnableAllCloudCrossSectionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableAllCloudCrossSectionsOperation.Descriptor,
            EnableAllCloudCrossSectionsOperation.CreateCommand, EnableAllCloudCrossSectionsOperation.OutputContracts, EnableAllCloudCrossSectionsOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.enable_disable_cloud_cross_sections")]
    public override Task<Api.EnableDisableCloudCrossSectionsResult> EnableDisableCloudCrossSections(Api.EnableDisableCloudCrossSectionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisableCloudCrossSectionsOperation.Descriptor,
            EnableDisableCloudCrossSectionsOperation.CreateCommand, EnableDisableCloudCrossSectionsOperation.OutputContracts, EnableDisableCloudCrossSectionsOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.enable_single_cloud_cross_section")]
    public override Task<Api.EnableSingleCloudCrossSectionResult> EnableSingleCloudCrossSection(Api.EnableSingleCloudCrossSectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableSingleCloudCrossSectionOperation.Descriptor,
            EnableSingleCloudCrossSectionOperation.CreateCommand, EnableSingleCloudCrossSectionOperation.OutputContracts, EnableSingleCloudCrossSectionOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.get_number_of_cross_sections_in_cross_section_cloud")]
    public override Task<Api.GetNumberOfCrossSectionsInCrossSectionCloudResult> GetNumberOfCrossSectionsInCrossSectionCloud(Api.GetNumberOfCrossSectionsInCrossSectionCloudRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfCrossSectionsInCrossSectionCloudOperation.Descriptor,
            GetNumberOfCrossSectionsInCrossSectionCloudOperation.CreateCommand, GetNumberOfCrossSectionsInCrossSectionCloudOperation.OutputContracts, GetNumberOfCrossSectionsInCrossSectionCloudOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_plane")]
    public override Task<Api.FilterCloudsToPlaneResult> FilterCloudsToPlane(Api.FilterCloudsToPlaneRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterCloudsToPlaneOperation.Descriptor,
            FilterCloudsToPlaneOperation.CreateCommand, FilterCloudsToPlaneOperation.OutputContracts, FilterCloudsToPlaneOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_group")]
    public override Task<Api.FilterCloudsToGroupResult> FilterCloudsToGroup(Api.FilterCloudsToGroupRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterCloudsToGroupOperation.Descriptor,
            FilterCloudsToGroupOperation.CreateCommand, FilterCloudsToGroupOperation.OutputContracts, FilterCloudsToGroupOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_surface")]
    public override Task<Api.FilterCloudsToSurfaceResult> FilterCloudsToSurface(Api.FilterCloudsToSurfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterCloudsToSurfaceOperation.Descriptor,
            FilterCloudsToSurfaceOperation.CreateCommand, FilterCloudsToSurfaceOperation.OutputContracts, FilterCloudsToSurfaceOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_bsplines")]
    public override Task<Api.FilterCloudsToBSplinesResult> FilterCloudsToBSplines(Api.FilterCloudsToBSplinesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterCloudsToBSplinesOperation.Descriptor,
            FilterCloudsToBSplinesOperation.CreateCommand, FilterCloudsToBSplinesOperation.OutputContracts, FilterCloudsToBSplinesOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_line_segment")]
    public override Task<Api.FilterCloudsToLineSegmentResult> FilterCloudsToLineSegment(Api.FilterCloudsToLineSegmentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterCloudsToLineSegmentOperation.Descriptor,
            FilterCloudsToLineSegmentOperation.CreateCommand, FilterCloudsToLineSegmentOperation.OutputContracts, FilterCloudsToLineSegmentOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_points")]
    public override Task<Api.FilterCloudsToVectorGroupsResolvePointsResult> FilterCloudsToVectorGroupsResolvePoints(Api.FilterCloudsToVectorGroupsResolvePointsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterCloudsToVectorGroupsResolvePointsOperation.Descriptor,
            FilterCloudsToVectorGroupsResolvePointsOperation.CreateCommand, FilterCloudsToVectorGroupsResolvePointsOperation.OutputContracts, FilterCloudsToVectorGroupsResolvePointsOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.filter_clouds_to_vector_groups_resolve_clouds")]
    public override Task<Api.FilterCloudsToVectorGroupsResolveCloudsResult> FilterCloudsToVectorGroupsResolveClouds(Api.FilterCloudsToVectorGroupsResolveCloudsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FilterCloudsToVectorGroupsResolveCloudsOperation.Descriptor,
            FilterCloudsToVectorGroupsResolveCloudsOperation.CreateCommand, FilterCloudsToVectorGroupsResolveCloudsOperation.OutputContracts, FilterCloudsToVectorGroupsResolveCloudsOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.rgb_cloud_point_filter")]
    public override Task<Api.RGBCloudPointFilterResult> RGBCloudPointFilter(Api.RGBCloudPointFilterRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RGBCloudPointFilterOperation.Descriptor,
            RGBCloudPointFilterOperation.CreateCommand, RGBCloudPointFilterOperation.OutputContracts, RGBCloudPointFilterOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.get_cloud_rgb_values")]
    public override Task<Api.GetCloudRGBValuesResult> GetCloudRGBValues(Api.GetCloudRGBValuesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCloudRGBValuesOperation.Descriptor,
            GetCloudRGBValuesOperation.CreateCommand, GetCloudRGBValuesOperation.OutputContracts, GetCloudRGBValuesOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.get_cloud_rgb_values_near_point")]
    public override Task<Api.GetCloudRGBValuesNearPointResult> GetCloudRGBValuesNearPoint(Api.GetCloudRGBValuesNearPointRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCloudRGBValuesNearPointOperation.Descriptor,
            GetCloudRGBValuesNearPointOperation.CreateCommand, GetCloudRGBValuesNearPointOperation.OutputContracts, GetCloudRGBValuesNearPointOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.subdivide_cloud_by_point_spacing")]
    public override Task<Api.SubdivideCloudByPointSpacingResult> SubdivideCloudByPointSpacing(Api.SubdivideCloudByPointSpacingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SubdivideCloudByPointSpacingOperation.Descriptor,
            SubdivideCloudByPointSpacingOperation.CreateCommand, SubdivideCloudByPointSpacingOperation.OutputContracts, SubdivideCloudByPointSpacingOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.delete_cloud_points_by_radial_distance_from_points")]
    public override Task<Api.DeleteCloudPointsByRadialDistanceFromPointsResult> DeleteCloudPointsByRadialDistanceFromPoints(Api.DeleteCloudPointsByRadialDistanceFromPointsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteCloudPointsByRadialDistanceFromPointsOperation.Descriptor,
            DeleteCloudPointsByRadialDistanceFromPointsOperation.CreateCommand, DeleteCloudPointsByRadialDistanceFromPointsOperation.OutputContracts, DeleteCloudPointsByRadialDistanceFromPointsOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.delete_cloud_points_by_xyz_range")]
    public override Task<Api.DeleteCloudPointsByXYZRangeResult> DeleteCloudPointsByXYZRange(Api.DeleteCloudPointsByXYZRangeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteCloudPointsByXYZRangeOperation.Descriptor,
            DeleteCloudPointsByXYZRangeOperation.CreateCommand, DeleteCloudPointsByXYZRangeOperation.OutputContracts, DeleteCloudPointsByXYZRangeOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.generate_general_mesh")]
    public override Task<Api.GenerateGeneralMeshResult> GenerateGeneralMesh(Api.GenerateGeneralMeshRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GenerateGeneralMeshOperation.Descriptor,
            GenerateGeneralMeshOperation.CreateCommand, GenerateGeneralMeshOperation.OutputContracts, GenerateGeneralMeshOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.consolidate_mesh")]
    public override Task<Api.ConsolidateMeshResult> ConsolidateMesh(Api.ConsolidateMeshRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ConsolidateMeshOperation.Descriptor,
            ConsolidateMeshOperation.CreateCommand, ConsolidateMeshOperation.OutputContracts, ConsolidateMeshOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.mesh_volume")]
    public override Task<Api.MeshVolumeResult> MeshVolume(Api.MeshVolumeRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeshVolumeOperation.Descriptor,
            MeshVolumeOperation.CreateCommand, MeshVolumeOperation.OutputContracts, MeshVolumeOperation.CreateResult);

    [OperationImplementation("cloud_and_mesh_operations.mesh_fill_holes")]
    public override Task<Api.MeshFillHolesResult> MeshFillHoles(Api.MeshFillHolesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MeshFillHolesOperation.Descriptor,
            MeshFillHolesOperation.CreateCommand, MeshFillHolesOperation.OutputContracts, MeshFillHolesOperation.CreateResult);

}
