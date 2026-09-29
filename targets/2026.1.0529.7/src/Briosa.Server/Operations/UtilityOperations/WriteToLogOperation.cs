using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class WriteToLogOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.write_to_log", "Write to Log", "briosa.UtilityOperations",
        "WriteToLog", "/briosa.UtilityOperations/WriteToLog", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.WriteToLogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Log Entry", WorkerMpValueKind.Text, new WorkerTextValue(request.LogEntry), "SetStringArg")], []);
    }

    public static Api.WriteToLogResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
