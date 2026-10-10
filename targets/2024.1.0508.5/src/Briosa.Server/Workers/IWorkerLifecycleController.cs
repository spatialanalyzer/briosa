namespace Briosa.Server.Workers;

internal interface IWorkerLifecycleController : IWorkerStatusProvider
{
    // Each task completes with the exchange's terminal result. The caller's token in
    // the acceptance applies only until the supervisor accepts the request (F10, #305).
    Task<WorkerLifecycleResult> StartAsync(LifecycleAcceptance acceptance);

    Task<WorkerLifecycleResult> ConnectAsync(
        int expectedGeneration,
        LifecycleAcceptance acceptance);

    Task<WorkerLifecycleResult> RecoverSdkAsync(
        int expectedGeneration,
        LifecycleAcceptance acceptance);

    Task<WorkerLifecycleResult> StopAsync(CancellationToken cancellationToken = default);

    Task<WorkerLifecycleSnapshot> AssociateApplicationGenerationAsync(
        int expectedGeneration,
        int? applicationGeneration,
        CancellationToken cancellationToken = default);
}
