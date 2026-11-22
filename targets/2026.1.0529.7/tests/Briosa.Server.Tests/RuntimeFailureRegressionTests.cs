using System.Diagnostics.Metrics;
using Briosa.Server.Operations;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Tests;

public sealed class RuntimeFailureRegressionTests
{
    [Fact]
    public async Task QueuedStopCannotReplaceTheCompletedStartResultSnapshot()
    {
        var worker = new CoordinatedWorker { HoldStartup = true };
        await using var workerScope = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        var starting = supervisor.StartAsync();
        await worker.StartupEntered.Task.WaitAsync(HangGuard);
        var stopping = supervisor.StopAsync();
        Assert.False(stopping.IsCompleted);
        worker.ReleaseStartup.TrySetResult();
        await Task.WhenAll(starting, stopping).WaitAsync(HangGuard);

        var started = Assert.IsType<WorkerLifecycleSucceeded>(await starting.ConfigureAwait(true));
        var stopped = Assert.IsType<WorkerLifecycleSucceeded>(await stopping.ConfigureAwait(true));
        Assert.Equal(WorkerLifecycleState.Ready, started.Snapshot.State);
        Assert.True(started.Snapshot.ReadyForExecution);
        Assert.Equal(WorkerLifecycleState.Stopped, stopped.Snapshot.State);
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Equal(started.Snapshot.Generation, stopped.Snapshot.Generation);
        Assert.True(started.Snapshot.StateRevision < stopped.Snapshot.StateRevision);
    }

    [Fact]
    public async Task QueuedCleanupCannotEraseTheStartupTimeoutResult()
    {
        var worker = new CoordinatedWorker { HoldStartup = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(1)), TestDurationClasses.SyntheticQuickPolicy(), clock);
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        var starting = supervisor.StartAsync();
        await worker.StartupEntered.Task.WaitAsync(HangGuard);
        var stopping = supervisor.StopAsync();
        await clock.FireNextAsync(TimeSpan.FromMilliseconds(50));
        await Task.WhenAll(starting, stopping).WaitAsync(HangGuard);

        var failed = Assert.IsType<WorkerLifecycleFailed>(await starting.ConfigureAwait(true));
        var stopped = Assert.IsType<WorkerLifecycleSucceeded>(await stopping.ConfigureAwait(true));
        Assert.Equal(WorkerLifecycleState.Degraded, failed.Snapshot.State);
        Assert.Equal(WorkerLifecycleFailure.StartupTimeout, failed.Snapshot.LifecycleFailure);
        Assert.True(failed.Snapshot.LifecycleTimedOut);
        Assert.Equal(WorkerLifecycleState.Stopped, stopped.Snapshot.State);
        Assert.Equal(WorkerLifecycleFailure.None, supervisor.Current.LifecycleFailure);
        Assert.Equal(WorkerTerminationKind.Forced, stopped.Snapshot.LastTermination);
        Assert.Equal(WorkerIncidentKind.StartFailed, stopped.Snapshot.LastIncident!.Kind);
        Assert.True(failed.Snapshot.StateRevision < stopped.Snapshot.StateRevision);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task NonFinitePublicInputIsRejectedWithoutLosingTheWorker(double value)
    {
        var worker = new CoordinatedWorker();
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);

        var executor = new OperationExecutor(supervisor,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System);
        using var timeout = new CancellationTokenSource(HangGuard);

        var error = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
            new global::Briosa.SetDoubleVariableRequest { Name = "regression", Value = value },
            SetDoubleVariableOperation.Descriptor, SetDoubleVariableOperation.CreateCommand, SetDoubleVariableOperation.OutputContracts,
            SetDoubleVariableOperation.CreateResult, timeout.Token));

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
        var detail = global::Briosa.OperationError.Parser.ParseFrom(Assert.Single(error.Trailers).ValueBytes);
        Assert.Equal(global::Briosa.ExecutionDisposition.NotStarted, detail.ExecutionDisposition);
        Assert.Equal(0, worker.ExecuteCount);
        Assert.True(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain(), timeout.Token)).Status);
        Assert.Equal(supervisor.ExecutionSnapshot.AdmittedRequests, supervisor.ExecutionSnapshot.TerminalRequests);
    }

    [Fact]
    public async Task OversizedPublicInputDoesNotRetireAHealthyWorker()
    {
        var worker = new CoordinatedWorker();
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var executor = new OperationExecutor(supervisor,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System);

        var error = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
            new global::Briosa.SetStringVariableRequest { Name = "regression", Value = new string('x', 70_000) },
            SetStringVariableOperation.Descriptor, SetStringVariableOperation.CreateCommand, SetStringVariableOperation.OutputContracts,
            SetStringVariableOperation.CreateResult, CancellationToken.None));

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
        var detail = global::Briosa.OperationError.Parser.ParseFrom(Assert.Single(error.Trailers).ValueBytes);
        Assert.Equal(global::Briosa.ExecutionDisposition.NotStarted, detail.ExecutionDisposition);
        Assert.Equal(0, worker.ExecuteCount);
        Assert.False(worker.HasExited);
        Assert.True(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
    }

    [Fact]
    public async Task FullQueueRejectsPublicCallsBeforeMappingWithTypedNotStartedEvidence()
    {
        var worker = new CoordinatedWorker { HoldExecution = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var active = supervisor.ExecuteAsync(Plain());
        await worker.ExecutionEntered.Task.WaitAsync(HangGuard);
        var first = supervisor.ExecuteAsync(Plain());
        var second = supervisor.ExecuteAsync(Plain());

        var executor = new OperationExecutor(supervisor,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System);
        var mapped = false;

        try
        {
            for (var index = 0; index < 64; index++)
            {
                var error = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
                    new global::Briosa.SetDoubleVariableRequest(), SetDoubleVariableOperation.Descriptor,
                    request => { mapped = true; return SetDoubleVariableOperation.CreateCommand(request); },
                    SetDoubleVariableOperation.OutputContracts, SetDoubleVariableOperation.CreateResult,
                    CancellationToken.None));
                Assert.Equal(StatusCode.ResourceExhausted, error.StatusCode);
                var detail = global::Briosa.OperationError.Parser.ParseFrom(Assert.Single(error.Trailers).ValueBytes);
                Assert.Equal(global::Briosa.OperationFailureKind.Overloaded, detail.Kind);
                Assert.Equal(global::Briosa.ExecutionDisposition.NotStarted, detail.ExecutionDisposition);
                Assert.Equal(global::Briosa.ReplayGuidance.MayReplay, detail.ReplayGuidance);
            }
            Assert.False(mapped);
            Assert.Equal(0, supervisor.ExecutionSnapshot.WaitingForAdmission);
            Assert.Equal(2, supervisor.ExecutionSnapshot.QueuedRequests);
            Assert.Equal(3, supervisor.ExecutionSnapshot.AdmittedRequests);
        }
        finally
        {
            worker.ReleaseExecution.TrySetResult();
        }
        Assert.All(await Task.WhenAll(active, first, second),
            outcome => Assert.Equal(WorkerExecutionStatus.Completed, outcome.Status));
    }

    [Fact]
    public async Task MappingFailureReleasesAdmissionReservation()
    {
        var worker = new CoordinatedWorker();
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        for (var index = 0; index < 4; index++)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => supervisor.ExecuteAsync(
                new WorkerCommandSubmission("regression.plain", () => throw new ArgumentException("invalid")),
                Guid.NewGuid()));
        }
        Assert.Equal(0, supervisor.ExecutionSnapshot.AdmittedRequests);
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
    }

    // #305 (F10) deliberately replaced the earlier rule that a cancelled startup kills
    // its child: an accepted Start finishes under the startup bound instead.
    [Fact]
    public async Task CancellingAnAcceptedStartupLetsTheChildFinishStarting()
    {
        var worker = new CoordinatedWorker { HoldStartup = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        using var caller = new CancellationTokenSource();
        var starting = supervisor.StartAsync(caller.Token);
        await worker.StartupEntered.Task.WaitAsync(HangGuard);
        try
        {
            await caller.CancelAsync();
            var detached = Assert.IsType<WorkerLifecycleDetached>(await starting.WaitAsync(HangGuard));
            Assert.Equal(WorkerLifecycleState.Starting, detached.Snapshot.State);
            Assert.False(worker.HasExited);
            Assert.False(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
        }
        finally
        {
            // Lets disposal finish even when an assertion fails: the virtual startup
            // bound never expires on its own.
            worker.ReleaseStartup.TrySetResult();
        }

        using var timeout = new CancellationTokenSource(HangGuard);
        while (!WorkerReadinessHealthCheck.IsReady(supervisor.Current))
            await Task.Delay(TimeSpan.FromMilliseconds(5), timeout.Token);
        Assert.False(worker.HasExited);
        Assert.Equal(0, worker.TerminationCount);
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
    }

    [Fact]
    public async Task CancellingStopBeforeItOwnsTheTransitionLeavesAdmissionUsable()
    {
        var worker = new CoordinatedWorker { HoldExecution = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var executing = supervisor.ExecuteAsync(Plain());
        await worker.ExecutionEntered.Task.WaitAsync(HangGuard);
        using var caller = new CancellationTokenSource();
        var stopping = supervisor.StopAsync(caller.Token);
        await caller.CancelAsync();
        worker.ReleaseExecution.TrySetResult();
        await executing.ConfigureAwait(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopping);

        Assert.True(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
    }

    [Fact]
    public async Task RejectedRecoveryLeavesTheHealthyGenerationUsable()
    {
        var worker = new CoordinatedWorker();
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        await Assert.ThrowsAsync<WorkerGenerationConflictException>(() => supervisor.RecoverSdkAsync(2));
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => supervisor.RecoverSdkAsync(1));
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
        Assert.True(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
        Assert.Equal(0, supervisor.Current.RecoveryCount);
    }

    [Fact]
    public async Task ConcurrentStopsShareOneOrderlyTeardown()
    {
        var worker = new CoordinatedWorker { HoldStop = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var first = supervisor.StopAsync();
        await worker.StopEntered.Task.WaitAsync(HangGuard);
        var second = supervisor.StopAsync();
        Assert.False(second.IsCompleted);
        worker.ReleaseStop.TrySetResult();
        await Task.WhenAll(first, second);
        Assert.True(worker.HasExited);
        Assert.Equal(1, worker.StopCount);
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
    }

    [Fact]
    public async Task CancellingAnAcceptedStopStillFinishesCleanup()
    {
        var worker = new CoordinatedWorker { HoldStop = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        using var caller = new CancellationTokenSource();
        var stopping = supervisor.StopAsync(caller.Token);
        await worker.StopEntered.Task.WaitAsync(HangGuard);

        Assert.Equal(WorkerLifecycleState.Stopping, supervisor.Current.State);
        Assert.False(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
        await caller.CancelAsync();
        worker.ReleaseStop.TrySetResult();
        await stopping.ConfigureAwait(true);

        Assert.True(worker.HasExited);
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
    }

    [Fact]
    public async Task UndefinedWorkerStatusCannotClaimTheCommandDidNotStart()
    {
        var worker = new CoordinatedWorker { UndefinedStatus = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);

        var result = await supervisor.ExecuteAsync(Plain());

        Assert.Equal(WorkerExecutionStatus.WorkerFailure, result.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, result.ExecutionDisposition);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.True(worker.HasExited);
    }

    [Fact]
    public async Task UnexpectedConsumerFailureResolvesActiveAndQueuedCallsAndClosesAdmission()
    {
        var worker = new CoordinatedWorker { HoldExecution = true, UnexpectedFailure = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var active = supervisor.ExecuteAsync(Plain());
        await worker.ExecutionEntered.Task.WaitAsync(HangGuard);
        var queued = supervisor.ExecuteAsync(Plain());
        worker.ReleaseExecution.TrySetResult();

        var result = await active.WaitAsync(HangGuard);
        var pending = await queued.WaitAsync(HangGuard);
        Assert.Equal(WorkerExecutionStatus.WorkerFailure, result.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, result.ExecutionDisposition);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, pending.ExecutionDisposition);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.False(supervisor.Current.AdmissionOpen);
        Assert.False(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
        Assert.True(worker.HasExited);
        Assert.Equal(1, worker.ExecuteCount);
        Assert.Equal(2, supervisor.ExecutionSnapshot.TerminalRequests);
        Assert.Equal(0, supervisor.ExecutionSnapshot.QueuedRequests);
        Assert.Equal(WorkerExecutionStatus.Unavailable, (await supervisor.ExecuteAsync(Plain())).Status);
    }

    // #405: once a consumer failure closes the queue, dispatch rejects. Health, discovery,
    // status and the readiness gauge agree from that moment, also while retirement waits
    // for the gate.
    [Fact]
    public async Task ConsumerFailureReportsNotReadyFromTheMomentAdmissionCloses()
    {
        var worker = new CoordinatedWorker { HoldExecution = true, UnexpectedFailure = true };
        var replacement = new CoordinatedWorker { ReadyDisconnected = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var replacementLifetime = replacement.ConfigureAwait(true);
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var telemetry = new BriosaTelemetry(new BriosaLogHealth());
        // Other tests' telemetry shares the meter name, so only this instance's gauge counts.
        using var listener = new MeterListener();
        int? readyGauge = null;
        listener.InstrumentPublished = (instrument, observer) =>
        {
            if (ReferenceEquals(instrument.Meter, telemetry.Meter) && instrument.Name == "briosa.ready")
                observer.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<int>((_, value, _, _) => readyGauge = value);
        listener.Start();
        int? MeasureReadyGauge()
        {
            readyGauge = null;
            listener.RecordObservableInstruments();
            return readyGauge;
        }

        // Virtual time, and the seam holds retirement after the queue closes and before
        // it waits for the gate: no deadline or scheduling delay decides the outcome.
        var supervisor = new WorkerProcessSupervisor(new SequenceFactory(worker, replacement),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
            TestDurationClasses.SyntheticQuickPolicy(TimeSpan.FromSeconds(5), 2), new HeartbeatTestClock(),
            telemetry: telemetry)
        {
            ConsumerFailureRetirementHold = () =>
            {
                held.TrySetResult();
                return release.Task;
            }
        };
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        // Telemetry is attached only once the initial snapshot exists, so sampling before
        // the first transition reads not-ready instead of failing.
        Assert.Equal(0, MeasureReadyGauge());
        var health = new WorkerReadinessHealthCheck(supervisor);
        var discovery = new ServerDiscoveryService(supervisor, new BuildIdentityProvider(),
            OperationPolicyTests.CreatePolicy());
        var status = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var ready = supervisor.Current;
        Assert.True(ready.ReadyForExecution);
        Assert.True(discovery.CreateServerInfo().ReadyForMp);
        Assert.Equal(1, MeasureReadyGauge());

        Task<WorkerExecutionOutcome> active;
        Task<WorkerExecutionOutcome> queued;
        WorkerLifecycleSnapshot closed;
        // Both holds are released on every path, so a failed wait cannot leave the
        // consumer parked in the exchange or the seam while the supervisor is disposed.
        try
        {
            active = supervisor.ExecuteAsync(Plain());
            await worker.ExecutionEntered.Task.WaitAsync(HangGuard);
            queued = supervisor.ExecuteAsync(Plain());
            worker.ReleaseExecution.TrySetResult();
            await held.Task.WaitAsync(HangGuard);

            // Retirement has not run: the generation and its worker are still live.
            closed = supervisor.Current;
            Assert.Equal(ready.Generation, closed.Generation);
            Assert.Equal(WorkerLifecycleState.Ready, closed.State);
            Assert.False(worker.HasExited);
            Assert.False(active.IsCompleted);
            Assert.False(queued.IsCompleted);

            Assert.True(closed.StateRevision > ready.StateRevision);
            Assert.False(closed.AdmissionOpen);
            Assert.False(closed.ReadyForExecution);
            Assert.Equal("worker-execution-consumer-failed", closed.DiagnosticCode);
            Assert.Equal(HealthStatus.Unhealthy,
                (await health.CheckHealthAsync(new HealthCheckContext())).Status);
            Assert.False(discovery.CreateServerInfo().ReadyForMp);
            var reported = status.Current;
            Assert.False(reported.ReadyForMp);
            Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Running, reported.SdkState);
            Assert.Equal((ulong)closed.StateRevision, reported.StateRevision);
            Assert.Equal(0, MeasureReadyGauge());
            var rejected = await supervisor.ExecuteAsync(Plain()).WaitAsync(HangGuard);
            Assert.Equal(WorkerExecutionStatus.Unavailable, rejected.Status);
            Assert.Equal(WorkerExecutionDisposition.NotStarted, rejected.ExecutionDisposition);
            Assert.Equal("worker-admission-closed", rejected.DiagnosticCode);
            Assert.Equal(1, worker.ExecuteCount);
        }
        finally
        {
            worker.ReleaseExecution.TrySetResult();
            release.TrySetResult();
        }

        // Cleanup finishes: the generation stays faulted with admission closed, records
        // the incident and the disposition, and waits for explicit recovery.
        var failed = await active.WaitAsync(HangGuard);
        var pending = await queued.WaitAsync(HangGuard);
        Assert.Equal(WorkerExecutionStatus.WorkerFailure, failed.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, failed.ExecutionDisposition);
        Assert.Equal(WorkerExecutionStatus.Unavailable, pending.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, pending.ExecutionDisposition);
        var faulted = supervisor.Current;
        Assert.Equal(ready.Generation, faulted.Generation);
        Assert.Equal(WorkerLifecycleState.Degraded, faulted.State);
        Assert.False(faulted.AdmissionOpen);
        Assert.False(faulted.ReadyForExecution);
        Assert.Equal("worker-execution-consumer-failed", faulted.DiagnosticCode);
        Assert.Equal(WorkerConnectionState.Faulted, faulted.Connection!.State);
        Assert.Equal(ready.Generation, faulted.LastIncident!.Generation);
        Assert.Equal("worker-execution-consumer-failed", faulted.LastIncident.DiagnosticCode);
        Assert.Equal("regression.plain", faulted.LastIncident.OperationId);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, faulted.LastIncident.ExecutionDisposition);
        Assert.True(worker.HasExited);
        Assert.Equal(HealthStatus.Unhealthy,
            (await health.CheckHealthAsync(new HealthCheckContext())).Status);
        Assert.False(discovery.CreateServerInfo().ReadyForMp);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Faulted, status.Current.SdkState);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            status.Current.RecoveryState);
        Assert.Equal(0, MeasureReadyGauge());
        Assert.Equal(WorkerExecutionStatus.Unavailable, (await supervisor.ExecuteAsync(Plain())).Status);

        // No snapshot of the failed generation reports open admission after the queue closed.
        Assert.All(supervisor.History.Where(snapshot => snapshot.Generation == ready.Generation &&
            snapshot.StateRevision >= closed.StateRevision), snapshot => Assert.False(snapshot.AdmissionOpen));

        // Explicit recovery replaces the generation and never replays the interrupted call.
        Assert.True((await supervisor.RecoverSdkAsync(ready.Generation)).Succeeded);
        Assert.Equal(ready.Generation + 1, supervisor.Current.Generation);
        Assert.Equal(WorkerLifecycleState.Ready, supervisor.Current.State);
        Assert.Equal(1, worker.ExecuteCount);
        Assert.Equal(0, replacement.ExecuteCount);
    }

    // #405 review: a logger that fails on the consumer failure's transition cannot stop
    // the cleanup or the consumer's completion that follow it. The worker is terminated
    // and explicit recovery succeeds.
    [Fact]
    public async Task AFailingTransitionLogCannotPreventRetirementOrRecovery()
    {
        var logger = new ConsumerFailureTransitionThrowingLogger();
        await AssertConsumerFailureRetiresAndRecoversAsync(new HeartbeatTestClock(), logger, arm: () => { });
        Assert.True(logger.Failures > 0, "The logger never failed, so the test proved nothing.");
    }

    // #405 review: a clock that fails once, at the closure timestamp (read 1) or at either
    // retirement timestamp (reads 2 and 3), cannot strand the active call or skip cleanup.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task AFailingClockReadCannotStrandAConsumerFailure(int failingRead)
    {
        var clock = new FailingUtcClock(new HeartbeatTestClock());
        await AssertConsumerFailureRetiresAndRecoversAsync(clock, logger: null,
            arm: () => clock.FailUtcRead(failingRead));
        Assert.True(clock.Failed, "The clock never failed, so the test proved nothing.");
    }

    // #405 review: a failing exchange measurement unwinds an exchange whose MP work has
    // completed. The consumer still releases the gate, so retirement runs, and the caller
    // keeps the completed outcome.
    [Fact]
    public async Task AFailingExchangeMeasurementCannotKeepTheGateFromRetirement()
    {
        var worker = new CoordinatedWorker();
        var replacement = new CoordinatedWorker { ReadyDisconnected = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var replacementLifetime = replacement.ConfigureAwait(true);
        using var telemetry = new BriosaTelemetry(new BriosaLogHealth());
        using var listener = new MeterListener();
        var measurements = 0;
        listener.InstrumentPublished = (instrument, observer) =>
        {
            if (ReferenceEquals(instrument.Meter, telemetry.Meter) &&
                instrument.Name == "briosa.worker.exchange.duration")
                observer.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, _, _, _) =>
        {
            if (Interlocked.Increment(ref measurements) == 1)
                throw new InvalidOperationException("Scripted measurement failure.");
        });
        listener.Start();
        var supervisor = new WorkerProcessSupervisor(new SequenceFactory(worker, replacement),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
            TestDurationClasses.SyntheticQuickPolicy(TimeSpan.FromSeconds(5), 2), new HeartbeatTestClock(),
            telemetry: telemetry);
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var generation = supervisor.Current.Generation;

        var outcome = await supervisor.ExecuteAsync(Plain()).WaitAsync(HangGuard);

        Assert.Equal(1, Volatile.Read(ref measurements));
        Assert.Equal(WorkerExecutionStatus.Completed, outcome.Status);
        Assert.Equal(WorkerExecutionDisposition.Completed, outcome.ExecutionDisposition);
        var faulted = supervisor.Current;
        Assert.Equal(WorkerLifecycleState.Degraded, faulted.State);
        Assert.Equal("worker-execution-consumer-failed", faulted.DiagnosticCode);
        Assert.Equal(WorkerExecutionDisposition.Completed, faulted.LastIncident!.ExecutionDisposition);
        Assert.Equal(1, worker.TerminationCount);
        Assert.True((await supervisor.RecoverSdkAsync(generation)).Succeeded);
        Assert.Equal(generation + 1, supervisor.Current.Generation);
        Assert.Equal(1, worker.ExecuteCount);
        Assert.Equal(0, replacement.ExecuteCount);
    }

    // Fails the consumer with one active and one queued call, arming a fault just before
    // the failure, then asserts the documented recovery state: both calls resolved,
    // admission closed before retirement and never reopened, the worker terminated, and
    // explicit recovery without replay.
    private static async Task AssertConsumerFailureRetiresAndRecoversAsync(
        TimeProvider clock, ILogger<WorkerProcessSupervisor>? logger, Action arm)
    {
        var worker = new CoordinatedWorker { HoldExecution = true, UnexpectedFailure = true };
        var replacement = new CoordinatedWorker { ReadyDisconnected = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var replacementLifetime = replacement.ConfigureAwait(true);
        var supervisor = new WorkerProcessSupervisor(new SequenceFactory(worker, replacement),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
            TestDurationClasses.SyntheticQuickPolicy(TimeSpan.FromSeconds(5), 2), clock, logger);
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync().ConfigureAwait(true)).Succeeded);
        var generation = supervisor.Current.Generation;
        Task<WorkerExecutionOutcome> active;
        Task<WorkerExecutionOutcome> queued;
        try
        {
            active = supervisor.ExecuteAsync(Plain());
            await worker.ExecutionEntered.Task.WaitAsync(HangGuard).ConfigureAwait(true);
            queued = supervisor.ExecuteAsync(Plain());
            arm();
        }
        finally
        {
            worker.ReleaseExecution.TrySetResult();
        }

        var failed = await active.WaitAsync(HangGuard).ConfigureAwait(true);
        var pending = await queued.WaitAsync(HangGuard).ConfigureAwait(true);
        Assert.Equal(WorkerExecutionStatus.WorkerFailure, failed.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, failed.ExecutionDisposition);
        Assert.Equal(WorkerExecutionStatus.Unavailable, pending.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, pending.ExecutionDisposition);
        var faulted = supervisor.Current;
        Assert.Equal(generation, faulted.Generation);
        Assert.Equal(WorkerLifecycleState.Degraded, faulted.State);
        Assert.False(faulted.AdmissionOpen);
        Assert.Equal("worker-execution-consumer-failed", faulted.DiagnosticCode);
        Assert.Equal(global::Briosa.SpatialAnalyzerSdkRecoveryState.RecoveryAvailable,
            new SpatialAnalyzerSdkLifecycleStateProjection(supervisor).Current.RecoveryState);
        Assert.Equal(1, worker.TerminationCount);
        Assert.True(worker.HasExited);
        var history = supervisor.History.Where(snapshot => snapshot.Generation == generation).ToList();
        var closedAt = history.FindIndex(snapshot => snapshot is
        {
            State: WorkerLifecycleState.Ready, AdmissionOpen: false,
            DiagnosticCode: "worker-execution-consumer-failed"
        });
        Assert.True(closedAt >= 0, "Admission did not close before retirement.");
        Assert.All(history.Skip(closedAt), snapshot => Assert.False(snapshot.AdmissionOpen));

        Assert.True((await supervisor.RecoverSdkAsync(generation).ConfigureAwait(true)).Succeeded);
        Assert.Equal(generation + 1, supervisor.Current.Generation);
        Assert.Equal(1, worker.ExecuteCount);
        Assert.Equal(0, replacement.ExecuteCount);
    }

    [Fact]
    public async Task AReservedMappingCannotEnterASuccessorGeneration()
    {
        var first = new CoordinatedWorker();
        var second = new CoordinatedWorker();
        await using var firstLifetime = first.ConfigureAwait(true);
        await using var secondLifetime = second.ConfigureAwait(true);
        // Virtual time: no lifecycle or watchdog deadline can expire while the mapping is held.
        var supervisor = new WorkerProcessSupervisor(new SequenceFactory(first, second),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
            TestDurationClasses.SyntheticQuickPolicy(TimeSpan.FromSeconds(5), 2), new HeartbeatTestClock());
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var mappingEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseMapping = new ManualResetEventSlim();
        // Mapping is synchronous, so the callback holds its reservation by blocking until
        // the test has replaced the generation. The bound only turns a hang into a failure.
        var submission = Task.Run(() => supervisor.ExecuteAsync(new WorkerCommandSubmission(
            "regression.plain", () =>
            {
                mappingEntered.TrySetResult();
                if (!releaseMapping.Wait(HangGuard)) throw new TimeoutException();
                return Plain();
            }), Guid.NewGuid()));
        await mappingEntered.Task.WaitAsync(HangGuard);
        try
        {
            await supervisor.StopAsync();
            Assert.True((await supervisor.StartAsync()).Succeeded);
            Assert.Equal(2, supervisor.Current.Generation);
        }
        finally
        {
            releaseMapping.Set();
        }

        var result = await submission.WaitAsync(HangGuard);
        Assert.Equal(WorkerExecutionStatus.Unavailable, result.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, result.ExecutionDisposition);
        Assert.Equal(0, second.ExecuteCount);
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
        Assert.Equal(1, second.ExecuteCount);
    }

    [Fact]
    public async Task SnapshotsOwnApplicationAssociationAndReadinessAcrossAllProjections()
    {
        var worker = new CoordinatedWorker { ReadyDisconnected = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        var firstRead = projection.Current;
        Assert.Equal((ulong)supervisor.Current.StateRevision, firstRead.StateRevision);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        // Connect publishes readiness and its application association in one snapshot.
        Assert.True((await supervisor.ConnectAsync(1, new LifecycleAcceptance(CancellationToken.None),
            _ => Task.FromResult<int?>(7))).Succeeded);
        var snapshot = supervisor.Current;
        Assert.Equal(7, snapshot.ApplicationGeneration);
        Assert.True(snapshot.AdmissionOpen);
        Assert.Equal(snapshot.ReadyForExecution, projection.Current.ReadyForMp);
        Assert.Equal(snapshot.ReadyForExecution, WorkerReadinessHealthCheck.IsReady(snapshot));
        Assert.Equal((ulong)snapshot.StateRevision, projection.Current.StateRevision);
        Assert.Equal(projection.Current, new SpatialAnalyzerSdkLifecycleStateProjection(supervisor).Current);
        Assert.Equal(snapshot, supervisor.Current);
        Assert.Contains(supervisor.History, state => state.State == WorkerLifecycleState.Ready &&
            !state.AdmissionOpen && !state.ReadyForExecution);
        Assert.DoesNotContain(supervisor.History, state => state.ReadyForExecution && state.ApplicationGeneration != 7);

        await Assert.ThrowsAsync<WorkerGenerationConflictException>(() =>
            supervisor.ConnectAsync(2, new LifecycleAcceptance(CancellationToken.None), _ => Task.FromResult<int?>(9)));
        await supervisor.StopAsync();
        Assert.False(supervisor.Current.AdmissionOpen);
        Assert.Null(supervisor.Current.ApplicationGeneration);
        Assert.False(projection.Current.HasApplicationGeneration);
    }
    [Fact]
    public async Task HeartbeatDoesNotQueueAheadOfAcceptedExecution()
    {
        var worker = new CoordinatedWorker { HoldExecution = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
            TestDurationClasses.SyntheticQuickPolicy(TimeSpan.FromSeconds(5), 2), clock);
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var active = supervisor.ExecuteAsync(Plain());
        await worker.ExecutionEntered.Task.WaitAsync(HangGuard);
        var queued = supervisor.ExecuteAsync(Plain());
        try
        {
            await clock.TickAsync();
            Assert.Equal(0, worker.PingCount);
        }
        finally
        {
            worker.ReleaseExecution.TrySetResult();
        }
        Assert.All(await Task.WhenAll(active, queued), outcome => Assert.Equal(WorkerExecutionStatus.Completed, outcome.Status));
        Assert.Equal(0, worker.PingCount);
        await clock.TickAsync();
        Assert.Equal(1, worker.PingCount);
        Assert.True(supervisor.Current.ReadyForExecution);
    }

    [Fact]
    public async Task UnexpectedHeartbeatFailureRetiresTheGeneration()
    {
        var worker = new CoordinatedWorker { UnexpectedHeartbeatFailure = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)), TestDurationClasses.SyntheticQuickPolicy(), clock);
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        await clock.FireNextAsync();
        await worker.Terminated.Task.WaitAsync(HangGuard);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.False(supervisor.Current.ReadyForExecution);
        Assert.Equal("worker-heartbeat-monitor-failed", supervisor.Current.DiagnosticCode);
        Assert.Equal(WorkerIncidentKind.ControlChannelLost, supervisor.Current.LastIncident!.Kind);
        var rejected = await supervisor.ExecuteAsync(Plain());
        Assert.Equal(WorkerExecutionStatus.Unavailable, rejected.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, rejected.ExecutionDisposition);
        Assert.Equal(0, worker.ExecuteCount);
    }

    private sealed class SequenceFactory(params IWorkerProcess[] workers) : IWorkerProcessFactory
    {
        public int Starts { get; private set; }
        public ValueTask<IWorkerProcess> StartAsync(int generation, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(workers[Starts++]);
    }

    [Fact]
    public async Task LostOutputDeliveryPreservesKnownMpCompletionAndReadiness()
    {
        var worker = new CoordinatedWorker { OutputDeliveryLost = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var outcome = await supervisor.ExecuteAsync(new WorkerMpCommand("regression.output-loss", "Output Loss",
            [], [new("Value", WorkerMpValueKind.FloatingPoint)]));
        var error = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(
            outcome, "regression.output-loss", global::Briosa.ReplaySafety.Unsafe,
            [new("value", "Value", WorkerMpValueKind.FloatingPoint)], false));
        var details = global::Briosa.OperationError.Parser.ParseFrom(Assert.Single(error.Trailers).ValueBytes);
        Assert.Equal(StatusCode.DataLoss, error.StatusCode);
        Assert.Equal(global::Briosa.ExecutionDisposition.Completed, details.ExecutionDisposition);
        Assert.Equal(global::Briosa.OperationFailureKind.OutputRetrievalFailure, details.Kind);
        Assert.Equal(global::Briosa.ReplayGuidance.DoNotReplay, details.ReplayGuidance);
        Assert.Equal(global::Briosa.MpExecutionState.Succeeded, details.MpExecution.State);
        Assert.Equal(2, details.MpExecution.MpResultCode);
        Assert.Equal(global::Briosa.OutputRetrievalState.Failed, Assert.Single(details.MpExecution.OutputRetrievals).State);
        Assert.Equal("failed", OperationAuditSummary.Create(outcome, "regression.output-loss").OutputRetrievalOutcome);
        Assert.True(supervisor.Current.ReadyForExecution);
        Assert.False(worker.HasExited);
    }

    [Fact]
    public async Task IncompleteTerminationBlocksReplacementAndRetainsTheGeneration()
    {
        var first = new CoordinatedWorker { UnexpectedFailure = true, HoldTermination = true };
        var second = new CoordinatedWorker();
        await using var firstScope = first.ConfigureAwait(true);
        await using var secondScope = second.ConfigureAwait(true);
        var factory = new SequenceFactory(first, second);
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(factory,
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), CleanupBound), TestDurationClasses.SyntheticQuickPolicy(), clock);
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        try
        {
            var failing = supervisor.ExecuteAsync(Plain());
            // The cleanup deadline and its outer wait bound expire in virtual time.
            await clock.FireNextAsync(CleanupBound, count: 2);
            var failed = await failing.WaitAsync(HangGuard);
            Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, failed.ExecutionDisposition);
            Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
            Assert.Equal("worker-termination-unconfirmed", supervisor.Current.DiagnosticCode);
            Assert.False(supervisor.Current.ReadyForExecution);
            Assert.Equal(123, supervisor.Current.ProcessId);
            var projected = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor).Current;
            Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Faulted, projected.SdkState);
            Assert.Equal(global::Briosa.SpatialAnalyzerSdkRecoveryState.OperatorActionRequired, projected.RecoveryState);
            Assert.False(projected.ReadyForMp);
            var recovering = supervisor.RecoverSdkAsync(1);
            // Recovery waits on the same outstanding cleanup under one new bound.
            await clock.FireNextAsync(CleanupBound);
            Assert.False((await recovering.WaitAsync(HangGuard)).Succeeded);
            Assert.Equal(1, factory.Starts);
            Assert.Equal(1, supervisor.Current.Generation);
            Assert.Equal(1, first.TerminationCount);
            var blocked = await supervisor.ExecuteAsync(Plain());
            Assert.Equal(WorkerExecutionDisposition.NotStarted, blocked.ExecutionDisposition);
        }
        finally
        {
            first.ReleaseTermination.TrySetResult();
        }
        Assert.True((await supervisor.RecoverSdkAsync(1)).Succeeded);
        Assert.Equal(2, factory.Starts);
        Assert.Equal(2, supervisor.Current.Generation);
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
    }

    [Fact]
    public async Task IncompleteStopReturnsFaultedInsteadOfReportingStopped()
    {
        var worker = new CoordinatedWorker { HoldStop = true, HoldTermination = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), CleanupBound), TestDurationClasses.SyntheticQuickPolicy(), clock);
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        try
        {
            var stopping = supervisor.StopAsync();
            // The stop acknowledgement deadline expires, then cleanup's two bounds.
            await clock.FireNextAsync(CleanupBound);
            await clock.FireNextAsync(CleanupBound, count: 2);
            await stopping.WaitAsync(HangGuard);
            Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
            Assert.Equal("worker-termination-unconfirmed", supervisor.Current.DiagnosticCode);
            Assert.False(supervisor.Current.ReadyForExecution);
            await Assert.ThrowsAsync<InvalidOperationException>(() => supervisor.StartAsync());
        }
        finally
        {
            worker.ReleaseTermination.TrySetResult();
        }
        await supervisor.StopAsync();
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
    }

    // A caller that stopped waiting leaves startup to its bound (#305); when that bound
    // expires and cleanup cannot confirm exit, the generation stays faulted.
    [Fact]
    public async Task DetachedStartupTimeoutWithUnconfirmedExitRemainsFaultedWithoutAConnectionSnapshot()
    {
        var worker = new CoordinatedWorker { HoldStartup = true, HoldTermination = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), CleanupBound), TestDurationClasses.SyntheticQuickPolicy(), clock);
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        using var caller = new CancellationTokenSource();
        var starting = supervisor.StartAsync(caller.Token);
        await worker.StartupEntered.Task.WaitAsync(HangGuard);
        try
        {
            await caller.CancelAsync();
            Assert.IsType<WorkerLifecycleDetached>(await starting.WaitAsync(HangGuard));
            Assert.Equal(0, worker.TerminationCount);
            await clock.FireNextAsync(TimeSpan.FromSeconds(5));
            await clock.FireNextAsync(CleanupBound, count: 2);
            using (var timeout = new CancellationTokenSource(HangGuard))
            {
                while (supervisor.Current.CleanupStatus is null)
                    await Task.Delay(TimeSpan.FromMilliseconds(5), timeout.Token);
            }
            Assert.Null(supervisor.Current.Connection);
            Assert.Equal(WorkerCleanupStatus.ExitUnconfirmed, supervisor.Current.CleanupStatus);
            var projected = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor).Current;
            Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Faulted, projected.SdkState);
            Assert.Equal(global::Briosa.SpatialAnalyzerSdkRecoveryState.OperatorActionRequired, projected.RecoveryState);
            Assert.False(projected.ReadyForMp);
            await Assert.ThrowsAsync<InvalidOperationException>(() => supervisor.StartAsync());
        }
        finally
        {
            // Releasing startup only matters if an assertion failed before the bound expired.
            worker.ReleaseStartup.TrySetResult();
            worker.ReleaseTermination.TrySetResult();
        }
        await supervisor.StopAsync();
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Null(supervisor.Current.CleanupStatus);
    }

    private static readonly TimeSpan CleanupBound = TimeSpan.FromMilliseconds(50);

    // Only a hang guard: handshakes and virtual time order every step, so this real
    // bound never decides an outcome however slowly the work is scheduled.
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(30);

    private static WorkerMpCommand Plain() => new("regression.plain", "Regression", [], []);

    // Virtual time: the startup, shutdown, and watchdog deadlines never expire on
    // the real clock, so scheduling delays cannot change these tests' outcomes.
    private static WorkerProcessSupervisor CreateSupervisor(CoordinatedWorker worker) => new(
        new Factory(worker),
        new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
        TestDurationClasses.SyntheticQuickPolicy(TimeSpan.FromSeconds(5), 2), new HeartbeatTestClock());

    // Fails every worker-transition log that carries the consumer-failure diagnostic. On
    // this head that is the retirement's fault transition; the closure is not logged.
    private sealed class ConsumerFailureTransitionThrowingLogger : ILogger<WorkerProcessSupervisor>
    {
        private int _failures;

        public int Failures => Volatile.Read(ref _failures);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> fields &&
                fields.Any(field => field.Key == "WorkerState") &&
                fields.Any(field => field is { Key: "DiagnosticCode", Value: "worker-execution-consumer-failed" }))
            {
                Interlocked.Increment(ref _failures);
                throw new InvalidOperationException("Scripted reporting failure.");
            }
        }
    }

    // Virtual time from the inner clock. Once armed, the given UTC read from then on
    // throws, exactly once.
    private sealed class FailingUtcClock(TimeProvider inner) : TimeProvider
    {
        private int _readsUntilFailure;
        private int _failed;

        public bool Failed => Volatile.Read(ref _failed) != 0;

        public void FailUtcRead(int read) => Volatile.Write(ref _readsUntilFailure, read);

        public override long TimestampFrequency => inner.TimestampFrequency;

        public override TimeZoneInfo LocalTimeZone => inner.LocalTimeZone;

        public override long GetTimestamp() => inner.GetTimestamp();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            inner.CreateTimer(callback, state, dueTime, period);

        public override DateTimeOffset GetUtcNow()
        {
            if (Volatile.Read(ref _readsUntilFailure) > 0 &&
                Interlocked.Decrement(ref _readsUntilFailure) == 0)
            {
                Volatile.Write(ref _failed, 1);
                throw new InvalidOperationException("Scripted clock failure.");
            }
            return inner.GetUtcNow();
        }
    }

    private sealed class Factory(IWorkerProcess worker) : IWorkerProcessFactory
    {
        public ValueTask<IWorkerProcess> StartAsync(int generation, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(worker);
    }

    private sealed class CoordinatedWorker : IWorkerProcess
    {
        private WorkerControlMessage? _request;
        private bool _readySent;
        public bool HoldStartup { get; init; }
        public bool ReadyDisconnected { get; init; }
        public bool HoldTermination { get; init; }
        public int TerminationCount { get; private set; }
        public TaskCompletionSource ReleaseTermination { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldExecution { get; init; }
        public bool HoldStop { get; init; }
        public bool UndefinedStatus { get; init; }
        public bool OutputDeliveryLost { get; init; }
        public bool UnexpectedFailure { get; init; }
        public int PingCount { get; private set; }
        public int ExecuteCount { get; private set; }
        public int StopCount { get; private set; }
        public bool HasExited { get; private set; }
        public int? ExitCode => HasExited ? 0 : null;
        public TaskCompletionSource StartupEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseStartup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ExecutionEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseExecution { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource StopEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseStop { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Terminated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool UnexpectedHeartbeatFailure { get; init; }

        private static WorkerConnectionSnapshot Connection(WorkerExecutionReadinessState readiness) => new(
            WorkerConnectionState.Connected, readiness, 0, 1, 1, "regression-ready", DateTimeOffset.UtcNow,
            new(new(SpatialAnalyzerApi.TargetVersion, WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
                new(SpatialAnalyzerApi.TargetVersion, WorkerRuntimeIdentityEvidenceSource.RuntimeVerified)));

        public async ValueTask SendAsync(WorkerControlMessage message, CancellationToken cancellationToken = default)
        {
            // Use real encoding: a fake that only records the message misses pre-dispatch failures.
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream);
            await channel.SendAsync(message, cancellationToken).ConfigureAwait(false);
            _request = message;
            if (message.Kind == WorkerControlMessageKind.Execute) ExecuteCount++;
        }

        public async ValueTask<WorkerControlMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            if (!_readySent)
            {
                StartupEntered.TrySetResult();
                if (HoldStartup) await ReleaseStartup.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                _readySent = true;
                return WorkerControlMessage.Ready(123, ReadyDisconnected
                    ? Connection(WorkerExecutionReadinessState.Unverified) with { State = WorkerConnectionState.Disconnected }
                    : Connection(WorkerExecutionReadinessState.Unverified));
            }
            var request = _request!;
            switch (request.Kind)
            {
                case WorkerControlMessageKind.Connect:
                    return WorkerControlMessage.ConnectionResult(request.CorrelationId, Connection(WorkerExecutionReadinessState.Unverified));
                case WorkerControlMessageKind.VerifyExecution:
                    return WorkerControlMessage.ExecutionVerificationResult(request.CorrelationId, Connection(WorkerExecutionReadinessState.ExecutionReady));
                case WorkerControlMessageKind.Execute:
                    ExecutionEntered.TrySetResult();
                    if (HoldExecution) await ReleaseExecution.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (UnexpectedFailure) throw new ArgumentException("Consumer regression");
                    return WorkerControlMessage.ExecutionResult(request.CorrelationId, new(
                        UndefinedStatus ? (WorkerExecutionResponseStatus)999 : WorkerExecutionResponseStatus.Completed,
                        UndefinedStatus ? null : OutputDeliveryLost ? new WorkerMpOutputsUnavailable(2, 7, "worker-output-encoding-rejected") : WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], null),
                        Connection(WorkerExecutionReadinessState.ExecutionReady), null));
                case WorkerControlMessageKind.Ping:
                    if (UnexpectedHeartbeatFailure) throw new ArgumentException("Monitor regression");
                    PingCount++;
                    return WorkerControlMessage.Pong(request.CorrelationId, Connection(WorkerExecutionReadinessState.ExecutionReady));
                case WorkerControlMessageKind.Stop:
                    StopCount++;
                    StopEntered.TrySetResult();
                    if (HoldStop) await ReleaseStop.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    HasExited = true;
                    return WorkerControlMessage.Stopped(request.CorrelationId);
                default:
                    throw new InvalidOperationException("Unexpected regression-test request.");
            }
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public async ValueTask TerminateAsync(CancellationToken cancellationToken = default)
        {
            TerminationCount++;
            if (HoldTermination) await ReleaseTermination.Task.ConfigureAwait(false);
            HasExited = true;
            Terminated.TrySetResult();
        }
        public ValueTask DisposeAsync()
        {
            HasExited = true;
            return ValueTask.CompletedTask;
        }
    }
}
