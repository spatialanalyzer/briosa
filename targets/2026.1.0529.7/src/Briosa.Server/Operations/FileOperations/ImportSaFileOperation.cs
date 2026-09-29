using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class ImportSaFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.import_sa_file", "Import SA File", "briosa.FileOperations",
        "ImportSaFile", "/briosa.FileOperations/ImportSaFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ImportSaFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("SA File Name", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.SaFileName, "sa_file_name"), "SetFilePathArg"),
            new("Allow Operator Selections", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AllowOperatorSelections), "SetBoolArg"),
            // The repeated protocol field has no presence bit; keep the established nonempty API requirement.
            new("Selected Collections (optional)", WorkerMpValueKind.StringList,
                StringListMapper.RequiredList(request.SelectedCollections, "selected_collections"), "SetStringRefListArg")
        ], []);
    }

    public static Api.ImportSaFileResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
