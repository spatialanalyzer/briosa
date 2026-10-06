using Briosa.Server.Security;
using Briosa.Server.Workers;

namespace Briosa.Server.Tests;

/// <summary>
/// Duration-class lookups for process-level supervisor tests whose synthetic
/// operation IDs are not registered operations. Production never uses these:
/// it fails closed for any ID without a reviewed classification row.
/// </summary>
internal static class TestDurationClasses
{
    /// <summary>
    /// Registered operations keep their reviewed class; synthetic test IDs are
    /// treated as <see cref="OperationDurationClass.Quick"/>.
    /// </summary>
    public static OperationDurationClass ReviewedOrSyntheticQuick(string operationId) =>
        WorkerExecutionPolicy.ReviewedDurationClass(operationId) is var reviewed &&
        reviewed != OperationDurationClass.Unspecified
            ? reviewed
            : OperationDurationClass.Quick;

    /// <summary>
    /// An execution policy for synthetic-ID supervisor tests, with the production
    /// quick default unless a test chooses its own budget.
    /// </summary>
    public static WorkerExecutionPolicy SyntheticQuickPolicy(TimeSpan? watchdogTimeout = null, int queueCapacity = 64) =>
        new(
            watchdogTimeout ?? WorkerProcessOptions.DefaultExecutionWatchdogTimeout,
            queueCapacity,
            durationClassOf: ReviewedOrSyntheticQuick);
}