using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class CloseAllWatchWindowsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.close_all_watch_windows", "Close All Watch Windows", "briosa.UtilityOperations",
        "CloseAllWatchWindows", "/briosa.UtilityOperations/CloseAllWatchWindows", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CloseAllWatchWindowsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.CloseAllWatchWindowsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
