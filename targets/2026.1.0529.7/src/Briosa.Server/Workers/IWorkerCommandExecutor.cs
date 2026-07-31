using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal interface IWorkerCommandExecutor
{
    Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        CancellationToken cancellationToken = default);

    Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(command, cancellationToken);
}
