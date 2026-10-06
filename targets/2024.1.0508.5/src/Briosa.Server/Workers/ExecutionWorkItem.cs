using System.Diagnostics;
using Briosa.Server.Security;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class ExecutionWorkItem(WorkerMpCommand command, Guid correlationId,
    int generation, ActivityContext parentContext, int retainedBytes,
    OperationDurationClass durationClass, TimeSpan executionBudget)
{
    private readonly TaskCompletionSource _admitted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<WorkerExecutionOutcome> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

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

    public void TrySetResult(WorkerExecutionOutcome outcome) =>
        _completion.TrySetResult(outcome);
}
