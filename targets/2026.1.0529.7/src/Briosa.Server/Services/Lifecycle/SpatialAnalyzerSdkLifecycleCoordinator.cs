using Briosa.Server.Workers;
using Briosa.Worker.Control;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Services;

[SuppressMessage(
    "Reliability",
    "CA2213:Disposable fields should be disposed",
    Justification = "The worker supervisor is a separately owned singleton disposed by the host.")]
internal sealed class SpatialAnalyzerSdkLifecycleCoordinator(
    IWorkerLifecycleController supervisor,
    SpatialAnalyzerSdkLifecycleStateProjection stateProjection,
    ISpatialAnalyzerLifecycleStateProvider applicationStateProvider) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ISpatialAnalyzerLifecycleStateProvider _applicationStateProvider =
        applicationStateProvider;
    private readonly SpatialAnalyzerSdkLifecycleStateProjection _stateProjection =
        stateProjection;
    private readonly IWorkerLifecycleController _supervisor = supervisor;

    public global::Briosa.SpatialAnalyzerSdkLifecycleState Current =>
        _stateProjection.Current;

    public async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> StartAsync(
        CancellationToken cancellationToken)
    {
        EnterTransition(cancellationToken);
        try
        {
            RequireStopped(_supervisor.Current);

            var transition = await CallSupervisorAsync(
                () => _supervisor.StartAsync(cancellationToken),
                RequireStopped).ConfigureAwait(false);
            if (!transition.Succeeded)
            {
                var snapshot = transition.Snapshot;
                var failed = SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(snapshot);
                if (snapshot.LifecycleTimedOut)
                {
                    throw SdkLifecycleException.DeadlineExceeded(
                        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
                        failed.DiagnosticCode,
                        failed,
                        global::Briosa.LifecycleRecoveryGuidance.RefreshState);
                }

                throw SdkLifecycleException.Unavailable(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkStartFailed,
                    failed.DiagnosticCode,
                    failed,
                    global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
            }

            return SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(transition.Snapshot);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> ConnectAsync(
        int expectedGeneration,
        bool reconnect,
        CancellationToken cancellationToken)
    {
        EnterTransition(cancellationToken);
        try
        {
            ValidateGeneration(expectedGeneration);
            var applicationBeforeConnect = await _applicationStateProvider
                .GetCurrentAsync(cancellationToken).ConfigureAwait(false);
            var current = _supervisor.Current;
            RequireConnectableGeneration(current);

            if (applicationBeforeConnect.ApplicationState is not
                (global::Briosa.SpatialAnalyzerApplicationState.Running or
                    global::Briosa.SpatialAnalyzerApplicationState.Ambiguous))
            {
                throw SdkLifecycleException.NotFound(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.ApplicationNotFound,
                    "spatial-analyzer-application-not-found",
                    Current,
                    global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange);
            }

            RequireConnectionTransition(current, reconnect);

            var transition = await CallSupervisorAsync(
                () => _supervisor.ConnectAsync(expectedGeneration, cancellationToken),
                fresh =>
                {
                    RequireGeneration(fresh, expectedGeneration);
                    RequireConnectableGeneration(fresh);
                    RequireConnectionTransition(fresh, reconnect);
                }).ConfigureAwait(false);

            if (!transition.Succeeded)
            {
                var snapshot = transition.Snapshot;
                var failed = SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(snapshot);
                if (snapshot.LifecycleFailure == WorkerLifecycleFailure.IdentityRejected)
                {
                    throw SdkLifecycleException.FailedPrecondition(
                        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.IdentityMismatch,
                        "runtime-identity-not-ready",
                        failed,
                        global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
                }

                var kind = failed.RecoveryState ==
                    global::Briosa.SpatialAnalyzerSdkRecoveryState.OperatorActionRequired
                        ? global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.OperatorActionRequired
                        : global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkConnectionFailed;
                var recoveryGuidance = kind ==
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.OperatorActionRequired
                        ? global::Briosa.LifecycleRecoveryGuidance.OperatorActionRequired
                        : failed.RecoveryState ==
                            global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable
                            ? global::Briosa.LifecycleRecoveryGuidance.RecoverSdkWithoutReplay
                            : global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange;
                if (snapshot.LifecycleTimedOut)
                {
                    throw SdkLifecycleException.DeadlineExceeded(
                        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
                        failed.DiagnosticCode,
                        failed,
                        recoveryGuidance);
                }

                if (kind ==
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.OperatorActionRequired)
                {
                    throw SdkLifecycleException.FailedPrecondition(
                        kind,
                        failed.DiagnosticCode ?? "sdk-operator-action-required",
                        failed,
                        recoveryGuidance);
                }

                throw SdkLifecycleException.Unavailable(
                    kind,
                    failed.DiagnosticCode ?? "sdk-connection-failed",
                    failed,
                    recoveryGuidance);
            }

            var applicationAfterConnect = await _applicationStateProvider
                .GetCurrentAsync(cancellationToken).ConfigureAwait(false);
            var associated = await CallSupervisorAsync(
                () => _supervisor.AssociateApplicationGenerationAsync(
                    expectedGeneration,
                    applicationBeforeConnect.HasApplicationGeneration &&
                    applicationAfterConnect.HasApplicationGeneration &&
                    applicationBeforeConnect.ApplicationGeneration ==
                        applicationAfterConnect.ApplicationGeneration
                        ? applicationAfterConnect.ApplicationGeneration
                        : null, cancellationToken),
                fresh => RequireGeneration(fresh, expectedGeneration)).ConfigureAwait(false);
            return SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(associated);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> StopAsync(
        int expectedGeneration,
        CancellationToken cancellationToken)
    {
        EnterTransition(cancellationToken);
        try
        {
            ValidateGeneration(expectedGeneration);
            RequireStoppable(_supervisor.Current);

            var transition = await CallSupervisorAsync(
                () => _supervisor.StopAsync(cancellationToken),
                RequireStoppable).ConfigureAwait(false);
            var snapshot = transition.Snapshot;
            var stopped = SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(snapshot);
            if (snapshot.LifecycleTimedOut)
            {
                throw SdkLifecycleException.DeadlineExceeded(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
                    stopped.DiagnosticCode,
                    stopped,
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            if (!transition.Succeeded)
            {
                throw SdkLifecycleException.Unavailable(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkStopFailed,
                    stopped.DiagnosticCode,
                    stopped,
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            return stopped;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> RecoverAsync(
        int expectedGeneration,
        global::Briosa.SpatialAnalyzerSdkRecoveryMode mode,
        CancellationToken cancellationToken)
    {
        EnterTransition(cancellationToken);
        try
        {
            ValidateGeneration(expectedGeneration);
            if (mode != global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay)
            {
                throw SdkLifecycleException.InvalidArgument(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Validation,
                    "sdk-recovery-mode-invalid",
                    Current);
            }

            RequireRecoverable(_supervisor.Current);

            var transition = await CallSupervisorAsync(
                () => _supervisor.RecoverSdkAsync(expectedGeneration, cancellationToken),
                fresh =>
                {
                    RequireGeneration(fresh, expectedGeneration);
                    RequireRecoverable(fresh);
                }).ConfigureAwait(false);
            if (!transition.Succeeded)
            {
                var snapshot = transition.Snapshot;
                var failed = SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(snapshot);
                if (snapshot.LifecycleTimedOut)
                {
                    throw SdkLifecycleException.DeadlineExceeded(
                        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
                        failed.DiagnosticCode,
                        failed,
                        global::Briosa.LifecycleRecoveryGuidance.RefreshState);
                }

                throw SdkLifecycleException.Unavailable(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkRecoveryFailed,
                    failed.DiagnosticCode,
                    failed,
                    global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
            }

            return SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(transition.Snapshot);
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private void ValidateGeneration(int expectedGeneration)
    {
        if (expectedGeneration <= 0)
        {
            throw SdkLifecycleException.InvalidArgument(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Validation,
                "sdk-generation-required",
                Current);
        }

        if (_supervisor.Current.Generation != expectedGeneration)
        {
            throw GenerationConflict(expectedGeneration);
        }
    }

    private void EnterTransition(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_gate.Wait(0, CancellationToken.None))
        {
            throw SdkLifecycleException.Aborted(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict,
                "sdk-lifecycle-transition-in-progress",
                Current,
                global::Briosa.LifecycleRecoveryGuidance.RefreshState);
        }
    }

    private SdkLifecycleException GenerationConflict(int expectedGeneration) =>
        SdkLifecycleException.Aborted(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict,
            expectedGeneration <= 0
                ? "sdk-generation-required"
                : "sdk-generation-stale",
            Current,
            global::Briosa.LifecycleRecoveryGuidance.RefreshState);

    // The supervisor re-checks state under its own gate and rejects a lost race with
    // InvalidOperationException; host shutdown can stop or dispose it outside this gate.
    // Classify both from fresh state so callers always receive typed lifecycle detail.
    private async Task<T> CallSupervisorAsync<T>(
        Func<Task<T>> call,
        Action<WorkerLifecycleSnapshot> requireStillValid)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            throw SdkLifecycleException.Unavailable(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning,
                "worker-supervisor-stopping",
                Current,
                global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange);
        }
        catch (InvalidOperationException)
        {
            var fresh = _supervisor.Current;
            requireStillValid(fresh);
            throw SdkLifecycleException.Aborted(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict,
                fresh.State is WorkerLifecycleState.Starting or WorkerLifecycleState.Stopping
                    ? "sdk-lifecycle-transition-in-progress"
                    : "sdk-lifecycle-state-changed",
                SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(fresh),
                global::Briosa.LifecycleRecoveryGuidance.RefreshState);
        }
    }

    private void RequireGeneration(WorkerLifecycleSnapshot current, int expectedGeneration)
    {
        if (current.Generation != expectedGeneration)
        {
            throw GenerationConflict(expectedGeneration);
        }
    }

    private static void RequireStopped(WorkerLifecycleSnapshot current)
    {
        if (current.State != WorkerLifecycleState.Stopped)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkAlreadyActive,
                "sdk-already-active",
                SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(current),
                global::Briosa.LifecycleRecoveryGuidance.RefreshState);
        }
    }

    private static void RequireStoppable(WorkerLifecycleSnapshot current)
    {
        if (current.State == WorkerLifecycleState.Stopped)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning,
                "sdk-not-running",
                SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(current),
                global::Briosa.LifecycleRecoveryGuidance.None);
        }
    }

    private static void RequireRecoverable(WorkerLifecycleSnapshot current)
    {
        if (current.State != WorkerLifecycleState.Degraded)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.RecoveryNotRequired,
                "sdk-recovery-not-required",
                SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(current),
                global::Briosa.LifecycleRecoveryGuidance.None);
        }
    }

    private static void RequireConnectableGeneration(WorkerLifecycleSnapshot current)
    {
        if (current.State == WorkerLifecycleState.Stopped)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning,
                "sdk-not-running",
                SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(current),
                global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange);
        }

        if (current.State == WorkerLifecycleState.Degraded)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkRecoveryRequired,
                "sdk-recovery-required",
                SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(current),
                global::Briosa.LifecycleRecoveryGuidance.RecoverSdkWithoutReplay);
        }
    }

    private static void RequireConnectionTransition(
        WorkerLifecycleSnapshot current,
        bool reconnect)
    {
        var state = SpatialAnalyzerSdkLifecycleStateProjection.ToPublicState(current);
        var connection = current.Connection;
        if (current.RuntimeIdentity?.ActivatedSdk.MatchState == Workers.RuntimeIdentityMatchState.Mismatch)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.IdentityMismatch,
                "activated-sdk-version-mismatch", state,
                global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
        }
        if (!reconnect && connection?.State == WorkerConnectionState.Connected)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkAlreadyConnected,
                "sdk-already-connected",
                state,
                global::Briosa.LifecycleRecoveryGuidance.RefreshState);
        }

        if (reconnect &&
            connection?.State == WorkerConnectionState.Connected &&
            connection.ExecutionReadinessState ==
                WorkerExecutionReadinessState.ExecutionReady)
        {
            throw SdkLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.ReconnectNotRequired,
                "sdk-reconnect-not-required",
                state,
                global::Briosa.LifecycleRecoveryGuidance.None);
        }

        // ConnectEx already attached this generation. The worker would only report
        // that attachment again: reconnecting cannot change the activated SDK or
        // supply missing identity evidence, so a new generation is required.
        if (reconnect &&
            current.State == WorkerLifecycleState.Ready &&
            connection?.State == WorkerConnectionState.Connected)
        {
            var identityNotReady = current.RuntimeIdentity?.AllowsExecution != true;
            throw SdkLifecycleException.FailedPrecondition(
                identityNotReady
                    ? global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.IdentityMismatch
                    : global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkAlreadyConnected,
                identityNotReady ? "runtime-identity-not-ready" : "sdk-already-connected",
                state,
                global::Briosa.LifecycleRecoveryGuidance.StopSdkFirst);
        }
    }
}
