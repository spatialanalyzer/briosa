using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportAsciiPointCloudsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_ascii_point_clouds", "Export ASCII Point Clouds", "briosa.FileOperations", "ExportAsciiPointClouds",
        "/briosa.FileOperations/ExportAsciiPointClouds", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportAsciiPointCloudsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("ASCII File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.AsciiFilePath, "ascii_file_path"), "SetFilePathArg"),
            new("Point Cloud List", WorkerMpValueKind.CollectionObjectNameList, CollectionObjectNameMapper.RequiredList(request.PointCloudList, "point_cloud_list"), "SetCollectionObjectNameRefListArg"),
            new("Data Delimiter", WorkerMpValueKind.ExportDataDelimiterType, AsciiFileOperationValueMapper.RequiredDelimiter(request.HasDataDelimiter ? request.DataDelimiter : null, "data_delimiter"), "SetExportDataDelimeterTypeArg"),
            new("Overwrite existing file?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasOverwriteExistingFile && request.OverwriteExistingFile), "SetBoolArg"),
            new("Show Progress Dialog?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasShowProgressDialog && request.ShowProgressDialog), "SetBoolArg"),
        ], []);
    }

    public static Api.ExportAsciiPointCloudsResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
