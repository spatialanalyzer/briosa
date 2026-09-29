namespace Briosa.Server.Workers;

internal sealed class WorkerExecutionPolicy
{
    public WorkerExecutionPolicy(TimeSpan watchdogTimeout, int queueCapacity,
        int maxRetainedWorkBytes = 32 * 1024 * 1024)
    {
        if (watchdogTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(watchdogTimeout),
                watchdogTimeout,
                "The watchdog timeout must be positive.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(queueCapacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetainedWorkBytes, 1024 * 1024);
        WatchdogTimeout = watchdogTimeout;
        MaxRetainedWorkBytes = maxRetainedWorkBytes;
        QueueCapacity = queueCapacity;
    }

    public TimeSpan WatchdogTimeout { get; }

    public int QueueCapacity { get; }

    public int MaxRetainedWorkBytes { get; }
}
