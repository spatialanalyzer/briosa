using Briosa.Server.Security;

namespace Briosa.Server.Workers;

/// <summary>
/// Execution admission limits and the per-duration-class execution budgets.
/// Each budget is enforced by the same supervisor watchdog: expiry after
/// dispatch retires the worker and leaves the operation
/// <see cref="WorkerExecutionDisposition.StartedOutcomeUnknown"/>.
/// </summary>
internal sealed class WorkerExecutionPolicy
{
    private readonly Func<string, OperationDurationClass> _durationClassOf;

    /// <param name="watchdogTimeout">Execution budget for <c>quick</c> operations.</param>
    /// <param name="queueCapacity">Maximum admitted requests.</param>
    /// <param name="maxRetainedWorkBytes">Retained-work reservation budget.</param>
    /// <param name="longRunningWatchdogTimeout">Execution budget for <c>long_running</c> operations.</param>
    /// <param name="interactiveWatchdogTimeout">Execution budget for <c>interactive</c> operations.</param>
    /// <param name="durationClassOf">
    /// Reviewed duration class of an operation ID. Defaults to the target's
    /// <see cref="OperationClassification"/> table; <see cref="OperationDurationClass.Unspecified"/>
    /// means unreviewed and the operation is not executed.
    /// </param>
    public WorkerExecutionPolicy(TimeSpan watchdogTimeout, int queueCapacity,
        int maxRetainedWorkBytes = 32 * 1024 * 1024,
        TimeSpan? longRunningWatchdogTimeout = null,
        TimeSpan? interactiveWatchdogTimeout = null,
        Func<string, OperationDurationClass>? durationClassOf = null)
    {
        ThrowIfNonPositive(watchdogTimeout, nameof(watchdogTimeout));
        var longRunning = longRunningWatchdogTimeout ??
            WorkerProcessOptions.DefaultLongRunningExecutionWatchdogTimeout;
        var interactive = interactiveWatchdogTimeout ??
            WorkerProcessOptions.DefaultInteractiveExecutionWatchdogTimeout;
        ThrowIfNonPositive(longRunning, nameof(longRunningWatchdogTimeout));
        ThrowIfNonPositive(interactive, nameof(interactiveWatchdogTimeout));

        ArgumentOutOfRangeException.ThrowIfLessThan(queueCapacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetainedWorkBytes, 1024 * 1024);
        WatchdogTimeout = watchdogTimeout;
        LongRunningWatchdogTimeout = longRunning;
        InteractiveWatchdogTimeout = interactive;
        MaxRetainedWorkBytes = maxRetainedWorkBytes;
        QueueCapacity = queueCapacity;
        _durationClassOf = durationClassOf ?? ReviewedDurationClass;
    }

    /// <summary>Execution budget for <c>quick</c> operations.</summary>
    public TimeSpan WatchdogTimeout { get; }

    /// <summary>Execution budget for <c>long_running</c> operations.</summary>
    public TimeSpan LongRunningWatchdogTimeout { get; }

    /// <summary>Execution budget for <c>interactive</c> operations.</summary>
    public TimeSpan InteractiveWatchdogTimeout { get; }

    public int QueueCapacity { get; }

    public int MaxRetainedWorkBytes { get; }

    /// <summary>
    /// Selects the execution budget from the operation's reviewed duration class.
    /// Returns false, failing closed, when the operation has no reviewed class.
    /// </summary>
    public bool TryGetExecutionBudget(
        string operationId,
        out OperationDurationClass durationClass,
        out TimeSpan budget)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        durationClass = _durationClassOf(operationId);
        budget = durationClass switch
        {
            OperationDurationClass.Quick => WatchdogTimeout,
            OperationDurationClass.LongRunning => LongRunningWatchdogTimeout,
            OperationDurationClass.Interactive => InteractiveWatchdogTimeout,
            _ => TimeSpan.Zero
        };
        if (budget > TimeSpan.Zero) return true;
        durationClass = OperationDurationClass.Unspecified;
        return false;
    }

    /// <summary>The reviewed duration class, or <c>Unspecified</c> for a missing or unreviewed row.</summary>
    internal static OperationDurationClass ReviewedDurationClass(string operationId) =>
        OperationClassification.Find(operationId) is { IsReviewed: true, Row: { } row }
            ? row.Duration
            : OperationDurationClass.Unspecified;

    private static void ThrowIfNonPositive(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The watchdog timeout must be positive.");
        }
    }
}
