using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using CoreProtocol = Briosa.Core.V1Alpha1;
using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

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
        "briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations",
        "GetWorkingDirectory",
        "/briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory",
        "read_only",
        CoreProtocol.OperationExecutionScope.GlobalStateRead,
        CoreProtocol.ReplaySafety.Safe,
        ["filesystem_metadata"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("directory", DirectoryArgumentName, WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(TargetProtocol.GetWorkingDirectoryRequest request)
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

    public static TargetProtocol.GetWorkingDirectoryResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var directory = completed.Execution.OutputValues.Single(value =>
            value.Name == DirectoryArgumentName &&
            value.Kind == WorkerMpValueKind.Text);
        return new TargetProtocol.GetWorkingDirectoryResult
        {
            Directory = directory.StringValue!,
            Execution = completed.Details
        };
    }
}
