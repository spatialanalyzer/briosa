using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal sealed class ViewControlService(OperationExecutor executor)
    : Api.ViewControl.ViewControlBase
{
    [OperationImplementation("view_control.auto_scale")]
    public override Task<Api.AutoScaleResult> AutoScale(
        Api.AutoScaleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AutoScaleOperation.Descriptor,
            AutoScaleOperation.CreateCommand, AutoScaleOperation.OutputContracts,
            AutoScaleOperation.CreateResult);

    [OperationImplementation("view_control.center_graphics_about_objects")]
    public override Task<Api.CenterGraphicsAboutObjectsResult> CenterGraphicsAboutObjects(
        Api.CenterGraphicsAboutObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CenterGraphicsAboutObjectsOperation.Descriptor,
            CenterGraphicsAboutObjectsOperation.CreateCommand, CenterGraphicsAboutObjectsOperation.OutputContracts,
            CenterGraphicsAboutObjectsOperation.CreateResult);

    [OperationImplementation("view_control.center_graphics_about_point")]
    public override Task<Api.CenterGraphicsAboutPointResult> CenterGraphicsAboutPoint(
        Api.CenterGraphicsAboutPointRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CenterGraphicsAboutPointOperation.Descriptor,
            CenterGraphicsAboutPointOperation.CreateCommand, CenterGraphicsAboutPointOperation.OutputContracts,
            CenterGraphicsAboutPointOperation.CreateResult);

    [OperationImplementation("view_control.define_point_of_view")]
    public override Task<Api.DefinePointOfViewResult> DefinePointOfView(
        Api.DefinePointOfViewRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DefinePointOfViewOperation.Descriptor,
            DefinePointOfViewOperation.CreateCommand, DefinePointOfViewOperation.OutputContracts,
            DefinePointOfViewOperation.CreateResult);

    [OperationImplementation("view_control.get_active_clipping_planes")]
    public override Task<Api.GetActiveClippingPlanesResult> GetActiveClippingPlanes(
        Api.GetActiveClippingPlanesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetActiveClippingPlanesOperation.Descriptor,
            GetActiveClippingPlanesOperation.CreateCommand, GetActiveClippingPlanesOperation.OutputContracts,
            GetActiveClippingPlanesOperation.CreateResult);

    [OperationImplementation("view_control.get_point_of_view_parameters")]
    public override Task<Api.GetPointOfViewParametersResult> GetPointOfViewParameters(
        Api.GetPointOfViewParametersRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetPointOfViewParametersOperation.Descriptor,
            GetPointOfViewParametersOperation.CreateCommand, GetPointOfViewParametersOperation.OutputContracts,
            GetPointOfViewParametersOperation.CreateResult);

    [OperationImplementation("view_control.hide_all_callout_views")]
    public override Task<Api.HideAllCalloutViewsResult> HideAllCalloutViews(
        Api.HideAllCalloutViewsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, HideAllCalloutViewsOperation.Descriptor,
            HideAllCalloutViewsOperation.CreateCommand, HideAllCalloutViewsOperation.OutputContracts,
            HideAllCalloutViewsOperation.CreateResult);

    [OperationImplementation("view_control.hide_objects")]
    public override Task<Api.HideObjectsResult> HideObjects(
        Api.HideObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, HideObjectsOperation.Descriptor,
            HideObjectsOperation.CreateCommand, HideObjectsOperation.OutputContracts,
            HideObjectsOperation.CreateResult);

    [OperationImplementation("view_control.highlight_objects")]
    public override Task<Api.HighlightObjectsResult> HighlightObjects(
        Api.HighlightObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, HighlightObjectsOperation.Descriptor,
            HighlightObjectsOperation.CreateCommand, HighlightObjectsOperation.OutputContracts,
            HighlightObjectsOperation.CreateResult);

    [OperationImplementation("view_control.highlight_point")]
    public override Task<Api.HighlightPointResult> HighlightPoint(
        Api.HighlightPointRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, HighlightPointOperation.Descriptor,
            HighlightPointOperation.CreateCommand, HighlightPointOperation.OutputContracts,
            HighlightPointOperation.CreateResult);

    [OperationImplementation("view_control.highlight_relationships")]
    public override Task<Api.HighlightRelationshipsResult> HighlightRelationships(
        Api.HighlightRelationshipsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, HighlightRelationshipsOperation.Descriptor,
            HighlightRelationshipsOperation.CreateCommand, HighlightRelationshipsOperation.OutputContracts,
            HighlightRelationshipsOperation.CreateResult);

    [OperationImplementation("view_control.load_ribbon_bar_from_xml_file")]
    public override Task<Api.LoadRibbonBarFromXmlFileResult> LoadRibbonBarFromXmlFile(
        Api.LoadRibbonBarFromXmlFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LoadRibbonBarFromXmlFileOperation.Descriptor,
            LoadRibbonBarFromXmlFileOperation.CreateCommand, LoadRibbonBarFromXmlFileOperation.OutputContracts,
            LoadRibbonBarFromXmlFileOperation.CreateResult);

    [OperationImplementation("view_control.refresh_views")]
    public override Task<Api.RefreshViewsResult> RefreshViews(
        Api.RefreshViewsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RefreshViewsOperation.Descriptor,
            RefreshViewsOperation.CreateCommand, RefreshViewsOperation.OutputContracts,
            RefreshViewsOperation.CreateResult);

    [OperationImplementation("view_control.reset_ribbon_bar_to_default")]
    public override Task<Api.ResetRibbonBarToDefaultResult> ResetRibbonBarToDefault(
        Api.ResetRibbonBarToDefaultRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ResetRibbonBarToDefaultOperation.Descriptor,
            ResetRibbonBarToDefaultOperation.CreateCommand, ResetRibbonBarToDefaultOperation.OutputContracts,
            ResetRibbonBarToDefaultOperation.CreateResult);

    [OperationImplementation("view_control.save_point_of_view")]
    public override Task<Api.SavePointOfViewResult> SavePointOfView(
        Api.SavePointOfViewRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SavePointOfViewOperation.Descriptor,
            SavePointOfViewOperation.CreateCommand, SavePointOfViewOperation.OutputContracts,
            SavePointOfViewOperation.CreateResult);

    [OperationImplementation("view_control.set_background_color")]
    public override Task<Api.SetBackgroundColorResult> SetBackgroundColor(
        Api.SetBackgroundColorRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetBackgroundColorOperation.Descriptor,
            SetBackgroundColorOperation.CreateCommand, SetBackgroundColorOperation.OutputContracts,
            SetBackgroundColorOperation.CreateResult);

    [OperationImplementation("view_control.set_mp_window_state")]
    public override Task<Api.SetMpWindowStateResult> SetMpWindowState(
        Api.SetMpWindowStateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetMpWindowStateOperation.Descriptor,
            SetMpWindowStateOperation.CreateCommand, SetMpWindowStateOperation.OutputContracts,
            SetMpWindowStateOperation.CreateResult);

    [OperationImplementation("view_control.set_objects_color")]
    public override Task<Api.SetObjectsColorResult> SetObjectsColor(
        Api.SetObjectsColorRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObjectsColorOperation.Descriptor,
            SetObjectsColorOperation.CreateCommand, SetObjectsColorOperation.OutputContracts,
            SetObjectsColorOperation.CreateResult);

    [OperationImplementation("view_control.set_objects_translucency")]
    public override Task<Api.SetObjectsTranslucencyResult> SetObjectsTranslucency(
        Api.SetObjectsTranslucencyRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetObjectsTranslucencyOperation.Descriptor,
            SetObjectsTranslucencyOperation.CreateCommand, SetObjectsTranslucencyOperation.OutputContracts,
            SetObjectsTranslucencyOperation.CreateResult);

    [OperationImplementation("view_control.set_point_of_view")]
    public override Task<Api.SetPointOfViewResult> SetPointOfView(
        Api.SetPointOfViewRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointOfViewOperation.Descriptor,
            SetPointOfViewOperation.CreateCommand, SetPointOfViewOperation.OutputContracts,
            SetPointOfViewOperation.CreateResult);

    [OperationImplementation("view_control.set_point_of_view_from_frame")]
    public override Task<Api.SetPointOfViewFromFrameResult> SetPointOfViewFromFrame(
        Api.SetPointOfViewFromFrameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointOfViewFromFrameOperation.Descriptor,
            SetPointOfViewFromFrameOperation.CreateCommand, SetPointOfViewFromFrameOperation.OutputContracts,
            SetPointOfViewFromFrameOperation.CreateResult);

    [OperationImplementation("view_control.set_point_of_view_from_instrument_updates")]
    public override Task<Api.SetPointOfViewFromInstrumentUpdatesResult> SetPointOfViewFromInstrumentUpdates(
        Api.SetPointOfViewFromInstrumentUpdatesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointOfViewFromInstrumentUpdatesOperation.Descriptor,
            SetPointOfViewFromInstrumentUpdatesOperation.CreateCommand,
            SetPointOfViewFromInstrumentUpdatesOperation.OutputContracts,
            SetPointOfViewFromInstrumentUpdatesOperation.CreateResult);

    [OperationImplementation("view_control.set_render_mode_type")]
    public override Task<Api.SetRenderModeTypeResult> SetRenderModeType(
        Api.SetRenderModeTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRenderModeTypeOperation.Descriptor,
            SetRenderModeTypeOperation.CreateCommand, SetRenderModeTypeOperation.OutputContracts,
            SetRenderModeTypeOperation.CreateResult);

    [OperationImplementation("view_control.set_sa_window_pos")]
    public override Task<Api.SetSaWindowPosResult> SetSaWindowPos(
        Api.SetSaWindowPosRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetSaWindowPosOperation.Descriptor,
            SetSaWindowPosOperation.CreateCommand, SetSaWindowPosOperation.OutputContracts,
            SetSaWindowPosOperation.CreateResult);

    [OperationImplementation("view_control.set_sa_window_size")]
    public override Task<Api.SetSaWindowSizeResult> SetSaWindowSize(
        Api.SetSaWindowSizeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetSaWindowSizeOperation.Descriptor,
            SetSaWindowSizeOperation.CreateCommand, SetSaWindowSizeOperation.OutputContracts,
            SetSaWindowSizeOperation.CreateResult);

    [OperationImplementation("view_control.set_sa_window_state")]
    public override Task<Api.SetSaWindowStateResult> SetSaWindowState(
        Api.SetSaWindowStateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetSaWindowStateOperation.Descriptor,
            SetSaWindowStateOperation.CreateCommand, SetSaWindowStateOperation.OutputContracts,
            SetSaWindowStateOperation.CreateResult);

    [OperationImplementation("view_control.set_target_labels_use_full_names")]
    public override Task<Api.SetTargetLabelsUseFullNamesResult> SetTargetLabelsUseFullNames(
        Api.SetTargetLabelsUseFullNamesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTargetLabelsUseFullNamesOperation.Descriptor,
            SetTargetLabelsUseFullNamesOperation.CreateCommand, SetTargetLabelsUseFullNamesOperation.OutputContracts,
            SetTargetLabelsUseFullNamesOperation.CreateResult);

    [OperationImplementation("view_control.set_toolkit_visibility")]
    public override Task<Api.SetToolkitVisibilityResult> SetToolkitVisibility(
        Api.SetToolkitVisibilityRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetToolkitVisibilityOperation.Descriptor,
            SetToolkitVisibilityOperation.CreateCommand, SetToolkitVisibilityOperation.OutputContracts,
            SetToolkitVisibilityOperation.CreateResult);

    [OperationImplementation("view_control.set_view_clipping_plane")]
    public override Task<Api.SetViewClippingPlaneResult> SetViewClippingPlane(
        Api.SetViewClippingPlaneRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetViewClippingPlaneOperation.Descriptor,
            SetViewClippingPlaneOperation.CreateCommand, SetViewClippingPlaneOperation.OutputContracts,
            SetViewClippingPlaneOperation.CreateResult);

    [OperationImplementation("view_control.set_working_color")]
    public override Task<Api.SetWorkingColorResult> SetWorkingColor(
        Api.SetWorkingColorRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetWorkingColorOperation.Descriptor,
            SetWorkingColorOperation.CreateCommand, SetWorkingColorOperation.OutputContracts,
            SetWorkingColorOperation.CreateResult);

    [OperationImplementation("view_control.set_working_color_auto_increment")]
    public override Task<Api.SetWorkingColorAutoIncrementResult> SetWorkingColorAutoIncrement(
        Api.SetWorkingColorAutoIncrementRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetWorkingColorAutoIncrementOperation.Descriptor,
            SetWorkingColorAutoIncrementOperation.CreateCommand, SetWorkingColorAutoIncrementOperation.OutputContracts,
            SetWorkingColorAutoIncrementOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_by_object_type")]
    public override Task<Api.ShowHideByObjectTypeResult> ShowHideByObjectType(
        Api.ShowHideByObjectTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideByObjectTypeOperation.Descriptor,
            ShowHideByObjectTypeOperation.CreateCommand, ShowHideByObjectTypeOperation.OutputContracts,
            ShowHideByObjectTypeOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_callout_view")]
    public override Task<Api.ShowHideCalloutViewResult> ShowHideCalloutView(
        Api.ShowHideCalloutViewRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideCalloutViewOperation.Descriptor,
            ShowHideCalloutViewOperation.CreateCommand, ShowHideCalloutViewOperation.OutputContracts,
            ShowHideCalloutViewOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_dimension")]
    public override Task<Api.ShowHideDimensionResult> ShowHideDimension(
        Api.ShowHideDimensionRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideDimensionOperation.Descriptor,
            ShowHideDimensionOperation.CreateCommand, ShowHideDimensionOperation.OutputContracts,
            ShowHideDimensionOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_points")]
    public override Task<Api.ShowHidePointsResult> ShowHidePoints(
        Api.ShowHidePointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHidePointsOperation.Descriptor,
            ShowHidePointsOperation.CreateCommand, ShowHidePointsOperation.OutputContracts,
            ShowHidePointsOperation.CreateResult);

    [OperationImplementation("view_control.show_by_object_type")]
    public override Task<Api.ShowByObjectTypeResult> ShowByObjectType(
        Api.ShowByObjectTypeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowByObjectTypeOperation.Descriptor,
            ShowByObjectTypeOperation.CreateCommand, ShowByObjectTypeOperation.OutputContracts,
            ShowByObjectTypeOperation.CreateResult);

    [OperationImplementation("view_control.show_items_in_tree")]
    public override Task<Api.ShowItemsInTreeResult> ShowItemsInTree(
        Api.ShowItemsInTreeRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowItemsInTreeOperation.Descriptor,
            ShowItemsInTreeOperation.CreateCommand, ShowItemsInTreeOperation.OutputContracts,
            ShowItemsInTreeOperation.CreateResult);

    [OperationImplementation("view_control.show_labels")]
    public override Task<Api.ShowLabelsResult> ShowLabels(
        Api.ShowLabelsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowLabelsOperation.Descriptor,
            ShowLabelsOperation.CreateCommand, ShowLabelsOperation.OutputContracts,
            ShowLabelsOperation.CreateResult);

    [OperationImplementation("view_control.show_objects")]
    public override Task<Api.ShowObjectsResult> ShowObjects(
        Api.ShowObjectsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowObjectsOperation.Descriptor,
            ShowObjectsOperation.CreateCommand, ShowObjectsOperation.OutputContracts,
            ShowObjectsOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_annotations_for_datums")]
    public override Task<Api.ShowHideAnnotationsForDatumsResult> ShowHideAnnotationsForDatums(
        Api.ShowHideAnnotationsForDatumsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideAnnotationsForDatumsOperation.Descriptor,
            ShowHideAnnotationsForDatumsOperation.CreateCommand, ShowHideAnnotationsForDatumsOperation.OutputContracts,
            ShowHideAnnotationsForDatumsOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_annotations_for_feature_checks")]
    public override Task<Api.ShowHideAnnotationsForFeatureChecksResult> ShowHideAnnotationsForFeatureChecks(
        Api.ShowHideAnnotationsForFeatureChecksRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideAnnotationsForFeatureChecksOperation.Descriptor,
            ShowHideAnnotationsForFeatureChecksOperation.CreateCommand, ShowHideAnnotationsForFeatureChecksOperation.OutputContracts,
            ShowHideAnnotationsForFeatureChecksOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_inspection_bar")]
    public override Task<Api.ShowHideInspectionBarResult> ShowHideInspectionBar(
        Api.ShowHideInspectionBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideInspectionBarOperation.Descriptor,
            ShowHideInspectionBarOperation.CreateCommand, ShowHideInspectionBarOperation.OutputContracts,
            ShowHideInspectionBarOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_instrument_interface")]
    public override Task<Api.ShowHideInstrumentInterfaceResult> ShowHideInstrumentInterface(
        Api.ShowHideInstrumentInterfaceRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideInstrumentInterfaceOperation.Descriptor,
            ShowHideInstrumentInterfaceOperation.CreateCommand, ShowHideInstrumentInterfaceOperation.OutputContracts,
            ShowHideInstrumentInterfaceOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_instrument_probe_tip")]
    public override Task<Api.ShowHideInstrumentProbeTipResult> ShowHideInstrumentProbeTip(
        Api.ShowHideInstrumentProbeTipRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideInstrumentProbeTipOperation.Descriptor,
            ShowHideInstrumentProbeTipOperation.CreateCommand, ShowHideInstrumentProbeTipOperation.OutputContracts,
            ShowHideInstrumentProbeTipOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_instruments")]
    public override Task<Api.ShowHideInstrumentsResult> ShowHideInstruments(
        Api.ShowHideInstrumentsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideInstrumentsOperation.Descriptor,
            ShowHideInstrumentsOperation.CreateCommand, ShowHideInstrumentsOperation.OutputContracts,
            ShowHideInstrumentsOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_relationship_report")]
    public override Task<Api.ShowHideRelationshipReportResult> ShowHideRelationshipReport(
        Api.ShowHideRelationshipReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideRelationshipReportOperation.Descriptor,
            ShowHideRelationshipReportOperation.CreateCommand, ShowHideRelationshipReportOperation.OutputContracts,
            ShowHideRelationshipReportOperation.CreateResult);

    [OperationImplementation("view_control.show_hide_relationship_watch")]
    public override Task<Api.ShowHideRelationshipWatchResult> ShowHideRelationshipWatch(
        Api.ShowHideRelationshipWatchRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowHideRelationshipWatchOperation.Descriptor,
            ShowHideRelationshipWatchOperation.CreateCommand, ShowHideRelationshipWatchOperation.OutputContracts,
            ShowHideRelationshipWatchOperation.CreateResult);

}
