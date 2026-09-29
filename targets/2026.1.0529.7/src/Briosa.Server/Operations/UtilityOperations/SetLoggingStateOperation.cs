using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetLoggingStateOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_logging_state", "Set Logging State", "briosa.UtilityOperations",
        "SetLoggingState", "/briosa.UtilityOperations/SetLoggingState", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetLoggingStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Active?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Active), "SetBoolArg")], []);
    }

    public static Api.SetLoggingStateResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
