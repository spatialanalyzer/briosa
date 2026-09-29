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
        executor.ExecuteAsync(request, context, AddChartsToReportBarOperation.Descriptor,
            AddChartsToReportBarOperation.CreateCommand, AddChartsToReportBarOperation.OutputContracts,
            AddChartsToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_custom_table_to_sa_report")]
    public override Task<Api.AddCustomTableToSaReportResult> AddCustomTableToSaReport(
        Api.AddCustomTableToSaReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddCustomTableToSaReportOperation.Descriptor,
            AddCustomTableToSaReportOperation.CreateCommand, AddCustomTableToSaReportOperation.OutputContracts,
            AddCustomTableToSaReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_custom_tables_to_report_bar")]
    public override Task<Api.AddCustomTablesToReportBarResult> AddCustomTablesToReportBar(
        Api.AddCustomTablesToReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddCustomTablesToReportBarOperation.Descriptor,
            AddCustomTablesToReportBarOperation.CreateCommand, AddCustomTablesToReportBarOperation.OutputContracts,
            AddCustomTablesToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_datums_to_report_bar")]
    public override Task<Api.AddDatumsToReportBarResult> AddDatumsToReportBar(
        Api.AddDatumsToReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddDatumsToReportBarOperation.Descriptor,
            AddDatumsToReportBarOperation.CreateCommand, AddDatumsToReportBarOperation.OutputContracts,
            AddDatumsToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_events_to_report_bar")]
    public override Task<Api.AddEventsToReportBarResult> AddEventsToReportBar(
        Api.AddEventsToReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddEventsToReportBarOperation.Descriptor,
            AddEventsToReportBarOperation.CreateCommand, AddEventsToReportBarOperation.OutputContracts,
            AddEventsToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_feature_checks_to_report_bar")]
    public override Task<Api.AddFeatureChecksToReportBarResult> AddFeatureChecksToReportBar(
        Api.AddFeatureChecksToReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddFeatureChecksToReportBarOperation.Descriptor,
            AddFeatureChecksToReportBarOperation.CreateCommand, AddFeatureChecksToReportBarOperation.OutputContracts,
            AddFeatureChecksToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_item_to_sa_report_at_location")]
    public override Task<Api.AddItemToSaReportAtLocationResult> AddItemToSaReportAtLocation(
        Api.AddItemToSaReportAtLocationRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddItemToSaReportAtLocationOperation.Descriptor,
            AddItemToSaReportAtLocationOperation.CreateCommand, AddItemToSaReportAtLocationOperation.OutputContracts,
            AddItemToSaReportAtLocationOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_objects_to_report_bar")]
    public override Task<Api.AddObjectsToReportBarResult> AddObjectsToReportBar(
        Api.AddObjectsToReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddObjectsToReportBarOperation.Descriptor,
            AddObjectsToReportBarOperation.CreateCommand, AddObjectsToReportBarOperation.OutputContracts,
            AddObjectsToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_pictures_to_report_bar")]
    public override Task<Api.AddPicturesToReportBarResult> AddPicturesToReportBar(
        Api.AddPicturesToReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddPicturesToReportBarOperation.Descriptor,
            AddPicturesToReportBarOperation.CreateCommand, AddPicturesToReportBarOperation.OutputContracts,
            AddPicturesToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.add_relationships_to_report_bar")]
    public override Task<Api.AddRelationshipsToReportBarResult> AddRelationshipsToReportBar(
        Api.AddRelationshipsToReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AddRelationshipsToReportBarOperation.Descriptor,
            AddRelationshipsToReportBarOperation.CreateCommand, AddRelationshipsToReportBarOperation.OutputContracts,
            AddRelationshipsToReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.append_items_to_sa_report")]
    public override Task<Api.AppendItemsToSaReportResult> AppendItemsToSaReport(
        Api.AppendItemsToSaReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, AppendItemsToSaReportOperation.Descriptor,
            AppendItemsToSaReportOperation.CreateCommand, AppendItemsToSaReportOperation.OutputContracts,
            AppendItemsToSaReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.capture_current_view")]
    public override Task<Api.CaptureCurrentViewResult> CaptureCurrentView(
        Api.CaptureCurrentViewRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CaptureCurrentViewOperation.Descriptor,
            CaptureCurrentViewOperation.CreateCommand, CaptureCurrentViewOperation.OutputContracts,
            CaptureCurrentViewOperation.CreateResult);

    [OperationImplementation("reporting_operations.capture_screen_to_file_bmp_jpg_png_gif_tiff")]
    public override Task<Api.CaptureScreenToFileBmpJpgPngGifTiffResult> CaptureScreenToFileBmpJpgPngGifTiff(
        Api.CaptureScreenToFileBmpJpgPngGifTiffRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CaptureScreenToFileBmpJpgPngGifTiffOperation.Descriptor,
            CaptureScreenToFileBmpJpgPngGifTiffOperation.CreateCommand,
            CaptureScreenToFileBmpJpgPngGifTiffOperation.OutputContracts,
            CaptureScreenToFileBmpJpgPngGifTiffOperation.CreateResult);

    [OperationImplementation("reporting_operations.clear_custom_table")]
    public override Task<Api.ClearCustomTableResult> ClearCustomTable(
        Api.ClearCustomTableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ClearCustomTableOperation.Descriptor,
            ClearCustomTableOperation.CreateCommand, ClearCustomTableOperation.OutputContracts,
            ClearCustomTableOperation.CreateResult);

    [OperationImplementation("reporting_operations.close_all_reports")]
    public override Task<Api.CloseAllReportsResult> CloseAllReports(
        Api.CloseAllReportsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CloseAllReportsOperation.Descriptor,
            CloseAllReportsOperation.CreateCommand, CloseAllReportsOperation.OutputContracts,
            CloseAllReportsOperation.CreateResult);

    [OperationImplementation("reporting_operations.close_html_display_board")]
    public override Task<Api.CloseHtmlDisplayBoardResult> CloseHtmlDisplayBoard(
        Api.CloseHtmlDisplayBoardRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CloseHtmlDisplayBoardOperation.Descriptor,
            CloseHtmlDisplayBoardOperation.CreateCommand, CloseHtmlDisplayBoardOperation.OutputContracts,
            CloseHtmlDisplayBoardOperation.CreateResult);

    [OperationImplementation("reporting_operations.combine_sa_reports")]
    public override Task<Api.CombineSaReportsResult> CombineSaReports(
        Api.CombineSaReportsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CombineSaReportsOperation.Descriptor,
            CombineSaReportsOperation.CreateCommand, CombineSaReportsOperation.OutputContracts,
            CombineSaReportsOperation.CreateResult);

    [OperationImplementation("reporting_operations.create_chart_from_vector_group")]
    public override Task<Api.CreateChartFromVectorGroupResult> CreateChartFromVectorGroup(
        Api.CreateChartFromVectorGroupRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreateChartFromVectorGroupOperation.Descriptor,
            CreateChartFromVectorGroupOperation.CreateCommand, CreateChartFromVectorGroupOperation.OutputContracts,
            CreateChartFromVectorGroupOperation.CreateResult);

    [OperationImplementation("reporting_operations.define_report_template")]
    public override Task<Api.DefineReportTemplateResult> DefineReportTemplate(
        Api.DefineReportTemplateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DefineReportTemplateOperation.Descriptor,
            DefineReportTemplateOperation.CreateCommand, DefineReportTemplateOperation.OutputContracts,
            DefineReportTemplateOperation.CreateResult);

    [OperationImplementation("reporting_operations.delete_chart")]
    public override Task<Api.DeleteChartResult> DeleteChart(
        Api.DeleteChartRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteChartOperation.Descriptor,
            DeleteChartOperation.CreateCommand, DeleteChartOperation.OutputContracts,
            DeleteChartOperation.CreateResult);

    [OperationImplementation("reporting_operations.delete_custom_table")]
    public override Task<Api.DeleteCustomTableResult> DeleteCustomTable(
        Api.DeleteCustomTableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteCustomTableOperation.Descriptor,
            DeleteCustomTableOperation.CreateCommand, DeleteCustomTableOperation.OutputContracts,
            DeleteCustomTableOperation.CreateResult);

    [OperationImplementation("reporting_operations.delete_picture")]
    public override Task<Api.DeletePictureResult> DeletePicture(
        Api.DeletePictureRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeletePictureOperation.Descriptor,
            DeletePictureOperation.CreateCommand, DeletePictureOperation.OutputContracts,
            DeletePictureOperation.CreateResult);

    [OperationImplementation("reporting_operations.delete_sa_doc")]
    public override Task<Api.DeleteSaDocResult> DeleteSaDoc(
        Api.DeleteSaDocRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteSaDocOperation.Descriptor,
            DeleteSaDocOperation.CreateCommand, DeleteSaDocOperation.OutputContracts,
            DeleteSaDocOperation.CreateResult);

    [OperationImplementation("reporting_operations.delete_sa_report")]
    public override Task<Api.DeleteSaReportResult> DeleteSaReport(
        Api.DeleteSaReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteSaReportOperation.Descriptor,
            DeleteSaReportOperation.CreateCommand, DeleteSaReportOperation.OutputContracts,
            DeleteSaReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.delete_sa_report_template")]
    public override Task<Api.DeleteSaReportTemplateResult> DeleteSaReportTemplate(
        Api.DeleteSaReportTemplateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteSaReportTemplateOperation.Descriptor,
            DeleteSaReportTemplateOperation.CreateCommand, DeleteSaReportTemplateOperation.OutputContracts,
            DeleteSaReportTemplateOperation.CreateResult);

    [OperationImplementation("reporting_operations.generate_quick_report_from_tab_order")]
    public override Task<Api.GenerateQuickReportFromTabOrderResult> GenerateQuickReportFromTabOrder(
        Api.GenerateQuickReportFromTabOrderRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GenerateQuickReportFromTabOrderOperation.Descriptor,
            GenerateQuickReportFromTabOrderOperation.CreateCommand,
            GenerateQuickReportFromTabOrderOperation.OutputContracts,
            GenerateQuickReportFromTabOrderOperation.CreateResult);

    [OperationImplementation("reporting_operations.generate_standard_html_report")]
    public override Task<Api.GenerateStandardHtmlReportResult> GenerateStandardHtmlReport(
        Api.GenerateStandardHtmlReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GenerateStandardHtmlReportOperation.Descriptor,
            GenerateStandardHtmlReportOperation.CreateCommand, GenerateStandardHtmlReportOperation.OutputContracts,
            GenerateStandardHtmlReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.generate_update_templated_report")]
    public override Task<Api.GenerateUpdateTemplatedReportResult> GenerateUpdateTemplatedReport(
        Api.GenerateUpdateTemplatedReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GenerateUpdateTemplatedReportOperation.Descriptor,
            GenerateUpdateTemplatedReportOperation.CreateCommand, GenerateUpdateTemplatedReportOperation.OutputContracts,
            GenerateUpdateTemplatedReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.get_custom_table_cell_double")]
    public override Task<Api.GetCustomTableCellDoubleResult> GetCustomTableCellDouble(
        Api.GetCustomTableCellDoubleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCustomTableCellDoubleOperation.Descriptor,
            GetCustomTableCellDoubleOperation.CreateCommand, GetCustomTableCellDoubleOperation.OutputContracts,
            GetCustomTableCellDoubleOperation.CreateResult);

    [OperationImplementation("reporting_operations.get_custom_table_cell_string")]
    public override Task<Api.GetCustomTableCellStringResult> GetCustomTableCellString(
        Api.GetCustomTableCellStringRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetCustomTableCellStringOperation.Descriptor,
            GetCustomTableCellStringOperation.CreateCommand, GetCustomTableCellStringOperation.OutputContracts,
            GetCustomTableCellStringOperation.CreateResult);

    [OperationImplementation("reporting_operations.get_defined_report_tags")]
    public override Task<Api.GetDefinedReportTagsResult> GetDefinedReportTags(
        Api.GetDefinedReportTagsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetDefinedReportTagsOperation.Descriptor,
            GetDefinedReportTagsOperation.CreateCommand, GetDefinedReportTagsOperation.OutputContracts,
            GetDefinedReportTagsOperation.CreateResult);

    [OperationImplementation("reporting_operations.get_report_tag_value")]
    public override Task<Api.GetReportTagValueResult> GetReportTagValue(
        Api.GetReportTagValueRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetReportTagValueOperation.Descriptor,
            GetReportTagValueOperation.CreateCommand, GetReportTagValueOperation.OutputContracts,
            GetReportTagValueOperation.CreateResult);

    [OperationImplementation("reporting_operations.html_display_board")]
    public override Task<Api.HtmlDisplayBoardResult> HtmlDisplayBoard(
        Api.HtmlDisplayBoardRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, HtmlDisplayBoardOperation.Descriptor,
            HtmlDisplayBoardOperation.CreateCommand, HtmlDisplayBoardOperation.OutputContracts,
            HtmlDisplayBoardOperation.CreateResult);

    [OperationImplementation("reporting_operations.make_custom_table")]
    public override Task<Api.MakeCustomTableResult> MakeCustomTable(
        Api.MakeCustomTableRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeCustomTableOperation.Descriptor,
            MakeCustomTableOperation.CreateCommand, MakeCustomTableOperation.OutputContracts,
            MakeCustomTableOperation.CreateResult);

    [OperationImplementation("reporting_operations.make_new_sa_report")]
    public override Task<Api.MakeNewSaReportResult> MakeNewSaReport(
        Api.MakeNewSaReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeNewSaReportOperation.Descriptor,
            MakeNewSaReportOperation.CreateCommand, MakeNewSaReportOperation.OutputContracts,
            MakeNewSaReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.make_utility_chart")]
    public override Task<Api.MakeUtilityChartResult> MakeUtilityChart(
        Api.MakeUtilityChartRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeUtilityChartOperation.Descriptor,
            MakeUtilityChartOperation.CreateCommand, MakeUtilityChartOperation.OutputContracts,
            MakeUtilityChartOperation.CreateResult);

    [OperationImplementation("reporting_operations.notify_user_double")]
    public override Task<Api.NotifyUserDoubleResult> NotifyUserDouble(
        Api.NotifyUserDoubleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, NotifyUserDoubleOperation.Descriptor,
            NotifyUserDoubleOperation.CreateCommand, NotifyUserDoubleOperation.OutputContracts,
            NotifyUserDoubleOperation.CreateResult);

    [OperationImplementation("reporting_operations.notify_user_html")]
    public override Task<Api.NotifyUserHtmlResult> NotifyUserHtml(
        Api.NotifyUserHtmlRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, NotifyUserHtmlOperation.Descriptor,
            NotifyUserHtmlOperation.CreateCommand, NotifyUserHtmlOperation.OutputContracts,
            NotifyUserHtmlOperation.CreateResult);

    [OperationImplementation("reporting_operations.notify_user_integer")]
    public override Task<Api.NotifyUserIntegerResult> NotifyUserInteger(
        Api.NotifyUserIntegerRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, NotifyUserIntegerOperation.Descriptor,
            NotifyUserIntegerOperation.CreateCommand, NotifyUserIntegerOperation.OutputContracts,
            NotifyUserIntegerOperation.CreateResult);

    [OperationImplementation("reporting_operations.notify_user_text_array")]
    public override Task<Api.NotifyUserTextArrayResult> NotifyUserTextArray(
        Api.NotifyUserTextArrayRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, NotifyUserTextArrayOperation.Descriptor,
            NotifyUserTextArrayOperation.CreateCommand, NotifyUserTextArrayOperation.OutputContracts,
            NotifyUserTextArrayOperation.CreateResult);

    [OperationImplementation("reporting_operations.output_sa_report_to_excel")]
    public override Task<Api.OutputSaReportToExcelResult> OutputSaReportToExcel(
        Api.OutputSaReportToExcelRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, OutputSaReportToExcelOperation.Descriptor,
            OutputSaReportToExcelOperation.CreateCommand, OutputSaReportToExcelOperation.OutputContracts,
            OutputSaReportToExcelOperation.CreateResult);

    [OperationImplementation("reporting_operations.output_sa_report_to_pdf")]
    public override Task<Api.OutputSaReportToPdfResult> OutputSaReportToPdf(
        Api.OutputSaReportToPdfRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, OutputSaReportToPdfOperation.Descriptor,
            OutputSaReportToPdfOperation.CreateCommand, OutputSaReportToPdfOperation.OutputContracts,
            OutputSaReportToPdfOperation.CreateResult);

    [OperationImplementation("reporting_operations.quick_report")]
    public override Task<Api.QuickReportResult> QuickReport(
        Api.QuickReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, QuickReportOperation.Descriptor,
            QuickReportOperation.CreateCommand, QuickReportOperation.OutputContracts,
            QuickReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.refresh_callout_views_in_sa_report")]
    public override Task<Api.RefreshCalloutViewsInSaReportResult> RefreshCalloutViewsInSaReport(
        Api.RefreshCalloutViewsInSaReportRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RefreshCalloutViewsInSaReportOperation.Descriptor,
            RefreshCalloutViewsInSaReportOperation.CreateCommand, RefreshCalloutViewsInSaReportOperation.OutputContracts,
            RefreshCalloutViewsInSaReportOperation.CreateResult);

    [OperationImplementation("reporting_operations.refresh_report_bar")]
    public override Task<Api.RefreshReportBarResult> RefreshReportBar(
        Api.RefreshReportBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RefreshReportBarOperation.Descriptor,
            RefreshReportBarOperation.CreateCommand, RefreshReportBarOperation.OutputContracts,
            RefreshReportBarOperation.CreateResult);

    [OperationImplementation("reporting_operations.remove_report_tag")]
    public override Task<Api.RemoveReportTagResult> RemoveReportTag(
        Api.RemoveReportTagRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RemoveReportTagOperation.Descriptor,
            RemoveReportTagOperation.CreateCommand, RemoveReportTagOperation.OutputContracts,
            RemoveReportTagOperation.CreateResult);

    [OperationImplementation("reporting_operations.rename_picture")]
    public override Task<Api.RenamePictureResult> RenamePicture(
        Api.RenamePictureRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RenamePictureOperation.Descriptor,
            RenamePictureOperation.CreateCommand, RenamePictureOperation.OutputContracts,
            RenamePictureOperation.CreateResult);

    [OperationImplementation("reporting_operations.save_chart_to_jpeg_file")]
    public override Task<Api.SaveChartToJPegFileResult> SaveChartToJPegFile(
        Api.SaveChartToJPegFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SaveChartToJPegFileOperation.Descriptor,
            SaveChartToJPegFileOperation.CreateCommand, SaveChartToJPegFileOperation.OutputContracts,
            SaveChartToJPegFileOperation.CreateResult);

    [OperationImplementation("reporting_operations.save_current_view_bmp_jpg_png_gif_tiff")]
    public override Task<Api.SaveCurrentViewBmpJpgPngGifTiffResult> SaveCurrentViewBmpJpgPngGifTiff(
        Api.SaveCurrentViewBmpJpgPngGifTiffRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SaveCurrentViewBmpJpgPngGifTiffOperation.Descriptor,
            SaveCurrentViewBmpJpgPngGifTiffOperation.CreateCommand,
            SaveCurrentViewBmpJpgPngGifTiffOperation.OutputContracts,
            SaveCurrentViewBmpJpgPngGifTiffOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_custom_table_cell_color")]
    public override Task<Api.SetCustomTableCellColorResult> SetCustomTableCellColor(
        Api.SetCustomTableCellColorRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCustomTableCellColorOperation.Descriptor,
            SetCustomTableCellColorOperation.CreateCommand, SetCustomTableCellColorOperation.OutputContracts,
            SetCustomTableCellColorOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_custom_table_cell_double")]
    public override Task<Api.SetCustomTableCellDoubleResult> SetCustomTableCellDouble(
        Api.SetCustomTableCellDoubleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCustomTableCellDoubleOperation.Descriptor,
            SetCustomTableCellDoubleOperation.CreateCommand, SetCustomTableCellDoubleOperation.OutputContracts,
            SetCustomTableCellDoubleOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_custom_table_cell_font")]
    public override Task<Api.SetCustomTableCellFontResult> SetCustomTableCellFont(
        Api.SetCustomTableCellFontRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCustomTableCellFontOperation.Descriptor,
            SetCustomTableCellFontOperation.CreateCommand, SetCustomTableCellFontOperation.OutputContracts,
            SetCustomTableCellFontOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_custom_table_cell_string")]
    public override Task<Api.SetCustomTableCellStringResult> SetCustomTableCellString(
        Api.SetCustomTableCellStringRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCustomTableCellStringOperation.Descriptor,
            SetCustomTableCellStringOperation.CreateCommand, SetCustomTableCellStringOperation.OutputContracts,
            SetCustomTableCellStringOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_custom_table_header_cell")]
    public override Task<Api.SetCustomTableHeaderCellResult> SetCustomTableHeaderCell(
        Api.SetCustomTableHeaderCellRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCustomTableHeaderCellOperation.Descriptor,
            SetCustomTableHeaderCellOperation.CreateCommand, SetCustomTableHeaderCellOperation.OutputContracts,
            SetCustomTableHeaderCellOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_custom_table_header_row")]
    public override Task<Api.SetCustomTableHeaderRowResult> SetCustomTableHeaderRow(
        Api.SetCustomTableHeaderRowRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCustomTableHeaderRowOperation.Descriptor,
            SetCustomTableHeaderRowOperation.CreateCommand, SetCustomTableHeaderRowOperation.OutputContracts,
            SetCustomTableHeaderRowOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_custom_table_title")]
    public override Task<Api.SetCustomTableTitleResult> SetCustomTableTitle(
        Api.SetCustomTableTitleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCustomTableTitleOperation.Descriptor,
            SetCustomTableTitleOperation.CreateCommand, SetCustomTableTitleOperation.OutputContracts,
            SetCustomTableTitleOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_point_group_report_options")]
    public override Task<Api.SetPointGroupReportOptionsResult> SetPointGroupReportOptions(
        Api.SetPointGroupReportOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetPointGroupReportOptionsOperation.Descriptor,
            SetPointGroupReportOptionsOperation.CreateCommand, SetPointGroupReportOptionsOperation.OutputContracts,
            SetPointGroupReportOptionsOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_relationship_report_options")]
    public override Task<Api.SetRelationshipReportOptionsResult> SetRelationshipReportOptions(
        Api.SetRelationshipReportOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetRelationshipReportOptionsOperation.Descriptor,
            SetRelationshipReportOptionsOperation.CreateCommand, SetRelationshipReportOptionsOperation.OutputContracts,
            SetRelationshipReportOptionsOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_report_bar_visibility")]
    public override Task<Api.SetReportBarVisibilityResult> SetReportBarVisibility(
        Api.SetReportBarVisibilityRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetReportBarVisibilityOperation.Descriptor,
            SetReportBarVisibilityOperation.CreateCommand, SetReportBarVisibilityOperation.OutputContracts,
            SetReportBarVisibilityOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_report_options_for_object")]
    public override Task<Api.SetReportOptionsForObjectResult> SetReportOptionsForObject(
        Api.SetReportOptionsForObjectRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetReportOptionsForObjectOperation.Descriptor,
            SetReportOptionsForObjectOperation.CreateCommand, SetReportOptionsForObjectOperation.OutputContracts,
            SetReportOptionsForObjectOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_report_tag_value_from_double")]
    public override Task<Api.SetReportTagValueFromDoubleResult> SetReportTagValueFromDouble(
        Api.SetReportTagValueFromDoubleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetReportTagValueFromDoubleOperation.Descriptor,
            SetReportTagValueFromDoubleOperation.CreateCommand, SetReportTagValueFromDoubleOperation.OutputContracts,
            SetReportTagValueFromDoubleOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_report_tag_value_from_integer")]
    public override Task<Api.SetReportTagValueFromIntegerResult> SetReportTagValueFromInteger(
        Api.SetReportTagValueFromIntegerRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetReportTagValueFromIntegerOperation.Descriptor,
            SetReportTagValueFromIntegerOperation.CreateCommand, SetReportTagValueFromIntegerOperation.OutputContracts,
            SetReportTagValueFromIntegerOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_report_tag_value_from_string")]
    public override Task<Api.SetReportTagValueFromStringResult> SetReportTagValueFromString(
        Api.SetReportTagValueFromStringRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetReportTagValueFromStringOperation.Descriptor,
            SetReportTagValueFromStringOperation.CreateCommand, SetReportTagValueFromStringOperation.OutputContracts,
            SetReportTagValueFromStringOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_scale_for_picture")]
    public override Task<Api.SetScaleForPictureResult> SetScaleForPicture(
        Api.SetScaleForPictureRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetScaleForPictureOperation.Descriptor,
            SetScaleForPictureOperation.CreateCommand, SetScaleForPictureOperation.OutputContracts,
            SetScaleForPictureOperation.CreateResult);

    [OperationImplementation("reporting_operations.set_vector_group_report_options")]
    public override Task<Api.SetVectorGroupReportOptionsResult> SetVectorGroupReportOptions(
        Api.SetVectorGroupReportOptionsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorGroupReportOptionsOperation.Descriptor,
            SetVectorGroupReportOptionsOperation.CreateCommand, SetVectorGroupReportOptionsOperation.OutputContracts,
            SetVectorGroupReportOptionsOperation.CreateResult);

}
