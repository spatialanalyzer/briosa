using Briosa.Server.Operations.WaveA;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal sealed class ConstructionOperationsService(OperationExecutor operationExecutor) :
    Api.ConstructionOperations.ConstructionOperationsBase
{
    private readonly OperationExecutor _operationExecutor =
        operationExecutor ?? throw new ArgumentNullException(nameof(operationExecutor));

    [OperationImplementation(GetActiveCollectionNameOperation.OperationId)]
    public override Task<Api.GetActiveCollectionNameResult> GetActiveCollectionName(
        Api.GetActiveCollectionNameRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetActiveCollectionNameOperation.Descriptor,
            GetActiveCollectionNameOperation.CreateCommand,
            GetActiveCollectionNameOperation.OutputContracts,
            GetActiveCollectionNameOperation.CreateResult);

    [OperationImplementation("construction_operations.add_collection_instruments_to_ref_list_wildcard_selection")]
    public override Task<Api.AddCollectionInstrumentsToRefListWildcardSelectionResult> AddCollectionInstrumentsToRefListWildcardSelection(Api.AddCollectionInstrumentsToRefListWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.AddCollectionInstrumentsToRefListWildcardSelectionRequest, Api.AddCollectionInstrumentsToRefListWildcardSelectionResult>(request, context, "construction_operations.add_collection_instruments_to_ref_list_wildcard_selection");

    [OperationImplementation("construction_operations.add_surface_to_mesh_offset_along_reference_direction")]
    public override Task<Api.AddSurfaceToMeshOffsetAlongReferenceDirectionResult> AddSurfaceToMeshOffsetAlongReferenceDirection(Api.AddSurfaceToMeshOffsetAlongReferenceDirectionRequest request, ServerCallContext context) =>
        Execute<Api.AddSurfaceToMeshOffsetAlongReferenceDirectionRequest, Api.AddSurfaceToMeshOffsetAlongReferenceDirectionResult>(request, context, "construction_operations.add_surface_to_mesh_offset_along_reference_direction");

    [OperationImplementation("construction_operations.auto_arrange_callout_view")]
    public override Task<Api.AutoArrangeCalloutViewResult> AutoArrangeCalloutView(Api.AutoArrangeCalloutViewRequest request, ServerCallContext context) =>
        Execute<Api.AutoArrangeCalloutViewRequest, Api.AutoArrangeCalloutViewResult>(request, context, "construction_operations.auto_arrange_callout_view");

    [OperationImplementation("construction_operations.average_set_of_groups")]
    public override Task<Api.AverageSetOfGroupsResult> AverageSetOfGroups(Api.AverageSetOfGroupsRequest request, ServerCallContext context) =>
        Execute<Api.AverageSetOfGroupsRequest, Api.AverageSetOfGroupsResult>(request, context, "construction_operations.average_set_of_groups");

    [OperationImplementation("construction_operations.clear_hidden_point_bar_database")]
    public override Task<Api.ClearHiddenPointBarDatabaseResult> ClearHiddenPointBarDatabase(Api.ClearHiddenPointBarDatabaseRequest request, ServerCallContext context) =>
        Execute<Api.ClearHiddenPointBarDatabaseRequest, Api.ClearHiddenPointBarDatabaseResult>(request, context, "construction_operations.clear_hidden_point_bar_database");

    [OperationImplementation("construction_operations.construct_boundary_points_from_cloud")]
    public override Task<Api.ConstructBoundaryPointsFromCloudResult> ConstructBoundaryPointsFromCloud(Api.ConstructBoundaryPointsFromCloudRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBoundaryPointsFromCloudRequest, Api.ConstructBoundaryPointsFromCloudResult>(request, context, "construction_operations.construct_boundary_points_from_cloud");

    [OperationImplementation("construction_operations.construct_b_spline_from_intersection_of_plane_and_surface")]
    public override Task<Api.ConstructBSplineFromIntersectionOfPlaneAndSurfaceResult> ConstructBSplineFromIntersectionOfPlaneAndSurface(Api.ConstructBSplineFromIntersectionOfPlaneAndSurfaceRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplineFromIntersectionOfPlaneAndSurfaceRequest, Api.ConstructBSplineFromIntersectionOfPlaneAndSurfaceResult>(request, context, "construction_operations.construct_b_spline_from_intersection_of_plane_and_surface");

    [OperationImplementation("construction_operations.construct_b_spline_from_intersection_of_surfaces")]
    public override Task<Api.ConstructBSplineFromIntersectionOfSurfacesResult> ConstructBSplineFromIntersectionOfSurfaces(Api.ConstructBSplineFromIntersectionOfSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplineFromIntersectionOfSurfacesRequest, Api.ConstructBSplineFromIntersectionOfSurfacesResult>(request, context, "construction_operations.construct_b_spline_from_intersection_of_surfaces");

    [OperationImplementation("construction_operations.construct_b_spline_from_points")]
    public override Task<Api.ConstructBSplineFromPointsResult> ConstructBSplineFromPoints(Api.ConstructBSplineFromPointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplineFromPointsRequest, Api.ConstructBSplineFromPointsResult>(request, context, "construction_operations.construct_b_spline_from_points");

    [OperationImplementation("construction_operations.construct_b_spline_from_point_set")]
    public override Task<Api.ConstructBSplineFromPointSetResult> ConstructBSplineFromPointSet(Api.ConstructBSplineFromPointSetRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplineFromPointSetRequest, Api.ConstructBSplineFromPointSetResult>(request, context, "construction_operations.construct_b_spline_from_point_set");

    [OperationImplementation("construction_operations.construct_b_spline_from_several_b_splines")]
    public override Task<Api.ConstructBSplineFromSeveralBSplinesResult> ConstructBSplineFromSeveralBSplines(Api.ConstructBSplineFromSeveralBSplinesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplineFromSeveralBSplinesRequest, Api.ConstructBSplineFromSeveralBSplinesResult>(request, context, "construction_operations.construct_b_spline_from_several_b_splines");

    [OperationImplementation("construction_operations.construct_b_splines_from_intersection_of_plane_and_mesh")]
    public override Task<Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshResult> ConstructBSplinesFromIntersectionOfPlaneAndMesh(Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshRequest, Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshResult>(request, context, "construction_operations.construct_b_splines_from_intersection_of_plane_and_mesh");

    [OperationImplementation("construction_operations.construct_b_splines_from_lines")]
    public override Task<Api.ConstructBSplinesFromLinesResult> ConstructBSplinesFromLines(Api.ConstructBSplinesFromLinesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplinesFromLinesRequest, Api.ConstructBSplinesFromLinesResult>(request, context, "construction_operations.construct_b_splines_from_lines");

    [OperationImplementation("construction_operations.construct_b_splines_from_surfaces")]
    public override Task<Api.ConstructBSplinesFromSurfacesResult> ConstructBSplinesFromSurfaces(Api.ConstructBSplinesFromSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructBSplinesFromSurfacesRequest, Api.ConstructBSplinesFromSurfacesResult>(request, context, "construction_operations.construct_b_splines_from_surfaces");

    [OperationImplementation("construction_operations.construct_circle")]
    public override Task<Api.ConstructCircleResult> ConstructCircle(Api.ConstructCircleRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCircleRequest, Api.ConstructCircleResult>(request, context, "construction_operations.construct_circle");

    [OperationImplementation("construction_operations.construct_circles_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructCirclesFromSurfaceFacesRuntimeSelectResult> ConstructCirclesFromSurfaceFacesRuntimeSelect(Api.ConstructCirclesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCirclesFromSurfaceFacesRuntimeSelectRequest, Api.ConstructCirclesFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_circles_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_circles_lines_from_surfaces")]
    public override Task<Api.ConstructCirclesLinesFromSurfacesResult> ConstructCirclesLinesFromSurfaces(Api.ConstructCirclesLinesFromSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCirclesLinesFromSurfacesRequest, Api.ConstructCirclesLinesFromSurfacesResult>(request, context, "construction_operations.construct_circles_lines_from_surfaces");

    [OperationImplementation("construction_operations.construct_collection")]
    public override Task<Api.ConstructCollectionResult> ConstructCollection(Api.ConstructCollectionRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCollectionRequest, Api.ConstructCollectionResult>(request, context, "construction_operations.construct_collection");

    [OperationImplementation("construction_operations.construct_cone")]
    public override Task<Api.ConstructConeResult> ConstructCone(Api.ConstructConeRequest request, ServerCallContext context) =>
        Execute<Api.ConstructConeRequest, Api.ConstructConeResult>(request, context, "construction_operations.construct_cone");

    [OperationImplementation("construction_operations.construct_cones_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructConesFromSurfaceFacesRuntimeSelectResult> ConstructConesFromSurfaceFacesRuntimeSelect(Api.ConstructConesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructConesFromSurfaceFacesRuntimeSelectRequest, Api.ConstructConesFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_cones_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_cross_section_cloud")]
    public override Task<Api.ConstructCrossSectionCloudResult> ConstructCrossSectionCloud(Api.ConstructCrossSectionCloudRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCrossSectionCloudRequest, Api.ConstructCrossSectionCloudResult>(request, context, "construction_operations.construct_cross_section_cloud");

    [OperationImplementation("construction_operations.construct_cross_section_cloud_user_select")]
    public override Task<Api.ConstructCrossSectionCloudUserSelectResult> ConstructCrossSectionCloudUserSelect(Api.ConstructCrossSectionCloudUserSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCrossSectionCloudUserSelectRequest, Api.ConstructCrossSectionCloudUserSelectResult>(request, context, "construction_operations.construct_cross_section_cloud_user_select");

    [OperationImplementation("construction_operations.construct_cylinder")]
    public override Task<Api.ConstructCylinderResult> ConstructCylinder(Api.ConstructCylinderRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCylinderRequest, Api.ConstructCylinderResult>(request, context, "construction_operations.construct_cylinder");

    [OperationImplementation("construction_operations.construct_cylinder_from_end_points")]
    public override Task<Api.ConstructCylinderFromEndPointsResult> ConstructCylinderFromEndPoints(Api.ConstructCylinderFromEndPointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCylinderFromEndPointsRequest, Api.ConstructCylinderFromEndPointsResult>(request, context, "construction_operations.construct_cylinder_from_end_points");

    [OperationImplementation("construction_operations.construct_cylinders_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructCylindersFromSurfaceFacesRuntimeSelectResult> ConstructCylindersFromSurfaceFacesRuntimeSelect(Api.ConstructCylindersFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructCylindersFromSurfaceFacesRuntimeSelectRequest, Api.ConstructCylindersFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_cylinders_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_ellipsoid")]
    public override Task<Api.ConstructEllipsoidResult> ConstructEllipsoid(Api.ConstructEllipsoidRequest request, ServerCallContext context) =>
        Execute<Api.ConstructEllipsoidRequest, Api.ConstructEllipsoidResult>(request, context, "construction_operations.construct_ellipsoid");

    [OperationImplementation("construction_operations.construct_folders")]
    public override Task<Api.ConstructFoldersResult> ConstructFolders(Api.ConstructFoldersRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFoldersRequest, Api.ConstructFoldersResult>(request, context, "construction_operations.construct_folders");

    [OperationImplementation("construction_operations.construct_frame")]
    public override Task<Api.ConstructFrameResult> ConstructFrame(Api.ConstructFrameRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameRequest, Api.ConstructFrameResult>(request, context, "construction_operations.construct_frame");

    [OperationImplementation("construction_operations.construct_frame_at_point_with_working_z_and_clocked_axis")]
    public override Task<Api.ConstructFrameAtPointWithWorkingZAndClockedAxisResult> ConstructFrameAtPointWithWorkingZAndClockedAxis(Api.ConstructFrameAtPointWithWorkingZAndClockedAxisRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameAtPointWithWorkingZAndClockedAxisRequest, Api.ConstructFrameAtPointWithWorkingZAndClockedAxisResult>(request, context, "construction_operations.construct_frame_at_point_with_working_z_and_clocked_axis");

    [OperationImplementation("construction_operations.construct_frame_at_robot_link")]
    public override Task<Api.ConstructFrameAtRobotLinkResult> ConstructFrameAtRobotLink(Api.ConstructFrameAtRobotLinkRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameAtRobotLinkRequest, Api.ConstructFrameAtRobotLinkResult>(request, context, "construction_operations.construct_frame_at_robot_link");

    [OperationImplementation("construction_operations.construct_frame_average_of_other_object_frames")]
    public override Task<Api.ConstructFrameAverageOfOtherObjectFramesResult> ConstructFrameAverageOfOtherObjectFrames(Api.ConstructFrameAverageOfOtherObjectFramesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameAverageOfOtherObjectFramesRequest, Api.ConstructFrameAverageOfOtherObjectFramesResult>(request, context, "construction_operations.construct_frame_average_of_other_object_frames");

    [OperationImplementation("construction_operations.construct_frame_copy_and_make_left_handed")]
    public override Task<Api.ConstructFrameCopyAndMakeLeftHandedResult> ConstructFrameCopyAndMakeLeftHanded(Api.ConstructFrameCopyAndMakeLeftHandedRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameCopyAndMakeLeftHandedRequest, Api.ConstructFrameCopyAndMakeLeftHandedResult>(request, context, "construction_operations.construct_frame_copy_and_make_left_handed");

    [OperationImplementation("construction_operations.construct_frame_from_point_measurement_probing_frames")]
    public override Task<Api.ConstructFrameFromPointMeasurementProbingFramesResult> ConstructFrameFromPointMeasurementProbingFrames(Api.ConstructFrameFromPointMeasurementProbingFramesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameFromPointMeasurementProbingFramesRequest, Api.ConstructFrameFromPointMeasurementProbingFramesResult>(request, context, "construction_operations.construct_frame_from_point_measurement_probing_frames");

    [OperationImplementation("construction_operations.construct_frame_from_transform_in_world")]
    public override Task<Api.ConstructFrameFromTransformInWorldResult> ConstructFrameFromTransformInWorld(Api.ConstructFrameFromTransformInWorldRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameFromTransformInWorldRequest, Api.ConstructFrameFromTransformInWorldResult>(request, context, "construction_operations.construct_frame_from_transform_in_world");

    [OperationImplementation("construction_operations.construct_frame_known_origin_object_direction_object_direction")]
    public override Task<Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionResult> ConstructFrameKnownOriginObjectDirectionObjectDirection(Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionRequest, Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionResult>(request, context, "construction_operations.construct_frame_known_origin_object_direction_object_direction");

    [OperationImplementation("construction_operations.construct_frame_on_instrument_base")]
    public override Task<Api.ConstructFrameOnInstrumentBaseResult> ConstructFrameOnInstrumentBase(Api.ConstructFrameOnInstrumentBaseRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameOnInstrumentBaseRequest, Api.ConstructFrameOnInstrumentBaseResult>(request, context, "construction_operations.construct_frame_on_instrument_base");

    [OperationImplementation("construction_operations.construct_frame_on_object")]
    public override Task<Api.ConstructFrameOnObjectResult> ConstructFrameOnObject(Api.ConstructFrameOnObjectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameOnObjectRequest, Api.ConstructFrameOnObjectResult>(request, context, "construction_operations.construct_frame_on_object");

    [OperationImplementation("construction_operations.construct_frame_pick_origin_and_point_on_x_axis_clock_z_along_working_z")]
    public override Task<Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZResult> ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZ(Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZRequest, Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZResult>(request, context, "construction_operations.construct_frame_pick_origin_and_point_on_x_axis_clock_z_along_working_z");

    [OperationImplementation("construction_operations.construct_frames_by_projecting_frames_on_mesh_along_frame_direction")]
    public override Task<Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionResult> ConstructFramesByProjectingFramesOnMeshAlongFrameDirection(Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionRequest, Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionResult>(request, context, "construction_operations.construct_frames_by_projecting_frames_on_mesh_along_frame_direction");

    [OperationImplementation("construction_operations.construct_frames_by_projecting_frames_on_mesh_along_reference_direction")]
    public override Task<Api.ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionResult> ConstructFramesByProjectingFramesOnMeshAlongReferenceDirection(Api.ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionRequest, Api.ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionResult>(request, context, "construction_operations.construct_frames_by_projecting_frames_on_mesh_along_reference_direction");

    [OperationImplementation("construction_operations.construct_frame_three_planes")]
    public override Task<Api.ConstructFrameThreePlanesResult> ConstructFrameThreePlanes(Api.ConstructFrameThreePlanesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameThreePlanesRequest, Api.ConstructFrameThreePlanesResult>(request, context, "construction_operations.construct_frame_three_planes");

    [OperationImplementation("construction_operations.construct_frame_three_points")]
    public override Task<Api.ConstructFrameThreePointsResult> ConstructFrameThreePoints(Api.ConstructFrameThreePointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameThreePointsRequest, Api.ConstructFrameThreePointsResult>(request, context, "construction_operations.construct_frame_three_points");

    [OperationImplementation("construction_operations.construct_frame_with_wizard")]
    public override Task<Api.ConstructFrameWithWizardResult> ConstructFrameWithWizard(Api.ConstructFrameWithWizardRequest request, ServerCallContext context) =>
        Execute<Api.ConstructFrameWithWizardRequest, Api.ConstructFrameWithWizardResult>(request, context, "construction_operations.construct_frame_with_wizard");

    [OperationImplementation("construction_operations.construct_geometry_from_surfaces")]
    public override Task<Api.ConstructGeometryFromSurfacesResult> ConstructGeometryFromSurfaces(Api.ConstructGeometryFromSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructGeometryFromSurfacesRequest, Api.ConstructGeometryFromSurfacesResult>(request, context, "construction_operations.construct_geometry_from_surfaces");

    [OperationImplementation("construction_operations.construct_line_center_of_slot")]
    public override Task<Api.ConstructLineCenterOfSlotResult> ConstructLineCenterOfSlot(Api.ConstructLineCenterOfSlotRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineCenterOfSlotRequest, Api.ConstructLineCenterOfSlotResult>(request, context, "construction_operations.construct_line_center_of_slot");

    [OperationImplementation("construction_operations.construct_line_from_instrument_shot")]
    public override Task<Api.ConstructLineFromInstrumentShotResult> ConstructLineFromInstrumentShot(Api.ConstructLineFromInstrumentShotRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineFromInstrumentShotRequest, Api.ConstructLineFromInstrumentShotResult>(request, context, "construction_operations.construct_line_from_instrument_shot");

    [OperationImplementation("construction_operations.construct_line_normal_to_object")]
    public override Task<Api.ConstructLineNormalToObjectResult> ConstructLineNormalToObject(Api.ConstructLineNormalToObjectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineNormalToObjectRequest, Api.ConstructLineNormalToObjectResult>(request, context, "construction_operations.construct_line_normal_to_object");

    [OperationImplementation("construction_operations.construct_line_normal_to_object_through_point")]
    public override Task<Api.ConstructLineNormalToObjectThroughPointResult> ConstructLineNormalToObjectThroughPoint(Api.ConstructLineNormalToObjectThroughPointRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineNormalToObjectThroughPointRequest, Api.ConstructLineNormalToObjectThroughPointResult>(request, context, "construction_operations.construct_line_normal_to_object_through_point");

    [OperationImplementation("construction_operations.construct_line_project_line_to_object_reference_plane")]
    public override Task<Api.ConstructLineProjectLineToObjectReferencePlaneResult> ConstructLineProjectLineToObjectReferencePlane(Api.ConstructLineProjectLineToObjectReferencePlaneRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineProjectLineToObjectReferencePlaneRequest, Api.ConstructLineProjectLineToObjectReferencePlaneResult>(request, context, "construction_operations.construct_line_project_line_to_object_reference_plane");

    [OperationImplementation("construction_operations.construct_lines_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructLinesFromSurfaceFacesRuntimeSelectResult> ConstructLinesFromSurfaceFacesRuntimeSelect(Api.ConstructLinesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLinesFromSurfaceFacesRuntimeSelectRequest, Api.ConstructLinesFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_lines_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_line_two_plane_intersection")]
    public override Task<Api.ConstructLineTwoPlaneIntersectionResult> ConstructLineTwoPlaneIntersection(Api.ConstructLineTwoPlaneIntersectionRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineTwoPlaneIntersectionRequest, Api.ConstructLineTwoPlaneIntersectionResult>(request, context, "construction_operations.construct_line_two_plane_intersection");

    [OperationImplementation("construction_operations.construct_line_two_points")]
    public override Task<Api.ConstructLineTwoPointsResult> ConstructLineTwoPoints(Api.ConstructLineTwoPointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineTwoPointsRequest, Api.ConstructLineTwoPointsResult>(request, context, "construction_operations.construct_line_two_points");

    [OperationImplementation("construction_operations.construct_line_two_points_vector_notation")]
    public override Task<Api.ConstructLineTwoPointsVectorNotationResult> ConstructLineTwoPointsVectorNotation(Api.ConstructLineTwoPointsVectorNotationRequest request, ServerCallContext context) =>
        Execute<Api.ConstructLineTwoPointsVectorNotationRequest, Api.ConstructLineTwoPointsVectorNotationResult>(request, context, "construction_operations.construct_line_two_points_vector_notation");

    [OperationImplementation("construction_operations.construct_mirror_cube_frame")]
    public override Task<Api.ConstructMirrorCubeFrameResult> ConstructMirrorCubeFrame(Api.ConstructMirrorCubeFrameRequest request, ServerCallContext context) =>
        Execute<Api.ConstructMirrorCubeFrameRequest, Api.ConstructMirrorCubeFrameResult>(request, context, "construction_operations.construct_mirror_cube_frame");

    [OperationImplementation("construction_operations.construct_objects_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructObjectsFromSurfaceFacesRuntimeSelectResult> ConstructObjectsFromSurfaceFacesRuntimeSelect(Api.ConstructObjectsFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructObjectsFromSurfaceFacesRuntimeSelectRequest, Api.ConstructObjectsFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_objects_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_perimeter_from_points")]
    public override Task<Api.ConstructPerimeterFromPointsResult> ConstructPerimeterFromPoints(Api.ConstructPerimeterFromPointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPerimeterFromPointsRequest, Api.ConstructPerimeterFromPointsResult>(request, context, "construction_operations.construct_perimeter_from_points");

    [OperationImplementation("construction_operations.construct_plane")]
    public override Task<Api.ConstructPlaneResult> ConstructPlane(Api.ConstructPlaneRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPlaneRequest, Api.ConstructPlaneResult>(request, context, "construction_operations.construct_plane");

    [OperationImplementation("construction_operations.construct_plane_normal_to_object_through_point")]
    public override Task<Api.ConstructPlaneNormalToObjectThroughPointResult> ConstructPlaneNormalToObjectThroughPoint(Api.ConstructPlaneNormalToObjectThroughPointRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPlaneNormalToObjectThroughPointRequest, Api.ConstructPlaneNormalToObjectThroughPointResult>(request, context, "construction_operations.construct_plane_normal_to_object_through_point");

    [OperationImplementation("construction_operations.construct_planes_bisect_two_planes")]
    public override Task<Api.ConstructPlanesBisectTwoPlanesResult> ConstructPlanesBisectTwoPlanes(Api.ConstructPlanesBisectTwoPlanesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPlanesBisectTwoPlanesRequest, Api.ConstructPlanesBisectTwoPlanesResult>(request, context, "construction_operations.construct_planes_bisect_two_planes");

    [OperationImplementation("construction_operations.construct_planes_bounding_point_group")]
    public override Task<Api.ConstructPlanesBoundingPointGroupResult> ConstructPlanesBoundingPointGroup(Api.ConstructPlanesBoundingPointGroupRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPlanesBoundingPointGroupRequest, Api.ConstructPlanesBoundingPointGroupResult>(request, context, "construction_operations.construct_planes_bounding_point_group");

    [OperationImplementation("construction_operations.construct_planes_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructPlanesFromSurfaceFacesRuntimeSelectResult> ConstructPlanesFromSurfaceFacesRuntimeSelect(Api.ConstructPlanesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPlanesFromSurfaceFacesRuntimeSelectRequest, Api.ConstructPlanesFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_planes_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_point_at_circle_center")]
    public override Task<Api.ConstructPointAtCircleCenterResult> ConstructPointAtCircleCenter(Api.ConstructPointAtCircleCenterRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtCircleCenterRequest, Api.ConstructPointAtCircleCenterResult>(request, context, "construction_operations.construct_point_at_circle_center");

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_b_spline_and_surfaces")]
    public override Task<Api.ConstructPointAtIntersectionOfBSplineAndSurfacesResult> ConstructPointAtIntersectionOfBSplineAndSurfaces(Api.ConstructPointAtIntersectionOfBSplineAndSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtIntersectionOfBSplineAndSurfacesRequest, Api.ConstructPointAtIntersectionOfBSplineAndSurfacesResult>(request, context, "construction_operations.construct_point_at_intersection_of_b_spline_and_surfaces");

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_plane_and_line")]
    public override Task<Api.ConstructPointAtIntersectionOfPlaneAndLineResult> ConstructPointAtIntersectionOfPlaneAndLine(Api.ConstructPointAtIntersectionOfPlaneAndLineRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtIntersectionOfPlaneAndLineRequest, Api.ConstructPointAtIntersectionOfPlaneAndLineResult>(request, context, "construction_operations.construct_point_at_intersection_of_plane_and_line");

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_planes")]
    public override Task<Api.ConstructPointAtIntersectionOfPlanesResult> ConstructPointAtIntersectionOfPlanes(Api.ConstructPointAtIntersectionOfPlanesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtIntersectionOfPlanesRequest, Api.ConstructPointAtIntersectionOfPlanesResult>(request, context, "construction_operations.construct_point_at_intersection_of_planes");

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_two_b_splines")]
    public override Task<Api.ConstructPointAtIntersectionOfTwoBSplinesResult> ConstructPointAtIntersectionOfTwoBSplines(Api.ConstructPointAtIntersectionOfTwoBSplinesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtIntersectionOfTwoBSplinesRequest, Api.ConstructPointAtIntersectionOfTwoBSplinesResult>(request, context, "construction_operations.construct_point_at_intersection_of_two_b_splines");

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_two_lines")]
    public override Task<Api.ConstructPointAtIntersectionOfTwoLinesResult> ConstructPointAtIntersectionOfTwoLines(Api.ConstructPointAtIntersectionOfTwoLinesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtIntersectionOfTwoLinesRequest, Api.ConstructPointAtIntersectionOfTwoLinesResult>(request, context, "construction_operations.construct_point_at_intersection_of_two_lines");

    [OperationImplementation("construction_operations.construct_point_at_line_midpoint")]
    public override Task<Api.ConstructPointAtLineMidpointResult> ConstructPointAtLineMidpoint(Api.ConstructPointAtLineMidpointRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtLineMidpointRequest, Api.ConstructPointAtLineMidpointResult>(request, context, "construction_operations.construct_point_at_line_midpoint");

    [OperationImplementation("construction_operations.construct_point_at_object_origin")]
    public override Task<Api.ConstructPointAtObjectOriginResult> ConstructPointAtObjectOrigin(Api.ConstructPointAtObjectOriginRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtObjectOriginRequest, Api.ConstructPointAtObjectOriginResult>(request, context, "construction_operations.construct_point_at_object_origin");

    [OperationImplementation("construction_operations.construct_point_at_projection_of_point_onto_object")]
    public override Task<Api.ConstructPointAtProjectionOfPointOntoObjectResult> ConstructPointAtProjectionOfPointOntoObject(Api.ConstructPointAtProjectionOfPointOntoObjectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointAtProjectionOfPointOntoObjectRequest, Api.ConstructPointAtProjectionOfPointOntoObjectResult>(request, context, "construction_operations.construct_point_at_projection_of_point_onto_object");

    [OperationImplementation("construction_operations.construct_point_cloud_from_existing_clouds")]
    public override Task<Api.ConstructPointCloudFromExistingCloudsResult> ConstructPointCloudFromExistingClouds(Api.ConstructPointCloudFromExistingCloudsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointCloudFromExistingCloudsRequest, Api.ConstructPointCloudFromExistingCloudsResult>(request, context, "construction_operations.construct_point_cloud_from_existing_clouds");

    [OperationImplementation("construction_operations.construct_point_cloud_from_visible_cloud_points")]
    public override Task<Api.ConstructPointCloudFromVisibleCloudPointsResult> ConstructPointCloudFromVisibleCloudPoints(Api.ConstructPointCloudFromVisibleCloudPointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointCloudFromVisibleCloudPointsRequest, Api.ConstructPointCloudFromVisibleCloudPointsResult>(request, context, "construction_operations.construct_point_cloud_from_visible_cloud_points");

    [OperationImplementation("construction_operations.construct_point_cloud_limiting_probing_directions")]
    public override Task<Api.ConstructPointCloudLimitingProbingDirectionsResult> ConstructPointCloudLimitingProbingDirections(Api.ConstructPointCloudLimitingProbingDirectionsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointCloudLimitingProbingDirectionsRequest, Api.ConstructPointCloudLimitingProbingDirectionsResult>(request, context, "construction_operations.construct_point_cloud_limiting_probing_directions");

    [OperationImplementation("construction_operations.construct_point_clouds_from_existing_cloud_points_runtime_select")]
    public override Task<Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectResult> ConstructPointCloudsFromExistingCloudPointsRuntimeSelect(Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectRequest, Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectResult>(request, context, "construction_operations.construct_point_clouds_from_existing_cloud_points_runtime_select");

    [OperationImplementation("construction_operations.construct_point_clouds_from_existing_clouds_uniform_spacing")]
    public override Task<Api.ConstructPointCloudsFromExistingCloudsUniformSpacingResult> ConstructPointCloudsFromExistingCloudsUniformSpacing(Api.ConstructPointCloudsFromExistingCloudsUniformSpacingRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointCloudsFromExistingCloudsUniformSpacingRequest, Api.ConstructPointCloudsFromExistingCloudsUniformSpacingResult>(request, context, "construction_operations.construct_point_clouds_from_existing_clouds_uniform_spacing");

    [OperationImplementation("construction_operations.construct_point_clouds_from_existing_point_group")]
    public override Task<Api.ConstructPointCloudsFromExistingPointGroupResult> ConstructPointCloudsFromExistingPointGroup(Api.ConstructPointCloudsFromExistingPointGroupRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointCloudsFromExistingPointGroupRequest, Api.ConstructPointCloudsFromExistingPointGroupResult>(request, context, "construction_operations.construct_point_clouds_from_existing_point_group");

    [OperationImplementation("construction_operations.construct_point_fit_to_points")]
    public override Task<Api.ConstructPointFitToPointsResult> ConstructPointFitToPoints(Api.ConstructPointFitToPointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointFitToPointsRequest, Api.ConstructPointFitToPointsResult>(request, context, "construction_operations.construct_point_fit_to_points");

    [OperationImplementation("construction_operations.construct_point_from_cloud_point_runtime_select")]
    public override Task<Api.ConstructPointFromCloudPointRuntimeSelectResult> ConstructPointFromCloudPointRuntimeSelect(Api.ConstructPointFromCloudPointRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointFromCloudPointRuntimeSelectRequest, Api.ConstructPointFromCloudPointRuntimeSelectResult>(request, context, "construction_operations.construct_point_from_cloud_point_runtime_select");

    [OperationImplementation("construction_operations.construct_point_from_survey_target_center")]
    public override Task<Api.ConstructPointFromSurveyTargetCenterResult> ConstructPointFromSurveyTargetCenter(Api.ConstructPointFromSurveyTargetCenterRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointFromSurveyTargetCenterRequest, Api.ConstructPointFromSurveyTargetCenterResult>(request, context, "construction_operations.construct_point_from_survey_target_center");

    [OperationImplementation("construction_operations.construct_point_group_from_point_cloud")]
    public override Task<Api.ConstructPointGroupFromPointCloudResult> ConstructPointGroupFromPointCloud(Api.ConstructPointGroupFromPointCloudRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointGroupFromPointCloudRequest, Api.ConstructPointGroupFromPointCloudResult>(request, context, "construction_operations.construct_point_group_from_point_cloud");

    [OperationImplementation("construction_operations.construct_point_group_from_point_name_ref_list")]
    public override Task<Api.ConstructPointGroupFromPointNameRefListResult> ConstructPointGroupFromPointNameRefList(Api.ConstructPointGroupFromPointNameRefListRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointGroupFromPointNameRefListRequest, Api.ConstructPointGroupFromPointNameRefListResult>(request, context, "construction_operations.construct_point_group_from_point_name_ref_list");

    [OperationImplementation("construction_operations.construct_point_groups_from_vector_groups")]
    public override Task<Api.ConstructPointGroupsFromVectorGroupsResult> ConstructPointGroupsFromVectorGroups(Api.ConstructPointGroupsFromVectorGroupsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointGroupsFromVectorGroupsRequest, Api.ConstructPointGroupsFromVectorGroupsResult>(request, context, "construction_operations.construct_point_groups_from_vector_groups");

    [OperationImplementation("construction_operations.construct_point_in_working_coordinates")]
    public override Task<Api.ConstructPointInWorkingCoordinatesResult> ConstructPointInWorkingCoordinates(Api.ConstructPointInWorkingCoordinatesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointInWorkingCoordinatesRequest, Api.ConstructPointInWorkingCoordinatesResult>(request, context, "construction_operations.construct_point_in_working_coordinates");

    [OperationImplementation("construction_operations.construct_points_at_intersection_of_circle_and_line")]
    public override Task<Api.ConstructPointsAtIntersectionOfCircleAndLineResult> ConstructPointsAtIntersectionOfCircleAndLine(Api.ConstructPointsAtIntersectionOfCircleAndLineRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsAtIntersectionOfCircleAndLineRequest, Api.ConstructPointsAtIntersectionOfCircleAndLineResult>(request, context, "construction_operations.construct_points_at_intersection_of_circle_and_line");

    [OperationImplementation("construction_operations.construct_points_at_intersection_of_principal_object_axes_and_surfaces")]
    public override Task<Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesResult> ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfaces(Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesRequest, Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesResult>(request, context, "construction_operations.construct_points_at_intersection_of_principal_object_axes_and_surfaces");

    [OperationImplementation("construction_operations.construct_points_at_projection_on_surfaces_parallel_to_wcf_axis")]
    public override Task<Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisResult> ConstructPointsAtProjectionOnSurfacesParallelToWcfAxis(Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisRequest, Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisResult>(request, context, "construction_operations.construct_points_at_projection_on_surfaces_parallel_to_wcf_axis");

    [OperationImplementation("construction_operations.construct_points_at_projection_on_surfaces_radial_from_wcf_axis")]
    public override Task<Api.ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisResult> ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxis(Api.ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisRequest, Api.ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisResult>(request, context, "construction_operations.construct_points_at_projection_on_surfaces_radial_from_wcf_axis");

    [OperationImplementation("construction_operations.construct_points_at_projection_on_surfaces_spherical_from_wcf_origin")]
    public override Task<Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginResult> ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOrigin(Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginRequest, Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginResult>(request, context, "construction_operations.construct_points_at_projection_on_surfaces_spherical_from_wcf_origin");

    [OperationImplementation("construction_operations.construct_points_auto_correspond_two_groups_inter_point_distance")]
    public override Task<Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceResult> ConstructPointsAutoCorrespondTwoGroupsInterPointDistance(Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceRequest, Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceResult>(request, context, "construction_operations.construct_points_auto_correspond_two_groups_inter_point_distance");

    [OperationImplementation("construction_operations.construct_points_auto_correspond_two_groups_proximity")]
    public override Task<Api.ConstructPointsAutoCorrespondTwoGroupsProximityResult> ConstructPointsAutoCorrespondTwoGroupsProximity(Api.ConstructPointsAutoCorrespondTwoGroupsProximityRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsAutoCorrespondTwoGroupsProximityRequest, Api.ConstructPointsAutoCorrespondTwoGroupsProximityResult>(request, context, "construction_operations.construct_points_auto_correspond_two_groups_proximity");

    [OperationImplementation("construction_operations.construct_points_by_projecting_points_on_mesh_along_direction")]
    public override Task<Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionResult> ConstructPointsByProjectingPointsOnMeshAlongDirection(Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionRequest, Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionResult>(request, context, "construction_operations.construct_points_by_projecting_points_on_mesh_along_direction");

    [OperationImplementation("construction_operations.construct_points_cylindrically_shifted")]
    public override Task<Api.ConstructPointsCylindricallyShiftedResult> ConstructPointsCylindricallyShifted(Api.ConstructPointsCylindricallyShiftedRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsCylindricallyShiftedRequest, Api.ConstructPointsCylindricallyShiftedResult>(request, context, "construction_operations.construct_points_cylindrically_shifted");

    [OperationImplementation("construction_operations.construct_points_from_cylinder")]
    public override Task<Api.ConstructPointsFromCylinderResult> ConstructPointsFromCylinder(Api.ConstructPointsFromCylinderRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsFromCylinderRequest, Api.ConstructPointsFromCylinderResult>(request, context, "construction_operations.construct_points_from_cylinder");

    [OperationImplementation("construction_operations.construct_points_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructPointsFromSurfaceFacesRuntimeSelectResult> ConstructPointsFromSurfaceFacesRuntimeSelect(Api.ConstructPointsFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsFromSurfaceFacesRuntimeSelectRequest, Api.ConstructPointsFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_points_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_points_from_surfaces_on_uv_grid")]
    public override Task<Api.ConstructPointsFromSurfacesOnUvGridResult> ConstructPointsFromSurfacesOnUvGrid(Api.ConstructPointsFromSurfacesOnUvGridRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsFromSurfacesOnUvGridRequest, Api.ConstructPointsFromSurfacesOnUvGridResult>(request, context, "construction_operations.construct_points_from_surfaces_on_uv_grid");

    [OperationImplementation("construction_operations.construct_points_layout_on_grid")]
    public override Task<Api.ConstructPointsLayoutOnGridResult> ConstructPointsLayoutOnGrid(Api.ConstructPointsLayoutOnGridRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsLayoutOnGridRequest, Api.ConstructPointsLayoutOnGridResult>(request, context, "construction_operations.construct_points_layout_on_grid");

    [OperationImplementation("construction_operations.construct_points_n_spaced_on_curves")]
    public override Task<Api.ConstructPointsNSpacedOnCurvesResult> ConstructPointsNSpacedOnCurves(Api.ConstructPointsNSpacedOnCurvesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsNSpacedOnCurvesRequest, Api.ConstructPointsNSpacedOnCurvesResult>(request, context, "construction_operations.construct_points_n_spaced_on_curves");

    [OperationImplementation("construction_operations.construct_points_on_curves_using_max_chordal_deviation")]
    public override Task<Api.ConstructPointsOnCurvesUsingMaxChordalDeviationResult> ConstructPointsOnCurvesUsingMaxChordalDeviation(Api.ConstructPointsOnCurvesUsingMaxChordalDeviationRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsOnCurvesUsingMaxChordalDeviationRequest, Api.ConstructPointsOnCurvesUsingMaxChordalDeviationResult>(request, context, "construction_operations.construct_points_on_curves_using_max_chordal_deviation");

    [OperationImplementation("construction_operations.construct_points_on_object_vertices")]
    public override Task<Api.ConstructPointsOnObjectVerticesResult> ConstructPointsOnObjectVertices(Api.ConstructPointsOnObjectVerticesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsOnObjectVerticesRequest, Api.ConstructPointsOnObjectVerticesResult>(request, context, "construction_operations.construct_points_on_object_vertices");

    [OperationImplementation("construction_operations.construct_points_on_surfaces_by_clicking")]
    public override Task<Api.ConstructPointsOnSurfacesByClickingResult> ConstructPointsOnSurfacesByClicking(Api.ConstructPointsOnSurfacesByClickingRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsOnSurfacesByClickingRequest, Api.ConstructPointsOnSurfacesByClickingResult>(request, context, "construction_operations.construct_points_on_surfaces_by_clicking");

    [OperationImplementation("construction_operations.construct_points_shifted_in_working_frame")]
    public override Task<Api.ConstructPointsShiftedInWorkingFrameResult> ConstructPointsShiftedInWorkingFrame(Api.ConstructPointsShiftedInWorkingFrameRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsShiftedInWorkingFrameRequest, Api.ConstructPointsShiftedInWorkingFrameResult>(request, context, "construction_operations.construct_points_shifted_in_working_frame");

    [OperationImplementation("construction_operations.construct_points_spaced_at_distance_on_curves")]
    public override Task<Api.ConstructPointsSpacedAtDistanceOnCurvesResult> ConstructPointsSpacedAtDistanceOnCurves(Api.ConstructPointsSpacedAtDistanceOnCurvesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsSpacedAtDistanceOnCurvesRequest, Api.ConstructPointsSpacedAtDistanceOnCurvesResult>(request, context, "construction_operations.construct_points_spaced_at_distance_on_curves");

    [OperationImplementation("construction_operations.construct_points_subset_with_greatest_spacing")]
    public override Task<Api.ConstructPointsSubsetWithGreatestSpacingResult> ConstructPointsSubsetWithGreatestSpacing(Api.ConstructPointsSubsetWithGreatestSpacingRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsSubsetWithGreatestSpacingRequest, Api.ConstructPointsSubsetWithGreatestSpacingResult>(request, context, "construction_operations.construct_points_subset_with_greatest_spacing");

    [OperationImplementation("construction_operations.construct_points_wildcard_selection")]
    public override Task<Api.ConstructPointsWildcardSelectionResult> ConstructPointsWildcardSelection(Api.ConstructPointsWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPointsWildcardSelectionRequest, Api.ConstructPointsWildcardSelectionResult>(request, context, "construction_operations.construct_points_wildcard_selection");

    [OperationImplementation("construction_operations.construct_polygonized_surface_from_point_clouds")]
    public override Task<Api.ConstructPolygonizedSurfaceFromPointCloudsResult> ConstructPolygonizedSurfaceFromPointClouds(Api.ConstructPolygonizedSurfaceFromPointCloudsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructPolygonizedSurfaceFromPointCloudsRequest, Api.ConstructPolygonizedSurfaceFromPointCloudsResult>(request, context, "construction_operations.construct_polygonized_surface_from_point_clouds");

    [OperationImplementation("construction_operations.construct_scale_bar")]
    public override Task<Api.ConstructScaleBarResult> ConstructScaleBar(Api.ConstructScaleBarRequest request, ServerCallContext context) =>
        Execute<Api.ConstructScaleBarRequest, Api.ConstructScaleBarResult>(request, context, "construction_operations.construct_scale_bar");

    [OperationImplementation("construction_operations.construct_sphere")]
    public override Task<Api.ConstructSphereResult> ConstructSphere(Api.ConstructSphereRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSphereRequest, Api.ConstructSphereResult>(request, context, "construction_operations.construct_sphere");

    [OperationImplementation("construction_operations.construct_spheres_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructSpheresFromSurfaceFacesRuntimeSelectResult> ConstructSpheresFromSurfaceFacesRuntimeSelect(Api.ConstructSpheresFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSpheresFromSurfaceFacesRuntimeSelectRequest, Api.ConstructSpheresFromSurfaceFacesRuntimeSelectResult>(request, context, "construction_operations.construct_spheres_from_surface_faces_runtime_select");

    [OperationImplementation("construction_operations.construct_surface_by_dissecting_surfaces")]
    public override Task<Api.ConstructSurfaceByDissectingSurfacesResult> ConstructSurfaceByDissectingSurfaces(Api.ConstructSurfaceByDissectingSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceByDissectingSurfacesRequest, Api.ConstructSurfaceByDissectingSurfacesResult>(request, context, "construction_operations.construct_surface_by_dissecting_surfaces");

    [OperationImplementation("construction_operations.construct_surface_by_offsetting_surface")]
    public override Task<Api.ConstructSurfaceByOffsettingSurfaceResult> ConstructSurfaceByOffsettingSurface(Api.ConstructSurfaceByOffsettingSurfaceRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceByOffsettingSurfaceRequest, Api.ConstructSurfaceByOffsettingSurfaceResult>(request, context, "construction_operations.construct_surface_by_offsetting_surface");

    [OperationImplementation("construction_operations.construct_surface_fit_from_nominal_surfaces_and_actual_data")]
    public override Task<Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataResult> ConstructSurfaceFitFromNominalSurfacesAndActualData(Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataRequest, Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataResult>(request, context, "construction_operations.construct_surface_fit_from_nominal_surfaces_and_actual_data");

    [OperationImplementation("construction_operations.construct_surface_from_annotation_links")]
    public override Task<Api.ConstructSurfaceFromAnnotationLinksResult> ConstructSurfaceFromAnnotationLinks(Api.ConstructSurfaceFromAnnotationLinksRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromAnnotationLinksRequest, Api.ConstructSurfaceFromAnnotationLinksResult>(request, context, "construction_operations.construct_surface_from_annotation_links");

    [OperationImplementation("construction_operations.construct_surface_from_b_splines")]
    public override Task<Api.ConstructSurfaceFromBSplinesResult> ConstructSurfaceFromBSplines(Api.ConstructSurfaceFromBSplinesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromBSplinesRequest, Api.ConstructSurfaceFromBSplinesResult>(request, context, "construction_operations.construct_surface_from_b_splines");

    [OperationImplementation("construction_operations.construct_surface_from_collection_of_surfaces")]
    public override Task<Api.ConstructSurfaceFromCollectionOfSurfacesResult> ConstructSurfaceFromCollectionOfSurfaces(Api.ConstructSurfaceFromCollectionOfSurfacesRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromCollectionOfSurfacesRequest, Api.ConstructSurfaceFromCollectionOfSurfacesResult>(request, context, "construction_operations.construct_surface_from_collection_of_surfaces");

    [OperationImplementation("construction_operations.construct_surface_from_cone")]
    public override Task<Api.ConstructSurfaceFromConeResult> ConstructSurfaceFromCone(Api.ConstructSurfaceFromConeRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromConeRequest, Api.ConstructSurfaceFromConeResult>(request, context, "construction_operations.construct_surface_from_cone");

    [OperationImplementation("construction_operations.construct_surface_from_cylinder")]
    public override Task<Api.ConstructSurfaceFromCylinderResult> ConstructSurfaceFromCylinder(Api.ConstructSurfaceFromCylinderRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromCylinderRequest, Api.ConstructSurfaceFromCylinderResult>(request, context, "construction_operations.construct_surface_from_cylinder");

    [OperationImplementation("construction_operations.construct_surface_from_plane")]
    public override Task<Api.ConstructSurfaceFromPlaneResult> ConstructSurfaceFromPlane(Api.ConstructSurfaceFromPlaneRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromPlaneRequest, Api.ConstructSurfaceFromPlaneResult>(request, context, "construction_operations.construct_surface_from_plane");

    [OperationImplementation("construction_operations.construct_surface_from_point_groups")]
    public override Task<Api.ConstructSurfaceFromPointGroupsResult> ConstructSurfaceFromPointGroups(Api.ConstructSurfaceFromPointGroupsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromPointGroupsRequest, Api.ConstructSurfaceFromPointGroupsResult>(request, context, "construction_operations.construct_surface_from_point_groups");

    [OperationImplementation("construction_operations.construct_surface_from_sphere")]
    public override Task<Api.ConstructSurfaceFromSphereResult> ConstructSurfaceFromSphere(Api.ConstructSurfaceFromSphereRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfaceFromSphereRequest, Api.ConstructSurfaceFromSphereResult>(request, context, "construction_operations.construct_surface_from_sphere");

    [OperationImplementation("construction_operations.construct_surfaces_by_dissecting_surfaces_from_ref_list")]
    public override Task<Api.ConstructSurfacesByDissectingSurfacesFromRefListResult> ConstructSurfacesByDissectingSurfacesFromRefList(Api.ConstructSurfacesByDissectingSurfacesFromRefListRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfacesByDissectingSurfacesFromRefListRequest, Api.ConstructSurfacesByDissectingSurfacesFromRefListResult>(request, context, "construction_operations.construct_surfaces_by_dissecting_surfaces_from_ref_list");

    [OperationImplementation("construction_operations.construct_surfaces_by_projecting_points")]
    public override Task<Api.ConstructSurfacesByProjectingPointsResult> ConstructSurfacesByProjectingPoints(Api.ConstructSurfacesByProjectingPointsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfacesByProjectingPointsRequest, Api.ConstructSurfacesByProjectingPointsResult>(request, context, "construction_operations.construct_surfaces_by_projecting_points");

    [OperationImplementation("construction_operations.construct_surfaces_from_objects")]
    public override Task<Api.ConstructSurfacesFromObjectsResult> ConstructSurfacesFromObjects(Api.ConstructSurfacesFromObjectsRequest request, ServerCallContext context) =>
        Execute<Api.ConstructSurfacesFromObjectsRequest, Api.ConstructSurfacesFromObjectsResult>(request, context, "construction_operations.construct_surfaces_from_objects");

    [OperationImplementation("construction_operations.construct_vector_group_area_profile_check")]
    public override Task<Api.ConstructVectorGroupAreaProfileCheckResult> ConstructVectorGroupAreaProfileCheck(Api.ConstructVectorGroupAreaProfileCheckRequest request, ServerCallContext context) =>
        Execute<Api.ConstructVectorGroupAreaProfileCheckRequest, Api.ConstructVectorGroupAreaProfileCheckResult>(request, context, "construction_operations.construct_vector_group_area_profile_check");

    [OperationImplementation("construction_operations.construct_vector_group_from_relationship")]
    public override Task<Api.ConstructVectorGroupFromRelationshipResult> ConstructVectorGroupFromRelationship(Api.ConstructVectorGroupFromRelationshipRequest request, ServerCallContext context) =>
        Execute<Api.ConstructVectorGroupFromRelationshipRequest, Api.ConstructVectorGroupFromRelationshipResult>(request, context, "construction_operations.construct_vector_group_from_relationship");

    [OperationImplementation("construction_operations.construct_vector_group_from_vector_name_ref_list")]
    public override Task<Api.ConstructVectorGroupFromVectorNameRefListResult> ConstructVectorGroupFromVectorNameRefList(Api.ConstructVectorGroupFromVectorNameRefListRequest request, ServerCallContext context) =>
        Execute<Api.ConstructVectorGroupFromVectorNameRefListRequest, Api.ConstructVectorGroupFromVectorNameRefListResult>(request, context, "construction_operations.construct_vector_group_from_vector_name_ref_list");

    [OperationImplementation("construction_operations.construct_vector_group_group_to_group_compare")]
    public override Task<Api.ConstructVectorGroupGroupToGroupCompareResult> ConstructVectorGroupGroupToGroupCompare(Api.ConstructVectorGroupGroupToGroupCompareRequest request, ServerCallContext context) =>
        Execute<Api.ConstructVectorGroupGroupToGroupCompareRequest, Api.ConstructVectorGroupGroupToGroupCompareResult>(request, context, "construction_operations.construct_vector_group_group_to_group_compare");

    [OperationImplementation("construction_operations.construct_vector_in_working_coordinates_begin_delta")]
    public override Task<Api.ConstructVectorInWorkingCoordinatesBeginDeltaResult> ConstructVectorInWorkingCoordinatesBeginDelta(Api.ConstructVectorInWorkingCoordinatesBeginDeltaRequest request, ServerCallContext context) =>
        Execute<Api.ConstructVectorInWorkingCoordinatesBeginDeltaRequest, Api.ConstructVectorInWorkingCoordinatesBeginDeltaResult>(request, context, "construction_operations.construct_vector_in_working_coordinates_begin_delta");

    [OperationImplementation("construction_operations.construct_vector_in_working_coordinates_begin_direction_magnitude")]
    public override Task<Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeResult> ConstructVectorInWorkingCoordinatesBeginDirectionMagnitude(Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeRequest request, ServerCallContext context) =>
        Execute<Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeRequest, Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeResult>(request, context, "construction_operations.construct_vector_in_working_coordinates_begin_direction_magnitude");

    [OperationImplementation("construction_operations.copy_groups_excluding_obscured_points")]
    public override Task<Api.CopyGroupsExcludingObscuredPointsResult> CopyGroupsExcludingObscuredPoints(Api.CopyGroupsExcludingObscuredPointsRequest request, ServerCallContext context) =>
        Execute<Api.CopyGroupsExcludingObscuredPointsRequest, Api.CopyGroupsExcludingObscuredPointsResult>(request, context, "construction_operations.copy_groups_excluding_obscured_points");

    [OperationImplementation("construction_operations.copy_object")]
    public override Task<Api.CopyObjectResult> CopyObject(Api.CopyObjectRequest request, ServerCallContext context) =>
        Execute<Api.CopyObjectRequest, Api.CopyObjectResult>(request, context, "construction_operations.copy_object");

    [OperationImplementation("construction_operations.copy_objects_point_to_point_delta")]
    public override Task<Api.CopyObjectsPointToPointDeltaResult> CopyObjectsPointToPointDelta(Api.CopyObjectsPointToPointDeltaRequest request, ServerCallContext context) =>
        Execute<Api.CopyObjectsPointToPointDeltaRequest, Api.CopyObjectsPointToPointDeltaResult>(request, context, "construction_operations.copy_objects_point_to_point_delta");

    [OperationImplementation("construction_operations.copy_objects_to_a_collection")]
    public override Task<Api.CopyObjectsToACollectionResult> CopyObjectsToACollection(Api.CopyObjectsToACollectionRequest request, ServerCallContext context) =>
        Execute<Api.CopyObjectsToACollectionRequest, Api.CopyObjectsToACollectionResult>(request, context, "construction_operations.copy_objects_to_a_collection");

    [OperationImplementation("construction_operations.create_hidden_point")]
    public override Task<Api.CreateHiddenPointResult> CreateHiddenPoint(Api.CreateHiddenPointRequest request, ServerCallContext context) =>
        Execute<Api.CreateHiddenPointRequest, Api.CreateHiddenPointResult>(request, context, "construction_operations.create_hidden_point");

    [OperationImplementation("construction_operations.create_hidden_point_rod")]
    public override Task<Api.CreateHiddenPointRodResult> CreateHiddenPointRod(Api.CreateHiddenPointRodRequest request, ServerCallContext context) =>
        Execute<Api.CreateHiddenPointRodRequest, Api.CreateHiddenPointRodResult>(request, context, "construction_operations.create_hidden_point_rod");

    [OperationImplementation("construction_operations.create_min_max_vector_group_callout")]
    public override Task<Api.CreateMinMaxVectorGroupCalloutResult> CreateMinMaxVectorGroupCallout(Api.CreateMinMaxVectorGroupCalloutRequest request, ServerCallContext context) =>
        Execute<Api.CreateMinMaxVectorGroupCalloutRequest, Api.CreateMinMaxVectorGroupCalloutResult>(request, context, "construction_operations.create_min_max_vector_group_callout");

    [OperationImplementation("construction_operations.create_picture_callout")]
    public override Task<Api.CreatePictureCalloutResult> CreatePictureCallout(Api.CreatePictureCalloutRequest request, ServerCallContext context) =>
        Execute<Api.CreatePictureCalloutRequest, Api.CreatePictureCalloutResult>(request, context, "construction_operations.create_picture_callout");

    [OperationImplementation("construction_operations.create_point_callout")]
    public override Task<Api.CreatePointCalloutResult> CreatePointCallout(Api.CreatePointCalloutRequest request, ServerCallContext context) =>
        Execute<Api.CreatePointCalloutRequest, Api.CreatePointCalloutResult>(request, context, "construction_operations.create_point_callout");

    [OperationImplementation("construction_operations.create_point_comparison_callout")]
    public override Task<Api.CreatePointComparisonCalloutResult> CreatePointComparisonCallout(Api.CreatePointComparisonCalloutRequest request, ServerCallContext context) =>
        Execute<Api.CreatePointComparisonCalloutRequest, Api.CreatePointComparisonCalloutResult>(request, context, "construction_operations.create_point_comparison_callout");

    [OperationImplementation("construction_operations.create_relationship_callout")]
    public override Task<Api.CreateRelationshipCalloutResult> CreateRelationshipCallout(Api.CreateRelationshipCalloutRequest request, ServerCallContext context) =>
        Execute<Api.CreateRelationshipCalloutRequest, Api.CreateRelationshipCalloutResult>(request, context, "construction_operations.create_relationship_callout");

    [OperationImplementation("construction_operations.create_text_callout")]
    public override Task<Api.CreateTextCalloutResult> CreateTextCallout(Api.CreateTextCalloutRequest request, ServerCallContext context) =>
        Execute<Api.CreateTextCalloutRequest, Api.CreateTextCalloutResult>(request, context, "construction_operations.create_text_callout");

    [OperationImplementation("construction_operations.create_vector_callout")]
    public override Task<Api.CreateVectorCalloutResult> CreateVectorCallout(Api.CreateVectorCalloutRequest request, ServerCallContext context) =>
        Execute<Api.CreateVectorCalloutRequest, Api.CreateVectorCalloutResult>(request, context, "construction_operations.create_vector_callout");

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_xyz")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerXyzResult> DecomposeTransformIntoDoublesEulerXyz(Api.DecomposeTransformIntoDoublesEulerXyzRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeTransformIntoDoublesEulerXyzRequest, Api.DecomposeTransformIntoDoublesEulerXyzResult>(request, context, "construction_operations.decompose_transform_into_doubles_euler_xyz");

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_zxz")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerZxzResult> DecomposeTransformIntoDoublesEulerZxz(Api.DecomposeTransformIntoDoublesEulerZxzRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeTransformIntoDoublesEulerZxzRequest, Api.DecomposeTransformIntoDoublesEulerZxzResult>(request, context, "construction_operations.decompose_transform_into_doubles_euler_zxz");

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_zyx")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerZyxResult> DecomposeTransformIntoDoublesEulerZyx(Api.DecomposeTransformIntoDoublesEulerZyxRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeTransformIntoDoublesEulerZyxRequest, Api.DecomposeTransformIntoDoublesEulerZyxResult>(request, context, "construction_operations.decompose_transform_into_doubles_euler_zyx");

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_zyz")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerZyzResult> DecomposeTransformIntoDoublesEulerZyz(Api.DecomposeTransformIntoDoublesEulerZyzRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeTransformIntoDoublesEulerZyzRequest, Api.DecomposeTransformIntoDoublesEulerZyzResult>(request, context, "construction_operations.decompose_transform_into_doubles_euler_zyz");

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_fixed_xyz")]
    public override Task<Api.DecomposeTransformIntoDoublesFixedXyzResult> DecomposeTransformIntoDoublesFixedXyz(Api.DecomposeTransformIntoDoublesFixedXyzRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeTransformIntoDoublesFixedXyzRequest, Api.DecomposeTransformIntoDoublesFixedXyzResult>(request, context, "construction_operations.decompose_transform_into_doubles_fixed_xyz");

    [OperationImplementation("construction_operations.decompose_transform_into_vectors_fixed_xyz")]
    public override Task<Api.DecomposeTransformIntoVectorsFixedXyzResult> DecomposeTransformIntoVectorsFixedXyz(Api.DecomposeTransformIntoVectorsFixedXyzRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeTransformIntoVectorsFixedXyzRequest, Api.DecomposeTransformIntoVectorsFixedXyzResult>(request, context, "construction_operations.decompose_transform_into_vectors_fixed_xyz");

    [OperationImplementation("construction_operations.decompose_transform_into_vectors_origin_and_axes")]
    public override Task<Api.DecomposeTransformIntoVectorsOriginAndAxesResult> DecomposeTransformIntoVectorsOriginAndAxes(Api.DecomposeTransformIntoVectorsOriginAndAxesRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeTransformIntoVectorsOriginAndAxesRequest, Api.DecomposeTransformIntoVectorsOriginAndAxesResult>(request, context, "construction_operations.decompose_transform_into_vectors_origin_and_axes");

    [OperationImplementation("construction_operations.decompose_world_transform_operator_into_doubles_fixed_xyz_in_world")]
    public override Task<Api.DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldResult> DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorld(Api.DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldRequest, Api.DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldResult>(request, context, "construction_operations.decompose_world_transform_operator_into_doubles_fixed_xyz_in_world");

    [OperationImplementation("construction_operations.decompose_world_transform_operator_into_vectors_fixed_xyz_in_world")]
    public override Task<Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldResult> DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorld(Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldRequest request, ServerCallContext context) =>
        Execute<Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldRequest, Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldResult>(request, context, "construction_operations.decompose_world_transform_operator_into_vectors_fixed_xyz_in_world");

    [OperationImplementation("construction_operations.delete_callout_view")]
    public override Task<Api.DeleteCalloutViewResult> DeleteCalloutView(Api.DeleteCalloutViewRequest request, ServerCallContext context) =>
        Execute<Api.DeleteCalloutViewRequest, Api.DeleteCalloutViewResult>(request, context, "construction_operations.delete_callout_view");

    [OperationImplementation("construction_operations.delete_collection")]
    public override Task<Api.DeleteCollectionResult> DeleteCollection(Api.DeleteCollectionRequest request, ServerCallContext context) =>
        Execute<Api.DeleteCollectionRequest, Api.DeleteCollectionResult>(request, context, "construction_operations.delete_collection");

    [OperationImplementation("construction_operations.delete_collections_by_wildcard")]
    public override Task<Api.DeleteCollectionsByWildcardResult> DeleteCollectionsByWildcard(Api.DeleteCollectionsByWildcardRequest request, ServerCallContext context) =>
        Execute<Api.DeleteCollectionsByWildcardRequest, Api.DeleteCollectionsByWildcardResult>(request, context, "construction_operations.delete_collections_by_wildcard");

    [OperationImplementation("construction_operations.delete_folders_by_wildcard")]
    public override Task<Api.DeleteFoldersByWildcardResult> DeleteFoldersByWildcard(Api.DeleteFoldersByWildcardRequest request, ServerCallContext context) =>
        Execute<Api.DeleteFoldersByWildcardRequest, Api.DeleteFoldersByWildcardResult>(request, context, "construction_operations.delete_folders_by_wildcard");

    [OperationImplementation("construction_operations.delete_hidden_point_rod")]
    public override Task<Api.DeleteHiddenPointRodResult> DeleteHiddenPointRod(Api.DeleteHiddenPointRodRequest request, ServerCallContext context) =>
        Execute<Api.DeleteHiddenPointRodRequest, Api.DeleteHiddenPointRodResult>(request, context, "construction_operations.delete_hidden_point_rod");

    [OperationImplementation("construction_operations.delete_points")]
    public override Task<Api.DeletePointsResult> DeletePoints(Api.DeletePointsRequest request, ServerCallContext context) =>
        Execute<Api.DeletePointsRequest, Api.DeletePointsResult>(request, context, "construction_operations.delete_points");

    [OperationImplementation("construction_operations.delete_points_wildcard_selection")]
    public override Task<Api.DeletePointsWildcardSelectionResult> DeletePointsWildcardSelection(Api.DeletePointsWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.DeletePointsWildcardSelectionRequest, Api.DeletePointsWildcardSelectionResult>(request, context, "construction_operations.delete_points_wildcard_selection");

    [OperationImplementation("construction_operations.extract_sphere_centers_from_point_cloud")]
    public override Task<Api.ExtractSphereCentersFromPointCloudResult> ExtractSphereCentersFromPointCloud(Api.ExtractSphereCentersFromPointCloudRequest request, ServerCallContext context) =>
        Execute<Api.ExtractSphereCentersFromPointCloudRequest, Api.ExtractSphereCentersFromPointCloudResult>(request, context, "construction_operations.extract_sphere_centers_from_point_cloud");

    [OperationImplementation("construction_operations.get_collection_instrument_ref_list_variable")]
    public override Task<Api.GetCollectionInstrumentRefListVariableResult> GetCollectionInstrumentRefListVariable(Api.GetCollectionInstrumentRefListVariableRequest request, ServerCallContext context) =>
        Execute<Api.GetCollectionInstrumentRefListVariableRequest, Api.GetCollectionInstrumentRefListVariableResult>(request, context, "construction_operations.get_collection_instrument_ref_list_variable");

    [OperationImplementation("construction_operations.get_gradient_at_projected_point_on_surface")]
    public override Task<Api.GetGradientAtProjectedPointOnSurfaceResult> GetGradientAtProjectedPointOnSurface(Api.GetGradientAtProjectedPointOnSurfaceRequest request, ServerCallContext context) =>
        Execute<Api.GetGradientAtProjectedPointOnSurfaceRequest, Api.GetGradientAtProjectedPointOnSurfaceResult>(request, context, "construction_operations.get_gradient_at_projected_point_on_surface");

    [OperationImplementation("construction_operations.get_gradient_at_projected_point_on_surface_edge")]
    public override Task<Api.GetGradientAtProjectedPointOnSurfaceEdgeResult> GetGradientAtProjectedPointOnSurfaceEdge(Api.GetGradientAtProjectedPointOnSurfaceEdgeRequest request, ServerCallContext context) =>
        Execute<Api.GetGradientAtProjectedPointOnSurfaceEdgeRequest, Api.GetGradientAtProjectedPointOnSurfaceEdgeResult>(request, context, "construction_operations.get_gradient_at_projected_point_on_surface_edge");

    [OperationImplementation("construction_operations.get_hidden_point_rod_index_by_name")]
    public override Task<Api.GetHiddenPointRodIndexByNameResult> GetHiddenPointRodIndexByName(Api.GetHiddenPointRodIndexByNameRequest request, ServerCallContext context) =>
        Execute<Api.GetHiddenPointRodIndexByNameRequest, Api.GetHiddenPointRodIndexByNameResult>(request, context, "construction_operations.get_hidden_point_rod_index_by_name");

    [OperationImplementation("construction_operations.get_ith_callout_position_in_callout_view")]
    public override Task<Api.GetIthCalloutPositionInCalloutViewResult> GetIthCalloutPositionInCalloutView(Api.GetIthCalloutPositionInCalloutViewRequest request, ServerCallContext context) =>
        Execute<Api.GetIthCalloutPositionInCalloutViewRequest, Api.GetIthCalloutPositionInCalloutViewResult>(request, context, "construction_operations.get_ith_callout_position_in_callout_view");

    [OperationImplementation("construction_operations.get_number_of_callouts_in_callout_view")]
    public override Task<Api.GetNumberOfCalloutsInCalloutViewResult> GetNumberOfCalloutsInCalloutView(Api.GetNumberOfCalloutsInCalloutViewRequest request, ServerCallContext context) =>
        Execute<Api.GetNumberOfCalloutsInCalloutViewRequest, Api.GetNumberOfCalloutsInCalloutViewResult>(request, context, "construction_operations.get_number_of_callouts_in_callout_view");

    [OperationImplementation("construction_operations.get_working_transform_of_object_fixed_xyz")]
    public override Task<Api.GetWorkingTransformOfObjectFixedXyzResult> GetWorkingTransformOfObjectFixedXyz(Api.GetWorkingTransformOfObjectFixedXyzRequest request, ServerCallContext context) =>
        Execute<Api.GetWorkingTransformOfObjectFixedXyzRequest, Api.GetWorkingTransformOfObjectFixedXyzResult>(request, context, "construction_operations.get_working_transform_of_object_fixed_xyz");

    [OperationImplementation("construction_operations.invert_transform")]
    public override Task<Api.InvertTransformResult> InvertTransform(Api.InvertTransformRequest request, ServerCallContext context) =>
        Execute<Api.InvertTransformRequest, Api.InvertTransformResult>(request, context, "construction_operations.invert_transform");

    [OperationImplementation("construction_operations.make_callout_view_ref_list_wildcard_selection")]
    public override Task<Api.MakeCalloutViewRefListWildcardSelectionResult> MakeCalloutViewRefListWildcardSelection(Api.MakeCalloutViewRefListWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.MakeCalloutViewRefListWildcardSelectionRequest, Api.MakeCalloutViewRefListWildcardSelectionResult>(request, context, "construction_operations.make_callout_view_ref_list_wildcard_selection");

    [OperationImplementation("construction_operations.make_collection_instrument_id_runtime_select")]
    public override Task<Api.MakeCollectionInstrumentIdRuntimeSelectResult> MakeCollectionInstrumentIdRuntimeSelect(Api.MakeCollectionInstrumentIdRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionInstrumentIdRuntimeSelectRequest, Api.MakeCollectionInstrumentIdRuntimeSelectResult>(request, context, "construction_operations.make_collection_instrument_id_runtime_select");

    [OperationImplementation("construction_operations.make_collection_instrument_ref_list_runtime_select")]
    public override Task<Api.MakeCollectionInstrumentRefListRuntimeSelectResult> MakeCollectionInstrumentRefListRuntimeSelect(Api.MakeCollectionInstrumentRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionInstrumentRefListRuntimeSelectRequest, Api.MakeCollectionInstrumentRefListRuntimeSelectResult>(request, context, "construction_operations.make_collection_instrument_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_collection_item_name_ref_list_wildcard_selection")]
    public override Task<Api.MakeCollectionItemNameRefListWildcardSelectionResult> MakeCollectionItemNameRefListWildcardSelection(Api.MakeCollectionItemNameRefListWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionItemNameRefListWildcardSelectionRequest, Api.MakeCollectionItemNameRefListWildcardSelectionResult>(request, context, "construction_operations.make_collection_item_name_ref_list_wildcard_selection");

    [OperationImplementation("construction_operations.make_collection_name_runtime_select")]
    public override Task<Api.MakeCollectionNameRuntimeSelectResult> MakeCollectionNameRuntimeSelect(Api.MakeCollectionNameRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionNameRuntimeSelectRequest, Api.MakeCollectionNameRuntimeSelectResult>(request, context, "construction_operations.make_collection_name_runtime_select");

    [OperationImplementation("construction_operations.make_collection_object_name_ensure_unique")]
    public override Task<Api.MakeCollectionObjectNameEnsureUniqueResult> MakeCollectionObjectNameEnsureUnique(Api.MakeCollectionObjectNameEnsureUniqueRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionObjectNameEnsureUniqueRequest, Api.MakeCollectionObjectNameEnsureUniqueResult>(request, context, "construction_operations.make_collection_object_name_ensure_unique");

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_by_type")]
    public override Task<Api.MakeCollectionObjectNameRefListByTypeResult> MakeCollectionObjectNameRefListByType(Api.MakeCollectionObjectNameRefListByTypeRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionObjectNameRefListByTypeRequest, Api.MakeCollectionObjectNameRefListByTypeResult>(request, context, "construction_operations.make_collection_object_name_ref_list_by_type");

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_by_type_and_color")]
    public override Task<Api.MakeCollectionObjectNameRefListByTypeAndColorResult> MakeCollectionObjectNameRefListByTypeAndColor(Api.MakeCollectionObjectNameRefListByTypeAndColorRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionObjectNameRefListByTypeAndColorRequest, Api.MakeCollectionObjectNameRefListByTypeAndColorResult>(request, context, "construction_operations.make_collection_object_name_ref_list_by_type_and_color");

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_from_all_groups_in_collection")]
    public override Task<Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionResult> MakeCollectionObjectNameRefListFromAllGroupsInCollection(Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionRequest, Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionResult>(request, context, "construction_operations.make_collection_object_name_ref_list_from_all_groups_in_collection");

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_runtime_select")]
    public override Task<Api.MakeCollectionObjectNameRefListRuntimeSelectResult> MakeCollectionObjectNameRefListRuntimeSelect(Api.MakeCollectionObjectNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionObjectNameRefListRuntimeSelectRequest, Api.MakeCollectionObjectNameRefListRuntimeSelectResult>(request, context, "construction_operations.make_collection_object_name_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_wildcard_selection")]
    public override Task<Api.MakeCollectionObjectNameRefListWildcardSelectionResult> MakeCollectionObjectNameRefListWildcardSelection(Api.MakeCollectionObjectNameRefListWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionObjectNameRefListWildcardSelectionRequest, Api.MakeCollectionObjectNameRefListWildcardSelectionResult>(request, context, "construction_operations.make_collection_object_name_ref_list_wildcard_selection");

    [OperationImplementation("construction_operations.make_collection_object_name_runtime_select")]
    public override Task<Api.MakeCollectionObjectNameRuntimeSelectResult> MakeCollectionObjectNameRuntimeSelect(Api.MakeCollectionObjectNameRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionObjectNameRuntimeSelectRequest, Api.MakeCollectionObjectNameRuntimeSelectResult>(request, context, "construction_operations.make_collection_object_name_runtime_select");

    [OperationImplementation("construction_operations.make_collection_vector_group_name_ref_list_runtime_select")]
    public override Task<Api.MakeCollectionVectorGroupNameRefListRuntimeSelectResult> MakeCollectionVectorGroupNameRefListRuntimeSelect(Api.MakeCollectionVectorGroupNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeCollectionVectorGroupNameRefListRuntimeSelectRequest, Api.MakeCollectionVectorGroupNameRefListRuntimeSelectResult>(request, context, "construction_operations.make_collection_vector_group_name_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_event_ref_list_wildcard_selection")]
    public override Task<Api.MakeEventRefListWildcardSelectionResult> MakeEventRefListWildcardSelection(Api.MakeEventRefListWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.MakeEventRefListWildcardSelectionRequest, Api.MakeEventRefListWildcardSelectionResult>(request, context, "construction_operations.make_event_ref_list_wildcard_selection");

    [OperationImplementation("construction_operations.make_picture_name_ref_list_runtime_select")]
    public override Task<Api.MakePictureNameRefListRuntimeSelectResult> MakePictureNameRefListRuntimeSelect(Api.MakePictureNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakePictureNameRefListRuntimeSelectRequest, Api.MakePictureNameRefListRuntimeSelectResult>(request, context, "construction_operations.make_picture_name_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_point_name_ensure_unique")]
    public override Task<Api.MakePointNameEnsureUniqueResult> MakePointNameEnsureUnique(Api.MakePointNameEnsureUniqueRequest request, ServerCallContext context) =>
        Execute<Api.MakePointNameEnsureUniqueRequest, Api.MakePointNameEnsureUniqueResult>(request, context, "construction_operations.make_point_name_ensure_unique");

    [OperationImplementation("construction_operations.make_point_name_ref_list_from_group")]
    public override Task<Api.MakePointNameRefListFromGroupResult> MakePointNameRefListFromGroup(Api.MakePointNameRefListFromGroupRequest request, ServerCallContext context) =>
        Execute<Api.MakePointNameRefListFromGroupRequest, Api.MakePointNameRefListFromGroupResult>(request, context, "construction_operations.make_point_name_ref_list_from_group");

    [OperationImplementation("construction_operations.make_point_name_ref_list_runtime_select")]
    public override Task<Api.MakePointNameRefListRuntimeSelectResult> MakePointNameRefListRuntimeSelect(Api.MakePointNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakePointNameRefListRuntimeSelectRequest, Api.MakePointNameRefListRuntimeSelectResult>(request, context, "construction_operations.make_point_name_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_point_name_ref_list_wildcard_select")]
    public override Task<Api.MakePointNameRefListWildcardSelectResult> MakePointNameRefListWildcardSelect(Api.MakePointNameRefListWildcardSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakePointNameRefListWildcardSelectRequest, Api.MakePointNameRefListWildcardSelectResult>(request, context, "construction_operations.make_point_name_ref_list_wildcard_select");

    [OperationImplementation("construction_operations.make_point_name_runtime_select")]
    public override Task<Api.MakePointNameRuntimeSelectResult> MakePointNameRuntimeSelect(Api.MakePointNameRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakePointNameRuntimeSelectRequest, Api.MakePointNameRuntimeSelectResult>(request, context, "construction_operations.make_point_name_runtime_select");

    [OperationImplementation("construction_operations.make_relationship_ref_list_runtime_select")]
    public override Task<Api.MakeRelationshipRefListRuntimeSelectResult> MakeRelationshipRefListRuntimeSelect(Api.MakeRelationshipRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeRelationshipRefListRuntimeSelectRequest, Api.MakeRelationshipRefListRuntimeSelectResult>(request, context, "construction_operations.make_relationship_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_relationship_ref_list_wildcard_selection")]
    public override Task<Api.MakeRelationshipRefListWildcardSelectionResult> MakeRelationshipRefListWildcardSelection(Api.MakeRelationshipRefListWildcardSelectionRequest request, ServerCallContext context) =>
        Execute<Api.MakeRelationshipRefListWildcardSelectionRequest, Api.MakeRelationshipRefListWildcardSelectionResult>(request, context, "construction_operations.make_relationship_ref_list_wildcard_selection");

    [OperationImplementation("construction_operations.make_report_ref_list_from_collection")]
    public override Task<Api.MakeReportRefListFromCollectionResult> MakeReportRefListFromCollection(Api.MakeReportRefListFromCollectionRequest request, ServerCallContext context) =>
        Execute<Api.MakeReportRefListFromCollectionRequest, Api.MakeReportRefListFromCollectionResult>(request, context, "construction_operations.make_report_ref_list_from_collection");

    [OperationImplementation("construction_operations.make_report_ref_list_runtime_select")]
    public override Task<Api.MakeReportRefListRuntimeSelectResult> MakeReportRefListRuntimeSelect(Api.MakeReportRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeReportRefListRuntimeSelectRequest, Api.MakeReportRefListRuntimeSelectResult>(request, context, "construction_operations.make_report_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_system_string")]
    public override Task<Api.MakeSystemStringResult> MakeSystemString(Api.MakeSystemStringRequest request, ServerCallContext context) =>
        Execute<Api.MakeSystemStringRequest, Api.MakeSystemStringResult>(request, context, "construction_operations.make_system_string");

    [OperationImplementation("construction_operations.make_transform_from_doubles_euler_parameters")]
    public override Task<Api.MakeTransformFromDoublesEulerParametersResult> MakeTransformFromDoublesEulerParameters(Api.MakeTransformFromDoublesEulerParametersRequest request, ServerCallContext context) =>
        Execute<Api.MakeTransformFromDoublesEulerParametersRequest, Api.MakeTransformFromDoublesEulerParametersResult>(request, context, "construction_operations.make_transform_from_doubles_euler_parameters");

    [OperationImplementation("construction_operations.make_transform_from_doubles_fixed_xyz")]
    public override Task<Api.MakeTransformFromDoublesFixedXyzResult> MakeTransformFromDoublesFixedXyz(Api.MakeTransformFromDoublesFixedXyzRequest request, ServerCallContext context) =>
        Execute<Api.MakeTransformFromDoublesFixedXyzRequest, Api.MakeTransformFromDoublesFixedXyzResult>(request, context, "construction_operations.make_transform_from_doubles_fixed_xyz");

    [OperationImplementation("construction_operations.make_vector_name_ref_list_from_vector_group")]
    public override Task<Api.MakeVectorNameRefListFromVectorGroupResult> MakeVectorNameRefListFromVectorGroup(Api.MakeVectorNameRefListFromVectorGroupRequest request, ServerCallContext context) =>
        Execute<Api.MakeVectorNameRefListFromVectorGroupRequest, Api.MakeVectorNameRefListFromVectorGroupResult>(request, context, "construction_operations.make_vector_name_ref_list_from_vector_group");

    [OperationImplementation("construction_operations.make_vector_name_ref_list_runtime_select")]
    public override Task<Api.MakeVectorNameRefListRuntimeSelectResult> MakeVectorNameRefListRuntimeSelect(Api.MakeVectorNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        Execute<Api.MakeVectorNameRefListRuntimeSelectRequest, Api.MakeVectorNameRefListRuntimeSelectResult>(request, context, "construction_operations.make_vector_name_ref_list_runtime_select");

    [OperationImplementation("construction_operations.make_vector_names_unique_in_vector_group")]
    public override Task<Api.MakeVectorNamesUniqueInVectorGroupResult> MakeVectorNamesUniqueInVectorGroup(Api.MakeVectorNamesUniqueInVectorGroupRequest request, ServerCallContext context) =>
        Execute<Api.MakeVectorNamesUniqueInVectorGroupRequest, Api.MakeVectorNamesUniqueInVectorGroupResult>(request, context, "construction_operations.make_vector_names_unique_in_vector_group");

    [OperationImplementation("construction_operations.mirror_objects")]
    public override Task<Api.MirrorObjectsResult> MirrorObjects(Api.MirrorObjectsRequest request, ServerCallContext context) =>
        Execute<Api.MirrorObjectsRequest, Api.MirrorObjectsResult>(request, context, "construction_operations.mirror_objects");

    [OperationImplementation("construction_operations.move_objects_point_to_point_delta")]
    public override Task<Api.MoveObjectsPointToPointDeltaResult> MoveObjectsPointToPointDelta(Api.MoveObjectsPointToPointDeltaRequest request, ServerCallContext context) =>
        Execute<Api.MoveObjectsPointToPointDeltaRequest, Api.MoveObjectsPointToPointDeltaResult>(request, context, "construction_operations.move_objects_point_to_point_delta");

    [OperationImplementation("construction_operations.move_objects_to_a_collection")]
    public override Task<Api.MoveObjectsToACollectionResult> MoveObjectsToACollection(Api.MoveObjectsToACollectionRequest request, ServerCallContext context) =>
        Execute<Api.MoveObjectsToACollectionRequest, Api.MoveObjectsToACollectionResult>(request, context, "construction_operations.move_objects_to_a_collection");

    [OperationImplementation("construction_operations.rename_callout_view")]
    public override Task<Api.RenameCalloutViewResult> RenameCalloutView(Api.RenameCalloutViewRequest request, ServerCallContext context) =>
        Execute<Api.RenameCalloutViewRequest, Api.RenameCalloutViewResult>(request, context, "construction_operations.rename_callout_view");

    [OperationImplementation("construction_operations.rename_collection")]
    public override Task<Api.RenameCollectionResult> RenameCollection(Api.RenameCollectionRequest request, ServerCallContext context) =>
        Execute<Api.RenameCollectionRequest, Api.RenameCollectionResult>(request, context, "construction_operations.rename_collection");

    [OperationImplementation("construction_operations.rename_item")]
    public override Task<Api.RenameItemResult> RenameItem(Api.RenameItemRequest request, ServerCallContext context) =>
        Execute<Api.RenameItemRequest, Api.RenameItemResult>(request, context, "construction_operations.rename_item");

    [OperationImplementation("construction_operations.rename_object")]
    public override Task<Api.RenameObjectResult> RenameObject(Api.RenameObjectRequest request, ServerCallContext context) =>
        Execute<Api.RenameObjectRequest, Api.RenameObjectResult>(request, context, "construction_operations.rename_object");

    [OperationImplementation("construction_operations.rename_point")]
    public override Task<Api.RenamePointResult> RenamePoint(Api.RenamePointRequest request, ServerCallContext context) =>
        Execute<Api.RenamePointRequest, Api.RenamePointResult>(request, context, "construction_operations.rename_point");

    [OperationImplementation("construction_operations.rename_points_with_name_pattern")]
    public override Task<Api.RenamePointsWithNamePatternResult> RenamePointsWithNamePattern(Api.RenamePointsWithNamePatternRequest request, ServerCallContext context) =>
        Execute<Api.RenamePointsWithNamePatternRequest, Api.RenamePointsWithNamePatternResult>(request, context, "construction_operations.rename_points_with_name_pattern");

    [OperationImplementation("construction_operations.set_callout_view_properties")]
    public override Task<Api.SetCalloutViewPropertiesResult> SetCalloutViewProperties(Api.SetCalloutViewPropertiesRequest request, ServerCallContext context) =>
        Execute<Api.SetCalloutViewPropertiesRequest, Api.SetCalloutViewPropertiesResult>(request, context, "construction_operations.set_callout_view_properties");

    [OperationImplementation("construction_operations.set_collection_instrument_ref_list_variable")]
    public override Task<Api.SetCollectionInstrumentRefListVariableResult> SetCollectionInstrumentRefListVariable(Api.SetCollectionInstrumentRefListVariableRequest request, ServerCallContext context) =>
        Execute<Api.SetCollectionInstrumentRefListVariableRequest, Api.SetCollectionInstrumentRefListVariableResult>(request, context, "construction_operations.set_collection_instrument_ref_list_variable");

    [OperationImplementation("construction_operations.set_default_callout_view_properties")]
    public override Task<Api.SetDefaultCalloutViewPropertiesResult> SetDefaultCalloutViewProperties(Api.SetDefaultCalloutViewPropertiesRequest request, ServerCallContext context) =>
        Execute<Api.SetDefaultCalloutViewPropertiesRequest, Api.SetDefaultCalloutViewPropertiesResult>(request, context, "construction_operations.set_default_callout_view_properties");

    [OperationImplementation("construction_operations.set_ith_callout_position_in_callout_view")]
    public override Task<Api.SetIthCalloutPositionInCalloutViewResult> SetIthCalloutPositionInCalloutView(Api.SetIthCalloutPositionInCalloutViewRequest request, ServerCallContext context) =>
        Execute<Api.SetIthCalloutPositionInCalloutViewRequest, Api.SetIthCalloutPositionInCalloutViewResult>(request, context, "construction_operations.set_ith_callout_position_in_callout_view");

    [OperationImplementation("construction_operations.set_or_construct_default_collection")]
    public override Task<Api.SetOrConstructDefaultCollectionResult> SetOrConstructDefaultCollection(Api.SetOrConstructDefaultCollectionRequest request, ServerCallContext context) =>
        Execute<Api.SetOrConstructDefaultCollectionRequest, Api.SetOrConstructDefaultCollectionResult>(request, context, "construction_operations.set_or_construct_default_collection");

    [OperationImplementation("construction_operations.set_point_position_in_working_coordinates")]
    public override Task<Api.SetPointPositionInWorkingCoordinatesResult> SetPointPositionInWorkingCoordinates(Api.SetPointPositionInWorkingCoordinatesRequest request, ServerCallContext context) =>
        Execute<Api.SetPointPositionInWorkingCoordinatesRequest, Api.SetPointPositionInWorkingCoordinatesResult>(request, context, "construction_operations.set_point_position_in_working_coordinates");

    [OperationImplementation("construction_operations.shift_plane")]
    public override Task<Api.ShiftPlaneResult> ShiftPlane(Api.ShiftPlaneRequest request, ServerCallContext context) =>
        Execute<Api.ShiftPlaneRequest, Api.ShiftPlaneResult>(request, context, "construction_operations.shift_plane");

    [OperationImplementation("construction_operations.transform_points_by_delta_about_working_frame")]
    public override Task<Api.TransformPointsByDeltaAboutWorkingFrameResult> TransformPointsByDeltaAboutWorkingFrame(Api.TransformPointsByDeltaAboutWorkingFrameRequest request, ServerCallContext context) =>
        Execute<Api.TransformPointsByDeltaAboutWorkingFrameRequest, Api.TransformPointsByDeltaAboutWorkingFrameResult>(request, context, "construction_operations.transform_points_by_delta_about_working_frame");

    private Task<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        string operationId)
        where TRequest : class, Google.Protobuf.IMessage<TRequest>
        where TResponse : class, Google.Protobuf.IMessage<TResponse>, new() =>
        MpOperationServiceExecutor.ExecuteAsync<TRequest, TResponse>(
            _operationExecutor,
            request,
            context,
            operationId);
}
