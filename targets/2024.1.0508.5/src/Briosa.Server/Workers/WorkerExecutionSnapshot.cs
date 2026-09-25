namespace Briosa.Server.Workers;

internal sealed record WorkerExecutionSnapshot(
    int QueueCapacity,
    int QueuedRequests,
    int WaitingForAdmission,
    int ActiveExecutions,
    int PeakQueuedRequests,
    long AdmittedRequests,
    long TerminalRequests,
    long ClientCancellationsBeforeAdmission,
    long ClientCancellationsAfterAdmission,
    long WatchdogTimeouts,
    long WorkerFailures);
