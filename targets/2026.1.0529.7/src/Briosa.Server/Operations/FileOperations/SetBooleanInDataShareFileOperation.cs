using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class SetBooleanInDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.set_boolean_in_data_share_file", "Set Boolean In DataShare File",
        "briosa.FileOperations", "SetBooleanInDataShareFile", "/briosa.FileOperations/SetBooleanInDataShareFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetBooleanInDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Boolean Name", WorkerMpValueKind.Text, new WorkerTextValue(request.BooleanName), "SetStringArg"),
                new("Boolean Value", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.BooleanValue), "SetBoolArg")], []);
    }

    public static Api.SetBooleanInDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
