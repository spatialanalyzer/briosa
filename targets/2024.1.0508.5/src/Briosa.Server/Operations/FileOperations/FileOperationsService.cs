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
        executor.ExecuteAsync(request, context, BackupNowOperation.Descriptor,
            BackupNowOperation.CreateCommand, BackupNowOperation.OutputContracts, BackupNowOperation.CreateResult);

    [OperationImplementation("file_operations.copy_general_file")]
    public override Task<Api.CopyGeneralFileResult> CopyGeneralFile(
        Api.CopyGeneralFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CopyGeneralFileOperation.Descriptor,
            CopyGeneralFileOperation.CreateCommand, CopyGeneralFileOperation.OutputContracts,
            CopyGeneralFileOperation.CreateResult);

    [OperationImplementation("file_operations.delete_general_file")]
    public override Task<Api.DeleteGeneralFileResult> DeleteGeneralFile(
        Api.DeleteGeneralFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteGeneralFileOperation.Descriptor,
            DeleteGeneralFileOperation.CreateCommand, DeleteGeneralFileOperation.OutputContracts,
            DeleteGeneralFileOperation.CreateResult);

    [OperationImplementation("file_operations.direct_cad_access")]
    public override Task<Api.DirectCadAccessResult> DirectCadAccess(
        Api.DirectCadAccessRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DirectCadAccessOperation.Descriptor, DirectCadAccessOperation.CreateCommand, DirectCadAccessOperation.OutputContracts, DirectCadAccessOperation.CreateResult);

    [OperationImplementation("file_operations.export_ascii_frame_set")]
    public override Task<Api.ExportAsciiFrameSetResult> ExportAsciiFrameSet(
        Api.ExportAsciiFrameSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportAsciiFrameSetOperation.Descriptor, ExportAsciiFrameSetOperation.CreateCommand, ExportAsciiFrameSetOperation.OutputContracts, ExportAsciiFrameSetOperation.CreateResult);

    [OperationImplementation("file_operations.export_ascii_frames")]
    public override Task<Api.ExportAsciiFramesResult> ExportAsciiFrames(
        Api.ExportAsciiFramesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportAsciiFramesOperation.Descriptor, ExportAsciiFramesOperation.CreateCommand, ExportAsciiFramesOperation.OutputContracts, ExportAsciiFramesOperation.CreateResult);

    [OperationImplementation("file_operations.export_ascii_point_clouds")]
    public override Task<Api.ExportAsciiPointCloudsResult> ExportAsciiPointClouds(
        Api.ExportAsciiPointCloudsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportAsciiPointCloudsOperation.Descriptor, ExportAsciiPointCloudsOperation.CreateCommand, ExportAsciiPointCloudsOperation.OutputContracts, ExportAsciiPointCloudsOperation.CreateResult);

    [OperationImplementation("file_operations.export_ascii_point_set")]
    public override Task<Api.ExportAsciiPointSetResult> ExportAsciiPointSet(
        Api.ExportAsciiPointSetRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportAsciiPointSetOperation.Descriptor, ExportAsciiPointSetOperation.CreateCommand, ExportAsciiPointSetOperation.OutputContracts, ExportAsciiPointSetOperation.CreateResult);

    [OperationImplementation("file_operations.export_ascii_points")]
    public override Task<Api.ExportAsciiPointsResult> ExportAsciiPoints(
        Api.ExportAsciiPointsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportAsciiPointsOperation.Descriptor, ExportAsciiPointsOperation.CreateCommand, ExportAsciiPointsOperation.OutputContracts, ExportAsciiPointsOperation.CreateResult);

    [OperationImplementation("file_operations.export_dxf")]
    public override Task<Api.ExportDxfResult> ExportDxf(
        Api.ExportDxfRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportDxfOperation.Descriptor, ExportDxfOperation.CreateCommand, ExportDxfOperation.OutputContracts, ExportDxfOperation.CreateResult);

    [OperationImplementation("file_operations.export_embedded_file")]
    public override Task<Api.ExportEmbeddedFileResult> ExportEmbeddedFile(
        Api.ExportEmbeddedFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportEmbeddedFileOperation.Descriptor,
            ExportEmbeddedFileOperation.CreateCommand, ExportEmbeddedFileOperation.OutputContracts,
            ExportEmbeddedFileOperation.CreateResult);

    [OperationImplementation("file_operations.export_hidden_point_bar_xml_file")]
    public override Task<Api.ExportHiddenPointBarXmlFileResult> ExportHiddenPointBarXmlFile(
        Api.ExportHiddenPointBarXmlFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportHiddenPointBarXmlFileOperation.Descriptor,
            ExportHiddenPointBarXmlFileOperation.CreateCommand, ExportHiddenPointBarXmlFileOperation.OutputContracts,
            ExportHiddenPointBarXmlFileOperation.CreateResult);

    [OperationImplementation("file_operations.export_iges_file_entire_model")]
    public override Task<Api.ExportIgesFileEntireModelResult> ExportIgesFileEntireModel(
        Api.ExportIgesFileEntireModelRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportIgesFileEntireModelOperation.Descriptor, ExportIgesFileEntireModelOperation.CreateCommand, ExportIgesFileEntireModelOperation.OutputContracts, ExportIgesFileEntireModelOperation.CreateResult);

    [OperationImplementation("file_operations.export_iges_file_partial_model")]
    public override Task<Api.ExportIgesFilePartialModelResult> ExportIgesFilePartialModel(
        Api.ExportIgesFilePartialModelRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportIgesFilePartialModelOperation.Descriptor, ExportIgesFilePartialModelOperation.CreateCommand, ExportIgesFilePartialModelOperation.OutputContracts, ExportIgesFilePartialModelOperation.CreateResult);

    [OperationImplementation("file_operations.export_ptx_point_clouds")]
    public override Task<Api.ExportPtxPointCloudsResult> ExportPtxPointClouds(
        Api.ExportPtxPointCloudsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportPtxPointCloudsOperation.Descriptor,
            ExportPtxPointCloudsOperation.CreateCommand, ExportPtxPointCloudsOperation.OutputContracts,
            ExportPtxPointCloudsOperation.CreateResult);

    [OperationImplementation("file_operations.export_qdas_characteristics")]
    public override Task<Api.ExportQdasCharacteristicsResult> ExportQdasCharacteristics(
        Api.ExportQdasCharacteristicsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportQdasCharacteristicsOperation.Descriptor,
            ExportQdasCharacteristicsOperation.CreateCommand, ExportQdasCharacteristicsOperation.OutputContracts,
            ExportQdasCharacteristicsOperation.CreateResult);

    [OperationImplementation("file_operations.export_qdas_data_list")]
    public override Task<Api.ExportQdasDataListResult> ExportQdasDataList(
        Api.ExportQdasDataListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportQdasDataListOperation.Descriptor,
            ExportQdasDataListOperation.CreateCommand, ExportQdasDataListOperation.OutputContracts,
            ExportQdasDataListOperation.CreateResult);

    [OperationImplementation("file_operations.export_scan_stripe_mesh_to_stl_file")]
    public override Task<Api.ExportScanStripeMeshToStlFileResult> ExportScanStripeMeshToStlFile(
        Api.ExportScanStripeMeshToStlFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportScanStripeMeshToStlFileOperation.Descriptor, ExportScanStripeMeshToStlFileOperation.CreateCommand, ExportScanStripeMeshToStlFileOperation.OutputContracts, ExportScanStripeMeshToStlFileOperation.CreateResult);

    [OperationImplementation("file_operations.export_step_file_entire_model")]
    public override Task<Api.ExportStepFileEntireModelResult> ExportStepFileEntireModel(
        Api.ExportStepFileEntireModelRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportStepFileEntireModelOperation.Descriptor, ExportStepFileEntireModelOperation.CreateCommand, ExportStepFileEntireModelOperation.OutputContracts, ExportStepFileEntireModelOperation.CreateResult);

    [OperationImplementation("file_operations.export_step_file_partial_model")]
    public override Task<Api.ExportStepFilePartialModelResult> ExportStepFilePartialModel(
        Api.ExportStepFilePartialModelRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportStepFilePartialModelOperation.Descriptor, ExportStepFilePartialModelOperation.CreateCommand, ExportStepFilePartialModelOperation.OutputContracts, ExportStepFilePartialModelOperation.CreateResult);

    [OperationImplementation("file_operations.export_vda_fs_file_entire_model")]
    public override Task<Api.ExportVdaFsFileEntireModelResult> ExportVdaFsFileEntireModel(
        Api.ExportVdaFsFileEntireModelRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportVdaFsFileEntireModelOperation.Descriptor, ExportVdaFsFileEntireModelOperation.CreateCommand, ExportVdaFsFileEntireModelOperation.OutputContracts, ExportVdaFsFileEntireModelOperation.CreateResult);

    [OperationImplementation("file_operations.export_vda_fs_file_partial_model")]
    public override Task<Api.ExportVdaFsFilePartialModelResult> ExportVdaFsFilePartialModel(
        Api.ExportVdaFsFilePartialModelRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportVdaFsFilePartialModelOperation.Descriptor, ExportVdaFsFilePartialModelOperation.CreateCommand, ExportVdaFsFilePartialModelOperation.OutputContracts, ExportVdaFsFilePartialModelOperation.CreateResult);

    [OperationImplementation("file_operations.export_vector_container_to_ascii_file")]
    public override Task<Api.ExportVectorContainerToAsciiFileResult> ExportVectorContainerToAsciiFile(
        Api.ExportVectorContainerToAsciiFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportVectorContainerToAsciiFileOperation.Descriptor, ExportVectorContainerToAsciiFileOperation.CreateCommand, ExportVectorContainerToAsciiFileOperation.OutputContracts, ExportVectorContainerToAsciiFileOperation.CreateResult);

    [OperationImplementation("file_operations.find_files_in_directory")]
    public override Task<Api.FindFilesInDirectoryResult> FindFilesInDirectory(
        Api.FindFilesInDirectoryRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FindFilesInDirectoryOperation.Descriptor,
            FindFilesInDirectoryOperation.CreateCommand, FindFilesInDirectoryOperation.OutputContracts,
            FindFilesInDirectoryOperation.CreateResult);

    [OperationImplementation("file_operations.find_sub_directories_in_directory")]
    public override Task<Api.FindSubDirectoriesInDirectoryResult> FindSubDirectoriesInDirectory(
        Api.FindSubDirectoriesInDirectoryRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, FindSubDirectoriesInDirectoryOperation.Descriptor,
            FindSubDirectoriesInDirectoryOperation.CreateCommand,
            FindSubDirectoriesInDirectoryOperation.OutputContracts,
            FindSubDirectoriesInDirectoryOperation.CreateResult);

    [OperationImplementation("file_operations.get_boolean_from_data_share_file")]
    public override Task<Api.GetBooleanFromDataShareFileResult> GetBooleanFromDataShareFile(
        Api.GetBooleanFromDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetBooleanFromDataShareFileOperation.Descriptor,
            GetBooleanFromDataShareFileOperation.CreateCommand, GetBooleanFromDataShareFileOperation.OutputContracts,
            GetBooleanFromDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.get_double_from_data_share_file")]
    public override Task<Api.GetDoubleFromDataShareFileResult> GetDoubleFromDataShareFile(
        Api.GetDoubleFromDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetDoubleFromDataShareFileOperation.Descriptor,
            GetDoubleFromDataShareFileOperation.CreateCommand, GetDoubleFromDataShareFileOperation.OutputContracts,
            GetDoubleFromDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.get_integer_from_data_share_file")]
    public override Task<Api.GetIntegerFromDataShareFileResult> GetIntegerFromDataShareFile(
        Api.GetIntegerFromDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetIntegerFromDataShareFileOperation.Descriptor,
            GetIntegerFromDataShareFileOperation.CreateCommand, GetIntegerFromDataShareFileOperation.OutputContracts,
            GetIntegerFromDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.get_qdas_catalog_entries")]
    public override Task<Api.GetQdasCatalogEntriesResult> GetQdasCatalogEntries(
        Api.GetQdasCatalogEntriesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetQdasCatalogEntriesOperation.Descriptor,
            GetQdasCatalogEntriesOperation.CreateCommand, GetQdasCatalogEntriesOperation.OutputContracts,
            GetQdasCatalogEntriesOperation.CreateResult);

    [OperationImplementation("file_operations.get_string_from_data_share_file")]
    public override Task<Api.GetStringFromDataShareFileResult> GetStringFromDataShareFile(
        Api.GetStringFromDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetStringFromDataShareFileOperation.Descriptor,
            GetStringFromDataShareFileOperation.CreateCommand, GetStringFromDataShareFileOperation.OutputContracts,
            GetStringFromDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.get_transform_from_data_share_file")]
    public override Task<Api.GetTransformFromDataShareFileResult> GetTransformFromDataShareFile(
        Api.GetTransformFromDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetTransformFromDataShareFileOperation.Descriptor,
            GetTransformFromDataShareFileOperation.CreateCommand, GetTransformFromDataShareFileOperation.OutputContracts,
            GetTransformFromDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.get_vector_from_data_share_file")]
    public override Task<Api.GetVectorFromDataShareFileResult> GetVectorFromDataShareFile(
        Api.GetVectorFromDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetVectorFromDataShareFileOperation.Descriptor,
            GetVectorFromDataShareFileOperation.CreateCommand, GetVectorFromDataShareFileOperation.OutputContracts,
            GetVectorFromDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.get_working_directory")]
    public override Task<Api.GetWorkingDirectoryResult> GetWorkingDirectory(
        Api.GetWorkingDirectoryRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetWorkingDirectoryOperation.Descriptor,
            GetWorkingDirectoryOperation.CreateCommand, GetWorkingDirectoryOperation.OutputContracts,
            GetWorkingDirectoryOperation.CreateResult);

    [OperationImplementation("file_operations.import_ascii_predefined_formats")]
    public override Task<Api.ImportAsciiPredefinedFormatsResult> ImportAsciiPredefinedFormats(
        Api.ImportAsciiPredefinedFormatsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportAsciiPredefinedFormatsOperation.Descriptor, ImportAsciiPredefinedFormatsOperation.CreateCommand, ImportAsciiPredefinedFormatsOperation.OutputContracts, ImportAsciiPredefinedFormatsOperation.CreateResult);

    [OperationImplementation("file_operations.import_ascii_predefined_frame_set_formats")]
    public override Task<Api.ImportAsciiPredefinedFrameSetFormatsResult> ImportAsciiPredefinedFrameSetFormats(
        Api.ImportAsciiPredefinedFrameSetFormatsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportAsciiPredefinedFrameSetFormatsOperation.Descriptor, ImportAsciiPredefinedFrameSetFormatsOperation.CreateCommand, ImportAsciiPredefinedFrameSetFormatsOperation.OutputContracts, ImportAsciiPredefinedFrameSetFormatsOperation.CreateResult);

    [OperationImplementation("file_operations.import_e57_file")]
    public override Task<Api.ImportE57FileResult> ImportE57File(
        Api.ImportE57FileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportE57FileOperation.Descriptor,
            ImportE57FileOperation.CreateCommand, ImportE57FileOperation.OutputContracts,
            ImportE57FileOperation.CreateResult);

    [OperationImplementation("file_operations.import_file_as_embedded_file")]
    public override Task<Api.ImportFileAsEmbeddedFileResult> ImportFileAsEmbeddedFile(
        Api.ImportFileAsEmbeddedFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportFileAsEmbeddedFileOperation.Descriptor,
            ImportFileAsEmbeddedFileOperation.CreateCommand, ImportFileAsEmbeddedFileOperation.OutputContracts,
            ImportFileAsEmbeddedFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_file_as_picture")]
    public override Task<Api.ImportFileAsPictureResult> ImportFileAsPicture(
        Api.ImportFileAsPictureRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportFileAsPictureOperation.Descriptor,
            ImportFileAsPictureOperation.CreateCommand, ImportFileAsPictureOperation.OutputContracts,
            ImportFileAsPictureOperation.CreateResult);

    [OperationImplementation("file_operations.import_hidden_point_bar_xml_file")]
    public override Task<Api.ImportHiddenPointBarXmlFileResult> ImportHiddenPointBarXmlFile(
        Api.ImportHiddenPointBarXmlFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportHiddenPointBarXmlFileOperation.Descriptor,
            ImportHiddenPointBarXmlFileOperation.CreateCommand, ImportHiddenPointBarXmlFileOperation.OutputContracts,
            ImportHiddenPointBarXmlFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_iges_file")]
    public override Task<Api.ImportIgesFileResult> ImportIgesFile(
        Api.ImportIgesFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportIgesFileOperation.Descriptor, ImportIgesFileOperation.CreateCommand, ImportIgesFileOperation.OutputContracts, ImportIgesFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_leica_gsi_file")]
    public override Task<Api.ImportLeicaGsiFileResult> ImportLeicaGsiFile(
        Api.ImportLeicaGsiFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportLeicaGsiFileOperation.Descriptor,
            ImportLeicaGsiFileOperation.CreateCommand, ImportLeicaGsiFileOperation.OutputContracts,
            ImportLeicaGsiFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_leica_sdb_file")]
    public override Task<Api.ImportLeicaSdbFileResult> ImportLeicaSdbFile(
        Api.ImportLeicaSdbFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportLeicaSdbFileOperation.Descriptor,
            ImportLeicaSdbFileOperation.CreateCommand, ImportLeicaSdbFileOperation.OutputContracts,
            ImportLeicaSdbFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_mp_file_as_embedded_mp")]
    public override Task<Api.ImportMpFileAsEmbeddedMpResult> ImportMpFileAsEmbeddedMp(
        Api.ImportMpFileAsEmbeddedMpRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportMpFileAsEmbeddedMpOperation.Descriptor,
            ImportMpFileAsEmbeddedMpOperation.CreateCommand, ImportMpFileAsEmbeddedMpOperation.OutputContracts,
            ImportMpFileAsEmbeddedMpOperation.CreateResult);

    [OperationImplementation("file_operations.import_nominals_from_xml_file")]
    public override Task<Api.ImportNominalsFromXmlFileResult> ImportNominalsFromXmlFile(
        Api.ImportNominalsFromXmlFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportNominalsFromXmlFileOperation.Descriptor,
            ImportNominalsFromXmlFileOperation.CreateCommand, ImportNominalsFromXmlFileOperation.OutputContracts,
            ImportNominalsFromXmlFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_polyworks_file")]
    public override Task<Api.ImportPolyworksFileResult> ImportPolyworksFile(
        Api.ImportPolyworksFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportPolyworksFileOperation.Descriptor, ImportPolyworksFileOperation.CreateCommand, ImportPolyworksFileOperation.OutputContracts, ImportPolyworksFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_qdas_catalog_file")]
    public override Task<Api.ImportQdasCatalogFileResult> ImportQdasCatalogFile(
        Api.ImportQdasCatalogFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportQdasCatalogFileOperation.Descriptor,
            ImportQdasCatalogFileOperation.CreateCommand, ImportQdasCatalogFileOperation.OutputContracts,
            ImportQdasCatalogFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_sa_file")]
    public override Task<Api.ImportSaFileResult> ImportSaFile(
        Api.ImportSaFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportSaFileOperation.Descriptor,
            ImportSaFileOperation.CreateCommand, ImportSaFileOperation.OutputContracts,
            ImportSaFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_sa_windows_placement")]
    public override Task<Api.ImportSaWindowsPlacementResult> ImportSaWindowsPlacement(
        Api.ImportSaWindowsPlacementRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportSaWindowsPlacementOperation.Descriptor,
            ImportSaWindowsPlacementOperation.CreateCommand, ImportSaWindowsPlacementOperation.OutputContracts,
            ImportSaWindowsPlacementOperation.CreateResult);

    [OperationImplementation("file_operations.import_sat_file")]
    public override Task<Api.ImportSatFileResult> ImportSatFile(
        Api.ImportSatFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportSatFileOperation.Descriptor, ImportSatFileOperation.CreateCommand, ImportSatFileOperation.OutputContracts, ImportSatFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_step_file")]
    public override Task<Api.ImportStepFileResult> ImportStepFile(
        Api.ImportStepFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportStepFileOperation.Descriptor, ImportStepFileOperation.CreateCommand, ImportStepFileOperation.OutputContracts, ImportStepFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_stl_file")]
    public override Task<Api.ImportStlFileResult> ImportStlFile(
        Api.ImportStlFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportStlFileOperation.Descriptor, ImportStlFileOperation.CreateCommand, ImportStlFileOperation.OutputContracts, ImportStlFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_vda_fs_file")]
    public override Task<Api.ImportVdaFsFileResult> ImportVdaFsFile(
        Api.ImportVdaFsFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportVdaFsFileOperation.Descriptor, ImportVdaFsFileOperation.CreateCommand, ImportVdaFsFileOperation.OutputContracts, ImportVdaFsFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_vstars_xyz_file")]
    public override Task<Api.ImportVstarsXyzFileResult> ImportVstarsXyzFile(
        Api.ImportVstarsXyzFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportVstarsXyzFileOperation.Descriptor,
            ImportVstarsXyzFileOperation.CreateCommand, ImportVstarsXyzFileOperation.OutputContracts,
            ImportVstarsXyzFileOperation.CreateResult);

    [OperationImplementation("file_operations.import_vstars_cameras")]
    public override Task<Api.ImportVstarsCamerasResult> ImportVstarsCameras(
        Api.ImportVstarsCamerasRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ImportVstarsCamerasOperation.Descriptor,
            ImportVstarsCamerasOperation.CreateCommand, ImportVstarsCamerasOperation.OutputContracts,
            ImportVstarsCamerasOperation.CreateResult);

    [OperationImplementation("file_operations.load_html_form")]
    public override Task<Api.LoadHtmlFormResult> LoadHtmlForm(
        Api.LoadHtmlFormRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LoadHtmlFormOperation.Descriptor,
            LoadHtmlFormOperation.CreateCommand, LoadHtmlFormOperation.OutputContracts,
            LoadHtmlFormOperation.CreateResult);

    [OperationImplementation("file_operations.load_html_form_in_edge_browser")]
    public override Task<Api.LoadHtmlFormInEdgeBrowserResult> LoadHtmlFormInEdgeBrowser(
        Api.LoadHtmlFormInEdgeBrowserRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, LoadHtmlFormInEdgeBrowserOperation.Descriptor,
            LoadHtmlFormInEdgeBrowserOperation.CreateCommand, LoadHtmlFormInEdgeBrowserOperation.OutputContracts,
            LoadHtmlFormInEdgeBrowserOperation.CreateResult);

    [OperationImplementation("file_operations.make_embedded_file_name_list")]
    public override Task<Api.MakeEmbeddedFileNameListResult> MakeEmbeddedFileNameList(
        Api.MakeEmbeddedFileNameListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MakeEmbeddedFileNameListOperation.Descriptor,
            MakeEmbeddedFileNameListOperation.CreateCommand, MakeEmbeddedFileNameListOperation.OutputContracts,
            MakeEmbeddedFileNameListOperation.CreateResult);

    [OperationImplementation("file_operations.merge_measurements_into_xml_file")]
    public override Task<Api.MergeMeasurementsIntoXmlFileResult> MergeMeasurementsIntoXmlFile(
        Api.MergeMeasurementsIntoXmlFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, MergeMeasurementsIntoXmlFileOperation.Descriptor,
            MergeMeasurementsIntoXmlFileOperation.CreateCommand, MergeMeasurementsIntoXmlFileOperation.OutputContracts,
            MergeMeasurementsIntoXmlFileOperation.CreateResult);

    [OperationImplementation("file_operations.new_sa_file")]
    public override Task<Api.NewSaFileResult> NewSaFile(
        Api.NewSaFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, NewSaFileOperation.Descriptor,
            NewSaFileOperation.CreateCommand, NewSaFileOperation.OutputContracts, NewSaFileOperation.CreateResult);

    [OperationImplementation("file_operations.open_sa_file")]
    public override Task<Api.OpenSaFileResult> OpenSaFile(
        Api.OpenSaFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, OpenSaFileOperation.Descriptor,
            OpenSaFileOperation.CreateCommand, OpenSaFileOperation.OutputContracts, OpenSaFileOperation.CreateResult);

    [OperationImplementation("file_operations.open_template_file")]
    public override Task<Api.OpenTemplateFileResult> OpenTemplateFile(
        Api.OpenTemplateFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, OpenTemplateFileOperation.Descriptor,
            OpenTemplateFileOperation.CreateCommand, OpenTemplateFileOperation.OutputContracts,
            OpenTemplateFileOperation.CreateResult);

    [OperationImplementation("file_operations.pop_poly_bay_analysis_window")]
    public override Task<Api.PopPolyBayAnalysisWindowResult> PopPolyBayAnalysisWindow(
        Api.PopPolyBayAnalysisWindowRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PopPolyBayAnalysisWindowOperation.Descriptor,
            PopPolyBayAnalysisWindowOperation.CreateCommand, PopPolyBayAnalysisWindowOperation.OutputContracts,
            PopPolyBayAnalysisWindowOperation.CreateResult);

    [OperationImplementation("file_operations.prepare_qdas_data_list")]
    public override Task<Api.PrepareQdasDataListResult> PrepareQdasDataList(
        Api.PrepareQdasDataListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, PrepareQdasDataListOperation.Descriptor,
            PrepareQdasDataListOperation.CreateCommand, PrepareQdasDataListOperation.OutputContracts,
            PrepareQdasDataListOperation.CreateResult);

    [OperationImplementation("file_operations.rename_general_file")]
    public override Task<Api.RenameGeneralFileResult> RenameGeneralFile(
        Api.RenameGeneralFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RenameGeneralFileOperation.Descriptor,
            RenameGeneralFileOperation.CreateCommand, RenameGeneralFileOperation.OutputContracts,
            RenameGeneralFileOperation.CreateResult);

    [OperationImplementation("file_operations.save")]
    public override Task<Api.SaveResult> Save(
        Api.SaveRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SaveOperation.Descriptor,
            SaveOperation.CreateCommand, SaveOperation.OutputContracts, SaveOperation.CreateResult);

    [OperationImplementation("file_operations.save_as_read_only_template")]
    public override Task<Api.SaveAsReadOnlyTemplateResult> SaveAsReadOnlyTemplate(
        Api.SaveAsReadOnlyTemplateRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SaveAsReadOnlyTemplateOperation.Descriptor,
            SaveAsReadOnlyTemplateOperation.CreateCommand, SaveAsReadOnlyTemplateOperation.OutputContracts,
            SaveAsReadOnlyTemplateOperation.CreateResult);

    [OperationImplementation("file_operations.save_as")]
    public override Task<Api.SaveAsResult> SaveAs(
        Api.SaveAsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SaveAsOperation.Descriptor,
            SaveAsOperation.CreateCommand, SaveAsOperation.OutputContracts, SaveAsOperation.CreateResult);

    [OperationImplementation("file_operations.set_boolean_in_data_share_file")]
    public override Task<Api.SetBooleanInDataShareFileResult> SetBooleanInDataShareFile(
        Api.SetBooleanInDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetBooleanInDataShareFileOperation.Descriptor,
            SetBooleanInDataShareFileOperation.CreateCommand, SetBooleanInDataShareFileOperation.OutputContracts,
            SetBooleanInDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.set_double_in_data_share_file")]
    public override Task<Api.SetDoubleInDataShareFileResult> SetDoubleInDataShareFile(
        Api.SetDoubleInDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetDoubleInDataShareFileOperation.Descriptor,
            SetDoubleInDataShareFileOperation.CreateCommand, SetDoubleInDataShareFileOperation.OutputContracts,
            SetDoubleInDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.set_integer_in_data_share_file")]
    public override Task<Api.SetIntegerInDataShareFileResult> SetIntegerInDataShareFile(
        Api.SetIntegerInDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetIntegerInDataShareFileOperation.Descriptor,
            SetIntegerInDataShareFileOperation.CreateCommand, SetIntegerInDataShareFileOperation.OutputContracts,
            SetIntegerInDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.set_string_in_data_share_file")]
    public override Task<Api.SetStringInDataShareFileResult> SetStringInDataShareFile(
        Api.SetStringInDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetStringInDataShareFileOperation.Descriptor,
            SetStringInDataShareFileOperation.CreateCommand, SetStringInDataShareFileOperation.OutputContracts,
            SetStringInDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.set_transform_in_data_share_file")]
    public override Task<Api.SetTransformInDataShareFileResult> SetTransformInDataShareFile(
        Api.SetTransformInDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTransformInDataShareFileOperation.Descriptor,
            SetTransformInDataShareFileOperation.CreateCommand, SetTransformInDataShareFileOperation.OutputContracts,
            SetTransformInDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.set_vector_in_data_share_file")]
    public override Task<Api.SetVectorInDataShareFileResult> SetVectorInDataShareFile(
        Api.SetVectorInDataShareFileRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetVectorInDataShareFileOperation.Descriptor,
            SetVectorInDataShareFileOperation.CreateCommand, SetVectorInDataShareFileOperation.OutputContracts,
            SetVectorInDataShareFileOperation.CreateResult);

    [OperationImplementation("file_operations.terminate_all_running_mps")]
    public override Task<Api.TerminateAllRunningMPsResult> TerminateAllRunningMPs(
        Api.TerminateAllRunningMPsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, TerminateAllRunningMPsOperation.Descriptor,
            TerminateAllRunningMPsOperation.CreateCommand, TerminateAllRunningMPsOperation.OutputContracts,
            TerminateAllRunningMPsOperation.CreateResult);

    [OperationImplementation("file_operations.verify_general_file_exists")]
    public override Task<Api.VerifyGeneralFileExistsResult> VerifyGeneralFileExists(
        Api.VerifyGeneralFileExistsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, VerifyGeneralFileExistsOperation.Descriptor,
            VerifyGeneralFileExistsOperation.CreateCommand, VerifyGeneralFileExistsOperation.OutputContracts,
            VerifyGeneralFileExistsOperation.CreateResult);

    [OperationImplementation("file_operations.verify_mp_file_exists")]
    public override Task<Api.VerifyMpFileExistsResult> VerifyMpFileExists(
        Api.VerifyMpFileExistsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, VerifyMpFileExistsOperation.Descriptor,
            VerifyMpFileExistsOperation.CreateCommand, VerifyMpFileExistsOperation.OutputContracts,
            VerifyMpFileExistsOperation.CreateResult);

}
