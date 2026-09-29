using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetFolderNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_folder_notes", "Set Folder Notes", "briosa.UtilityOperations",
        "SetFolderNotes", "/briosa.UtilityOperations/SetFolderNotes", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetFolderNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Notes.Count == 0)
            throw new ArgumentException("Notes are required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Folder Path", WorkerMpValueKind.Text, new WorkerTextValue(request.FolderPath), "SetStringArg"),
            new("Notes", WorkerMpValueKind.EditText, new WorkerStringListValue(request.Notes), "SetEditTextArg"),
            new("Append? (FALSE = Overwrite)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAppend || request.Append), "SetBoolArg")
        ], []);
    }

    public static Api.SetFolderNotesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
