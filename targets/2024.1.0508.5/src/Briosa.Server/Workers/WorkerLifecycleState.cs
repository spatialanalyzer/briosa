namespace Briosa.Server.Workers;

internal enum WorkerLifecycleState
{
    Stopped,
    Starting,
    Ready,
    Degraded,
    Stopping
}
