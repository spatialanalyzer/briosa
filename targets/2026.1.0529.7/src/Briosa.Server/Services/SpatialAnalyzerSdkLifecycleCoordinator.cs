using Briosa.Server.Workers;
using Briosa.Worker.Control;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Services;

[SuppressMessage(
    "Reliability",
    "CA2213:Disposable fields should be disposed",
    Justification = "The worker supervisor is a separately owned singleton disposed by the host.")]
internal sealed class SpatialAnalyzerSdkLifecycleCoordinator(
    WorkerProcessSupervisor supervisor,
    SpatialAnalyzerSdkLifecycleStateProjection stateProjection,
    ISpatialAnalyzerLifecycleStateProvider applicationStateProvider) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ISpatialAnalyzerLifecycleStateProvider _applicationStateProvider =
        applicationStateProvider;
    private readonly SpatialAnalyzerSdkLifecycleStateProjection _stateProjection =
        stateProjection;
    private readonly WorkerProcessSupervisor _supervisor = supervisor;

    public global::Briosa.SpatialAnalyzerSdkLifecycleState Current =>
        _stateProjection.Current;

    public async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState> StartAsync(
        CancellationToken cancellationToken)
    {
        EnterTransition(cancellationToken);
        try
        {
            if (_supervisor.Current.State != WorkerLifecycleState.Stopped)
            {
                throw SdkLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkAlreadyActive,
                    "sdk-already-active",
                    Current,
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            if (!await _supervisor.StartAsync(cancellationToken).ConfigureAwait(false))
            {
                var failed = Current;
                if (IsTimeoutDiagnostic(failed.DiagnosticCode))
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

            _stateProjection.MarkDisconnected();
            return Current;
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
            if (current.State == WorkerLifecycleState.Stopped)
            {
                throw SdkLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning,
                    "sdk-not-running",
                    Current,
                    global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange);
            }

            if (current.State == WorkerLifecycleState.Degraded)
            {
                throw SdkLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkRecoveryRequired,
                    "sdk-recovery-required",
                    Current,
                    global::Briosa.LifecycleRecoveryGuidance.RecoverSdkWithoutReplay);
            }

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

            var connection = current.Connection;
            if (!reconnect && connection?.State == WorkerConnectionState.Connected)
            {
                throw SdkLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkAlreadyConnected,
                    "sdk-already-connected",
                    Current,
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
                    Current,
                    global::Briosa.LifecycleRecoveryGuidance.None);
            }

            bool connected;
            try
            {
                connected = await _supervisor.ConnectAsync(
                    expectedGeneration,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (WorkerGenerationConflictException)
            {
                throw GenerationConflict(expectedGeneration);
            }

            if (!connected)
            {
                _stateProjection.MarkDisconnected();
                var failed = Current;
                if (string.Equals(
                        failed.DiagnosticCode,
                        "runtime-identity-not-ready",
                        StringComparison.Ordinal))
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
                if (IsTimeoutDiagnostic(failed.DiagnosticCode))
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
            _stateProjection.MarkConnected(
                applicationBeforeConnect.HasApplicationGeneration &&
                applicationAfterConnect.HasApplicationGeneration &&
                applicationBeforeConnect.ApplicationGeneration ==
                    applicationAfterConnect.ApplicationGeneration
                    ? applicationAfterConnect.ApplicationGeneration
                    : null);
            return Current;
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
            if (_supervisor.Current.State == WorkerLifecycleState.Stopped)
            {
                throw SdkLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning,
                    "sdk-not-running",
                    Current,
                    global::Briosa.LifecycleRecoveryGuidance.None);
            }

            await _supervisor.StopAsync(cancellationToken).ConfigureAwait(false);
            _stateProjection.MarkDisconnected();
            var stopped = Current;
            if (IsTimeoutDiagnostic(stopped.DiagnosticCode))
            {
                throw SdkLifecycleException.DeadlineExceeded(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
                    stopped.DiagnosticCode,
                    stopped,
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            if (_supervisor.Current.LastTermination != WorkerTerminationKind.Graceful)
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

            if (_supervisor.Current.State != WorkerLifecycleState.Degraded)
            {
                throw SdkLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.RecoveryNotRequired,
                    "sdk-recovery-not-required",
                    Current,
                    global::Briosa.LifecycleRecoveryGuidance.None);
            }

            if (!await _supervisor.RecoverSdkAsync(
                    expectedGeneration,
                    cancellationToken).ConfigureAwait(false))
            {
                var failed = Current;
                if (IsTimeoutDiagnostic(failed.DiagnosticCode))
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

            _stateProjection.MarkDisconnected();
            return Current;
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

    private static bool IsTimeoutDiagnostic(string? diagnosticCode) =>
        diagnosticCode?.Contains("timeout", StringComparison.Ordinal) == true;
}

[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "This internal transport exception must always contain typed lifecycle detail.")]
internal sealed class SdkLifecycleException : Exception
{
    private SdkLifecycleException(
        Grpc.Core.StatusCode statusCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance)
        : base(diagnosticCode)
    {
        StatusCode = statusCode;
        Detail = new global::Briosa.SpatialAnalyzerSdkLifecycleError
        {
            Kind = kind,
            DiagnosticCode = diagnosticCode,
            State = state,
            RecoveryGuidance = recoveryGuidance
        };
    }

    public Grpc.Core.StatusCode StatusCode { get; }

    public global::Briosa.SpatialAnalyzerSdkLifecycleError Detail { get; }

    public static SdkLifecycleException InvalidArgument(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state) =>
        new(Grpc.Core.StatusCode.InvalidArgument, kind, diagnosticCode, state,
            global::Briosa.LifecycleRecoveryGuidance.RefreshState);

    public static SdkLifecycleException FailedPrecondition(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.FailedPrecondition, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SdkLifecycleException NotFound(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.NotFound, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SdkLifecycleException Aborted(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Aborted, kind, diagnosticCode, state, recoveryGuidance);

    public static SdkLifecycleException Unavailable(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Unavailable, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SdkLifecycleException DeadlineExceeded(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.DeadlineExceeded, kind, diagnosticCode, state,
            recoveryGuidance);
}
