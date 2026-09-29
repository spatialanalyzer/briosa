using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class SetStringInDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.set_string_in_data_share_file", "Set String In DataShare File",
        "briosa.FileOperations", "SetStringInDataShareFile", "/briosa.FileOperations/SetStringInDataShareFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetStringInDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("String Name", WorkerMpValueKind.Text, new WorkerTextValue(request.StringName), "SetStringArg"),
                new("String Value", WorkerMpValueKind.Text, new WorkerTextValue(request.StringValue), "SetStringArg")], []);
    }

    public static Api.SetStringInDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
