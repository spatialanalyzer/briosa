using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class SaveAsReadOnlyTemplateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.save_as_read_only_template", "Save As Read-Only Template", "briosa.FileOperations",
        "SaveAsReadOnlyTemplate", "/briosa.FileOperations/SaveAsReadOnlyTemplate", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SaveAsReadOnlyTemplateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Template File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.TemplateFileName, "template_file_name"), "SetFilePathArg")], []);
    }

    public static Api.SaveAsReadOnlyTemplateResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
