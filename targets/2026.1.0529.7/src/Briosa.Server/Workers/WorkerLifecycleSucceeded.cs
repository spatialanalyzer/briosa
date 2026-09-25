namespace Briosa.Server.Workers;

internal sealed record WorkerLifecycleSucceeded(WorkerLifecycleSnapshot Snapshot) : WorkerLifecycleResult(Snapshot);
