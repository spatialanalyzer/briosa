using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

internal sealed class RecordingWorker(
    Func<WorkerMpCommand, IReadOnlyList<WorkerMpOutputValue>>? outputFactory = null)
    : IWorkerCommandExecutor
{
    public List<WorkerMpCommand> Commands { get; } = [];

    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command, CancellationToken cancellationToken = default)
    {
        Commands.Add(command);
        var outputs = outputFactory?.Invoke(command) ?? [];
        var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.Completed, result, null, "completed", 1));
    }
}
