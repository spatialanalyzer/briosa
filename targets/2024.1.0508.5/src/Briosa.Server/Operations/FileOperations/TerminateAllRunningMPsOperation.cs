using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class TerminateAllRunningMPsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.terminate_all_running_mps", "Terminate All Running MPs", "briosa.FileOperations",
        "TerminateAllRunningMPs", "/briosa.FileOperations/TerminateAllRunningMPs", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TerminateAllRunningMPsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.TerminateAllRunningMPsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
