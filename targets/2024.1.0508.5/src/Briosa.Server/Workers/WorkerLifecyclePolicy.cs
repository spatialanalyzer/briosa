namespace Briosa.Server.Workers;

/// <summary>Timeouts for explicitly requested lifecycle work and health checks.</summary>
/// <remarks>
/// These bounds never use an operation's duration-class execution budget.
/// <see cref="ReadinessProbeTimeout"/> bounds only the private execution-readiness
/// probe, independently of the execution watchdogs in <see cref="WorkerExecutionPolicy"/>.
/// </remarks>
internal sealed class WorkerLifecyclePolicy
{
    public WorkerLifecyclePolicy(
        TimeSpan heartbeatInterval,
        TimeSpan heartbeatTimeout,
        TimeSpan startupTimeout,
        TimeSpan shutdownTimeout,
        int lifecycleHistoryCapacity = 256,
        TimeSpan? readinessProbeTimeout = null)
    {
        var readiness = readinessProbeTimeout ?? WorkerProcessOptions.DefaultReadinessProbeTimeout;
        ThrowIfNonPositive(heartbeatInterval, nameof(heartbeatInterval));
        ThrowIfNonPositive(heartbeatTimeout, nameof(heartbeatTimeout));
        ThrowIfNonPositive(startupTimeout, nameof(startupTimeout));
        ThrowIfNonPositive(shutdownTimeout, nameof(shutdownTimeout));
        ThrowIfNonPositive(readiness, nameof(readinessProbeTimeout));
        ArgumentOutOfRangeException.ThrowIfLessThan(lifecycleHistoryCapacity, 1);
        HeartbeatInterval = heartbeatInterval;
        HeartbeatTimeout = heartbeatTimeout;
        StartupTimeout = startupTimeout;
        ShutdownTimeout = shutdownTimeout;
        ReadinessProbeTimeout = readiness;
        LifecycleHistoryCapacity = lifecycleHistoryCapacity;
    }

    public TimeSpan HeartbeatInterval { get; }
    public TimeSpan HeartbeatTimeout { get; }

    /// <summary>Bounds worker launch, COM activation, Ready, and ConnectEx.</summary>
    public TimeSpan StartupTimeout { get; }
    public TimeSpan ShutdownTimeout { get; }

    /// <summary>Bounds the private execution-readiness probe exchange.</summary>
    public TimeSpan ReadinessProbeTimeout { get; }
    public int LifecycleHistoryCapacity { get; }

    private static void ThrowIfNonPositive(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(parameterName, value, "The duration must be positive.");
    }
}
