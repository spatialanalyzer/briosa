namespace Briosa.Server.Workers;

internal enum WorkerIncidentKind
{
    StartFailed,
    SdkProcessExited,
    WatchdogTerminated,
    SdkConnectionLost,
    WorkerProcessExited,
    ControlChannelLost
}
