using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal sealed class GdtOperationsService(OperationExecutor executor)
    : Api.GdtOperations.GdtOperationsBase
{
    [OperationImplementation("gdt_operations.make_feature_checks")]
    public override Task<Api.MakeFeatureChecksResult> MakeFeatureChecks(Api.MakeFeatureChecksRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeFeatureChecksRequest, Api.MakeFeatureChecksResult>(executor, request, context, "gdt_operations.make_feature_checks");

    [OperationImplementation("gdt_operations.get_feature_check_reporting_frame")]
    public override Task<Api.GetFeatureCheckReportingFrameResult> GetFeatureCheckReportingFrame(Api.GetFeatureCheckReportingFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFeatureCheckReportingFrameRequest, Api.GetFeatureCheckReportingFrameResult>(executor, request, context, "gdt_operations.get_feature_check_reporting_frame");

    [OperationImplementation("gdt_operations.feature_inspection_auto_filter")]
    public override Task<Api.FeatureInspectionAutoFilterResult> FeatureInspectionAutoFilter(Api.FeatureInspectionAutoFilterRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FeatureInspectionAutoFilterRequest, Api.FeatureInspectionAutoFilterResult>(executor, request, context, "gdt_operations.feature_inspection_auto_filter");

    [OperationImplementation("gdt_operations.get_feature_check_reporting_options")]
    public override Task<Api.GetFeatureCheckReportingOptionsResult> GetFeatureCheckReportingOptions(Api.GetFeatureCheckReportingOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFeatureCheckReportingOptionsRequest, Api.GetFeatureCheckReportingOptionsResult>(executor, request, context, "gdt_operations.get_feature_check_reporting_options");

    [OperationImplementation("gdt_operations.make_surface_face_list_runtime_select")]
    public override Task<Api.MakeSurfaceFaceListRuntimeSelectResult> MakeSurfaceFaceListRuntimeSelect(Api.MakeSurfaceFaceListRuntimeSelectRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeSurfaceFaceListRuntimeSelectRequest, Api.MakeSurfaceFaceListRuntimeSelectResult>(executor, request, context, "gdt_operations.make_surface_face_list_runtime_select");

    [OperationImplementation("gdt_operations.get_gdt_options")]
    public override Task<Api.GetGdtOptionsResult> GetGdtOptions(Api.GetGdtOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetGdtOptionsRequest, Api.GetGdtOptionsResult>(executor, request, context, "gdt_operations.get_gdt_options");

    [OperationImplementation("gdt_operations.make_gdt_feature_check_annotation")]
    public override Task<Api.MakeGdtFeatureCheckAnnotationResult> MakeGdtFeatureCheckAnnotation(Api.MakeGdtFeatureCheckAnnotationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGdtFeatureCheckAnnotationRequest, Api.MakeGdtFeatureCheckAnnotationResult>(executor, request, context, "gdt_operations.make_gdt_feature_check_annotation");

    [OperationImplementation("gdt_operations.make_surface_face_list_from_surface")]
    public override Task<Api.MakeSurfaceFaceListFromSurfaceResult> MakeSurfaceFaceListFromSurface(Api.MakeSurfaceFaceListFromSurfaceRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeSurfaceFaceListFromSurfaceRequest, Api.MakeSurfaceFaceListFromSurfaceResult>(executor, request, context, "gdt_operations.make_surface_face_list_from_surface");

    [OperationImplementation("gdt_operations.evaluate_feature_check")]
    public override Task<Api.EvaluateFeatureCheckResult> EvaluateFeatureCheck(Api.EvaluateFeatureCheckRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EvaluateFeatureCheckRequest, Api.EvaluateFeatureCheckResult>(executor, request, context, "gdt_operations.evaluate_feature_check");

    [OperationImplementation("gdt_operations.set_gdt_options")]
    public override Task<Api.SetGdtOptionsResult> SetGdtOptions(Api.SetGdtOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGdtOptionsRequest, Api.SetGdtOptionsResult>(executor, request, context, "gdt_operations.set_gdt_options");

    [OperationImplementation("gdt_operations.make_feature_check_ref_list_from_collection")]
    public override Task<Api.MakeFeatureCheckRefListFromCollectionResult> MakeFeatureCheckRefListFromCollection(Api.MakeFeatureCheckRefListFromCollectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeFeatureCheckRefListFromCollectionRequest, Api.MakeFeatureCheckRefListFromCollectionResult>(executor, request, context, "gdt_operations.make_feature_check_ref_list_from_collection");

    [OperationImplementation("gdt_operations.make_datum_ref_list_from_collection")]
    public override Task<Api.MakeDatumRefListFromCollectionResult> MakeDatumRefListFromCollection(Api.MakeDatumRefListFromCollectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeDatumRefListFromCollectionRequest, Api.MakeDatumRefListFromCollectionResult>(executor, request, context, "gdt_operations.make_datum_ref_list_from_collection");

    [OperationImplementation("gdt_operations.make_annotation_ref_list_from_collection")]
    public override Task<Api.MakeAnnotationRefListFromCollectionResult> MakeAnnotationRefListFromCollection(Api.MakeAnnotationRefListFromCollectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeAnnotationRefListFromCollectionRequest, Api.MakeAnnotationRefListFromCollectionResult>(executor, request, context, "gdt_operations.make_annotation_ref_list_from_collection");

    [OperationImplementation("gdt_operations.get_feature_check_datum_references")]
    public override Task<Api.GetFeatureCheckDatumReferencesResult> GetFeatureCheckDatumReferences(Api.GetFeatureCheckDatumReferencesRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFeatureCheckDatumReferencesRequest, Api.GetFeatureCheckDatumReferencesResult>(executor, request, context, "gdt_operations.get_feature_check_datum_references");

    [OperationImplementation("gdt_operations.get_feature_check_measurements")]
    public override Task<Api.GetFeatureCheckMeasurementsResult> GetFeatureCheckMeasurements(Api.GetFeatureCheckMeasurementsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFeatureCheckMeasurementsRequest, Api.GetFeatureCheckMeasurementsResult>(executor, request, context, "gdt_operations.get_feature_check_measurements");

    [OperationImplementation("gdt_operations.get_datum_measurements")]
    public override Task<Api.GetDatumMeasurementsResult> GetDatumMeasurements(Api.GetDatumMeasurementsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetDatumMeasurementsRequest, Api.GetDatumMeasurementsResult>(executor, request, context, "gdt_operations.get_datum_measurements");

    [OperationImplementation("gdt_operations.set_global_force_simultaneous_evaluation")]
    public override Task<Api.SetGlobalForceSimultaneousEvaluationResult> SetGlobalForceSimultaneousEvaluation(Api.SetGlobalForceSimultaneousEvaluationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetGlobalForceSimultaneousEvaluationRequest, Api.SetGlobalForceSimultaneousEvaluationResult>(executor, request, context, "gdt_operations.set_global_force_simultaneous_evaluation");

    [OperationImplementation("gdt_operations.set_datum_measurements")]
    public override Task<Api.SetDatumMeasurementsResult> SetDatumMeasurements(Api.SetDatumMeasurementsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetDatumMeasurementsRequest, Api.SetDatumMeasurementsResult>(executor, request, context, "gdt_operations.set_datum_measurements");

    [OperationImplementation("gdt_operations.get_feature_check_cylinder_eval_options")]
    public override Task<Api.GetFeatureCheckCylinderEvalOptionsResult> GetFeatureCheckCylinderEvalOptions(Api.GetFeatureCheckCylinderEvalOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetFeatureCheckCylinderEvalOptionsRequest, Api.GetFeatureCheckCylinderEvalOptionsResult>(executor, request, context, "gdt_operations.get_feature_check_cylinder_eval_options");

    [OperationImplementation("gdt_operations.evaluate_feature_checks")]
    public override Task<Api.EvaluateFeatureChecksResult> EvaluateFeatureChecks(Api.EvaluateFeatureChecksRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EvaluateFeatureChecksRequest, Api.EvaluateFeatureChecksResult>(executor, request, context, "gdt_operations.evaluate_feature_checks");

    [OperationImplementation("gdt_operations.delete_feature_checks")]
    public override Task<Api.DeleteFeatureChecksResult> DeleteFeatureChecks(Api.DeleteFeatureChecksRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteFeatureChecksRequest, Api.DeleteFeatureChecksResult>(executor, request, context, "gdt_operations.delete_feature_checks");

    [OperationImplementation("gdt_operations.set_feature_check_reporting_options")]
    public override Task<Api.SetFeatureCheckReportingOptionsResult> SetFeatureCheckReportingOptions(Api.SetFeatureCheckReportingOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetFeatureCheckReportingOptionsRequest, Api.SetFeatureCheckReportingOptionsResult>(executor, request, context, "gdt_operations.set_feature_check_reporting_options");

    [OperationImplementation("gdt_operations.make_gdt_datum_annotation")]
    public override Task<Api.MakeGdtDatumAnnotationResult> MakeGdtDatumAnnotation(Api.MakeGdtDatumAnnotationRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeGdtDatumAnnotationRequest, Api.MakeGdtDatumAnnotationResult>(executor, request, context, "gdt_operations.make_gdt_datum_annotation");

    [OperationImplementation("gdt_operations.refresh_datums_feature_checks_from_annotations")]
    public override Task<Api.RefreshDatumsFeatureChecksFromAnnotationsResult> RefreshDatumsFeatureChecksFromAnnotations(Api.RefreshDatumsFeatureChecksFromAnnotationsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RefreshDatumsFeatureChecksFromAnnotationsRequest, Api.RefreshDatumsFeatureChecksFromAnnotationsResult>(executor, request, context, "gdt_operations.refresh_datums_feature_checks_from_annotations");

    [OperationImplementation("gdt_operations.enable_disable_datum_alignment_for_feature_check")]
    public override Task<Api.EnableDisableDatumAlignmentForFeatureCheckResult> EnableDisableDatumAlignmentForFeatureCheck(Api.EnableDisableDatumAlignmentForFeatureCheckRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.EnableDisableDatumAlignmentForFeatureCheckRequest, Api.EnableDisableDatumAlignmentForFeatureCheckResult>(executor, request, context, "gdt_operations.enable_disable_datum_alignment_for_feature_check");

    [OperationImplementation("gdt_operations.make_feature_check_reference_list_wildcard_selection")]
    public override Task<Api.MakeFeatureCheckReferenceListWildcardSelectionResult> MakeFeatureCheckReferenceListWildcardSelection(Api.MakeFeatureCheckReferenceListWildcardSelectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeFeatureCheckReferenceListWildcardSelectionRequest, Api.MakeFeatureCheckReferenceListWildcardSelectionResult>(executor, request, context, "gdt_operations.make_feature_check_reference_list_wildcard_selection");

    [OperationImplementation("gdt_operations.generate_feature_check_summary")]
    public override Task<Api.GenerateFeatureCheckSummaryResult> GenerateFeatureCheckSummary(Api.GenerateFeatureCheckSummaryRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GenerateFeatureCheckSummaryRequest, Api.GenerateFeatureCheckSummaryResult>(executor, request, context, "gdt_operations.generate_feature_check_summary");

    [OperationImplementation("gdt_operations.set_feature_check_reporting_frame")]
    public override Task<Api.SetFeatureCheckReportingFrameResult> SetFeatureCheckReportingFrame(Api.SetFeatureCheckReportingFrameRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetFeatureCheckReportingFrameRequest, Api.SetFeatureCheckReportingFrameResult>(executor, request, context, "gdt_operations.set_feature_check_reporting_frame");

    [OperationImplementation("gdt_operations.start_stop_feature_check_trapping")]
    public override Task<Api.StartStopFeatureCheckTrappingResult> StartStopFeatureCheckTrapping(Api.StartStopFeatureCheckTrappingRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.StartStopFeatureCheckTrappingRequest, Api.StartStopFeatureCheckTrappingResult>(executor, request, context, "gdt_operations.start_stop_feature_check_trapping");

    [OperationImplementation("gdt_operations.set_feature_check_cylinder_eval_options")]
    public override Task<Api.SetFeatureCheckCylinderEvalOptionsResult> SetFeatureCheckCylinderEvalOptions(Api.SetFeatureCheckCylinderEvalOptionsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetFeatureCheckCylinderEvalOptionsRequest, Api.SetFeatureCheckCylinderEvalOptionsResult>(executor, request, context, "gdt_operations.set_feature_check_cylinder_eval_options");

    [OperationImplementation("gdt_operations.set_feature_check_measurements")]
    public override Task<Api.SetFeatureCheckMeasurementsResult> SetFeatureCheckMeasurements(Api.SetFeatureCheckMeasurementsRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetFeatureCheckMeasurementsRequest, Api.SetFeatureCheckMeasurementsResult>(executor, request, context, "gdt_operations.set_feature_check_measurements");

    [OperationImplementation("gdt_operations.make_annotation_ref_list_wildcard_selection")]
    public override Task<Api.MakeAnnotationRefListWildcardSelectionResult> MakeAnnotationRefListWildcardSelection(Api.MakeAnnotationRefListWildcardSelectionRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeAnnotationRefListWildcardSelectionRequest, Api.MakeAnnotationRefListWildcardSelectionResult>(executor, request, context, "gdt_operations.make_annotation_ref_list_wildcard_selection");

    [OperationImplementation("gdt_operations.datum_alignment")]
    public override Task<Api.DatumAlignmentResult> DatumAlignment(Api.DatumAlignmentRequest request, ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DatumAlignmentRequest, Api.DatumAlignmentResult>(executor, request, context, "gdt_operations.datum_alignment");

}
