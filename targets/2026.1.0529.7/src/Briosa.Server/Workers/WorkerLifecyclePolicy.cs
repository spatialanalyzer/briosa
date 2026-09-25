namespace Briosa.Server.Workers;

/// <summary>Timeouts for explicitly requested lifecycle work and health checks.</summary>
internal sealed class WorkerLifecyclePolicy
{
    public WorkerLifecyclePolicy(
        TimeSpan heartbeatInterval,
        TimeSpan heartbeatTimeout,
        TimeSpan startupTimeout,
        TimeSpan shutdownTimeout,
        int lifecycleHistoryCapacity = 256)
    {
        ThrowIfNonPositive(heartbeatInterval, nameof(heartbeatInterval));
        ThrowIfNonPositive(heartbeatTimeout, nameof(heartbeatTimeout));
        ThrowIfNonPositive(startupTimeout, nameof(startupTimeout));
        ThrowIfNonPositive(shutdownTimeout, nameof(shutdownTimeout));
        ArgumentOutOfRangeException.ThrowIfLessThan(lifecycleHistoryCapacity, 1);
        HeartbeatInterval = heartbeatInterval;
        HeartbeatTimeout = heartbeatTimeout;
        StartupTimeout = startupTimeout;
        ShutdownTimeout = shutdownTimeout;
        LifecycleHistoryCapacity = lifecycleHistoryCapacity;
    }

    public TimeSpan HeartbeatInterval { get; }
    public TimeSpan HeartbeatTimeout { get; }
    public TimeSpan StartupTimeout { get; }
    public TimeSpan ShutdownTimeout { get; }
    public int LifecycleHistoryCapacity { get; }

    private static void ThrowIfNonPositive(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(parameterName, value, "The duration must be positive.");
    }
}
