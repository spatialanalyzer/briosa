using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class GetVectorFromDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.get_vector_from_data_share_file", "Get Vector From DataShare File",
        "briosa.FileOperations", "GetVectorFromDataShareFile", "/briosa.FileOperations/GetVectorFromDataShareFile",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("vector_value", "Vector Value", WorkerMpValueKind.Vector)];

    public static WorkerMpCommand CreateCommand(Api.GetVectorFromDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Vector Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.VectorName), "SetStringArg")],
            [new("Vector Value", WorkerMpValueKind.Vector, "GetVectorArg")]);
    }

    public static Api.GetVectorFromDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        VectorValue = VectorMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerVectorValue>()),
        Execution = completed.Details
    };
}
