using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportPtxPointCloudsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_ptx_point_clouds", "Export PTX Point Clouds", "briosa.FileOperations",
        "ExportPtxPointClouds", "/briosa.FileOperations/ExportPtxPointClouds", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportPtxPointCloudsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("PTX File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.PtxFilePath, "ptx_file_path"), "SetFilePathArg"),
            new("Point Cloud List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.PointCloudList, "point_cloud_list"),
                "SetCollectionObjectNameRefListArg"),
            new("Overwrite existing file?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOverwriteExistingFile && request.OverwriteExistingFile), "SetBoolArg"),
            new("Show Progress Dialog?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowProgressDialog && request.ShowProgressDialog), "SetBoolArg")
        ], []);
    }

    public static Api.ExportPtxPointCloudsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
