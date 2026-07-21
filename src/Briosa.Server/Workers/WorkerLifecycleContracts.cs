namespace Briosa.Server.Workers;

internal enum WorkerLifecycleState
{
    Stopped,
    Starting,
    Ready,
    Degraded
}

internal enum WorkerTerminationKind
{
    None,
    Graceful,
    Crash,
    Forced
}

internal sealed record WorkerLifecycleSnapshot(
    WorkerLifecycleState State,
    int Generation,
    int? ProcessId,
    int RestartCount,
    WorkerTerminationKind LastTermination,
    string DiagnosticCode,
    DateTimeOffset TransitionedAt);

internal sealed class WorkerRestartPolicy
{
    public WorkerRestartPolicy(
        int maximumRestarts,
        TimeSpan restartWindow,
        TimeSpan heartbeatInterval,
        TimeSpan heartbeatTimeout,
        TimeSpan startupTimeout,
        TimeSpan shutdownTimeout,
        TimeSpan restartDelay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumRestarts);
        ThrowIfNonPositive(restartWindow, nameof(restartWindow));
        ThrowIfNonPositive(heartbeatInterval, nameof(heartbeatInterval));
        ThrowIfNonPositive(heartbeatTimeout, nameof(heartbeatTimeout));
        ThrowIfNonPositive(startupTimeout, nameof(startupTimeout));
        ThrowIfNonPositive(shutdownTimeout, nameof(shutdownTimeout));
        if (restartDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(restartDelay),
                restartDelay,
                "The restart delay cannot be negative.");
        }

        MaximumRestarts = maximumRestarts;
        RestartWindow = restartWindow;
        HeartbeatInterval = heartbeatInterval;
        HeartbeatTimeout = heartbeatTimeout;
        StartupTimeout = startupTimeout;
        ShutdownTimeout = shutdownTimeout;
        RestartDelay = restartDelay;
    }

    public int MaximumRestarts { get; }

    public TimeSpan RestartWindow { get; }

    public TimeSpan HeartbeatInterval { get; }

    public TimeSpan HeartbeatTimeout { get; }

    public TimeSpan StartupTimeout { get; }

    public TimeSpan ShutdownTimeout { get; }

    public TimeSpan RestartDelay { get; }

    private static void ThrowIfNonPositive(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The duration must be positive.");
        }
    }
}
