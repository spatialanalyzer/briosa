using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed record WorkerLifecycleSnapshot(
    WorkerLifecycleState State,
    int Generation,
    int? ProcessId,
    int RecoveryCount,
    WorkerTerminationKind LastTermination,
    string DiagnosticCode,
    WorkerConnectionSnapshot? Connection,
    DateTimeOffset TransitionedAt,
    ExactTargetIdentitySnapshot? RuntimeIdentity = null,
    long StateRevision = 0,
    WorkerIncidentSnapshot? LastIncident = null,
    bool AdmissionOpen = false,
    int? ApplicationGeneration = null)
{
    public bool ReadyForExecution =>
        State == WorkerLifecycleState.Ready && AdmissionOpen &&
        Connection is
        {
            State: WorkerConnectionState.Connected,
            Failure: WorkerConnectionFailure.None,
            ExecutionReadinessState: WorkerExecutionReadinessState.ExecutionReady
        } && RuntimeIdentity?.AllowsExecution == true;
}
