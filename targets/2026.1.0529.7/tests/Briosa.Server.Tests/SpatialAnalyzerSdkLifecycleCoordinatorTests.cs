using Briosa.Server.Services;
using Briosa.Server.Workers;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "xUnit tests intentionally retain their test synchronization behavior.")]
public sealed class SpatialAnalyzerSdkLifecycleCoordinatorTests
{
    [Fact]
    public async Task ExplicitLifecycleStartsDisconnectedThenConnectsAndStops()
    {
        await using var supervisor = CreateSupervisor(_ => "disconnected");
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        var application = new FakeApplicationStateProvider(RunningApplication(7));
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            application);

        var initial = coordinator.Current;
        var started = await coordinator.StartAsync(CancellationToken.None);
        var connected = await coordinator.ConnectAsync(
            started.SdkGeneration,
            reconnect: false,
            CancellationToken.None);
        var stopped = await coordinator.StopAsync(
            connected.SdkGeneration,
            CancellationToken.None);

        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Stopped, initial.SdkState);
        Assert.False(initial.HasSdkGeneration);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Running, started.SdkState);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerConnectionState.Disconnected,
            started.ConnectionState);
        Assert.False(started.ReadyForMp);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Ready, connected.SdkState);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerConnectionState.Connected,
            connected.ConnectionState);
        Assert.True(connected.ReadyForMp);
        Assert.True(connected.HasApplicationGeneration);
        Assert.Equal(7, connected.ApplicationGeneration);
        Assert.True(connected.StateRevision > started.StateRevision);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Stopped, stopped.SdkState);
        Assert.False(stopped.HasSdkGeneration);
        Assert.False(stopped.HasApplicationGeneration);
    }

    [Fact]
    public async Task SdkStartupTimeoutReturnsTypedDeadline()
    {
        await using var supervisor = CreateSupervisor(
            _ => "hang-before-ready",
            startupTimeout: TimeSpan.FromMilliseconds(150));
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            new FakeApplicationStateProvider(RunningApplication(1)));

        var exception = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.StartAsync(CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.DeadlineExceeded, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
            exception.Detail.Kind);
        Assert.Equal("worker-startup-timeout", exception.Detail.DiagnosticCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            exception.Detail.State.RecoveryState);
    }

    [Fact]
    public async Task ConnectTimeoutQuarantinesTheGenerationAndReturnsTypedDeadline()
    {
        await using var supervisor = CreateSupervisor(
            _ => "hang-on-connect",
            startupTimeout: TimeSpan.FromSeconds(1));
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            new FakeApplicationStateProvider(RunningApplication(2)));
        var started = await coordinator.StartAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(
                started.SdkGeneration,
                reconnect: false,
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.DeadlineExceeded, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
            exception.Detail.Kind);
        Assert.Equal("connect-ex-timeout", exception.Detail.DiagnosticCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Faulted,
            exception.Detail.State.SdkState);
        Assert.Equal(
            global::Briosa.LifecycleRecoveryGuidance.RecoverSdkWithoutReplay,
            exception.Detail.RecoveryGuidance);
    }

    [Fact]
    public async Task SdkShutdownTimeoutReturnsTypedDeadlineWithStoppedState()
    {
        await using var supervisor = CreateSupervisor(
            _ => "ignore-stop",
            shutdownTimeout: TimeSpan.FromMilliseconds(150));
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            new FakeApplicationStateProvider(RunningApplication(2)));
        var started = await coordinator.StartAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.StopAsync(started.SdkGeneration, CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.DeadlineExceeded, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.Timeout,
            exception.Detail.Kind);
        Assert.Equal("worker-stop-ack-timeout", exception.Detail.DiagnosticCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Stopped,
            exception.Detail.State.SdkState);
    }

    [Fact]
    public async Task SdkActivationFailureReturnsTypedFaultAndCanBeRecovered()
    {
        await using var supervisor = CreateSupervisor(generation => generation == 1
            ? "sdk-activation-failed"
            : "disconnected");
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            new FakeApplicationStateProvider(RunningApplication(1)));

        var exception = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.StartAsync(CancellationToken.None));
        var faulted = exception.Detail.State;
        var recovered = await coordinator.RecoverAsync(
            faulted.SdkGeneration,
            global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay,
            CancellationToken.None);

        Assert.Equal(Grpc.Core.StatusCode.Unavailable, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkStartFailed,
            exception.Detail.Kind);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Faulted, faulted.SdkState);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            faulted.RecoveryState);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkTerminationKind.StartFailed,
            faulted.LastIncident!.TerminationKind);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Running, recovered.SdkState);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerConnectionState.Disconnected,
            recovered.ConnectionState);
    }

    [Fact]
    public async Task UnavailableConnectExCanBeRetriedWithReconnect()
    {
        await using var supervisor = CreateSupervisor(_ => "connect-unavailable-once");
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            new FakeApplicationStateProvider(RunningApplication(4)));
        var started = await coordinator.StartAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(
                started.SdkGeneration,
                reconnect: false,
                CancellationToken.None));
        var reconnected = await coordinator.ConnectAsync(
            started.SdkGeneration,
            reconnect: true,
            CancellationToken.None);

        Assert.Equal(Grpc.Core.StatusCode.Unavailable, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkConnectionFailed,
            exception.Detail.Kind);
        Assert.Equal(
            global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange,
            exception.Detail.RecoveryGuidance);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkRecoveryState.NotRequired,
            exception.Detail.State.RecoveryState);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Ready, reconnected.SdkState);
        Assert.True(reconnected.ReadyForMp);
        Assert.Equal(started.SdkGeneration, reconnected.SdkGeneration);
    }

    [Fact]
    public async Task ConnectRequiresARunningApplicationAndReturnsTypedNotFound()
    {
        await using var supervisor = CreateSupervisor(_ => "disconnected");
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        var application = new FakeApplicationStateProvider(new()
        {
            StateRevision = 1,
            ApplicationState = global::Briosa.SpatialAnalyzerApplicationState.NotRunning,
            Ownership = global::Briosa.SpatialAnalyzerOwnership.None,
            DiagnosticCode = "spatial-analyzer-not-running"
        });
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            application);
        var started = await coordinator.StartAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(
                started.SdkGeneration,
                reconnect: false,
                CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.NotFound, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.ApplicationNotFound,
            exception.Detail.Kind);
        Assert.Equal(
            global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange,
            exception.Detail.RecoveryGuidance);
        Assert.False(exception.Detail.State.ReadyForMp);
    }

    [Fact]
    public async Task StaleGenerationCannotStopAReplacement()
    {
        await using var supervisor = CreateSupervisor(_ => "disconnected");
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            new FakeApplicationStateProvider(RunningApplication(1)));
        var first = await coordinator.StartAsync(CancellationToken.None);
        _ = await coordinator.StopAsync(first.SdkGeneration, CancellationToken.None);
        var second = await coordinator.StartAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.StopAsync(first.SdkGeneration, CancellationToken.None));

        Assert.Equal(Grpc.Core.StatusCode.Aborted, exception.StatusCode);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict,
            exception.Detail.Kind);
        Assert.Equal(second.SdkGeneration, coordinator.Current.SdkGeneration);
    }

    [Fact]
    public async Task UnexpectedSdkEngineLossRetainsIncidentAcrossExplicitRecovery()
    {
        await using var supervisor = CreateSupervisor(generation => generation == 1
            ? "sdk-process-exit-on-ping"
            : "disconnected");
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(
            supervisor,
            projection,
            new FakeApplicationStateProvider(RunningApplication(3)));
        var started = await coordinator.StartAsync(CancellationToken.None);
        _ = await coordinator.ConnectAsync(
            started.SdkGeneration,
            reconnect: false,
            CancellationToken.None);

        var faulted = await WaitForState(
            coordinator,
            state => state.SdkState == global::Briosa.SpatialAnalyzerSdkState.Faulted);
        var recovered = await coordinator.RecoverAsync(
            faulted.SdkGeneration,
            global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay,
            CancellationToken.None);

        Assert.False(faulted.ReadyForMp);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            faulted.RecoveryState);
        Assert.NotNull(faulted.LastIncident);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkTerminationKind.SdkProcessExited,
            faulted.LastIncident.TerminationKind);
        Assert.Equal(started.SdkGeneration, faulted.LastIncident.SdkGeneration);
        Assert.Equal(started.SdkGeneration + 1, recovered.SdkGeneration);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Running, recovered.SdkState);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerConnectionState.Disconnected,
            recovered.ConnectionState);
        Assert.False(recovered.ReadyForMp);
        Assert.NotNull(recovered.LastIncident);
        Assert.Equal(
            global::Briosa.SpatialAnalyzerSdkTerminationKind.SdkProcessExited,
            recovered.LastIncident.TerminationKind);
    }

    private static WorkerProcessSupervisor CreateSupervisor(
        Func<int, string> scenarioFactory,
        TimeSpan? startupTimeout = null,
        TimeSpan? shutdownTimeout = null) =>
        new(
            new NamedPipeWorkerProcessFactory(generation =>
                CreateLaunch(scenarioFactory(generation))),
            new WorkerRestartPolicy(
                maximumRestarts: 0,
                restartWindow: TimeSpan.FromSeconds(1),
                heartbeatInterval: TimeSpan.FromMilliseconds(25),
                heartbeatTimeout: TimeSpan.FromMilliseconds(250),
                startupTimeout: startupTimeout ?? TimeSpan.FromSeconds(3),
                shutdownTimeout: shutdownTimeout ?? TimeSpan.FromSeconds(2),
                restartDelay: TimeSpan.Zero),
            new WorkerExecutionPolicy(
                watchdogTimeout: TimeSpan.FromSeconds(2),
                queueCapacity: 4),
            identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(
                "2026.1.0529.7",
                activatedSdkVersion: "2026.1.0529.7",
                connectedSpatialAnalyzerVersion: "2026.1.0529.7"));

    private static WorkerProcessLaunch CreateLaunch(string scenario)
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable));
        return new WorkerProcessLaunch(
            executable,
            ["--scenario", scenario],
            Path.GetDirectoryName(executable));
    }

    private static global::Briosa.SpatialAnalyzerLifecycleState RunningApplication(
        int generation) => new()
        {
            StateRevision = 2,
            ApplicationState = global::Briosa.SpatialAnalyzerApplicationState.Running,
            Ownership = global::Briosa.SpatialAnalyzerOwnership.External,
            ApplicationGeneration = generation,
            DiagnosticCode = "external-spatial-analyzer-running"
        };

    private static async Task<global::Briosa.SpatialAnalyzerSdkLifecycleState>
        WaitForState(
            SpatialAnalyzerSdkLifecycleCoordinator coordinator,
            Func<global::Briosa.SpatialAnalyzerSdkLifecycleState, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            var current = coordinator.Current;
            if (predicate(current))
            {
                return current;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    private sealed class FakeApplicationStateProvider(
        global::Briosa.SpatialAnalyzerLifecycleState state)
        : ISpatialAnalyzerLifecycleStateProvider
    {
        public Task<global::Briosa.SpatialAnalyzerLifecycleState> GetCurrentAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(state.Clone());
        }
    }
}
