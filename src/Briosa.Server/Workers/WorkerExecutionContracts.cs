using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal enum WorkerExecutionStatus
{
    PolicyDenied,
    Unsupported,
    Completed,
    Unavailable,
    ClientCancelled,
    WatchdogTimeout,
    WorkerFailure
}

internal enum WorkerExecutionDisposition
{
    NotStarted,
    StartedOutcomeUnknown,
    Completed
}

internal sealed record WorkerExecutionOutcome(
    WorkerExecutionStatus Status,
    WorkerExecutionDisposition ExecutionDisposition,
    WorkerMpExecutionResult? Execution,
    WorkerConnectionSnapshot? Connection,
    string DiagnosticCode,
    int Generation,
    Guid CorrelationId = default);

internal sealed record WorkerExecutionSnapshot(
    int QueueCapacity,
    int QueuedRequests,
    int WaitingForAdmission,
    int ActiveExecutions,
    int PeakQueuedRequests,
    long AdmittedRequests,
    long TerminalRequests,
    long ClientCancellationsBeforeAdmission,
    long ClientCancellationsAfterAdmission,
    long WatchdogTimeouts,
    long WorkerFailures);

internal sealed class WorkerExecutionPolicy
{
    public WorkerExecutionPolicy(TimeSpan watchdogTimeout, int queueCapacity)
    {
        if (watchdogTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(watchdogTimeout),
                watchdogTimeout,
                "The watchdog timeout must be positive.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(queueCapacity, 1);
        WatchdogTimeout = watchdogTimeout;
        QueueCapacity = queueCapacity;
    }

    public TimeSpan WatchdogTimeout { get; }

    public int QueueCapacity { get; }
}
