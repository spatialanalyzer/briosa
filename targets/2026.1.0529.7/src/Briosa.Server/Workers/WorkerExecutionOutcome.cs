using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed record WorkerExecutionOutcome
{
    public WorkerExecutionOutcome(
        WorkerExecutionStatus Status,
        WorkerExecutionDisposition ExecutionDisposition,
        WorkerMpExecutionResult? Execution,
        WorkerConnectionSnapshot? Connection,
        string DiagnosticCode,
        int Generation,
        Guid CorrelationId = default)
    {
        var shapeIsValid = Status switch
        {
            WorkerExecutionStatus.Completed =>
                Execution is not null && ExecutionDisposition == DispositionFor(Execution),
            WorkerExecutionStatus.PolicyDenied or WorkerExecutionStatus.Unsupported or
                WorkerExecutionStatus.RequestRejected or WorkerExecutionStatus.Overloaded =>
                Execution is null && ExecutionDisposition == WorkerExecutionDisposition.NotStarted,
            WorkerExecutionStatus.WatchdogTimeout =>
                Execution is null && ExecutionDisposition == WorkerExecutionDisposition.StartedOutcomeUnknown,
            WorkerExecutionStatus.Unavailable or WorkerExecutionStatus.ClientCancelled or
                WorkerExecutionStatus.WorkerFailure =>
                Execution is null && ExecutionDisposition != WorkerExecutionDisposition.Completed,
            _ => false
        };
        if (!Enum.IsDefined(ExecutionDisposition) || Generation < 0 ||
            string.IsNullOrWhiteSpace(DiagnosticCode) || !shapeIsValid)
        {
            throw new ArgumentException("The worker execution outcome is contradictory.");
        }

        this.Status = Status;
        this.ExecutionDisposition = ExecutionDisposition;
        this.Execution = Execution;
        this.Connection = Connection;
        this.DiagnosticCode = DiagnosticCode;
        this.Generation = Generation;
        this.CorrelationId = CorrelationId;
    }

    public WorkerExecutionStatus Status { get; }
    public WorkerExecutionDisposition ExecutionDisposition { get; }
    public WorkerMpExecutionResult? Execution { get; }
    public WorkerConnectionSnapshot? Connection { get; }
    public string DiagnosticCode { get; }
    public int Generation { get; }
    public Guid CorrelationId { get; }

    public WorkerExecutionOutcome WithGeneration(int generation) =>
        new(Status, ExecutionDisposition, Execution, Connection, DiagnosticCode,
            generation, CorrelationId);

    public static WorkerExecutionOutcome FromWorkerResponse(
        WorkerExecutionResponse response,
        int generation,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new(
            response.Status == WorkerExecutionResponseStatus.Completed
                ? WorkerExecutionStatus.Completed : WorkerExecutionStatus.Unavailable,
            response.Execution is null
                ? WorkerExecutionDisposition.NotStarted : DispositionFor(response.Execution),
            response.Execution,
            response.Connection,
            response.DiagnosticCode ?? response.Execution?.DiagnosticCode ??
                "worker-execution-completed",
            generation,
            correlationId);
    }

    // An SDK call fault proves its phase: before ExecuteStep nothing started, and
    // only an output-getter fault follows a retrieved MP result.
    private static WorkerExecutionDisposition DispositionFor(WorkerMpExecutionResult execution) =>
        execution is WorkerArgumentsRejected or
            WorkerSdkCallFaulted { Phase: WorkerSdkCallPhase.BeforeExecute }
            ? WorkerExecutionDisposition.NotStarted
            : execution.MpResultRetrieved
                ? WorkerExecutionDisposition.Completed
                : WorkerExecutionDisposition.StartedOutcomeUnknown;
}
