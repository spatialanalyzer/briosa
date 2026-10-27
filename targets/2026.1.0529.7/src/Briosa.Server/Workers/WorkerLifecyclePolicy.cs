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

    /// <summary>
    /// The longest an accepted Start, Connect, or Recover takes to its typed outcome,
    /// failure cleanup included (#321). Connect runs ConnectEx and the application
    /// association under the startup bound, the readiness probe, a liveness ping, and
    /// forced cleanup. Recover drains the heartbeat and cleans up the faulted worker,
    /// then starts a replacement whose Ready can report an attached SDK, which runs
    /// the probe and its cleanup. Start alone is never longer than Recover.
    /// </summary>
    public TimeSpan AcceptedExchangeBound
    {
        get
        {
            var start = StartupTimeout + ReadinessProbeTimeout + ShutdownTimeout;
            var connect = StartupTimeout + ReadinessProbeTimeout + StartupTimeout +
                HeartbeatTimeout + ShutdownTimeout;
            var recover = HeartbeatTimeout + ShutdownTimeout + start;
            return connect > recover ? connect : recover;
        }
    }

    /// <summary>
    /// The worker stop after any accepted exchange: heartbeat drain, stop exchange, and
    /// forced cleanup. An MP command still in flight adds its own execution budget.
    /// </summary>
    public TimeSpan StopBound => HeartbeatTimeout + ShutdownTimeout + ShutdownTimeout;

    private static void ThrowIfNonPositive(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(parameterName, value, "The duration must be positive.");
    }
}
