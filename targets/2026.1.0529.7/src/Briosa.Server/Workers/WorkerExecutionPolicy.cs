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
        out TimeSpan budget) =>
        TryGetExecutionBudget(operationId, effectiveDurationClass: null, out durationClass, out budget);

    /// <summary>
    /// Selects the execution budget for one admitted request. Admission policy
    /// supplies <paramref name="effectiveDurationClass"/>, for example
    /// <see cref="OperationDurationClass.Interactive"/> when the request turns on
    /// operator UI. Without it the operation's reviewed class applies. The
    /// effective class may only escalate: the budget is never shorter than the
    /// reviewed class's budget. Returns false, failing closed, when the operation
    /// has no reviewed class or the effective class is not a reviewed value.
    /// </summary>
    public bool TryGetExecutionBudget(
        string operationId,
        OperationDurationClass? effectiveDurationClass,
        out OperationDurationClass durationClass,
        out TimeSpan budget)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        durationClass = OperationDurationClass.Unspecified;
        budget = TimeSpan.Zero;
        var reviewed = _durationClassOf(operationId);
        var reviewedBudget = BudgetFor(reviewed);
        if (reviewedBudget <= TimeSpan.Zero) return false;
        if (effectiveDurationClass is not { } effective)
        {
            durationClass = reviewed;
            budget = reviewedBudget;
            return true;
        }

        var effectiveBudget = BudgetFor(effective);
        if (effectiveBudget <= TimeSpan.Zero) return false;
        (durationClass, budget) = effectiveBudget >= reviewedBudget
            ? (effective, effectiveBudget)
            : (reviewed, reviewedBudget);
        return true;
    }

    private TimeSpan BudgetFor(OperationDurationClass durationClass) => durationClass switch
    {
        OperationDurationClass.Quick => WatchdogTimeout,
        OperationDurationClass.LongRunning => LongRunningWatchdogTimeout,
        OperationDurationClass.Interactive => InteractiveWatchdogTimeout,
        _ => TimeSpan.Zero
    };

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
