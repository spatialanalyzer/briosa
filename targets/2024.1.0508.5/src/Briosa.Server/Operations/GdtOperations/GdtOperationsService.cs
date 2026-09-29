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
        executor.ExecuteAsync(request, context, MakeFeatureChecksOperation.Descriptor,
            MakeFeatureChecksOperation.CreateCommand, MakeFeatureChecksOperation.OutputContracts,
            MakeFeatureChecksOperation.CreateResult);

    [OperationImplementation("gdt_operations.get_feature_check_reporting_frame")]
    public override Task<Api.GetFeatureCheckReportingFrameResult> GetFeatureCheckReportingFrame(Api.GetFeatureCheckReportingFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFeatureCheckReportingFrameOperation.Descriptor,
            GetFeatureCheckReportingFrameOperation.CreateCommand, GetFeatureCheckReportingFrameOperation.OutputContracts,
            GetFeatureCheckReportingFrameOperation.CreateResult);

    [OperationImplementation("gdt_operations.feature_inspection_auto_filter")]
    public override Task<Api.FeatureInspectionAutoFilterResult> FeatureInspectionAutoFilter(Api.FeatureInspectionAutoFilterRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FeatureInspectionAutoFilterOperation.Descriptor,
            FeatureInspectionAutoFilterOperation.CreateCommand, FeatureInspectionAutoFilterOperation.OutputContracts,
            FeatureInspectionAutoFilterOperation.CreateResult);

    [OperationImplementation("gdt_operations.get_feature_check_reporting_options")]
    public override Task<Api.GetFeatureCheckReportingOptionsResult> GetFeatureCheckReportingOptions(Api.GetFeatureCheckReportingOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFeatureCheckReportingOptionsOperation.Descriptor,
            GetFeatureCheckReportingOptionsOperation.CreateCommand, GetFeatureCheckReportingOptionsOperation.OutputContracts,
            GetFeatureCheckReportingOptionsOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_surface_face_list_runtime_select")]
    public override Task<Api.MakeSurfaceFaceListRuntimeSelectResult> MakeSurfaceFaceListRuntimeSelect(Api.MakeSurfaceFaceListRuntimeSelectRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeSurfaceFaceListRuntimeSelectOperation.Descriptor,
            MakeSurfaceFaceListRuntimeSelectOperation.CreateCommand, MakeSurfaceFaceListRuntimeSelectOperation.OutputContracts,
            MakeSurfaceFaceListRuntimeSelectOperation.CreateResult);

    [OperationImplementation("gdt_operations.get_gdt_options")]
    public override Task<Api.GetGdtOptionsResult> GetGdtOptions(Api.GetGdtOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetGdtOptionsOperation.Descriptor,
            GetGdtOptionsOperation.CreateCommand, GetGdtOptionsOperation.OutputContracts,
            GetGdtOptionsOperation.CreateResult);

    [OperationImplementation("gdt_operations.set_global_force_simultaneous_evaluation")]
    public override Task<Api.SetGlobalForceSimultaneousEvaluationResult> SetGlobalForceSimultaneousEvaluation(
        Api.SetGlobalForceSimultaneousEvaluationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGlobalForceSimultaneousEvaluationOperation.Descriptor,
            SetGlobalForceSimultaneousEvaluationOperation.CreateCommand,
            SetGlobalForceSimultaneousEvaluationOperation.OutputContracts,
            SetGlobalForceSimultaneousEvaluationOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_gdt_feature_check_annotation")]
    public override Task<Api.MakeGdtFeatureCheckAnnotationResult> MakeGdtFeatureCheckAnnotation(Api.MakeGdtFeatureCheckAnnotationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGdtFeatureCheckAnnotationOperation.Descriptor,
            MakeGdtFeatureCheckAnnotationOperation.CreateCommand, MakeGdtFeatureCheckAnnotationOperation.OutputContracts,
            MakeGdtFeatureCheckAnnotationOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_surface_face_list_from_surface")]
    public override Task<Api.MakeSurfaceFaceListFromSurfaceResult> MakeSurfaceFaceListFromSurface(Api.MakeSurfaceFaceListFromSurfaceRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeSurfaceFaceListFromSurfaceOperation.Descriptor,
            MakeSurfaceFaceListFromSurfaceOperation.CreateCommand, MakeSurfaceFaceListFromSurfaceOperation.OutputContracts,
            MakeSurfaceFaceListFromSurfaceOperation.CreateResult);

    [OperationImplementation("gdt_operations.evaluate_feature_check")]
    public override Task<Api.EvaluateFeatureCheckResult> EvaluateFeatureCheck(Api.EvaluateFeatureCheckRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EvaluateFeatureCheckOperation.Descriptor,
            EvaluateFeatureCheckOperation.CreateCommand, EvaluateFeatureCheckOperation.OutputContracts,
            EvaluateFeatureCheckOperation.CreateResult);

    [OperationImplementation("gdt_operations.set_gdt_options")]
    public override Task<Api.SetGdtOptionsResult> SetGdtOptions(Api.SetGdtOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetGdtOptionsOperation.Descriptor,
            SetGdtOptionsOperation.CreateCommand, SetGdtOptionsOperation.OutputContracts,
            SetGdtOptionsOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_feature_check_ref_list_from_collection")]
    public override Task<Api.MakeFeatureCheckRefListFromCollectionResult> MakeFeatureCheckRefListFromCollection(Api.MakeFeatureCheckRefListFromCollectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeFeatureCheckRefListFromCollectionOperation.Descriptor,
            MakeFeatureCheckRefListFromCollectionOperation.CreateCommand, MakeFeatureCheckRefListFromCollectionOperation.OutputContracts,
            MakeFeatureCheckRefListFromCollectionOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_datum_ref_list_from_collection")]
    public override Task<Api.MakeDatumRefListFromCollectionResult> MakeDatumRefListFromCollection(Api.MakeDatumRefListFromCollectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeDatumRefListFromCollectionOperation.Descriptor,
            MakeDatumRefListFromCollectionOperation.CreateCommand, MakeDatumRefListFromCollectionOperation.OutputContracts,
            MakeDatumRefListFromCollectionOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_annotation_ref_list_from_collection")]
    public override Task<Api.MakeAnnotationRefListFromCollectionResult> MakeAnnotationRefListFromCollection(Api.MakeAnnotationRefListFromCollectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeAnnotationRefListFromCollectionOperation.Descriptor,
            MakeAnnotationRefListFromCollectionOperation.CreateCommand, MakeAnnotationRefListFromCollectionOperation.OutputContracts,
            MakeAnnotationRefListFromCollectionOperation.CreateResult);

    [OperationImplementation("gdt_operations.get_feature_check_datum_references")]
    public override Task<Api.GetFeatureCheckDatumReferencesResult> GetFeatureCheckDatumReferences(Api.GetFeatureCheckDatumReferencesRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFeatureCheckDatumReferencesOperation.Descriptor,
            GetFeatureCheckDatumReferencesOperation.CreateCommand, GetFeatureCheckDatumReferencesOperation.OutputContracts,
            GetFeatureCheckDatumReferencesOperation.CreateResult);

    [OperationImplementation("gdt_operations.get_feature_check_measurements")]
    public override Task<Api.GetFeatureCheckMeasurementsResult> GetFeatureCheckMeasurements(Api.GetFeatureCheckMeasurementsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFeatureCheckMeasurementsOperation.Descriptor,
            GetFeatureCheckMeasurementsOperation.CreateCommand, GetFeatureCheckMeasurementsOperation.OutputContracts,
            GetFeatureCheckMeasurementsOperation.CreateResult);

    [OperationImplementation("gdt_operations.get_datum_measurements")]
    public override Task<Api.GetDatumMeasurementsResult> GetDatumMeasurements(Api.GetDatumMeasurementsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetDatumMeasurementsOperation.Descriptor,
            GetDatumMeasurementsOperation.CreateCommand, GetDatumMeasurementsOperation.OutputContracts,
            GetDatumMeasurementsOperation.CreateResult);

    [OperationImplementation("gdt_operations.set_datum_measurements")]
    public override Task<Api.SetDatumMeasurementsResult> SetDatumMeasurements(Api.SetDatumMeasurementsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetDatumMeasurementsOperation.Descriptor,
            SetDatumMeasurementsOperation.CreateCommand, SetDatumMeasurementsOperation.OutputContracts,
            SetDatumMeasurementsOperation.CreateResult);

    [OperationImplementation("gdt_operations.get_feature_check_cylinder_eval_options")]
    public override Task<Api.GetFeatureCheckCylinderEvalOptionsResult> GetFeatureCheckCylinderEvalOptions(Api.GetFeatureCheckCylinderEvalOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetFeatureCheckCylinderEvalOptionsOperation.Descriptor,
            GetFeatureCheckCylinderEvalOptionsOperation.CreateCommand, GetFeatureCheckCylinderEvalOptionsOperation.OutputContracts,
            GetFeatureCheckCylinderEvalOptionsOperation.CreateResult);

    [OperationImplementation("gdt_operations.evaluate_feature_checks")]
    public override Task<Api.EvaluateFeatureChecksResult> EvaluateFeatureChecks(Api.EvaluateFeatureChecksRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EvaluateFeatureChecksOperation.Descriptor,
            EvaluateFeatureChecksOperation.CreateCommand, EvaluateFeatureChecksOperation.OutputContracts,
            EvaluateFeatureChecksOperation.CreateResult);

    [OperationImplementation("gdt_operations.delete_feature_checks")]
    public override Task<Api.DeleteFeatureChecksResult> DeleteFeatureChecks(Api.DeleteFeatureChecksRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteFeatureChecksOperation.Descriptor,
            DeleteFeatureChecksOperation.CreateCommand, DeleteFeatureChecksOperation.OutputContracts,
            DeleteFeatureChecksOperation.CreateResult);

    [OperationImplementation("gdt_operations.set_feature_check_reporting_options")]
    public override Task<Api.SetFeatureCheckReportingOptionsResult> SetFeatureCheckReportingOptions(Api.SetFeatureCheckReportingOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetFeatureCheckReportingOptionsOperation.Descriptor,
            SetFeatureCheckReportingOptionsOperation.CreateCommand, SetFeatureCheckReportingOptionsOperation.OutputContracts,
            SetFeatureCheckReportingOptionsOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_gdt_datum_annotation")]
    public override Task<Api.MakeGdtDatumAnnotationResult> MakeGdtDatumAnnotation(Api.MakeGdtDatumAnnotationRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeGdtDatumAnnotationOperation.Descriptor,
            MakeGdtDatumAnnotationOperation.CreateCommand, MakeGdtDatumAnnotationOperation.OutputContracts,
            MakeGdtDatumAnnotationOperation.CreateResult);

    [OperationImplementation("gdt_operations.refresh_datums_feature_checks_from_annotations")]
    public override Task<Api.RefreshDatumsFeatureChecksFromAnnotationsResult> RefreshDatumsFeatureChecksFromAnnotations(Api.RefreshDatumsFeatureChecksFromAnnotationsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RefreshDatumsFeatureChecksFromAnnotationsOperation.Descriptor,
            RefreshDatumsFeatureChecksFromAnnotationsOperation.CreateCommand, RefreshDatumsFeatureChecksFromAnnotationsOperation.OutputContracts,
            RefreshDatumsFeatureChecksFromAnnotationsOperation.CreateResult);

    [OperationImplementation("gdt_operations.enable_disable_datum_alignment_for_feature_check")]
    public override Task<Api.EnableDisableDatumAlignmentForFeatureCheckResult> EnableDisableDatumAlignmentForFeatureCheck(Api.EnableDisableDatumAlignmentForFeatureCheckRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, EnableDisableDatumAlignmentForFeatureCheckOperation.Descriptor,
            EnableDisableDatumAlignmentForFeatureCheckOperation.CreateCommand,
            EnableDisableDatumAlignmentForFeatureCheckOperation.OutputContracts,
            EnableDisableDatumAlignmentForFeatureCheckOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_feature_check_reference_list_wildcard_selection")]
    public override Task<Api.MakeFeatureCheckReferenceListWildcardSelectionResult> MakeFeatureCheckReferenceListWildcardSelection(Api.MakeFeatureCheckReferenceListWildcardSelectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeFeatureCheckReferenceListWildcardSelectionOperation.Descriptor,
            MakeFeatureCheckReferenceListWildcardSelectionOperation.CreateCommand, MakeFeatureCheckReferenceListWildcardSelectionOperation.OutputContracts,
            MakeFeatureCheckReferenceListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("gdt_operations.generate_feature_check_summary")]
    public override Task<Api.GenerateFeatureCheckSummaryResult> GenerateFeatureCheckSummary(Api.GenerateFeatureCheckSummaryRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GenerateFeatureCheckSummaryOperation.Descriptor,
            GenerateFeatureCheckSummaryOperation.CreateCommand, GenerateFeatureCheckSummaryOperation.OutputContracts,
            GenerateFeatureCheckSummaryOperation.CreateResult);

    [OperationImplementation("gdt_operations.set_feature_check_reporting_frame")]
    public override Task<Api.SetFeatureCheckReportingFrameResult> SetFeatureCheckReportingFrame(Api.SetFeatureCheckReportingFrameRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetFeatureCheckReportingFrameOperation.Descriptor,
            SetFeatureCheckReportingFrameOperation.CreateCommand, SetFeatureCheckReportingFrameOperation.OutputContracts,
            SetFeatureCheckReportingFrameOperation.CreateResult);

    [OperationImplementation("gdt_operations.start_stop_feature_check_trapping")]
    public override Task<Api.StartStopFeatureCheckTrappingResult> StartStopFeatureCheckTrapping(Api.StartStopFeatureCheckTrappingRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, StartStopFeatureCheckTrappingOperation.Descriptor,
            StartStopFeatureCheckTrappingOperation.CreateCommand,
            StartStopFeatureCheckTrappingOperation.OutputContracts,
            StartStopFeatureCheckTrappingOperation.CreateResult);

    [OperationImplementation("gdt_operations.set_feature_check_cylinder_eval_options")]
    public override Task<Api.SetFeatureCheckCylinderEvalOptionsResult> SetFeatureCheckCylinderEvalOptions(Api.SetFeatureCheckCylinderEvalOptionsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetFeatureCheckCylinderEvalOptionsOperation.Descriptor,
            SetFeatureCheckCylinderEvalOptionsOperation.CreateCommand, SetFeatureCheckCylinderEvalOptionsOperation.OutputContracts,
            SetFeatureCheckCylinderEvalOptionsOperation.CreateResult);

    [OperationImplementation("gdt_operations.set_feature_check_measurements")]
    public override Task<Api.SetFeatureCheckMeasurementsResult> SetFeatureCheckMeasurements(Api.SetFeatureCheckMeasurementsRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetFeatureCheckMeasurementsOperation.Descriptor,
            SetFeatureCheckMeasurementsOperation.CreateCommand, SetFeatureCheckMeasurementsOperation.OutputContracts,
            SetFeatureCheckMeasurementsOperation.CreateResult);

    [OperationImplementation("gdt_operations.make_annotation_ref_list_wildcard_selection")]
    public override Task<Api.MakeAnnotationRefListWildcardSelectionResult> MakeAnnotationRefListWildcardSelection(Api.MakeAnnotationRefListWildcardSelectionRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeAnnotationRefListWildcardSelectionOperation.Descriptor,
            MakeAnnotationRefListWildcardSelectionOperation.CreateCommand, MakeAnnotationRefListWildcardSelectionOperation.OutputContracts,
            MakeAnnotationRefListWildcardSelectionOperation.CreateResult);

    [OperationImplementation("gdt_operations.datum_alignment")]
    public override Task<Api.DatumAlignmentResult> DatumAlignment(Api.DatumAlignmentRequest request, ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DatumAlignmentOperation.Descriptor,
            DatumAlignmentOperation.CreateCommand, DatumAlignmentOperation.OutputContracts,
            DatumAlignmentOperation.CreateResult);

}
