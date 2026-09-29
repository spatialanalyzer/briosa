using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class GetIntegerFromDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.get_integer_from_data_share_file", "Get Integer From DataShare File",
        "briosa.FileOperations", "GetIntegerFromDataShareFile", "/briosa.FileOperations/GetIntegerFromDataShareFile",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("integer_value", "Integer Value", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetIntegerFromDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Integer Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.IntegerName), "SetStringArg")],
            [new("Integer Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetIntegerFromDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        IntegerValue = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
