using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal sealed class ReportingOperationsService(OperationExecutor executor)
    : Api.ReportingOperations.ReportingOperationsBase
{
    [OperationImplementation("reporting_operations.add_charts_to_report_bar")]
    public override Task<Api.AddChartsToReportBarResult> AddChartsToReportBar(
        Api.AddChartsToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddChartsToReportBarRequest, Api.AddChartsToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_charts_to_report_bar");

    [OperationImplementation("reporting_operations.add_custom_table_to_sa_report")]
    public override Task<Api.AddCustomTableToSaReportResult> AddCustomTableToSaReport(
        Api.AddCustomTableToSaReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddCustomTableToSaReportRequest, Api.AddCustomTableToSaReportResult>(
            executor,
            request,
            context,
            "reporting_operations.add_custom_table_to_sa_report");

    [OperationImplementation("reporting_operations.add_custom_tables_to_report_bar")]
    public override Task<Api.AddCustomTablesToReportBarResult> AddCustomTablesToReportBar(
        Api.AddCustomTablesToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddCustomTablesToReportBarRequest, Api.AddCustomTablesToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_custom_tables_to_report_bar");

    [OperationImplementation("reporting_operations.add_datums_to_report_bar")]
    public override Task<Api.AddDatumsToReportBarResult> AddDatumsToReportBar(
        Api.AddDatumsToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddDatumsToReportBarRequest, Api.AddDatumsToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_datums_to_report_bar");

    [OperationImplementation("reporting_operations.add_events_to_report_bar")]
    public override Task<Api.AddEventsToReportBarResult> AddEventsToReportBar(
        Api.AddEventsToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddEventsToReportBarRequest, Api.AddEventsToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_events_to_report_bar");

    [OperationImplementation("reporting_operations.add_feature_checks_to_report_bar")]
    public override Task<Api.AddFeatureChecksToReportBarResult> AddFeatureChecksToReportBar(
        Api.AddFeatureChecksToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddFeatureChecksToReportBarRequest, Api.AddFeatureChecksToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_feature_checks_to_report_bar");

    [OperationImplementation("reporting_operations.add_item_to_sa_report_at_location")]
    public override Task<Api.AddItemToSaReportAtLocationResult> AddItemToSaReportAtLocation(
        Api.AddItemToSaReportAtLocationRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddItemToSaReportAtLocationRequest, Api.AddItemToSaReportAtLocationResult>(
            executor,
            request,
            context,
            "reporting_operations.add_item_to_sa_report_at_location");

    [OperationImplementation("reporting_operations.add_objects_to_report_bar")]
    public override Task<Api.AddObjectsToReportBarResult> AddObjectsToReportBar(
        Api.AddObjectsToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddObjectsToReportBarRequest, Api.AddObjectsToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_objects_to_report_bar");

    [OperationImplementation("reporting_operations.add_pictures_to_report_bar")]
    public override Task<Api.AddPicturesToReportBarResult> AddPicturesToReportBar(
        Api.AddPicturesToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddPicturesToReportBarRequest, Api.AddPicturesToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_pictures_to_report_bar");

    [OperationImplementation("reporting_operations.add_relationships_to_report_bar")]
    public override Task<Api.AddRelationshipsToReportBarResult> AddRelationshipsToReportBar(
        Api.AddRelationshipsToReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddRelationshipsToReportBarRequest, Api.AddRelationshipsToReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.add_relationships_to_report_bar");

    [OperationImplementation("reporting_operations.append_items_to_sa_report")]
    public override Task<Api.AppendItemsToSaReportResult> AppendItemsToSaReport(
        Api.AppendItemsToSaReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AppendItemsToSaReportRequest, Api.AppendItemsToSaReportResult>(
            executor,
            request,
            context,
            "reporting_operations.append_items_to_sa_report");

    [OperationImplementation("reporting_operations.capture_current_view")]
    public override Task<Api.CaptureCurrentViewResult> CaptureCurrentView(
        Api.CaptureCurrentViewRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CaptureCurrentViewRequest, Api.CaptureCurrentViewResult>(
            executor,
            request,
            context,
            "reporting_operations.capture_current_view");

    [OperationImplementation("reporting_operations.capture_screen_to_file_bmp_jpg_png_gif_tiff")]
    public override Task<Api.CaptureScreenToFileBmpJpgPngGifTiffResult> CaptureScreenToFileBmpJpgPngGifTiff(
        Api.CaptureScreenToFileBmpJpgPngGifTiffRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CaptureScreenToFileBmpJpgPngGifTiffRequest, Api.CaptureScreenToFileBmpJpgPngGifTiffResult>(
            executor,
            request,
            context,
            "reporting_operations.capture_screen_to_file_bmp_jpg_png_gif_tiff");

    [OperationImplementation("reporting_operations.clear_custom_table")]
    public override Task<Api.ClearCustomTableResult> ClearCustomTable(
        Api.ClearCustomTableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ClearCustomTableRequest, Api.ClearCustomTableResult>(
            executor,
            request,
            context,
            "reporting_operations.clear_custom_table");

    [OperationImplementation("reporting_operations.close_all_reports")]
    public override Task<Api.CloseAllReportsResult> CloseAllReports(
        Api.CloseAllReportsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CloseAllReportsRequest, Api.CloseAllReportsResult>(
            executor,
            request,
            context,
            "reporting_operations.close_all_reports");

    [OperationImplementation("reporting_operations.close_html_display_board")]
    public override Task<Api.CloseHtmlDisplayBoardResult> CloseHtmlDisplayBoard(
        Api.CloseHtmlDisplayBoardRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CloseHtmlDisplayBoardRequest, Api.CloseHtmlDisplayBoardResult>(
            executor,
            request,
            context,
            "reporting_operations.close_html_display_board");

    [OperationImplementation("reporting_operations.combine_sa_reports")]
    public override Task<Api.CombineSaReportsResult> CombineSaReports(
        Api.CombineSaReportsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CombineSaReportsRequest, Api.CombineSaReportsResult>(
            executor,
            request,
            context,
            "reporting_operations.combine_sa_reports");

    [OperationImplementation("reporting_operations.create_chart_from_vector_group")]
    public override Task<Api.CreateChartFromVectorGroupResult> CreateChartFromVectorGroup(
        Api.CreateChartFromVectorGroupRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreateChartFromVectorGroupRequest, Api.CreateChartFromVectorGroupResult>(
            executor,
            request,
            context,
            "reporting_operations.create_chart_from_vector_group");

    [OperationImplementation("reporting_operations.define_report_template")]
    public override Task<Api.DefineReportTemplateResult> DefineReportTemplate(
        Api.DefineReportTemplateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DefineReportTemplateRequest, Api.DefineReportTemplateResult>(
            executor,
            request,
            context,
            "reporting_operations.define_report_template");

    [OperationImplementation("reporting_operations.delete_chart")]
    public override Task<Api.DeleteChartResult> DeleteChart(
        Api.DeleteChartRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteChartRequest, Api.DeleteChartResult>(
            executor,
            request,
            context,
            "reporting_operations.delete_chart");

    [OperationImplementation("reporting_operations.delete_custom_table")]
    public override Task<Api.DeleteCustomTableResult> DeleteCustomTable(
        Api.DeleteCustomTableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteCustomTableRequest, Api.DeleteCustomTableResult>(
            executor,
            request,
            context,
            "reporting_operations.delete_custom_table");

    [OperationImplementation("reporting_operations.delete_picture")]
    public override Task<Api.DeletePictureResult> DeletePicture(
        Api.DeletePictureRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeletePictureRequest, Api.DeletePictureResult>(
            executor,
            request,
            context,
            "reporting_operations.delete_picture");

    [OperationImplementation("reporting_operations.delete_sa_doc")]
    public override Task<Api.DeleteSaDocResult> DeleteSaDoc(
        Api.DeleteSaDocRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteSaDocRequest, Api.DeleteSaDocResult>(
            executor,
            request,
            context,
            "reporting_operations.delete_sa_doc");

    [OperationImplementation("reporting_operations.delete_sa_report")]
    public override Task<Api.DeleteSaReportResult> DeleteSaReport(
        Api.DeleteSaReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteSaReportRequest, Api.DeleteSaReportResult>(
            executor,
            request,
            context,
            "reporting_operations.delete_sa_report");

    [OperationImplementation("reporting_operations.delete_sa_report_template")]
    public override Task<Api.DeleteSaReportTemplateResult> DeleteSaReportTemplate(
        Api.DeleteSaReportTemplateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteSaReportTemplateRequest, Api.DeleteSaReportTemplateResult>(
            executor,
            request,
            context,
            "reporting_operations.delete_sa_report_template");

    [OperationImplementation("reporting_operations.generate_quick_report_from_tab_order")]
    public override Task<Api.GenerateQuickReportFromTabOrderResult> GenerateQuickReportFromTabOrder(
        Api.GenerateQuickReportFromTabOrderRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GenerateQuickReportFromTabOrderRequest, Api.GenerateQuickReportFromTabOrderResult>(
            executor,
            request,
            context,
            "reporting_operations.generate_quick_report_from_tab_order");

    [OperationImplementation("reporting_operations.generate_standard_html_report")]
    public override Task<Api.GenerateStandardHtmlReportResult> GenerateStandardHtmlReport(
        Api.GenerateStandardHtmlReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GenerateStandardHtmlReportRequest, Api.GenerateStandardHtmlReportResult>(
            executor,
            request,
            context,
            "reporting_operations.generate_standard_html_report");

    [OperationImplementation("reporting_operations.generate_update_templated_report")]
    public override Task<Api.GenerateUpdateTemplatedReportResult> GenerateUpdateTemplatedReport(
        Api.GenerateUpdateTemplatedReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GenerateUpdateTemplatedReportRequest, Api.GenerateUpdateTemplatedReportResult>(
            executor,
            request,
            context,
            "reporting_operations.generate_update_templated_report");

    [OperationImplementation("reporting_operations.get_custom_table_cell_double")]
    public override Task<Api.GetCustomTableCellDoubleResult> GetCustomTableCellDouble(
        Api.GetCustomTableCellDoubleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCustomTableCellDoubleRequest, Api.GetCustomTableCellDoubleResult>(
            executor,
            request,
            context,
            "reporting_operations.get_custom_table_cell_double");

    [OperationImplementation("reporting_operations.get_custom_table_cell_string")]
    public override Task<Api.GetCustomTableCellStringResult> GetCustomTableCellString(
        Api.GetCustomTableCellStringRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetCustomTableCellStringRequest, Api.GetCustomTableCellStringResult>(
            executor,
            request,
            context,
            "reporting_operations.get_custom_table_cell_string");

    [OperationImplementation("reporting_operations.get_defined_report_tags")]
    public override Task<Api.GetDefinedReportTagsResult> GetDefinedReportTags(
        Api.GetDefinedReportTagsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetDefinedReportTagsRequest, Api.GetDefinedReportTagsResult>(
            executor,
            request,
            context,
            "reporting_operations.get_defined_report_tags");

    [OperationImplementation("reporting_operations.get_report_tag_value")]
    public override Task<Api.GetReportTagValueResult> GetReportTagValue(
        Api.GetReportTagValueRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetReportTagValueRequest, Api.GetReportTagValueResult>(
            executor,
            request,
            context,
            "reporting_operations.get_report_tag_value");

    [OperationImplementation("reporting_operations.html_display_board")]
    public override Task<Api.HtmlDisplayBoardResult> HtmlDisplayBoard(
        Api.HtmlDisplayBoardRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.HtmlDisplayBoardRequest, Api.HtmlDisplayBoardResult>(
            executor,
            request,
            context,
            "reporting_operations.html_display_board");

    [OperationImplementation("reporting_operations.make_custom_table")]
    public override Task<Api.MakeCustomTableResult> MakeCustomTable(
        Api.MakeCustomTableRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeCustomTableRequest, Api.MakeCustomTableResult>(
            executor,
            request,
            context,
            "reporting_operations.make_custom_table");

    [OperationImplementation("reporting_operations.make_new_sa_report")]
    public override Task<Api.MakeNewSaReportResult> MakeNewSaReport(
        Api.MakeNewSaReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeNewSaReportRequest, Api.MakeNewSaReportResult>(
            executor,
            request,
            context,
            "reporting_operations.make_new_sa_report");

    [OperationImplementation("reporting_operations.make_utility_chart")]
    public override Task<Api.MakeUtilityChartResult> MakeUtilityChart(
        Api.MakeUtilityChartRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeUtilityChartRequest, Api.MakeUtilityChartResult>(
            executor,
            request,
            context,
            "reporting_operations.make_utility_chart");

    [OperationImplementation("reporting_operations.notify_user_double")]
    public override Task<Api.NotifyUserDoubleResult> NotifyUserDouble(
        Api.NotifyUserDoubleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.NotifyUserDoubleRequest, Api.NotifyUserDoubleResult>(
            executor,
            request,
            context,
            "reporting_operations.notify_user_double");

    [OperationImplementation("reporting_operations.notify_user_html")]
    public override Task<Api.NotifyUserHtmlResult> NotifyUserHtml(
        Api.NotifyUserHtmlRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.NotifyUserHtmlRequest, Api.NotifyUserHtmlResult>(
            executor,
            request,
            context,
            "reporting_operations.notify_user_html");

    [OperationImplementation("reporting_operations.notify_user_integer")]
    public override Task<Api.NotifyUserIntegerResult> NotifyUserInteger(
        Api.NotifyUserIntegerRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.NotifyUserIntegerRequest, Api.NotifyUserIntegerResult>(
            executor,
            request,
            context,
            "reporting_operations.notify_user_integer");

    [OperationImplementation("reporting_operations.notify_user_text_array")]
    public override Task<Api.NotifyUserTextArrayResult> NotifyUserTextArray(
        Api.NotifyUserTextArrayRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.NotifyUserTextArrayRequest, Api.NotifyUserTextArrayResult>(
            executor,
            request,
            context,
            "reporting_operations.notify_user_text_array");

    [OperationImplementation("reporting_operations.output_sa_report_to_excel")]
    public override Task<Api.OutputSaReportToExcelResult> OutputSaReportToExcel(
        Api.OutputSaReportToExcelRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.OutputSaReportToExcelRequest, Api.OutputSaReportToExcelResult>(
            executor,
            request,
            context,
            "reporting_operations.output_sa_report_to_excel");

    [OperationImplementation("reporting_operations.output_sa_report_to_pdf")]
    public override Task<Api.OutputSaReportToPdfResult> OutputSaReportToPdf(
        Api.OutputSaReportToPdfRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.OutputSaReportToPdfRequest, Api.OutputSaReportToPdfResult>(
            executor,
            request,
            context,
            "reporting_operations.output_sa_report_to_pdf");

    [OperationImplementation("reporting_operations.quick_report")]
    public override Task<Api.QuickReportResult> QuickReport(
        Api.QuickReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.QuickReportRequest, Api.QuickReportResult>(
            executor,
            request,
            context,
            "reporting_operations.quick_report");

    [OperationImplementation("reporting_operations.refresh_callout_views_in_sa_report")]
    public override Task<Api.RefreshCalloutViewsInSaReportResult> RefreshCalloutViewsInSaReport(
        Api.RefreshCalloutViewsInSaReportRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RefreshCalloutViewsInSaReportRequest, Api.RefreshCalloutViewsInSaReportResult>(
            executor,
            request,
            context,
            "reporting_operations.refresh_callout_views_in_sa_report");

    [OperationImplementation("reporting_operations.refresh_report_bar")]
    public override Task<Api.RefreshReportBarResult> RefreshReportBar(
        Api.RefreshReportBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RefreshReportBarRequest, Api.RefreshReportBarResult>(
            executor,
            request,
            context,
            "reporting_operations.refresh_report_bar");

    [OperationImplementation("reporting_operations.remove_report_tag")]
    public override Task<Api.RemoveReportTagResult> RemoveReportTag(
        Api.RemoveReportTagRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RemoveReportTagRequest, Api.RemoveReportTagResult>(
            executor,
            request,
            context,
            "reporting_operations.remove_report_tag");

    [OperationImplementation("reporting_operations.rename_picture")]
    public override Task<Api.RenamePictureResult> RenamePicture(
        Api.RenamePictureRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RenamePictureRequest, Api.RenamePictureResult>(
            executor,
            request,
            context,
            "reporting_operations.rename_picture");

    [OperationImplementation("reporting_operations.save_chart_to_jpeg_file")]
    public override Task<Api.SaveChartToJPegFileResult> SaveChartToJPegFile(
        Api.SaveChartToJPegFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SaveChartToJPegFileRequest, Api.SaveChartToJPegFileResult>(
            executor,
            request,
            context,
            "reporting_operations.save_chart_to_jpeg_file");

    [OperationImplementation("reporting_operations.save_current_view_bmp_jpg_png_gif_tiff")]
    public override Task<Api.SaveCurrentViewBmpJpgPngGifTiffResult> SaveCurrentViewBmpJpgPngGifTiff(
        Api.SaveCurrentViewBmpJpgPngGifTiffRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SaveCurrentViewBmpJpgPngGifTiffRequest, Api.SaveCurrentViewBmpJpgPngGifTiffResult>(
            executor,
            request,
            context,
            "reporting_operations.save_current_view_bmp_jpg_png_gif_tiff");

    [OperationImplementation("reporting_operations.set_custom_table_cell_color")]
    public override Task<Api.SetCustomTableCellColorResult> SetCustomTableCellColor(
        Api.SetCustomTableCellColorRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCustomTableCellColorRequest, Api.SetCustomTableCellColorResult>(
            executor,
            request,
            context,
            "reporting_operations.set_custom_table_cell_color");

    [OperationImplementation("reporting_operations.set_custom_table_cell_double")]
    public override Task<Api.SetCustomTableCellDoubleResult> SetCustomTableCellDouble(
        Api.SetCustomTableCellDoubleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCustomTableCellDoubleRequest, Api.SetCustomTableCellDoubleResult>(
            executor,
            request,
            context,
            "reporting_operations.set_custom_table_cell_double");

    [OperationImplementation("reporting_operations.set_custom_table_cell_font")]
    public override Task<Api.SetCustomTableCellFontResult> SetCustomTableCellFont(
        Api.SetCustomTableCellFontRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCustomTableCellFontRequest, Api.SetCustomTableCellFontResult>(
            executor,
            request,
            context,
            "reporting_operations.set_custom_table_cell_font");

    [OperationImplementation("reporting_operations.set_custom_table_cell_string")]
    public override Task<Api.SetCustomTableCellStringResult> SetCustomTableCellString(
        Api.SetCustomTableCellStringRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCustomTableCellStringRequest, Api.SetCustomTableCellStringResult>(
            executor,
            request,
            context,
            "reporting_operations.set_custom_table_cell_string");

    [OperationImplementation("reporting_operations.set_custom_table_header_cell")]
    public override Task<Api.SetCustomTableHeaderCellResult> SetCustomTableHeaderCell(
        Api.SetCustomTableHeaderCellRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCustomTableHeaderCellRequest, Api.SetCustomTableHeaderCellResult>(
            executor,
            request,
            context,
            "reporting_operations.set_custom_table_header_cell");

    [OperationImplementation("reporting_operations.set_custom_table_header_row")]
    public override Task<Api.SetCustomTableHeaderRowResult> SetCustomTableHeaderRow(
        Api.SetCustomTableHeaderRowRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCustomTableHeaderRowRequest, Api.SetCustomTableHeaderRowResult>(
            executor,
            request,
            context,
            "reporting_operations.set_custom_table_header_row");

    [OperationImplementation("reporting_operations.set_custom_table_title")]
    public override Task<Api.SetCustomTableTitleResult> SetCustomTableTitle(
        Api.SetCustomTableTitleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCustomTableTitleRequest, Api.SetCustomTableTitleResult>(
            executor,
            request,
            context,
            "reporting_operations.set_custom_table_title");

    [OperationImplementation("reporting_operations.set_point_group_report_options")]
    public override Task<Api.SetPointGroupReportOptionsResult> SetPointGroupReportOptions(
        Api.SetPointGroupReportOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetPointGroupReportOptionsRequest, Api.SetPointGroupReportOptionsResult>(
            executor,
            request,
            context,
            "reporting_operations.set_point_group_report_options");

    [OperationImplementation("reporting_operations.set_relationship_report_options")]
    public override Task<Api.SetRelationshipReportOptionsResult> SetRelationshipReportOptions(
        Api.SetRelationshipReportOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetRelationshipReportOptionsRequest, Api.SetRelationshipReportOptionsResult>(
            executor,
            request,
            context,
            "reporting_operations.set_relationship_report_options");

    [OperationImplementation("reporting_operations.set_report_bar_visibility")]
    public override Task<Api.SetReportBarVisibilityResult> SetReportBarVisibility(
        Api.SetReportBarVisibilityRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetReportBarVisibilityRequest, Api.SetReportBarVisibilityResult>(
            executor,
            request,
            context,
            "reporting_operations.set_report_bar_visibility");

    [OperationImplementation("reporting_operations.set_report_options_for_object")]
    public override Task<Api.SetReportOptionsForObjectResult> SetReportOptionsForObject(
        Api.SetReportOptionsForObjectRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetReportOptionsForObjectRequest, Api.SetReportOptionsForObjectResult>(
            executor,
            request,
            context,
            "reporting_operations.set_report_options_for_object");

    [OperationImplementation("reporting_operations.set_report_tag_value_from_double")]
    public override Task<Api.SetReportTagValueFromDoubleResult> SetReportTagValueFromDouble(
        Api.SetReportTagValueFromDoubleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetReportTagValueFromDoubleRequest, Api.SetReportTagValueFromDoubleResult>(
            executor,
            request,
            context,
            "reporting_operations.set_report_tag_value_from_double");

    [OperationImplementation("reporting_operations.set_report_tag_value_from_integer")]
    public override Task<Api.SetReportTagValueFromIntegerResult> SetReportTagValueFromInteger(
        Api.SetReportTagValueFromIntegerRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetReportTagValueFromIntegerRequest, Api.SetReportTagValueFromIntegerResult>(
            executor,
            request,
            context,
            "reporting_operations.set_report_tag_value_from_integer");

    [OperationImplementation("reporting_operations.set_report_tag_value_from_string")]
    public override Task<Api.SetReportTagValueFromStringResult> SetReportTagValueFromString(
        Api.SetReportTagValueFromStringRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetReportTagValueFromStringRequest, Api.SetReportTagValueFromStringResult>(
            executor,
            request,
            context,
            "reporting_operations.set_report_tag_value_from_string");

    [OperationImplementation("reporting_operations.set_scale_for_picture")]
    public override Task<Api.SetScaleForPictureResult> SetScaleForPicture(
        Api.SetScaleForPictureRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetScaleForPictureRequest, Api.SetScaleForPictureResult>(
            executor,
            request,
            context,
            "reporting_operations.set_scale_for_picture");

    [OperationImplementation("reporting_operations.set_vector_group_report_options")]
    public override Task<Api.SetVectorGroupReportOptionsResult> SetVectorGroupReportOptions(
        Api.SetVectorGroupReportOptionsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorGroupReportOptionsRequest, Api.SetVectorGroupReportOptionsResult>(
            executor,
            request,
            context,
            "reporting_operations.set_vector_group_report_options");

}
