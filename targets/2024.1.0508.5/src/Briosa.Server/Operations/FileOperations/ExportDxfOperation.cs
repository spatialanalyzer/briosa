using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportDxfOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_dxf", "Export DXF", "briosa.FileOperations", "ExportDxf",
        "/briosa.FileOperations/ExportDxf", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportDxfRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("DXF File Path", WorkerMpValueKind.FileReference, FileReferenceMapper.Required(request.DxfFilePath, "dxf_file_path"), "SetFilePathArg"),
            new("Point Names", WorkerMpValueKind.PointNameList, PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
            new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList, CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names"), "SetCollectionObjectNameRefListArg"),
            new("Include Point Labels?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasIncludePointLabels || request.IncludePointLabels), "SetBoolArg")
        ], []);
    }

    public static Api.ExportDxfResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
