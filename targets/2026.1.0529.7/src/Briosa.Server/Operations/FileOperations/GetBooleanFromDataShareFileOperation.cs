using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class GetBooleanFromDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.get_boolean_from_data_share_file", "Get Boolean From DataShare File",
        "briosa.FileOperations", "GetBooleanFromDataShareFile", "/briosa.FileOperations/GetBooleanFromDataShareFile",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("boolean_value", "Boolean Value", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.GetBooleanFromDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Boolean Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.BooleanName), "SetStringArg")],
            [new("Boolean Value", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.GetBooleanFromDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        BooleanValue = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}
