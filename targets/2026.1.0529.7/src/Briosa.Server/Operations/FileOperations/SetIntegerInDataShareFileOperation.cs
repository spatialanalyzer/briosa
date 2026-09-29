using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class SetIntegerInDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.set_integer_in_data_share_file", "Set Integer In DataShare File",
        "briosa.FileOperations", "SetIntegerInDataShareFile", "/briosa.FileOperations/SetIntegerInDataShareFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetIntegerInDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Integer Name", WorkerMpValueKind.Text, new WorkerTextValue(request.IntegerName), "SetStringArg"),
                new("Integer Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.IntegerValue), "SetIntegerArg")], []);
    }

    public static Api.SetIntegerInDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
