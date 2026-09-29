using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class GetStringFromDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.get_string_from_data_share_file", "Get String From DataShare File",
        "briosa.FileOperations", "GetStringFromDataShareFile", "/briosa.FileOperations/GetStringFromDataShareFile",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("string_value", "String Value", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetStringFromDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("String Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.StringName), "SetStringArg")],
            [new("String Value", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.GetStringFromDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        StringValue = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
