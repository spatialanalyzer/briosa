using Briosa.Desktop;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "xUnit tests intentionally retain their test synchronization behavior.")]
public sealed class SpatialAnalyzerSdkLifecycleTypedErrorTests
{
    private const string SdkErrorMetadataKey =
        "briosa-spatial-analyzer-sdk-lifecycle-error-bin";

    [Fact]
    public async Task ReconnectWhileConnectedWithoutExecutionReadinessIsTypedAndDoesNotCallTheSupervisor()
    {
        var supervisor = new FakeLifecycleController(Snapshot(
            WorkerLifecycleState.Ready, connection: Connection(WorkerExecutionReadinessState.Unverified)));
        await using var coordinator = CreateCoordinator(supervisor);

        var failure = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(1, reconnect: true, CancellationToken.None));

        AssertIdentityNotReadyReconnect(failure.StatusCode, failure.Detail);
        Assert.Equal(0, supervisor.Calls);
    }

    [Theory]
    [InlineData(false, "Degraded", 1, StatusCode.FailedPrecondition,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkRecoveryRequired, "sdk-recovery-required",
        global::Briosa.LifecycleRecoveryGuidance.RecoverSdkWithoutReplay)]
    [InlineData(true, "Stopped", 1, StatusCode.FailedPrecondition,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning, "sdk-not-running",
        global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange)]
    [InlineData(false, "Stopping", 1, StatusCode.Aborted,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict, "sdk-lifecycle-transition-in-progress",
        global::Briosa.LifecycleRecoveryGuidance.RefreshState)]
    [InlineData(true, "Ready", 2, StatusCode.Aborted,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict, "sdk-generation-stale",
        global::Briosa.LifecycleRecoveryGuidance.RefreshState)]
    [InlineData(false, "Ready", 1, StatusCode.Aborted,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.StateConflict, "sdk-lifecycle-state-changed",
        global::Briosa.LifecycleRecoveryGuidance.RefreshState)]
    public async Task SupervisorRejectionAfterAStateChangeIsClassifiedFromFreshState(
        bool reconnect,
        string freshState,
        int freshGeneration,
        StatusCode statusCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance)
    {
        var supervisor = new FakeLifecycleController(Snapshot(WorkerLifecycleState.Ready,
            connection: Connection(WorkerExecutionReadinessState.Unverified, WorkerConnectionState.Disconnected)));
        supervisor.Rejection = () =>
        {
            supervisor.Current = Snapshot(
                Enum.Parse<WorkerLifecycleState>(freshState), freshGeneration, revision: 2);
            return new InvalidOperationException("supervisor rejected the transition");
        };
        await using var coordinator = CreateCoordinator(supervisor);

        var failure = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(1, reconnect, CancellationToken.None));

        Assert.Equal(statusCode, failure.StatusCode);
        Assert.Equal(kind, failure.Detail.Kind);
        Assert.Equal(diagnosticCode, failure.Detail.DiagnosticCode);
        Assert.Equal(recoveryGuidance, failure.Detail.RecoveryGuidance);
        Assert.False(failure.Detail.State.ReadyForMp);
        Assert.Equal(1, supervisor.Calls);
    }

    [Fact]
    public async Task SupervisorRejectionForAConnectedGenerationKeepsReconnectTyped()
    {
        var supervisor = new FakeLifecycleController(Snapshot(WorkerLifecycleState.Ready,
            connection: Connection(WorkerExecutionReadinessState.Unverified, WorkerConnectionState.Disconnected)));
        supervisor.Rejection = () =>
        {
            supervisor.Current = Snapshot(WorkerLifecycleState.Ready, revision: 2,
                connection: Connection(WorkerExecutionReadinessState.Unverified));
            return new InvalidOperationException("already connected");
        };
        await using var coordinator = CreateCoordinator(supervisor);

        var failure = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(1, reconnect: true, CancellationToken.None));

        AssertIdentityNotReadyReconnect(failure.StatusCode, failure.Detail);
    }

    [Fact]
    public async Task StartStopAndRecoverRejectionsAreClassifiedFromFreshState()
    {
        var supervisor = new FakeLifecycleController(Snapshot(WorkerLifecycleState.Stopped));
        supervisor.Rejection = () =>
        {
            supervisor.Current = Snapshot(WorkerLifecycleState.Starting, revision: 2);
            return new InvalidOperationException("already active");
        };
        await using var coordinator = CreateCoordinator(supervisor);
        var start = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.StartAsync(CancellationToken.None));

        supervisor.Current = Snapshot(WorkerLifecycleState.Ready, revision: 3);
        supervisor.Rejection = () =>
        {
            supervisor.Current = Snapshot(WorkerLifecycleState.Stopped, revision: 4);
            return new InvalidOperationException("stopped by the host");
        };
        var stop = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.StopAsync(1, CancellationToken.None));

        supervisor.Current = Snapshot(WorkerLifecycleState.Degraded, revision: 5);
        supervisor.Rejection = () =>
        {
            supervisor.Current = Snapshot(WorkerLifecycleState.Stopped, revision: 6);
            return new InvalidOperationException("not faulted");
        };
        var recover = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.RecoverAsync(1,
                global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay,
                CancellationToken.None));

        Assert.Equal(StatusCode.FailedPrecondition, start.StatusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkAlreadyActive, start.Detail.Kind);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Starting, start.Detail.State.SdkState);
        Assert.Equal(StatusCode.FailedPrecondition, stop.StatusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning, stop.Detail.Kind);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Stopped, stop.Detail.State.SdkState);
        Assert.Equal(StatusCode.FailedPrecondition, recover.StatusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.RecoveryNotRequired, recover.Detail.Kind);
        Assert.Equal(3, supervisor.Calls);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("connect")]
    [InlineData("reconnect")]
    [InlineData("stop")]
    [InlineData("recover")]
    public async Task DisposedSupervisorReturnsTypedUnavailable(string action)
    {
        var supervisor = new FakeLifecycleController(Snapshot(action switch
        {
            "start" => WorkerLifecycleState.Stopped,
            "recover" => WorkerLifecycleState.Degraded,
            _ => WorkerLifecycleState.Ready
        }, connection: Connection(WorkerExecutionReadinessState.Unverified, WorkerConnectionState.Disconnected)));
        supervisor.Rejection = () => new ObjectDisposedException(nameof(WorkerProcessSupervisor));
        await using var coordinator = CreateCoordinator(supervisor);

        var failure = await Assert.ThrowsAsync<SdkLifecycleException>(() => action switch
        {
            "start" => coordinator.StartAsync(CancellationToken.None),
            "connect" => coordinator.ConnectAsync(1, reconnect: false, CancellationToken.None),
            "reconnect" => coordinator.ConnectAsync(1, reconnect: true, CancellationToken.None),
            "stop" => coordinator.StopAsync(1, CancellationToken.None),
            _ => coordinator.RecoverAsync(1,
                global::Briosa.SpatialAnalyzerSdkRecoveryMode.ReplaceWithoutReplay,
                CancellationToken.None)
        });

        Assert.Equal(StatusCode.Unavailable, failure.StatusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning, failure.Detail.Kind);
        Assert.Equal("worker-supervisor-stopping", failure.Detail.DiagnosticCode);
        Assert.Equal(global::Briosa.LifecycleRecoveryGuidance.RetryAfterStateChange,
            failure.Detail.RecoveryGuidance);
        Assert.Equal(1, supervisor.Calls);
    }

    [Fact]
    public async Task UnattestedConnectThenReconnectReturnTypedErrorsWithoutMpReadiness()
    {
        await using var supervisor = CreateUnattestedSupervisor();
        await using var coordinator = new SpatialAnalyzerSdkLifecycleCoordinator(supervisor,
            new SpatialAnalyzerSdkLifecycleStateProjection(supervisor), new RunningApplication());
        var started = await coordinator.StartAsync(CancellationToken.None);

        var connect = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(started.SdkGeneration, reconnect: false, CancellationToken.None));
        var reconnect = await Assert.ThrowsAsync<SdkLifecycleException>(() =>
            coordinator.ConnectAsync(started.SdkGeneration, reconnect: true, CancellationToken.None));

        Assert.Equal(StatusCode.FailedPrecondition, connect.StatusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.IdentityMismatch, connect.Detail.Kind);
        Assert.Equal("runtime-identity-not-ready", connect.Detail.DiagnosticCode);
        Assert.False(connect.Detail.State.ReadyForMp);
        AssertIdentityNotReadyReconnect(reconnect.StatusCode, reconnect.Detail);
        Assert.Equal(started.SdkGeneration, reconnect.Detail.State.SdkGeneration);
        Assert.False(coordinator.Current.ReadyForMp);
        Assert.False(supervisor.Current.ReadyForExecution);

        var stopped = await coordinator.StopAsync(started.SdkGeneration, CancellationToken.None);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Stopped, stopped.SdkState);
    }

    [Fact]
    public async Task GrpcReconnectWithoutExecutionReadinessReturnsTypedTrailer()
    {
        var supervisor = new FakeLifecycleController(Snapshot(
            WorkerLifecycleState.Ready, connection: Connection(WorkerExecutionReadinessState.Unverified)));
        await using var coordinator = CreateCoordinator(supervisor);
        await using var host = await GrpcTestHost.StartSdkLifecycleAsync(coordinator);
        var client = new global::Briosa.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient(host.Channel);

        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ReconnectToSpatialAnalyzerAsync(
                new global::Briosa.ReconnectToSpatialAnalyzerRequest { ExpectedSdkGeneration = 1 }));

        var detail = global::Briosa.SpatialAnalyzerSdkLifecycleError.Parser.ParseFrom(
            Assert.Single(error.Trailers, entry => entry.Key == SdkErrorMetadataKey).ValueBytes);
        AssertIdentityNotReadyReconnect(error.StatusCode, detail);
        Assert.Equal("ReconnectToSpatialAnalyzer", detail.Rpc);
        Assert.Equal("runtime-identity-not-ready", error.Status.Detail);
        Assert.Equal(0, supervisor.Calls);
    }

    [Fact]
    public async Task GrpcStopRacingHostShutdownReturnsTypedTrailer()
    {
        var supervisor = new FakeLifecycleController(Snapshot(WorkerLifecycleState.Ready))
        {
            Rejection = () => new ObjectDisposedException(nameof(WorkerProcessSupervisor))
        };
        await using var coordinator = CreateCoordinator(supervisor);
        await using var host = await GrpcTestHost.StartSdkLifecycleAsync(coordinator);
        var client = new global::Briosa.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient(host.Channel);

        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.StopSpatialAnalyzerSdkAsync(
                new global::Briosa.StopSpatialAnalyzerSdkRequest { ExpectedSdkGeneration = 1 }));

        var detail = global::Briosa.SpatialAnalyzerSdkLifecycleError.Parser.ParseFrom(
            Assert.Single(error.Trailers, entry => entry.Key == SdkErrorMetadataKey).ValueBytes);
        Assert.Equal(StatusCode.Unavailable, error.StatusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.SdkNotRunning, detail.Kind);
        Assert.Equal("worker-supervisor-stopping", detail.DiagnosticCode);
        Assert.Equal("StopSpatialAnalyzerSdk", detail.Rpc);
    }

    private static void AssertIdentityNotReadyReconnect(
        StatusCode statusCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleError detail)
    {
        Assert.Equal(StatusCode.FailedPrecondition, statusCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.IdentityMismatch, detail.Kind);
        Assert.Equal("runtime-identity-not-ready", detail.DiagnosticCode);
        Assert.Equal(global::Briosa.LifecycleRecoveryGuidance.StopSdkFirst, detail.RecoveryGuidance);
        Assert.Equal(global::Briosa.SpatialAnalyzerConnectionState.Connected, detail.State.ConnectionState);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Running, detail.State.SdkState);
        Assert.False(detail.State.ReadyForMp);
    }

    private static SpatialAnalyzerSdkLifecycleCoordinator CreateCoordinator(
        FakeLifecycleController supervisor) =>
        new(supervisor, new SpatialAnalyzerSdkLifecycleStateProjection(supervisor), new RunningApplication());

    private static WorkerProcessSupervisor CreateUnattestedSupervisor() => new(
        new NamedPipeWorkerProcessFactory(_ => new WorkerProcessLaunch(
            Path.Combine(AppContext.BaseDirectory, "worker-test-host", "Briosa.Worker.TestHost.exe"),
            ["--scenario", "disconnected"])),
        new WorkerLifecyclePolicy(
            heartbeatInterval: TimeSpan.FromMilliseconds(50),
            heartbeatTimeout: TimeSpan.FromSeconds(1),
            startupTimeout: TimeSpan.FromSeconds(3),
            shutdownTimeout: TimeSpan.FromSeconds(2)),
        new WorkerExecutionPolicy(watchdogTimeout: TimeSpan.FromSeconds(2), queueCapacity: 4),
        identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(DesktopProtocol.Target));

    private static WorkerLifecycleSnapshot Snapshot(
        WorkerLifecycleState state,
        int generation = 1,
        long revision = 1,
        WorkerConnectionSnapshot? connection = null) =>
        new(state, generation, state == WorkerLifecycleState.Stopped ? null : 100, 0,
            WorkerTerminationKind.None, "test-state", connection, DateTimeOffset.UnixEpoch,
            StateRevision: revision, AdmissionOpen: state == WorkerLifecycleState.Ready);

    private static WorkerConnectionSnapshot Connection(
        WorkerExecutionReadinessState readiness,
        WorkerConnectionState state = WorkerConnectionState.Connected) =>
        new(state, readiness, 0, 1, 1, "runtime-identity-not-ready", DateTimeOffset.UnixEpoch);

    private sealed class FakeLifecycleController(WorkerLifecycleSnapshot current)
        : IWorkerLifecycleController
    {
        public WorkerLifecycleSnapshot Current { get; set; } = current;

        public Func<Exception>? Rejection { get; set; }

        public int Calls { get; private set; }

        public Task<WorkerLifecycleResult> StartAsync(CancellationToken cancellationToken = default) =>
            Reject();

        public Task<WorkerLifecycleResult> ConnectAsync(
            int expectedGeneration,
            CancellationToken cancellationToken = default) =>
            Reject();

        public Task<WorkerLifecycleResult> RecoverSdkAsync(
            int expectedGeneration,
            CancellationToken cancellationToken = default) =>
            Reject();

        public Task<WorkerLifecycleResult> StopAsync(CancellationToken cancellationToken = default) =>
            Reject();

        public Task<WorkerLifecycleSnapshot> AssociateApplicationGenerationAsync(
            int expectedGeneration,
            int? applicationGeneration,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Current);

        private Task<WorkerLifecycleResult> Reject()
        {
            Calls++;
            return Task.FromException<WorkerLifecycleResult>(
                Rejection?.Invoke() ?? new InvalidOperationException("unexpected supervisor call"));
        }
    }

    private sealed class RunningApplication : ISpatialAnalyzerLifecycleStateProvider
    {
        public Task<global::Briosa.SpatialAnalyzerLifecycleState> GetCurrentAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(new global::Briosa.SpatialAnalyzerLifecycleState
            {
                StateRevision = 1,
                ApplicationState = global::Briosa.SpatialAnalyzerApplicationState.Running,
                Ownership = global::Briosa.SpatialAnalyzerOwnership.External,
                ApplicationGeneration = 1
            });
    }
}
