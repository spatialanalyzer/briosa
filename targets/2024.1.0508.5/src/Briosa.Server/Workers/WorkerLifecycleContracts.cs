using Briosa.Worker.Control;
using System.Diagnostics.CodeAnalysis;

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
    WorkerConnectionSnapshot? Connection,
    DateTimeOffset TransitionedAt,
    ExactTargetIdentitySnapshot? RuntimeIdentity = null,
    long StateRevision = 0,
    WorkerIncidentSnapshot? LastIncident = null);

internal sealed record WorkerIncidentSnapshot(
    int Generation,
    WorkerTerminationKind Termination,
    WorkerExecutionDisposition? ExecutionDisposition,
    string? OperationId,
    string DiagnosticCode);

[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "This internal exception must always retain both generation values.")]
internal sealed class WorkerGenerationConflictException(
    int expectedGeneration,
    int actualGeneration) : InvalidOperationException(
        $"Expected SDK generation '{expectedGeneration}', but the current generation is '{actualGeneration}'.")
{
    public int ExpectedGeneration { get; } = expectedGeneration;

    public int ActualGeneration { get; } = actualGeneration;
}

internal sealed class WorkerRestartPolicy
{
    public WorkerRestartPolicy(
        int maximumRestarts,
        TimeSpan restartWindow,
        TimeSpan heartbeatInterval,
        TimeSpan heartbeatTimeout,
        TimeSpan startupTimeout,
        TimeSpan shutdownTimeout,
        TimeSpan restartDelay,
        int lifecycleHistoryCapacity = 256)
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

        ArgumentOutOfRangeException.ThrowIfLessThan(lifecycleHistoryCapacity, 1);

        MaximumRestarts = maximumRestarts;
        RestartWindow = restartWindow;
        HeartbeatInterval = heartbeatInterval;
        HeartbeatTimeout = heartbeatTimeout;
        StartupTimeout = startupTimeout;
        ShutdownTimeout = shutdownTimeout;
        RestartDelay = restartDelay;
        LifecycleHistoryCapacity = lifecycleHistoryCapacity;
    }

    public int MaximumRestarts { get; }

    public TimeSpan RestartWindow { get; }

    public TimeSpan HeartbeatInterval { get; }

    public TimeSpan HeartbeatTimeout { get; }

    public TimeSpan StartupTimeout { get; }

    public TimeSpan ShutdownTimeout { get; }

    public TimeSpan RestartDelay { get; }

    public int LifecycleHistoryCapacity { get; }

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
