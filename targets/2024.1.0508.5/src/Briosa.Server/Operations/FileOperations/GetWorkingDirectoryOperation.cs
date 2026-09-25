using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

/// <summary>
/// Implements the exact "Get Working Directory" MP command contract.
/// </summary>
internal static class GetWorkingDirectoryOperation
{
    public const string OperationId = "file_operations.get_working_directory";
    public const string StepName = "Get Working Directory";
    public const string DirectoryArgumentName = "Directory";
    public const string DirectoryGetter = "GetStringArg";

    public static OperationDescriptor Descriptor { get; } = new(
        OperationId,
        StepName,
        "briosa.FileOperations",
        "GetWorkingDirectory",
        "/briosa.FileOperations/GetWorkingDirectory",
        "read_only",
        Api.OperationExecutionScope.GlobalStateRead,
        Api.ReplaySafety.Safe,
        ["filesystem_metadata"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("directory", DirectoryArgumentName, WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetWorkingDirectoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new WorkerMpCommand(
            OperationId,
            StepName,
            inputArguments: [],
            outputArguments:
            [
                new(DirectoryArgumentName, WorkerMpValueKind.Text, DirectoryGetter)
            ]);
    }

    // OperationExecutor validates ordered output shape and retrieval before mapping.
    public static Api.GetWorkingDirectoryResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Directory = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
