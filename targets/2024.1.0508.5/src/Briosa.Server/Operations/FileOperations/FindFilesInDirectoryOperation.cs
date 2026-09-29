using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class FindFilesInDirectoryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.find_files_in_directory", "Find Files in Directory", "briosa.FileOperations",
        "FindFilesInDirectory", "/briosa.FileOperations/FindFilesInDirectory", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("files", "Files", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.FindFilesInDirectoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Directory", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasDirectory ? request.Directory : string.Empty), "SetStringArg"),
            new("File Name Pattern", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasFileNamePattern ? request.FileNamePattern : "*.*"), "SetStringArg"),
            new("Recursive?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasRecursive && request.Recursive), "SetBoolArg")
        ], [new("Files", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.FindFilesInDirectoryResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.FindFilesInDirectoryResult { Execution = completed.Details };
        result.Files.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
