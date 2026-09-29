using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Services;

internal sealed class SpatialAnalyzerSdkLifecycleStateProjection(
    IWorkerStatusProvider supervisor) : ISpatialAnalyzerSdkLifecycleStateProvider
{
    private readonly IWorkerStatusProvider _supervisor = supervisor;

    public global::Briosa.SpatialAnalyzerSdkLifecycleState Current => ToPublicState(_supervisor.Current);

    internal static global::Briosa.SpatialAnalyzerSdkLifecycleState ToPublicState(
        WorkerLifecycleSnapshot snapshot)
    {
        var connection = snapshot.Connection;
        var state = new global::Briosa.SpatialAnalyzerSdkLifecycleState
        {
            StateRevision = checked((ulong)snapshot.StateRevision),
            SdkState = ToSdkState(snapshot),
            ConnectionState = ToConnectionState(connection?.State),
            ExecutionReadinessState = ToReadinessState(
                connection?.ExecutionReadinessState),
            ReadyForMp = snapshot.ReadyForExecution,
            RecoveryState = ToRecoveryState(snapshot),
            DiagnosticCode = snapshot.DiagnosticCode
        };
        if (snapshot.State != WorkerLifecycleState.Stopped && snapshot.Generation > 0)
        {
            state.SdkGeneration = snapshot.Generation;
        }

        if (snapshot.ApplicationGeneration.HasValue &&
            connection?.State == WorkerConnectionState.Connected)
        {
            state.ApplicationGeneration = snapshot.ApplicationGeneration.Value;
        }

        if (snapshot.LastIncident is { } incident)
        {
            state.LastIncident = ToPublicIncident(incident);
        }

        return state;
    }

    private static global::Briosa.SpatialAnalyzerSdkIncident ToPublicIncident(
        WorkerIncidentSnapshot incident)
    {
        var result = new global::Briosa.SpatialAnalyzerSdkIncident
        {
            SdkGeneration = incident.Generation,
            TerminationKind = ToTerminationKind(incident),
            DiagnosticCode = incident.DiagnosticCode
        };
        if (incident.ExecutionDisposition.HasValue)
        {
            result.ExecutionDisposition = incident.ExecutionDisposition.Value switch
            {
                WorkerExecutionDisposition.NotStarted =>
                    global::Briosa.ExecutionDisposition.NotStarted,
                WorkerExecutionDisposition.StartedOutcomeUnknown =>
                    global::Briosa.ExecutionDisposition.StartedOutcomeUnknown,
                WorkerExecutionDisposition.Completed =>
                    global::Briosa.ExecutionDisposition.Completed,
                _ => global::Briosa.ExecutionDisposition.Unspecified
            };
        }

        if (!string.IsNullOrWhiteSpace(incident.OperationId))
        {
            result.OperationId = incident.OperationId;
        }

        return result;
    }

    private static global::Briosa.SpatialAnalyzerSdkState ToSdkState(
        WorkerLifecycleSnapshot snapshot) => snapshot.State switch
        {
            WorkerLifecycleState.Stopped => global::Briosa.SpatialAnalyzerSdkState.Stopped,
            WorkerLifecycleState.Stopping => global::Briosa.SpatialAnalyzerSdkState.Stopping,
            WorkerLifecycleState.Starting when snapshot.Connection?.ExecutionReadinessState == WorkerExecutionReadinessState.Verifying => global::Briosa.SpatialAnalyzerSdkState.Verifying,
            WorkerLifecycleState.Starting when snapshot.Connection?.State == WorkerConnectionState.Connecting => global::Briosa.SpatialAnalyzerSdkState.Connecting,
            WorkerLifecycleState.Starting => global::Briosa.SpatialAnalyzerSdkState.Starting,
            WorkerLifecycleState.Ready when snapshot.ReadyForExecution =>
                global::Briosa.SpatialAnalyzerSdkState.Ready,
            WorkerLifecycleState.Ready => global::Briosa.SpatialAnalyzerSdkState.Running,
            WorkerLifecycleState.Degraded => global::Briosa.SpatialAnalyzerSdkState.Faulted,
            _ => global::Briosa.SpatialAnalyzerSdkState.Unspecified
        };

    private static global::Briosa.SpatialAnalyzerConnectionState ToConnectionState(
        WorkerConnectionState? state) => state switch
        {
            WorkerConnectionState.Disconnected =>
                global::Briosa.SpatialAnalyzerConnectionState.Disconnected,
            WorkerConnectionState.Connecting =>
                global::Briosa.SpatialAnalyzerConnectionState.Connecting,
            WorkerConnectionState.Connected =>
                global::Briosa.SpatialAnalyzerConnectionState.Connected,
            WorkerConnectionState.Faulted =>
                global::Briosa.SpatialAnalyzerConnectionState.Faulted,
            WorkerConnectionState.Stopping =>
                global::Briosa.SpatialAnalyzerConnectionState.Stopping,
            _ => global::Briosa.SpatialAnalyzerConnectionState.Disconnected
        };

    private static global::Briosa.SpatialAnalyzerExecutionReadinessState ToReadinessState(
        WorkerExecutionReadinessState? state) => state switch
        {
            WorkerExecutionReadinessState.Unverified =>
                global::Briosa.SpatialAnalyzerExecutionReadinessState.Unverified,
            WorkerExecutionReadinessState.Verifying =>
                global::Briosa.SpatialAnalyzerExecutionReadinessState.Verifying,
            WorkerExecutionReadinessState.ExecutionReady =>
                global::Briosa.SpatialAnalyzerExecutionReadinessState.ExecutionReady,
            WorkerExecutionReadinessState.CompetingClientSuspected =>
                global::Briosa.SpatialAnalyzerExecutionReadinessState.CompetingClientSuspected,
            WorkerExecutionReadinessState.OperatorRecoveryRequired =>
                global::Briosa.SpatialAnalyzerExecutionReadinessState.OperatorRecoveryRequired,
            _ => global::Briosa.SpatialAnalyzerExecutionReadinessState.Unverified
        };

    private static global::Briosa.SpatialAnalyzerSdkRecoveryState ToRecoveryState(
        WorkerLifecycleSnapshot snapshot) => snapshot.State switch
        {
            WorkerLifecycleState.Degraded when snapshot.CleanupStatus is
                WorkerCleanupStatus.ExitUnconfirmed or WorkerCleanupStatus.ResourcesUnreleased =>
                    global::Briosa.SpatialAnalyzerSdkRecoveryState.OperatorActionRequired,
            WorkerLifecycleState.Degraded when snapshot.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.OperatorRecoveryRequired =>
                    global::Briosa.SpatialAnalyzerSdkRecoveryState.OperatorActionRequired,
            WorkerLifecycleState.Degraded =>
                global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            _ => global::Briosa.SpatialAnalyzerSdkRecoveryState.NotRequired
        };

    private static global::Briosa.SpatialAnalyzerSdkTerminationKind ToTerminationKind(
        WorkerIncidentSnapshot incident) => incident.Kind switch
    {
        WorkerIncidentKind.StartFailed => global::Briosa.SpatialAnalyzerSdkTerminationKind.StartFailed,
        WorkerIncidentKind.SdkProcessExited => global::Briosa.SpatialAnalyzerSdkTerminationKind.SdkProcessExited,
        WorkerIncidentKind.WatchdogTerminated => global::Briosa.SpatialAnalyzerSdkTerminationKind.WatchdogTerminated,
        WorkerIncidentKind.SdkConnectionLost => global::Briosa.SpatialAnalyzerSdkTerminationKind.SdkConnectionLost,
        WorkerIncidentKind.WorkerProcessExited => global::Briosa.SpatialAnalyzerSdkTerminationKind.WorkerProcessExited,
        WorkerIncidentKind.ControlChannelLost => global::Briosa.SpatialAnalyzerSdkTerminationKind.ControlChannelLost,
        _ => global::Briosa.SpatialAnalyzerSdkTerminationKind.Unspecified
    };
}
