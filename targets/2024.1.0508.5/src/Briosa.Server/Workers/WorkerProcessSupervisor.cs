using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Briosa.Server.Operations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Workers;

internal sealed partial class WorkerProcessSupervisor : IWorkerCommandExecutor, IWorkerStatusProvider, IAsyncDisposable
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
    private IWorkerProcess? _worker;
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
            TimeSpan.FromSeconds(30),
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
        Interlocked.Read(ref _workerFailureCount));

    public Task<bool> StartAsync(CancellationToken cancellationToken = default) =>
        RunLifecycle(StartCore, cancellationToken);

    public Task<bool> ConnectAsync(int expectedGeneration, CancellationToken cancellationToken = default) =>
        RunLifecycle(token => ConnectCore(expectedGeneration, token), cancellationToken);

    public Task<bool> RecoverSdkAsync(int expectedGeneration, CancellationToken cancellationToken = default) =>
        RunLifecycle(token => RecoverSdkCore(expectedGeneration, token), cancellationToken);

    private async Task<bool> RunLifecycle(Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task<bool> StartCore(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Current.State != WorkerLifecycleState.Stopped)
            {
                throw new InvalidOperationException("The worker supervisor is already active.");
            }

            _recoveryCount = 0;
            var started = await StartWorker(cancellationToken).ConfigureAwait(false);
            if (started)
            {
                StartRuntimeLoops();
            }

            return started;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> ConnectCore(
        int expectedGeneration,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = Current;
            if (expectedGeneration <= 0 || current.Generation != expectedGeneration)
            {
                throw new WorkerGenerationConflictException(
                    expectedGeneration,
                    current.Generation);
            }

            var worker = _worker;
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

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_policy.StartupTimeout);
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
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await RetireWorker(
                    "connect-ex-timeout",
                    connecting).ConfigureAwait(false);
                return false;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await RetireWorker(
                    "connect-ex-cancelled",
                    connecting).ConfigureAwait(false);
                throw;
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                await RetireWorker(
                    worker.HasExited
                        ? "worker-exited-during-connect"
                        : "sdk-connection-control-failed",
                    connecting).ConfigureAwait(false);
                return false;
            }

            var connection = response.Connection!;
            if (connection.State != WorkerConnectionState.Connected)
            {
                if (RequiresSdkRecovery(connection))
                {
                    await RetireWorker(
                        connection.DiagnosticCode,
                        connection).ConfigureAwait(false);
                }
                else
                {
                    Transition(
                        WorkerLifecycleState.Ready,
                        _reportedProcessId,
                        current.LastTermination,
                        connection.DiagnosticCode,
                        connection);
                }

                return false;
            }

            if (!_identityPolicy.Evaluate(connection.RuntimeIdentity).AllowsExecution)
            {
                Transition(
                    WorkerLifecycleState.Ready,
                    _reportedProcessId,
                    current.LastTermination,
                    "runtime-identity-not-ready",
                    connection with
                    {
                        ExecutionReadinessState = WorkerExecutionReadinessState.Unverified,
                        DiagnosticCode = "runtime-identity-not-ready",
                        TransitionedAt = _timeProvider.GetUtcNow()
                    });
                return false;
            }

            Transition(
                WorkerLifecycleState.Starting,
                _reportedProcessId,
                current.LastTermination,
                "execution-readiness-probe-started",
                connection with
                {
                    ExecutionReadinessState = WorkerExecutionReadinessState.Verifying,
                    DiagnosticCode = "execution-readiness-probe-started",
                    TransitionedAt = _timeProvider.GetUtcNow()
                });
            return await VerifyWorkerExecution(connection, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> RecoverSdkCore(
        int expectedGeneration,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = Current;
            if (expectedGeneration <= 0 || current.Generation != expectedGeneration)
            {
                throw new WorkerGenerationConflictException(
                    expectedGeneration,
                    current.Generation);
            }

            if (current.State != WorkerLifecycleState.Degraded)
            {
                throw new InvalidOperationException(
                    "SDK recovery is available only for a faulted generation.");
            }
        }
        finally
        {
            _gate.Release();
        }

        // Validation must succeed before admission or runtime loops are changed.
        // The lifecycle gate excludes competing start/stop/recovery transitions.
        await StopRuntimeLoops().ConfigureAwait(false);
        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await CleanupWorker(force: true).ConfigureAwait(false);
            _recoveryCount++;
            var started = await StartWorker(cancellationToken).ConfigureAwait(false);
            if (started)
            {
                StartRuntimeLoops();
            }

            return started;
        }
        finally
        {
            _gate.Release();
        }
    }


    public async Task AssociateApplicationGenerationAsync(int expectedGeneration,
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
                current.ApplicationGeneration == applicationGeneration) return;
            PublishSnapshot(current with
            {
                ApplicationGeneration = applicationGeneration,
                StateRevision = Interlocked.Increment(ref _stateRevision),
                TransitionedAt = _timeProvider.GetUtcNow()
            });
        }
        finally
        {
            _gate.Release();
        }
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
        if (!queue.TryReserve())
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
                Activity.Current?.Context ?? default);
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
            return Unavailable("worker-execution-queue-closed", effectiveCorrelationId) with { Generation = queue.Generation };
        }
        finally
        {
            if (!handedOff) queue.ReleaseReservation();
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
            return ClientCancelled(
                effectiveCorrelationId,
                WorkerExecutionDisposition.StartedOutcomeUnknown) with
            { Generation = item.Generation };
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopCore(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task StopCore(CancellationToken cancellationToken)
    {
        // Cancellation may abandon waiting to begin, but never leave an accepted
        // teardown half complete. Acquire ownership before closing admission.
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = Current;
            Transition(WorkerLifecycleState.Stopping, current.ProcessId,
                current.LastTermination, "worker-stopping", current.Connection);
        }
        finally
        {
            _gate.Release();
        }

        await StopRuntimeLoops().ConfigureAwait(false);

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await StopWorker().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task StopRuntimeLoops()
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

        await StopAsync().ConfigureAwait(false);
        _gate.Dispose();
        _lifecycleGate.Dispose();
    }


    private async Task ProcessExecutions(WorkerExecutionQueue queue)
    {
        var cancellationToken = queue.CancellationToken;
        try
        {
            await foreach (var item in queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await item.WaitUntilAdmitted().ConfigureAwait(false);
                Interlocked.Decrement(ref _queuedRequestCount);
                queue.ReleaseReservation();
                Interlocked.Increment(ref _activeExecutionCount);
                try
                {
                    WorkerExecutionOutcome outcome;
                    try
                    {
                        outcome = await ExecuteWorker(item, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _activeExecutionCount);
                    }
                    Complete(item, outcome);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    // The consumer owns resolution even if a programming or
                    // observer error escapes the exchange path. Never replay it.
                    await FailConsumer(queue, item).ConfigureAwait(false);
                    break;
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
                queue.ReleaseReservation();
                Complete(item, Unavailable("worker-execution-queue-closed", item.CorrelationId) with { Generation = item.Generation });
            }
        }
    }

    private async Task FailConsumer(WorkerExecutionQueue queue, ExecutionWorkItem item)
    {
        const string diagnostic = "worker-execution-consumer-failed";
        await queue.CloseAsync().ConfigureAwait(false);
        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (Current.Generation == queue.Generation)
            {
                await RetireWorker(diagnostic, operationId: item.Command.OperationId,
                    executionDisposition: item.ResolvedOutcome?.ExecutionDisposition ??
                        item.DispatchDisposition).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
            if (!item.Task.IsCompleted)
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
        var queue = new WorkerExecutionQueue(_generation, _executionPolicy.QueueCapacity);
        _executionQueue = queue;
        queue.Completion = ProcessExecutions(queue);
        _lastSuccessfulExchange = _timeProvider.GetTimestamp();
        _heartbeatMonitor = new WorkerHeartbeatMonitor(_policy.HeartbeatInterval, _timeProvider, ProbeIdleWorker);
        var current = Current;
        Transition(current.State, current.ProcessId, current.LastTermination,
            current.DiagnosticCode, current.Connection);
    }

    private async Task<WorkerExecutionOutcome> ExecuteWorker(
        ExecutionWorkItem item,
        CancellationToken cancellationToken)
    {
        var command = item.Command;
        var correlationId = item.CorrelationId;
        var acquired = false;
        var requestMayHaveStarted = false;
        var requestSent = false;
        long? exchangeStarted = null;
        Activity? exchange = null;
        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            item.QueueMilliseconds = _timeProvider.GetElapsedTime(item.AdmittedAt).TotalMilliseconds;
            _telemetry?.Queue(command.OperationId, item.QueueMilliseconds);
            using (var queueActivity = BriosaTelemetry.Activities.StartActivity(
                "briosa.queue", ActivityKind.Internal, item.ParentContext,
                startTime: item.AdmittedUtc))
            {
                queueActivity?.SetTag("briosa.operation", BriosaTelemetry.OperationId(command.OperationId));
            }
            var generation = Current.Generation;
            var worker = _worker;
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
            // cancellation after the request enters the channel.
            using var watchdog = new CancellationTokenSource(
                _executionPolicy.WatchdogTimeout);
            try
            {
                exchangeStarted = _timeProvider.GetTimestamp();
                exchange = BriosaTelemetry.Start("briosa.worker.exchange", command.OperationId, item.ParentContext);
                LogExecutionDispatched(correlationId, command.OperationId, generation);
                requestMayHaveStarted = true;
                item.DispatchDisposition = WorkerExecutionDisposition.StartedOutcomeUnknown;
                await worker.SendAsync(
                    WorkerControlMessage.Execute(correlationId, command),
                    watchdog.Token).ConfigureAwait(false);
                requestSent = true;
                var response = await worker.ReceiveAsync(watchdog.Token).ConfigureAwait(false);
                if (response.Kind != WorkerControlMessageKind.ExecutionResult ||
                    response.CorrelationId != correlationId ||
                    response.ExecutionResponse is null)
                {
                    throw new InvalidDataException(
                        "The worker returned an invalid execution response.");
                }

                var executionResponse = response.ExecutionResponse;
                if (!Enum.IsDefined(executionResponse.Status) ||
                    (executionResponse.Status == WorkerExecutionResponseStatus.Completed) !=
                    (executionResponse.Execution is not null) ||
                    executionResponse.Connection.RuntimeIdentity !=
                        Current.Connection?.RuntimeIdentity ||
                    executionResponse.Execution is WorkerMpResultAvailable { ResultCode: 2 } execution &&
                    !OutputsMatch(command.OutputArguments, execution.OutputValues))
                {
                    throw new InvalidDataException(
                        "The worker execution response has an invalid result shape.");
                }

                _lastSuccessfulExchange = _timeProvider.GetTimestamp();
                item.ResolvedOutcome = new WorkerExecutionOutcome(
                    executionResponse.Status == WorkerExecutionResponseStatus.Completed
                        ? WorkerExecutionStatus.Completed
                        : WorkerExecutionStatus.Unavailable,
                    executionResponse.Status == WorkerExecutionResponseStatus.Completed
                        ? ClassifyExecutionDisposition(executionResponse.Execution!)
                        : WorkerExecutionDisposition.NotStarted,
                    executionResponse.Execution,
                    executionResponse.Connection,
                    executionResponse.DiagnosticCode ??
                        executionResponse.Execution?.DiagnosticCode ??
                        "worker-execution-completed",
                    generation,
                    correlationId);
                return item.ResolvedOutcome;
            }
            catch (OperationCanceledException) when (watchdog.IsCancellationRequested)
            {
                await RetireWorker(
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
            catch (WorkerMessageRejectedException) when (!requestSent)
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
                await RetireWorker(
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

    private async Task<bool> ProbeIdleWorker(CancellationToken cancellationToken)
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
                probe = await ProbeWorker().ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException &&
                (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
            {
                // An unexpected monitor failure must not silently leave a ready
                // generation without supervision. The controller owns retirement.
                await RetireWorker("worker-heartbeat-monitor-failed").ConfigureAwait(false);
                return false;
            }
            var (healthy, diagnosticCode, connection) = probe;
            if (!healthy)
                await RetireWorker(diagnosticCode, connection).ConfigureAwait(false);
            return healthy;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> StartWorker(CancellationToken cancellationToken)
    {
        _generation++;
        Transition(
            WorkerLifecycleState.Starting,
            processId: null,
            Current.LastTermination,
            "worker-starting");

        WorkerControlMessage ready;
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(_policy.StartupTimeout);
            try
            {
                _worker = await _processFactory.StartAsync(_generation, timeout.Token)
                    .ConfigureAwait(false);
                ready = await _worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await CleanupWorker(force: true).ConfigureAwait(false);
                Transition(
                    WorkerLifecycleState.Stopped,
                    processId: null,
                    WorkerTerminationKind.Forced,
                    "worker-startup-cancelled");
                throw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await CleanupWorker(force: true).ConfigureAwait(false);
                Transition(
                    WorkerLifecycleState.Degraded,
                    processId: null,
                    WorkerTerminationKind.Forced,
                    "worker-startup-timeout",
                    incident: new WorkerIncidentSnapshot(_generation, WorkerTerminationKind.Forced,
                        null, null, "worker-startup-timeout", WorkerIncidentKind.StartFailed));
                return false;
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                var termination = _worker?.HasExited == true
                    ? WorkerTerminationKind.Crash
                    : WorkerTerminationKind.Forced;
                await CleanupWorker(force: true).ConfigureAwait(false);
                Transition(
                    WorkerLifecycleState.Degraded,
                    processId: null,
                    termination,
                    "worker-startup-failed",
                    incident: new WorkerIncidentSnapshot(_generation, termination,
                        null, null, "worker-startup-failed", WorkerIncidentKind.StartFailed));
                return false;
            }
        }

        _reportedProcessId = ready.ProcessId!.Value;
        var connection = ready.Connection!;
        if (connection.State == WorkerConnectionState.Faulted)
        {
            var diagnosticCode = connection.DiagnosticCode;
            await CleanupWorker(force: true).ConfigureAwait(false);
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
                    diagnosticCode, WorkerIncidentKind.StartFailed));
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
                });
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
        return await VerifyWorkerExecution(connection, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> VerifyWorkerExecution(
        WorkerConnectionSnapshot attachedConnection,
        CancellationToken cancellationToken)
    {
        var worker = _worker ?? throw new InvalidOperationException("The worker is missing.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_executionPolicy.WatchdogTimeout);
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

            await QuarantineExecutionTarget(
                response.Connection,
                WorkerTerminationKind.Forced,
                response.Connection.DiagnosticCode,
                competingClientSuspected: false).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await QuarantineExecutionTarget(
                attachedConnection,
                WorkerTerminationKind.Forced,
                "execution-readiness-probe-timeout",
                competingClientSuspected: true).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await QuarantineExecutionTarget(
                attachedConnection,
                WorkerTerminationKind.Forced,
                "execution-readiness-probe-cancelled",
                competingClientSuspected: true).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception) when (IsRecoverableProcessFailure(exception))
        {
            var termination = worker.HasExited
                ? WorkerTerminationKind.Crash
                : WorkerTerminationKind.Forced;
            await QuarantineExecutionTarget(
                attachedConnection,
                termination,
                worker.HasExited
                    ? "execution-readiness-worker-exited"
                    : "execution-readiness-control-failed",
                competingClientSuspected: true).ConfigureAwait(false);
            return false;
        }
    }

    private async Task QuarantineExecutionTarget(
        WorkerConnectionSnapshot connection,
        WorkerTerminationKind termination,
        string diagnosticCode,
        bool competingClientSuspected)
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
                });
        }

        await CleanupWorker(force: true).ConfigureAwait(false);
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
            });
    }

    private async Task<(bool Healthy, string DiagnosticCode, WorkerConnectionSnapshot? Connection)> ProbeWorker()
    {
        var worker = _worker;
        if (worker is null)
        {
            return (false, "worker-missing", null);
        }

        if (worker.HasExited)
        {
            return (false, "worker-exited", null);
        }

        // Let an entered ping/pong exchange finish under its own deadline. Cancelling it
        // from StopRuntimeLoops could leave a partial frame before Stop reuses the pipe.
        using var timeout = new CancellationTokenSource(_policy.HeartbeatTimeout);
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

    private async Task RetireWorker(
        string diagnosticCode,
        WorkerConnectionSnapshot? connection = null,
        string? operationId = null,
        WorkerExecutionDisposition? executionDisposition = null,
        WorkerIncidentKind? incidentKind = null)
    {
        var termination = _worker?.HasExited == true
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
                diagnosticCode, incidentKind ?? ClassifyIncident(termination, faultedConnection?.Failure)));
        await CleanupWorker(force: true).ConfigureAwait(false);
    }

    private async Task StopWorker()
    {
        var worker = _worker;
        if (worker is null)
        {
            Transition(
                WorkerLifecycleState.Stopped,
                processId: null,
                Current.LastTermination,
                "worker-stopped");
            return;
        }

        var termination = WorkerTerminationKind.Graceful;
        var diagnosticCode = "worker-stopped";
        if (worker.HasExited)
        {
            termination = WorkerTerminationKind.Crash;
            diagnosticCode = "worker-already-exited";
        }
        else
        {
            using var timeout = new CancellationTokenSource(_policy.ShutdownTimeout);
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
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                termination = worker.HasExited
                    ? WorkerTerminationKind.Crash
                    : WorkerTerminationKind.Forced;
                diagnosticCode = StopDiagnosticCode(stopPhase, "failed");
            }
        }

        await CleanupWorker(force: termination != WorkerTerminationKind.Graceful)
            .ConfigureAwait(false);
        Transition(
            WorkerLifecycleState.Stopped,
            processId: null,
            termination,
            diagnosticCode);
    }

    private async Task CleanupWorker(bool force)
    {
        var worker = _worker;
        _worker = null;
        _reportedProcessId = 0;
        if (worker is null)
        {
            return;
        }

        if (force)
        {
            await worker.TerminateAsync().ConfigureAwait(false);
        }

        await worker.DisposeAsync().ConfigureAwait(false);
    }

    private void Transition(
        WorkerLifecycleState state,
        int? processId,
        WorkerTerminationKind termination,
        string diagnosticCode,
        WorkerConnectionSnapshot? connection = null,
        WorkerIncidentSnapshot? incident = null)
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
                : null);
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



    private static bool OutputsMatch(
        IReadOnlyList<WorkerMpOutputArgument> requested,
        IReadOnlyList<WorkerMpOutputValue> returned) =>
        requested.Count == returned.Count &&
        requested.Zip(returned).All(pair =>
            pair.First.Name == pair.Second.Name &&
            pair.First.Kind == pair.Second.Kind);

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

    private static WorkerExecutionDisposition ClassifyExecutionDisposition(
        WorkerMpExecutionResult execution) =>
        execution is WorkerArgumentsRejected
            ? WorkerExecutionDisposition.NotStarted
            : execution.MpResultRetrieved
                ? WorkerExecutionDisposition.Completed
                : WorkerExecutionDisposition.StartedOutcomeUnknown;
    [LoggerMessage(EventId = 1300, Level = LogLevel.Information,
        Message = "Dispatch {CorrelationId} operation {OperationId} on generation {Generation}.")]
    private partial void LogExecutionDispatched(Guid correlationId, string operationId, int generation);

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
