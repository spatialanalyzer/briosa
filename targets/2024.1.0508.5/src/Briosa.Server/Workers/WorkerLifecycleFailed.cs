namespace Briosa.Server.Workers;

internal sealed record WorkerLifecycleFailed(WorkerLifecycleSnapshot Snapshot) : WorkerLifecycleResult(Snapshot);
