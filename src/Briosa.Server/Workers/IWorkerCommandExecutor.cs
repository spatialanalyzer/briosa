using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal interface IWorkerCommandExecutor
{
    Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        CancellationToken cancellationToken = default);
}
