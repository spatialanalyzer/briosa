using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class TrimLogFileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.trim_log_file", "Trim Log File", "briosa.UtilityOperations",
        "TrimLogFile", "/briosa.UtilityOperations/TrimLogFile", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TrimLogFileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Number of Entries to Keep", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasNumberOfEntriesToKeep ? request.NumberOfEntriesToKeep : 10),
                "SetIntegerArg")], []);
    }

    public static Api.TrimLogFileResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
