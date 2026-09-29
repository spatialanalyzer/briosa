using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class OpenTemplateFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.open_template_file", "Open Template File", "briosa.FileOperations", "OpenTemplateFile",
        "/briosa.FileOperations/OpenTemplateFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.OpenTemplateFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Template File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.TemplateFileName, "template_file_name"), "SetFilePathArg")], []);
    }

    public static Api.OpenTemplateFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
