using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Briosa.Server.Workers;

namespace Briosa.Server.Services;

[SuppressMessage(
    "Reliability",
    "CA2213:Disposable fields should be disposed",
    Justification = "The SDK coordinator and process platform are separately owned singletons.")]
internal sealed partial class SpatialAnalyzerLifecycleCoordinator(
    SpatialAnalyzerApplicationOptions options,
    ISpatialAnalyzerProcessPlatform processPlatform,
    ISpatialAnalyzerSdkLifecycleStateProvider sdkStateProvider,
    TimeProvider timeProvider,
    ILogger<SpatialAnalyzerLifecycleCoordinator>? logger = null)
    : ISpatialAnalyzerLifecycleStateProvider, IDisposable, IAsyncDisposable
{
    // An external application stays selected until a direct check confirms that it
    // exited, or until this many consecutive observations could neither see nor check
    // it (#317). Until then it keeps its generation, reported with this diagnostic.
    internal const int UnconfirmedObservationLimit = 3;
    internal const string UnconfirmedDiagnosticCode = "external-spatial-analyzer-unconfirmed";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SpatialAnalyzerApplicationOptions _options = options;
    private readonly ISpatialAnalyzerProcessPlatform _processPlatform = processPlatform;
    private readonly ISpatialAnalyzerSdkLifecycleStateProvider _sdkStateProvider = sdkStateProvider;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<SpatialAnalyzerLifecycleCoordinator> _logger =
        logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SpatialAnalyzerLifecycleCoordinator>.Instance;
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
    private int _unconfirmedObservations;
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

    // An accepted launch detaches from its caller (#322), as Start, Connect, and Recover
    // do (#315). It keeps the gate while it waits for the main window under its own
    // bound, so a caller that stopped waiting leaves no partial transition behind.
    public async Task<global::Briosa.SpatialAnalyzerLifecycleState> LaunchAsync(
        global::Briosa.LaunchSpatialAnalyzerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested) throw RequestWithdrawn();
        EnterMutation(cancellationToken);
        var acceptance = new LifecycleAcceptance(cancellationToken);
        try
        {
            return await acceptance.WaitAsync(
                LaunchTransitionAsync(request, acceptance),
                () => throw CallerStoppedWaiting()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw RequestWithdrawn();
        }
    }

    private async Task<global::Briosa.SpatialAnalyzerLifecycleState> LaunchTransitionAsync(
        global::Briosa.LaunchSpatialAnalyzerRequest request,
        LifecycleAcceptance acceptance)
    {
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
            var installationDiagnostic = _options.InstallationDiagnostic ??
                (_options.RequireInstalledIdentity
                    ? SpatialAnalyzerInstallationDiscovery.Check(_options.ExecutablePath).DiagnosticCode
                    : null);
            if (installationDiagnostic is not null)
            {
                throw SpatialAnalyzerLifecycleException.FailedPrecondition(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
                    installationDiagnostic,
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
            }

            if (!File.Exists(_options.ExecutablePath))
            {
                throw SpatialAnalyzerLifecycleException.NotFound(
                    global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
                    "spatial-analyzer-installation-not-found",
                    _current.Clone(),
                    global::Briosa.LifecycleRecoveryGuidance.CorrectEnvironment);
            }

            // Every precondition held; from here only the launch's own outcome or its
            // startup bound ends it.
            acceptance.Accept();
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

            using var timeout = new CancellationTokenSource(_options.StartupTimeout, _timeProvider);
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
            catch (OperationCanceledException)
            {
                // Only the startup bound can cancel the wait. The process keeps running
                // and stays STARTING until an observation sees its window or its exit.
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

    // A detached launch owns the gate until it finishes under its own bound (#322).
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        _gate.Wait(CancellationToken.None);
        ReleaseResources();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        ReleaseResources();
    }

    private void ReleaseResources()
    {
        _ownedProcess?.Dispose();
        _gate.Dispose();
    }

    // Before acceptance nothing changed, so the caller has nothing to reconcile.
    private SpatialAnalyzerLifecycleException RequestWithdrawn() =>
        SpatialAnalyzerLifecycleException.Cancelled(
            "application-lifecycle-request-withdrawn",
            _current.Clone(),
            global::Briosa.LifecycleRecoveryGuidance.None);

    // The accepted launch continues and records its own outcome. The caller refreshes
    // state through GetSpatialAnalyzerState.
    private SpatialAnalyzerLifecycleException CallerStoppedWaiting() =>
        SpatialAnalyzerLifecycleException.Cancelled(
            "application-lifecycle-caller-stopped-waiting",
            _current.Clone(),
            global::Briosa.LifecycleRecoveryGuidance.RefreshState);

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

        var observed = _processPlatform.ObserveEligibleProcesses(_options.ExecutablePath)
            .Select(static observation => observation.Identity)
            .ToList();
        var unconfirmed = false;
        if (_selectedIdentity is { } selected && !observed.Contains(selected))
        {
            // A scan skips a process it cannot read, so one missed scan does not show
            // that the selected application exited (#317). Check it directly first.
            switch (_processPlatform.ObserveProcess(_options.ExecutablePath, selected))
            {
                case SpatialAnalyzerProcessPresence.Running:
                    observed.Add(selected);
                    break;
                case SpatialAnalyzerProcessPresence.Unknown when
                    _unconfirmedObservations + 1 < UnconfirmedObservationLimit:
                    observed.Add(selected);
                    unconfirmed = true;
                    break;
                default:
                    // Exited, or unconfirmed too many times in a row: gone.
                    break;
            }
        }

        _unconfirmedObservations = unconfirmed ? _unconfirmedObservations + 1 : 0;
        if (observed.Count == 0)
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

        if (observed.Count > 1)
        {
            // An unconfirmed selection beside another eligible application could be
            // either a second instance or a restart, so it is ambiguous too.
            _selectedIdentity = null;
            _unconfirmedObservations = 0;
            TransitionIfChanged(
                global::Briosa.SpatialAnalyzerApplicationState.Ambiguous,
                global::Briosa.SpatialAnalyzerOwnership.None,
                applicationGeneration: null,
                "spatial-analyzer-selection-ambiguous");
            return;
        }

        var identity = observed[0];
        if (_selectedIdentity != identity)
        {
            _selectedIdentity = identity;
            _applicationGeneration++;
            Transition(
                global::Briosa.SpatialAnalyzerApplicationState.Running,
                global::Briosa.SpatialAnalyzerOwnership.External,
                _applicationGeneration,
                "external-spatial-analyzer-running");
            return;
        }

        // The same application keeps its generation. An unconfirmed observation is
        // reported as such, so it cannot confirm a Connect's association.
        TransitionIfChanged(
            global::Briosa.SpatialAnalyzerApplicationState.Running,
            global::Briosa.SpatialAnalyzerOwnership.External,
            _applicationGeneration,
            unconfirmed ? UnconfirmedDiagnosticCode : "external-spatial-analyzer-running");
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
        LogApplicationTransition(
            applicationState == global::Briosa.SpatialAnalyzerApplicationState.Faulted ? LogLevel.Warning : LogLevel.Information,
            applicationState, ownership, applicationGeneration, diagnosticCode);
    }

    [LoggerMessage(EventId = 1400,
        Message = "Application transitioned to {ApplicationState}, ownership {Ownership}, generation {Generation}, diagnostic {DiagnosticCode}.")]
    private partial void LogApplicationTransition(LogLevel level,
        global::Briosa.SpatialAnalyzerApplicationState applicationState,
        global::Briosa.SpatialAnalyzerOwnership ownership, int? generation, string diagnosticCode);
}
