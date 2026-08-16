using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal sealed class FileOperationsService(OperationExecutor executor)
    : Api.FileOperations.FileOperationsBase
{
    [OperationImplementation("file_operations.backup_now")]
    public override Task<Api.BackupNowResult> BackupNow(
        Api.BackupNowRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.BackupNowRequest, Api.BackupNowResult>(
            executor,
            request,
            context,
            "file_operations.backup_now");

    [OperationImplementation("file_operations.copy_general_file")]
    public override Task<Api.CopyGeneralFileResult> CopyGeneralFile(
        Api.CopyGeneralFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CopyGeneralFileRequest, Api.CopyGeneralFileResult>(
            executor,
            request,
            context,
            "file_operations.copy_general_file");

    [OperationImplementation("file_operations.delete_general_file")]
    public override Task<Api.DeleteGeneralFileResult> DeleteGeneralFile(
        Api.DeleteGeneralFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteGeneralFileRequest, Api.DeleteGeneralFileResult>(
            executor,
            request,
            context,
            "file_operations.delete_general_file");

    [OperationImplementation("file_operations.direct_cad_access")]
    public override Task<Api.DirectCadAccessResult> DirectCadAccess(
        Api.DirectCadAccessRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DirectCadAccessRequest, Api.DirectCadAccessResult>(
            executor,
            request,
            context,
            "file_operations.direct_cad_access");

    [OperationImplementation("file_operations.export_ascii_frame_set")]
    public override Task<Api.ExportAsciiFrameSetResult> ExportAsciiFrameSet(
        Api.ExportAsciiFrameSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportAsciiFrameSetRequest, Api.ExportAsciiFrameSetResult>(
            executor,
            request,
            context,
            "file_operations.export_ascii_frame_set");

    [OperationImplementation("file_operations.export_ascii_frames")]
    public override Task<Api.ExportAsciiFramesResult> ExportAsciiFrames(
        Api.ExportAsciiFramesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportAsciiFramesRequest, Api.ExportAsciiFramesResult>(
            executor,
            request,
            context,
            "file_operations.export_ascii_frames");

    [OperationImplementation("file_operations.export_ascii_point_clouds")]
    public override Task<Api.ExportAsciiPointCloudsResult> ExportAsciiPointClouds(
        Api.ExportAsciiPointCloudsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportAsciiPointCloudsRequest, Api.ExportAsciiPointCloudsResult>(
            executor,
            request,
            context,
            "file_operations.export_ascii_point_clouds");

    [OperationImplementation("file_operations.export_ascii_point_set")]
    public override Task<Api.ExportAsciiPointSetResult> ExportAsciiPointSet(
        Api.ExportAsciiPointSetRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportAsciiPointSetRequest, Api.ExportAsciiPointSetResult>(
            executor,
            request,
            context,
            "file_operations.export_ascii_point_set");

    [OperationImplementation("file_operations.export_ascii_points")]
    public override Task<Api.ExportAsciiPointsResult> ExportAsciiPoints(
        Api.ExportAsciiPointsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportAsciiPointsRequest, Api.ExportAsciiPointsResult>(
            executor,
            request,
            context,
            "file_operations.export_ascii_points");

    [OperationImplementation("file_operations.export_dxf")]
    public override Task<Api.ExportDxfResult> ExportDxf(
        Api.ExportDxfRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportDxfRequest, Api.ExportDxfResult>(
            executor,
            request,
            context,
            "file_operations.export_dxf");

    [OperationImplementation("file_operations.export_embedded_file")]
    public override Task<Api.ExportEmbeddedFileResult> ExportEmbeddedFile(
        Api.ExportEmbeddedFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportEmbeddedFileRequest, Api.ExportEmbeddedFileResult>(
            executor,
            request,
            context,
            "file_operations.export_embedded_file");

    [OperationImplementation("file_operations.export_hidden_point_bar_xml_file")]
    public override Task<Api.ExportHiddenPointBarXmlFileResult> ExportHiddenPointBarXmlFile(
        Api.ExportHiddenPointBarXmlFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportHiddenPointBarXmlFileRequest, Api.ExportHiddenPointBarXmlFileResult>(
            executor,
            request,
            context,
            "file_operations.export_hidden_point_bar_xml_file");

    [OperationImplementation("file_operations.export_iges_file_entire_model")]
    public override Task<Api.ExportIgesFileEntireModelResult> ExportIgesFileEntireModel(
        Api.ExportIgesFileEntireModelRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportIgesFileEntireModelRequest, Api.ExportIgesFileEntireModelResult>(
            executor,
            request,
            context,
            "file_operations.export_iges_file_entire_model");

    [OperationImplementation("file_operations.export_iges_file_partial_model")]
    public override Task<Api.ExportIgesFilePartialModelResult> ExportIgesFilePartialModel(
        Api.ExportIgesFilePartialModelRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportIgesFilePartialModelRequest, Api.ExportIgesFilePartialModelResult>(
            executor,
            request,
            context,
            "file_operations.export_iges_file_partial_model");

    [OperationImplementation("file_operations.export_ptx_point_clouds")]
    public override Task<Api.ExportPtxPointCloudsResult> ExportPtxPointClouds(
        Api.ExportPtxPointCloudsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportPtxPointCloudsRequest, Api.ExportPtxPointCloudsResult>(
            executor,
            request,
            context,
            "file_operations.export_ptx_point_clouds");

    [OperationImplementation("file_operations.export_qdas_characteristics")]
    public override Task<Api.ExportQdasCharacteristicsResult> ExportQdasCharacteristics(
        Api.ExportQdasCharacteristicsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportQdasCharacteristicsRequest, Api.ExportQdasCharacteristicsResult>(
            executor,
            request,
            context,
            "file_operations.export_qdas_characteristics");

    [OperationImplementation("file_operations.export_qdas_data_list")]
    public override Task<Api.ExportQdasDataListResult> ExportQdasDataList(
        Api.ExportQdasDataListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportQdasDataListRequest, Api.ExportQdasDataListResult>(
            executor,
            request,
            context,
            "file_operations.export_qdas_data_list");

    [OperationImplementation("file_operations.export_scan_stripe_mesh_to_stl_file")]
    public override Task<Api.ExportScanStripeMeshToStlFileResult> ExportScanStripeMeshToStlFile(
        Api.ExportScanStripeMeshToStlFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportScanStripeMeshToStlFileRequest, Api.ExportScanStripeMeshToStlFileResult>(
            executor,
            request,
            context,
            "file_operations.export_scan_stripe_mesh_to_stl_file");

    [OperationImplementation("file_operations.export_step_file_entire_model")]
    public override Task<Api.ExportStepFileEntireModelResult> ExportStepFileEntireModel(
        Api.ExportStepFileEntireModelRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportStepFileEntireModelRequest, Api.ExportStepFileEntireModelResult>(
            executor,
            request,
            context,
            "file_operations.export_step_file_entire_model");

    [OperationImplementation("file_operations.export_step_file_partial_model")]
    public override Task<Api.ExportStepFilePartialModelResult> ExportStepFilePartialModel(
        Api.ExportStepFilePartialModelRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportStepFilePartialModelRequest, Api.ExportStepFilePartialModelResult>(
            executor,
            request,
            context,
            "file_operations.export_step_file_partial_model");

    [OperationImplementation("file_operations.export_vda_fs_file_entire_model")]
    public override Task<Api.ExportVdaFsFileEntireModelResult> ExportVdaFsFileEntireModel(
        Api.ExportVdaFsFileEntireModelRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportVdaFsFileEntireModelRequest, Api.ExportVdaFsFileEntireModelResult>(
            executor,
            request,
            context,
            "file_operations.export_vda_fs_file_entire_model");

    [OperationImplementation("file_operations.export_vda_fs_file_partial_model")]
    public override Task<Api.ExportVdaFsFilePartialModelResult> ExportVdaFsFilePartialModel(
        Api.ExportVdaFsFilePartialModelRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportVdaFsFilePartialModelRequest, Api.ExportVdaFsFilePartialModelResult>(
            executor,
            request,
            context,
            "file_operations.export_vda_fs_file_partial_model");

    [OperationImplementation("file_operations.export_vector_container_to_ascii_file")]
    public override Task<Api.ExportVectorContainerToAsciiFileResult> ExportVectorContainerToAsciiFile(
        Api.ExportVectorContainerToAsciiFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportVectorContainerToAsciiFileRequest, Api.ExportVectorContainerToAsciiFileResult>(
            executor,
            request,
            context,
            "file_operations.export_vector_container_to_ascii_file");

    [OperationImplementation("file_operations.find_files_in_directory")]
    public override Task<Api.FindFilesInDirectoryResult> FindFilesInDirectory(
        Api.FindFilesInDirectoryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FindFilesInDirectoryRequest, Api.FindFilesInDirectoryResult>(
            executor,
            request,
            context,
            "file_operations.find_files_in_directory");

    [OperationImplementation("file_operations.find_sub_directories_in_directory")]
    public override Task<Api.FindSubDirectoriesInDirectoryResult> FindSubDirectoriesInDirectory(
        Api.FindSubDirectoriesInDirectoryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.FindSubDirectoriesInDirectoryRequest, Api.FindSubDirectoriesInDirectoryResult>(
            executor,
            request,
            context,
            "file_operations.find_sub_directories_in_directory");

    [OperationImplementation("file_operations.get_boolean_from_data_share_file")]
    public override Task<Api.GetBooleanFromDataShareFileResult> GetBooleanFromDataShareFile(
        Api.GetBooleanFromDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetBooleanFromDataShareFileRequest, Api.GetBooleanFromDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.get_boolean_from_data_share_file");

    [OperationImplementation("file_operations.get_double_from_data_share_file")]
    public override Task<Api.GetDoubleFromDataShareFileResult> GetDoubleFromDataShareFile(
        Api.GetDoubleFromDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetDoubleFromDataShareFileRequest, Api.GetDoubleFromDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.get_double_from_data_share_file");

    [OperationImplementation("file_operations.get_integer_from_data_share_file")]
    public override Task<Api.GetIntegerFromDataShareFileResult> GetIntegerFromDataShareFile(
        Api.GetIntegerFromDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetIntegerFromDataShareFileRequest, Api.GetIntegerFromDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.get_integer_from_data_share_file");

    [OperationImplementation("file_operations.get_qdas_catalog_entries")]
    public override Task<Api.GetQdasCatalogEntriesResult> GetQdasCatalogEntries(
        Api.GetQdasCatalogEntriesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetQdasCatalogEntriesRequest, Api.GetQdasCatalogEntriesResult>(
            executor,
            request,
            context,
            "file_operations.get_qdas_catalog_entries");

    [OperationImplementation("file_operations.get_string_from_data_share_file")]
    public override Task<Api.GetStringFromDataShareFileResult> GetStringFromDataShareFile(
        Api.GetStringFromDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetStringFromDataShareFileRequest, Api.GetStringFromDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.get_string_from_data_share_file");

    [OperationImplementation("file_operations.get_transform_from_data_share_file")]
    public override Task<Api.GetTransformFromDataShareFileResult> GetTransformFromDataShareFile(
        Api.GetTransformFromDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetTransformFromDataShareFileRequest, Api.GetTransformFromDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.get_transform_from_data_share_file");

    [OperationImplementation("file_operations.get_vector_from_data_share_file")]
    public override Task<Api.GetVectorFromDataShareFileResult> GetVectorFromDataShareFile(
        Api.GetVectorFromDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetVectorFromDataShareFileRequest, Api.GetVectorFromDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.get_vector_from_data_share_file");

    [OperationImplementation("file_operations.get_working_directory")]
    public override Task<Api.GetWorkingDirectoryResult> GetWorkingDirectory(
        Api.GetWorkingDirectoryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetWorkingDirectoryRequest, Api.GetWorkingDirectoryResult>(
            executor,
            request,
            context,
            "file_operations.get_working_directory");

    [OperationImplementation("file_operations.import_ascii_predefined_formats")]
    public override Task<Api.ImportAsciiPredefinedFormatsResult> ImportAsciiPredefinedFormats(
        Api.ImportAsciiPredefinedFormatsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportAsciiPredefinedFormatsRequest, Api.ImportAsciiPredefinedFormatsResult>(
            executor,
            request,
            context,
            "file_operations.import_ascii_predefined_formats");

    [OperationImplementation("file_operations.import_ascii_predefined_frame_set_formats")]
    public override Task<Api.ImportAsciiPredefinedFrameSetFormatsResult> ImportAsciiPredefinedFrameSetFormats(
        Api.ImportAsciiPredefinedFrameSetFormatsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportAsciiPredefinedFrameSetFormatsRequest, Api.ImportAsciiPredefinedFrameSetFormatsResult>(
            executor,
            request,
            context,
            "file_operations.import_ascii_predefined_frame_set_formats");

    [OperationImplementation("file_operations.import_e57_file")]
    public override Task<Api.ImportE57FileResult> ImportE57File(
        Api.ImportE57FileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportE57FileRequest, Api.ImportE57FileResult>(
            executor,
            request,
            context,
            "file_operations.import_e57_file");

    [OperationImplementation("file_operations.import_file_as_embedded_file")]
    public override Task<Api.ImportFileAsEmbeddedFileResult> ImportFileAsEmbeddedFile(
        Api.ImportFileAsEmbeddedFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportFileAsEmbeddedFileRequest, Api.ImportFileAsEmbeddedFileResult>(
            executor,
            request,
            context,
            "file_operations.import_file_as_embedded_file");

    [OperationImplementation("file_operations.import_file_as_picture")]
    public override Task<Api.ImportFileAsPictureResult> ImportFileAsPicture(
        Api.ImportFileAsPictureRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportFileAsPictureRequest, Api.ImportFileAsPictureResult>(
            executor,
            request,
            context,
            "file_operations.import_file_as_picture");

    [OperationImplementation("file_operations.import_hidden_point_bar_xml_file")]
    public override Task<Api.ImportHiddenPointBarXmlFileResult> ImportHiddenPointBarXmlFile(
        Api.ImportHiddenPointBarXmlFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportHiddenPointBarXmlFileRequest, Api.ImportHiddenPointBarXmlFileResult>(
            executor,
            request,
            context,
            "file_operations.import_hidden_point_bar_xml_file");

    [OperationImplementation("file_operations.import_iges_file")]
    public override Task<Api.ImportIgesFileResult> ImportIgesFile(
        Api.ImportIgesFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportIgesFileRequest, Api.ImportIgesFileResult>(
            executor,
            request,
            context,
            "file_operations.import_iges_file");

    [OperationImplementation("file_operations.import_leica_gsi_file")]
    public override Task<Api.ImportLeicaGsiFileResult> ImportLeicaGsiFile(
        Api.ImportLeicaGsiFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportLeicaGsiFileRequest, Api.ImportLeicaGsiFileResult>(
            executor,
            request,
            context,
            "file_operations.import_leica_gsi_file");

    [OperationImplementation("file_operations.import_leica_sdb_file")]
    public override Task<Api.ImportLeicaSdbFileResult> ImportLeicaSdbFile(
        Api.ImportLeicaSdbFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportLeicaSdbFileRequest, Api.ImportLeicaSdbFileResult>(
            executor,
            request,
            context,
            "file_operations.import_leica_sdb_file");

    [OperationImplementation("file_operations.import_mp_file_as_embedded_mp")]
    public override Task<Api.ImportMpFileAsEmbeddedMpResult> ImportMpFileAsEmbeddedMp(
        Api.ImportMpFileAsEmbeddedMpRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportMpFileAsEmbeddedMpRequest, Api.ImportMpFileAsEmbeddedMpResult>(
            executor,
            request,
            context,
            "file_operations.import_mp_file_as_embedded_mp");

    [OperationImplementation("file_operations.import_nominals_from_xml_file")]
    public override Task<Api.ImportNominalsFromXmlFileResult> ImportNominalsFromXmlFile(
        Api.ImportNominalsFromXmlFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportNominalsFromXmlFileRequest, Api.ImportNominalsFromXmlFileResult>(
            executor,
            request,
            context,
            "file_operations.import_nominals_from_xml_file");

    [OperationImplementation("file_operations.import_polyworks_file")]
    public override Task<Api.ImportPolyworksFileResult> ImportPolyworksFile(
        Api.ImportPolyworksFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportPolyworksFileRequest, Api.ImportPolyworksFileResult>(
            executor,
            request,
            context,
            "file_operations.import_polyworks_file");

    [OperationImplementation("file_operations.import_qdas_catalog_file")]
    public override Task<Api.ImportQdasCatalogFileResult> ImportQdasCatalogFile(
        Api.ImportQdasCatalogFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportQdasCatalogFileRequest, Api.ImportQdasCatalogFileResult>(
            executor,
            request,
            context,
            "file_operations.import_qdas_catalog_file");

    [OperationImplementation("file_operations.import_sa_file")]
    public override Task<Api.ImportSaFileResult> ImportSaFile(
        Api.ImportSaFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportSaFileRequest, Api.ImportSaFileResult>(
            executor,
            request,
            context,
            "file_operations.import_sa_file");

    [OperationImplementation("file_operations.import_sa_windows_placement")]
    public override Task<Api.ImportSaWindowsPlacementResult> ImportSaWindowsPlacement(
        Api.ImportSaWindowsPlacementRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportSaWindowsPlacementRequest, Api.ImportSaWindowsPlacementResult>(
            executor,
            request,
            context,
            "file_operations.import_sa_windows_placement");

    [OperationImplementation("file_operations.import_sat_file")]
    public override Task<Api.ImportSatFileResult> ImportSatFile(
        Api.ImportSatFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportSatFileRequest, Api.ImportSatFileResult>(
            executor,
            request,
            context,
            "file_operations.import_sat_file");

    [OperationImplementation("file_operations.import_step_file")]
    public override Task<Api.ImportStepFileResult> ImportStepFile(
        Api.ImportStepFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportStepFileRequest, Api.ImportStepFileResult>(
            executor,
            request,
            context,
            "file_operations.import_step_file");

    [OperationImplementation("file_operations.import_stl_file")]
    public override Task<Api.ImportStlFileResult> ImportStlFile(
        Api.ImportStlFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportStlFileRequest, Api.ImportStlFileResult>(
            executor,
            request,
            context,
            "file_operations.import_stl_file");

    [OperationImplementation("file_operations.import_vda_fs_file")]
    public override Task<Api.ImportVdaFsFileResult> ImportVdaFsFile(
        Api.ImportVdaFsFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportVdaFsFileRequest, Api.ImportVdaFsFileResult>(
            executor,
            request,
            context,
            "file_operations.import_vda_fs_file");

    [OperationImplementation("file_operations.import_vstars_xyz_file")]
    public override Task<Api.ImportVstarsXyzFileResult> ImportVstarsXyzFile(
        Api.ImportVstarsXyzFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportVstarsXyzFileRequest, Api.ImportVstarsXyzFileResult>(
            executor,
            request,
            context,
            "file_operations.import_vstars_xyz_file");

    [OperationImplementation("file_operations.import_vstars_cameras")]
    public override Task<Api.ImportVstarsCamerasResult> ImportVstarsCameras(
        Api.ImportVstarsCamerasRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ImportVstarsCamerasRequest, Api.ImportVstarsCamerasResult>(
            executor,
            request,
            context,
            "file_operations.import_vstars_cameras");

    [OperationImplementation("file_operations.load_html_form")]
    public override Task<Api.LoadHtmlFormResult> LoadHtmlForm(
        Api.LoadHtmlFormRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LoadHtmlFormRequest, Api.LoadHtmlFormResult>(
            executor,
            request,
            context,
            "file_operations.load_html_form");

    [OperationImplementation("file_operations.load_html_form_in_edge_browser")]
    public override Task<Api.LoadHtmlFormInEdgeBrowserResult> LoadHtmlFormInEdgeBrowser(
        Api.LoadHtmlFormInEdgeBrowserRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.LoadHtmlFormInEdgeBrowserRequest, Api.LoadHtmlFormInEdgeBrowserResult>(
            executor,
            request,
            context,
            "file_operations.load_html_form_in_edge_browser");

    [OperationImplementation("file_operations.make_embedded_file_name_list")]
    public override Task<Api.MakeEmbeddedFileNameListResult> MakeEmbeddedFileNameList(
        Api.MakeEmbeddedFileNameListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MakeEmbeddedFileNameListRequest, Api.MakeEmbeddedFileNameListResult>(
            executor,
            request,
            context,
            "file_operations.make_embedded_file_name_list");

    [OperationImplementation("file_operations.merge_measurements_into_xml_file")]
    public override Task<Api.MergeMeasurementsIntoXmlFileResult> MergeMeasurementsIntoXmlFile(
        Api.MergeMeasurementsIntoXmlFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.MergeMeasurementsIntoXmlFileRequest, Api.MergeMeasurementsIntoXmlFileResult>(
            executor,
            request,
            context,
            "file_operations.merge_measurements_into_xml_file");

    [OperationImplementation("file_operations.new_sa_file")]
    public override Task<Api.NewSaFileResult> NewSaFile(
        Api.NewSaFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.NewSaFileRequest, Api.NewSaFileResult>(
            executor,
            request,
            context,
            "file_operations.new_sa_file");

    [OperationImplementation("file_operations.open_sa_file")]
    public override Task<Api.OpenSaFileResult> OpenSaFile(
        Api.OpenSaFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.OpenSaFileRequest, Api.OpenSaFileResult>(
            executor,
            request,
            context,
            "file_operations.open_sa_file");

    [OperationImplementation("file_operations.open_template_file")]
    public override Task<Api.OpenTemplateFileResult> OpenTemplateFile(
        Api.OpenTemplateFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.OpenTemplateFileRequest, Api.OpenTemplateFileResult>(
            executor,
            request,
            context,
            "file_operations.open_template_file");

    [OperationImplementation("file_operations.pop_poly_bay_analysis_window")]
    public override Task<Api.PopPolyBayAnalysisWindowResult> PopPolyBayAnalysisWindow(
        Api.PopPolyBayAnalysisWindowRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PopPolyBayAnalysisWindowRequest, Api.PopPolyBayAnalysisWindowResult>(
            executor,
            request,
            context,
            "file_operations.pop_poly_bay_analysis_window");

    [OperationImplementation("file_operations.prepare_qdas_data_list")]
    public override Task<Api.PrepareQdasDataListResult> PrepareQdasDataList(
        Api.PrepareQdasDataListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.PrepareQdasDataListRequest, Api.PrepareQdasDataListResult>(
            executor,
            request,
            context,
            "file_operations.prepare_qdas_data_list");

    [OperationImplementation("file_operations.rename_general_file")]
    public override Task<Api.RenameGeneralFileResult> RenameGeneralFile(
        Api.RenameGeneralFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RenameGeneralFileRequest, Api.RenameGeneralFileResult>(
            executor,
            request,
            context,
            "file_operations.rename_general_file");

    [OperationImplementation("file_operations.save")]
    public override Task<Api.SaveResult> Save(
        Api.SaveRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SaveRequest, Api.SaveResult>(
            executor,
            request,
            context,
            "file_operations.save");

    [OperationImplementation("file_operations.save_as_read_only_template")]
    public override Task<Api.SaveAsReadOnlyTemplateResult> SaveAsReadOnlyTemplate(
        Api.SaveAsReadOnlyTemplateRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SaveAsReadOnlyTemplateRequest, Api.SaveAsReadOnlyTemplateResult>(
            executor,
            request,
            context,
            "file_operations.save_as_read_only_template");

    [OperationImplementation("file_operations.save_as")]
    public override Task<Api.SaveAsResult> SaveAs(
        Api.SaveAsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SaveAsRequest, Api.SaveAsResult>(
            executor,
            request,
            context,
            "file_operations.save_as");

    [OperationImplementation("file_operations.set_boolean_in_data_share_file")]
    public override Task<Api.SetBooleanInDataShareFileResult> SetBooleanInDataShareFile(
        Api.SetBooleanInDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetBooleanInDataShareFileRequest, Api.SetBooleanInDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.set_boolean_in_data_share_file");

    [OperationImplementation("file_operations.set_double_in_data_share_file")]
    public override Task<Api.SetDoubleInDataShareFileResult> SetDoubleInDataShareFile(
        Api.SetDoubleInDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetDoubleInDataShareFileRequest, Api.SetDoubleInDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.set_double_in_data_share_file");

    [OperationImplementation("file_operations.set_integer_in_data_share_file")]
    public override Task<Api.SetIntegerInDataShareFileResult> SetIntegerInDataShareFile(
        Api.SetIntegerInDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetIntegerInDataShareFileRequest, Api.SetIntegerInDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.set_integer_in_data_share_file");

    [OperationImplementation("file_operations.set_string_in_data_share_file")]
    public override Task<Api.SetStringInDataShareFileResult> SetStringInDataShareFile(
        Api.SetStringInDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetStringInDataShareFileRequest, Api.SetStringInDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.set_string_in_data_share_file");

    [OperationImplementation("file_operations.set_transform_in_data_share_file")]
    public override Task<Api.SetTransformInDataShareFileResult> SetTransformInDataShareFile(
        Api.SetTransformInDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTransformInDataShareFileRequest, Api.SetTransformInDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.set_transform_in_data_share_file");

    [OperationImplementation("file_operations.set_vector_in_data_share_file")]
    public override Task<Api.SetVectorInDataShareFileResult> SetVectorInDataShareFile(
        Api.SetVectorInDataShareFileRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetVectorInDataShareFileRequest, Api.SetVectorInDataShareFileResult>(
            executor,
            request,
            context,
            "file_operations.set_vector_in_data_share_file");

    [OperationImplementation("file_operations.terminate_all_running_mps")]
    public override Task<Api.TerminateAllRunningMPsResult> TerminateAllRunningMPs(
        Api.TerminateAllRunningMPsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.TerminateAllRunningMPsRequest, Api.TerminateAllRunningMPsResult>(
            executor,
            request,
            context,
            "file_operations.terminate_all_running_mps");

    [OperationImplementation("file_operations.use_nrkxml_library")]
    public override Task<Api.UseNrkxmlLibraryResult> UseNrkxmlLibrary(
        Api.UseNrkxmlLibraryRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.UseNrkxmlLibraryRequest, Api.UseNrkxmlLibraryResult>(
            executor,
            request,
            context,
            "file_operations.use_nrkxml_library");

    [OperationImplementation("file_operations.verify_general_file_exists")]
    public override Task<Api.VerifyGeneralFileExistsResult> VerifyGeneralFileExists(
        Api.VerifyGeneralFileExistsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.VerifyGeneralFileExistsRequest, Api.VerifyGeneralFileExistsResult>(
            executor,
            request,
            context,
            "file_operations.verify_general_file_exists");

    [OperationImplementation("file_operations.verify_mp_file_exists")]
    public override Task<Api.VerifyMpFileExistsResult> VerifyMpFileExists(
        Api.VerifyMpFileExistsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.VerifyMpFileExistsRequest, Api.VerifyMpFileExistsResult>(
            executor,
            request,
            context,
            "file_operations.verify_mp_file_exists");

    internal Task<Api.GetWorkingDirectoryResult> ExecuteGetWorkingDirectory(
        Api.GetWorkingDirectoryRequest request,
        CancellationToken cancellationToken,
        DateTime? deadline = null,
        Guid? correlationId = null,
        string actorCategory = "internal-unattributed") =>
        executor.ExecuteAsync(
            request,
            GetWorkingDirectoryOperation.Descriptor,
            GetWorkingDirectoryOperation.CreateCommand,
            GetWorkingDirectoryOperation.OutputContracts,
            GetWorkingDirectoryOperation.CreateResult,
            cancellationToken,
            deadline,
            correlationId,
            actorCategory);

}
