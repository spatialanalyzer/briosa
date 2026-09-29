using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetFolderNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_folder_notes", "Get Folder Notes", "briosa.UtilityOperations",
        "GetFolderNotes", "/briosa.UtilityOperations/GetFolderNotes", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("notes", "Notes", WorkerMpValueKind.EditText)];

    public static WorkerMpCommand CreateCommand(Api.GetFolderNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Folder Path", WorkerMpValueKind.Text, new WorkerTextValue(request.FolderPath), "SetStringArg")],
            [new("Notes", WorkerMpValueKind.EditText, "GetEditTextArg")]);
    }

    public static Api.GetFolderNotesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetFolderNotesResult { Execution = completed.Details };
        result.Notes.AddRange(completed.Execution.OutputValues[0].RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
