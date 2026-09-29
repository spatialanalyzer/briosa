using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportFileAsPictureOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_file_as_picture", "Import File as Picture", "briosa.FileOperations",
        "ImportFileAsPicture", "/briosa.FileOperations/ImportFileAsPicture", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportFileAsPictureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("External File Name", WorkerMpValueKind.FileReference,
                    FileReferenceMapper.Required(request.ExternalFileName, "external_file_name"), "SetFilePathArg"),
                new("Replace Existing?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.ReplaceExisting), "SetBoolArg")], []);
    }

    public static Api.ImportFileAsPictureResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
