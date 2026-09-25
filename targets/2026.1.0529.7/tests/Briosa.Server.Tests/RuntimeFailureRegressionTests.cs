using Briosa.Server.Operations;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
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
        await worker.StartupEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var stopping = supervisor.StopAsync();
        Assert.False(stopping.IsCompleted);
        worker.ReleaseStartup.TrySetResult();
        await Task.WhenAll(starting, stopping).WaitAsync(TimeSpan.FromSeconds(3));

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
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(1)));
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        var starting = supervisor.StartAsync();
        await worker.StartupEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var stopping = supervisor.StopAsync();
        await Task.WhenAll(starting, stopping).WaitAsync(TimeSpan.FromSeconds(3));

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
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));

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
        await worker.ExecutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
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

    [Fact]
    public async Task CancellingStartupCleansUpTheCreatedChild()
    {
        var worker = new CoordinatedWorker { HoldStartup = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        using var caller = new CancellationTokenSource();
        var starting = supervisor.StartAsync(caller.Token);
        await worker.StartupEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await caller.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => starting);

        Assert.True(worker.HasExited);
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.False(WorkerReadinessHealthCheck.IsReady(supervisor.Current));
    }

    [Fact]
    public async Task CancellingStopBeforeItOwnsTheTransitionLeavesAdmissionUsable()
    {
        var worker = new CoordinatedWorker { HoldExecution = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var executing = supervisor.ExecuteAsync(Plain());
        await worker.ExecutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
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
        await worker.StopEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
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
        await worker.StopEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));

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
        await worker.ExecutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var queued = supervisor.ExecuteAsync(Plain());
        worker.ReleaseExecution.TrySetResult();

        var result = await active.WaitAsync(TimeSpan.FromSeconds(3));
        var pending = await queued.WaitAsync(TimeSpan.FromSeconds(3));
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

    [Fact]
    public async Task AReservedMappingCannotEnterASuccessorGeneration()
    {
        var first = new CoordinatedWorker();
        var second = new CoordinatedWorker();
        await using var firstLifetime = first.ConfigureAwait(true);
        await using var secondLifetime = second.ConfigureAwait(true);
        var supervisor = new WorkerProcessSupervisor(new SequenceFactory(first, second),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
            new WorkerExecutionPolicy(TimeSpan.FromSeconds(5), 2));
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var mappingEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseMapping = new ManualResetEventSlim();
        var submission = Task.Run(() => supervisor.ExecuteAsync(new WorkerCommandSubmission(
            "regression.plain", () =>
            {
                mappingEntered.TrySetResult();
                if (!releaseMapping.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
                return Plain();
            }), Guid.NewGuid()));
        await mappingEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
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

        var result = await submission.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(WorkerExecutionStatus.Unavailable, result.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, result.ExecutionDisposition);
        Assert.Equal(0, second.ExecuteCount);
        Assert.Equal(WorkerExecutionStatus.Completed, (await supervisor.ExecuteAsync(Plain())).Status);
        Assert.Equal(1, second.ExecuteCount);
    }

    [Fact]
    public async Task SnapshotsOwnApplicationAssociationAndReadinessAcrossAllProjections()
    {
        var worker = new CoordinatedWorker();
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        var projection = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor);
        var firstRead = projection.Current;
        Assert.Equal((ulong)supervisor.Current.StateRevision, firstRead.StateRevision);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        await supervisor.AssociateApplicationGenerationAsync(1, 7);
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

        await supervisor.AssociateApplicationGenerationAsync(1, null);
        Assert.Null(supervisor.Current.ApplicationGeneration);
        Assert.False(projection.Current.HasApplicationGeneration);
        await Assert.ThrowsAsync<WorkerGenerationConflictException>(() =>
            supervisor.AssociateApplicationGenerationAsync(2, 9));
        await supervisor.StopAsync();
        await supervisor.AssociateApplicationGenerationAsync(1, 9);
        Assert.False(supervisor.Current.AdmissionOpen);
        Assert.Null(supervisor.Current.ApplicationGeneration);
    }

    [Fact]
    public async Task HeartbeatDoesNotQueueAheadOfAcceptedExecution()
    {
        var worker = new CoordinatedWorker { HoldExecution = true };
        await using var workerLifetime = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
            new WorkerExecutionPolicy(TimeSpan.FromSeconds(5), 2), clock);
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        var active = supervisor.ExecuteAsync(Plain());
        await worker.ExecutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
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
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)), timeProvider: clock);
        await using var supervisorLifetime = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        await clock.FireNextAsync();
        await worker.Terminated.Task.WaitAsync(TimeSpan.FromSeconds(3));
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
        Assert.Equal("failed", OperationAuditSummary.Create(outcome).OutputRetrievalOutcome);
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
        var supervisor = new WorkerProcessSupervisor(factory,
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(50)));
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        try
        {
            var failed = await supervisor.ExecuteAsync(Plain()).WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, failed.ExecutionDisposition);
            Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
            Assert.Equal("worker-termination-unconfirmed", supervisor.Current.DiagnosticCode);
            Assert.False(supervisor.Current.ReadyForExecution);
            Assert.Equal(123, supervisor.Current.ProcessId);
            var projected = new SpatialAnalyzerSdkLifecycleStateProjection(supervisor).Current;
            Assert.Equal(global::Briosa.SpatialAnalyzerSdkState.Faulted, projected.SdkState);
            Assert.Equal(global::Briosa.SpatialAnalyzerSdkRecoveryState.OperatorActionRequired, projected.RecoveryState);
            Assert.False(projected.ReadyForMp);
            Assert.False((await supervisor.RecoverSdkAsync(1).WaitAsync(TimeSpan.FromSeconds(3))).Succeeded);
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
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(50)));
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        Assert.True((await supervisor.StartAsync()).Succeeded);
        try
        {
            await supervisor.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
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

    [Fact]
    public async Task CancelledStartupWithUnconfirmedExitRemainsFaultedWithoutAConnectionSnapshot()
    {
        var worker = new CoordinatedWorker { HoldStartup = true, HoldTermination = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var supervisor = new WorkerProcessSupervisor(new Factory(worker),
            new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(50)));
        await using var supervisorScope = supervisor.ConfigureAwait(true);
        using var caller = new CancellationTokenSource();
        var starting = supervisor.StartAsync(caller.Token);
        await worker.StartupEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        try
        {
            await caller.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => starting.WaitAsync(TimeSpan.FromSeconds(3)));
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
            worker.ReleaseTermination.TrySetResult();
        }
        await supervisor.StopAsync();
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Null(supervisor.Current.CleanupStatus);
    }

    private static WorkerMpCommand Plain() => new("regression.plain", "Regression", [], []);

    private static WorkerProcessSupervisor CreateSupervisor(CoordinatedWorker worker) => new(
        new Factory(worker),
        new WorkerLifecyclePolicy(TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)),
        new WorkerExecutionPolicy(TimeSpan.FromSeconds(5), 2));

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
                return WorkerControlMessage.Ready(123, Connection(WorkerExecutionReadinessState.Unverified));
            }
            var request = _request!;
            switch (request.Kind)
            {
                case WorkerControlMessageKind.VerifyExecution:
                    return WorkerControlMessage.ExecutionVerificationResult(request.CorrelationId, Connection(WorkerExecutionReadinessState.ExecutionReady));
                case WorkerControlMessageKind.Execute:
                    ExecutionEntered.TrySetResult();
                    if (HoldExecution) await ReleaseExecution.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (UnexpectedFailure) throw new ArgumentException("Consumer regression");
                    return WorkerControlMessage.ExecutionResult(request.CorrelationId, new(
                        UndefinedStatus ? (WorkerExecutionResponseStatus)999 : WorkerExecutionResponseStatus.Completed,
                        UndefinedStatus ? null : OutputDeliveryLost ? new WorkerMpOutputsUnavailable(7, "worker-output-encoding-rejected") : WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], null),
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
