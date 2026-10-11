using System.Diagnostics;
using Briosa.Server.Security;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class ExecutionWorkItem(WorkerMpCommand command, Guid correlationId,
    int generation, ActivityContext parentContext, int retainedBytes,
    OperationDurationClass durationClass, TimeSpan executionBudget)
{
    // Each admitted item leaves Queued exactly once: the consumer claims it for
    // resolution or dispatch, or its caller abandons it before the claim (F9, #305).
    private const int Queued = 0;
    private const int Claimed = 1;
    private const int Abandoned = 2;
    private readonly TaskCompletionSource _admitted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<WorkerExecutionOutcome> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _state;

    // WorkerMpCommand already owns immutable argument collections.
    public WorkerMpCommand Command { get; } = command;
    public int RetainedBytes { get; } = retainedBytes;
    public Guid CorrelationId { get; } = correlationId;
    public int Generation { get; } = generation;
    public ActivityContext ParentContext { get; } = parentContext;

    // Chosen on the host at admission from the reviewed duration class.
    public OperationDurationClass DurationClass { get; } = durationClass;
    public TimeSpan ExecutionBudget { get; } = executionBudget;
    public long AdmittedAt { get; set; }
    public DateTimeOffset AdmittedUtc { get; set; }
    public double AdmissionMilliseconds { get; set; }
    public double QueueMilliseconds { get; set; }
    public double ExchangeMilliseconds { get; set; }
    public WorkerExecutionDisposition DispatchDisposition { get; set; } = WorkerExecutionDisposition.NotStarted;
    public WorkerExecutionOutcome? ResolvedOutcome { get; set; }


    public Task<WorkerExecutionOutcome> Task => _completion.Task;

    public Task WaitUntilAdmitted() => _admitted.Task;

    public void MarkAdmitted() => _admitted.TrySetResult();

    public bool IsAbandoned => Volatile.Read(ref _state) == Abandoned;

    /// <summary>
    /// Called by the consumer before it resolves or dispatches the item. Returns
    /// true if it holds the claim, or false if the caller abandoned the item first,
    /// which then must never reach the worker.
    /// </summary>
    public bool TryClaim() => Interlocked.CompareExchange(ref _state, Claimed, Queued) != Abandoned;

    /// <summary>
    /// Called by a caller that stopped waiting. Returns true only if the consumer
    /// has not claimed the item, so execution provably did not start.
    /// </summary>
    public bool TryAbandon() => Interlocked.CompareExchange(ref _state, Abandoned, Queued) == Queued;

    public void TrySetResult(WorkerExecutionOutcome outcome) =>
        _completion.TrySetResult(outcome);
}
