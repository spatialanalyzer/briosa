using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal sealed class RelationshipOperationsService(OperationExecutor executor)
    : Api.RelationshipOperations.RelationshipOperationsBase
{
    [OperationImplementation("relationship_operations.enable_disable_relationships_for_optimization")]
    public override Task<Api.EnableDisableRelationshipsForOptimizationResult> EnableDisableRelationshipsForOptimization(
        Api.EnableDisableRelationshipsForOptimizationRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EnableDisableRelationshipsForOptimizationRequest, Api.EnableDisableRelationshipsForOptimizationResult>(
            executor,
            request,
            context,
            "relationship_operations.enable_disable_relationships_for_optimization");

    [OperationImplementation("relationship_operations.geom_relationship_ignore_input_points")]
    public override Task<Api.GeomRelationshipIgnoreInputPointsResult> GeomRelationshipIgnoreInputPoints(
        Api.GeomRelationshipIgnoreInputPointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GeomRelationshipIgnoreInputPointsRequest, Api.GeomRelationshipIgnoreInputPointsResult>(
            executor,
            request,
            context,
            "relationship_operations.geom_relationship_ignore_input_points");

    [OperationImplementation("relationship_operations.geom_relationship_reuse_ignored_input_points")]
    public override Task<Api.GeomRelationshipReuseIgnoredInputPointsResult> GeomRelationshipReuseIgnoredInputPoints(
        Api.GeomRelationshipReuseIgnoredInputPointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GeomRelationshipReuseIgnoredInputPointsRequest, Api.GeomRelationshipReuseIgnoredInputPointsResult>(
            executor,
            request,
            context,
            "relationship_operations.geom_relationship_reuse_ignored_input_points");

    [OperationImplementation("relationship_operations.get_geom_relationship_auto_vectors")]
    public override Task<Api.GetGeomRelationshipAutoVectorsResult> GetGeomRelationshipAutoVectors(
        Api.GetGeomRelationshipAutoVectorsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipAutoVectorsRequest, Api.GetGeomRelationshipAutoVectorsResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_auto_vectors");

    [OperationImplementation("relationship_operations.get_geom_relationship_cardinal_points")]
    public override Task<Api.GetGeomRelationshipCardinalPointsResult> GetGeomRelationshipCardinalPoints(
        Api.GetGeomRelationshipCardinalPointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipCardinalPointsRequest, Api.GetGeomRelationshipCardinalPointsResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_cardinal_points");

    [OperationImplementation("relationship_operations.get_geom_relationship_criteria")]
    public override Task<Api.GetGeomRelationshipCriteriaResult> GetGeomRelationshipCriteria(
        Api.GetGeomRelationshipCriteriaRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipCriteriaRequest, Api.GetGeomRelationshipCriteriaResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_criteria");

    [OperationImplementation("relationship_operations.get_geom_relationship_measured_avg_point")]
    public override Task<Api.GetGeomRelationshipMeasuredAvgPointResult> GetGeomRelationshipMeasuredAvgPoint(
        Api.GetGeomRelationshipMeasuredAvgPointRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipMeasuredAvgPointRequest, Api.GetGeomRelationshipMeasuredAvgPointResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_measured_avg_point");

    [OperationImplementation("relationship_operations.get_geom_relationship_measured_geometry")]
    public override Task<Api.GetGeomRelationshipMeasuredGeometryResult> GetGeomRelationshipMeasuredGeometry(
        Api.GetGeomRelationshipMeasuredGeometryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipMeasuredGeometryRequest, Api.GetGeomRelationshipMeasuredGeometryResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_measured_geometry");

    [OperationImplementation("relationship_operations.get_geom_relationship_nominal_avg_point")]
    public override Task<Api.GetGeomRelationshipNominalAvgPointResult> GetGeomRelationshipNominalAvgPoint(
        Api.GetGeomRelationshipNominalAvgPointRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipNominalAvgPointRequest, Api.GetGeomRelationshipNominalAvgPointResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_nominal_avg_point");

    [OperationImplementation("relationship_operations.get_geom_relationship_nominal_geometry")]
    public override Task<Api.GetGeomRelationshipNominalGeometryResult> GetGeomRelationshipNominalGeometry(
        Api.GetGeomRelationshipNominalGeometryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipNominalGeometryRequest, Api.GetGeomRelationshipNominalGeometryResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_nominal_geometry");

    [OperationImplementation("relationship_operations.get_geom_relationship_point_list")]
    public override Task<Api.GetGeomRelationshipPointListResult> GetGeomRelationshipPointList(
        Api.GetGeomRelationshipPointListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipPointListRequest, Api.GetGeomRelationshipPointListResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_point_list");

    [OperationImplementation("relationship_operations.get_geom_relationship_projection_plane")]
    public override Task<Api.GetGeomRelationshipProjectionPlaneResult> GetGeomRelationshipProjectionPlane(
        Api.GetGeomRelationshipProjectionPlaneRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGeomRelationshipProjectionPlaneRequest, Api.GetGeomRelationshipProjectionPlaneResult>(
            executor,
            request,
            context,
            "relationship_operations.get_geom_relationship_projection_plane");

    [OperationImplementation("relationship_operations.get_pipe_relationship_cut_status")]
    public override Task<Api.GetPipeRelationshipCutStatusResult> GetPipeRelationshipCutStatus(
        Api.GetPipeRelationshipCutStatusRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPipeRelationshipCutStatusRequest, Api.GetPipeRelationshipCutStatusResult>(
            executor,
            request,
            context,
            "relationship_operations.get_pipe_relationship_cut_status");

    [OperationImplementation("relationship_operations.get_pipe_relationship_properties")]
    public override Task<Api.GetPipeRelationshipPropertiesResult> GetPipeRelationshipProperties(
        Api.GetPipeRelationshipPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPipeRelationshipPropertiesRequest, Api.GetPipeRelationshipPropertiesResult>(
            executor,
            request,
            context,
            "relationship_operations.get_pipe_relationship_properties");

    [OperationImplementation("relationship_operations.get_pipe_relationship_weights")]
    public override Task<Api.GetPipeRelationshipWeightsResult> GetPipeRelationshipWeights(
        Api.GetPipeRelationshipWeightsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPipeRelationshipWeightsRequest, Api.GetPipeRelationshipWeightsResult>(
            executor,
            request,
            context,
            "relationship_operations.get_pipe_relationship_weights");

    [OperationImplementation("relationship_operations.get_relationship_fit_constraints_scalar_type")]
    public override Task<Api.GetRelationshipFitConstraintsScalarTypeResult> GetRelationshipFitConstraintsScalarType(
        Api.GetRelationshipFitConstraintsScalarTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipFitConstraintsScalarTypeRequest, Api.GetRelationshipFitConstraintsScalarTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_fit_constraints_scalar_type");

    [OperationImplementation("relationship_operations.get_relationship_outlier_rejection_scalar_type")]
    public override Task<Api.GetRelationshipOutlierRejectionScalarTypeResult> GetRelationshipOutlierRejectionScalarType(
        Api.GetRelationshipOutlierRejectionScalarTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipOutlierRejectionScalarTypeRequest, Api.GetRelationshipOutlierRejectionScalarTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_outlier_rejection_scalar_type");

    [OperationImplementation("relationship_operations.get_relationship_projection_options")]
    public override Task<Api.GetRelationshipProjectionOptionsResult> GetRelationshipProjectionOptions(
        Api.GetRelationshipProjectionOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipProjectionOptionsRequest, Api.GetRelationshipProjectionOptionsResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_projection_options");

    [OperationImplementation("relationship_operations.get_relationship_reporting_frame")]
    public override Task<Api.GetRelationshipReportingFrameResult> GetRelationshipReportingFrame(
        Api.GetRelationshipReportingFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipReportingFrameRequest, Api.GetRelationshipReportingFrameResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_reporting_frame");

    [OperationImplementation("relationship_operations.get_relationship_sub_sampling_options")]
    public override Task<Api.GetRelationshipSubSamplingOptionsResult> GetRelationshipSubSamplingOptions(
        Api.GetRelationshipSubSamplingOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipSubSamplingOptionsRequest, Api.GetRelationshipSubSamplingOptionsResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_sub_sampling_options");

    [OperationImplementation("relationship_operations.get_relationship_tolerance_scalar_type")]
    public override Task<Api.GetRelationshipToleranceScalarTypeResult> GetRelationshipToleranceScalarType(
        Api.GetRelationshipToleranceScalarTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipToleranceScalarTypeRequest, Api.GetRelationshipToleranceScalarTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_tolerance_scalar_type");

    [OperationImplementation("relationship_operations.get_relationship_tolerance_vector_type")]
    public override Task<Api.GetRelationshipToleranceVectorTypeResult> GetRelationshipToleranceVectorType(
        Api.GetRelationshipToleranceVectorTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipToleranceVectorTypeRequest, Api.GetRelationshipToleranceVectorTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_tolerance_vector_type");

    [OperationImplementation("relationship_operations.get_relationship_type")]
    public override Task<Api.GetRelationshipTypeResult> GetRelationshipType(
        Api.GetRelationshipTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipTypeRequest, Api.GetRelationshipTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_type");

    [OperationImplementation("relationship_operations.get_relationship_weighting")]
    public override Task<Api.GetRelationshipWeightingResult> GetRelationshipWeighting(
        Api.GetRelationshipWeightingRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetRelationshipWeightingRequest, Api.GetRelationshipWeightingResult>(
            executor,
            request,
            context,
            "relationship_operations.get_relationship_weighting");

    [OperationImplementation("relationship_operations.make_pipe_fitting_relationship")]
    public override Task<Api.MakePipeFittingRelationshipResult> MakePipeFittingRelationship(
        Api.MakePipeFittingRelationshipRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakePipeFittingRelationshipRequest, Api.MakePipeFittingRelationshipResult>(
            executor,
            request,
            context,
            "relationship_operations.make_pipe_fitting_relationship");

    [OperationImplementation("relationship_operations.make_pipe_relationship_cut")]
    public override Task<Api.MakePipeRelationshipCutResult> MakePipeRelationshipCut(
        Api.MakePipeRelationshipCutRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakePipeRelationshipCutRequest, Api.MakePipeRelationshipCutResult>(
            executor,
            request,
            context,
            "relationship_operations.make_pipe_relationship_cut");

    [OperationImplementation("relationship_operations.pipe_relationship_force_cut_to_frame")]
    public override Task<Api.PipeRelationshipForceCutToFrameResult> PipeRelationshipForceCutToFrame(
        Api.PipeRelationshipForceCutToFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PipeRelationshipForceCutToFrameRequest, Api.PipeRelationshipForceCutToFrameResult>(
            executor,
            request,
            context,
            "relationship_operations.pipe_relationship_force_cut_to_frame");

    [OperationImplementation("relationship_operations.set_geom_relationship_auto_measure_nominal_feature")]
    public override Task<Api.SetGeomRelationshipAutoMeasureNominalFeatureResult> SetGeomRelationshipAutoMeasureNominalFeature(
        Api.SetGeomRelationshipAutoMeasureNominalFeatureRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipAutoMeasureNominalFeatureRequest, Api.SetGeomRelationshipAutoMeasureNominalFeatureResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_auto_measure_nominal_feature");

    [OperationImplementation("relationship_operations.set_geom_relationship_auto_vectors_nominal_avn")]
    public override Task<Api.SetGeomRelationshipAutoVectorsNominalAvnResult> SetGeomRelationshipAutoVectorsNominalAvn(
        Api.SetGeomRelationshipAutoVectorsNominalAvnRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipAutoVectorsNominalAvnRequest, Api.SetGeomRelationshipAutoVectorsNominalAvnResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_auto_vectors_nominal_avn");

    [OperationImplementation("relationship_operations.set_geom_relationship_cardinal_points")]
    public override Task<Api.SetGeomRelationshipCardinalPointsResult> SetGeomRelationshipCardinalPoints(
        Api.SetGeomRelationshipCardinalPointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipCardinalPointsRequest, Api.SetGeomRelationshipCardinalPointsResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_cardinal_points");

    [OperationImplementation("relationship_operations.set_geom_relationship_criteria")]
    public override Task<Api.SetGeomRelationshipCriteriaResult> SetGeomRelationshipCriteria(
        Api.SetGeomRelationshipCriteriaRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipCriteriaRequest, Api.SetGeomRelationshipCriteriaResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_criteria");

    [OperationImplementation("relationship_operations.set_geom_relationship_measured_geometry")]
    public override Task<Api.SetGeomRelationshipMeasuredGeometryResult> SetGeomRelationshipMeasuredGeometry(
        Api.SetGeomRelationshipMeasuredGeometryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipMeasuredGeometryRequest, Api.SetGeomRelationshipMeasuredGeometryResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_measured_geometry");

    [OperationImplementation("relationship_operations.set_geom_relationship_nominal_avg_point")]
    public override Task<Api.SetGeomRelationshipNominalAvgPointResult> SetGeomRelationshipNominalAvgPoint(
        Api.SetGeomRelationshipNominalAvgPointRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipNominalAvgPointRequest, Api.SetGeomRelationshipNominalAvgPointResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_nominal_avg_point");

    [OperationImplementation("relationship_operations.set_geom_relationship_nominal_geometry")]
    public override Task<Api.SetGeomRelationshipNominalGeometryResult> SetGeomRelationshipNominalGeometry(
        Api.SetGeomRelationshipNominalGeometryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipNominalGeometryRequest, Api.SetGeomRelationshipNominalGeometryResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_nominal_geometry");

    [OperationImplementation("relationship_operations.set_geom_relationship_projection_plane")]
    public override Task<Api.SetGeomRelationshipProjectionPlaneResult> SetGeomRelationshipProjectionPlane(
        Api.SetGeomRelationshipProjectionPlaneRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGeomRelationshipProjectionPlaneRequest, Api.SetGeomRelationshipProjectionPlaneResult>(
            executor,
            request,
            context,
            "relationship_operations.set_geom_relationship_projection_plane");

    [OperationImplementation("relationship_operations.set_object_to_object_direction_relationship_fit_constraints")]
    public override Task<Api.SetObjectToObjectDirectionRelationshipFitConstraintsResult> SetObjectToObjectDirectionRelationshipFitConstraints(
        Api.SetObjectToObjectDirectionRelationshipFitConstraintsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObjectToObjectDirectionRelationshipFitConstraintsRequest, Api.SetObjectToObjectDirectionRelationshipFitConstraintsResult>(
            executor,
            request,
            context,
            "relationship_operations.set_object_to_object_direction_relationship_fit_constraints");

    [OperationImplementation("relationship_operations.set_pipe_relationship_segment_properties")]
    public override Task<Api.SetPipeRelationshipSegmentPropertiesResult> SetPipeRelationshipSegmentProperties(
        Api.SetPipeRelationshipSegmentPropertiesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPipeRelationshipSegmentPropertiesRequest, Api.SetPipeRelationshipSegmentPropertiesResult>(
            executor,
            request,
            context,
            "relationship_operations.set_pipe_relationship_segment_properties");

    [OperationImplementation("relationship_operations.set_pipe_relationship_weights")]
    public override Task<Api.SetPipeRelationshipWeightsResult> SetPipeRelationshipWeights(
        Api.SetPipeRelationshipWeightsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPipeRelationshipWeightsRequest, Api.SetPipeRelationshipWeightsResult>(
            executor,
            request,
            context,
            "relationship_operations.set_pipe_relationship_weights");

    [OperationImplementation("relationship_operations.set_relationship_auto_vectors_fit_avf")]
    public override Task<Api.SetRelationshipAutoVectorsFitAvfResult> SetRelationshipAutoVectorsFitAvf(
        Api.SetRelationshipAutoVectorsFitAvfRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipAutoVectorsFitAvfRequest, Api.SetRelationshipAutoVectorsFitAvfResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_auto_vectors_fit_avf");

    [OperationImplementation("relationship_operations.set_relationship_auto_vectors_group_default_prefix")]
    public override Task<Api.SetRelationshipAutoVectorsGroupDefaultPrefixResult> SetRelationshipAutoVectorsGroupDefaultPrefix(
        Api.SetRelationshipAutoVectorsGroupDefaultPrefixRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipAutoVectorsGroupDefaultPrefixRequest, Api.SetRelationshipAutoVectorsGroupDefaultPrefixResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_auto_vectors_group_default_prefix");

    [OperationImplementation("relationship_operations.set_relationship_desired_meas_count")]
    public override Task<Api.SetRelationshipDesiredMeasCountResult> SetRelationshipDesiredMeasCount(
        Api.SetRelationshipDesiredMeasCountRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipDesiredMeasCountRequest, Api.SetRelationshipDesiredMeasCountResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_desired_meas_count");

    [OperationImplementation("relationship_operations.set_relationship_dormant_status")]
    public override Task<Api.SetRelationshipDormantStatusResult> SetRelationshipDormantStatus(
        Api.SetRelationshipDormantStatusRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipDormantStatusRequest, Api.SetRelationshipDormantStatusResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_dormant_status");

    [OperationImplementation("relationship_operations.set_relationship_fit_constraints_scalar_type")]
    public override Task<Api.SetRelationshipFitConstraintsScalarTypeResult> SetRelationshipFitConstraintsScalarType(
        Api.SetRelationshipFitConstraintsScalarTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipFitConstraintsScalarTypeRequest, Api.SetRelationshipFitConstraintsScalarTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_fit_constraints_scalar_type");

    [OperationImplementation("relationship_operations.set_relationship_orientation_fit_constraints_vector_type")]
    public override Task<Api.SetRelationshipOrientationFitConstraintsVectorTypeResult> SetRelationshipOrientationFitConstraintsVectorType(
        Api.SetRelationshipOrientationFitConstraintsVectorTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipOrientationFitConstraintsVectorTypeRequest, Api.SetRelationshipOrientationFitConstraintsVectorTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_orientation_fit_constraints_vector_type");

    [OperationImplementation("relationship_operations.set_relationship_outlier_rejection_scalar_type")]
    public override Task<Api.SetRelationshipOutlierRejectionScalarTypeResult> SetRelationshipOutlierRejectionScalarType(
        Api.SetRelationshipOutlierRejectionScalarTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipOutlierRejectionScalarTypeRequest, Api.SetRelationshipOutlierRejectionScalarTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_outlier_rejection_scalar_type");

    [OperationImplementation("relationship_operations.set_relationship_position_fit_constraints_vector_type")]
    public override Task<Api.SetRelationshipPositionFitConstraintsVectorTypeResult> SetRelationshipPositionFitConstraintsVectorType(
        Api.SetRelationshipPositionFitConstraintsVectorTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipPositionFitConstraintsVectorTypeRequest, Api.SetRelationshipPositionFitConstraintsVectorTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_position_fit_constraints_vector_type");

    [OperationImplementation("relationship_operations.set_relationship_projection_options")]
    public override Task<Api.SetRelationshipProjectionOptionsResult> SetRelationshipProjectionOptions(
        Api.SetRelationshipProjectionOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipProjectionOptionsRequest, Api.SetRelationshipProjectionOptionsResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_projection_options");

    [OperationImplementation("relationship_operations.set_relationship_reporting_frame")]
    public override Task<Api.SetRelationshipReportingFrameResult> SetRelationshipReportingFrame(
        Api.SetRelationshipReportingFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipReportingFrameRequest, Api.SetRelationshipReportingFrameResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_reporting_frame");

    [OperationImplementation("relationship_operations.set_relationship_sigmoidal_gap_fit_constraints")]
    public override Task<Api.SetRelationshipSigmoidalGapFitConstraintsResult> SetRelationshipSigmoidalGapFitConstraints(
        Api.SetRelationshipSigmoidalGapFitConstraintsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipSigmoidalGapFitConstraintsRequest, Api.SetRelationshipSigmoidalGapFitConstraintsResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_sigmoidal_gap_fit_constraints");

    [OperationImplementation("relationship_operations.set_relationship_sub_sampling_options")]
    public override Task<Api.SetRelationshipSubSamplingOptionsResult> SetRelationshipSubSamplingOptions(
        Api.SetRelationshipSubSamplingOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipSubSamplingOptionsRequest, Api.SetRelationshipSubSamplingOptionsResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_sub_sampling_options");

    [OperationImplementation("relationship_operations.set_relationship_tolerance_scalar_type")]
    public override Task<Api.SetRelationshipToleranceScalarTypeResult> SetRelationshipToleranceScalarType(
        Api.SetRelationshipToleranceScalarTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipToleranceScalarTypeRequest, Api.SetRelationshipToleranceScalarTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_tolerance_scalar_type");

    [OperationImplementation("relationship_operations.set_relationship_tolerance_vector_type")]
    public override Task<Api.SetRelationshipToleranceVectorTypeResult> SetRelationshipToleranceVectorType(
        Api.SetRelationshipToleranceVectorTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipToleranceVectorTypeRequest, Api.SetRelationshipToleranceVectorTypeResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_tolerance_vector_type");

    [OperationImplementation("relationship_operations.set_relationship_voxel_cloud_display")]
    public override Task<Api.SetRelationshipVoxelCloudDisplayResult> SetRelationshipVoxelCloudDisplay(
        Api.SetRelationshipVoxelCloudDisplayRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipVoxelCloudDisplayRequest, Api.SetRelationshipVoxelCloudDisplayResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_voxel_cloud_display");

    [OperationImplementation("relationship_operations.set_relationship_weighting")]
    public override Task<Api.SetRelationshipWeightingResult> SetRelationshipWeighting(
        Api.SetRelationshipWeightingRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipWeightingRequest, Api.SetRelationshipWeightingResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_weighting");

    [OperationImplementation("relationship_operations.set_relationship_weights_normalized")]
    public override Task<Api.SetRelationshipWeightsNormalizedResult> SetRelationshipWeightsNormalized(
        Api.SetRelationshipWeightsNormalizedRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipWeightsNormalizedRequest, Api.SetRelationshipWeightsNormalizedResult>(
            executor,
            request,
            context,
            "relationship_operations.set_relationship_weights_normalized");

}
