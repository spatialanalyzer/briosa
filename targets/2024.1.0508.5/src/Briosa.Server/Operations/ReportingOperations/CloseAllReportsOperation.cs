using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class CloseAllReportsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.close_all_reports", "Close All Reports", "briosa.ReportingOperations",
        "CloseAllReports", "/briosa.ReportingOperations/CloseAllReports", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CloseAllReportsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.CloseAllReportsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
