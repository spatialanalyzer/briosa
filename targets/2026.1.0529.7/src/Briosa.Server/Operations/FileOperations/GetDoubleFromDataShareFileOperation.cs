using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class GetDoubleFromDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.get_double_from_data_share_file", "Get Double From DataShare File",
        "briosa.FileOperations", "GetDoubleFromDataShareFile", "/briosa.FileOperations/GetDoubleFromDataShareFile",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("double_value", "Double Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetDoubleFromDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Double Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.DoubleName), "SetStringArg")],
            [new("Double Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetDoubleFromDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        DoubleValue = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
