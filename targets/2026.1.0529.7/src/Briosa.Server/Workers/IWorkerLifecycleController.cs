namespace Briosa.Server.Workers;

internal interface IWorkerLifecycleController : IWorkerStatusProvider
{
    Task<WorkerLifecycleResult> StartAsync(CancellationToken cancellationToken = default);

    Task<WorkerLifecycleResult> ConnectAsync(
        int expectedGeneration,
        CancellationToken cancellationToken = default);

    Task<WorkerLifecycleResult> RecoverSdkAsync(
        int expectedGeneration,
        CancellationToken cancellationToken = default);

    Task<WorkerLifecycleResult> StopAsync(CancellationToken cancellationToken = default);

    Task<WorkerLifecycleSnapshot> AssociateApplicationGenerationAsync(
        int expectedGeneration,
        int? applicationGeneration,
        CancellationToken cancellationToken = default);
}
