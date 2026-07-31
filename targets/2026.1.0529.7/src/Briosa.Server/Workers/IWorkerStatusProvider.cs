namespace Briosa.Server.Workers;

internal interface IWorkerStatusProvider
{
    WorkerLifecycleSnapshot Current { get; }
}
