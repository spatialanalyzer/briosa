using Briosa.Server.Operations.WaveA;
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
        MpOperationServiceExecutor.ExecuteAsync<Api.AutoScaleRequest, Api.AutoScaleResult>(
            executor,
            request,
            context,
            "view_control.auto_scale");

    [OperationImplementation("view_control.center_graphics_about_objects")]
    public override Task<Api.CenterGraphicsAboutObjectsResult> CenterGraphicsAboutObjects(
        Api.CenterGraphicsAboutObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CenterGraphicsAboutObjectsRequest, Api.CenterGraphicsAboutObjectsResult>(
            executor,
            request,
            context,
            "view_control.center_graphics_about_objects");

    [OperationImplementation("view_control.center_graphics_about_point")]
    public override Task<Api.CenterGraphicsAboutPointResult> CenterGraphicsAboutPoint(
        Api.CenterGraphicsAboutPointRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CenterGraphicsAboutPointRequest, Api.CenterGraphicsAboutPointResult>(
            executor,
            request,
            context,
            "view_control.center_graphics_about_point");

    [OperationImplementation("view_control.define_point_of_view")]
    public override Task<Api.DefinePointOfViewResult> DefinePointOfView(
        Api.DefinePointOfViewRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DefinePointOfViewRequest, Api.DefinePointOfViewResult>(
            executor,
            request,
            context,
            "view_control.define_point_of_view");

    [OperationImplementation("view_control.get_active_clipping_planes")]
    public override Task<Api.GetActiveClippingPlanesResult> GetActiveClippingPlanes(
        Api.GetActiveClippingPlanesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetActiveClippingPlanesRequest, Api.GetActiveClippingPlanesResult>(
            executor,
            request,
            context,
            "view_control.get_active_clipping_planes");

    [OperationImplementation("view_control.get_point_of_view_parameters")]
    public override Task<Api.GetPointOfViewParametersResult> GetPointOfViewParameters(
        Api.GetPointOfViewParametersRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetPointOfViewParametersRequest, Api.GetPointOfViewParametersResult>(
            executor,
            request,
            context,
            "view_control.get_point_of_view_parameters");

    [OperationImplementation("view_control.hide_all_callout_views")]
    public override Task<Api.HideAllCalloutViewsResult> HideAllCalloutViews(
        Api.HideAllCalloutViewsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.HideAllCalloutViewsRequest, Api.HideAllCalloutViewsResult>(
            executor,
            request,
            context,
            "view_control.hide_all_callout_views");

    [OperationImplementation("view_control.hide_objects")]
    public override Task<Api.HideObjectsResult> HideObjects(
        Api.HideObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.HideObjectsRequest, Api.HideObjectsResult>(
            executor,
            request,
            context,
            "view_control.hide_objects");

    [OperationImplementation("view_control.highlight_objects")]
    public override Task<Api.HighlightObjectsResult> HighlightObjects(
        Api.HighlightObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.HighlightObjectsRequest, Api.HighlightObjectsResult>(
            executor,
            request,
            context,
            "view_control.highlight_objects");

    [OperationImplementation("view_control.highlight_point")]
    public override Task<Api.HighlightPointResult> HighlightPoint(
        Api.HighlightPointRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.HighlightPointRequest, Api.HighlightPointResult>(
            executor,
            request,
            context,
            "view_control.highlight_point");

    [OperationImplementation("view_control.highlight_relationships")]
    public override Task<Api.HighlightRelationshipsResult> HighlightRelationships(
        Api.HighlightRelationshipsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.HighlightRelationshipsRequest, Api.HighlightRelationshipsResult>(
            executor,
            request,
            context,
            "view_control.highlight_relationships");

    [OperationImplementation("view_control.load_ribbon_bar_from_xml_file")]
    public override Task<Api.LoadRibbonBarFromXmlFileResult> LoadRibbonBarFromXmlFile(
        Api.LoadRibbonBarFromXmlFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LoadRibbonBarFromXmlFileRequest, Api.LoadRibbonBarFromXmlFileResult>(
            executor,
            request,
            context,
            "view_control.load_ribbon_bar_from_xml_file");

    [OperationImplementation("view_control.refresh_views")]
    public override Task<Api.RefreshViewsResult> RefreshViews(
        Api.RefreshViewsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RefreshViewsRequest, Api.RefreshViewsResult>(
            executor,
            request,
            context,
            "view_control.refresh_views");

    [OperationImplementation("view_control.reset_ribbon_bar_to_default")]
    public override Task<Api.ResetRibbonBarToDefaultResult> ResetRibbonBarToDefault(
        Api.ResetRibbonBarToDefaultRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ResetRibbonBarToDefaultRequest, Api.ResetRibbonBarToDefaultResult>(
            executor,
            request,
            context,
            "view_control.reset_ribbon_bar_to_default");

    [OperationImplementation("view_control.save_point_of_view")]
    public override Task<Api.SavePointOfViewResult> SavePointOfView(
        Api.SavePointOfViewRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SavePointOfViewRequest, Api.SavePointOfViewResult>(
            executor,
            request,
            context,
            "view_control.save_point_of_view");

    [OperationImplementation("view_control.set_background_color")]
    public override Task<Api.SetBackgroundColorResult> SetBackgroundColor(
        Api.SetBackgroundColorRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetBackgroundColorRequest, Api.SetBackgroundColorResult>(
            executor,
            request,
            context,
            "view_control.set_background_color");

    [OperationImplementation("view_control.set_mp_window_state")]
    public override Task<Api.SetMpWindowStateResult> SetMpWindowState(
        Api.SetMpWindowStateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetMpWindowStateRequest, Api.SetMpWindowStateResult>(
            executor,
            request,
            context,
            "view_control.set_mp_window_state");

    [OperationImplementation("view_control.set_objects_color")]
    public override Task<Api.SetObjectsColorResult> SetObjectsColor(
        Api.SetObjectsColorRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObjectsColorRequest, Api.SetObjectsColorResult>(
            executor,
            request,
            context,
            "view_control.set_objects_color");

    [OperationImplementation("view_control.set_objects_translucency")]
    public override Task<Api.SetObjectsTranslucencyResult> SetObjectsTranslucency(
        Api.SetObjectsTranslucencyRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetObjectsTranslucencyRequest, Api.SetObjectsTranslucencyResult>(
            executor,
            request,
            context,
            "view_control.set_objects_translucency");

    [OperationImplementation("view_control.set_point_of_view")]
    public override Task<Api.SetPointOfViewResult> SetPointOfView(
        Api.SetPointOfViewRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointOfViewRequest, Api.SetPointOfViewResult>(
            executor,
            request,
            context,
            "view_control.set_point_of_view");

    [OperationImplementation("view_control.set_point_of_view_from_frame")]
    public override Task<Api.SetPointOfViewFromFrameResult> SetPointOfViewFromFrame(
        Api.SetPointOfViewFromFrameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointOfViewFromFrameRequest, Api.SetPointOfViewFromFrameResult>(
            executor,
            request,
            context,
            "view_control.set_point_of_view_from_frame");

    [OperationImplementation("view_control.set_point_of_view_from_instrument_updates")]
    public override Task<Api.SetPointOfViewFromInstrumentUpdatesResult> SetPointOfViewFromInstrumentUpdates(
        Api.SetPointOfViewFromInstrumentUpdatesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointOfViewFromInstrumentUpdatesRequest, Api.SetPointOfViewFromInstrumentUpdatesResult>(
            executor,
            request,
            context,
            "view_control.set_point_of_view_from_instrument_updates");

    [OperationImplementation("view_control.set_render_mode_type")]
    public override Task<Api.SetRenderModeTypeResult> SetRenderModeType(
        Api.SetRenderModeTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRenderModeTypeRequest, Api.SetRenderModeTypeResult>(
            executor,
            request,
            context,
            "view_control.set_render_mode_type");

    [OperationImplementation("view_control.set_sa_window_pos")]
    public override Task<Api.SetSaWindowPosResult> SetSaWindowPos(
        Api.SetSaWindowPosRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetSaWindowPosRequest, Api.SetSaWindowPosResult>(
            executor,
            request,
            context,
            "view_control.set_sa_window_pos");

    [OperationImplementation("view_control.set_sa_window_size")]
    public override Task<Api.SetSaWindowSizeResult> SetSaWindowSize(
        Api.SetSaWindowSizeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetSaWindowSizeRequest, Api.SetSaWindowSizeResult>(
            executor,
            request,
            context,
            "view_control.set_sa_window_size");

    [OperationImplementation("view_control.set_sa_window_state")]
    public override Task<Api.SetSaWindowStateResult> SetSaWindowState(
        Api.SetSaWindowStateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetSaWindowStateRequest, Api.SetSaWindowStateResult>(
            executor,
            request,
            context,
            "view_control.set_sa_window_state");

    [OperationImplementation("view_control.set_target_labels_use_full_names")]
    public override Task<Api.SetTargetLabelsUseFullNamesResult> SetTargetLabelsUseFullNames(
        Api.SetTargetLabelsUseFullNamesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTargetLabelsUseFullNamesRequest, Api.SetTargetLabelsUseFullNamesResult>(
            executor,
            request,
            context,
            "view_control.set_target_labels_use_full_names");

    [OperationImplementation("view_control.set_toolkit_visibility")]
    public override Task<Api.SetToolkitVisibilityResult> SetToolkitVisibility(
        Api.SetToolkitVisibilityRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetToolkitVisibilityRequest, Api.SetToolkitVisibilityResult>(
            executor,
            request,
            context,
            "view_control.set_toolkit_visibility");

    [OperationImplementation("view_control.set_view_clipping_plane")]
    public override Task<Api.SetViewClippingPlaneResult> SetViewClippingPlane(
        Api.SetViewClippingPlaneRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetViewClippingPlaneRequest, Api.SetViewClippingPlaneResult>(
            executor,
            request,
            context,
            "view_control.set_view_clipping_plane");

    [OperationImplementation("view_control.set_working_color")]
    public override Task<Api.SetWorkingColorResult> SetWorkingColor(
        Api.SetWorkingColorRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetWorkingColorRequest, Api.SetWorkingColorResult>(
            executor,
            request,
            context,
            "view_control.set_working_color");

    [OperationImplementation("view_control.set_working_color_auto_increment")]
    public override Task<Api.SetWorkingColorAutoIncrementResult> SetWorkingColorAutoIncrement(
        Api.SetWorkingColorAutoIncrementRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetWorkingColorAutoIncrementRequest, Api.SetWorkingColorAutoIncrementResult>(
            executor,
            request,
            context,
            "view_control.set_working_color_auto_increment");

    [OperationImplementation("view_control.show_hide_by_object_type")]
    public override Task<Api.ShowHideByObjectTypeResult> ShowHideByObjectType(
        Api.ShowHideByObjectTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideByObjectTypeRequest, Api.ShowHideByObjectTypeResult>(
            executor,
            request,
            context,
            "view_control.show_hide_by_object_type");

    [OperationImplementation("view_control.show_hide_callout_view")]
    public override Task<Api.ShowHideCalloutViewResult> ShowHideCalloutView(
        Api.ShowHideCalloutViewRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideCalloutViewRequest, Api.ShowHideCalloutViewResult>(
            executor,
            request,
            context,
            "view_control.show_hide_callout_view");

    [OperationImplementation("view_control.show_hide_dimension")]
    public override Task<Api.ShowHideDimensionResult> ShowHideDimension(
        Api.ShowHideDimensionRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideDimensionRequest, Api.ShowHideDimensionResult>(
            executor,
            request,
            context,
            "view_control.show_hide_dimension");

    [OperationImplementation("view_control.show_hide_points")]
    public override Task<Api.ShowHidePointsResult> ShowHidePoints(
        Api.ShowHidePointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHidePointsRequest, Api.ShowHidePointsResult>(
            executor,
            request,
            context,
            "view_control.show_hide_points");

    [OperationImplementation("view_control.show_by_object_type")]
    public override Task<Api.ShowByObjectTypeResult> ShowByObjectType(
        Api.ShowByObjectTypeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowByObjectTypeRequest, Api.ShowByObjectTypeResult>(
            executor,
            request,
            context,
            "view_control.show_by_object_type");

    [OperationImplementation("view_control.show_items_in_tree")]
    public override Task<Api.ShowItemsInTreeResult> ShowItemsInTree(
        Api.ShowItemsInTreeRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowItemsInTreeRequest, Api.ShowItemsInTreeResult>(
            executor,
            request,
            context,
            "view_control.show_items_in_tree");

    [OperationImplementation("view_control.show_labels")]
    public override Task<Api.ShowLabelsResult> ShowLabels(
        Api.ShowLabelsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowLabelsRequest, Api.ShowLabelsResult>(
            executor,
            request,
            context,
            "view_control.show_labels");

    [OperationImplementation("view_control.show_objects")]
    public override Task<Api.ShowObjectsResult> ShowObjects(
        Api.ShowObjectsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowObjectsRequest, Api.ShowObjectsResult>(
            executor,
            request,
            context,
            "view_control.show_objects");

    [OperationImplementation("view_control.show_hide_annotations_for_datums")]
    public override Task<Api.ShowHideAnnotationsForDatumsResult> ShowHideAnnotationsForDatums(
        Api.ShowHideAnnotationsForDatumsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideAnnotationsForDatumsRequest, Api.ShowHideAnnotationsForDatumsResult>(
            executor,
            request,
            context,
            "view_control.show_hide_annotations_for_datums");

    [OperationImplementation("view_control.show_hide_annotations_for_feature_checks")]
    public override Task<Api.ShowHideAnnotationsForFeatureChecksResult> ShowHideAnnotationsForFeatureChecks(
        Api.ShowHideAnnotationsForFeatureChecksRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideAnnotationsForFeatureChecksRequest, Api.ShowHideAnnotationsForFeatureChecksResult>(
            executor,
            request,
            context,
            "view_control.show_hide_annotations_for_feature_checks");

    [OperationImplementation("view_control.show_hide_inspection_bar")]
    public override Task<Api.ShowHideInspectionBarResult> ShowHideInspectionBar(
        Api.ShowHideInspectionBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideInspectionBarRequest, Api.ShowHideInspectionBarResult>(
            executor,
            request,
            context,
            "view_control.show_hide_inspection_bar");

    [OperationImplementation("view_control.show_hide_instrument_interface")]
    public override Task<Api.ShowHideInstrumentInterfaceResult> ShowHideInstrumentInterface(
        Api.ShowHideInstrumentInterfaceRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideInstrumentInterfaceRequest, Api.ShowHideInstrumentInterfaceResult>(
            executor,
            request,
            context,
            "view_control.show_hide_instrument_interface");

    [OperationImplementation("view_control.show_hide_instrument_probe_tip")]
    public override Task<Api.ShowHideInstrumentProbeTipResult> ShowHideInstrumentProbeTip(
        Api.ShowHideInstrumentProbeTipRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideInstrumentProbeTipRequest, Api.ShowHideInstrumentProbeTipResult>(
            executor,
            request,
            context,
            "view_control.show_hide_instrument_probe_tip");

    [OperationImplementation("view_control.show_hide_instruments")]
    public override Task<Api.ShowHideInstrumentsResult> ShowHideInstruments(
        Api.ShowHideInstrumentsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideInstrumentsRequest, Api.ShowHideInstrumentsResult>(
            executor,
            request,
            context,
            "view_control.show_hide_instruments");

    [OperationImplementation("view_control.show_hide_relationship_report")]
    public override Task<Api.ShowHideRelationshipReportResult> ShowHideRelationshipReport(
        Api.ShowHideRelationshipReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideRelationshipReportRequest, Api.ShowHideRelationshipReportResult>(
            executor,
            request,
            context,
            "view_control.show_hide_relationship_report");

    [OperationImplementation("view_control.show_hide_relationship_watch")]
    public override Task<Api.ShowHideRelationshipWatchResult> ShowHideRelationshipWatch(
        Api.ShowHideRelationshipWatchRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowHideRelationshipWatchRequest, Api.ShowHideRelationshipWatchResult>(
            executor,
            request,
            context,
            "view_control.show_hide_relationship_watch");

}
