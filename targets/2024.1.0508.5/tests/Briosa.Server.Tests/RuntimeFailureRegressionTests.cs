using Briosa.Server.Operations;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Tests;

public sealed class RuntimeFailureRegressionTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task NonFinitePublicInputIsRejectedWithoutLosingTheWorker(double value)
    {
        var worker = new CoordinatedWorker();
        await using var workerLifetime = worker.ConfigureAwait(true);
        await using var supervisor = CreateSupervisor(worker);
        Assert.True(await supervisor.StartAsync());

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
        Assert.True(await supervisor.StartAsync());
        var operation = MpOperationCatalog.Get("variables.set_string_variable");
        var executor = new OperationExecutor(supervisor,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System);

        var error = await Assert.ThrowsAsync<RpcException>(() => executor.ExecuteAsync(
            new global::Briosa.SetStringVariableRequest { Name = "regression", Value = new string('x', 70_000) },
            operation.Descriptor, operation.CreateCommand, operation.OutputContracts,
            operation.CreateResult<global::Briosa.SetStringVariableResult>, CancellationToken.None));

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
        Assert.True(await supervisor.StartAsync());
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
        Assert.True(await supervisor.StartAsync());
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
        Assert.True(await supervisor.StartAsync());
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
        Assert.True(await supervisor.StartAsync());
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
        Assert.True(await supervisor.StartAsync());
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
        Assert.True(await supervisor.StartAsync());
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
        Assert.True(await supervisor.StartAsync());

        var result = await supervisor.ExecuteAsync(Plain());

        Assert.Equal(WorkerExecutionStatus.WorkerFailure, result.Status);
        Assert.Equal(WorkerExecutionDisposition.StartedOutcomeUnknown, result.ExecutionDisposition);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.True(worker.HasExited);
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
        public bool HoldExecution { get; init; }
        public bool HoldStop { get; init; }
        public bool UndefinedStatus { get; init; }
        public int ExecuteCount { get; private set; }
        public int StopCount { get; private set; }
        public bool HasExited { get; private set; }
        public int? ExitCode => HasExited ? 0 : null;
        public TaskCompletionSource StartupEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ExecutionEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseExecution { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource StopEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseStop { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
                if (HoldStartup) await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
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
                    return WorkerControlMessage.ExecutionResult(request.CorrelationId, new(
                        UndefinedStatus ? (WorkerExecutionResponseStatus)999 : WorkerExecutionResponseStatus.Completed,
                        UndefinedStatus ? null : WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], null),
                        Connection(WorkerExecutionReadinessState.ExecutionReady), null));
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
        public ValueTask TerminateAsync(CancellationToken cancellationToken = default)
        {
            HasExited = true;
            return ValueTask.CompletedTask;
        }
        public ValueTask DisposeAsync() => TerminateAsync();
    }
}
