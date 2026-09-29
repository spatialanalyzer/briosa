using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class FindSubDirectoriesInDirectoryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.find_sub_directories_in_directory", "Find Sub-Directories in Directory",
        "briosa.FileOperations", "FindSubDirectoriesInDirectory",
        "/briosa.FileOperations/FindSubDirectoriesInDirectory", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("sub_directories", "Sub-Directories", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.FindSubDirectoriesInDirectoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Directory", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasDirectory ? request.Directory : string.Empty), "SetStringArg"),
            new("Recursive?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasRecursive && request.Recursive), "SetBoolArg")
        ], [new("Sub-Directories", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.FindSubDirectoriesInDirectoryResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.FindSubDirectoriesInDirectoryResult { Execution = completed.Details };
        result.SubDirectories.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
