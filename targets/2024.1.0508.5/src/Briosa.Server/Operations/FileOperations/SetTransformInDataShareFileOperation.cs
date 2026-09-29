using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class SetTransformInDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.set_transform_in_data_share_file", "Set Transform In DataShare File",
        "briosa.FileOperations", "SetTransformInDataShareFile", "/briosa.FileOperations/SetTransformInDataShareFile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetTransformInDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Transform Name", WorkerMpValueKind.Text, new WorkerTextValue(request.TransformName), "SetStringArg"),
                new("Transform Value", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.TransformValue, "transform_value"), "SetTransformArg")], []);
    }

    public static Api.SetTransformInDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
