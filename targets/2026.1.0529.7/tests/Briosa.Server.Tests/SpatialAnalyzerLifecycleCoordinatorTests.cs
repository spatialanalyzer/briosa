using System.Collections.Concurrent;
using System.Diagnostics;
using Briosa.Server.Services;
using Microsoft.Extensions.Logging;

namespace Briosa.Server.Tests;

public sealed class SpatialAnalyzerLifecycleCoordinatorTests
{
    // A distinct bound, so the virtual clock can tell it from the window poll.
    private static readonly TimeSpan ApplicationStartupTimeout = TimeSpan.FromSeconds(7);

    // Only a hang guard: the virtual clock orders every launch step.
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task StateDiscoveryReportsOneExternalExactTargetWithoutOwnership()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200)));
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var state = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.External, state.Ownership);
        Assert.True(state.ApplicationGeneration > 0);
    }

    [Fact]
    public async Task StateDiscoveryFailsHonestWhenSeveralEligibleApplicationsExist()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.AddRange(
        [
            new SpatialAnalyzerProcessObservation(
                new SpatialAnalyzerProcessIdentity(41, 4200)),
            new SpatialAnalyzerProcessObservation(
                new SpatialAnalyzerProcessIdentity(42, 4300))
        ]);
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var state = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Ambiguous, state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.None, state.Ownership);
        Assert.False(state.HasApplicationGeneration);
    }

    [Fact]
    public async Task LaunchUsesOnlyDocumentedQuickStartAndMinimizedArguments()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var state = await coordinator.LaunchAsync(
            new global::Briosa.LaunchSpatialAnalyzerRequest
            {
                QuickStartInstrumentName = "Leica AT960",
                StartMinimized = true
            },
            CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.ServerLaunched, state.Ownership);
        Assert.Equal(["/QUICK", "Leica AT960", "-MINIMIZE"], platform.LastArguments);
        Assert.False(platform.LastUseShellExecute);
    }

    [Fact]
    public async Task RelativeJobPathIsRejectedAsInvalidArgument()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var exception = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest
                {
                    JobFilePath = "relative-job.xit"
                },
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.Validation,
            exception.Detail.Kind);
        Assert.Null(platform.StartedProcess);
    }

    [Fact]
    public async Task MissingAbsoluteJobPathIsReportedAsNotFound()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        using var coordinator = CreateCoordinator(executable.Path, platform);
        var missingJobPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"briosa-missing-{Guid.NewGuid():N}",
            "missing-job.xit");

        var exception = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest
                {
                    JobFilePath = missingJobPath
                },
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.NotFound, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.LaunchFailed,
            exception.Detail.Kind);
        Assert.Null(platform.StartedProcess);
    }

    [Fact]
    public async Task OwnedCloseRequiresStoppedSdkAndExactGeneration()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        var sdk = new FakeSdkStateProvider(global::Briosa.SpatialAnalyzerSdkState.Running);
        using var coordinator = CreateCoordinator(executable.Path, platform, sdk);
        var launched = await coordinator.LaunchAsync(
            new global::Briosa.LaunchSpatialAnalyzerRequest(),
            CancellationToken.None);

        var activeSdk = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.CloseOwnedAsync(
                launched.ApplicationGeneration,
                CancellationToken.None));
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.SdkNotStopped,
            activeSdk.Detail.Kind);

        sdk.Set(global::Briosa.SpatialAnalyzerSdkState.Stopped);
        var stale = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.CloseOwnedAsync(
                launched.ApplicationGeneration + 1,
                CancellationToken.None));
        Assert.Equal(Grpc.Core.StatusCode.Aborted, stale.StatusCode);

        var closed = await coordinator.CloseOwnedAsync(
            launched.ApplicationGeneration,
            CancellationToken.None);
        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.NotRunning, closed.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.None, closed.Ownership);
        Assert.False(closed.HasApplicationGeneration);
        Assert.True(platform.StartedProcess!.CloseRequested);
    }

    [Fact]
    public async Task LaunchNeverClosesOrReplacesAnExternalApplication()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200)));
        using var coordinator = CreateCoordinator(executable.Path, platform);

        var exception = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest(),
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.FailedPrecondition, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
            exception.Detail.Kind);
        Assert.Null(platform.StartedProcess);
    }

    // #317: a scan can miss a running SpatialAnalyzer whose details it could not read.
    // The direct check then finds the same process ID and start time, so the state and
    // generation stay as they were instead of reporting an exit and then a restart.
    [Fact]
    public async Task MissedScanOfTheSameRunningProcessKeepsStateAndGeneration()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        var running = new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200));
        platform.Observations.Add(running);
        using var coordinator = CreateCoordinator(executable.Path, platform);
        var first = await coordinator.GetCurrentAsync(CancellationToken.None);

        platform.Observations.Clear();
        platform.Presence = _ => SpatialAnalyzerProcessPresence.Running;
        var missed = await coordinator.GetCurrentAsync(CancellationToken.None);
        platform.Observations.Add(running);
        var seenAgain = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, first.ApplicationState);
        Assert.Equal(first, missed);
        Assert.Equal(first, seenAgain);
        // Only the missed scan checks the selected process directly.
        Assert.Equal(1, platform.PresenceChecks);
    }

    // When the direct check cannot read the process either, the selection is held and
    // reported as unconfirmed with the same generation. Seeing the process again
    // restores the confirmed state and restarts the count; the third consecutive
    // unconfirmed observation treats the application as gone.
    [Fact]
    public async Task UnconfirmableObservationsKeepTheGenerationUntilTheyRepeat()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        var running = new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200));
        platform.Observations.Add(running);
        platform.Presence = _ => SpatialAnalyzerProcessPresence.Unknown;
        using var coordinator = CreateCoordinator(executable.Path, platform);
        var first = await coordinator.GetCurrentAsync(CancellationToken.None);

        platform.Observations.Clear();
        var unconfirmed = await coordinator.GetCurrentAsync(CancellationToken.None);
        platform.Observations.Add(running);
        var confirmed = await coordinator.GetCurrentAsync(CancellationToken.None);
        platform.Observations.Clear();
        var held = new[]
        {
            await coordinator.GetCurrentAsync(CancellationToken.None),
            await coordinator.GetCurrentAsync(CancellationToken.None)
        };
        var gone = await coordinator.GetCurrentAsync(CancellationToken.None);

        foreach (var state in held.Prepend(unconfirmed))
        {
            Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, state.ApplicationState);
            Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.External, state.Ownership);
            Assert.Equal(first.ApplicationGeneration, state.ApplicationGeneration);
            Assert.Equal("external-spatial-analyzer-unconfirmed", state.DiagnosticCode);
        }

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, confirmed.ApplicationState);
        Assert.Equal(first.ApplicationGeneration, confirmed.ApplicationGeneration);
        Assert.Equal("external-spatial-analyzer-running", confirmed.DiagnosticCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Exited, gone.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.None, gone.Ownership);
        Assert.Equal(first.ApplicationGeneration, gone.ApplicationGeneration);
        Assert.Equal("external-spatial-analyzer-exited", gone.DiagnosticCode);
    }

    // A confirmed exit is reported on the first missed scan, and an application seen
    // after it is a new generation.
    [Fact]
    public async Task ConfirmedExitIsReportedOnTheFirstMissedScan()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200)));
        using var coordinator = CreateCoordinator(executable.Path, platform);
        var first = await coordinator.GetCurrentAsync(CancellationToken.None);

        platform.Observations.Clear();
        platform.Presence = _ => SpatialAnalyzerProcessPresence.Exited;
        var exited = await coordinator.GetCurrentAsync(CancellationToken.None);
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(51, 5200)));
        var next = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Exited, exited.ApplicationState);
        Assert.Equal(first.ApplicationGeneration, exited.ApplicationGeneration);
        Assert.Equal("external-spatial-analyzer-exited", exited.DiagnosticCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, next.ApplicationState);
        Assert.Equal(first.ApplicationGeneration + 1, next.ApplicationGeneration);
    }

    // A restart is a new process ID, or the same process ID with a new start time. The
    // first scan that sees it reports a new generation.
    [Theory]
    [InlineData(51, 5200)]
    [InlineData(41, 5200)]
    public async Task RestartIsANewGenerationOnTheFirstScan(int processId, long startTicks)
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200)));
        using var coordinator = CreateCoordinator(executable.Path, platform);
        var first = await coordinator.GetCurrentAsync(CancellationToken.None);

        platform.Observations.Clear();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(processId, startTicks)));
        var restarted = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, restarted.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.External, restarted.Ownership);
        Assert.Equal(first.ApplicationGeneration + 1, restarted.ApplicationGeneration);
        Assert.Equal("external-spatial-analyzer-running", restarted.DiagnosticCode);
    }

    // If the selected process cannot be checked while another eligible application is
    // running, a restart cannot be told apart from a second instance.
    [Fact]
    public async Task UnconfirmableSelectionBesideAnotherEligibleApplicationIsAmbiguous()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(41, 4200)));
        using var coordinator = CreateCoordinator(executable.Path, platform);
        _ = await coordinator.GetCurrentAsync(CancellationToken.None);

        platform.Observations.Clear();
        platform.Observations.Add(new SpatialAnalyzerProcessObservation(
            new SpatialAnalyzerProcessIdentity(51, 5200)));
        platform.Presence = _ => SpatialAnalyzerProcessPresence.Unknown;
        var state = await coordinator.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Ambiguous, state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.None, state.Ownership);
        Assert.False(state.HasApplicationGeneration);
    }

    // #322: like the SDK exchanges in #315, an accepted launch no longer observes its
    // caller's cancellation. The caller gets a typed detach at once; the launch keeps
    // the application lifecycle and waits for the window under its own bound. Either
    // the window appears, and the launch records the running application, or the
    // bound expires and the server-launched process stays STARTING.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationAfterLaunchAcceptanceLeavesTheWindowWaitToItsBound(bool exceedBound)
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform { WindowReadyAtStart = false };
        var clock = new HeartbeatTestClock();
        using var coordinator = CreateCoordinator(
            executable.Path, platform, timeProvider: clock, startupTimeout: ApplicationStartupTimeout);
        using var cancellation = new CancellationTokenSource();

        var launching = coordinator.LaunchAsync(
            new global::Briosa.LaunchSpatialAnalyzerRequest(), cancellation.Token);
        // Acceptance and the process start happen before the first wait.
        var process = Assert.IsType<FakeOwnedProcess>(platform.StartedProcess);
        await cancellation.CancelAsync();
        var stoppedWaiting = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => launching.WaitAsync(HangGuard));

        Assert.Equal(Grpc.Core.StatusCode.Cancelled, stoppedWaiting.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
            stoppedWaiting.Detail.Kind);
        Assert.Equal("application-lifecycle-caller-stopped-waiting", stoppedWaiting.Detail.DiagnosticCode);
        Assert.Equal(
            global::Briosa.LifecycleRecoveryGuidance.RefreshState,
            stoppedWaiting.Detail.RecoveryGuidance);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerApplicationState.Starting,
            stoppedWaiting.Detail.State.ApplicationState);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerOwnership.ServerLaunched,
            stoppedWaiting.Detail.State.Ownership);
        var generation = stoppedWaiting.Detail.State.ApplicationGeneration;

        // The detached launch still owns the application lifecycle, so nothing can
        // close or replace the application it started.
        var closing = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.CloseOwnedAsync(generation, CancellationToken.None));
        Assert.Equal("application-lifecycle-transition-in-progress", closing.Detail.DiagnosticCode);
        var relaunching = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest(), CancellationToken.None));
        Assert.Equal("application-lifecycle-transition-in-progress", relaunching.Detail.DiagnosticCode);
        Assert.False(process.CloseRequested);
        Assert.Equal(1, platform.Starts);

        if (exceedBound)
        {
            await clock.FireNextAsync(ApplicationStartupTimeout);
        }
        else
        {
            process.WindowReady = true;
            await clock.FireNextAsync();
        }

        var state = await coordinator.GetCurrentAsync(CancellationToken.None).WaitAsync(HangGuard);
        Assert.Equal(
            exceedBound
                ? global::Briosa.SpatialAnalyzerApplicationState.Starting
                : global::Briosa.SpatialAnalyzerApplicationState.Running,
            state.ApplicationState);
        Assert.Equal(global::Briosa.SpatialAnalyzerOwnership.ServerLaunched, state.Ownership);
        Assert.Equal(generation, state.ApplicationGeneration);
        Assert.Equal(
            exceedBound ? "spatial-analyzer-starting" : "spatial-analyzer-running",
            state.DiagnosticCode);
        Assert.False(process.CloseRequested);
    }

    // #322: a launch cancelled before acceptance is withdrawn with a typed detail and
    // starts nothing, whether the caller cancelled before calling or while the server
    // was still checking the launch preconditions.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaunchCancelledBeforeAcceptanceIsWithdrawnWithoutStartingAnything(bool duringChecks)
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform();
        using var coordinator = CreateCoordinator(executable.Path, platform);
        var before = await coordinator.GetCurrentAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        if (duringChecks)
        {
            platform.Observing = cancellation.Cancel;
        }
        else
        {
            await cancellation.CancelAsync();
        }

        var withdrawn = await Assert.ThrowsAsync<SpatialAnalyzerLifecycleException>(
            () => coordinator.LaunchAsync(
                new global::Briosa.LaunchSpatialAnalyzerRequest(), cancellation.Token));
        platform.Observing = null;

        Assert.Equal(Grpc.Core.StatusCode.Cancelled, withdrawn.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerLifecycleFailureKind.StateConflict,
            withdrawn.Detail.Kind);
        Assert.Equal("application-lifecycle-request-withdrawn", withdrawn.Detail.DiagnosticCode);
        Assert.Equal(global::Briosa.LifecycleRecoveryGuidance.None, withdrawn.Detail.RecoveryGuidance);
        Assert.Null(platform.StartedProcess);
        Assert.Equal(before, await coordinator.GetCurrentAsync(CancellationToken.None));
    }

    // #322 through the gRPC service: the typed detach of a caller that stopped waiting
    // is audited, although gRPC does not deliver a response to that caller.
    [Fact]
    public async Task LaunchServiceAuditsACallerThatStoppedWaiting()
    {
        using var executable = TemporaryFile.Create();
        var platform = new FakeProcessPlatform { WindowReadyAtStart = false };
        var clock = new HeartbeatTestClock();
        using var coordinator = CreateCoordinator(
            executable.Path, platform, timeProvider: clock, startupTimeout: ApplicationStartupTimeout);
        var audit = new CapturingLogger<LifecycleAuditLogger>();
        var service = new SpatialAnalyzerLifecycleService(coordinator, new LifecycleAuditLogger(audit));
        using var cancellation = new CancellationTokenSource();

        var call = service.LaunchSpatialAnalyzer(
            new global::Briosa.LaunchSpatialAnalyzerRequest(),
            new InMemoryServerCallContext(
                "/briosa.SpatialAnalyzerLifecycle/LaunchSpatialAnalyzer", null, cancellation.Token));
        var process = Assert.IsType<FakeOwnedProcess>(platform.StartedProcess);
        await cancellation.CancelAsync();
        var error = await Assert.ThrowsAsync<Grpc.Core.RpcException>(() => call.WaitAsync(HangGuard));
        process.WindowReady = true;
        await clock.FireNextAsync();

        Assert.Equal(Grpc.Core.StatusCode.Cancelled, error.StatusCode);
        var detail = global::Briosa.SpatialAnalyzerLifecycleError.Parser.ParseFrom(
            Assert.Single(error.Trailers).ValueBytes);
        Assert.Equal("LaunchSpatialAnalyzer", detail.Rpc);
        Assert.Equal("application-lifecycle-caller-stopped-waiting", detail.DiagnosticCode);
        var rejected = Assert.Single(audit.Entries, entry => entry.EventId.Id == 1302);
        Assert.Equal("LaunchSpatialAnalyzer", rejected.Value("Rpc"));
        Assert.Equal(Grpc.Core.StatusCode.Cancelled, rejected.Value("GrpcStatus"));
        Assert.Equal("application-lifecycle-caller-stopped-waiting", rejected.Value("DiagnosticCode"));
        var running = await coordinator.GetCurrentAsync(CancellationToken.None).WaitAsync(HangGuard);
        Assert.Equal(global::Briosa.SpatialAnalyzerApplicationState.Running, running.ApplicationState);
    }

    private static SpatialAnalyzerLifecycleCoordinator CreateCoordinator(
        string executablePath,
        FakeProcessPlatform platform,
        FakeSdkStateProvider? sdk = null,
        TimeProvider? timeProvider = null,
        TimeSpan? startupTimeout = null) =>
        new(
            new SpatialAnalyzerApplicationOptions(
                executablePath,
                startupTimeout ?? TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)),
            platform,
            sdk ?? new FakeSdkStateProvider(global::Briosa.SpatialAnalyzerSdkState.Stopped),
            timeProvider ?? TimeProvider.System);

    private sealed class FakeSdkStateProvider(
        global::Briosa.SpatialAnalyzerSdkState sdkState)
        : ISpatialAnalyzerSdkLifecycleStateProvider
    {
        public global::Briosa.SpatialAnalyzerSdkLifecycleState Current { get; private set; } =
            new()
            {
                SdkState = sdkState,
                ConnectionState = global::Briosa.SpatialAnalyzerConnectionState.Disconnected,
                ExecutionReadinessState =
                    global::Briosa.SpatialAnalyzerExecutionReadinessState.Unverified
            };

        public void Set(global::Briosa.SpatialAnalyzerSdkState sdkState)
        {
            var state = Current.Clone();
            state.SdkState = sdkState;
            Current = state;
        }
    }

    private sealed class FakeProcessPlatform : ISpatialAnalyzerProcessPlatform
    {
        public List<SpatialAnalyzerProcessObservation> Observations { get; } = [];

        public FakeOwnedProcess? StartedProcess { get; private set; }

        public IReadOnlyList<string> LastArguments { get; private set; } = [];

        public bool LastUseShellExecute { get; private set; }

        // The direct check. By default it agrees with the scan, like a scan that read
        // every candidate.
        public Func<SpatialAnalyzerProcessIdentity, SpatialAnalyzerProcessPresence>? Presence { get; set; }

        public int PresenceChecks { get; private set; }

        public bool WindowReadyAtStart { get; init; } = true;

        public int Starts { get; private set; }

        // Runs during each scan, which the coordinator performs before accepting a launch.
        public Action? Observing { get; set; }

        public IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(
            string executablePath)
        {
            Observing?.Invoke();
            return [.. Observations];
        }

        public SpatialAnalyzerProcessPresence ObserveProcess(
            string executablePath,
            SpatialAnalyzerProcessIdentity identity)
        {
            PresenceChecks++;
            return Presence?.Invoke(identity) ??
                (Observations.Any(observation => observation.Identity == identity)
                    ? SpatialAnalyzerProcessPresence.Running
                    : SpatialAnalyzerProcessPresence.Exited);
        }

        public ISpatialAnalyzerOwnedProcess Start(ProcessStartInfo startInfo)
        {
            LastArguments = [.. startInfo.ArgumentList];
            LastUseShellExecute = startInfo.UseShellExecute;
            Starts++;
            StartedProcess = new FakeOwnedProcess { WindowReady = WindowReadyAtStart };
            return StartedProcess;
        }
    }

    private sealed class FakeOwnedProcess : ISpatialAnalyzerOwnedProcess
    {
        private volatile bool _windowReady;

        public SpatialAnalyzerProcessIdentity Identity { get; } = new(51, 5200);

        public bool HasExited { get; private set; }

        // The test shows the main window while a launch waits for it.
        public bool WindowReady
        {
            get => _windowReady;
            set => _windowReady = value;
        }

        public bool IsApplicationWindowReady => !HasExited && WindowReady;

        public bool CloseRequested { get; private set; }

        public bool RequestClose()
        {
            CloseRequested = true;
            HasExited = true;
            return true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public void Refresh()
        {
        }

        public void Dispose()
        {
        }
    }

    // Captures structured log state so tests can assert value-free audit evidence.
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue(new LogEntry(eventId,
                state as IReadOnlyList<KeyValuePair<string, object?>> ?? []));
    }

    private sealed record LogEntry(EventId EventId, IReadOnlyList<KeyValuePair<string, object?>> State)
    {
        public object? Value(string name) => State.FirstOrDefault(pair => pair.Key == name).Value;
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path) => Path = path;

        public string Path { get; }

        public static TemporaryFile Create() => new(System.IO.Path.GetTempFileName());

        public void Dispose() => File.Delete(Path);
    }
}
