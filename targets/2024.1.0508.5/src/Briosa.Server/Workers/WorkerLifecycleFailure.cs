namespace Briosa.Server.Workers;

internal enum WorkerLifecycleFailure
{
    None,
    StartupFailed,
    StartupTimeout,
    ConnectionFailed,
    ConnectionTimeout,
    IdentityRejected,
    ReadinessFailed,
    ReadinessTimeout,
    StopFailed,
    StopTimeout,
    CleanupIncomplete,
    Cancelled
}
