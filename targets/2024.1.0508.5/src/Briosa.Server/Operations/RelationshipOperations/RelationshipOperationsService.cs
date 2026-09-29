using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal sealed partial class RelationshipOperationsService(OperationExecutor executor)
    : Api.RelationshipOperations.RelationshipOperationsBase
{
    [OperationImplementation("relationship_operations.enable_disable_relationships_for_optimization")]
    public override Task<Api.EnableDisableRelationshipsForOptimizationResult> EnableDisableRelationshipsForOptimization(
        Api.EnableDisableRelationshipsForOptimizationRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisableRelationshipsForOptimizationOperation.Descriptor,
            EnableDisableRelationshipsForOptimizationOperation.CreateCommand,
            EnableDisableRelationshipsForOptimizationOperation.OutputContracts,
            EnableDisableRelationshipsForOptimizationOperation.CreateResult);
    [OperationImplementation("relationship_operations.geom_relationship_ignore_input_points")]
    public override Task<Api.GeomRelationshipIgnoreInputPointsResult> GeomRelationshipIgnoreInputPoints(
        Api.GeomRelationshipIgnoreInputPointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GeomRelationshipIgnoreInputPointsOperation.Descriptor,
            GeomRelationshipIgnoreInputPointsOperation.CreateCommand, GeomRelationshipIgnoreInputPointsOperation.OutputContracts,
            GeomRelationshipIgnoreInputPointsOperation.CreateResult);

    [OperationImplementation("relationship_operations.geom_relationship_reuse_ignored_input_points")]
    public override Task<Api.GeomRelationshipReuseIgnoredInputPointsResult> GeomRelationshipReuseIgnoredInputPoints(
        Api.GeomRelationshipReuseIgnoredInputPointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GeomRelationshipReuseIgnoredInputPointsOperation.Descriptor,
            GeomRelationshipReuseIgnoredInputPointsOperation.CreateCommand, GeomRelationshipReuseIgnoredInputPointsOperation.OutputContracts,
            GeomRelationshipReuseIgnoredInputPointsOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_auto_vectors")]
    public override Task<Api.GetGeomRelationshipAutoVectorsResult> GetGeomRelationshipAutoVectors(
        Api.GetGeomRelationshipAutoVectorsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipAutoVectorsOperation.Descriptor,
            GetGeomRelationshipAutoVectorsOperation.CreateCommand,
            GetGeomRelationshipAutoVectorsOperation.OutputContracts,
            GetGeomRelationshipAutoVectorsOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_cardinal_points")]
    public override Task<Api.GetGeomRelationshipCardinalPointsResult> GetGeomRelationshipCardinalPoints(
        Api.GetGeomRelationshipCardinalPointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipCardinalPointsOperation.Descriptor,
            GetGeomRelationshipCardinalPointsOperation.CreateCommand,
            GetGeomRelationshipCardinalPointsOperation.OutputContracts,
            GetGeomRelationshipCardinalPointsOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_criteria")]
    public override Task<Api.GetGeomRelationshipCriteriaResult> GetGeomRelationshipCriteria(
        Api.GetGeomRelationshipCriteriaRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipCriteriaOperation.Descriptor,
            GetGeomRelationshipCriteriaOperation.CreateCommand,
            GetGeomRelationshipCriteriaOperation.OutputContracts,
            GetGeomRelationshipCriteriaOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_measured_avg_point")]
    public override Task<Api.GetGeomRelationshipMeasuredAvgPointResult> GetGeomRelationshipMeasuredAvgPoint(
        Api.GetGeomRelationshipMeasuredAvgPointRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipMeasuredAvgPointOperation.Descriptor,
            GetGeomRelationshipMeasuredAvgPointOperation.CreateCommand,
            GetGeomRelationshipMeasuredAvgPointOperation.OutputContracts,
            GetGeomRelationshipMeasuredAvgPointOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_measured_geometry")]
    public override Task<Api.GetGeomRelationshipMeasuredGeometryResult> GetGeomRelationshipMeasuredGeometry(
        Api.GetGeomRelationshipMeasuredGeometryRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipMeasuredGeometryOperation.Descriptor,
            GetGeomRelationshipMeasuredGeometryOperation.CreateCommand,
            GetGeomRelationshipMeasuredGeometryOperation.OutputContracts,
            GetGeomRelationshipMeasuredGeometryOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_nominal_avg_point")]
    public override Task<Api.GetGeomRelationshipNominalAvgPointResult> GetGeomRelationshipNominalAvgPoint(
        Api.GetGeomRelationshipNominalAvgPointRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipNominalAvgPointOperation.Descriptor,
            GetGeomRelationshipNominalAvgPointOperation.CreateCommand,
            GetGeomRelationshipNominalAvgPointOperation.OutputContracts,
            GetGeomRelationshipNominalAvgPointOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_nominal_geometry")]
    public override Task<Api.GetGeomRelationshipNominalGeometryResult> GetGeomRelationshipNominalGeometry(
        Api.GetGeomRelationshipNominalGeometryRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipNominalGeometryOperation.Descriptor,
            GetGeomRelationshipNominalGeometryOperation.CreateCommand,
            GetGeomRelationshipNominalGeometryOperation.OutputContracts,
            GetGeomRelationshipNominalGeometryOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_point_list")]
    public override Task<Api.GetGeomRelationshipPointListResult> GetGeomRelationshipPointList(
        Api.GetGeomRelationshipPointListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipPointListOperation.Descriptor,
            GetGeomRelationshipPointListOperation.CreateCommand,
            GetGeomRelationshipPointListOperation.OutputContracts,
            GetGeomRelationshipPointListOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_geom_relationship_projection_plane")]
    public override Task<Api.GetGeomRelationshipProjectionPlaneResult> GetGeomRelationshipProjectionPlane(
        Api.GetGeomRelationshipProjectionPlaneRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGeomRelationshipProjectionPlaneOperation.Descriptor,
            GetGeomRelationshipProjectionPlaneOperation.CreateCommand,
            GetGeomRelationshipProjectionPlaneOperation.OutputContracts,
            GetGeomRelationshipProjectionPlaneOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_pipe_relationship_cut_status")]
    public override Task<Api.GetPipeRelationshipCutStatusResult> GetPipeRelationshipCutStatus(
        Api.GetPipeRelationshipCutStatusRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPipeRelationshipCutStatusOperation.Descriptor,
            GetPipeRelationshipCutStatusOperation.CreateCommand,
            GetPipeRelationshipCutStatusOperation.OutputContracts,
            GetPipeRelationshipCutStatusOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_pipe_relationship_properties")]
    public override Task<Api.GetPipeRelationshipPropertiesResult> GetPipeRelationshipProperties(
        Api.GetPipeRelationshipPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPipeRelationshipPropertiesOperation.Descriptor,
            GetPipeRelationshipPropertiesOperation.CreateCommand, GetPipeRelationshipPropertiesOperation.OutputContracts,
            GetPipeRelationshipPropertiesOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_pipe_relationship_weights")]
    public override Task<Api.GetPipeRelationshipWeightsResult> GetPipeRelationshipWeights(
        Api.GetPipeRelationshipWeightsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPipeRelationshipWeightsOperation.Descriptor,
            GetPipeRelationshipWeightsOperation.CreateCommand, GetPipeRelationshipWeightsOperation.OutputContracts,
            GetPipeRelationshipWeightsOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_fit_constraints_scalar_type")]
    public override Task<Api.GetRelationshipFitConstraintsScalarTypeResult> GetRelationshipFitConstraintsScalarType(
        Api.GetRelationshipFitConstraintsScalarTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipFitConstraintsScalarTypeOperation.Descriptor,
            GetRelationshipFitConstraintsScalarTypeOperation.CreateCommand, GetRelationshipFitConstraintsScalarTypeOperation.OutputContracts,
            GetRelationshipFitConstraintsScalarTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_outlier_rejection_scalar_type")]
    public override Task<Api.GetRelationshipOutlierRejectionScalarTypeResult> GetRelationshipOutlierRejectionScalarType(
        Api.GetRelationshipOutlierRejectionScalarTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipOutlierRejectionScalarTypeOperation.Descriptor,
            GetRelationshipOutlierRejectionScalarTypeOperation.CreateCommand,
            GetRelationshipOutlierRejectionScalarTypeOperation.OutputContracts,
            GetRelationshipOutlierRejectionScalarTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_projection_options")]
    public override Task<Api.GetRelationshipProjectionOptionsResult> GetRelationshipProjectionOptions(
        Api.GetRelationshipProjectionOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipProjectionOptionsOperation.Descriptor,
            GetRelationshipProjectionOptionsOperation.CreateCommand,
            GetRelationshipProjectionOptionsOperation.OutputContracts,
            GetRelationshipProjectionOptionsOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_reporting_frame")]
    public override Task<Api.GetRelationshipReportingFrameResult> GetRelationshipReportingFrame(
        Api.GetRelationshipReportingFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipReportingFrameOperation.Descriptor,
            GetRelationshipReportingFrameOperation.CreateCommand,
            GetRelationshipReportingFrameOperation.OutputContracts,
            GetRelationshipReportingFrameOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_sub_sampling_options")]
    public override Task<Api.GetRelationshipSubSamplingOptionsResult> GetRelationshipSubSamplingOptions(
        Api.GetRelationshipSubSamplingOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipSubSamplingOptionsOperation.Descriptor,
            GetRelationshipSubSamplingOptionsOperation.CreateCommand,
            GetRelationshipSubSamplingOptionsOperation.OutputContracts,
            GetRelationshipSubSamplingOptionsOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_tolerance_scalar_type")]
    public override Task<Api.GetRelationshipToleranceScalarTypeResult> GetRelationshipToleranceScalarType(
        Api.GetRelationshipToleranceScalarTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipToleranceScalarTypeOperation.Descriptor,
            GetRelationshipToleranceScalarTypeOperation.CreateCommand,
            GetRelationshipToleranceScalarTypeOperation.OutputContracts,
            GetRelationshipToleranceScalarTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_tolerance_vector_type")]
    public override Task<Api.GetRelationshipToleranceVectorTypeResult> GetRelationshipToleranceVectorType(
        Api.GetRelationshipToleranceVectorTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipToleranceVectorTypeOperation.Descriptor,
            GetRelationshipToleranceVectorTypeOperation.CreateCommand,
            GetRelationshipToleranceVectorTypeOperation.OutputContracts,
            GetRelationshipToleranceVectorTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_type")]
    public override Task<Api.GetRelationshipTypeResult> GetRelationshipType(
        Api.GetRelationshipTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipTypeOperation.Descriptor,
            GetRelationshipTypeOperation.CreateCommand, GetRelationshipTypeOperation.OutputContracts,
            GetRelationshipTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.get_relationship_weighting")]
    public override Task<Api.GetRelationshipWeightingResult> GetRelationshipWeighting(
        Api.GetRelationshipWeightingRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetRelationshipWeightingOperation.Descriptor,
            GetRelationshipWeightingOperation.CreateCommand, GetRelationshipWeightingOperation.OutputContracts,
            GetRelationshipWeightingOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_pipe_fitting_relationship")]
    public override Task<Api.MakePipeFittingRelationshipResult> MakePipeFittingRelationship(
        Api.MakePipeFittingRelationshipRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakePipeFittingRelationshipOperation.Descriptor,
            MakePipeFittingRelationshipOperation.CreateCommand, MakePipeFittingRelationshipOperation.OutputContracts,
            MakePipeFittingRelationshipOperation.CreateResult);

    [OperationImplementation("relationship_operations.make_pipe_relationship_cut")]
    public override Task<Api.MakePipeRelationshipCutResult> MakePipeRelationshipCut(
        Api.MakePipeRelationshipCutRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakePipeRelationshipCutOperation.Descriptor,
            MakePipeRelationshipCutOperation.CreateCommand, MakePipeRelationshipCutOperation.OutputContracts,
            MakePipeRelationshipCutOperation.CreateResult);

    [OperationImplementation("relationship_operations.pipe_relationship_force_cut_to_frame")]
    public override Task<Api.PipeRelationshipForceCutToFrameResult> PipeRelationshipForceCutToFrame(
        Api.PipeRelationshipForceCutToFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PipeRelationshipForceCutToFrameOperation.Descriptor,
            PipeRelationshipForceCutToFrameOperation.CreateCommand,
            PipeRelationshipForceCutToFrameOperation.OutputContracts,
            PipeRelationshipForceCutToFrameOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_geom_relationship_auto_vectors_nominal_avn")]
    public override Task<Api.SetGeomRelationshipAutoVectorsNominalAvnResult> SetGeomRelationshipAutoVectorsNominalAvn(
        Api.SetGeomRelationshipAutoVectorsNominalAvnRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeomRelationshipAutoVectorsNominalAvnOperation.Descriptor,
            SetGeomRelationshipAutoVectorsNominalAvnOperation.CreateCommand,
            SetGeomRelationshipAutoVectorsNominalAvnOperation.OutputContracts,
            SetGeomRelationshipAutoVectorsNominalAvnOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_geom_relationship_cardinal_points")]
    public override Task<Api.SetGeomRelationshipCardinalPointsResult> SetGeomRelationshipCardinalPoints(
        Api.SetGeomRelationshipCardinalPointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeomRelationshipCardinalPointsOperation.Descriptor,
            SetGeomRelationshipCardinalPointsOperation.CreateCommand,
            SetGeomRelationshipCardinalPointsOperation.OutputContracts,
            SetGeomRelationshipCardinalPointsOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_geom_relationship_criteria")]
    public override Task<Api.SetGeomRelationshipCriteriaResult> SetGeomRelationshipCriteria(
        Api.SetGeomRelationshipCriteriaRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeomRelationshipCriteriaOperation.Descriptor,
            SetGeomRelationshipCriteriaOperation.CreateCommand, SetGeomRelationshipCriteriaOperation.OutputContracts,
            SetGeomRelationshipCriteriaOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_geom_relationship_measured_geometry")]
    public override Task<Api.SetGeomRelationshipMeasuredGeometryResult> SetGeomRelationshipMeasuredGeometry(
        Api.SetGeomRelationshipMeasuredGeometryRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeomRelationshipMeasuredGeometryOperation.Descriptor,
            SetGeomRelationshipMeasuredGeometryOperation.CreateCommand,
            SetGeomRelationshipMeasuredGeometryOperation.OutputContracts,
            SetGeomRelationshipMeasuredGeometryOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_geom_relationship_nominal_avg_point")]
    public override Task<Api.SetGeomRelationshipNominalAvgPointResult> SetGeomRelationshipNominalAvgPoint(
        Api.SetGeomRelationshipNominalAvgPointRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeomRelationshipNominalAvgPointOperation.Descriptor,
            SetGeomRelationshipNominalAvgPointOperation.CreateCommand,
            SetGeomRelationshipNominalAvgPointOperation.OutputContracts,
            SetGeomRelationshipNominalAvgPointOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_geom_relationship_nominal_geometry")]
    public override Task<Api.SetGeomRelationshipNominalGeometryResult> SetGeomRelationshipNominalGeometry(
        Api.SetGeomRelationshipNominalGeometryRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeomRelationshipNominalGeometryOperation.Descriptor,
            SetGeomRelationshipNominalGeometryOperation.CreateCommand,
            SetGeomRelationshipNominalGeometryOperation.OutputContracts,
            SetGeomRelationshipNominalGeometryOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_geom_relationship_projection_plane")]
    public override Task<Api.SetGeomRelationshipProjectionPlaneResult> SetGeomRelationshipProjectionPlane(
        Api.SetGeomRelationshipProjectionPlaneRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGeomRelationshipProjectionPlaneOperation.Descriptor,
            SetGeomRelationshipProjectionPlaneOperation.CreateCommand,
            SetGeomRelationshipProjectionPlaneOperation.OutputContracts,
            SetGeomRelationshipProjectionPlaneOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_object_to_object_direction_relationship_fit_constraints")]
    public override Task<Api.SetObjectToObjectDirectionRelationshipFitConstraintsResult> SetObjectToObjectDirectionRelationshipFitConstraints(
        Api.SetObjectToObjectDirectionRelationshipFitConstraintsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObjectToObjectDirectionRelationshipFitConstraintsOperation.Descriptor,
            SetObjectToObjectDirectionRelationshipFitConstraintsOperation.CreateCommand,
            SetObjectToObjectDirectionRelationshipFitConstraintsOperation.OutputContracts,
            SetObjectToObjectDirectionRelationshipFitConstraintsOperation.CreateResult);
    [OperationImplementation("relationship_operations.set_pipe_relationship_segment_properties")]
    public override Task<Api.SetPipeRelationshipSegmentPropertiesResult> SetPipeRelationshipSegmentProperties(
        Api.SetPipeRelationshipSegmentPropertiesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPipeRelationshipSegmentPropertiesOperation.Descriptor,
            SetPipeRelationshipSegmentPropertiesOperation.CreateCommand,
            SetPipeRelationshipSegmentPropertiesOperation.OutputContracts,
            SetPipeRelationshipSegmentPropertiesOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_pipe_relationship_weights")]
    public override Task<Api.SetPipeRelationshipWeightsResult> SetPipeRelationshipWeights(
        Api.SetPipeRelationshipWeightsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPipeRelationshipWeightsOperation.Descriptor,
            SetPipeRelationshipWeightsOperation.CreateCommand, SetPipeRelationshipWeightsOperation.OutputContracts,
            SetPipeRelationshipWeightsOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_auto_vectors_fit_avf")]
    public override Task<Api.SetRelationshipAutoVectorsFitAvfResult> SetRelationshipAutoVectorsFitAvf(
        Api.SetRelationshipAutoVectorsFitAvfRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipAutoVectorsFitAvfOperation.Descriptor,
            SetRelationshipAutoVectorsFitAvfOperation.CreateCommand, SetRelationshipAutoVectorsFitAvfOperation.OutputContracts,
            SetRelationshipAutoVectorsFitAvfOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_desired_meas_count")]
    public override Task<Api.SetRelationshipDesiredMeasCountResult> SetRelationshipDesiredMeasCount(
        Api.SetRelationshipDesiredMeasCountRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipDesiredMeasCountOperation.Descriptor,
            SetRelationshipDesiredMeasCountOperation.CreateCommand, SetRelationshipDesiredMeasCountOperation.OutputContracts,
            SetRelationshipDesiredMeasCountOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_dormant_status")]
    public override Task<Api.SetRelationshipDormantStatusResult> SetRelationshipDormantStatus(
        Api.SetRelationshipDormantStatusRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipDormantStatusOperation.Descriptor,
            SetRelationshipDormantStatusOperation.CreateCommand, SetRelationshipDormantStatusOperation.OutputContracts,
            SetRelationshipDormantStatusOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_fit_constraints_scalar_type")]
    public override Task<Api.SetRelationshipFitConstraintsScalarTypeResult> SetRelationshipFitConstraintsScalarType(
        Api.SetRelationshipFitConstraintsScalarTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipFitConstraintsScalarTypeOperation.Descriptor,
            SetRelationshipFitConstraintsScalarTypeOperation.CreateCommand, [],
            SetRelationshipFitConstraintsScalarTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_orientation_fit_constraints_vector_type")]
    public override Task<Api.SetRelationshipOrientationFitConstraintsVectorTypeResult> SetRelationshipOrientationFitConstraintsVectorType(
        Api.SetRelationshipOrientationFitConstraintsVectorTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipOrientationFitConstraintsVectorTypeOperation.Descriptor,
            SetRelationshipOrientationFitConstraintsVectorTypeOperation.CreateCommand,
            SetRelationshipOrientationFitConstraintsVectorTypeOperation.OutputContracts,
            SetRelationshipOrientationFitConstraintsVectorTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_outlier_rejection_scalar_type")]
    public override Task<Api.SetRelationshipOutlierRejectionScalarTypeResult> SetRelationshipOutlierRejectionScalarType(
        Api.SetRelationshipOutlierRejectionScalarTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipOutlierRejectionScalarTypeOperation.Descriptor,
            SetRelationshipOutlierRejectionScalarTypeOperation.CreateCommand, SetRelationshipOutlierRejectionScalarTypeOperation.OutputContracts,
            SetRelationshipOutlierRejectionScalarTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_position_fit_constraints_vector_type")]
    public override Task<Api.SetRelationshipPositionFitConstraintsVectorTypeResult> SetRelationshipPositionFitConstraintsVectorType(
        Api.SetRelationshipPositionFitConstraintsVectorTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipPositionFitConstraintsVectorTypeOperation.Descriptor,
            SetRelationshipPositionFitConstraintsVectorTypeOperation.CreateCommand,
            SetRelationshipPositionFitConstraintsVectorTypeOperation.OutputContracts,
            SetRelationshipPositionFitConstraintsVectorTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_projection_options")]
    public override Task<Api.SetRelationshipProjectionOptionsResult> SetRelationshipProjectionOptions(
        Api.SetRelationshipProjectionOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipProjectionOptionsOperation.Descriptor,
            SetRelationshipProjectionOptionsOperation.CreateCommand, SetRelationshipProjectionOptionsOperation.OutputContracts,
            SetRelationshipProjectionOptionsOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_reporting_frame")]
    public override Task<Api.SetRelationshipReportingFrameResult> SetRelationshipReportingFrame(
        Api.SetRelationshipReportingFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipReportingFrameOperation.Descriptor,
            SetRelationshipReportingFrameOperation.CreateCommand, SetRelationshipReportingFrameOperation.OutputContracts,
            SetRelationshipReportingFrameOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_sub_sampling_options")]
    public override Task<Api.SetRelationshipSubSamplingOptionsResult> SetRelationshipSubSamplingOptions(
        Api.SetRelationshipSubSamplingOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipSubSamplingOptionsOperation.Descriptor,
            SetRelationshipSubSamplingOptionsOperation.CreateCommand, SetRelationshipSubSamplingOptionsOperation.OutputContracts,
            SetRelationshipSubSamplingOptionsOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_tolerance_scalar_type")]
    public override Task<Api.SetRelationshipToleranceScalarTypeResult> SetRelationshipToleranceScalarType(
        Api.SetRelationshipToleranceScalarTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipToleranceScalarTypeOperation.Descriptor,
            SetRelationshipToleranceScalarTypeOperation.CreateCommand, SetRelationshipToleranceScalarTypeOperation.OutputContracts,
            SetRelationshipToleranceScalarTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_tolerance_vector_type")]
    public override Task<Api.SetRelationshipToleranceVectorTypeResult> SetRelationshipToleranceVectorType(
        Api.SetRelationshipToleranceVectorTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipToleranceVectorTypeOperation.Descriptor,
            SetRelationshipToleranceVectorTypeOperation.CreateCommand, SetRelationshipToleranceVectorTypeOperation.OutputContracts,
            SetRelationshipToleranceVectorTypeOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_voxel_cloud_display")]
    public override Task<Api.SetRelationshipVoxelCloudDisplayResult> SetRelationshipVoxelCloudDisplay(
        Api.SetRelationshipVoxelCloudDisplayRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipVoxelCloudDisplayOperation.Descriptor,
            SetRelationshipVoxelCloudDisplayOperation.CreateCommand, SetRelationshipVoxelCloudDisplayOperation.OutputContracts,
            SetRelationshipVoxelCloudDisplayOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_weighting")]
    public override Task<Api.SetRelationshipWeightingResult> SetRelationshipWeighting(
        Api.SetRelationshipWeightingRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipWeightingOperation.Descriptor,
            SetRelationshipWeightingOperation.CreateCommand, SetRelationshipWeightingOperation.OutputContracts,
            SetRelationshipWeightingOperation.CreateResult);

    [OperationImplementation("relationship_operations.set_relationship_weights_normalized")]
    public override Task<Api.SetRelationshipWeightsNormalizedResult> SetRelationshipWeightsNormalized(
        Api.SetRelationshipWeightsNormalizedRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipWeightsNormalizedOperation.Descriptor,
            SetRelationshipWeightsNormalizedOperation.CreateCommand, SetRelationshipWeightsNormalizedOperation.OutputContracts,
            SetRelationshipWeightsNormalizedOperation.CreateResult);

}
