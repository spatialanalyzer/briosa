using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Services;

internal interface ISpatialAnalyzerLifecycleStateProvider
{
    Task<global::Briosa.SpatialAnalyzerLifecycleState> GetCurrentAsync(
        CancellationToken cancellationToken);
}

[SuppressMessage(
    "Reliability",
    "CA2213:Disposable fields should be disposed",
    Justification = "The SDK coordinator and process platform are separately owned singletons.")]
internal sealed class SpatialAnalyzerLifecycleCoordinator(
    SpatialAnalyzerApplicationOptions options,
    ISpatialAnalyzerProcessPlatform processPlatform,
    ISpatialAnalyzerSdkLifecycleStateProvider sdkStateProvider,
    TimeProvider timeProvider) : ISpatialAnalyzerLifecycleStateProvider, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SpatialAnalyzerApplicationOptions _options = options;
    private readonly ISpatialAnalyzerProcessPlatform _processPlatform = processPlatform;
    private readonly ISpatialAnalyzerSdkLifecycleStateProvider _sdkStateProvider = sdkStateProvider;
    private readonly TimeProvider _timeProvider = timeProvider;
    private ISpatialAnalyzerOwnedProcess? _ownedProcess;
    private SpatialAnalyzerProcessIdentity? _selectedIdentity;
    private global::Briosa.SpatialAnalyzerLifecycleState _current = new()
    {
        StateRevision = 1,
        ApplicationState = global::Briosa.SpatialAnalyzerApplicationState.NotRunning,
        Ownership = global::Briosa.SpatialAnalyzerOwnership.None,
        DiagnosticCode = "spatial-analyzer-not-running"
    };
    private int _applicationGeneration;
    private int _disposeState;

    public async Task<global::Briosa.SpatialAnalyzerLifecycleState> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RefreshObservedState();
            return _current.Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<global::Briosa.SpatialAnalyzerLifecycleState> LaunchAsync(
        global::Briosa.LaunchSpatialAnalyzerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnterMutation(cancellationToken);
        try
        {
            RefreshObservedState();
            EnsureSdkAllowsLaunch();
            if (_current.ApplicationState is not
                (global::Briosa.SpatialAnalyzerApplicationState.NotRunning or
                    global::Briosa.SpatialAnalyzerApplicationState.Exited or
                    global::Briosa.SpatialAnalyzerApplicationState.Faulted))
            {
                var kind = _current.ApplicationState ==
                    global::Briosa.SpatialAnalyzerApplicationState.Ambiguous
                        ? global::Briosa.SpatialAnalyzerLifecycleFailureKind.ApplicationAmbiguous
                        : global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict;
                throw SpatialAnalyzerLifecycleException.FailedPrecondition(
                    kind,
                    "spatial-analyzer-already-running",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            var startInfo = CreateStartInfo(request);
            if (!File.Exists(_options.ExecutablePath))
            {
                throw SpatialAnalyzerLifecycleException.NotFound(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
                    "spatial-analyzer-installation-not-found",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
            }

            _applicationGeneration++;
            Transition(
                global::Briosa.SpatialAnalyzerApplicationState.Starting,
                global::Briosa.SpatialAnalyzerOwnership.None,
                _applicationGeneration,
                "spatial-analyzer-starting");
            try
            {
                _ownedProcess = _processPlatform.Start(startInfo);
                _selectedIdentity = _ownedProcess.Identity;
                Transition(
                    global::Briosa.SpatialAnalyzerApplicationState.Starting,
                    global::Briosa.SpatialAnalyzerOwnership.ServerLaunched,
                    _applicationGeneration,
                    "spatial-analyzer-starting");
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                _selectedIdentity = null;
                _ownedProcess?.Dispose();
                _ownedProcess = null;
                Transition(
                    global::Briosa.SpatialAnalyzerApplicationState.Faulted,
                    global::Briosa.SpatialAnalyzerOwnership.None,
                    applicationGeneration: null,
                    "spatial-analyzer-launch-failed");
                throw SpatialAnalyzerLifecycleException.Unavailable(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
                    "spatial-analyzer-launch-failed",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeout.CancelAfter(_options.StartupTimeout);
            try
            {
                while (!_ownedProcess.IsApplicationWindowReady)
                {
                    if (_ownedProcess.HasExited)
                    {
                        Transition(
                            global::Briosa.SpatialAnalyzerApplicationState.Exited,
                            global::Briosa.SpatialAnalyzerOwnership.None,
                            _applicationGeneration,
                            "spatial-analyzer-exited-during-startup");
                        throw SpatialAnalyzerLifecycleException.Unavailable(
                            global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
                            "spatial-analyzer-exited-during-startup",
                            _current.Clone(),
                            global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
                    }

                    await Task.Delay(
                        TimeSpan.FromMilliseconds(100),
                        _timeProvider,
                        timeout.Token).ConfigureAwait(false);
                    _ownedProcess.Refresh();
                }
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested)
            {
                throw SpatialAnalyzerLifecycleException.DeadlineExceeded(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.Timeout,
                    "spatial-analyzer-startup-timeout",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            Transition(
                global::Briosa.SpatialAnalyzerApplicationState.Running,
                global::Briosa.SpatialAnalyzerOwnership.ServerLaunched,
                _applicationGeneration,
                "spatial-analyzer-running");
            return _current.Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<global::Briosa.SpatialAnalyzerLifecycleState> CloseOwnedAsync(
        int expectedApplicationGeneration,
        CancellationToken cancellationToken)
    {
        EnterMutation(cancellationToken);
        try
        {
            RefreshObservedState();
            if (expectedApplicationGeneration <= 0)
            {
                throw SpatialAnalyzerLifecycleException.InvalidArgument(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.Validation,
                    "application-generation-required",
                    _current.Clone());
            }

            if (!_current.HasApplicationGeneration ||
                _current.ApplicationGeneration != expectedApplicationGeneration)
            {
                throw SpatialAnalyzerLifecycleException.Aborted(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
                    "application-generation-stale",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            if (_ownedProcess is null ||
                _current.Ownership != global::Briosa.SpatialAnalyzerOwnership.ServerLaunched ||
                _selectedIdentity != _ownedProcess.Identity)
            {
                throw SpatialAnalyzerLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.NotOwned,
                    "spatial-analyzer-not-owned",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.None);
            }

            if (_sdkStateProvider.Current.SdkState !=
                global::Briosa.SpatialAnalyzerSdkState.Stopped)
            {
                throw SpatialAnalyzerLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.SdkNotStopped,
                    "sdk-must-be-stopped-first",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.StopSdkFirst);
            }

            Transition(
                global::Briosa.SpatialAnalyzerApplicationState.Closing,
                global::Briosa.SpatialAnalyzerOwnership.ServerLaunched,
                _applicationGeneration,
                "spatial-analyzer-closing");
            if (!_ownedProcess.RequestClose())
            {
                Transition(
                    global::Briosa.SpatialAnalyzerApplicationState.Running,
                    global::Briosa.SpatialAnalyzerOwnership.ServerLaunched,
                    _applicationGeneration,
                    "spatial-analyzer-close-not-accepted");
                throw SpatialAnalyzerLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
                    "spatial-analyzer-close-not-accepted",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeout.CancelAfter(_options.ShutdownTimeout);
            try
            {
                await _ownedProcess.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested)
            {
                RefreshObservedState();
                throw SpatialAnalyzerLifecycleException.DeadlineExceeded(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.Timeout,
                    "spatial-analyzer-shutdown-timeout",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.RefreshState);
            }

            _ownedProcess.Dispose();
            _ownedProcess = null;
            _selectedIdentity = null;
            Transition(
                global::Briosa.SpatialAnalyzerApplicationState.NotRunning,
                global::Briosa.SpatialAnalyzerOwnership.None,
                applicationGeneration: null,
                "spatial-analyzer-not-running");
            return _current.Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        _ownedProcess?.Dispose();
        _gate.Dispose();
    }

    private ProcessStartInfo CreateStartInfo(
        global::Briosa.LaunchSpatialAnalyzerRequest request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(_options.ExecutablePath)!,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        switch (request.InitialContentCase)
        {
            case global::Briosa.LaunchSpatialAnalyzerRequest.InitialContentOneofCase.JobFilePath:
                if (string.IsNullOrWhiteSpace(request.JobFilePath) ||
                    !Path.IsPathFullyQualified(request.JobFilePath))
                {
                    throw SpatialAnalyzerLifecycleException.InvalidArgument(
                        global::Briosa.SpatialAnalyzerLifecycleFailureKind.Validation,
                        "spatial-analyzer-job-file-invalid",
                        _current.Clone());
                }

                if (!File.Exists(request.JobFilePath))
                {
                    throw SpatialAnalyzerLifecycleException.NotFound(
                        global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
                        "spatial-analyzer-job-file-not-found",
                        _current.Clone(),
                        global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
                }

                startInfo.ArgumentList.Add(Path.GetFullPath(request.JobFilePath));
                break;
            case global::Briosa.LaunchSpatialAnalyzerRequest.InitialContentOneofCase.QuickStartInstrumentName:
                if (string.IsNullOrWhiteSpace(request.QuickStartInstrumentName) ||
                    request.QuickStartInstrumentName.Length > 256 ||
                    request.QuickStartInstrumentName.Any(char.IsControl))
                {
                    throw SpatialAnalyzerLifecycleException.InvalidArgument(
                        global::Briosa.SpatialAnalyzerLifecycleFailureKind.Validation,
                        "spatial-analyzer-instrument-name-invalid",
                        _current.Clone());
                }

                startInfo.ArgumentList.Add("/QUICK");
                startInfo.ArgumentList.Add(request.QuickStartInstrumentName);
                break;
        }

        if (request.StartMinimized)
        {
            startInfo.ArgumentList.Add("-MINIMIZE");
        }

        return startInfo;
    }

    private void EnterMutation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_gate.Wait(0, CancellationToken.None))
        {
            throw SpatialAnalyzerLifecycleException.Aborted(
                global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
                "application-lifecycle-transition-in-progress",
                _current.Clone(),
                global::Briosa.LifecycleRecoveryGuidance.RefreshState);
        }
    }

    private void EnsureSdkAllowsLaunch()
    {
        var sdk = _sdkStateProvider.Current;
        if (sdk.ConnectionState is
                global::Briosa.SpatialAnalyzerConnectionState.Connected or
                global::Briosa.SpatialAnalyzerConnectionState.Connecting or
                global::Briosa.SpatialAnalyzerConnectionState.Stopping ||
            sdk.SdkState is
                global::Briosa.SpatialAnalyzerSdkState.Verifying or
                global::Briosa.SpatialAnalyzerSdkState.Recovering)
        {
            throw SpatialAnalyzerLifecycleException.FailedPrecondition(
                global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
                "sdk-state-prevents-spatial-analyzer-launch",
                _current.Clone(),
                global::Briosa.LifecycleRecoveryGuidance.StopSdkFirst);
        }
    }

    private void RefreshObservedState()
    {
        if (_ownedProcess is not null)
        {
            _ownedProcess.Refresh();
            if (_ownedProcess.HasExited)
            {
                TransitionIfChanged(
                    global::Briosa.SpatialAnalyzerApplicationState.Exited,
                    global::Briosa.SpatialAnalyzerOwnership.None,
                    _applicationGeneration,
                    "owned-spatial-analyzer-exited");
                _ownedProcess.Dispose();
                _ownedProcess = null;
                _selectedIdentity = null;
            }
            else if (_ownedProcess.IsApplicationWindowReady)
            {
                TransitionIfChanged(
                    global::Briosa.SpatialAnalyzerApplicationState.Running,
                    global::Briosa.SpatialAnalyzerOwnership.ServerLaunched,
                    _applicationGeneration,
                    "spatial-analyzer-running");
            }

            return;
        }

        var observations = _processPlatform.ObserveEligibleProcesses(
            _options.ExecutablePath);
        if (observations.Count == 0)
        {
            if (_current.ApplicationState ==
                global::Briosa.SpatialAnalyzerApplicationState.Running)
            {
                Transition(
                    global::Briosa.SpatialAnalyzerApplicationState.Exited,
                    global::Briosa.SpatialAnalyzerOwnership.None,
                    _current.HasApplicationGeneration
                        ? _current.ApplicationGeneration
                        : null,
                    "external-spatial-analyzer-exited");
            }
            else if (_current.ApplicationState is not
                (global::Briosa.SpatialAnalyzerApplicationState.Exited or
                    global::Briosa.SpatialAnalyzerApplicationState.Faulted))
            {
                TransitionIfChanged(
                    global::Briosa.SpatialAnalyzerApplicationState.NotRunning,
                    global::Briosa.SpatialAnalyzerOwnership.None,
                    applicationGeneration: null,
                    "spatial-analyzer-not-running");
            }

            _selectedIdentity = null;
            return;
        }

        if (observations.Count > 1)
        {
            _selectedIdentity = null;
            TransitionIfChanged(
                global::Briosa.SpatialAnalyzerApplicationState.Ambiguous,
                global::Briosa.SpatialAnalyzerOwnership.None,
                applicationGeneration: null,
                "spatial-analyzer-selection-ambiguous");
            return;
        }

        var observed = observations[0].Identity;
        if (_selectedIdentity != observed)
        {
            _selectedIdentity = observed;
            _applicationGeneration++;
            Transition(
                global::Briosa.SpatialAnalyzerApplicationState.Running,
                global::Briosa.SpatialAnalyzerOwnership.External,
                _applicationGeneration,
                "external-spatial-analyzer-running");
        }
    }

    private void TransitionIfChanged(
        global::Briosa.SpatialAnalyzerApplicationState applicationState,
        global::Briosa.SpatialAnalyzerOwnership ownership,
        int? applicationGeneration,
        string diagnosticCode)
    {
        if (_current.ApplicationState == applicationState &&
            _current.Ownership == ownership &&
            _current.HasApplicationGeneration == applicationGeneration.HasValue &&
            (!applicationGeneration.HasValue ||
                _current.ApplicationGeneration == applicationGeneration.Value) &&
            string.Equals(
                _current.DiagnosticCode,
                diagnosticCode,
                StringComparison.Ordinal))
        {
            return;
        }

        Transition(applicationState, ownership, applicationGeneration, diagnosticCode);
    }

    private void Transition(
        global::Briosa.SpatialAnalyzerApplicationState applicationState,
        global::Briosa.SpatialAnalyzerOwnership ownership,
        int? applicationGeneration,
        string diagnosticCode)
    {
        var state = new global::Briosa.SpatialAnalyzerLifecycleState
        {
            StateRevision = checked(_current.StateRevision + 1),
            ApplicationState = applicationState,
            Ownership = ownership,
            DiagnosticCode = diagnosticCode
        };
        if (applicationGeneration.HasValue)
        {
            state.ApplicationGeneration = applicationGeneration.Value;
        }

        _current = state;
    }
}

[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "This internal transport exception must always contain typed lifecycle detail.")]
internal sealed class SpatialAnalyzerLifecycleException : Exception
{
    private SpatialAnalyzerLifecycleException(
        Grpc.Core.StatusCode statusCode,
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance)
        : base(diagnosticCode)
    {
        StatusCode = statusCode;
        Detail = new global::Briosa.SpatialAnalyzerLifecycleError
        {
            Kind = kind,
            DiagnosticCode = diagnosticCode,
            State = state,
            RecoveryGuidance = recoveryGuidance
        };
    }

    public Grpc.Core.StatusCode StatusCode { get; }

    public global::Briosa.SpatialAnalyzerLifecycleError Detail { get; }

    public static SpatialAnalyzerLifecycleException InvalidArgument(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state) =>
        new(Grpc.Core.StatusCode.InvalidArgument, kind, diagnosticCode, state,
            global::Briosa.LifecycleRecoveryGuidance.RefreshState);

    public static SpatialAnalyzerLifecycleException NotFound(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.NotFound, kind, diagnosticCode, state, recoveryGuidance);

    public static SpatialAnalyzerLifecycleException FailedPrecondition(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.FailedPrecondition, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SpatialAnalyzerLifecycleException Aborted(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Aborted, kind, diagnosticCode, state, recoveryGuidance);

    public static SpatialAnalyzerLifecycleException Unavailable(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Unavailable, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SpatialAnalyzerLifecycleException DeadlineExceeded(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.DeadlineExceeded, kind, diagnosticCode, state,
            recoveryGuidance);
}
