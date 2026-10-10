using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Channels;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.Logging;
using ServerIdentityMatchState = Briosa.Server.Workers.RuntimeIdentityMatchState;
using ServerIdentitySource = Briosa.Server.Workers.RuntimeIdentityEvidenceSource;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
public sealed class WorkerProcessSupervisorTests
{
    [Fact]
    public async Task NormalLifecycleReportsStatesAndReleasesStaOnOwningThread()
    {
        var lifecycleRecordPath = Path.GetTempFileName();
        try
        {
            await using var supervisor = CreateSupervisor(
                _ => CreateLaunch("normal", lifecycleRecordPath),
                CreatePolicy());

            Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
            var ready = supervisor.Current;
            await supervisor.StopAsync();

            Assert.Equal(WorkerLifecycleState.Ready, ready.State);
            Assert.True(ready.ProcessId > 0);
            Assert.Equal(WorkerConnectionState.Connected, ready.Connection!.State);
            Assert.Equal(
                WorkerExecutionReadinessState.ExecutionReady,
                ready.Connection.ExecutionReadinessState);
            Assert.Equal(0, ready.Connection.StatusCode);
            Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
            Assert.Equal(WorkerTerminationKind.Graceful, supervisor.Current.LastTermination);
            Assert.Contains(
                supervisor.History,
                snapshot => snapshot.State == WorkerLifecycleState.Starting);
            Assert.Contains(
                supervisor.History,
                snapshot => snapshot.State == WorkerLifecycleState.Ready);

            using var lifecycle = JsonDocument.Parse(
                await File.ReadAllTextAsync(lifecycleRecordPath));
            var root = lifecycle.RootElement;
            Assert.Equal("STA", root.GetProperty("InitializationApartment").GetString());
            Assert.Equal("STA", root.GetProperty("ReleaseApartment").GetString());
            Assert.Equal(
                root.GetProperty("InitializationThreadId").GetInt32(),
                root.GetProperty("ReleaseThreadId").GetInt32());
        }
        finally
        {
            File.Delete(lifecycleRecordPath);
        }
    }

    [Fact]
    public async Task ProcessReportedRuntimeMismatchOverridesAttestationAndBlocksAdmission()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("runtime-identity-mismatch"),
            CreatePolicy());

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var current = supervisor.Current;
        var outcome = await supervisor.ExecuteAsync(CreateCommand("blocked-by-identity"));

        Assert.Equal(WorkerLifecycleState.Ready, current.State);
        Assert.Equal(
            WorkerExecutionReadinessState.Unverified,
            current.Connection!.ExecutionReadinessState);
        Assert.False(current.RuntimeIdentity!.AllowsExecution);
        Assert.Equal(
            ServerIdentitySource.RuntimeVerification,
            current.RuntimeIdentity.ActivatedSdk.Source);
        Assert.Equal(
            ServerIdentityMatchState.Mismatch,
            current.RuntimeIdentity.ActivatedSdk.MatchState);
        Assert.Equal(WorkerExecutionStatus.Unavailable, outcome.Status);
        Assert.Equal("runtime-identity-not-ready", outcome.DiagnosticCode);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
    }

    [Fact]
    public async Task MalformedProcessIdentityEvidenceFailsWorkerStartup()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("malformed-runtime-identity"),
            CreatePolicy());

        Assert.False((await supervisor.StartAsync()).Succeeded);

        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.Equal("worker-startup-failed", supervisor.Current.DiagnosticCode);
    }

    [Fact]
    public async Task UnavailableRuntimeIdentityWithoutAttestationBlocksAdmission()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(),
            identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(
                "2026.1.0529.7"));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var outcome = await supervisor.ExecuteAsync(CreateCommand("unattested"));

        Assert.False(supervisor.Current.RuntimeIdentity!.AllowsExecution);
        Assert.Equal(WorkerExecutionStatus.Unavailable, outcome.Status);
        Assert.Equal("runtime-identity-not-ready", outcome.DiagnosticCode);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
    }

    [Fact]
    public async Task HungWorkerRequiresExplicitSdkRecovery()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? "hang-on-ping" : "normal"),
            CreatePolicy(),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        await clock.FireNextAsync(HeartbeatInterval);
        await clock.FireNextAsync(HeartbeatTimeout);
        var faulted = await WaitFor(
            supervisor,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.DiagnosticCode == "worker-heartbeat-timeout");

        Assert.Equal(1, faulted.Generation);
        Assert.Equal(0, faulted.RecoveryCount);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Forced &&
                snapshot.DiagnosticCode == "worker-heartbeat-timeout");

        Assert.True((await supervisor.RecoverSdkAsync(faulted.Generation)).Succeeded);
        Assert.Equal(2, supervisor.Current.Generation);
        Assert.Equal(1, supervisor.Current.RecoveryCount);
        Assert.Equal(WorkerLifecycleState.Ready, supervisor.Current.State);

        await supervisor.StopAsync();
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
    }

    [Fact]
    public async Task CrashedWorkerIsObservedAndRequiresExplicitSdkRecovery()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? "crash-on-ping" : "normal"),
            CreatePolicy(),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        await clock.FireNextAsync(HeartbeatInterval);
        var faulted = await WaitFor(
            supervisor,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded);

        Assert.Equal(1, faulted.Generation);
        Assert.Equal(0, faulted.RecoveryCount);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Crash);

        Assert.True((await supervisor.RecoverSdkAsync(faulted.Generation)).Succeeded);
        Assert.Equal(2, supervisor.Current.Generation);

        await supervisor.StopAsync();
    }

    [Fact]
    public async Task ExplicitRecoveryReplacesRuntimeLoopsAfterReplacementProbeQuarantine()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation switch
            {
                1 => "crash-on-ping",
                2 => "hang-on-verify",
                _ => "normal"
            }),
            CreatePolicy(readinessProbeTimeout: TimeSpan.FromMilliseconds(150)),
            CreateExecutionPolicy(),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        await clock.FireNextAsync(HeartbeatInterval);
        var quarantined = await WaitFor(
            supervisor,
            snapshot => snapshot.Generation == 1 &&
                snapshot.State == WorkerLifecycleState.Degraded);

        Assert.Equal(0, quarantined.RecoveryCount);
        var recovering = supervisor.RecoverSdkAsync(quarantined.Generation);
        await clock.FireNextAsync(TimeSpan.FromMilliseconds(150));
        Assert.False((await recovering.WaitAsync(ProcessBound)).Succeeded);
        Assert.Equal(2, supervisor.Current.Generation);
        Assert.Equal(
            WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection?.ExecutionReadinessState);
        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var completed = await supervisor.ExecuteAsync(CreateCommand("after-recovery"));

        Assert.Equal(3, supervisor.Current.Generation);
        Assert.Equal(WorkerExecutionStatus.Completed, completed.Status);
        Assert.Equal(3, completed.Generation);
    }

    [Theory]
    [InlineData(
        "hang-on-verify",
        "Forced",
        "execution-readiness-probe-timeout")]
    [InlineData(
        "crash-on-verify",
        "Crash",
        "execution-readiness-worker-exited")]
    public async Task AmbiguousVerificationQuarantinesWithoutAutomaticRestart(
        string firstScenario,
        string expectedTermination,
        string expectedDiagnosticCode)
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? firstScenario : "normal"),
            CreatePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(10),
                readinessProbeTimeout: TimeSpan.FromMilliseconds(150)),
            CreateExecutionPolicy(),
            timeProvider: clock);

        var starting = supervisor.StartAsync();
        if (firstScenario == "hang-on-verify")
            await clock.FireNextAsync(TimeSpan.FromMilliseconds(150));
        Assert.False((await starting.WaitAsync(ProcessBound)).Succeeded);
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(1, supervisor.Current.Generation);
        Assert.Equal(0, supervisor.Current.RecoveryCount);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.Equal(
            Enum.Parse<WorkerTerminationKind>(expectedTermination),
            supervisor.Current.LastTermination);
        Assert.Equal(expectedDiagnosticCode, supervisor.Current.DiagnosticCode);
        Assert.Equal(
            WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.CompetingClientSuspected);
        Assert.Equal(
            1,
            supervisor.History.Count(snapshot =>
                snapshot.DiagnosticCode == "worker-starting"));

        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        Assert.Equal(2, supervisor.Current.Generation);
        Assert.Equal(
            WorkerExecutionReadinessState.ExecutionReady,
            supervisor.Current.Connection!.ExecutionReadinessState);
    }

    [Fact]
    public async Task CompletedVerificationFailureRequiresExplicitRecoveryWithoutCompetingClaim()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? "reject-verify" : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.False((await supervisor.StartAsync()).Succeeded);

        Assert.Equal(1, supervisor.Current.Generation);
        Assert.Equal("execution-readiness-probe-mp-failed", supervisor.Current.DiagnosticCode);
        Assert.Equal(
            WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
        Assert.DoesNotContain(
            supervisor.History,
            snapshot => snapshot.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.CompetingClientSuspected);
        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
    }

    // #305 (F10) deliberately replaced the earlier rule that cancelling the caller
    // during verification quarantines the target. The caller stops waiting; the
    // accepted probe keeps running, and only its own bound can quarantine.
    [Fact]
    public async Task CallerCancellationDuringVerificationLeavesTheProbeToItsOwnBound()
    {
        var clock = new HeartbeatTestClock();
        // Distinct from the five-second startup bound, so the wait matches the probe.
        var probeBound = TimeSpan.FromSeconds(7);
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("hang-on-verify"),
            CreatePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(10),
                readinessProbeTimeout: probeBound),
            timeProvider: clock);
        using var cancellation = new CancellationTokenSource();

        try
        {
            var starting = supervisor.StartAsync(cancellation.Token);
            await clock.WaitForScheduledAsync(probeBound);
            await cancellation.CancelAsync();
            var detached = Assert.IsType<WorkerLifecycleDetached>(await starting.WaitAsync(ProcessBound));

            Assert.Equal(
                WorkerExecutionReadinessState.Verifying,
                detached.Snapshot.Connection!.ExecutionReadinessState);
            Assert.Equal(
                WorkerExecutionReadinessState.Verifying,
                supervisor.Current.Connection!.ExecutionReadinessState);
            Assert.NotNull(supervisor.Current.ProcessId);

            await clock.FireNextAsync(probeBound);
            var quarantined = await WaitFor(
                supervisor,
                snapshot => snapshot.State == WorkerLifecycleState.Degraded && snapshot.ProcessId is null);
            Assert.Equal("execution-readiness-probe-timeout", quarantined.DiagnosticCode);
            Assert.Equal(WorkerLifecycleFailure.ReadinessTimeout, quarantined.LifecycleFailure);
            Assert.Equal(
                WorkerExecutionReadinessState.OperatorRecoveryRequired,
                quarantined.Connection!.ExecutionReadinessState);
            Assert.DoesNotContain(
                supervisor.History,
                snapshot => snapshot.DiagnosticCode.Contains("cancelled", StringComparison.Ordinal));
        }
        catch
        {
            // Lets disposal finish when an assertion fails before the bound fires.
            clock.Advance(TimeSpan.FromHours(9));
            throw;
        }
    }

    [Fact]
    public async Task CallerCancellationDuringConnectExLeavesTheExchangeToTheStartupBound()
    {
        var clock = new HeartbeatTestClock();
        var startupBound = TimeSpan.FromSeconds(5);
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("hang-on-connect"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            timeProvider: clock);
        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        using var cancellation = new CancellationTokenSource();

        try
        {
            var connecting = supervisor.ConnectAsync(supervisor.Current.Generation, cancellation.Token);
            await clock.WaitForScheduledAsync(startupBound);
            await cancellation.CancelAsync();
            var detached = Assert.IsType<WorkerLifecycleDetached>(await connecting.WaitAsync(ProcessBound));

            Assert.Equal(WorkerConnectionState.Connecting, detached.Snapshot.Connection!.State);
            Assert.Equal(WorkerLifecycleState.Starting, supervisor.Current.State);
            await clock.FireNextAsync(startupBound);
            var retired = await WaitFor(
                supervisor,
                snapshot => snapshot.State == WorkerLifecycleState.Degraded);
            Assert.Equal("connect-ex-timeout", retired.DiagnosticCode);
            Assert.Equal(WorkerLifecycleFailure.ConnectionTimeout, retired.LifecycleFailure);
            Assert.DoesNotContain(
                supervisor.History,
                snapshot => snapshot.DiagnosticCode.Contains("cancelled", StringComparison.Ordinal));
        }
        catch
        {
            // Lets disposal finish when an assertion fails before the bound fires.
            clock.Advance(TimeSpan.FromHours(9));
            throw;
        }
    }

    [Fact]
    public async Task CrashDoesNotStartAnAutomaticReplacementLoop()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("crash-on-ping"),
            CreatePolicy(),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        await clock.FireNextAsync(HeartbeatInterval);
        var faulted = await WaitFor(
            supervisor,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded);

        Assert.Equal(1, faulted.Generation);
        Assert.Equal(0, faulted.RecoveryCount);
        Assert.Equal(
            1,
            supervisor.History.Count(
                snapshot => snapshot.DiagnosticCode == "worker-starting"));

        await supervisor.StopAsync();
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
    }

    [Fact]
    public async Task GracefulStopTimeoutEscalatesToForcedTermination()
    {
        var policy = CreatePolicy(
            heartbeatInterval: TimeSpan.FromSeconds(10),
            shutdownTimeout: TimeSpan.FromMilliseconds(200));
        var clock = new HeartbeatTestClock();
        StopSendTrackingFactory? tracking = null;
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("ignore-stop"),
            policy,
            timeProvider: clock,
            wrapFactory: factory => tracking = new StopSendTrackingFactory(factory));

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        var stopping = supervisor.StopAsync();
        await tracking!.StopSent.Task.WaitAsync(ProcessBound);
        await clock.FireNextAsync(TimeSpan.FromMilliseconds(200));
        await stopping.WaitAsync(ProcessBound);

        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Equal(WorkerTerminationKind.Forced, supervisor.Current.LastTermination);
        Assert.Equal("worker-stop-ack-timeout", supervisor.Current.DiagnosticCode);
    }

    [Fact]
    public async Task ShutdownDrainsAnInFlightHeartbeatBeforeSendingStop()
    {
        await using var process = new CoordinatedHeartbeatProcess();
        var clock = new HeartbeatTestClock();
        await using var supervisor = new WorkerProcessSupervisor(
            new FixedWorkerProcessFactory(process),
            CreatePolicy(
                heartbeatInterval: TimeSpan.FromMilliseconds(1),
                shutdownTimeout: TimeSpan.FromSeconds(1)),
            CreateExecutionPolicy(),
            clock,
            identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(
                "2026.1.0529.7",
                activatedSdkVersion: "2026.1.0529.7",
                connectedSpatialAnalyzerVersion: "2026.1.0529.7"));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        // The held ping cannot reach its virtual heartbeat timeout.
        await clock.FireNextAsync(TimeSpan.FromMilliseconds(1));
        await process.PingStarted.WaitAsync(HangGuard);

        var stopping = supervisor.StopAsync();
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            Assert.False(stopping.IsCompleted);
            Assert.False(process.PingWasCancelled);
        }
        finally
        {
            process.ReleaseHeartbeat();
        }

        await stopping.WaitAsync(HangGuard);

        Assert.False(process.PingWasCancelled);
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Equal(WorkerTerminationKind.Graceful, supervisor.Current.LastTermination);
        Assert.Equal("worker-stopped", supervisor.Current.DiagnosticCode);
    }

    [Fact]
    public async Task FullQueueRejectsWithoutWaitingAndDrainIsObservable()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(queueCapacity: 2));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var active = supervisor.ExecuteAsync(CreateCommand("active"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.ActiveExecutions == 1);
        var queued = new[]
        {
            supervisor.ExecuteAsync(CreateCommand("queued-1")),
            supervisor.ExecuteAsync(CreateCommand("queued-2"))
        };
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.QueuedRequests == 2);
        var blocked = supervisor.ExecuteAsync(
            CreateCommand("blocked"));
        var rejected = await blocked;
        var completed = await Task.WhenAll([active, .. queued]);
        var drained = await WaitForExecution(
            supervisor,
            snapshot => snapshot.TerminalRequests == 3);

        Assert.Equal(WorkerExecutionStatus.Overloaded, rejected.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, rejected.ExecutionDisposition);
        Assert.All(
            completed,
            outcome => Assert.Equal(WorkerExecutionStatus.Completed, outcome.Status));
        Assert.Equal(2, drained.QueueCapacity);
        Assert.Equal(0, drained.QueuedRequests);
        Assert.Equal(0, drained.WaitingForAdmission);
        Assert.Equal(0, drained.ActiveExecutions);
        Assert.Equal(2, drained.PeakQueuedRequests);
        Assert.Equal(3, drained.AdmittedRequests);
        Assert.Equal(3, drained.TerminalRequests);
        Assert.Equal(0, drained.ClientCancellationsBeforeAdmission);
        Assert.Equal(0, drained.ClientCancellationsAfterAdmission);
    }

    // #305 (F9) replaced the earlier rule that a request cancelled while queued may
    // still be dispatched later. A hanging exchange holds the single consumer on the
    // virtual clock, so the queued request cannot be claimed before it is abandoned.
    [Fact]
    public async Task CancellationWhileQueuedAbandonsTheRequestWithoutDispatch()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("hang-on-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(TimeSpan.FromMilliseconds(150), queueCapacity: 2),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        var active = supervisor.ExecuteAsync(CreateCommand("active"));
        await clock.WaitForScheduledAsync(TimeSpan.FromMilliseconds(150));
        using var cancellation = new CancellationTokenSource();
        var queued = supervisor.ExecuteAsync(
            CreateCommand("cancelled-while-queued"),
            cancellation.Token);
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.QueuedRequests == 1);

        await cancellation.CancelAsync();
        WorkerExecutionOutcome abandoned;
        try
        {
            abandoned = await queued.WaitAsync(ProcessBound);
        }
        catch
        {
            // Lets disposal finish: the hung exchange ends only at its virtual budget.
            clock.Advance(TimeSpan.FromHours(9));
            throw;
        }
        await clock.FireNextAsync(TimeSpan.FromMilliseconds(150));
        var timedOut = await active.WaitAsync(ProcessBound);
        var drained = await WaitForExecution(
            supervisor,
            snapshot => snapshot.TerminalRequests == 2 && snapshot.QueuedRequests == 0);

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, abandoned.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, abandoned.ExecutionDisposition);
        Assert.Equal("queued-request-abandoned", abandoned.DiagnosticCode);
        Assert.Equal(1, abandoned.Generation);
        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, timedOut.Status);
        Assert.Equal("active", supervisor.Current.LastIncident!.OperationId);
        Assert.Equal(2, drained.AdmittedRequests);
        Assert.Equal(2, drained.TerminalRequests);
        Assert.Equal(1, drained.AbandonedRequests);
        Assert.Equal(0, drained.ActiveExecutions);
        Assert.Equal(1, drained.ClientCancellationsAfterAdmission);
    }

    [Fact]
    public async Task StopTerminatesEveryAdmissionAfterOverload()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(queueCapacity: 1));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var active = supervisor.ExecuteAsync(CreateCommand("active"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.ActiveExecutions == 1);
        var queued = supervisor.ExecuteAsync(CreateCommand("queued"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.QueuedRequests == 1);
        var waiting = supervisor.ExecuteAsync(CreateCommand("waiting"));
        Assert.Equal(WorkerExecutionStatus.Overloaded, (await waiting).Status);

        var stopping = supervisor.StopAsync();
        var outcomes = await Task.WhenAll(active, queued, waiting);
        await stopping;
        var drained = supervisor.ExecutionSnapshot;

        Assert.Equal(WorkerExecutionStatus.Completed, outcomes[0].Status);
        Assert.All(outcomes.Skip(1), outcome =>
        {
            Assert.True(outcome.Status is WorkerExecutionStatus.Unavailable or WorkerExecutionStatus.Overloaded);
            Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
        });
        Assert.Equal(drained.AdmittedRequests, drained.TerminalRequests);
        Assert.Equal(0, drained.QueuedRequests);
        Assert.Equal(0, drained.WaitingForAdmission);
        Assert.Equal(0, drained.ActiveExecutions);
        Assert.Equal(WorkerTerminationKind.Graceful, supervisor.Current.LastTermination);
        Assert.Equal("worker-stopped", supervisor.Current.DiagnosticCode);
    }

    [Fact]
    public async Task WatchdogFaultDoesNotReplayQueuedCallsBeforeExplicitRecovery()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1
                ? "drop-execution-response"
                : "normal"),
            CreatePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(10),
                lifecycleHistoryCapacity: 16),
            CreateExecutionPolicy(
                watchdogTimeout: TimeSpan.FromMilliseconds(150),
                queueCapacity: 2),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        var active = supervisor.ExecuteAsync(CreateCommand("watchdog-active"));
        var queued = supervisor.ExecuteAsync(CreateCommand("watchdog-queued"));
        await clock.FireNextAsync(TimeSpan.FromMilliseconds(150));
        var failures = await Task.WhenAll(active, queued).WaitAsync(ProcessBound);
        var snapshot = supervisor.ExecutionSnapshot;

        Assert.Contains(failures, outcome =>
            outcome.Status == WorkerExecutionStatus.WatchdogTimeout &&
            outcome.ExecutionDisposition ==
                WorkerExecutionDisposition.StartedOutcomeUnknown);
        Assert.Contains(failures, outcome =>
            outcome.Status == WorkerExecutionStatus.Unavailable &&
            outcome.ExecutionDisposition == WorkerExecutionDisposition.NotStarted);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.Equal(1, supervisor.Current.Generation);
        Assert.NotNull(supervisor.Current.LastIncident);
        Assert.Equal(
            "watchdog-active",
            supervisor.Current.LastIncident!.OperationId);
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            supervisor.Current.LastIncident.ExecutionDisposition);

        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var recovered = await supervisor.ExecuteAsync(CreateCommand("recovered"));
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Equal(1, snapshot.WatchdogTimeouts);
        Assert.Equal(2, snapshot.AdmittedRequests);
        Assert.Equal(snapshot.AdmittedRequests, snapshot.TerminalRequests);
        Assert.InRange(supervisor.History.Count, 1, 16);
        Assert.Equal(supervisor.Current, supervisor.History[^1]);
    }

    [Fact]
    public async Task WorkerCrashDoesNotReplayQueuedCallsBeforeExplicitRecovery()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1
                ? "crash-on-execute"
                : "normal"),
            CreatePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(10),
                lifecycleHistoryCapacity: 16),
            CreateExecutionPolicy(queueCapacity: 2));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var active = supervisor.ExecuteAsync(CreateCommand("crash-active"));
        var queued = supervisor.ExecuteAsync(CreateCommand("crash-queued"));
        var failures = await Task.WhenAll(active, queued);
        var snapshot = supervisor.ExecutionSnapshot;

        Assert.Contains(failures, outcome =>
            outcome.Status == WorkerExecutionStatus.WorkerFailure &&
            outcome.ExecutionDisposition ==
                WorkerExecutionDisposition.StartedOutcomeUnknown);
        Assert.Contains(failures, outcome =>
            outcome.Status == WorkerExecutionStatus.Unavailable &&
            outcome.ExecutionDisposition == WorkerExecutionDisposition.NotStarted);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.Equal(1, supervisor.Current.Generation);

        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var recovered = await supervisor.ExecuteAsync(CreateCommand("recovered"));
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Equal(1, snapshot.WorkerFailures);
        Assert.Equal(2, snapshot.AdmittedRequests);
        Assert.Equal(snapshot.AdmittedRequests, snapshot.TerminalRequests);
        Assert.InRange(supervisor.History.Count, 1, 16);
        Assert.Equal(supervisor.Current, supervisor.History[^1]);
    }

    [Fact]
    public async Task LifecycleHistoryRetainsOnlyTheReviewedCapacity()
    {
        const int historyCapacity = 6;
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(10),
                lifecycleHistoryCapacity: historyCapacity));

        for (var cycle = 0; cycle < 5; cycle++)
        {
            Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
            await supervisor.StopAsync();
            Assert.InRange(supervisor.History.Count, 1, historyCapacity);
            Assert.Equal(supervisor.Current, supervisor.History[^1]);
        }

        Assert.Equal(historyCapacity, supervisor.History.Count);
        Assert.DoesNotContain(
            supervisor.History,
            snapshot => snapshot.DiagnosticCode == "not-started");
    }


    [Fact]
    public async Task ConcurrentRequestsRemainSerializedAcrossTheWorkerPipe()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var first = supervisor.ExecuteAsync(CreateCommand("first"));
        await Task.Delay(TimeSpan.FromMilliseconds(25));
        var second = supervisor.ExecuteAsync(CreateCommand("second"));
        await Task.Delay(TimeSpan.FromMilliseconds(75));

        Assert.False(second.IsCompleted);
        var results = await Task.WhenAll(first, second);

        Assert.All(
            results,
            result => Assert.Equal(WorkerExecutionStatus.Completed, result.Status));
        Assert.Equal(300, results[0].Execution!.DurationMilliseconds);
        Assert.Equal(5, results[1].Execution!.DurationMilliseconds);
        Assert.All(results, result => Assert.Equal(1, result.Generation));
    }

    [Fact]
    public async Task CallerCancellationAfterDispatchIsUnknownWithoutDesynchronizingThePipe()
    {
        var dispatched = new DispatchSignal();
        await using var supervisor = new WorkerProcessSupervisor(
            new NamedPipeWorkerProcessFactory(_ => CreateLaunch("delay-first-execute")),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(),
            logger: dispatched,
            identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(
                "2026.1.0529.7",
                activatedSdkVersion: "2026.1.0529.7",
                connectedSpatialAnalyzerVersion: "2026.1.0529.7"));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        using var clientCancellation = new CancellationTokenSource();
        var waiting = supervisor.ExecuteAsync(
            CreateCommand("cancelled-wait"),
            clientCancellation.Token);
        // The consumer claimed the request before logging its dispatch.
        await dispatched.Dispatched.WaitAsync(ProcessBound);
        await clientCancellation.CancelAsync();
        var cancelled = await waiting;
        var next = await supervisor.ExecuteAsync(CreateCommand("after-cancellation"));

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, cancelled.Status);
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            cancelled.ExecutionDisposition);
        Assert.Equal("client-wait-cancelled", cancelled.DiagnosticCode);
        Assert.Equal(WorkerExecutionStatus.Completed, next.Status);
        Assert.Equal(1, next.Generation);
        Assert.Equal(1, supervisor.Current.Generation);
    }

    [Fact]
    public async Task CancellationBeforeEnqueueProvesExecutionDidNotStart()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        using var clientCancellation = new CancellationTokenSource();
        await clientCancellation.CancelAsync();

        var cancelled = await supervisor.ExecuteAsync(
            CreateCommand("cancelled-before-enqueue"),
            clientCancellation.Token);

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, cancelled.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, cancelled.ExecutionDisposition);
    }

    [Fact]
    public async Task ExecutionWatchdogRequiresExplicitReplacementBeforeNextCallSucceeds()
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(
                generation == 1 ? "hang-on-execute" : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(TimeSpan.FromMilliseconds(150)),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        var timingOut = supervisor.ExecuteAsync(CreateCommand("hang"));
        await clock.FireNextAsync(TimeSpan.FromMilliseconds(150));
        var timedOut = await timingOut.WaitAsync(ProcessBound);
        var blocked = await supervisor.ExecuteAsync(CreateCommand("before-recovery"));
        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var recovered = await supervisor.ExecuteAsync(CreateCommand("after-hang"));

        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, timedOut.Status);
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            timedOut.ExecutionDisposition);
        Assert.Null(timedOut.Execution);
        Assert.Equal(1, timedOut.Generation);
        Assert.Equal(WorkerExecutionStatus.Unavailable, blocked.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, blocked.ExecutionDisposition);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Forced &&
                snapshot.DiagnosticCode == "worker-execution-watchdog-timeout");
    }

    [Fact]
    public async Task WorkerCrashDuringExecutionRequiresExplicitReplacement()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(
                generation == 1 ? "crash-on-execute" : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var failed = await supervisor.ExecuteAsync(CreateCommand("crash"));
        var blocked = await supervisor.ExecuteAsync(CreateCommand("before-recovery"));
        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var recovered = await supervisor.ExecuteAsync(CreateCommand("after-crash"));

        Assert.Equal(WorkerExecutionStatus.WorkerFailure, failed.Status);
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            failed.ExecutionDisposition);
        Assert.Null(failed.Execution);
        Assert.Equal(WorkerExecutionStatus.Unavailable, blocked.Status);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Crash);
    }

    [Theory]
    [InlineData("crash-after-execute", (int)WorkerExecutionStatus.WorkerFailure)]
    [InlineData("drop-execution-response", (int)WorkerExecutionStatus.WatchdogTimeout)]
    public async Task CompletionBeforeCrashOrLostResponseRequiresReconciliation(
        string scenario,
        int expectedStatus)
    {
        var clock = new HeartbeatTestClock();
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? scenario : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(TimeSpan.FromMilliseconds(150)),
            timeProvider: clock);

        Assert.True((await StartWithinProcessBound(supervisor)).Succeeded, supervisor.Current.DiagnosticCode);
        var completing = supervisor.ExecuteAsync(CreateCommand("ambiguous-completion"));
        if (scenario == "drop-execution-response")
            await clock.FireNextAsync(TimeSpan.FromMilliseconds(150));
        var ambiguous = await completing.WaitAsync(ProcessBound);
        var blocked = await supervisor.ExecuteAsync(CreateCommand("before-recovery"));
        Assert.True((await supervisor.RecoverSdkAsync(supervisor.Current.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var recovered = await supervisor.ExecuteAsync(CreateCommand("after-ambiguous-completion"));

        Assert.Equal((WorkerExecutionStatus)expectedStatus, ambiguous.Status);
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            ambiguous.ExecutionDisposition);
        Assert.Null(ambiguous.Execution);
        Assert.Equal(1, ambiguous.Generation);
        Assert.Equal(WorkerExecutionStatus.Unavailable, blocked.Status);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(WorkerExecutionDisposition.Completed, recovered.ExecutionDisposition);
        Assert.Equal(2, recovered.Generation);
    }

    [Theory]
    [InlineData("unexpected-kind-on-execute")]
    [InlineData("mismatched-correlation-on-execute")]
    [InlineData("truncated-frame-on-execute")]
    public async Task MalformedExecutionResponseRetiresTheWorkerWithoutPipeReuse(string scenario)
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? scenario : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var failed = await supervisor.ExecuteAsync(CreateCommand("malformed-response"));
        var retired = supervisor.Current;
        var blocked = await supervisor.ExecuteAsync(CreateCommand("before-recovery"));
        Assert.True((await supervisor.RecoverSdkAsync(retired.Generation)).Succeeded,
            supervisor.Current.DiagnosticCode);
        var recovered = await supervisor.ExecuteAsync(CreateCommand("after-recovery"));

        Assert.Equal(WorkerExecutionStatus.WorkerFailure, failed.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, failed.ExecutionDisposition);
        Assert.Null(failed.Execution);
        Assert.Equal("worker-execution-control-failed", failed.DiagnosticCode);
        Assert.Equal(1, failed.Generation);
        Assert.Equal(WorkerLifecycleState.Degraded, retired.State);
        Assert.Equal(1, retired.Generation);
        Assert.Equal(WorkerTerminationKind.Forced, retired.LastTermination);
        Assert.False(retired.AdmissionOpen);
        Assert.Equal(WorkerIncidentKind.ControlChannelLost, retired.LastIncident!.Kind);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown,
            retired.LastIncident.ExecutionDisposition);
        Assert.Equal(WorkerExecutionStatus.Unavailable, blocked.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, blocked.ExecutionDisposition);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Equal(1, supervisor.Current.RecoveryCount);
    }

    [Fact]
    public async Task StartupRejectsAnImpersonatingPipeClientAndCleansUp()
    {
        var pipeRecordPath = Path.Combine(Path.GetTempPath(), $"briosa-pipe-{Guid.NewGuid():N}.txt");
        try
        {
            var factory = new StartupTrackingFactory(new NamedPipeWorkerProcessFactory(
                _ => CreateLaunch("hang-before-ready", pipeRecordPath: pipeRecordPath)));
            var supervisor = new WorkerProcessSupervisor(factory, CreatePolicy());
            await using var supervisorScope = supervisor.ConfigureAwait(true);
            var starting = supervisor.StartAsync();
            var (pipeName, launchedProcessId) = await ReadPipeRecord(pipeRecordPath);

            // A same-user process reaches the random pipe first and claims the
            // launched child's process ID in an otherwise valid ready message.
            using var impersonator = new NamedPipeClientStream(
                ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await impersonator.ConnectAsync(5_000);
            using var channel = new WorkerControlChannel(impersonator, leaveOpen: true);
            await TrySendAsync(channel, WorkerControlMessage.Ready(
                launchedProcessId,
                new WorkerConnectionSnapshot(
                    WorkerConnectionState.Disconnected,
                    WorkerExecutionReadinessState.Unverified,
                    StatusCode: null,
                    Attempt: 0,
                    MaximumAttempts: 1,
                    "sdk-started",
                    DateTimeOffset.UtcNow,
                    new WorkerRuntimeIdentitySnapshot(
                        new WorkerRuntimeIdentityEvidence(
                            Version: null,
                            WorkerRuntimeIdentityEvidenceSource.Unavailable),
                        new WorkerRuntimeIdentityEvidence(
                            Version: null,
                            WorkerRuntimeIdentityEvidenceSource.Unavailable)))));

            var result = await starting.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(result.Succeeded);
            Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
            Assert.Equal("worker-startup-failed", supervisor.Current.DiagnosticCode);
            Assert.Equal(WorkerLifecycleFailure.StartupFailed, supervisor.Current.LifecycleFailure);
            Assert.Equal(WorkerIncidentKind.StartFailed, supervisor.Current.LastIncident!.Kind);
            Assert.Null(supervisor.Current.ProcessId);
            Assert.DoesNotContain(supervisor.History, snapshot => snapshot.State == WorkerLifecycleState.Ready);
            Assert.NotNull(factory.Child);
            Assert.True(factory.Child.ExitConfirmedBeforeDisposal);
            Assert.True(await IsClosedAsync(impersonator));
        }
        finally
        {
            File.Delete(pipeRecordPath);
        }
    }

    [Fact]
    public async Task ProductionWorkerExitsWhenItsParentProcessDies()
    {
        var executable = ResolveProductionWorker();
        using var parent = Process.Start(new ProcessStartInfo(CreateLaunch("hang-before-ready").FileName)
        {
            ArgumentList = { "--scenario", "hang-before-ready", "--control-pipe", "briosa-unused" },
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("The sacrificial parent did not start.");
        var pipeName = $"briosa-parent-death-{Guid.NewGuid():N}";
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var worker = Process.Start(new ProcessStartInfo(executable)
        {
            ArgumentList =
            {
                "--disable-sdk-activation",
                "--sa-host", "sa-lab",
                "--control-pipe", pipeName,
                "--parent-process-id", parent.Id.ToString(CultureInfo.InvariantCulture)
            },
            WorkingDirectory = Path.GetDirectoryName(executable),
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("The worker did not start.");
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            var ready = await channel.ReceiveAsync(timeout.Token);
            Assert.Equal(WorkerControlMessageKind.Ready, ready.Kind);
            Assert.Equal(worker.Id, ready.ProcessId);

            // The control pipe stays open: only the parent's death can end the worker.
            parent.Kill();
            await parent.WaitForExitAsync(timeout.Token);
            await worker.WaitForExitAsync(timeout.Token);

            Assert.Equal(20, worker.ExitCode);
        }
        finally
        {
            foreach (var process in new[] { worker, parent })
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
        }
    }

    [Fact]
    public async Task RequestedOutputArgumentsRoundTripAcrossTheWorkerPipe()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var result = await supervisor.ExecuteAsync(CreateCommand("mixed-outputs"));

        Assert.Equal(WorkerExecutionStatus.Completed, result.Status);
        var outputs = result.Execution!.OutputValues;
        Assert.Equal(7, outputs.Count);
        Assert.All(outputs, output => Assert.True(output.Retrieved));
        Assert.Equal(
            1.25,
            ((Assert.Single(outputs, output => output.Name == "Planar Offset").ReadValue() as WorkerDoubleValue)?.Value));
        Assert.Equal(
            "scripted-output",
            ((Assert.Single(outputs, output => output.Name == "Working Directory").ReadValue() as WorkerTextValue)?.Value));

        var point = (Assert.Single(outputs, output => output.Name == "Point Name").ReadValue() as WorkerPointNameValue);
        Assert.Equal("Collection", point!.CollectionName);
        Assert.Equal("Group", point.GroupName);
        Assert.Equal("Point", point.TargetName);

        var vector = (Assert.Single(
            outputs,
            output => output.Name == "Component Weights").ReadValue() as WorkerVectorValue);
        Assert.Equal(new WorkerVectorValue(1, 2, 3), vector);

        var tolerance = (Assert.Single(
            outputs,
            output => output.Name == "Position Tolerance").ReadValue() as WorkerToleranceVectorOptionsValue);
        Assert.True(tolerance!.HighX.Enabled);
        Assert.Equal(1, tolerance.HighX.Value);
        Assert.False(tolerance.LowMagnitude.Enabled);
        Assert.Equal(-4, tolerance.LowMagnitude.Value);
    }

    [Fact]
    public async Task IdentityReferenceValuesRoundTripAcrossTheWorkerPipe()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var result = await supervisor.ExecuteAsync(CreateIdentityReferenceCommand());

        Assert.Equal(WorkerExecutionStatus.Completed, result.Status);
        var outputs = result.Execution!.OutputValues;
        Assert.Equal(7, outputs.Count);
        Assert.All(outputs, output => Assert.True(output.Retrieved));
        Assert.Equal(
            17,
            (outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionInstrumentId).ReadValue() as WorkerCollectionInstrumentIdValue)!.InstrumentId);
        Assert.Equal(
            WorkerItemTypeValue.Picture,
            (outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionItemName).ReadValue() as WorkerCollectionItemNameValue)!.ItemType);
        Assert.Equal(
            WorkerItemTypeValue.SaReport,
            (outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionItemNameList).ReadValue() as WorkerCollectionItemNameListValue)!.Values[0].ItemType);
        Assert.Equal(
            WorkerObjectTypeValue.PointGroup,
            (outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionObjectName).ReadValue() as WorkerCollectionObjectNameValue)!.ObjectType);
        Assert.Equal(
            "Point",
            (outputs.Single(value => value.Kind == WorkerMpValueKind.PointNameList).ReadValue() as WorkerPointNameListValue)!.Values[0].TargetName);
        Assert.Equal(
            ["A", "B"],
            (outputs.Single(value => value.Kind == WorkerMpValueKind.StringList).ReadValue() as WorkerStringListValue)!.Values);
        Assert.Equal(
            "Vector",
            (outputs.Single(value => value.Kind == WorkerMpValueKind.VectorNameList).ReadValue() as WorkerVectorNameListValue)!.Values[0].VectorName);
    }
    [Fact]
    public async Task MpFailureIsPreservedWhenExecuteStepReturnsTrue()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("mp-failure"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var result = await supervisor.ExecuteAsync(CreateCommand("mp-failure"));

        Assert.Equal(WorkerExecutionStatus.Completed, result.Status);
        Assert.True(result.Execution!.ExecuteStepReturned);
        Assert.False(result.Execution.MpSucceeded);
        Assert.Equal(3, result.Execution.MpResultCode);
        Assert.Equal("scripted-mp-failure", result.DiagnosticCode);
    }

    [Fact]
    public async Task ConnectExUnavailabilityKeepsTheSdkAvailableForReconnect()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("connect-unavailable-once"),
            CreatePolicy());

        Assert.True((await supervisor.StartAsync()).Succeeded, supervisor.Current.DiagnosticCode);
        var generation = supervisor.Current.Generation;

        Assert.False((await supervisor.ConnectAsync(generation)).Succeeded);
        Assert.Equal(WorkerLifecycleState.Ready, supervisor.Current.State);
        Assert.Equal(
            WorkerConnectionState.Faulted,
            supervisor.Current.Connection!.State);
        Assert.Equal(
            "connect-ex-unavailable",
            supervisor.Current.Connection.DiagnosticCode);

        await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(WorkerLifecycleState.Ready, supervisor.Current.State);
        Assert.Equal(generation, supervisor.Current.Generation);
        Assert.True((await supervisor.ConnectAsync(generation)).Succeeded);
        Assert.Equal(WorkerLifecycleState.Ready, supervisor.Current.State);
        Assert.Equal(
            WorkerConnectionState.Connected,
            supervisor.Current.Connection!.State);
        Assert.Equal(
            WorkerExecutionReadinessState.ExecutionReady,
            supervisor.Current.Connection.ExecutionReadinessState);
    }

    [Fact]
    public async Task ProductionWorkerCompletesControlLifecycleWithoutSpatialAnalyzer()
    {
        var executable = ResolveProductionWorker();
        await using var supervisor = CreateSupervisor(
            _ => new WorkerProcessLaunch(
                executable,
                ["--disable-sdk-activation", "--sa-host", "sa-lab"],
                workingDirectory: Path.GetDirectoryName(executable)),
            CreatePolicy(shutdownTimeout: TimeSpan.FromSeconds(5)));

        Assert.False((await supervisor.StartAsync()).Succeeded);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.Equal(WorkerConnectionState.Faulted, supervisor.Current.Connection!.State);
        Assert.Equal(
            WorkerExecutionReadinessState.Unverified,
            supervisor.Current.Connection.ExecutionReadinessState);
        Assert.Null(supervisor.Current.Connection.StatusCode);
        Assert.Equal(
            "sdk-client-activation-failed",
            supervisor.Current.Connection.DiagnosticCode);
        Assert.Equal(
            "sdk-client-activation-failed",
            supervisor.Current.LastIncident!.DiagnosticCode);

        var unavailable = await supervisor.ExecuteAsync(
            CreateCommand("sdk-unavailable"));
        Assert.Equal(WorkerExecutionStatus.Unavailable, unavailable.Status);
        Assert.Null(unavailable.Execution);
        Assert.Equal("worker-not-ready", unavailable.DiagnosticCode);
        Assert.Equal(WorkerConnectionState.Faulted, unavailable.Connection!.State);

        await supervisor.StopAsync();

        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Equal(WorkerTerminationKind.Forced, supervisor.Current.LastTermination);
    }

    // With a caller that cancels, #305 (F10) keeps the owned child alive until the
    // startup bound rather than terminating it at once; both paths end the same way.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartupOwnsAndTerminatesAChildThatNeverConnects(bool cancelCaller)
    {
        var factory = new StartupTrackingFactory(new NamedPipeWorkerProcessFactory(
            _ => CreateLaunch("hang-before-ready")));
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(factory,
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(3)), timeProvider: clock);
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        using var caller = new CancellationTokenSource();
        var starting = supervisor.StartAsync(caller.Token);
        await factory.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        if (cancelCaller)
        {
            await caller.CancelAsync();
            try
            {
                var detached = Assert.IsType<WorkerLifecycleDetached>(
                    await starting.WaitAsync(TimeSpan.FromSeconds(6)));
                Assert.Equal(WorkerLifecycleState.Starting, detached.Snapshot.State);
                Assert.False(factory.Child!.HasExited);
            }
            catch
            {
                // Lets disposal finish: the startup ends only at its virtual bound.
                clock.Advance(TimeSpan.FromHours(9));
                throw;
            }
            await clock.FireNextAsync(TimeSpan.FromMilliseconds(500));
            _ = await WaitFor(supervisor, snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.ProcessId is null);
        }
        else
        {
            await clock.FireNextAsync(TimeSpan.FromMilliseconds(500));
            Assert.False((await starting.WaitAsync(TimeSpan.FromSeconds(6))).Succeeded);
        }
        Assert.Equal("worker-startup-timeout", supervisor.Current.DiagnosticCode);
        Assert.Equal(WorkerLifecycleFailure.StartupTimeout, supervisor.Current.LifecycleFailure);
        Assert.NotNull(factory.Child);
        Assert.True(factory.Child.ExitConfirmedBeforeDisposal);
        Assert.False(supervisor.Current.ReadyForExecution);
    }

    // Completes when the supervisor logs a dispatch, which follows the consumer's claim.
    private sealed class DispatchSignal : ILogger<WorkerProcessSupervisor>
    {
        private readonly TaskCompletionSource _dispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Dispatched => _dispatched.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id == 1300) _dispatched.TrySetResult();
        }
    }

    private sealed class StartupTrackingFactory(IWorkerProcessFactory factory) : IWorkerProcessFactory
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public StartupTrackingProcess? Child { get; private set; }
        public async ValueTask<IWorkerProcess> StartAsync(int generation, CancellationToken cancellationToken = default)
        {
            Child = new StartupTrackingProcess(await factory.StartAsync(generation, cancellationToken).ConfigureAwait(false));
            Started.TrySetResult();
            return Child;
        }
    }

    private sealed class StartupTrackingProcess(IWorkerProcess inner) : IWorkerProcess
    {
        public bool ExitConfirmedBeforeDisposal { get; private set; }
        public bool HasExited => inner.HasExited;
        public int? ExitCode => inner.ExitCode;
        public ValueTask SendAsync(WorkerControlMessage message, CancellationToken cancellationToken = default) =>
            inner.SendAsync(message, cancellationToken);
        public ValueTask<WorkerControlMessage> ReceiveAsync(CancellationToken cancellationToken = default) =>
            inner.ReceiveAsync(cancellationToken);
        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => inner.WaitForExitAsync(cancellationToken);
        public ValueTask TerminateAsync(CancellationToken cancellationToken = default) => inner.TerminateAsync(cancellationToken);
        public ValueTask DisposeAsync()
        {
            ExitConfirmedBeforeDisposal = inner.HasExited;
            return inner.DisposeAsync();
        }
    }

    private static WorkerProcessSupervisor CreateSupervisor(
        Func<int, WorkerProcessLaunch> launchFactory,
        WorkerLifecyclePolicy policy,
        WorkerExecutionPolicy? executionPolicy = null,
        ExactTargetIdentityPolicy? identityPolicy = null,
        TimeProvider? timeProvider = null,
        Func<IWorkerProcessFactory, IWorkerProcessFactory>? wrapFactory = null) =>
        new(
            (wrapFactory ?? (factory => factory))(new NamedPipeWorkerProcessFactory(launchFactory)),
            policy,
            executionPolicy ?? CreateExecutionPolicy(),
            timeProvider,
            identityPolicy: identityPolicy ??
                ExactTargetIdentityPolicy.CreateForTesting(
                    "2026.1.0529.7",
                    activatedSdkVersion: "2026.1.0529.7",
                    connectedSpatialAnalyzerVersion: "2026.1.0529.7"));

    private static string ResolveProductionWorker()
    {
        var sourceWorkerOutput = Environment.GetEnvironmentVariable(
            "BRIOSA_SOURCE_WORKER_OUTPUT");
        var executable = string.IsNullOrWhiteSpace(sourceWorkerOutput)
            ? Path.Combine(
                AppContext.BaseDirectory,
                "worker-under-test",
                "Briosa.Worker.exe")
            : Path.Combine(sourceWorkerOutput, "Briosa.Worker.exe");
        Assert.True(
            File.Exists(executable),
            $"The worker executable was not found at '{executable}'.");
        return executable;
    }

    private static async Task<(string PipeName, int ProcessId)> ReadPipeRecord(string path)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!File.Exists(path))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }

        var lines = await File.ReadAllLinesAsync(path, timeout.Token);
        return (lines[0], int.Parse(lines[1], CultureInfo.InvariantCulture));
    }

    // The supervisor may already have rejected and closed the pipe.
    private static async Task TrySendAsync(WorkerControlChannel channel, WorkerControlMessage message)
    {
        try
        {
            await channel.SendAsync(message);
        }
        catch (IOException)
        {
        }
    }

    private static async Task<bool> IsClosedAsync(Stream stream)
    {
        var buffer = new byte[1];
        try
        {
            return await stream.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(5)) == 0;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private static WorkerProcessLaunch CreateLaunch(
        string scenario,
        string? lifecycleRecordPath = null,
        string? pipeRecordPath = null)
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable), $"The fake worker executable was not found at '{executable}'.");

        var arguments = new List<string>
        {
            "--scenario",
            scenario
        };
        if (lifecycleRecordPath is not null)
        {
            arguments.Add("--lifecycle-record");
            arguments.Add(lifecycleRecordPath);
        }

        if (pipeRecordPath is not null)
        {
            arguments.Add("--pipe-record");
            arguments.Add(pipeRecordPath);
        }

        return new WorkerProcessLaunch(
            executable,
            arguments,
            Path.GetDirectoryName(executable));
    }


    private static WorkerMpCommand CreateIdentityReferenceCommand() =>
        new(
            "identity-reference-pipe",
            "Identity Reference Pipe",
            [
                new WorkerMpInputArgument("Item", WorkerMpValueKind.CollectionItemName, new WorkerCollectionItemNameValue(
                        "Collection",
                        "Picture",
                        WorkerItemTypeValue.Picture)),
                new WorkerMpInputArgument("Items", WorkerMpValueKind.CollectionItemNameList, new WorkerCollectionItemNameListValue(
                        [new WorkerCollectionItemNameValue(
                            "Collection",
                            "Report",
                            WorkerItemTypeValue.SaReport)])),
                new WorkerMpInputArgument("Object", WorkerMpValueKind.CollectionObjectName, new WorkerCollectionObjectNameValue(
                        "Collection",
                        "Object",
                        WorkerObjectTypeValue.PointGroup)),
                new WorkerMpInputArgument("Points", WorkerMpValueKind.PointNameList, new WorkerPointNameListValue(
                        [new WorkerPointNameValue("Collection", "Group", "Point")])),
                new WorkerMpInputArgument("Strings", WorkerMpValueKind.StringList, new WorkerStringListValue([])),
                new WorkerMpInputArgument("Machine", WorkerMpValueKind.CollectionMachineId, new WorkerCollectionMachineIdValue("Collection", 4))
            ],
            [
                new WorkerMpOutputArgument(
                    "Instrument",
                    WorkerMpValueKind.CollectionInstrumentId),
                new WorkerMpOutputArgument(
                    "Item",
                    WorkerMpValueKind.CollectionItemName),
                new WorkerMpOutputArgument(
                    "Items",
                    WorkerMpValueKind.CollectionItemNameList),
                new WorkerMpOutputArgument(
                    "Object",
                    WorkerMpValueKind.CollectionObjectName),
                new WorkerMpOutputArgument(
                    "Points",
                    WorkerMpValueKind.PointNameList),
                new WorkerMpOutputArgument(
                    "Strings",
                    WorkerMpValueKind.StringList),
                new WorkerMpOutputArgument(
                    "Vectors",
                    WorkerMpValueKind.VectorNameList)
            ]);
    private static WorkerMpCommand CreateCommand(string operationId) =>
        new(
            operationId,
            "Scripted Step",
            [
                new WorkerMpInputArgument("Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                new WorkerMpInputArgument("Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(2)),
                new WorkerMpInputArgument("Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.01)),
                new WorkerMpInputArgument("Label", WorkerMpValueKind.Text, new WorkerTextValue("portable-test")),
                new WorkerMpInputArgument("Point Name", WorkerMpValueKind.PointName, new WorkerPointNameValue("", "", "")),
                new WorkerMpInputArgument("Direction", WorkerMpValueKind.Vector, new WorkerVectorValue(1, 0, 0)),
                new WorkerMpInputArgument("Position Tolerance", WorkerMpValueKind.ToleranceVectorOptions, CreateToleranceVectorOptions())
            ],
            [
                new WorkerMpOutputArgument("Enabled Result", WorkerMpValueKind.Logical),
                new WorkerMpOutputArgument("Count Result", WorkerMpValueKind.WholeNumber),
                new WorkerMpOutputArgument("Planar Offset", WorkerMpValueKind.FloatingPoint),
                new WorkerMpOutputArgument("Working Directory", WorkerMpValueKind.Text),
                new WorkerMpOutputArgument("Point Name", WorkerMpValueKind.PointName),
                new WorkerMpOutputArgument("Component Weights", WorkerMpValueKind.Vector),
                new WorkerMpOutputArgument(
                    "Position Tolerance",
                    WorkerMpValueKind.ToleranceVectorOptions)
            ]);

    private static WorkerToleranceVectorOptionsValue CreateToleranceVectorOptions() =>
        new(
            new WorkerToleranceLimit(Enabled: true, Value: 1),
            new WorkerToleranceLimit(Enabled: true, Value: 2),
            new WorkerToleranceLimit(Enabled: true, Value: 3),
            new WorkerToleranceLimit(Enabled: true, Value: 4),
            new WorkerToleranceLimit(Enabled: false, Value: -1),
            new WorkerToleranceLimit(Enabled: false, Value: -2),
            new WorkerToleranceLimit(Enabled: false, Value: -3),
            new WorkerToleranceLimit(Enabled: false, Value: -4));

    private static WorkerExecutionPolicy CreateExecutionPolicy(
        TimeSpan? watchdogTimeout = null,
        int queueCapacity = 16) =>
        new(
            watchdogTimeout ?? TimeSpan.FromSeconds(2),
            queueCapacity,
            durationClassOf: TestDurationClasses.ReviewedOrSyntheticQuick);

    // Virtual-time tests fire server-side deadlines explicitly, but a real fake
    // worker process must still start and exit within a generous real bound.
    private static readonly TimeSpan ProcessBound = TimeSpan.FromSeconds(30);
    // Process-free tests order steps with handshakes; this real bound is only a hang guard.
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromMilliseconds(250);

    private static Task<WorkerLifecycleResult> StartWithinProcessBound(WorkerProcessSupervisor supervisor) =>
        supervisor.StartAsync().WaitAsync(ProcessBound);

    private static WorkerLifecyclePolicy CreatePolicy(
        TimeSpan? heartbeatInterval = null,
        TimeSpan? shutdownTimeout = null,
        int lifecycleHistoryCapacity = 256,
        TimeSpan? readinessProbeTimeout = null) =>
        new(
            heartbeatInterval ?? HeartbeatInterval,
            heartbeatTimeout: HeartbeatTimeout,
            startupTimeout: TimeSpan.FromSeconds(5),
            shutdownTimeout ?? TimeSpan.FromMilliseconds(500),
            lifecycleHistoryCapacity,
            readinessProbeTimeout);

    private static async Task<WorkerExecutionSnapshot> WaitForExecution(
        WorkerProcessSupervisor supervisor,
        Func<WorkerExecutionSnapshot, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var snapshot = supervisor.ExecutionSnapshot;
            if (predicate(snapshot))
            {
                return snapshot;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

    private static async Task<WorkerLifecycleSnapshot> WaitFor(
        WorkerProcessSupervisor supervisor,
        Func<WorkerLifecycleSnapshot, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var snapshot = supervisor.Current;
            if (predicate(snapshot))
            {
                return snapshot;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    private sealed class FixedWorkerProcessFactory(IWorkerProcess process) : IWorkerProcessFactory
    {
        public ValueTask<IWorkerProcess> StartAsync(
            int generation,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(process);
    }

    private sealed class CoordinatedHeartbeatProcess : IWorkerProcess
    {
        private readonly WorkerConnectionSnapshot _connection = new(
            WorkerConnectionState.Disconnected,
            WorkerExecutionReadinessState.Unverified,
            StatusCode: null,
            Attempt: 1,
            MaximumAttempts: 1,
            "sdk-started",
            DateTimeOffset.UtcNow,
            new WorkerRuntimeIdentitySnapshot(
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable),
                new WorkerRuntimeIdentityEvidence(
                    Version: null,
                    WorkerRuntimeIdentityEvidenceSource.Unavailable)));
        private readonly TaskCompletionSource _exit = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _heartbeatRelease = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _pingStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Channel<WorkerControlMessage> _responses =
            Channel.CreateUnbounded<WorkerControlMessage>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
        private int _exited;
        private int _pingWasCancelled;
        private int _poisoned;

        public CoordinatedHeartbeatProcess()
        {
            Assert.True(_responses.Writer.TryWrite(
                WorkerControlMessage.Ready(
                    processId: 1234,
                    _connection)));
        }

        public bool HasExited => Volatile.Read(ref _exited) != 0;

        public int? ExitCode => HasExited ? 0 : null;

        public Task PingStarted => _pingStarted.Task;

        public bool PingWasCancelled => Volatile.Read(ref _pingWasCancelled) != 0;

        public async ValueTask SendAsync(
            WorkerControlMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message.Kind == WorkerControlMessageKind.Ping)
            {
                _pingStarted.TrySetResult();
                try
                {
                    await _heartbeatRelease.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Interlocked.Exchange(ref _pingWasCancelled, 1);
                    Interlocked.Exchange(ref _poisoned, 1);
                    throw;
                }

                Assert.True(_responses.Writer.TryWrite(
                    WorkerControlMessage.Pong(message.CorrelationId, _connection)));
                return;
            }

            if (message.Kind == WorkerControlMessageKind.Stop)
            {
                if (Volatile.Read(ref _poisoned) != 0)
                {
                    throw new IOException("The cancelled heartbeat poisoned the control channel.");
                }

                Assert.True(_responses.Writer.TryWrite(
                    WorkerControlMessage.Stopped(message.CorrelationId)));
                MarkExited();
                return;
            }

            throw new InvalidDataException($"Unexpected message kind {message.Kind}.");
        }

        public ValueTask<WorkerControlMessage> ReceiveAsync(
            CancellationToken cancellationToken = default) =>
            _responses.Reader.ReadAsync(cancellationToken);

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            _exit.Task.WaitAsync(cancellationToken);

        public ValueTask TerminateAsync(CancellationToken cancellationToken = default)
        {
            MarkExited();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            MarkExited();
            return ValueTask.CompletedTask;
        }

        public void ReleaseHeartbeat() => _heartbeatRelease.TrySetResult();

        private void MarkExited()
        {
            Interlocked.Exchange(ref _exited, 1);
            _exit.TrySetResult();
        }
    }

}

[CollectionDefinition("Worker process lifecycle", DisableParallelization = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "xUnit requires collection definition classes to be public.")]
public sealed class WorkerProcessLifecycleGroup;
