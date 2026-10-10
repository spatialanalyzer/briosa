using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Workers;

internal sealed partial class WorkerProcessSupervisor :
    IWorkerCommandExecutor, IWorkerCommandDispatcher, IWorkerLifecycleController, IAsyncDisposable
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<WorkerLifecycleSnapshot> _history = [];
    private readonly Lock _historyLock = new();
    private readonly WorkerExecutionPolicy _executionPolicy;
    private readonly ExactTargetIdentityPolicy _identityPolicy;
    private readonly IWorkerProcessFactory _processFactory;
    private readonly WorkerLifecyclePolicy _policy;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WorkerProcessSupervisor> _logger;
    private readonly BriosaTelemetry? _telemetry;
    private WorkerExecutionQueue? _executionQueue;
    private WorkerHeartbeatMonitor? _heartbeatMonitor;
    private WorkerProcessLifetime? _processLifetime;
    private IWorkerProcess? Worker => _processLifetime?.Process;
    private WorkerLifecycleSnapshot _current;
    private int _generation;
    private int _reportedProcessId;
    private int _recoveryCount;
    private int _disposeState;
    private int _queuedRequestCount;
    private int _activeExecutionCount;
    private int _peakQueuedRequestCount;
    private long _admittedRequestCount;
    private long _terminalRequestCount;
    private long _clientCancellationBeforeAdmissionCount;
    private long _clientCancellationAfterAdmissionCount;
    private long _abandonedRequestCount;
    private long _watchdogTimeoutCount;
    private long _workerFailureCount;
    private long _stateRevision = 1;
    private long _lastSuccessfulExchange;

    public WorkerProcessSupervisor(
        IWorkerProcessFactory processFactory,
        WorkerLifecyclePolicy policy,
        WorkerExecutionPolicy? executionPolicy = null,
        TimeProvider? timeProvider = null,
        ILogger<WorkerProcessSupervisor>? logger = null,
        ExactTargetIdentityPolicy? identityPolicy = null,
        BriosaTelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(policy);
        _processFactory = processFactory;
        _policy = policy;
        _executionPolicy = executionPolicy ?? new WorkerExecutionPolicy(
            WorkerProcessOptions.DefaultExecutionWatchdogTimeout,
            queueCapacity: 64);
        _identityPolicy = identityPolicy ?? ExactTargetIdentityPolicy.CreateRuntimeOnly(
            SpatialAnalyzerApi.TargetVersion);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<WorkerProcessSupervisor>.Instance;
        _telemetry = telemetry;
        telemetry?.Attach(this);
        _current = new WorkerLifecycleSnapshot(
            WorkerLifecycleState.Stopped,
            Generation: 0,
            ProcessId: null,
            RecoveryCount: 0,
            WorkerTerminationKind.None,
            "not-started",
            Connection: null,
            _timeProvider.GetUtcNow(), StateRevision: _stateRevision);
        _history.Add(_current);
    }

    public WorkerLifecycleSnapshot Current
    {
        get
        {
            lock (_historyLock)
            {
                return _current;
            }
        }
    }

    public IReadOnlyList<WorkerLifecycleSnapshot> History
    {
        get
        {
            lock (_historyLock)
            {
                return [.. _history];
            }
        }
    }

    internal WorkerLifecyclePolicy LifecyclePolicy => _policy;

    internal WorkerExecutionPolicy ExecutionPolicy => _executionPolicy;

    public WorkerExecutionSnapshot ExecutionSnapshot => new(
        _executionPolicy.QueueCapacity,
        Volatile.Read(ref _queuedRequestCount),
        WaitingForAdmission: 0,
        Volatile.Read(ref _activeExecutionCount),
        Volatile.Read(ref _peakQueuedRequestCount),
        Interlocked.Read(ref _admittedRequestCount),
        Interlocked.Read(ref _terminalRequestCount),
        Interlocked.Read(ref _clientCancellationBeforeAdmissionCount),
        Interlocked.Read(ref _clientCancellationAfterAdmissionCount),
        Interlocked.Read(ref _watchdogTimeoutCount),
        Interlocked.Read(ref _workerFailureCount))
    {
        ReservedWorkBytes = _executionQueue?.ReservedBytes ?? 0,
        MaxRetainedWorkBytes = _executionPolicy.MaxRetainedWorkBytes,
        AbandonedRequests = Interlocked.Read(ref _abandonedRequestCount)
    };

    // A direct caller that stops waiting after acceptance receives WorkerLifecycleDetached
    // at once; the accepted exchange keeps the lifecycle gate until it finishes.
    public Task<WorkerLifecycleResult> StartAsync(CancellationToken cancellationToken = default) =>
        WaitAsCallerAsync(StartAsync, cancellationToken);

    public Task<WorkerLifecycleResult> StartAsync(LifecycleAcceptance acceptance) =>
        RunLifecycleAsync(StartCoreAsync, acceptance);

    public Task<WorkerLifecycleResult> ConnectAsync(int expectedGeneration, CancellationToken cancellationToken = default) =>
        WaitAsCallerAsync(acceptance => ConnectAsync(expectedGeneration, acceptance), cancellationToken);

    public Task<WorkerLifecycleResult> ConnectAsync(int expectedGeneration, LifecycleAcceptance acceptance) =>
        RunLifecycleAsync(accepted => ConnectCoreAsync(expectedGeneration, accepted), acceptance);

    public Task<WorkerLifecycleResult> RecoverSdkAsync(int expectedGeneration, CancellationToken cancellationToken = default) =>
        WaitAsCallerAsync(acceptance => RecoverSdkAsync(expectedGeneration, acceptance), cancellationToken);

    public Task<WorkerLifecycleResult> RecoverSdkAsync(int expectedGeneration, LifecycleAcceptance acceptance) =>
        RunLifecycleAsync(accepted => RecoverSdkCoreAsync(expectedGeneration, accepted), acceptance);

    private Task<WorkerLifecycleResult> WaitAsCallerAsync(
        Func<LifecycleAcceptance, Task<WorkerLifecycleResult>> exchange, CancellationToken cancellationToken)
    {
        var acceptance = new LifecycleAcceptance(cancellationToken);
        return acceptance.WaitAsync(exchange(acceptance), () => new WorkerLifecycleDetached(Current));
    }

    private async Task<WorkerLifecycleResult> RunLifecycleAsync(
        Func<LifecycleAcceptance, Task<WorkerLifecycleResult>> action, LifecycleAcceptance acceptance)
    {
        ArgumentNullException.ThrowIfNull(acceptance);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        // Waiting to begin is not acceptance: the caller may still withdraw here.
        await _lifecycleGate.WaitAsync(acceptance.CallerToken).ConfigureAwait(false);
        try
        {
            return await action(acceptance).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    // Called under _gate: neither another lifecycle action nor a monitor/execution
    // transition can replace the snapshot between deciding the result and capturing it.
    private WorkerLifecycleResult CaptureLifecycleResult(bool succeeded) =>
        succeeded ? new WorkerLifecycleSucceeded(Current) : new WorkerLifecycleFailed(Current);

    // Callers hold _gate while checking and acting on this generation.
    private WorkerLifecycleSnapshot RequireExpectedGeneration(int expectedGeneration)
    {
        var current = Current;
        if (expectedGeneration <= 0 || current.Generation != expectedGeneration)
            throw new WorkerGenerationConflictException(expectedGeneration, current.Generation);
        return current;
    }

    // Called after acceptance: worker launch, COM activation, Ready, and any readiness
    // probe run under the startup and probe bounds only, never the caller's token.
    private async Task<bool> StartGenerationAsync()
    {
        var started = await StartWorkerAsync().ConfigureAwait(false);
        if (started) StartRuntimeLoops();
        return started;
    }

    private async Task<WorkerLifecycleResult> StartCoreAsync(LifecycleAcceptance acceptance)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _gate.WaitAsync(acceptance.CallerToken).ConfigureAwait(false);
        try
        {
            if (Current.State != WorkerLifecycleState.Stopped)
            {
                throw new InvalidOperationException("The worker supervisor is already active.");
            }

            acceptance.Accept();
            _recoveryCount = 0;
            return CaptureLifecycleResult(await StartGenerationAsync().ConfigureAwait(false));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<WorkerLifecycleResult> ConnectCoreAsync(
        int expectedGeneration,
        LifecycleAcceptance acceptance)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _gate.WaitAsync(acceptance.CallerToken).ConfigureAwait(false);
        try
        {
            var current = RequireExpectedGeneration(expectedGeneration);

            var worker = Worker;
            if (current.State != WorkerLifecycleState.Ready || worker is null)
            {
                throw new InvalidOperationException(
                    "A live SDK worker generation is required before connection.");
            }

            if (current.Connection?.State == WorkerConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    "The SDK worker generation is already connected.");
            }

            acceptance.Accept();
            var connecting = current.Connection! with
            {
                State = WorkerConnectionState.Connecting,
                ExecutionReadinessState = WorkerExecutionReadinessState.Unverified,
                DiagnosticCode = "connect-ex-started",
                Failure = WorkerConnectionFailure.None,
                TransitionedAt = _timeProvider.GetUtcNow()
            };
            Transition(
                WorkerLifecycleState.Starting,
                _reportedProcessId,
                current.LastTermination,
                "connect-ex-started",
                connecting);

            // The accepted ConnectEx exchange runs under the startup bound only. A caller
            // that stops waiting cannot interrupt it or retire the generation.
            using var timeout = new CancellationTokenSource(_policy.StartupTimeout, _timeProvider);
            var correlationId = Guid.NewGuid();
            WorkerControlMessage response;
            try
            {
                await worker.SendAsync(
                    WorkerControlMessage.Connect(correlationId),
                    timeout.Token).ConfigureAwait(false);
                response = await worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
                if (response.Kind != WorkerControlMessageKind.ConnectionResult ||
                    response.CorrelationId != correlationId ||
                    response.Connection is null ||
                    !ExactTargetIdentityPolicy.IsWellFormed(
                        response.Connection.RuntimeIdentity))
                {
                    throw new InvalidDataException(
                        "The worker returned an invalid connection response.");
                }
            }
            catch (OperationCanceledException)
            {
                // Only the startup bound can cancel this exchange.
                await RetireWorkerAsync(
                    "connect-ex-timeout",
                    connecting, lifecycleFailure: WorkerLifecycleFailure.ConnectionTimeout).ConfigureAwait(false);
                return CaptureLifecycleResult(false);
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                await RetireWorkerAsync(
                    worker.HasExited
                        ? "worker-exited-during-connect"
                        : "sdk-connection-control-failed",
                    connecting, lifecycleFailure: WorkerLifecycleFailure.ConnectionFailed).ConfigureAwait(false);
                return CaptureLifecycleResult(false);
            }

            return await ApplyConnectionResultAsync(response.Connection!, current)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Called with _gate held; the returned result captures the transition it describes.
    private async Task<WorkerLifecycleResult> ApplyConnectionResultAsync(
        WorkerConnectionSnapshot connection,
        WorkerLifecycleSnapshot beforeConnection)
    {
        if (connection.State != WorkerConnectionState.Connected)
        {
            if (RequiresSdkRecovery(connection))
            {
                await RetireWorkerAsync(
                    connection.DiagnosticCode,
                    connection, lifecycleFailure: WorkerLifecycleFailure.ConnectionFailed).ConfigureAwait(false);
            }
            else
            {
                Transition(
                    WorkerLifecycleState.Ready,
                    _reportedProcessId,
                    beforeConnection.LastTermination,
                    connection.DiagnosticCode,
                    connection, lifecycleFailure: WorkerLifecycleFailure.ConnectionFailed);
            }

            return CaptureLifecycleResult(false);
        }

        if (!_identityPolicy.Evaluate(connection.RuntimeIdentity).AllowsExecution)
        {
            Transition(
                WorkerLifecycleState.Ready,
                _reportedProcessId,
                beforeConnection.LastTermination,
                "runtime-identity-not-ready",
                connection with
                {
                    ExecutionReadinessState = WorkerExecutionReadinessState.Unverified,
                    DiagnosticCode = "runtime-identity-not-ready",
                    TransitionedAt = _timeProvider.GetUtcNow()
                }, lifecycleFailure: WorkerLifecycleFailure.IdentityRejected);
            return CaptureLifecycleResult(false);
        }

        Transition(
            WorkerLifecycleState.Starting,
            _reportedProcessId,
            beforeConnection.LastTermination,
            "execution-readiness-probe-started",
            connection with
            {
                ExecutionReadinessState = WorkerExecutionReadinessState.Verifying,
                DiagnosticCode = "execution-readiness-probe-started",
                TransitionedAt = _timeProvider.GetUtcNow()
            });
        var verified = await VerifyWorkerExecutionAsync(connection)
            .ConfigureAwait(false);
        return CaptureLifecycleResult(verified);
    }

    private async Task<WorkerLifecycleResult> RecoverSdkCoreAsync(
        int expectedGeneration,
        LifecycleAcceptance acceptance)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _gate.WaitAsync(acceptance.CallerToken).ConfigureAwait(false);
        try
        {
            var current = RequireExpectedGeneration(expectedGeneration);

            if (current.State != WorkerLifecycleState.Degraded)
            {
                throw new InvalidOperationException(
                    "SDK recovery is available only for a faulted generation.");
            }

            acceptance.Accept();
        }
        finally
        {
            _gate.Release();
        }

        // Validation must succeed before admission or runtime loops are changed.
        // The lifecycle gate excludes competing start/stop/recovery transitions.
        await StopRuntimeLoopsAsync().ConfigureAwait(false);
        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (!await CleanupWorkerAsync(force: true).ConfigureAwait(false)) return CaptureLifecycleResult(false);
            _recoveryCount++;
            return CaptureLifecycleResult(await StartGenerationAsync().ConfigureAwait(false));
        }
        finally
        {
            _gate.Release();
        }
    }


    public async Task<WorkerLifecycleSnapshot> AssociateApplicationGenerationAsync(int expectedGeneration,
        int? applicationGeneration, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = Current;
            if (current.Generation != expectedGeneration)
                throw new WorkerGenerationConflictException(expectedGeneration, current.Generation);
            if (current.State != WorkerLifecycleState.Ready ||
                current.Connection?.State != WorkerConnectionState.Connected ||
                current.ApplicationGeneration == applicationGeneration) return current;
            PublishSnapshot(current with
            {
                ApplicationGeneration = applicationGeneration,
                StateRevision = Interlocked.Increment(ref _stateRevision),
                TransitionedAt = _timeProvider.GetUtcNow()
            });
            return Current;
        }
        finally
        {
            _gate.Release();
        }
    }

    // The caller keeps waiting for an accepted teardown, which its shutdown bounds limit.
    public Task<WorkerLifecycleResult> StopAsync(CancellationToken cancellationToken = default) =>
        RunLifecycleAsync(StopCoreAsync, new LifecycleAcceptance(cancellationToken));

    private async Task<WorkerLifecycleResult> StopCoreAsync(LifecycleAcceptance acceptance)
    {
        // Cancellation may abandon waiting to begin, but never leave an accepted
        // teardown half complete. Acquire ownership before closing admission.
        await _gate.WaitAsync(acceptance.CallerToken).ConfigureAwait(false);
        try
        {
            acceptance.Accept();
            var current = Current;
            Transition(WorkerLifecycleState.Stopping, current.ProcessId,
                current.LastTermination, "worker-stopping", current.Connection);
        }
        finally
        {
            _gate.Release();
        }

        await StopRuntimeLoopsAsync().ConfigureAwait(false);

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await StopWorkerAsync().ConfigureAwait(false);
            return CaptureLifecycleResult(Current.State == WorkerLifecycleState.Stopped &&
                Current.LifecycleFailure == WorkerLifecycleFailure.None);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task StopRuntimeLoopsAsync()
    {
        var executionQueue = _executionQueue;
        _executionQueue = null;
        if (executionQueue is not null)
        {
            await executionQueue.CloseAsync().ConfigureAwait(false);
        }

        var monitor = _heartbeatMonitor;
        _heartbeatMonitor = null;
        try
        {
            if (monitor is not null)
                await monitor.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            // A monitor fault cannot strand the generation's queued work.
            if (executionQueue is not null)
                await executionQueue.Completion.ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        // Disposal has already closed the public lifecycle entry points.
        await _lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await StopCoreAsync(new LifecycleAcceptance(CancellationToken.None)).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
        _gate.Dispose();
        _lifecycleGate.Dispose();
    }


    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(command, Guid.NewGuid(), cancellationToken);

    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(new WorkerCommandSubmission(command.OperationId, () => command),
            correlationId, cancellationToken);

    public async Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerCommandSubmission submission,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        var effectiveCorrelationId = correlationId != Guid.Empty
            ? correlationId
            : Guid.NewGuid();
        if (cancellationToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _clientCancellationBeforeAdmissionCount);
            return ClientCancelled(
                effectiveCorrelationId,
                WorkerExecutionDisposition.NotStarted);
        }

        // The host chooses the execution budget before admission from the request's
        // effective duration class, set by admission policy (for example interactive
        // when the request turns on operator UI), or else the reviewed class. It is
        // never shorter than the reviewed class's budget. An operation without a
        // reviewed class never executes.
        if (!_executionPolicy.TryGetExecutionBudget(submission.OperationId,
                submission.DurationClass, out var durationClass, out var executionBudget))
        {
            return new WorkerExecutionOutcome(WorkerExecutionStatus.PolicyDenied,
                WorkerExecutionDisposition.NotStarted, null, Current.Connection,
                "operation-duration-unreviewed", Current.Generation, effectiveCorrelationId);
        }

        var queue = _executionQueue;
        var snapshot = Current;
        if (queue is null || queue.IsClosed || queue.Generation != snapshot.Generation ||
            !snapshot.ReadyForExecution)
        {
            return Unavailable(snapshot.State == WorkerLifecycleState.Ready
                ? GetExecutionNotReadyDiagnostic(snapshot) : "worker-not-ready", effectiveCorrelationId);
        }

        // A reservation covers both mapping and queue handoff. There are no
        // capacity waiters retaining requests outside the bounded queue.
        if (!queue.TryReserve(submission.RetainedBytes))
        {
            return new WorkerExecutionOutcome(WorkerExecutionStatus.Overloaded,
                WorkerExecutionDisposition.NotStarted, null, Current.Connection,
                "worker-admission-full", Current.Generation, effectiveCorrelationId);
        }

        ExecutionWorkItem? item = null;
        var handedOff = false;
        var admissionStarted = _timeProvider.GetTimestamp();
        using var admissionActivity = BriosaTelemetry.Start("briosa.admission", submission.OperationId);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            queue.CancellationToken.ThrowIfCancellationRequested();
            var command = submission.CreateCommand();
            if (command.OperationId != submission.OperationId)
                throw new ArgumentException("The operation command does not match its submission.");
            cancellationToken.ThrowIfCancellationRequested();
            queue.CancellationToken.ThrowIfCancellationRequested();
            item = new ExecutionWorkItem(command, effectiveCorrelationId, queue.Generation,
                Activity.Current?.Context ?? default, submission.RetainedBytes,
                durationClass, executionBudget);
            if (!queue.TryWrite(item))
            {
                return Unavailable(
                    "worker-execution-queue-closed",
                    effectiveCorrelationId);
            }

            handedOff = true;
            item.AdmissionMilliseconds = _timeProvider.GetElapsedTime(admissionStarted).TotalMilliseconds;
            MarkAdmitted(item);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _clientCancellationBeforeAdmissionCount);
            return ClientCancelled(
                effectiveCorrelationId,
                WorkerExecutionDisposition.NotStarted);
        }
        catch (OperationCanceledException) when (queue.IsClosed)
        {
            return Unavailable("worker-execution-queue-closed", effectiveCorrelationId)
                .WithGeneration(queue.Generation);
        }
        finally
        {
            if (!handedOff) queue.ReleaseReservation(submission.RetainedBytes);
            _telemetry?.Admission(submission.OperationId,
                _timeProvider.GetElapsedTime(admissionStarted).TotalMilliseconds);
            admissionActivity?.Stop();
        }
        try
        {
            return await item.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _clientCancellationAfterAdmissionCount);
            if (item.TryAbandon())
            {
                // Abandonment won before the consumer claimed the item, so it is
                // never sent to the worker. Only this caller can prove NotStarted.
                Interlocked.Increment(ref _abandonedRequestCount);
                var abandoned = new WorkerExecutionOutcome(
                    WorkerExecutionStatus.ClientCancelled,
                    WorkerExecutionDisposition.NotStarted,
                    Execution: null,
                    Current.Connection,
                    "queued-request-abandoned",
                    item.Generation,
                    effectiveCorrelationId);
                Complete(item, abandoned);
                return abandoned;
            }

            // The consumer claimed the item first; it drains under its execution budget.
            return ClientCancelled(
                effectiveCorrelationId,
                WorkerExecutionDisposition.StartedOutcomeUnknown)
                .WithGeneration(item.Generation);
        }
    }

    private async Task ProcessExecutionsAsync(WorkerExecutionQueue queue)
    {
        var cancellationToken = queue.CancellationToken;
        try
        {
            await foreach (var item in queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await item.WaitUntilAdmitted().ConfigureAwait(false);
                Interlocked.Decrement(ref _queuedRequestCount);
                queue.ReleaseCountReservation();
                if (item.IsAbandoned)
                {
                    // Its caller already resolved it as NotStarted; never dispatch it.
                    queue.ReleaseBytes(item.RetainedBytes);
                    continue;
                }

                Interlocked.Increment(ref _activeExecutionCount);
                try
                {
                    WorkerExecutionOutcome? outcome;
                    try
                    {
                        outcome = await ExecuteWorkerAsync(item, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _activeExecutionCount);
                    }
                    // A null outcome means the caller abandoned the item before the claim.
                    if (outcome is not null) Complete(item, outcome);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    // The consumer owns resolution even if a programming or
                    // observer error escapes the exchange path. Never replay it.
                    await FailConsumerAsync(queue, item).ConfigureAwait(false);
                    break;
                }
                finally
                {
                    queue.ReleaseBytes(item.RetainedBytes);
                }

            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await queue.CloseAsync().ConfigureAwait(false);
            while (queue.Reader.TryRead(out var item))
            {
                await item.WaitUntilAdmitted().ConfigureAwait(false);
                Interlocked.Decrement(ref _queuedRequestCount);
                queue.ReleaseReservation(item.RetainedBytes);
                // Shutdown and abandonment race for a queued item; exactly one resolves it.
                if (item.TryClaim())
                    Complete(item, Unavailable("worker-execution-queue-closed", item.CorrelationId)
                        .WithGeneration(item.Generation));
            }
        }
    }

    private async Task FailConsumerAsync(WorkerExecutionQueue queue, ExecutionWorkItem item)
    {
        const string diagnostic = "worker-execution-consumer-failed";
        await queue.CloseAsync().ConfigureAwait(false);
        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (Current.Generation == queue.Generation)
            {
                await RetireWorkerAsync(diagnostic, operationId: item.Command.OperationId,
                    executionDisposition: item.ResolvedOutcome?.ExecutionDisposition ??
                        item.DispatchDisposition).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
            if (item.TryClaim() && !item.Task.IsCompleted)
                Complete(item, item.ResolvedOutcome ?? new WorkerExecutionOutcome(
                    WorkerExecutionStatus.WorkerFailure, item.DispatchDisposition,
                    null, null, diagnostic, item.Generation, item.CorrelationId));
        }
    }

    private void MarkAdmitted(ExecutionWorkItem item)
    {
        item.AdmittedAt = _timeProvider.GetTimestamp();
        item.AdmittedUtc = _timeProvider.GetUtcNow();
        Interlocked.Increment(ref _admittedRequestCount);
        var queueDepth = Interlocked.Increment(ref _queuedRequestCount);
        var observedPeak = Volatile.Read(ref _peakQueuedRequestCount);
        while (queueDepth > observedPeak)
        {
            var previous = Interlocked.CompareExchange(
                ref _peakQueuedRequestCount,
                queueDepth,
                observedPeak);
            if (previous == observedPeak)
            {
                break;
            }

            observedPeak = previous;
        }

        item.MarkAdmitted();
    }

    private void Complete(ExecutionWorkItem item, WorkerExecutionOutcome outcome)
    {
        Interlocked.Increment(ref _terminalRequestCount);
        if (outcome.Status == WorkerExecutionStatus.WatchdogTimeout)
        {
            Interlocked.Increment(ref _watchdogTimeoutCount);
        }
        else if (outcome.Status == WorkerExecutionStatus.WorkerFailure)
        {
            Interlocked.Increment(ref _workerFailureCount);
        }

        try
        {
        // The queue owns this event even after the RPC caller has stopped waiting.
        // Restore only correlation context, never request objects or ambient scopes.
        using var resolution = BriosaTelemetry.Start("briosa.execution.resolved",
            item.Command.OperationId, item.ParentContext);
        _telemetry?.Resolved(item.Command.OperationId, outcome);
        var summary = OperationAuditSummary.Create(outcome);
        var level = outcome.Status is WorkerExecutionStatus.WorkerFailure or WorkerExecutionStatus.WatchdogTimeout
            ? LogLevel.Error
            : summary.MpOutcome == "succeeded" && summary.OutputRetrievalOutcome == "retrieved"
                ? LogLevel.Information : LogLevel.Warning;
        if (_logger.IsEnabled(level))
        {
            var replaySafety = BriosaTelemetry.ReplaySafety(item.Command.OperationId);
            LogExecutionResolved(level, item.CorrelationId, item.Command.OperationId,
            outcome.Generation == 0 ? item.Generation : outcome.Generation, summary.ExecutionDisposition,
            outcome.Execution?.MpResultRetrieved, summary.MpResultCode, summary.MpOutcome,
            summary.OutputRetrievalOutcome, summary.SdkDurationMilliseconds,
            item.AdmissionMilliseconds, item.QueueMilliseconds, item.ExchangeMilliseconds,
            replaySafety,
                outcome.DiagnosticCode);
        }
        }
        finally
        {
            item.TrySetResult(outcome);
        }
    }

    private void StartRuntimeLoops()
    {
        var queue = new WorkerExecutionQueue(_generation, _executionPolicy.QueueCapacity,
            _executionPolicy.MaxRetainedWorkBytes);
        _executionQueue = queue;
        queue.Completion = ProcessExecutionsAsync(queue);
        _lastSuccessfulExchange = _timeProvider.GetTimestamp();
        _heartbeatMonitor = new WorkerHeartbeatMonitor(_policy.HeartbeatInterval, _timeProvider, ProbeIdleWorkerAsync);
        var current = Current;
        Transition(current.State, current.ProcessId, current.LastTermination,
            current.DiagnosticCode, current.Connection);
    }

    // Returns null when the caller abandoned the item before the consumer claimed it:
    // that caller already resolved it as NotStarted, and it never reaches the worker.
    private async Task<WorkerExecutionOutcome?> ExecuteWorkerAsync(
        ExecutionWorkItem item,
        CancellationToken cancellationToken)
    {
        var command = item.Command;
        var correlationId = item.CorrelationId;
        var acquired = false;
        var requestMayHaveStarted = false;
        long? exchangeStarted = null;
        Activity? exchange = null;
        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            // The claim ends the caller's chance to abandon. From here the consumer
            // resolves the item, either before dispatch or by draining its exchange.
            if (!item.TryClaim()) return null;
            item.QueueMilliseconds = _timeProvider.GetElapsedTime(item.AdmittedAt).TotalMilliseconds;
            _telemetry?.Queue(command.OperationId, item.QueueMilliseconds);
            using (var queueActivity = BriosaTelemetry.Activities.StartActivity(
                "briosa.queue", ActivityKind.Internal, item.ParentContext,
                startTime: item.AdmittedUtc))
            {
                queueActivity?.SetTag("briosa.operation", BriosaTelemetry.OperationId(command.OperationId));
            }
            var generation = Current.Generation;
            var worker = Worker;
            if (Current.State != WorkerLifecycleState.Ready || worker is null || item.Generation != Current.Generation ||
                !Current.ReadyForExecution)
            {
                return Unavailable(
                    Current.State == WorkerLifecycleState.Ready && worker is not null
                        ? GetExecutionNotReadyDiagnostic(Current)
                        : "worker-not-ready",
                    correlationId);
            }

            // A cancelled length-prefixed exchange cannot safely share its pipe with Stop.
            // Runtime-loop cancellation stops admission; this operation's watchdog owns
            // cancellation once the consumer has claimed the request. Its budget was
            // chosen at admission from the operation's reviewed duration class.
            using var watchdog = new CancellationTokenSource(
                item.ExecutionBudget, _timeProvider);
            try
            {
                exchangeStarted = _timeProvider.GetTimestamp();
                exchange = BriosaTelemetry.Start("briosa.worker.exchange", command.OperationId, item.ParentContext);
                LogExecutionDispatched(correlationId, command.OperationId, generation,
                    item.DurationClass, item.ExecutionBudget.TotalMilliseconds);
                requestMayHaveStarted = true;
                item.DispatchDisposition = WorkerExecutionDisposition.StartedOutcomeUnknown;
                var executionResponse = await WorkerCommandExchange.RunAsync(
                    worker, command, correlationId, Current.Connection?.RuntimeIdentity,
                    watchdog.Token).ConfigureAwait(false);

                _lastSuccessfulExchange = _timeProvider.GetTimestamp();
                item.ResolvedOutcome = WorkerExecutionOutcome.FromWorkerResponse(
                    executionResponse, generation, correlationId);
                return item.ResolvedOutcome;
            }
            catch (OperationCanceledException) when (watchdog.IsCancellationRequested)
            {
                await RetireWorkerAsync(
                    "worker-execution-watchdog-timeout",
                    incidentKind: WorkerIncidentKind.WatchdogTerminated,
                    operationId: command.OperationId,
                    executionDisposition:
                        WorkerExecutionDisposition.StartedOutcomeUnknown)
                    .ConfigureAwait(false);
                return new WorkerExecutionOutcome(
                    WorkerExecutionStatus.WatchdogTimeout,
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    Execution: null,
                    Connection: null,
                    "worker-execution-watchdog-timeout",
                    generation,
                    correlationId);
            }
            catch (WorkerMessageRejectedException)
            {
                // The channel guarantees no header/payload bytes were written.
                item.DispatchDisposition = WorkerExecutionDisposition.NotStarted;
                return new WorkerExecutionOutcome(
                    WorkerExecutionStatus.RequestRejected,
                    WorkerExecutionDisposition.NotStarted,
                    Execution: null,
                    Current.Connection,
                    "request-encoding-rejected",
                    generation,
                    correlationId);
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                var diagnosticCode = worker.HasExited
                    ? "worker-exited-during-execution"
                    : "worker-execution-control-failed";
                await RetireWorkerAsync(
                    diagnosticCode,
                    operationId: command.OperationId,
                    executionDisposition:
                        WorkerExecutionDisposition.StartedOutcomeUnknown)
                    .ConfigureAwait(false);
                return new WorkerExecutionOutcome(
                    WorkerExecutionStatus.WorkerFailure,
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    Execution: null,
                    Connection: null,
                    diagnosticCode,
                    generation,
                    correlationId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown and abandonment race for an item still waiting for the gate.
            if (!item.TryClaim()) return null;
            return Unavailable(
                "worker-supervisor-stopping",
                correlationId,
                requestMayHaveStarted
                    ? WorkerExecutionDisposition.StartedOutcomeUnknown
                    : WorkerExecutionDisposition.NotStarted);
        }
        finally
        {
            if (exchangeStarted is { } started)
            {
                item.ExchangeMilliseconds = _timeProvider.GetElapsedTime(started).TotalMilliseconds;
                _telemetry?.Exchange(command.OperationId, item.ExchangeMilliseconds);
            }
            exchange?.Dispose();
            if (acquired)
            {
                _gate.Release();
            }
        }
    }

    private WorkerExecutionOutcome ClientCancelled(
        Guid correlationId,
        WorkerExecutionDisposition disposition) =>
        new(
            WorkerExecutionStatus.ClientCancelled,
            disposition,
            Execution: null,
            Current.Connection,
            "client-wait-cancelled",
            Current.Generation,
            correlationId);

    private WorkerExecutionOutcome Unavailable(
        string diagnosticCode,
        Guid correlationId,
        WorkerExecutionDisposition disposition = WorkerExecutionDisposition.NotStarted) =>
        new(
            WorkerExecutionStatus.Unavailable,
            disposition,
            Execution: null,
            Current.Connection,
            diagnosticCode,
            Current.Generation,
            correlationId);

    private async Task<bool> ProbeIdleWorkerAsync(CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return true;
        try
        {
            if (Current.State is WorkerLifecycleState.Degraded or WorkerLifecycleState.Stopped or WorkerLifecycleState.Stopping)
                return false;
            if (Current.State != WorkerLifecycleState.Ready ||
                Volatile.Read(ref _activeExecutionCount) != 0 ||
                _executionQueue is { Reservations: > 0 } ||
                _timeProvider.GetElapsedTime(_lastSuccessfulExchange) < _policy.HeartbeatInterval)
                return true;

            cancellationToken.ThrowIfCancellationRequested();
            (bool healthy, string diagnosticCode, WorkerConnectionSnapshot? connection) probe;
            try
            {
                probe = await ProbeWorkerAsync().ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException &&
                (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
            {
                // An unexpected monitor failure must not silently leave a ready
                // generation without supervision. The controller owns retirement.
                await RetireWorkerAsync("worker-heartbeat-monitor-failed").ConfigureAwait(false);
                return false;
            }
            var (healthy, diagnosticCode, connection) = probe;
            if (!healthy)
                await RetireWorkerAsync(diagnosticCode, connection).ConfigureAwait(false);
            return healthy;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> StartWorkerAsync()
    {
        if (_processLifetime is not null)
            throw new InvalidOperationException("The previous worker has not been released.");
        _generation++;
        Transition(
            WorkerLifecycleState.Starting,
            processId: null,
            Current.LastTermination,
            "worker-starting");

        // Launch, COM activation, and Ready run under the startup bound only; a caller
        // that stops waiting cannot terminate the child it has asked for.
        WorkerControlMessage ready;
        using (var timeout = new CancellationTokenSource(_policy.StartupTimeout, _timeProvider))
        {
            try
            {
                var worker = await _processFactory.StartAsync(_generation, timeout.Token)
                    .ConfigureAwait(false);
                _processLifetime = new WorkerProcessLifetime(worker, _timeProvider);
                ready = await worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
                if (ready.Kind != WorkerControlMessageKind.Ready ||
                    ready.ProcessId is not > 0 ||
                    ready.Connection is null ||
                    !ExactTargetIdentityPolicy.IsWellFormed(
                        ready.Connection.RuntimeIdentity) ||
                    ready.Connection.State == WorkerConnectionState.Connected &&
                    ready.Connection.ExecutionReadinessState !=
                        WorkerExecutionReadinessState.Unverified)
                {
                    throw new InvalidDataException(
                        "The worker did not provide a valid ready message.");
                }
            }
            catch (OperationCanceledException)
            {
                // Only the startup bound can cancel this exchange.
                if (!await CleanupWorkerAsync(force: true).ConfigureAwait(false)) return false;
                Transition(
                    WorkerLifecycleState.Degraded,
                    processId: null,
                    WorkerTerminationKind.Forced,
                    "worker-startup-timeout",
                    incident: new WorkerIncidentSnapshot(_generation, WorkerTerminationKind.Forced,
                        null, null, "worker-startup-timeout", WorkerIncidentKind.StartFailed),
                    lifecycleFailure: WorkerLifecycleFailure.StartupTimeout);
                return false;
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                var termination = Worker?.HasExited == true
                    ? WorkerTerminationKind.Crash
                    : WorkerTerminationKind.Forced;
                if (!await CleanupWorkerAsync(force: true).ConfigureAwait(false)) return false;
                Transition(
                    WorkerLifecycleState.Degraded,
                    processId: null,
                    termination,
                    "worker-startup-failed",
                    incident: new WorkerIncidentSnapshot(_generation, termination,
                        null, null, "worker-startup-failed", WorkerIncidentKind.StartFailed),
                    lifecycleFailure: WorkerLifecycleFailure.StartupFailed);
                return false;
            }
        }

        _reportedProcessId = ready.ProcessId!.Value;
        var connection = ready.Connection!;
        if (connection.State == WorkerConnectionState.Faulted)
        {
            var diagnosticCode = connection.DiagnosticCode;
            if (!await CleanupWorkerAsync(force: true).ConfigureAwait(false)) return false;
            Transition(
                WorkerLifecycleState.Degraded,
                processId: null,
                WorkerTerminationKind.Forced,
                diagnosticCode,
                connection,
                new WorkerIncidentSnapshot(
                    Current.Generation,
                    WorkerTerminationKind.Forced,
                    ExecutionDisposition: null,
                    OperationId: null,
                    diagnosticCode, WorkerIncidentKind.StartFailed),
                lifecycleFailure: WorkerLifecycleFailure.StartupFailed);
            return false;
        }

        if (connection.State != WorkerConnectionState.Connected)
        {
            Transition(
                WorkerLifecycleState.Ready,
                _reportedProcessId,
                Current.LastTermination,
                "worker-ready-without-sdk",
                connection);
            return true;
        }

        if (!_identityPolicy.Evaluate(connection.RuntimeIdentity).AllowsExecution)
        {
            Transition(
                WorkerLifecycleState.Ready,
                _reportedProcessId,
                Current.LastTermination,
                "worker-ready-identity-not-ready",
                connection with
                {
                    ExecutionReadinessState = WorkerExecutionReadinessState.Unverified,
                    DiagnosticCode = "runtime-identity-not-ready",
                    TransitionedAt = _timeProvider.GetUtcNow()
                }, lifecycleFailure: WorkerLifecycleFailure.IdentityRejected);
            return true;
        }

        var verifying = connection with
        {
            ExecutionReadinessState = WorkerExecutionReadinessState.Verifying,
            DiagnosticCode = "execution-readiness-probe-started",
            TransitionedAt = _timeProvider.GetUtcNow()
        };
        Transition(
            WorkerLifecycleState.Starting,
            _reportedProcessId,
            Current.LastTermination,
            "execution-readiness-probe-started",
            verifying);
        return await VerifyWorkerExecutionAsync(connection)
            .ConfigureAwait(false);
    }

    private async Task<bool> VerifyWorkerExecutionAsync(
        WorkerConnectionSnapshot attachedConnection)
    {
        var worker = Worker ?? throw new InvalidOperationException("The worker is missing.");
        // The probe has its own bound; duration-class execution budgets never apply.
        // It belongs to an accepted exchange, so caller cancellation cannot quarantine.
        using var timeout = new CancellationTokenSource(_policy.ReadinessProbeTimeout, _timeProvider);
        var correlationId = Guid.NewGuid();
        try
        {
            await worker.SendAsync(
                WorkerControlMessage.VerifyExecution(correlationId),
                timeout.Token).ConfigureAwait(false);
            var response = await worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            if (response.Kind != WorkerControlMessageKind.ExecutionVerificationResult ||
                response.CorrelationId != correlationId ||
                response.Connection is null ||
                response.Connection.State != WorkerConnectionState.Connected ||
                response.Connection.RuntimeIdentity != attachedConnection.RuntimeIdentity ||
                !ExactTargetIdentityPolicy.IsWellFormed(
                    response.Connection.RuntimeIdentity))
            {
                throw new InvalidDataException(
                    "The worker returned an invalid execution-verification response.");
            }

            if (response.Connection.ExecutionReadinessState ==
                WorkerExecutionReadinessState.ExecutionReady)
            {
                Transition(
                    WorkerLifecycleState.Ready,
                    _reportedProcessId,
                    Current.LastTermination,
                    "worker-execution-ready",
                    response.Connection);
                return true;
            }

            await QuarantineExecutionTargetAsync(
                response.Connection,
                WorkerTerminationKind.Forced,
                response.Connection.DiagnosticCode,
                competingClientSuspected: false).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            // Only the probe bound can cancel this exchange.
            await QuarantineExecutionTargetAsync(
                attachedConnection,
                WorkerTerminationKind.Forced,
                "execution-readiness-probe-timeout",
                competingClientSuspected: true, failure: WorkerLifecycleFailure.ReadinessTimeout).ConfigureAwait(false);
            return false;
        }
        catch (Exception exception) when (IsRecoverableProcessFailure(exception))
        {
            var termination = worker.HasExited
                ? WorkerTerminationKind.Crash
                : WorkerTerminationKind.Forced;
            await QuarantineExecutionTargetAsync(
                attachedConnection,
                termination,
                worker.HasExited
                    ? "execution-readiness-worker-exited"
                    : "execution-readiness-control-failed",
                competingClientSuspected: true).ConfigureAwait(false);
            return false;
        }
    }

    private async Task QuarantineExecutionTargetAsync(
        WorkerConnectionSnapshot connection,
        WorkerTerminationKind termination,
        string diagnosticCode,
        bool competingClientSuspected,
        WorkerLifecycleFailure failure = WorkerLifecycleFailure.ReadinessFailed)
    {
        if (competingClientSuspected)
        {
            Transition(
                WorkerLifecycleState.Degraded,
                _reportedProcessId == 0 ? null : _reportedProcessId,
                termination,
                diagnosticCode,
                connection with
                {
                    ExecutionReadinessState =
                        WorkerExecutionReadinessState.CompetingClientSuspected,
                    DiagnosticCode = diagnosticCode,
                    TransitionedAt = _timeProvider.GetUtcNow()
                }, lifecycleFailure: failure);
        }

        if (!await CleanupWorkerAsync(force: true).ConfigureAwait(false)) return;
        Transition(
            WorkerLifecycleState.Degraded,
            processId: null,
            termination,
            diagnosticCode,
            connection with
            {
                ExecutionReadinessState =
                    WorkerExecutionReadinessState.OperatorRecoveryRequired,
                DiagnosticCode = diagnosticCode,
                TransitionedAt = _timeProvider.GetUtcNow()
            }, lifecycleFailure: failure);
    }

    private async Task<(bool Healthy, string DiagnosticCode, WorkerConnectionSnapshot? Connection)> ProbeWorkerAsync()
    {
        var worker = Worker;
        if (worker is null)
        {
            return (false, "worker-missing", null);
        }

        if (worker.HasExited)
        {
            return (false, "worker-exited", null);
        }

        // Let an entered ping/pong exchange finish under its own deadline. Cancelling it
        // from StopRuntimeLoopsAsync could leave a partial frame before Stop reuses the pipe.
        using var timeout = new CancellationTokenSource(_policy.HeartbeatTimeout, _timeProvider);
        var correlationId = Guid.NewGuid();
        try
        {
            await worker.SendAsync(
                WorkerControlMessage.Ping(correlationId),
                timeout.Token).ConfigureAwait(false);
            var response = await worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            if (response.Kind != WorkerControlMessageKind.Pong ||
                response.CorrelationId != correlationId ||
                response.Connection is null)
            {
                return (false, "worker-invalid-heartbeat", null);
            }

            if (response.Connection.State == WorkerConnectionState.Faulted &&
                RequiresSdkRecovery(response.Connection))
            {
                return (false, response.Connection.DiagnosticCode, response.Connection);
            }

            _lastSuccessfulExchange = _timeProvider.GetTimestamp();
            return (true, "worker-responsive", response.Connection);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return (false, "worker-heartbeat-timeout", null);
        }
        catch (Exception exception) when (IsRecoverableProcessFailure(exception))
        {
            return (false, worker.HasExited ? "worker-exited" : "worker-control-failed", null);
        }
    }

    private async Task RetireWorkerAsync(
        string diagnosticCode,
        WorkerConnectionSnapshot? connection = null,
        string? operationId = null,
        WorkerExecutionDisposition? executionDisposition = null,
        WorkerIncidentKind? incidentKind = null,
        WorkerLifecycleFailure lifecycleFailure = WorkerLifecycleFailure.None)
    {
        var termination = _processLifetime is { ReleaseStarted: false } && Worker?.HasExited == true
            ? WorkerTerminationKind.Crash
            : WorkerTerminationKind.Forced;
        var faultedConnection = connection ?? Current.Connection;
        if (faultedConnection is not null &&
            faultedConnection.State != WorkerConnectionState.Faulted)
        {
            faultedConnection = faultedConnection with
            {
                State = WorkerConnectionState.Faulted,
                ExecutionReadinessState = WorkerExecutionReadinessState.Unverified,
                DiagnosticCode = diagnosticCode,
                TransitionedAt = _timeProvider.GetUtcNow()
            };
        }

        Transition(
            WorkerLifecycleState.Degraded,
            _reportedProcessId == 0 ? null : _reportedProcessId,
            termination,
            diagnosticCode,
            faultedConnection,
            new WorkerIncidentSnapshot(
                Current.Generation,
                termination,
                executionDisposition,
                operationId,
                diagnosticCode, incidentKind ?? ClassifyIncident(termination, faultedConnection?.Failure)),
            lifecycleFailure: lifecycleFailure);
        await CleanupWorkerAsync(force: true).ConfigureAwait(false);
    }

    private async Task StopWorkerAsync()
    {
        if (_processLifetime is { ReleaseStarted: true } &&
            !await CleanupWorkerAsync(force: true).ConfigureAwait(false)) return;

        var worker = Worker;
        if (worker is null)
        {
            Transition(
                WorkerLifecycleState.Stopped,
                processId: null,
                Current.LastTermination,
                "worker-stopped");
            return;
        }

        var failure = WorkerLifecycleFailure.None;
        var termination = WorkerTerminationKind.Graceful;
        var diagnosticCode = "worker-stopped";
        if (worker.HasExited)
        {
            termination = WorkerTerminationKind.Crash;
            diagnosticCode = "worker-already-exited";
            failure = WorkerLifecycleFailure.StopFailed;
        }
        else
        {
            using var timeout = new CancellationTokenSource(_policy.ShutdownTimeout, _timeProvider);
            var correlationId = Guid.NewGuid();
            var stopPhase = WorkerStopPhase.Send;
            try
            {
                await worker.SendAsync(
                    WorkerControlMessage.Stop(correlationId),
                    timeout.Token).ConfigureAwait(false);
                stopPhase = WorkerStopPhase.Acknowledgement;
                var response = await worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
                if (response.Kind != WorkerControlMessageKind.Stopped ||
                    response.CorrelationId != correlationId)
                {
                    throw new InvalidDataException(
                        "The worker returned an invalid stop acknowledgement.");
                }

                stopPhase = WorkerStopPhase.ProcessExit;
                await worker.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                termination = WorkerTerminationKind.Forced;
                diagnosticCode = StopDiagnosticCode(stopPhase, "timeout");
                failure = WorkerLifecycleFailure.StopTimeout;
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                termination = worker.HasExited
                    ? WorkerTerminationKind.Crash
                    : WorkerTerminationKind.Forced;
                diagnosticCode = StopDiagnosticCode(stopPhase, "failed");
                failure = WorkerLifecycleFailure.StopFailed;
            }
        }

        if (!await CleanupWorkerAsync(force: termination != WorkerTerminationKind.Graceful)
            .ConfigureAwait(false)) return;
        Transition(
            WorkerLifecycleState.Stopped,
            processId: null,
            termination,
            diagnosticCode, lifecycleFailure: failure);
    }

    private async Task<bool> CleanupWorkerAsync(bool force)
    {
        var lifetime = _processLifetime;
        if (lifetime is null) return true;

        var status = await lifetime.ReleaseAsync(force, _policy.ShutdownTimeout).ConfigureAwait(false);
        if (status != WorkerCleanupStatus.Complete)
        {
            var diagnostic = status == WorkerCleanupStatus.ExitUnconfirmed
                ? "worker-termination-unconfirmed" : "worker-resources-unreleased";
            Transition(WorkerLifecycleState.Degraded,
                _reportedProcessId == 0 ? null : _reportedProcessId,
                WorkerTerminationKind.Forced, diagnostic,
                Current.Connection is { } connection ? connection with
                {
                    State = WorkerConnectionState.Faulted,
                    ExecutionReadinessState = WorkerExecutionReadinessState.OperatorRecoveryRequired,
                    DiagnosticCode = diagnostic,
                    TransitionedAt = _timeProvider.GetUtcNow()
                } : null, cleanupStatus: status, lifecycleFailure: WorkerLifecycleFailure.CleanupIncomplete);
            return false;
        }

        _processLifetime = null;
        _reportedProcessId = 0;
        return true;
    }

    private void Transition(
        WorkerLifecycleState state,
        int? processId,
        WorkerTerminationKind termination,
        string diagnosticCode,
        WorkerConnectionSnapshot? connection = null,
        WorkerIncidentSnapshot? incident = null,
        WorkerCleanupStatus? cleanupStatus = null,
        WorkerLifecycleFailure lifecycleFailure = WorkerLifecycleFailure.None)
    {
        var snapshot = new WorkerLifecycleSnapshot(
            state,
            _generation,
            processId,
            _recoveryCount,
            termination,
            diagnosticCode,
            connection,
            _timeProvider.GetUtcNow(),
            _identityPolicy.Evaluate(connection?.RuntimeIdentity),
            Interlocked.Increment(ref _stateRevision),
            incident ?? Current.LastIncident,
            AdmissionOpen: state == WorkerLifecycleState.Ready &&
                _executionQueue is { IsClosed: false } queue && queue.Generation == _generation,
            ApplicationGeneration: connection?.State == WorkerConnectionState.Connected
                ? (Current.Generation == _generation ? Current.ApplicationGeneration : null)
                : null,
            CleanupStatus: cleanupStatus, LifecycleFailure: lifecycleFailure);
        PublishSnapshot(snapshot);
    }

    private void PublishSnapshot(WorkerLifecycleSnapshot snapshot)
    {
        lock (_historyLock)
        {
            _current = snapshot;
            _history.Add(snapshot);
            var excess = _history.Count - _policy.LifecycleHistoryCapacity;
            if (excess > 0)
            {
                _history.RemoveRange(0, excess);
            }
        }
        LogWorkerTransition(
            snapshot.State == WorkerLifecycleState.Degraded ? LogLevel.Error :
                snapshot.Connection?.State == WorkerConnectionState.Connected &&
                snapshot.RuntimeIdentity?.AllowsExecution == false ? LogLevel.Warning : LogLevel.Information,
            snapshot.State,
            snapshot.Generation,
            snapshot.RecoveryCount,
            snapshot.LastTermination,
            snapshot.DiagnosticCode,
            snapshot.Connection?.State,
            snapshot.Connection?.ExecutionReadinessState,
            snapshot.Connection?.StatusCode,
            snapshot.RuntimeIdentity?.ActivatedSdk.Source,
            snapshot.RuntimeIdentity?.ActivatedSdk.MatchState,
            snapshot.RuntimeIdentity?.ConnectedSpatialAnalyzer.Source,
            snapshot.RuntimeIdentity?.ConnectedSpatialAnalyzer.MatchState);
    }
    [LoggerMessage(
        EventId = 1201,
        Message = "Worker transitioned to {WorkerState} at generation {Generation} with recovery count {RecoveryCount}, termination {Termination}, diagnostic {DiagnosticCode}, connection state {ConnectionState}, execution readiness {ExecutionReadinessState}, ConnectEx status {StatusCode}, activated SDK identity {ActivatedSdkIdentitySource}/{ActivatedSdkIdentityMatchState}, and connected SA identity {ConnectedSaIdentitySource}/{ConnectedSaIdentityMatchState}.")]
    private partial void LogWorkerTransition(
        LogLevel level,
        WorkerLifecycleState workerState,
        int generation,
        int recoveryCount,
        WorkerTerminationKind termination,
        string diagnosticCode,
        WorkerConnectionState? connectionState,
        WorkerExecutionReadinessState? executionReadinessState,
        int? statusCode,
        RuntimeIdentityEvidenceSource? activatedSdkIdentitySource,
        RuntimeIdentityMatchState? activatedSdkIdentityMatchState,
        RuntimeIdentityEvidenceSource? connectedSaIdentitySource,
        RuntimeIdentityMatchState? connectedSaIdentityMatchState);



    private static string GetExecutionNotReadyDiagnostic(
        WorkerLifecycleSnapshot snapshot)
    {
        if (!snapshot.AdmissionOpen) return "worker-admission-closed";
        if (snapshot.Connection?.State != WorkerConnectionState.Connected)
        {
            return "sdk-connection-not-ready";
        }

        if (snapshot.RuntimeIdentity?.AllowsExecution != true)
        {
            return "runtime-identity-not-ready";
        }

        return snapshot.Connection.DiagnosticCode;
    }

    private static WorkerIncidentKind ClassifyIncident(
        WorkerTerminationKind termination, WorkerConnectionFailure? failure) => failure switch
    {
        WorkerConnectionFailure.ActivationFailed or WorkerConnectionFailure.NotStarted => WorkerIncidentKind.StartFailed,
        WorkerConnectionFailure.ProcessExited => WorkerIncidentKind.SdkProcessExited,
        WorkerConnectionFailure.ConnectFailed or WorkerConnectionFailure.LivenessUnavailable => WorkerIncidentKind.SdkConnectionLost,
        _ => termination == WorkerTerminationKind.Crash
            ? WorkerIncidentKind.WorkerProcessExited : WorkerIncidentKind.ControlChannelLost
    };

    private static bool RequiresSdkRecovery(WorkerConnectionSnapshot connection) =>
        connection.Failure != WorkerConnectionFailure.None;

    [LoggerMessage(EventId = 1300, Level = LogLevel.Information,
        Message = "Dispatch {CorrelationId} operation {OperationId} on generation {Generation} with duration class {DurationClass} and execution budget {ExecutionBudgetMilliseconds} ms.")]
    private partial void LogExecutionDispatched(Guid correlationId, string operationId, int generation,
        OperationDurationClass durationClass, double executionBudgetMilliseconds);

    [LoggerMessage(EventId = 1301,
        Message = "Execution resolved {CorrelationId} operation {OperationId} generation {Generation}: disposition {ExecutionDisposition}, MP retrieved {MpResultRetrieved}, code {MpResultCode}, outcome {MpOutcome}, outputs {OutputRetrievalOutcome}, SDK {SdkDurationMilliseconds} ms, admission {AdmissionMilliseconds} ms, queue {QueueMilliseconds} ms, exchange {ExchangeMilliseconds} ms, replay {ReplaySafety}, diagnostic {DiagnosticCode}.")]
    private partial void LogExecutionResolved(LogLevel level, Guid correlationId, string operationId,
        int generation, string executionDisposition, bool? mpResultRetrieved, int? mpResultCode,
        string mpOutcome, string outputRetrievalOutcome, long? sdkDurationMilliseconds,
        double admissionMilliseconds, double queueMilliseconds, double exchangeMilliseconds,
        global::Briosa.ReplaySafety replaySafety, string diagnosticCode);

    private static bool IsRecoverableProcessFailure(Exception exception) =>
        exception is IOException or
            InvalidDataException or
            InvalidOperationException or
            ObjectDisposedException or
            Win32Exception or
            JsonException;

    private static string StopDiagnosticCode(WorkerStopPhase phase, string outcome) =>
        $"worker-stop-{phase switch
        {
            WorkerStopPhase.Send => "send",
            WorkerStopPhase.Acknowledgement => "ack",
            WorkerStopPhase.ProcessExit => "exit",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        }}-{outcome}";

    private enum WorkerStopPhase
    {
        Send,
        Acknowledgement,
        ProcessExit
    }
}
