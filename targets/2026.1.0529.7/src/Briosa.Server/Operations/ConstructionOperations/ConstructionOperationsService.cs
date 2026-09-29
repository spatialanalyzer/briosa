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
        _operationExecutor.ExecuteAsync(request, context, AddCollectionInstrumentsToRefListWildcardSelectionOperation.Descriptor,
            AddCollectionInstrumentsToRefListWildcardSelectionOperation.CreateCommand, AddCollectionInstrumentsToRefListWildcardSelectionOperation.OutputContracts,
            AddCollectionInstrumentsToRefListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.add_surface_to_mesh_offset_along_reference_direction")]
    public override Task<Api.AddSurfaceToMeshOffsetAlongReferenceDirectionResult> AddSurfaceToMeshOffsetAlongReferenceDirection(Api.AddSurfaceToMeshOffsetAlongReferenceDirectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, AddSurfaceToMeshOffsetAlongReferenceDirectionOperation.Descriptor,
            AddSurfaceToMeshOffsetAlongReferenceDirectionOperation.CreateCommand, AddSurfaceToMeshOffsetAlongReferenceDirectionOperation.OutputContracts, AddSurfaceToMeshOffsetAlongReferenceDirectionOperation.CreateResult);

    [OperationImplementation("construction_operations.auto_arrange_callout_view")]
    public override Task<Api.AutoArrangeCalloutViewResult> AutoArrangeCalloutView(Api.AutoArrangeCalloutViewRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, AutoArrangeCalloutViewOperation.Descriptor,
            AutoArrangeCalloutViewOperation.CreateCommand, AutoArrangeCalloutViewOperation.OutputContracts, AutoArrangeCalloutViewOperation.CreateResult);

    [OperationImplementation("construction_operations.average_set_of_groups")]
    public override Task<Api.AverageSetOfGroupsResult> AverageSetOfGroups(Api.AverageSetOfGroupsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, AverageSetOfGroupsOperation.Descriptor,
            AverageSetOfGroupsOperation.CreateCommand, AverageSetOfGroupsOperation.OutputContracts, AverageSetOfGroupsOperation.CreateResult);

    [OperationImplementation("construction_operations.clear_hidden_point_bar_database")]
    public override Task<Api.ClearHiddenPointBarDatabaseResult> ClearHiddenPointBarDatabase(Api.ClearHiddenPointBarDatabaseRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ClearHiddenPointBarDatabaseOperation.Descriptor,
            ClearHiddenPointBarDatabaseOperation.CreateCommand, ClearHiddenPointBarDatabaseOperation.OutputContracts, ClearHiddenPointBarDatabaseOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_boundary_points_from_cloud")]
    public override Task<Api.ConstructBoundaryPointsFromCloudResult> ConstructBoundaryPointsFromCloud(Api.ConstructBoundaryPointsFromCloudRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBoundaryPointsFromCloudOperation.Descriptor,
            ConstructBoundaryPointsFromCloudOperation.CreateCommand, ConstructBoundaryPointsFromCloudOperation.OutputContracts, ConstructBoundaryPointsFromCloudOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_spline_from_intersection_of_plane_and_surface")]
    public override Task<Api.ConstructBSplineFromIntersectionOfPlaneAndSurfaceResult> ConstructBSplineFromIntersectionOfPlaneAndSurface(Api.ConstructBSplineFromIntersectionOfPlaneAndSurfaceRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplineFromIntersectionOfPlaneAndSurfaceOperation.Descriptor,
            ConstructBSplineFromIntersectionOfPlaneAndSurfaceOperation.CreateCommand, ConstructBSplineFromIntersectionOfPlaneAndSurfaceOperation.OutputContracts, ConstructBSplineFromIntersectionOfPlaneAndSurfaceOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_spline_from_intersection_of_surfaces")]
    public override Task<Api.ConstructBSplineFromIntersectionOfSurfacesResult> ConstructBSplineFromIntersectionOfSurfaces(Api.ConstructBSplineFromIntersectionOfSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplineFromIntersectionOfSurfacesOperation.Descriptor,
            ConstructBSplineFromIntersectionOfSurfacesOperation.CreateCommand, ConstructBSplineFromIntersectionOfSurfacesOperation.OutputContracts, ConstructBSplineFromIntersectionOfSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_spline_from_points")]
    public override Task<Api.ConstructBSplineFromPointsResult> ConstructBSplineFromPoints(Api.ConstructBSplineFromPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplineFromPointsOperation.Descriptor,
            ConstructBSplineFromPointsOperation.CreateCommand, ConstructBSplineFromPointsOperation.OutputContracts, ConstructBSplineFromPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_spline_from_point_set")]
    public override Task<Api.ConstructBSplineFromPointSetResult> ConstructBSplineFromPointSet(Api.ConstructBSplineFromPointSetRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplineFromPointSetOperation.Descriptor,
            ConstructBSplineFromPointSetOperation.CreateCommand, ConstructBSplineFromPointSetOperation.OutputContracts, ConstructBSplineFromPointSetOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_spline_from_several_b_splines")]
    public override Task<Api.ConstructBSplineFromSeveralBSplinesResult> ConstructBSplineFromSeveralBSplines(Api.ConstructBSplineFromSeveralBSplinesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplineFromSeveralBSplinesOperation.Descriptor,
            ConstructBSplineFromSeveralBSplinesOperation.CreateCommand, ConstructBSplineFromSeveralBSplinesOperation.OutputContracts, ConstructBSplineFromSeveralBSplinesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_splines_from_intersection_of_plane_and_mesh")]
    public override Task<Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshResult> ConstructBSplinesFromIntersectionOfPlaneAndMesh(Api.ConstructBSplinesFromIntersectionOfPlaneAndMeshRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplinesFromIntersectionOfPlaneAndMeshOperation.Descriptor,
            ConstructBSplinesFromIntersectionOfPlaneAndMeshOperation.CreateCommand, ConstructBSplinesFromIntersectionOfPlaneAndMeshOperation.OutputContracts, ConstructBSplinesFromIntersectionOfPlaneAndMeshOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_splines_from_lines")]
    public override Task<Api.ConstructBSplinesFromLinesResult> ConstructBSplinesFromLines(Api.ConstructBSplinesFromLinesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplinesFromLinesOperation.Descriptor,
            ConstructBSplinesFromLinesOperation.CreateCommand, ConstructBSplinesFromLinesOperation.OutputContracts, ConstructBSplinesFromLinesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_b_splines_from_surfaces")]
    public override Task<Api.ConstructBSplinesFromSurfacesResult> ConstructBSplinesFromSurfaces(Api.ConstructBSplinesFromSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructBSplinesFromSurfacesOperation.Descriptor,
            ConstructBSplinesFromSurfacesOperation.CreateCommand, ConstructBSplinesFromSurfacesOperation.OutputContracts, ConstructBSplinesFromSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_circle")]
    public override Task<Api.ConstructCircleResult> ConstructCircle(Api.ConstructCircleRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCircleOperation.Descriptor,
            ConstructCircleOperation.CreateCommand, ConstructCircleOperation.OutputContracts, ConstructCircleOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_circles_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructCirclesFromSurfaceFacesRuntimeSelectResult> ConstructCirclesFromSurfaceFacesRuntimeSelect(Api.ConstructCirclesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCirclesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructCirclesFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructCirclesFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructCirclesFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_circles_lines_from_surfaces")]
    public override Task<Api.ConstructCirclesLinesFromSurfacesResult> ConstructCirclesLinesFromSurfaces(Api.ConstructCirclesLinesFromSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCirclesLinesFromSurfacesOperation.Descriptor,
            ConstructCirclesLinesFromSurfacesOperation.CreateCommand, ConstructCirclesLinesFromSurfacesOperation.OutputContracts, ConstructCirclesLinesFromSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_collection")]
    public override Task<Api.ConstructCollectionResult> ConstructCollection(Api.ConstructCollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCollectionOperation.Descriptor,
            ConstructCollectionOperation.CreateCommand, ConstructCollectionOperation.OutputContracts, ConstructCollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_cone")]
    public override Task<Api.ConstructConeResult> ConstructCone(Api.ConstructConeRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructConeOperation.Descriptor,
            ConstructConeOperation.CreateCommand, ConstructConeOperation.OutputContracts, ConstructConeOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_cones_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructConesFromSurfaceFacesRuntimeSelectResult> ConstructConesFromSurfaceFacesRuntimeSelect(Api.ConstructConesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructConesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructConesFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructConesFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructConesFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_cross_section_cloud")]
    public override Task<Api.ConstructCrossSectionCloudResult> ConstructCrossSectionCloud(Api.ConstructCrossSectionCloudRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCrossSectionCloudOperation.Descriptor,
            ConstructCrossSectionCloudOperation.CreateCommand, ConstructCrossSectionCloudOperation.OutputContracts, ConstructCrossSectionCloudOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_cross_section_cloud_user_select")]
    public override Task<Api.ConstructCrossSectionCloudUserSelectResult> ConstructCrossSectionCloudUserSelect(Api.ConstructCrossSectionCloudUserSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCrossSectionCloudUserSelectOperation.Descriptor,
            ConstructCrossSectionCloudUserSelectOperation.CreateCommand, ConstructCrossSectionCloudUserSelectOperation.OutputContracts, ConstructCrossSectionCloudUserSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_cylinder")]
    public override Task<Api.ConstructCylinderResult> ConstructCylinder(Api.ConstructCylinderRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCylinderOperation.Descriptor,
            ConstructCylinderOperation.CreateCommand, ConstructCylinderOperation.OutputContracts, ConstructCylinderOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_cylinder_from_end_points")]
    public override Task<Api.ConstructCylinderFromEndPointsResult> ConstructCylinderFromEndPoints(Api.ConstructCylinderFromEndPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCylinderFromEndPointsOperation.Descriptor,
            ConstructCylinderFromEndPointsOperation.CreateCommand, ConstructCylinderFromEndPointsOperation.OutputContracts, ConstructCylinderFromEndPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_cylinders_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructCylindersFromSurfaceFacesRuntimeSelectResult> ConstructCylindersFromSurfaceFacesRuntimeSelect(Api.ConstructCylindersFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructCylindersFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructCylindersFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructCylindersFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructCylindersFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_ellipse")]
    public override Task<Api.ConstructEllipseResult> ConstructEllipse(Api.ConstructEllipseRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructEllipseOperation.Descriptor,
            ConstructEllipseOperation.CreateCommand, ConstructEllipseOperation.OutputContracts, ConstructEllipseOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_ellipsoid")]
    public override Task<Api.ConstructEllipsoidResult> ConstructEllipsoid(Api.ConstructEllipsoidRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructEllipsoidOperation.Descriptor,
            ConstructEllipsoidOperation.CreateCommand, ConstructEllipsoidOperation.OutputContracts, ConstructEllipsoidOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_folders")]
    public override Task<Api.ConstructFoldersResult> ConstructFolders(Api.ConstructFoldersRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFoldersOperation.Descriptor,
            ConstructFoldersOperation.CreateCommand, ConstructFoldersOperation.OutputContracts, ConstructFoldersOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame")]
    public override Task<Api.ConstructFrameResult> ConstructFrame(Api.ConstructFrameRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameOperation.Descriptor,
            ConstructFrameOperation.CreateCommand, ConstructFrameOperation.OutputContracts, ConstructFrameOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_at_point_with_working_z_and_clocked_axis")]
    public override Task<Api.ConstructFrameAtPointWithWorkingZAndClockedAxisResult> ConstructFrameAtPointWithWorkingZAndClockedAxis(Api.ConstructFrameAtPointWithWorkingZAndClockedAxisRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameAtPointWithWorkingZAndClockedAxisOperation.Descriptor,
            ConstructFrameAtPointWithWorkingZAndClockedAxisOperation.CreateCommand, ConstructFrameAtPointWithWorkingZAndClockedAxisOperation.OutputContracts,
            ConstructFrameAtPointWithWorkingZAndClockedAxisOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_at_robot_link")]
    public override Task<Api.ConstructFrameAtRobotLinkResult> ConstructFrameAtRobotLink(Api.ConstructFrameAtRobotLinkRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameAtRobotLinkOperation.Descriptor,
            ConstructFrameAtRobotLinkOperation.CreateCommand, ConstructFrameAtRobotLinkOperation.OutputContracts,
            ConstructFrameAtRobotLinkOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_average_of_other_object_frames")]
    public override Task<Api.ConstructFrameAverageOfOtherObjectFramesResult> ConstructFrameAverageOfOtherObjectFrames(Api.ConstructFrameAverageOfOtherObjectFramesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameAverageOfOtherObjectFramesOperation.Descriptor,
            ConstructFrameAverageOfOtherObjectFramesOperation.CreateCommand, ConstructFrameAverageOfOtherObjectFramesOperation.OutputContracts,
            ConstructFrameAverageOfOtherObjectFramesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_copy_and_make_left_handed")]
    public override Task<Api.ConstructFrameCopyAndMakeLeftHandedResult> ConstructFrameCopyAndMakeLeftHanded(Api.ConstructFrameCopyAndMakeLeftHandedRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameCopyAndMakeLeftHandedOperation.Descriptor,
            ConstructFrameCopyAndMakeLeftHandedOperation.CreateCommand, ConstructFrameCopyAndMakeLeftHandedOperation.OutputContracts,
            ConstructFrameCopyAndMakeLeftHandedOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_from_point_measurement_probing_frames")]
    public override Task<Api.ConstructFrameFromPointMeasurementProbingFramesResult> ConstructFrameFromPointMeasurementProbingFrames(Api.ConstructFrameFromPointMeasurementProbingFramesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameFromPointMeasurementProbingFramesOperation.Descriptor,
            ConstructFrameFromPointMeasurementProbingFramesOperation.CreateCommand, ConstructFrameFromPointMeasurementProbingFramesOperation.OutputContracts,
            ConstructFrameFromPointMeasurementProbingFramesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_from_transform_in_world")]
    public override Task<Api.ConstructFrameFromTransformInWorldResult> ConstructFrameFromTransformInWorld(Api.ConstructFrameFromTransformInWorldRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameFromTransformInWorldOperation.Descriptor,
            ConstructFrameFromTransformInWorldOperation.CreateCommand, ConstructFrameFromTransformInWorldOperation.OutputContracts,
            ConstructFrameFromTransformInWorldOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_known_origin_object_direction_object_direction")]
    public override Task<Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionResult> ConstructFrameKnownOriginObjectDirectionObjectDirection(Api.ConstructFrameKnownOriginObjectDirectionObjectDirectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameKnownOriginObjectDirectionObjectDirectionOperation.Descriptor,
            ConstructFrameKnownOriginObjectDirectionObjectDirectionOperation.CreateCommand,
            ConstructFrameKnownOriginObjectDirectionObjectDirectionOperation.OutputContracts,
            ConstructFrameKnownOriginObjectDirectionObjectDirectionOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_on_instrument_base")]
    public override Task<Api.ConstructFrameOnInstrumentBaseResult> ConstructFrameOnInstrumentBase(Api.ConstructFrameOnInstrumentBaseRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameOnInstrumentBaseOperation.Descriptor,
            ConstructFrameOnInstrumentBaseOperation.CreateCommand, ConstructFrameOnInstrumentBaseOperation.OutputContracts,
            ConstructFrameOnInstrumentBaseOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_on_object")]
    public override Task<Api.ConstructFrameOnObjectResult> ConstructFrameOnObject(Api.ConstructFrameOnObjectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameOnObjectOperation.Descriptor,
            ConstructFrameOnObjectOperation.CreateCommand, ConstructFrameOnObjectOperation.OutputContracts,
            ConstructFrameOnObjectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_pick_origin_and_point_on_x_axis_clock_z_along_working_z")]
    public override Task<Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZResult> ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZ(Api.ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZOperation.Descriptor,
            ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZOperation.CreateCommand,
            ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZOperation.OutputContracts,
            ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frames_by_projecting_frames_on_mesh_along_frame_direction")]
    public override Task<Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionResult> ConstructFramesByProjectingFramesOnMeshAlongFrameDirection(Api.ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionOperation.Descriptor,
            ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionOperation.CreateCommand, ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionOperation.OutputContracts, ConstructFramesByProjectingFramesOnMeshAlongFrameDirectionOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frames_by_projecting_frames_on_mesh_along_reference_direction")]
    public override Task<Api.ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionResult> ConstructFramesByProjectingFramesOnMeshAlongReferenceDirection(Api.ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionOperation.Descriptor,
            ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionOperation.CreateCommand, ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionOperation.OutputContracts, ConstructFramesByProjectingFramesOnMeshAlongReferenceDirectionOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_three_planes")]
    public override Task<Api.ConstructFrameThreePlanesResult> ConstructFrameThreePlanes(Api.ConstructFrameThreePlanesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameThreePlanesOperation.Descriptor,
            ConstructFrameThreePlanesOperation.CreateCommand, ConstructFrameThreePlanesOperation.OutputContracts,
            ConstructFrameThreePlanesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_three_points")]
    public override Task<Api.ConstructFrameThreePointsResult> ConstructFrameThreePoints(Api.ConstructFrameThreePointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameThreePointsOperation.Descriptor,
            ConstructFrameThreePointsOperation.CreateCommand, ConstructFrameThreePointsOperation.OutputContracts,
            ConstructFrameThreePointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_frame_with_wizard")]
    public override Task<Api.ConstructFrameWithWizardResult> ConstructFrameWithWizard(Api.ConstructFrameWithWizardRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructFrameWithWizardOperation.Descriptor,
            ConstructFrameWithWizardOperation.CreateCommand, ConstructFrameWithWizardOperation.OutputContracts,
            ConstructFrameWithWizardOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_geometry_from_surfaces")]
    public override Task<Api.ConstructGeometryFromSurfacesResult> ConstructGeometryFromSurfaces(Api.ConstructGeometryFromSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructGeometryFromSurfacesOperation.Descriptor,
            ConstructGeometryFromSurfacesOperation.CreateCommand, ConstructGeometryFromSurfacesOperation.OutputContracts, ConstructGeometryFromSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_center_of_slot")]
    public override Task<Api.ConstructLineCenterOfSlotResult> ConstructLineCenterOfSlot(Api.ConstructLineCenterOfSlotRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineCenterOfSlotOperation.Descriptor,
            ConstructLineCenterOfSlotOperation.CreateCommand, ConstructLineCenterOfSlotOperation.OutputContracts, ConstructLineCenterOfSlotOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_from_instrument_shot")]
    public override Task<Api.ConstructLineFromInstrumentShotResult> ConstructLineFromInstrumentShot(Api.ConstructLineFromInstrumentShotRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineFromInstrumentShotOperation.Descriptor,
            ConstructLineFromInstrumentShotOperation.CreateCommand, ConstructLineFromInstrumentShotOperation.OutputContracts, ConstructLineFromInstrumentShotOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_normal_to_object")]
    public override Task<Api.ConstructLineNormalToObjectResult> ConstructLineNormalToObject(Api.ConstructLineNormalToObjectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineNormalToObjectOperation.Descriptor,
            ConstructLineNormalToObjectOperation.CreateCommand, ConstructLineNormalToObjectOperation.OutputContracts, ConstructLineNormalToObjectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_normal_to_object_through_point")]
    public override Task<Api.ConstructLineNormalToObjectThroughPointResult> ConstructLineNormalToObjectThroughPoint(Api.ConstructLineNormalToObjectThroughPointRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineNormalToObjectThroughPointOperation.Descriptor,
            ConstructLineNormalToObjectThroughPointOperation.CreateCommand, ConstructLineNormalToObjectThroughPointOperation.OutputContracts, ConstructLineNormalToObjectThroughPointOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_project_line_to_object_reference_plane")]
    public override Task<Api.ConstructLineProjectLineToObjectReferencePlaneResult> ConstructLineProjectLineToObjectReferencePlane(Api.ConstructLineProjectLineToObjectReferencePlaneRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineProjectLineToObjectReferencePlaneOperation.Descriptor,
            ConstructLineProjectLineToObjectReferencePlaneOperation.CreateCommand, ConstructLineProjectLineToObjectReferencePlaneOperation.OutputContracts, ConstructLineProjectLineToObjectReferencePlaneOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_lines_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructLinesFromSurfaceFacesRuntimeSelectResult> ConstructLinesFromSurfaceFacesRuntimeSelect(Api.ConstructLinesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLinesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructLinesFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructLinesFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructLinesFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_two_plane_intersection")]
    public override Task<Api.ConstructLineTwoPlaneIntersectionResult> ConstructLineTwoPlaneIntersection(Api.ConstructLineTwoPlaneIntersectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineTwoPlaneIntersectionOperation.Descriptor,
            ConstructLineTwoPlaneIntersectionOperation.CreateCommand, ConstructLineTwoPlaneIntersectionOperation.OutputContracts, ConstructLineTwoPlaneIntersectionOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_two_points")]
    public override Task<Api.ConstructLineTwoPointsResult> ConstructLineTwoPoints(Api.ConstructLineTwoPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineTwoPointsOperation.Descriptor,
            ConstructLineTwoPointsOperation.CreateCommand, ConstructLineTwoPointsOperation.OutputContracts, ConstructLineTwoPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_line_two_points_vector_notation")]
    public override Task<Api.ConstructLineTwoPointsVectorNotationResult> ConstructLineTwoPointsVectorNotation(Api.ConstructLineTwoPointsVectorNotationRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructLineTwoPointsVectorNotationOperation.Descriptor,
            ConstructLineTwoPointsVectorNotationOperation.CreateCommand, ConstructLineTwoPointsVectorNotationOperation.OutputContracts, ConstructLineTwoPointsVectorNotationOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_mirror_cube_frame")]
    public override Task<Api.ConstructMirrorCubeFrameResult> ConstructMirrorCubeFrame(Api.ConstructMirrorCubeFrameRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructMirrorCubeFrameOperation.Descriptor,
            ConstructMirrorCubeFrameOperation.CreateCommand, ConstructMirrorCubeFrameOperation.OutputContracts,
            ConstructMirrorCubeFrameOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_objects_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructObjectsFromSurfaceFacesRuntimeSelectResult> ConstructObjectsFromSurfaceFacesRuntimeSelect(Api.ConstructObjectsFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructObjectsFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructObjectsFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructObjectsFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructObjectsFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_perimeter_from_points")]
    public override Task<Api.ConstructPerimeterFromPointsResult> ConstructPerimeterFromPoints(Api.ConstructPerimeterFromPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPerimeterFromPointsOperation.Descriptor,
            ConstructPerimeterFromPointsOperation.CreateCommand, ConstructPerimeterFromPointsOperation.OutputContracts, ConstructPerimeterFromPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_plane")]
    public override Task<Api.ConstructPlaneResult> ConstructPlane(Api.ConstructPlaneRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPlaneOperation.Descriptor,
            ConstructPlaneOperation.CreateCommand, ConstructPlaneOperation.OutputContracts, ConstructPlaneOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_plane_normal_to_object_through_point")]
    public override Task<Api.ConstructPlaneNormalToObjectThroughPointResult> ConstructPlaneNormalToObjectThroughPoint(Api.ConstructPlaneNormalToObjectThroughPointRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPlaneNormalToObjectThroughPointOperation.Descriptor,
            ConstructPlaneNormalToObjectThroughPointOperation.CreateCommand, ConstructPlaneNormalToObjectThroughPointOperation.OutputContracts, ConstructPlaneNormalToObjectThroughPointOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_planes_bisect_two_planes")]
    public override Task<Api.ConstructPlanesBisectTwoPlanesResult> ConstructPlanesBisectTwoPlanes(Api.ConstructPlanesBisectTwoPlanesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPlanesBisectTwoPlanesOperation.Descriptor,
            ConstructPlanesBisectTwoPlanesOperation.CreateCommand, ConstructPlanesBisectTwoPlanesOperation.OutputContracts, ConstructPlanesBisectTwoPlanesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_planes_bounding_point_group")]
    public override Task<Api.ConstructPlanesBoundingPointGroupResult> ConstructPlanesBoundingPointGroup(Api.ConstructPlanesBoundingPointGroupRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPlanesBoundingPointGroupOperation.Descriptor,
            ConstructPlanesBoundingPointGroupOperation.CreateCommand, ConstructPlanesBoundingPointGroupOperation.OutputContracts, ConstructPlanesBoundingPointGroupOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_planes_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructPlanesFromSurfaceFacesRuntimeSelectResult> ConstructPlanesFromSurfaceFacesRuntimeSelect(Api.ConstructPlanesFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPlanesFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructPlanesFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructPlanesFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructPlanesFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_circle_center")]
    public override Task<Api.ConstructPointAtCircleCenterResult> ConstructPointAtCircleCenter(Api.ConstructPointAtCircleCenterRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtCircleCenterOperation.Descriptor,
            ConstructPointAtCircleCenterOperation.CreateCommand, ConstructPointAtCircleCenterOperation.OutputContracts, ConstructPointAtCircleCenterOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_b_spline_and_surfaces")]
    public override Task<Api.ConstructPointAtIntersectionOfBSplineAndSurfacesResult> ConstructPointAtIntersectionOfBSplineAndSurfaces(Api.ConstructPointAtIntersectionOfBSplineAndSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtIntersectionOfBSplineAndSurfacesOperation.Descriptor,
            ConstructPointAtIntersectionOfBSplineAndSurfacesOperation.CreateCommand, ConstructPointAtIntersectionOfBSplineAndSurfacesOperation.OutputContracts, ConstructPointAtIntersectionOfBSplineAndSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_plane_and_line")]
    public override Task<Api.ConstructPointAtIntersectionOfPlaneAndLineResult> ConstructPointAtIntersectionOfPlaneAndLine(Api.ConstructPointAtIntersectionOfPlaneAndLineRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtIntersectionOfPlaneAndLineOperation.Descriptor,
            ConstructPointAtIntersectionOfPlaneAndLineOperation.CreateCommand, ConstructPointAtIntersectionOfPlaneAndLineOperation.OutputContracts, ConstructPointAtIntersectionOfPlaneAndLineOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_planes")]
    public override Task<Api.ConstructPointAtIntersectionOfPlanesResult> ConstructPointAtIntersectionOfPlanes(Api.ConstructPointAtIntersectionOfPlanesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtIntersectionOfPlanesOperation.Descriptor,
            ConstructPointAtIntersectionOfPlanesOperation.CreateCommand, ConstructPointAtIntersectionOfPlanesOperation.OutputContracts, ConstructPointAtIntersectionOfPlanesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_two_b_splines")]
    public override Task<Api.ConstructPointAtIntersectionOfTwoBSplinesResult> ConstructPointAtIntersectionOfTwoBSplines(Api.ConstructPointAtIntersectionOfTwoBSplinesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtIntersectionOfTwoBSplinesOperation.Descriptor,
            ConstructPointAtIntersectionOfTwoBSplinesOperation.CreateCommand, ConstructPointAtIntersectionOfTwoBSplinesOperation.OutputContracts, ConstructPointAtIntersectionOfTwoBSplinesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_intersection_of_two_lines")]
    public override Task<Api.ConstructPointAtIntersectionOfTwoLinesResult> ConstructPointAtIntersectionOfTwoLines(Api.ConstructPointAtIntersectionOfTwoLinesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtIntersectionOfTwoLinesOperation.Descriptor,
            ConstructPointAtIntersectionOfTwoLinesOperation.CreateCommand, ConstructPointAtIntersectionOfTwoLinesOperation.OutputContracts, ConstructPointAtIntersectionOfTwoLinesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_line_midpoint")]
    public override Task<Api.ConstructPointAtLineMidpointResult> ConstructPointAtLineMidpoint(Api.ConstructPointAtLineMidpointRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtLineMidpointOperation.Descriptor,
            ConstructPointAtLineMidpointOperation.CreateCommand, ConstructPointAtLineMidpointOperation.OutputContracts, ConstructPointAtLineMidpointOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_object_origin")]
    public override Task<Api.ConstructPointAtObjectOriginResult> ConstructPointAtObjectOrigin(Api.ConstructPointAtObjectOriginRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtObjectOriginOperation.Descriptor,
            ConstructPointAtObjectOriginOperation.CreateCommand, ConstructPointAtObjectOriginOperation.OutputContracts, ConstructPointAtObjectOriginOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_at_projection_of_point_onto_object")]
    public override Task<Api.ConstructPointAtProjectionOfPointOntoObjectResult> ConstructPointAtProjectionOfPointOntoObject(Api.ConstructPointAtProjectionOfPointOntoObjectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointAtProjectionOfPointOntoObjectOperation.Descriptor,
            ConstructPointAtProjectionOfPointOntoObjectOperation.CreateCommand, ConstructPointAtProjectionOfPointOntoObjectOperation.OutputContracts, ConstructPointAtProjectionOfPointOntoObjectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_cloud_from_existing_clouds")]
    public override Task<Api.ConstructPointCloudFromExistingCloudsResult> ConstructPointCloudFromExistingClouds(Api.ConstructPointCloudFromExistingCloudsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointCloudFromExistingCloudsOperation.Descriptor,
            ConstructPointCloudFromExistingCloudsOperation.CreateCommand, ConstructPointCloudFromExistingCloudsOperation.OutputContracts, ConstructPointCloudFromExistingCloudsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_cloud_from_visible_cloud_points")]
    public override Task<Api.ConstructPointCloudFromVisibleCloudPointsResult> ConstructPointCloudFromVisibleCloudPoints(Api.ConstructPointCloudFromVisibleCloudPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointCloudFromVisibleCloudPointsOperation.Descriptor,
            ConstructPointCloudFromVisibleCloudPointsOperation.CreateCommand, ConstructPointCloudFromVisibleCloudPointsOperation.OutputContracts, ConstructPointCloudFromVisibleCloudPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_cloud_limiting_probing_directions")]
    public override Task<Api.ConstructPointCloudLimitingProbingDirectionsResult> ConstructPointCloudLimitingProbingDirections(Api.ConstructPointCloudLimitingProbingDirectionsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointCloudLimitingProbingDirectionsOperation.Descriptor,
            ConstructPointCloudLimitingProbingDirectionsOperation.CreateCommand, ConstructPointCloudLimitingProbingDirectionsOperation.OutputContracts, ConstructPointCloudLimitingProbingDirectionsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_clouds_from_existing_cloud_points_runtime_select")]
    public override Task<Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectResult> ConstructPointCloudsFromExistingCloudPointsRuntimeSelect(Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointCloudsFromExistingCloudPointsRuntimeSelectOperation.Descriptor,
            ConstructPointCloudsFromExistingCloudPointsRuntimeSelectOperation.CreateCommand, ConstructPointCloudsFromExistingCloudPointsRuntimeSelectOperation.OutputContracts, ConstructPointCloudsFromExistingCloudPointsRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_clouds_from_existing_clouds_uniform_spacing")]
    public override Task<Api.ConstructPointCloudsFromExistingCloudsUniformSpacingResult> ConstructPointCloudsFromExistingCloudsUniformSpacing(Api.ConstructPointCloudsFromExistingCloudsUniformSpacingRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointCloudsFromExistingCloudsUniformSpacingOperation.Descriptor,
            ConstructPointCloudsFromExistingCloudsUniformSpacingOperation.CreateCommand, ConstructPointCloudsFromExistingCloudsUniformSpacingOperation.OutputContracts, ConstructPointCloudsFromExistingCloudsUniformSpacingOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_clouds_from_existing_point_group")]
    public override Task<Api.ConstructPointCloudsFromExistingPointGroupResult> ConstructPointCloudsFromExistingPointGroup(Api.ConstructPointCloudsFromExistingPointGroupRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointCloudsFromExistingPointGroupOperation.Descriptor,
            ConstructPointCloudsFromExistingPointGroupOperation.CreateCommand, ConstructPointCloudsFromExistingPointGroupOperation.OutputContracts, ConstructPointCloudsFromExistingPointGroupOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_fit_to_points")]
    public override Task<Api.ConstructPointFitToPointsResult> ConstructPointFitToPoints(Api.ConstructPointFitToPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointFitToPointsOperation.Descriptor,
            ConstructPointFitToPointsOperation.CreateCommand, ConstructPointFitToPointsOperation.OutputContracts, ConstructPointFitToPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_from_cloud_point_runtime_select")]
    public override Task<Api.ConstructPointFromCloudPointRuntimeSelectResult> ConstructPointFromCloudPointRuntimeSelect(Api.ConstructPointFromCloudPointRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointFromCloudPointRuntimeSelectOperation.Descriptor,
            ConstructPointFromCloudPointRuntimeSelectOperation.CreateCommand, ConstructPointFromCloudPointRuntimeSelectOperation.OutputContracts, ConstructPointFromCloudPointRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_from_survey_target_center")]
    public override Task<Api.ConstructPointFromSurveyTargetCenterResult> ConstructPointFromSurveyTargetCenter(Api.ConstructPointFromSurveyTargetCenterRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointFromSurveyTargetCenterOperation.Descriptor,
            ConstructPointFromSurveyTargetCenterOperation.CreateCommand, ConstructPointFromSurveyTargetCenterOperation.OutputContracts, ConstructPointFromSurveyTargetCenterOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_group_from_point_cloud")]
    public override Task<Api.ConstructPointGroupFromPointCloudResult> ConstructPointGroupFromPointCloud(Api.ConstructPointGroupFromPointCloudRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointGroupFromPointCloudOperation.Descriptor,
            ConstructPointGroupFromPointCloudOperation.CreateCommand, ConstructPointGroupFromPointCloudOperation.OutputContracts, ConstructPointGroupFromPointCloudOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_group_from_point_name_ref_list")]
    public override Task<Api.ConstructPointGroupFromPointNameRefListResult> ConstructPointGroupFromPointNameRefList(Api.ConstructPointGroupFromPointNameRefListRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointGroupFromPointNameRefListOperation.Descriptor,
            ConstructPointGroupFromPointNameRefListOperation.CreateCommand, ConstructPointGroupFromPointNameRefListOperation.OutputContracts, ConstructPointGroupFromPointNameRefListOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_groups_from_vector_groups")]
    public override Task<Api.ConstructPointGroupsFromVectorGroupsResult> ConstructPointGroupsFromVectorGroups(Api.ConstructPointGroupsFromVectorGroupsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointGroupsFromVectorGroupsOperation.Descriptor,
            ConstructPointGroupsFromVectorGroupsOperation.CreateCommand, ConstructPointGroupsFromVectorGroupsOperation.OutputContracts, ConstructPointGroupsFromVectorGroupsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_point_in_working_coordinates")]
    public override Task<Api.ConstructPointInWorkingCoordinatesResult> ConstructPointInWorkingCoordinates(Api.ConstructPointInWorkingCoordinatesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointInWorkingCoordinatesOperation.Descriptor,
            ConstructPointInWorkingCoordinatesOperation.CreateCommand, ConstructPointInWorkingCoordinatesOperation.OutputContracts, ConstructPointInWorkingCoordinatesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_at_intersection_of_circle_and_line")]
    public override Task<Api.ConstructPointsAtIntersectionOfCircleAndLineResult> ConstructPointsAtIntersectionOfCircleAndLine(Api.ConstructPointsAtIntersectionOfCircleAndLineRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsAtIntersectionOfCircleAndLineOperation.Descriptor,
            ConstructPointsAtIntersectionOfCircleAndLineOperation.CreateCommand, ConstructPointsAtIntersectionOfCircleAndLineOperation.OutputContracts, ConstructPointsAtIntersectionOfCircleAndLineOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_at_intersection_of_principal_object_axes_and_surfaces")]
    public override Task<Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesResult> ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfaces(Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation.Descriptor,
            ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation.CreateCommand, ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation.OutputContracts, ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_at_projection_on_surfaces_parallel_to_wcf_axis")]
    public override Task<Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisResult> ConstructPointsAtProjectionOnSurfacesParallelToWcfAxis(Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisOperation.Descriptor,
            ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisOperation.CreateCommand, ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisOperation.OutputContracts, ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_at_projection_on_surfaces_radial_from_wcf_axis")]
    public override Task<Api.ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisResult> ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxis(Api.ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisOperation.Descriptor,
            ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisOperation.CreateCommand, ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisOperation.OutputContracts, ConstructPointsAtProjectionOnSurfacesRadialFromWcfAxisOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_at_projection_on_surfaces_spherical_from_wcf_origin")]
    public override Task<Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginResult> ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOrigin(Api.ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation.Descriptor,
            ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation.CreateCommand, ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation.OutputContracts, ConstructPointsAtProjectionOnSurfacesSphericalFromWcfOriginOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_auto_correspond_two_groups_inter_point_distance")]
    public override Task<Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceResult> ConstructPointsAutoCorrespondTwoGroupsInterPointDistance(Api.ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceOperation.Descriptor,
            ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceOperation.CreateCommand, ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceOperation.OutputContracts, ConstructPointsAutoCorrespondTwoGroupsInterPointDistanceOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_auto_correspond_two_groups_proximity")]
    public override Task<Api.ConstructPointsAutoCorrespondTwoGroupsProximityResult> ConstructPointsAutoCorrespondTwoGroupsProximity(Api.ConstructPointsAutoCorrespondTwoGroupsProximityRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsAutoCorrespondTwoGroupsProximityOperation.Descriptor,
            ConstructPointsAutoCorrespondTwoGroupsProximityOperation.CreateCommand, ConstructPointsAutoCorrespondTwoGroupsProximityOperation.OutputContracts, ConstructPointsAutoCorrespondTwoGroupsProximityOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_by_projecting_points_on_mesh_along_direction")]
    public override Task<Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionResult> ConstructPointsByProjectingPointsOnMeshAlongDirection(Api.ConstructPointsByProjectingPointsOnMeshAlongDirectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsByProjectingPointsOnMeshAlongDirectionOperation.Descriptor,
            ConstructPointsByProjectingPointsOnMeshAlongDirectionOperation.CreateCommand, ConstructPointsByProjectingPointsOnMeshAlongDirectionOperation.OutputContracts, ConstructPointsByProjectingPointsOnMeshAlongDirectionOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_cylindrically_shifted")]
    public override Task<Api.ConstructPointsCylindricallyShiftedResult> ConstructPointsCylindricallyShifted(Api.ConstructPointsCylindricallyShiftedRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsCylindricallyShiftedOperation.Descriptor,
            ConstructPointsCylindricallyShiftedOperation.CreateCommand, ConstructPointsCylindricallyShiftedOperation.OutputContracts, ConstructPointsCylindricallyShiftedOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_from_cylinder")]
    public override Task<Api.ConstructPointsFromCylinderResult> ConstructPointsFromCylinder(Api.ConstructPointsFromCylinderRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsFromCylinderOperation.Descriptor,
            ConstructPointsFromCylinderOperation.CreateCommand, ConstructPointsFromCylinderOperation.OutputContracts, ConstructPointsFromCylinderOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructPointsFromSurfaceFacesRuntimeSelectResult> ConstructPointsFromSurfaceFacesRuntimeSelect(Api.ConstructPointsFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructPointsFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructPointsFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructPointsFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_from_surfaces_on_uv_grid")]
    public override Task<Api.ConstructPointsFromSurfacesOnUvGridResult> ConstructPointsFromSurfacesOnUvGrid(Api.ConstructPointsFromSurfacesOnUvGridRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsFromSurfacesOnUvGridOperation.Descriptor,
            ConstructPointsFromSurfacesOnUvGridOperation.CreateCommand, ConstructPointsFromSurfacesOnUvGridOperation.OutputContracts, ConstructPointsFromSurfacesOnUvGridOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_layout_on_grid")]
    public override Task<Api.ConstructPointsLayoutOnGridResult> ConstructPointsLayoutOnGrid(Api.ConstructPointsLayoutOnGridRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsLayoutOnGridOperation.Descriptor,
            ConstructPointsLayoutOnGridOperation.CreateCommand, ConstructPointsLayoutOnGridOperation.OutputContracts, ConstructPointsLayoutOnGridOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_n_spaced_on_curves")]
    public override Task<Api.ConstructPointsNSpacedOnCurvesResult> ConstructPointsNSpacedOnCurves(Api.ConstructPointsNSpacedOnCurvesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsNSpacedOnCurvesOperation.Descriptor,
            ConstructPointsNSpacedOnCurvesOperation.CreateCommand, ConstructPointsNSpacedOnCurvesOperation.OutputContracts, ConstructPointsNSpacedOnCurvesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_on_curves_using_max_chordal_deviation")]
    public override Task<Api.ConstructPointsOnCurvesUsingMaxChordalDeviationResult> ConstructPointsOnCurvesUsingMaxChordalDeviation(Api.ConstructPointsOnCurvesUsingMaxChordalDeviationRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsOnCurvesUsingMaxChordalDeviationOperation.Descriptor,
            ConstructPointsOnCurvesUsingMaxChordalDeviationOperation.CreateCommand, ConstructPointsOnCurvesUsingMaxChordalDeviationOperation.OutputContracts, ConstructPointsOnCurvesUsingMaxChordalDeviationOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_on_object_vertices")]
    public override Task<Api.ConstructPointsOnObjectVerticesResult> ConstructPointsOnObjectVertices(Api.ConstructPointsOnObjectVerticesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsOnObjectVerticesOperation.Descriptor,
            ConstructPointsOnObjectVerticesOperation.CreateCommand, ConstructPointsOnObjectVerticesOperation.OutputContracts, ConstructPointsOnObjectVerticesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_on_surfaces_by_clicking")]
    public override Task<Api.ConstructPointsOnSurfacesByClickingResult> ConstructPointsOnSurfacesByClicking(Api.ConstructPointsOnSurfacesByClickingRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsOnSurfacesByClickingOperation.Descriptor,
            ConstructPointsOnSurfacesByClickingOperation.CreateCommand, ConstructPointsOnSurfacesByClickingOperation.OutputContracts, ConstructPointsOnSurfacesByClickingOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_shifted_in_working_frame")]
    public override Task<Api.ConstructPointsShiftedInWorkingFrameResult> ConstructPointsShiftedInWorkingFrame(Api.ConstructPointsShiftedInWorkingFrameRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsShiftedInWorkingFrameOperation.Descriptor,
            ConstructPointsShiftedInWorkingFrameOperation.CreateCommand, ConstructPointsShiftedInWorkingFrameOperation.OutputContracts, ConstructPointsShiftedInWorkingFrameOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_spaced_at_distance_on_curves")]
    public override Task<Api.ConstructPointsSpacedAtDistanceOnCurvesResult> ConstructPointsSpacedAtDistanceOnCurves(Api.ConstructPointsSpacedAtDistanceOnCurvesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsSpacedAtDistanceOnCurvesOperation.Descriptor,
            ConstructPointsSpacedAtDistanceOnCurvesOperation.CreateCommand, ConstructPointsSpacedAtDistanceOnCurvesOperation.OutputContracts, ConstructPointsSpacedAtDistanceOnCurvesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_subset_with_greatest_spacing")]
    public override Task<Api.ConstructPointsSubsetWithGreatestSpacingResult> ConstructPointsSubsetWithGreatestSpacing(Api.ConstructPointsSubsetWithGreatestSpacingRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsSubsetWithGreatestSpacingOperation.Descriptor,
            ConstructPointsSubsetWithGreatestSpacingOperation.CreateCommand, ConstructPointsSubsetWithGreatestSpacingOperation.OutputContracts, ConstructPointsSubsetWithGreatestSpacingOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_points_wildcard_selection")]
    public override Task<Api.ConstructPointsWildcardSelectionResult> ConstructPointsWildcardSelection(Api.ConstructPointsWildcardSelectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPointsWildcardSelectionOperation.Descriptor,
            ConstructPointsWildcardSelectionOperation.CreateCommand, ConstructPointsWildcardSelectionOperation.OutputContracts, ConstructPointsWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_polygonized_surface_from_point_clouds")]
    public override Task<Api.ConstructPolygonizedSurfaceFromPointCloudsResult> ConstructPolygonizedSurfaceFromPointClouds(Api.ConstructPolygonizedSurfaceFromPointCloudsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructPolygonizedSurfaceFromPointCloudsOperation.Descriptor,
            ConstructPolygonizedSurfaceFromPointCloudsOperation.CreateCommand, ConstructPolygonizedSurfaceFromPointCloudsOperation.OutputContracts, ConstructPolygonizedSurfaceFromPointCloudsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_scale_bar")]
    public override Task<Api.ConstructScaleBarResult> ConstructScaleBar(Api.ConstructScaleBarRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructScaleBarOperation.Descriptor,
            ConstructScaleBarOperation.CreateCommand, ConstructScaleBarOperation.OutputContracts, ConstructScaleBarOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_sphere")]
    public override Task<Api.ConstructSphereResult> ConstructSphere(Api.ConstructSphereRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSphereOperation.Descriptor,
            ConstructSphereOperation.CreateCommand, ConstructSphereOperation.OutputContracts, ConstructSphereOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_spheres_from_surface_faces_runtime_select")]
    public override Task<Api.ConstructSpheresFromSurfaceFacesRuntimeSelectResult> ConstructSpheresFromSurfaceFacesRuntimeSelect(Api.ConstructSpheresFromSurfaceFacesRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSpheresFromSurfaceFacesRuntimeSelectOperation.Descriptor,
            ConstructSpheresFromSurfaceFacesRuntimeSelectOperation.CreateCommand, ConstructSpheresFromSurfaceFacesRuntimeSelectOperation.OutputContracts, ConstructSpheresFromSurfaceFacesRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_by_dissecting_surfaces")]
    public override Task<Api.ConstructSurfaceByDissectingSurfacesResult> ConstructSurfaceByDissectingSurfaces(Api.ConstructSurfaceByDissectingSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceByDissectingSurfacesOperation.Descriptor,
            ConstructSurfaceByDissectingSurfacesOperation.CreateCommand, ConstructSurfaceByDissectingSurfacesOperation.OutputContracts, ConstructSurfaceByDissectingSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_by_offsetting_surface")]
    public override Task<Api.ConstructSurfaceByOffsettingSurfaceResult> ConstructSurfaceByOffsettingSurface(Api.ConstructSurfaceByOffsettingSurfaceRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceByOffsettingSurfaceOperation.Descriptor,
            ConstructSurfaceByOffsettingSurfaceOperation.CreateCommand, ConstructSurfaceByOffsettingSurfaceOperation.OutputContracts, ConstructSurfaceByOffsettingSurfaceOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_fit_from_nominal_surfaces_and_actual_data")]
    public override Task<Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataResult> ConstructSurfaceFitFromNominalSurfacesAndActualData(Api.ConstructSurfaceFitFromNominalSurfacesAndActualDataRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFitFromNominalSurfacesAndActualDataOperation.Descriptor,
            ConstructSurfaceFitFromNominalSurfacesAndActualDataOperation.CreateCommand, ConstructSurfaceFitFromNominalSurfacesAndActualDataOperation.OutputContracts, ConstructSurfaceFitFromNominalSurfacesAndActualDataOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_annotation_links")]
    public override Task<Api.ConstructSurfaceFromAnnotationLinksResult> ConstructSurfaceFromAnnotationLinks(Api.ConstructSurfaceFromAnnotationLinksRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromAnnotationLinksOperation.Descriptor,
            ConstructSurfaceFromAnnotationLinksOperation.CreateCommand, ConstructSurfaceFromAnnotationLinksOperation.OutputContracts, ConstructSurfaceFromAnnotationLinksOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_b_splines")]
    public override Task<Api.ConstructSurfaceFromBSplinesResult> ConstructSurfaceFromBSplines(Api.ConstructSurfaceFromBSplinesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromBSplinesOperation.Descriptor,
            ConstructSurfaceFromBSplinesOperation.CreateCommand, ConstructSurfaceFromBSplinesOperation.OutputContracts, ConstructSurfaceFromBSplinesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_collection_of_surfaces")]
    public override Task<Api.ConstructSurfaceFromCollectionOfSurfacesResult> ConstructSurfaceFromCollectionOfSurfaces(Api.ConstructSurfaceFromCollectionOfSurfacesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromCollectionOfSurfacesOperation.Descriptor,
            ConstructSurfaceFromCollectionOfSurfacesOperation.CreateCommand, ConstructSurfaceFromCollectionOfSurfacesOperation.OutputContracts, ConstructSurfaceFromCollectionOfSurfacesOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_cone")]
    public override Task<Api.ConstructSurfaceFromConeResult> ConstructSurfaceFromCone(Api.ConstructSurfaceFromConeRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromConeOperation.Descriptor,
            ConstructSurfaceFromConeOperation.CreateCommand, ConstructSurfaceFromConeOperation.OutputContracts, ConstructSurfaceFromConeOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_cylinder")]
    public override Task<Api.ConstructSurfaceFromCylinderResult> ConstructSurfaceFromCylinder(Api.ConstructSurfaceFromCylinderRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromCylinderOperation.Descriptor,
            ConstructSurfaceFromCylinderOperation.CreateCommand, ConstructSurfaceFromCylinderOperation.OutputContracts, ConstructSurfaceFromCylinderOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_plane")]
    public override Task<Api.ConstructSurfaceFromPlaneResult> ConstructSurfaceFromPlane(Api.ConstructSurfaceFromPlaneRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromPlaneOperation.Descriptor,
            ConstructSurfaceFromPlaneOperation.CreateCommand, ConstructSurfaceFromPlaneOperation.OutputContracts, ConstructSurfaceFromPlaneOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_point_groups")]
    public override Task<Api.ConstructSurfaceFromPointGroupsResult> ConstructSurfaceFromPointGroups(Api.ConstructSurfaceFromPointGroupsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromPointGroupsOperation.Descriptor,
            ConstructSurfaceFromPointGroupsOperation.CreateCommand, ConstructSurfaceFromPointGroupsOperation.OutputContracts, ConstructSurfaceFromPointGroupsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surface_from_sphere")]
    public override Task<Api.ConstructSurfaceFromSphereResult> ConstructSurfaceFromSphere(Api.ConstructSurfaceFromSphereRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfaceFromSphereOperation.Descriptor,
            ConstructSurfaceFromSphereOperation.CreateCommand, ConstructSurfaceFromSphereOperation.OutputContracts, ConstructSurfaceFromSphereOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surfaces_by_dissecting_surfaces_from_ref_list")]
    public override Task<Api.ConstructSurfacesByDissectingSurfacesFromRefListResult> ConstructSurfacesByDissectingSurfacesFromRefList(Api.ConstructSurfacesByDissectingSurfacesFromRefListRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfacesByDissectingSurfacesFromRefListOperation.Descriptor,
            ConstructSurfacesByDissectingSurfacesFromRefListOperation.CreateCommand, ConstructSurfacesByDissectingSurfacesFromRefListOperation.OutputContracts,
            ConstructSurfacesByDissectingSurfacesFromRefListOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surfaces_by_projecting_points")]
    public override Task<Api.ConstructSurfacesByProjectingPointsResult> ConstructSurfacesByProjectingPoints(Api.ConstructSurfacesByProjectingPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfacesByProjectingPointsOperation.Descriptor,
            ConstructSurfacesByProjectingPointsOperation.CreateCommand, ConstructSurfacesByProjectingPointsOperation.OutputContracts, ConstructSurfacesByProjectingPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_surfaces_from_objects")]
    public override Task<Api.ConstructSurfacesFromObjectsResult> ConstructSurfacesFromObjects(Api.ConstructSurfacesFromObjectsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructSurfacesFromObjectsOperation.Descriptor,
            ConstructSurfacesFromObjectsOperation.CreateCommand, ConstructSurfacesFromObjectsOperation.OutputContracts, ConstructSurfacesFromObjectsOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_vector_group_area_profile_check")]
    public override Task<Api.ConstructVectorGroupAreaProfileCheckResult> ConstructVectorGroupAreaProfileCheck(Api.ConstructVectorGroupAreaProfileCheckRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructVectorGroupAreaProfileCheckOperation.Descriptor,
            ConstructVectorGroupAreaProfileCheckOperation.CreateCommand, ConstructVectorGroupAreaProfileCheckOperation.OutputContracts, ConstructVectorGroupAreaProfileCheckOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_vector_group_from_relationship")]
    public override Task<Api.ConstructVectorGroupFromRelationshipResult> ConstructVectorGroupFromRelationship(Api.ConstructVectorGroupFromRelationshipRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructVectorGroupFromRelationshipOperation.Descriptor,
            ConstructVectorGroupFromRelationshipOperation.CreateCommand, ConstructVectorGroupFromRelationshipOperation.OutputContracts, ConstructVectorGroupFromRelationshipOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_vector_group_from_vector_name_ref_list")]
    public override Task<Api.ConstructVectorGroupFromVectorNameRefListResult> ConstructVectorGroupFromVectorNameRefList(Api.ConstructVectorGroupFromVectorNameRefListRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructVectorGroupFromVectorNameRefListOperation.Descriptor,
            ConstructVectorGroupFromVectorNameRefListOperation.CreateCommand, ConstructVectorGroupFromVectorNameRefListOperation.OutputContracts,
            ConstructVectorGroupFromVectorNameRefListOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_vector_group_group_to_group_compare")]
    public override Task<Api.ConstructVectorGroupGroupToGroupCompareResult> ConstructVectorGroupGroupToGroupCompare(Api.ConstructVectorGroupGroupToGroupCompareRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructVectorGroupGroupToGroupCompareOperation.Descriptor,
            ConstructVectorGroupGroupToGroupCompareOperation.CreateCommand, ConstructVectorGroupGroupToGroupCompareOperation.OutputContracts, ConstructVectorGroupGroupToGroupCompareOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_vector_in_working_coordinates_begin_delta")]
    public override Task<Api.ConstructVectorInWorkingCoordinatesBeginDeltaResult> ConstructVectorInWorkingCoordinatesBeginDelta(Api.ConstructVectorInWorkingCoordinatesBeginDeltaRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructVectorInWorkingCoordinatesBeginDeltaOperation.Descriptor,
            ConstructVectorInWorkingCoordinatesBeginDeltaOperation.CreateCommand, ConstructVectorInWorkingCoordinatesBeginDeltaOperation.OutputContracts, ConstructVectorInWorkingCoordinatesBeginDeltaOperation.CreateResult);

    [OperationImplementation("construction_operations.construct_vector_in_working_coordinates_begin_direction_magnitude")]
    public override Task<Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeResult> ConstructVectorInWorkingCoordinatesBeginDirectionMagnitude(Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeOperation.Descriptor,
            ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeOperation.CreateCommand, ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeOperation.OutputContracts, ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeOperation.CreateResult);

    [OperationImplementation("construction_operations.copy_groups_excluding_obscured_points")]
    public override Task<Api.CopyGroupsExcludingObscuredPointsResult> CopyGroupsExcludingObscuredPoints(Api.CopyGroupsExcludingObscuredPointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CopyGroupsExcludingObscuredPointsOperation.Descriptor,
            CopyGroupsExcludingObscuredPointsOperation.CreateCommand, CopyGroupsExcludingObscuredPointsOperation.OutputContracts, CopyGroupsExcludingObscuredPointsOperation.CreateResult);

    [OperationImplementation("construction_operations.copy_object")]
    public override Task<Api.CopyObjectResult> CopyObject(Api.CopyObjectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CopyObjectOperation.Descriptor,
            CopyObjectOperation.CreateCommand, CopyObjectOperation.OutputContracts, CopyObjectOperation.CreateResult);

    [OperationImplementation("construction_operations.copy_objects_point_to_point_delta")]
    public override Task<Api.CopyObjectsPointToPointDeltaResult> CopyObjectsPointToPointDelta(Api.CopyObjectsPointToPointDeltaRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CopyObjectsPointToPointDeltaOperation.Descriptor,
            CopyObjectsPointToPointDeltaOperation.CreateCommand, CopyObjectsPointToPointDeltaOperation.OutputContracts, CopyObjectsPointToPointDeltaOperation.CreateResult);

    [OperationImplementation("construction_operations.copy_objects_to_a_collection")]
    public override Task<Api.CopyObjectsToACollectionResult> CopyObjectsToACollection(Api.CopyObjectsToACollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CopyObjectsToACollectionOperation.Descriptor,
            CopyObjectsToACollectionOperation.CreateCommand, CopyObjectsToACollectionOperation.OutputContracts, CopyObjectsToACollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.create_hidden_point")]
    public override Task<Api.CreateHiddenPointResult> CreateHiddenPoint(Api.CreateHiddenPointRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreateHiddenPointOperation.Descriptor,
            CreateHiddenPointOperation.CreateCommand, CreateHiddenPointOperation.OutputContracts, CreateHiddenPointOperation.CreateResult);

    [OperationImplementation("construction_operations.create_hidden_point_rod")]
    public override Task<Api.CreateHiddenPointRodResult> CreateHiddenPointRod(Api.CreateHiddenPointRodRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreateHiddenPointRodOperation.Descriptor,
            CreateHiddenPointRodOperation.CreateCommand, CreateHiddenPointRodOperation.OutputContracts, CreateHiddenPointRodOperation.CreateResult);

    [OperationImplementation("construction_operations.create_min_max_vector_group_callout")]
    public override Task<Api.CreateMinMaxVectorGroupCalloutResult> CreateMinMaxVectorGroupCallout(Api.CreateMinMaxVectorGroupCalloutRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreateMinMaxVectorGroupCalloutOperation.Descriptor,
            CreateMinMaxVectorGroupCalloutOperation.CreateCommand, CreateMinMaxVectorGroupCalloutOperation.OutputContracts, CreateMinMaxVectorGroupCalloutOperation.CreateResult);

    [OperationImplementation("construction_operations.create_picture_callout")]
    public override Task<Api.CreatePictureCalloutResult> CreatePictureCallout(Api.CreatePictureCalloutRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreatePictureCalloutOperation.Descriptor,
            CreatePictureCalloutOperation.CreateCommand, CreatePictureCalloutOperation.OutputContracts, CreatePictureCalloutOperation.CreateResult);

    [OperationImplementation("construction_operations.create_point_callout")]
    public override Task<Api.CreatePointCalloutResult> CreatePointCallout(Api.CreatePointCalloutRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreatePointCalloutOperation.Descriptor,
            CreatePointCalloutOperation.CreateCommand, CreatePointCalloutOperation.OutputContracts, CreatePointCalloutOperation.CreateResult);

    [OperationImplementation("construction_operations.create_point_comparison_callout")]
    public override Task<Api.CreatePointComparisonCalloutResult> CreatePointComparisonCallout(Api.CreatePointComparisonCalloutRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreatePointComparisonCalloutOperation.Descriptor,
            CreatePointComparisonCalloutOperation.CreateCommand, CreatePointComparisonCalloutOperation.OutputContracts, CreatePointComparisonCalloutOperation.CreateResult);

    [OperationImplementation("construction_operations.create_relationship_callout")]
    public override Task<Api.CreateRelationshipCalloutResult> CreateRelationshipCallout(Api.CreateRelationshipCalloutRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreateRelationshipCalloutOperation.Descriptor,
            CreateRelationshipCalloutOperation.CreateCommand, CreateRelationshipCalloutOperation.OutputContracts, CreateRelationshipCalloutOperation.CreateResult);

    [OperationImplementation("construction_operations.create_text_callout")]
    public override Task<Api.CreateTextCalloutResult> CreateTextCallout(Api.CreateTextCalloutRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreateTextCalloutOperation.Descriptor,
            CreateTextCalloutOperation.CreateCommand, CreateTextCalloutOperation.OutputContracts, CreateTextCalloutOperation.CreateResult);

    [OperationImplementation("construction_operations.create_vector_callout")]
    public override Task<Api.CreateVectorCalloutResult> CreateVectorCallout(Api.CreateVectorCalloutRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, CreateVectorCalloutOperation.Descriptor,
            CreateVectorCalloutOperation.CreateCommand, CreateVectorCalloutOperation.OutputContracts, CreateVectorCalloutOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_xyz")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerXyzResult> DecomposeTransformIntoDoublesEulerXyz(Api.DecomposeTransformIntoDoublesEulerXyzRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeTransformIntoDoublesEulerXyzOperation.Descriptor,
            DecomposeTransformIntoDoublesEulerXyzOperation.CreateCommand, DecomposeTransformIntoDoublesEulerXyzOperation.OutputContracts,
            DecomposeTransformIntoDoublesEulerXyzOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_zxz")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerZxzResult> DecomposeTransformIntoDoublesEulerZxz(Api.DecomposeTransformIntoDoublesEulerZxzRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeTransformIntoDoublesEulerZxzOperation.Descriptor,
            DecomposeTransformIntoDoublesEulerZxzOperation.CreateCommand, DecomposeTransformIntoDoublesEulerZxzOperation.OutputContracts,
            DecomposeTransformIntoDoublesEulerZxzOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_zyx")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerZyxResult> DecomposeTransformIntoDoublesEulerZyx(Api.DecomposeTransformIntoDoublesEulerZyxRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeTransformIntoDoublesEulerZyxOperation.Descriptor,
            DecomposeTransformIntoDoublesEulerZyxOperation.CreateCommand, DecomposeTransformIntoDoublesEulerZyxOperation.OutputContracts,
            DecomposeTransformIntoDoublesEulerZyxOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_euler_zyz")]
    public override Task<Api.DecomposeTransformIntoDoublesEulerZyzResult> DecomposeTransformIntoDoublesEulerZyz(Api.DecomposeTransformIntoDoublesEulerZyzRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeTransformIntoDoublesEulerZyzOperation.Descriptor,
            DecomposeTransformIntoDoublesEulerZyzOperation.CreateCommand, DecomposeTransformIntoDoublesEulerZyzOperation.OutputContracts,
            DecomposeTransformIntoDoublesEulerZyzOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_transform_into_doubles_fixed_xyz")]
    public override Task<Api.DecomposeTransformIntoDoublesFixedXyzResult> DecomposeTransformIntoDoublesFixedXyz(Api.DecomposeTransformIntoDoublesFixedXyzRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeTransformIntoDoublesFixedXyzOperation.Descriptor,
            DecomposeTransformIntoDoublesFixedXyzOperation.CreateCommand, DecomposeTransformIntoDoublesFixedXyzOperation.OutputContracts,
            DecomposeTransformIntoDoublesFixedXyzOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_transform_into_vectors_fixed_xyz")]
    public override Task<Api.DecomposeTransformIntoVectorsFixedXyzResult> DecomposeTransformIntoVectorsFixedXyz(Api.DecomposeTransformIntoVectorsFixedXyzRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeTransformIntoVectorsFixedXyzOperation.Descriptor,
            DecomposeTransformIntoVectorsFixedXyzOperation.CreateCommand, DecomposeTransformIntoVectorsFixedXyzOperation.OutputContracts,
            DecomposeTransformIntoVectorsFixedXyzOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_transform_into_vectors_origin_and_axes")]
    public override Task<Api.DecomposeTransformIntoVectorsOriginAndAxesResult> DecomposeTransformIntoVectorsOriginAndAxes(Api.DecomposeTransformIntoVectorsOriginAndAxesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeTransformIntoVectorsOriginAndAxesOperation.Descriptor,
            DecomposeTransformIntoVectorsOriginAndAxesOperation.CreateCommand, DecomposeTransformIntoVectorsOriginAndAxesOperation.OutputContracts,
            DecomposeTransformIntoVectorsOriginAndAxesOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_world_transform_operator_into_doubles_fixed_xyz_in_world")]
    public override Task<Api.DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldResult> DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorld(Api.DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldOperation.Descriptor,
            DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldOperation.CreateCommand,
            DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldOperation.OutputContracts,
            DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorldOperation.CreateResult);

    [OperationImplementation("construction_operations.decompose_world_transform_operator_into_vectors_fixed_xyz_in_world")]
    public override Task<Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldResult> DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorld(Api.DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldOperation.Descriptor,
            DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldOperation.CreateCommand,
            DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldOperation.OutputContracts,
            DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorldOperation.CreateResult);

    [OperationImplementation("construction_operations.delete_callout_view")]
    public override Task<Api.DeleteCalloutViewResult> DeleteCalloutView(Api.DeleteCalloutViewRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DeleteCalloutViewOperation.Descriptor,
            DeleteCalloutViewOperation.CreateCommand, DeleteCalloutViewOperation.OutputContracts, DeleteCalloutViewOperation.CreateResult);

    [OperationImplementation("construction_operations.delete_collection")]
    public override Task<Api.DeleteCollectionResult> DeleteCollection(Api.DeleteCollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DeleteCollectionOperation.Descriptor,
            DeleteCollectionOperation.CreateCommand, DeleteCollectionOperation.OutputContracts, DeleteCollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.delete_collections_by_wildcard")]
    public override Task<Api.DeleteCollectionsByWildcardResult> DeleteCollectionsByWildcard(Api.DeleteCollectionsByWildcardRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DeleteCollectionsByWildcardOperation.Descriptor,
            DeleteCollectionsByWildcardOperation.CreateCommand, DeleteCollectionsByWildcardOperation.OutputContracts, DeleteCollectionsByWildcardOperation.CreateResult);

    [OperationImplementation("construction_operations.delete_folders_by_wildcard")]
    public override Task<Api.DeleteFoldersByWildcardResult> DeleteFoldersByWildcard(Api.DeleteFoldersByWildcardRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DeleteFoldersByWildcardOperation.Descriptor,
            DeleteFoldersByWildcardOperation.CreateCommand, DeleteFoldersByWildcardOperation.OutputContracts, DeleteFoldersByWildcardOperation.CreateResult);

    [OperationImplementation("construction_operations.delete_hidden_point_rod")]
    public override Task<Api.DeleteHiddenPointRodResult> DeleteHiddenPointRod(Api.DeleteHiddenPointRodRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DeleteHiddenPointRodOperation.Descriptor,
            DeleteHiddenPointRodOperation.CreateCommand, DeleteHiddenPointRodOperation.OutputContracts, DeleteHiddenPointRodOperation.CreateResult);

    [OperationImplementation("construction_operations.delete_points")]
    public override Task<Api.DeletePointsResult> DeletePoints(Api.DeletePointsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DeletePointsOperation.Descriptor,
            DeletePointsOperation.CreateCommand, DeletePointsOperation.OutputContracts, DeletePointsOperation.CreateResult);

    [OperationImplementation("construction_operations.delete_points_wildcard_selection")]
    public override Task<Api.DeletePointsWildcardSelectionResult> DeletePointsWildcardSelection(Api.DeletePointsWildcardSelectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, DeletePointsWildcardSelectionOperation.Descriptor,
            DeletePointsWildcardSelectionOperation.CreateCommand, DeletePointsWildcardSelectionOperation.OutputContracts, DeletePointsWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.extract_sphere_centers_from_point_cloud")]
    public override Task<Api.ExtractSphereCentersFromPointCloudResult> ExtractSphereCentersFromPointCloud(Api.ExtractSphereCentersFromPointCloudRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ExtractSphereCentersFromPointCloudOperation.Descriptor,
            ExtractSphereCentersFromPointCloudOperation.CreateCommand, ExtractSphereCentersFromPointCloudOperation.OutputContracts, ExtractSphereCentersFromPointCloudOperation.CreateResult);

    [OperationImplementation("construction_operations.get_collection_instrument_ref_list_variable")]
    public override Task<Api.GetCollectionInstrumentRefListVariableResult> GetCollectionInstrumentRefListVariable(Api.GetCollectionInstrumentRefListVariableRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, GetCollectionInstrumentRefListVariableOperation.Descriptor,
            GetCollectionInstrumentRefListVariableOperation.CreateCommand, GetCollectionInstrumentRefListVariableOperation.OutputContracts,
            GetCollectionInstrumentRefListVariableOperation.CreateResult);

    [OperationImplementation("construction_operations.get_gradient_at_projected_point_on_surface")]
    public override Task<Api.GetGradientAtProjectedPointOnSurfaceResult> GetGradientAtProjectedPointOnSurface(Api.GetGradientAtProjectedPointOnSurfaceRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, GetGradientAtProjectedPointOnSurfaceOperation.Descriptor,
            GetGradientAtProjectedPointOnSurfaceOperation.CreateCommand, GetGradientAtProjectedPointOnSurfaceOperation.OutputContracts, GetGradientAtProjectedPointOnSurfaceOperation.CreateResult);

    [OperationImplementation("construction_operations.get_gradient_at_projected_point_on_surface_edge")]
    public override Task<Api.GetGradientAtProjectedPointOnSurfaceEdgeResult> GetGradientAtProjectedPointOnSurfaceEdge(Api.GetGradientAtProjectedPointOnSurfaceEdgeRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, GetGradientAtProjectedPointOnSurfaceEdgeOperation.Descriptor,
            GetGradientAtProjectedPointOnSurfaceEdgeOperation.CreateCommand, GetGradientAtProjectedPointOnSurfaceEdgeOperation.OutputContracts, GetGradientAtProjectedPointOnSurfaceEdgeOperation.CreateResult);

    [OperationImplementation("construction_operations.get_hidden_point_rod_index_by_name")]
    public override Task<Api.GetHiddenPointRodIndexByNameResult> GetHiddenPointRodIndexByName(Api.GetHiddenPointRodIndexByNameRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, GetHiddenPointRodIndexByNameOperation.Descriptor,
            GetHiddenPointRodIndexByNameOperation.CreateCommand, GetHiddenPointRodIndexByNameOperation.OutputContracts, GetHiddenPointRodIndexByNameOperation.CreateResult);

    [OperationImplementation("construction_operations.get_ith_callout_position_in_callout_view")]
    public override Task<Api.GetIthCalloutPositionInCalloutViewResult> GetIthCalloutPositionInCalloutView(Api.GetIthCalloutPositionInCalloutViewRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, GetIthCalloutPositionInCalloutViewOperation.Descriptor,
            GetIthCalloutPositionInCalloutViewOperation.CreateCommand, GetIthCalloutPositionInCalloutViewOperation.OutputContracts, GetIthCalloutPositionInCalloutViewOperation.CreateResult);

    [OperationImplementation("construction_operations.get_number_of_callouts_in_callout_view")]
    public override Task<Api.GetNumberOfCalloutsInCalloutViewResult> GetNumberOfCalloutsInCalloutView(Api.GetNumberOfCalloutsInCalloutViewRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, GetNumberOfCalloutsInCalloutViewOperation.Descriptor,
            GetNumberOfCalloutsInCalloutViewOperation.CreateCommand, GetNumberOfCalloutsInCalloutViewOperation.OutputContracts, GetNumberOfCalloutsInCalloutViewOperation.CreateResult);

    [OperationImplementation("construction_operations.get_working_transform_of_object_fixed_xyz")]
    public override Task<Api.GetWorkingTransformOfObjectFixedXyzResult> GetWorkingTransformOfObjectFixedXyz(Api.GetWorkingTransformOfObjectFixedXyzRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, GetWorkingTransformOfObjectFixedXyzOperation.Descriptor,
            GetWorkingTransformOfObjectFixedXyzOperation.CreateCommand, GetWorkingTransformOfObjectFixedXyzOperation.OutputContracts,
            GetWorkingTransformOfObjectFixedXyzOperation.CreateResult);

    [OperationImplementation("construction_operations.invert_transform")]
    public override Task<Api.InvertTransformResult> InvertTransform(Api.InvertTransformRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, InvertTransformOperation.Descriptor,
            InvertTransformOperation.CreateCommand, InvertTransformOperation.OutputContracts, InvertTransformOperation.CreateResult);

    [OperationImplementation("construction_operations.make_callout_view_ref_list_wildcard_selection")]
    public override Task<Api.MakeCalloutViewRefListWildcardSelectionResult> MakeCalloutViewRefListWildcardSelection(Api.MakeCalloutViewRefListWildcardSelectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCalloutViewRefListWildcardSelectionOperation.Descriptor,
            MakeCalloutViewRefListWildcardSelectionOperation.CreateCommand, MakeCalloutViewRefListWildcardSelectionOperation.OutputContracts,
            MakeCalloutViewRefListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_instrument_id_runtime_select")]
    public override Task<Api.MakeCollectionInstrumentIdRuntimeSelectResult> MakeCollectionInstrumentIdRuntimeSelect(Api.MakeCollectionInstrumentIdRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionInstrumentIdRuntimeSelectOperation.Descriptor,
            MakeCollectionInstrumentIdRuntimeSelectOperation.CreateCommand, MakeCollectionInstrumentIdRuntimeSelectOperation.OutputContracts,
            MakeCollectionInstrumentIdRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_instrument_ref_list_runtime_select")]
    public override Task<Api.MakeCollectionInstrumentRefListRuntimeSelectResult> MakeCollectionInstrumentRefListRuntimeSelect(Api.MakeCollectionInstrumentRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionInstrumentRefListRuntimeSelectOperation.Descriptor,
            MakeCollectionInstrumentRefListRuntimeSelectOperation.CreateCommand, MakeCollectionInstrumentRefListRuntimeSelectOperation.OutputContracts,
            MakeCollectionInstrumentRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_item_name_ref_list_wildcard_selection")]
    public override Task<Api.MakeCollectionItemNameRefListWildcardSelectionResult> MakeCollectionItemNameRefListWildcardSelection(Api.MakeCollectionItemNameRefListWildcardSelectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionItemNameRefListWildcardSelectionOperation.Descriptor,
            MakeCollectionItemNameRefListWildcardSelectionOperation.CreateCommand,
            MakeCollectionItemNameRefListWildcardSelectionOperation.OutputContracts,
            MakeCollectionItemNameRefListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_name_runtime_select")]
    public override Task<Api.MakeCollectionNameRuntimeSelectResult> MakeCollectionNameRuntimeSelect(Api.MakeCollectionNameRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionNameRuntimeSelectOperation.Descriptor,
            MakeCollectionNameRuntimeSelectOperation.CreateCommand, MakeCollectionNameRuntimeSelectOperation.OutputContracts, MakeCollectionNameRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_object_name_ensure_unique")]
    public override Task<Api.MakeCollectionObjectNameEnsureUniqueResult> MakeCollectionObjectNameEnsureUnique(Api.MakeCollectionObjectNameEnsureUniqueRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionObjectNameEnsureUniqueOperation.Descriptor,
            MakeCollectionObjectNameEnsureUniqueOperation.CreateCommand, MakeCollectionObjectNameEnsureUniqueOperation.OutputContracts, MakeCollectionObjectNameEnsureUniqueOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_by_type")]
    public override Task<Api.MakeCollectionObjectNameRefListByTypeResult> MakeCollectionObjectNameRefListByType(Api.MakeCollectionObjectNameRefListByTypeRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionObjectNameRefListByTypeOperation.Descriptor,
            MakeCollectionObjectNameRefListByTypeOperation.CreateCommand, MakeCollectionObjectNameRefListByTypeOperation.OutputContracts, MakeCollectionObjectNameRefListByTypeOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_by_type_and_color")]
    public override Task<Api.MakeCollectionObjectNameRefListByTypeAndColorResult> MakeCollectionObjectNameRefListByTypeAndColor(Api.MakeCollectionObjectNameRefListByTypeAndColorRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionObjectNameRefListByTypeAndColorOperation.Descriptor,
            MakeCollectionObjectNameRefListByTypeAndColorOperation.CreateCommand, MakeCollectionObjectNameRefListByTypeAndColorOperation.OutputContracts, MakeCollectionObjectNameRefListByTypeAndColorOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_from_all_groups_in_collection")]
    public override Task<Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionResult> MakeCollectionObjectNameRefListFromAllGroupsInCollection(Api.MakeCollectionObjectNameRefListFromAllGroupsInCollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionObjectNameRefListFromAllGroupsInCollectionOperation.Descriptor,
            MakeCollectionObjectNameRefListFromAllGroupsInCollectionOperation.CreateCommand, MakeCollectionObjectNameRefListFromAllGroupsInCollectionOperation.OutputContracts, MakeCollectionObjectNameRefListFromAllGroupsInCollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_runtime_select")]
    public override Task<Api.MakeCollectionObjectNameRefListRuntimeSelectResult> MakeCollectionObjectNameRefListRuntimeSelect(Api.MakeCollectionObjectNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionObjectNameRefListRuntimeSelectOperation.Descriptor,
            MakeCollectionObjectNameRefListRuntimeSelectOperation.CreateCommand, MakeCollectionObjectNameRefListRuntimeSelectOperation.OutputContracts, MakeCollectionObjectNameRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_object_name_ref_list_wildcard_selection")]
    public override Task<Api.MakeCollectionObjectNameRefListWildcardSelectionResult> MakeCollectionObjectNameRefListWildcardSelection(Api.MakeCollectionObjectNameRefListWildcardSelectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionObjectNameRefListWildcardSelectionOperation.Descriptor,
            MakeCollectionObjectNameRefListWildcardSelectionOperation.CreateCommand, MakeCollectionObjectNameRefListWildcardSelectionOperation.OutputContracts, MakeCollectionObjectNameRefListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_object_name_runtime_select")]
    public override Task<Api.MakeCollectionObjectNameRuntimeSelectResult> MakeCollectionObjectNameRuntimeSelect(Api.MakeCollectionObjectNameRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionObjectNameRuntimeSelectOperation.Descriptor,
            MakeCollectionObjectNameRuntimeSelectOperation.CreateCommand, MakeCollectionObjectNameRuntimeSelectOperation.OutputContracts, MakeCollectionObjectNameRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_collection_vector_group_name_ref_list_runtime_select")]
    public override Task<Api.MakeCollectionVectorGroupNameRefListRuntimeSelectResult> MakeCollectionVectorGroupNameRefListRuntimeSelect(Api.MakeCollectionVectorGroupNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.Descriptor,
            MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.CreateCommand, MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.OutputContracts,
            MakeCollectionVectorGroupNameRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_event_ref_list_wildcard_selection")]
    public override Task<Api.MakeEventRefListWildcardSelectionResult> MakeEventRefListWildcardSelection(Api.MakeEventRefListWildcardSelectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeEventRefListWildcardSelectionOperation.Descriptor,
            MakeEventRefListWildcardSelectionOperation.CreateCommand, MakeEventRefListWildcardSelectionOperation.OutputContracts,
            MakeEventRefListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.make_picture_name_ref_list_runtime_select")]
    public override Task<Api.MakePictureNameRefListRuntimeSelectResult> MakePictureNameRefListRuntimeSelect(Api.MakePictureNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakePictureNameRefListRuntimeSelectOperation.Descriptor,
            MakePictureNameRefListRuntimeSelectOperation.CreateCommand, MakePictureNameRefListRuntimeSelectOperation.OutputContracts,
            MakePictureNameRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_point_name_ensure_unique")]
    public override Task<Api.MakePointNameEnsureUniqueResult> MakePointNameEnsureUnique(Api.MakePointNameEnsureUniqueRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakePointNameEnsureUniqueOperation.Descriptor,
            MakePointNameEnsureUniqueOperation.CreateCommand, MakePointNameEnsureUniqueOperation.OutputContracts, MakePointNameEnsureUniqueOperation.CreateResult);

    [OperationImplementation("construction_operations.make_point_name_ref_list_from_group")]
    public override Task<Api.MakePointNameRefListFromGroupResult> MakePointNameRefListFromGroup(Api.MakePointNameRefListFromGroupRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakePointNameRefListFromGroupOperation.Descriptor,
            MakePointNameRefListFromGroupOperation.CreateCommand, MakePointNameRefListFromGroupOperation.OutputContracts, MakePointNameRefListFromGroupOperation.CreateResult);

    [OperationImplementation("construction_operations.make_point_name_ref_list_runtime_select")]
    public override Task<Api.MakePointNameRefListRuntimeSelectResult> MakePointNameRefListRuntimeSelect(Api.MakePointNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakePointNameRefListRuntimeSelectOperation.Descriptor,
            MakePointNameRefListRuntimeSelectOperation.CreateCommand, MakePointNameRefListRuntimeSelectOperation.OutputContracts, MakePointNameRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_point_name_ref_list_wildcard_select")]
    public override Task<Api.MakePointNameRefListWildcardSelectResult> MakePointNameRefListWildcardSelect(Api.MakePointNameRefListWildcardSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakePointNameRefListWildcardSelectOperation.Descriptor,
            MakePointNameRefListWildcardSelectOperation.CreateCommand, MakePointNameRefListWildcardSelectOperation.OutputContracts, MakePointNameRefListWildcardSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_point_name_runtime_select")]
    public override Task<Api.MakePointNameRuntimeSelectResult> MakePointNameRuntimeSelect(Api.MakePointNameRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakePointNameRuntimeSelectOperation.Descriptor,
            MakePointNameRuntimeSelectOperation.CreateCommand, MakePointNameRuntimeSelectOperation.OutputContracts, MakePointNameRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_relationship_ref_list_runtime_select")]
    public override Task<Api.MakeRelationshipRefListRuntimeSelectResult> MakeRelationshipRefListRuntimeSelect(Api.MakeRelationshipRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeRelationshipRefListRuntimeSelectOperation.Descriptor,
            MakeRelationshipRefListRuntimeSelectOperation.CreateCommand, MakeRelationshipRefListRuntimeSelectOperation.OutputContracts,
            MakeRelationshipRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_relationship_ref_list_wildcard_selection")]
    public override Task<Api.MakeRelationshipRefListWildcardSelectionResult> MakeRelationshipRefListWildcardSelection(Api.MakeRelationshipRefListWildcardSelectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeRelationshipRefListWildcardSelectionOperation.Descriptor,
            MakeRelationshipRefListWildcardSelectionOperation.CreateCommand, MakeRelationshipRefListWildcardSelectionOperation.OutputContracts,
            MakeRelationshipRefListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("construction_operations.make_report_ref_list_from_collection")]
    public override Task<Api.MakeReportRefListFromCollectionResult> MakeReportRefListFromCollection(Api.MakeReportRefListFromCollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeReportRefListFromCollectionOperation.Descriptor,
            MakeReportRefListFromCollectionOperation.CreateCommand, MakeReportRefListFromCollectionOperation.OutputContracts,
            MakeReportRefListFromCollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.make_report_ref_list_runtime_select")]
    public override Task<Api.MakeReportRefListRuntimeSelectResult> MakeReportRefListRuntimeSelect(Api.MakeReportRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeReportRefListRuntimeSelectOperation.Descriptor,
            MakeReportRefListRuntimeSelectOperation.CreateCommand, MakeReportRefListRuntimeSelectOperation.OutputContracts,
            MakeReportRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_system_string")]
    public override Task<Api.MakeSystemStringResult> MakeSystemString(Api.MakeSystemStringRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeSystemStringOperation.Descriptor,
            MakeSystemStringOperation.CreateCommand, MakeSystemStringOperation.OutputContracts,
            MakeSystemStringOperation.CreateResult);

    [OperationImplementation("construction_operations.make_transform_from_doubles_euler_parameters")]
    public override Task<Api.MakeTransformFromDoublesEulerParametersResult> MakeTransformFromDoublesEulerParameters(Api.MakeTransformFromDoublesEulerParametersRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeTransformFromDoublesEulerParametersOperation.Descriptor,
            MakeTransformFromDoublesEulerParametersOperation.CreateCommand, MakeTransformFromDoublesEulerParametersOperation.OutputContracts,
            MakeTransformFromDoublesEulerParametersOperation.CreateResult);

    [OperationImplementation("construction_operations.make_transform_from_doubles_fixed_xyz")]
    public override Task<Api.MakeTransformFromDoublesFixedXyzResult> MakeTransformFromDoublesFixedXyz(Api.MakeTransformFromDoublesFixedXyzRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeTransformFromDoublesFixedXyzOperation.Descriptor,
            MakeTransformFromDoublesFixedXyzOperation.CreateCommand, MakeTransformFromDoublesFixedXyzOperation.OutputContracts,
            MakeTransformFromDoublesFixedXyzOperation.CreateResult);

    [OperationImplementation("construction_operations.make_vector_name_ref_list_from_vector_group")]
    public override Task<Api.MakeVectorNameRefListFromVectorGroupResult> MakeVectorNameRefListFromVectorGroup(Api.MakeVectorNameRefListFromVectorGroupRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeVectorNameRefListFromVectorGroupOperation.Descriptor,
            MakeVectorNameRefListFromVectorGroupOperation.CreateCommand, MakeVectorNameRefListFromVectorGroupOperation.OutputContracts,
            MakeVectorNameRefListFromVectorGroupOperation.CreateResult);

    [OperationImplementation("construction_operations.make_vector_name_ref_list_runtime_select")]
    public override Task<Api.MakeVectorNameRefListRuntimeSelectResult> MakeVectorNameRefListRuntimeSelect(Api.MakeVectorNameRefListRuntimeSelectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeVectorNameRefListRuntimeSelectOperation.Descriptor,
            MakeVectorNameRefListRuntimeSelectOperation.CreateCommand, MakeVectorNameRefListRuntimeSelectOperation.OutputContracts,
            MakeVectorNameRefListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("construction_operations.make_vector_names_unique_in_vector_group")]
    public override Task<Api.MakeVectorNamesUniqueInVectorGroupResult> MakeVectorNamesUniqueInVectorGroup(Api.MakeVectorNamesUniqueInVectorGroupRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MakeVectorNamesUniqueInVectorGroupOperation.Descriptor,
            MakeVectorNamesUniqueInVectorGroupOperation.CreateCommand, MakeVectorNamesUniqueInVectorGroupOperation.OutputContracts,
            MakeVectorNamesUniqueInVectorGroupOperation.CreateResult);

    [OperationImplementation("construction_operations.mirror_objects")]
    public override Task<Api.MirrorObjectsResult> MirrorObjects(Api.MirrorObjectsRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MirrorObjectsOperation.Descriptor,
            MirrorObjectsOperation.CreateCommand, MirrorObjectsOperation.OutputContracts, MirrorObjectsOperation.CreateResult);

    [OperationImplementation("construction_operations.move_objects_point_to_point_delta")]
    public override Task<Api.MoveObjectsPointToPointDeltaResult> MoveObjectsPointToPointDelta(Api.MoveObjectsPointToPointDeltaRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MoveObjectsPointToPointDeltaOperation.Descriptor,
            MoveObjectsPointToPointDeltaOperation.CreateCommand, MoveObjectsPointToPointDeltaOperation.OutputContracts, MoveObjectsPointToPointDeltaOperation.CreateResult);

    [OperationImplementation("construction_operations.move_objects_to_a_collection")]
    public override Task<Api.MoveObjectsToACollectionResult> MoveObjectsToACollection(Api.MoveObjectsToACollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, MoveObjectsToACollectionOperation.Descriptor,
            MoveObjectsToACollectionOperation.CreateCommand, MoveObjectsToACollectionOperation.OutputContracts, MoveObjectsToACollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.rename_callout_view")]
    public override Task<Api.RenameCalloutViewResult> RenameCalloutView(Api.RenameCalloutViewRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, RenameCalloutViewOperation.Descriptor,
            RenameCalloutViewOperation.CreateCommand, RenameCalloutViewOperation.OutputContracts, RenameCalloutViewOperation.CreateResult);

    [OperationImplementation("construction_operations.rename_collection")]
    public override Task<Api.RenameCollectionResult> RenameCollection(Api.RenameCollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, RenameCollectionOperation.Descriptor,
            RenameCollectionOperation.CreateCommand, RenameCollectionOperation.OutputContracts, RenameCollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.rename_item")]
    public override Task<Api.RenameItemResult> RenameItem(Api.RenameItemRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, RenameItemOperation.Descriptor,
            RenameItemOperation.CreateCommand, RenameItemOperation.OutputContracts, RenameItemOperation.CreateResult);

    [OperationImplementation("construction_operations.rename_object")]
    public override Task<Api.RenameObjectResult> RenameObject(Api.RenameObjectRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, RenameObjectOperation.Descriptor,
            RenameObjectOperation.CreateCommand, RenameObjectOperation.OutputContracts, RenameObjectOperation.CreateResult);

    [OperationImplementation("construction_operations.rename_point")]
    public override Task<Api.RenamePointResult> RenamePoint(Api.RenamePointRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, RenamePointOperation.Descriptor,
            RenamePointOperation.CreateCommand, RenamePointOperation.OutputContracts, RenamePointOperation.CreateResult);

    [OperationImplementation("construction_operations.rename_points_with_name_pattern")]
    public override Task<Api.RenamePointsWithNamePatternResult> RenamePointsWithNamePattern(Api.RenamePointsWithNamePatternRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, RenamePointsWithNamePatternOperation.Descriptor,
            RenamePointsWithNamePatternOperation.CreateCommand, RenamePointsWithNamePatternOperation.OutputContracts, RenamePointsWithNamePatternOperation.CreateResult);

    [OperationImplementation("construction_operations.set_callout_view_properties")]
    public override Task<Api.SetCalloutViewPropertiesResult> SetCalloutViewProperties(Api.SetCalloutViewPropertiesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, SetCalloutViewPropertiesOperation.Descriptor,
            SetCalloutViewPropertiesOperation.CreateCommand, SetCalloutViewPropertiesOperation.OutputContracts, SetCalloutViewPropertiesOperation.CreateResult);

    [OperationImplementation("construction_operations.set_collection_instrument_ref_list_variable")]
    public override Task<Api.SetCollectionInstrumentRefListVariableResult> SetCollectionInstrumentRefListVariable(Api.SetCollectionInstrumentRefListVariableRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, SetCollectionInstrumentRefListVariableOperation.Descriptor,
            SetCollectionInstrumentRefListVariableOperation.CreateCommand, SetCollectionInstrumentRefListVariableOperation.OutputContracts,
            SetCollectionInstrumentRefListVariableOperation.CreateResult);

    [OperationImplementation("construction_operations.set_default_callout_view_properties")]
    public override Task<Api.SetDefaultCalloutViewPropertiesResult> SetDefaultCalloutViewProperties(Api.SetDefaultCalloutViewPropertiesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, SetDefaultCalloutViewPropertiesOperation.Descriptor,
            SetDefaultCalloutViewPropertiesOperation.CreateCommand, SetDefaultCalloutViewPropertiesOperation.OutputContracts, SetDefaultCalloutViewPropertiesOperation.CreateResult);

    [OperationImplementation("construction_operations.set_ith_callout_position_in_callout_view")]
    public override Task<Api.SetIthCalloutPositionInCalloutViewResult> SetIthCalloutPositionInCalloutView(Api.SetIthCalloutPositionInCalloutViewRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, SetIthCalloutPositionInCalloutViewOperation.Descriptor,
            SetIthCalloutPositionInCalloutViewOperation.CreateCommand, SetIthCalloutPositionInCalloutViewOperation.OutputContracts, SetIthCalloutPositionInCalloutViewOperation.CreateResult);

    [OperationImplementation("construction_operations.set_or_construct_default_collection")]
    public override Task<Api.SetOrConstructDefaultCollectionResult> SetOrConstructDefaultCollection(Api.SetOrConstructDefaultCollectionRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, SetOrConstructDefaultCollectionOperation.Descriptor,
            SetOrConstructDefaultCollectionOperation.CreateCommand, SetOrConstructDefaultCollectionOperation.OutputContracts, SetOrConstructDefaultCollectionOperation.CreateResult);

    [OperationImplementation("construction_operations.set_point_position_in_working_coordinates")]
    public override Task<Api.SetPointPositionInWorkingCoordinatesResult> SetPointPositionInWorkingCoordinates(Api.SetPointPositionInWorkingCoordinatesRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, SetPointPositionInWorkingCoordinatesOperation.Descriptor,
            SetPointPositionInWorkingCoordinatesOperation.CreateCommand, SetPointPositionInWorkingCoordinatesOperation.OutputContracts, SetPointPositionInWorkingCoordinatesOperation.CreateResult);

    [OperationImplementation("construction_operations.shift_plane")]
    public override Task<Api.ShiftPlaneResult> ShiftPlane(Api.ShiftPlaneRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, ShiftPlaneOperation.Descriptor,
            ShiftPlaneOperation.CreateCommand, ShiftPlaneOperation.OutputContracts, ShiftPlaneOperation.CreateResult);

    [OperationImplementation("construction_operations.transform_points_by_delta_about_working_frame")]
    public override Task<Api.TransformPointsByDeltaAboutWorkingFrameResult> TransformPointsByDeltaAboutWorkingFrame(Api.TransformPointsByDeltaAboutWorkingFrameRequest request, ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(request, context, TransformPointsByDeltaAboutWorkingFrameOperation.Descriptor,
            TransformPointsByDeltaAboutWorkingFrameOperation.CreateCommand, TransformPointsByDeltaAboutWorkingFrameOperation.OutputContracts, TransformPointsByDeltaAboutWorkingFrameOperation.CreateResult);

}
