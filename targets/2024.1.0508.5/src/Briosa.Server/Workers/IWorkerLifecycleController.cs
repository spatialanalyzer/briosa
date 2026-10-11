namespace Briosa.Server.Workers;

internal interface IWorkerLifecycleController : IWorkerStatusProvider
{
    // Each task completes with the exchange's terminal result. The caller's token in
    // the acceptance applies only until the supervisor accepts the request (F10, #305).
    Task<WorkerLifecycleResult> StartAsync(LifecycleAcceptance acceptance);

    // The supervisor calls applicationGeneration inside the accepted exchange, after
    // the readiness probe succeeds and before MP admission opens, so readiness and the
    // application association are published together.
    Task<WorkerLifecycleResult> ConnectAsync(
        int expectedGeneration,
        LifecycleAcceptance acceptance,
        Func<CancellationToken, Task<int?>>? applicationGeneration);

    Task<WorkerLifecycleResult> RecoverSdkAsync(
        int expectedGeneration,
        LifecycleAcceptance acceptance);

    Task<WorkerLifecycleResult> StopAsync(CancellationToken cancellationToken = default);
}
