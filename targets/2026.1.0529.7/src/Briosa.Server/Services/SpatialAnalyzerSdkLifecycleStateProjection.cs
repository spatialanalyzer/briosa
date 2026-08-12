using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Services;

internal interface ISpatialAnalyzerSdkLifecycleStateProvider
{
    global::Briosa.SpatialAnalyzerSdkLifecycleState Current { get; }
}

internal sealed class SpatialAnalyzerSdkLifecycleStateProjection(
    WorkerProcessSupervisor supervisor) : ISpatialAnalyzerSdkLifecycleStateProvider
{
    private readonly Lock _lock = new();
    private readonly WorkerProcessSupervisor _supervisor = supervisor;
    private int? _applicationGeneration;
    private long _lastWorkerRevision = -1;
    private ulong _stateRevision = 1;

    public global::Briosa.SpatialAnalyzerSdkLifecycleState Current
    {
        get
        {
            lock (_lock)
            {
                var snapshot = _supervisor.Current;
                ObserveWorkerRevision(snapshot);
                if (snapshot.Connection?.State != WorkerConnectionState.Connected &&
                    _applicationGeneration.HasValue)
                {
                    _applicationGeneration = null;
                    _stateRevision++;
                }

                return ToPublicState(snapshot, _applicationGeneration, _stateRevision);
            }
        }
    }

    public void MarkConnected(int? applicationGeneration)
    {
        lock (_lock)
        {
            ObserveWorkerRevision(_supervisor.Current);
            if (_applicationGeneration != applicationGeneration)
            {
                _applicationGeneration = applicationGeneration;
                _stateRevision++;
            }
        }
    }

    public void MarkDisconnected()
    {
        lock (_lock)
        {
            ObserveWorkerRevision(_supervisor.Current);
            if (_applicationGeneration.HasValue)
            {
                _applicationGeneration = null;
                _stateRevision++;
            }
        }
    }

    private void ObserveWorkerRevision(WorkerLifecycleSnapshot snapshot)
    {
        if (_lastWorkerRevision == snapshot.StateRevision)
        {
            return;
        }

        _lastWorkerRevision = snapshot.StateRevision;
        _stateRevision++;
    }

    private static global::Briosa.SpatialAnalyzerSdkLifecycleState ToPublicState(
        WorkerLifecycleSnapshot snapshot,
        int? applicationGeneration,
        ulong stateRevision)
    {
        var connection = snapshot.Connection;
        var state = new global::Briosa.SpatialAnalyzerSdkLifecycleState
        {
            StateRevision = stateRevision,
            SdkState = ToSdkState(snapshot),
            ConnectionState = ToConnectionState(connection?.State),
            ExecutionReadinessState = ToReadinessState(
                connection?.ExecutionReadinessState),
            ReadyForMp = snapshot.State == WorkerLifecycleState.Ready &&
                connection?.State == WorkerConnectionState.Connected &&
                connection.ExecutionReadinessState ==
                    WorkerExecutionReadinessState.ExecutionReady &&
                snapshot.RuntimeIdentity?.AllowsExecution == true,
            RecoveryState = ToRecoveryState(snapshot),
            DiagnosticCode = snapshot.DiagnosticCode
        };
        if (snapshot.State != WorkerLifecycleState.Stopped && snapshot.Generation > 0)
        {
            state.SdkGeneration = snapshot.Generation;
        }

        if (applicationGeneration.HasValue &&
            connection?.State == WorkerConnectionState.Connected)
        {
            state.ApplicationGeneration = applicationGeneration.Value;
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
            WorkerLifecycleState.Starting when snapshot.DiagnosticCode.Contains(
                "execution-readiness",
                StringComparison.Ordinal) => global::Briosa.SpatialAnalyzerSdkState.Verifying,
            WorkerLifecycleState.Starting when snapshot.DiagnosticCode.Contains(
                "connect",
                StringComparison.Ordinal) => global::Briosa.SpatialAnalyzerSdkState.Connecting,
            WorkerLifecycleState.Starting => global::Briosa.SpatialAnalyzerSdkState.Starting,
            WorkerLifecycleState.Ready when snapshot.Connection is
                {
                    State: WorkerConnectionState.Connected,
                    ExecutionReadinessState: WorkerExecutionReadinessState.ExecutionReady
                } && snapshot.RuntimeIdentity?.AllowsExecution == true =>
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
            WorkerLifecycleState.Degraded when snapshot.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.OperatorRecoveryRequired =>
                    global::Briosa.SpatialAnalyzerSdkRecoveryState.OperatorActionRequired,
            WorkerLifecycleState.Degraded =>
                global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            _ => global::Briosa.SpatialAnalyzerSdkRecoveryState.NotRequired
        };

    private static global::Briosa.SpatialAnalyzerSdkTerminationKind ToTerminationKind(
        WorkerIncidentSnapshot incident)
    {
        if (incident.DiagnosticCode.Contains("activation", StringComparison.Ordinal) ||
            incident.DiagnosticCode.Contains("startup", StringComparison.Ordinal) ||
            incident.DiagnosticCode.Contains("sdk-not-started", StringComparison.Ordinal))
        {
            return global::Briosa.SpatialAnalyzerSdkTerminationKind.StartFailed;
        }

        if (incident.DiagnosticCode.Contains("sdk-process-exited", StringComparison.Ordinal))
        {
            return global::Briosa.SpatialAnalyzerSdkTerminationKind.SdkProcessExited;
        }

        if (incident.DiagnosticCode.Contains("watchdog", StringComparison.Ordinal))
        {
            return global::Briosa.SpatialAnalyzerSdkTerminationKind.WatchdogTerminated;
        }

        if (incident.DiagnosticCode.Contains("connection", StringComparison.Ordinal))
        {
            return global::Briosa.SpatialAnalyzerSdkTerminationKind.SdkConnectionLost;
        }

        if (incident.Termination == WorkerTerminationKind.Crash)
        {
            return global::Briosa.SpatialAnalyzerSdkTerminationKind.WorkerProcessExited;
        }

        return global::Briosa.SpatialAnalyzerSdkTerminationKind.ControlChannelLost;
    }
}
