using System.Diagnostics;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class ExecutionWorkItem(WorkerMpCommand command, Guid correlationId,
    int generation, ActivityContext parentContext)
{
    private readonly TaskCompletionSource _admitted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<WorkerExecutionOutcome> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // WorkerMpCommand already owns immutable argument collections.
    public WorkerMpCommand Command { get; } = command;
    public Guid CorrelationId { get; } = correlationId;
    public int Generation { get; } = generation;
    public ActivityContext ParentContext { get; } = parentContext;
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

