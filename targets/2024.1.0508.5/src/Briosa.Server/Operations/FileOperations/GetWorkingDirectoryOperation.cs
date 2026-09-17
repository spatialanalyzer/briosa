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

    public static Api.GetWorkingDirectoryResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var directory = completed.Execution.OutputValues.Single(value =>
            value.Name == DirectoryArgumentName &&
            value.Kind == WorkerMpValueKind.Text);
        return new Api.GetWorkingDirectoryResult
        {
            Directory = directory.StringValue!,
            Execution = completed.Details
        };
    }
}
