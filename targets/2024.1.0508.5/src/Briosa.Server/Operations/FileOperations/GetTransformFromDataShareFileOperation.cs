using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class GetTransformFromDataShareFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.get_transform_from_data_share_file", "Get Transform From DataShare File",
        "briosa.FileOperations", "GetTransformFromDataShareFile", "/briosa.FileOperations/GetTransformFromDataShareFile",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("transform_value", "Transform Value", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.GetTransformFromDataShareFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("DataShare File Path", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.DataShareFilePath, "data_share_file_path"), "SetFilePathArg"),
                new("Transform Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.TransformName), "SetStringArg")],
            [new("Transform Value", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.GetTransformFromDataShareFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TransformValue = TransformMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerTransformValue>()),
        Execution = completed.Details
    };
}
