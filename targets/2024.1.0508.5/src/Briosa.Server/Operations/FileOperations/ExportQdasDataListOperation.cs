using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ExportQdasDataListOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.export_qdas_data_list", "Export QDAS Data List", "briosa.FileOperations",
        "ExportQdasDataList", "/briosa.FileOperations/ExportQdasDataList", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ExportQdasDataListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("QDAS Export File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.QdasExportFilePath, "qdas_export_file_path"), "SetFilePathArg")
        ], []);
    }

    public static Api.ExportQdasDataListResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
