namespace Briosa.Server.Workers;

internal enum WorkerExecutionStatus
{
    PolicyDenied,
    Unsupported,
    Completed,
    Unavailable,
    ClientCancelled,
    WatchdogTimeout,
    WorkerFailure,
    RequestRejected,
    Overloaded
}
