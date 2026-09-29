namespace Briosa.Server.Workers;

internal interface IWorkerCommandDispatcher
{
    Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerCommandSubmission submission,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
