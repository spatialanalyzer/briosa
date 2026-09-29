namespace Briosa.Server.Workers;

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
