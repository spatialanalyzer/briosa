using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Services;

internal readonly record struct OperationAuditSummary(
    string ExecutionDisposition,
    string MpOutcome,
    string OutputRetrievalOutcome,
    long? SdkDurationMilliseconds,
    int? MpResultCode)
{
    public static OperationAuditSummary Create(WorkerExecutionOutcome? outcome)
    {
        var execution = outcome?.Execution;
        if (execution is null)
        {
            return new OperationAuditSummary(
                FormatExecutionDisposition(outcome?.ExecutionDisposition),
                outcome?.ExecutionDisposition ==
                    WorkerExecutionDisposition.StartedOutcomeUnknown
                        ? "outcome_unknown"
                        : "not_started",
                "not_attempted",
                SdkDurationMilliseconds: null,
                MpResultCode: null);
        }

        if (!execution.ExecuteStepReturned)
        {
            return new OperationAuditSummary(
                FormatExecutionDisposition(outcome?.ExecutionDisposition),
                execution is WorkerArgumentsRejected
                    ? "argument_rejected"
                    : "execute_step_rejected",
                "not_attempted",
                execution.DurationMilliseconds,
                execution.MpResultCode);
        }

        if (!execution.MpResultRetrieved)
        {
            return new OperationAuditSummary(
                FormatExecutionDisposition(outcome?.ExecutionDisposition),
                "result_unavailable",
                "not_attempted",
                execution.DurationMilliseconds,
                execution.MpResultCode);
        }

        if (!execution.MpSucceeded)
        {
            return new OperationAuditSummary(
                FormatExecutionDisposition(outcome?.ExecutionDisposition),
                "failed",
                "not_attempted",
                execution.DurationMilliseconds,
                execution.MpResultCode);
        }

        return new OperationAuditSummary(
            FormatExecutionDisposition(outcome?.ExecutionDisposition),
            "succeeded",
            execution is not WorkerMpOutputsUnavailable && execution.OutputValues.All(output => output.Retrieved)
                ? "retrieved"
                : "failed",
            execution.DurationMilliseconds,
            execution.MpResultCode);
    }

    private static string FormatExecutionDisposition(
        WorkerExecutionDisposition? disposition) =>
        disposition switch
        {
            WorkerExecutionDisposition.NotStarted => "not_started",
            WorkerExecutionDisposition.StartedOutcomeUnknown => "started_outcome_unknown",
            WorkerExecutionDisposition.Completed => "completed",
            _ => "unspecified"
        };
}
