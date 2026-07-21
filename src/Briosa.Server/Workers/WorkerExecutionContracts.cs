using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal enum WorkerExecutionStatus
{
    Completed,
    Unavailable,
    ClientCancelled,
    WatchdogTimeout,
    WorkerFailure
}

internal sealed record WorkerExecutionOutcome(
    WorkerExecutionStatus Status,
    WorkerMpExecutionResult? Execution,
    WorkerConnectionSnapshot? Connection,
    string DiagnosticCode,
    int Generation);

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
