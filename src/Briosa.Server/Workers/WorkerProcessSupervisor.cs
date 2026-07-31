using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Channels;
using Briosa.Server.Operations;
using Briosa.Worker.Control;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Workers;

internal sealed partial class WorkerProcessSupervisor : IWorkerCommandExecutor, IWorkerStatusProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<WorkerLifecycleSnapshot> _history = [];
    private readonly Lock _historyLock = new();
    private readonly WorkerExecutionPolicy _executionPolicy;
    private readonly ExactTargetIdentityPolicy _identityPolicy;
    private readonly IWorkerProcessFactory _processFactory;
    private readonly WorkerRestartPolicy _policy;
    private readonly Queue<DateTimeOffset> _restartTimes = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WorkerProcessSupervisor> _logger;
    [SuppressMessage(
        "Reliability",
        "CA2213:Disposable fields should be disposed",
        Justification = "ExecuteAsync callers capture the generation-scoped source while waiting for admission. Disposal can race creation of their linked cancellation source; cancellation plus garbage collection safely retires it because Briosa never requests its wait handle.")]
    private CancellationTokenSource? _executionCancellation;
    private Channel<ExecutionWorkItem>? _executionQueue;
    [SuppressMessage(
        "Reliability",
        "CA2213:Disposable fields should be disposed",
        Justification = "A generation-scoped semaphore can still be observed by ExecuteAsync callers after runtime-loop shutdown; disposing it would race those callers. SemaphoreSlim allocates no wait handle unless AvailableWaitHandle is requested, which Briosa never does, and the retired instance is reclaimed after those callers return.")]
    private SemaphoreSlim? _executionQueueSlots;
    private Task? _executionTask;
    private CancellationTokenSource? _monitorCancellation;
    private Task? _monitorTask;
    private IWorkerProcess? _worker;
    private WorkerLifecycleSnapshot _current;
    private int _generation;
    private int _reportedProcessId;
    private int _restartCount;
    private int _disposeState;
    private int _queuedRequestCount;
    private int _admissionWaiterCount;
    private int _activeExecutionCount;
    private int _peakQueuedRequestCount;
    private long _admittedRequestCount;
    private long _terminalRequestCount;
    private long _clientCancellationBeforeAdmissionCount;
    private long _clientCancellationAfterAdmissionCount;
    private long _watchdogTimeoutCount;
    private long _workerFailureCount;

    public WorkerProcessSupervisor(
        IWorkerProcessFactory processFactory,
        WorkerRestartPolicy policy,
        WorkerExecutionPolicy? executionPolicy = null,
        TimeProvider? timeProvider = null,
        ILogger<WorkerProcessSupervisor>? logger = null,
        ExactTargetIdentityPolicy? identityPolicy = null)
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
        _current = new WorkerLifecycleSnapshot(
            WorkerLifecycleState.Stopped,
            Generation: 0,
            ProcessId: null,
            RestartCount: 0,
            WorkerTerminationKind.None,
            "not-started",
            Connection: null,
            _timeProvider.GetUtcNow());
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
        Volatile.Read(ref _admissionWaiterCount),
        Volatile.Read(ref _activeExecutionCount),
        Volatile.Read(ref _peakQueuedRequestCount),
        Interlocked.Read(ref _admittedRequestCount),
        Interlocked.Read(ref _terminalRequestCount),
        Interlocked.Read(ref _clientCancellationBeforeAdmissionCount),
        Interlocked.Read(ref _clientCancellationAfterAdmissionCount),
        Interlocked.Read(ref _watchdogTimeoutCount),
        Interlocked.Read(ref _workerFailureCount));

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Current.State != WorkerLifecycleState.Stopped)
            {
                throw new InvalidOperationException("The worker supervisor is already active.");
            }

            _restartTimes.Clear();
            _restartCount = 0;
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

    public async Task<bool> RecoverExecutionAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
        if (Current.State != WorkerLifecycleState.Degraded ||
            Current.Connection?.ExecutionReadinessState !=
                WorkerExecutionReadinessState.OperatorRecoveryRequired)
        {
            throw new InvalidOperationException(
                "Execution recovery is available only while the SpatialAnalyzer target is quarantined.");
        }

        await StopRuntimeLoops().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Current.State != WorkerLifecycleState.Degraded ||
                Current.Connection?.ExecutionReadinessState !=
                    WorkerExecutionReadinessState.OperatorRecoveryRequired)
            {
                throw new InvalidOperationException(
                    "Execution recovery is available only while the SpatialAnalyzer target is quarantined.");
            }

            _restartTimes.Clear();
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


    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(command, Guid.NewGuid(), cancellationToken);

    public async Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
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
        var queueSlots = _executionQueueSlots;
        var executionCancellation = _executionCancellation;
        if (queue is null || queueSlots is null || executionCancellation is null ||
            Current.State != WorkerLifecycleState.Ready ||
            Current.RuntimeIdentity?.AllowsExecution != true)
        {
            return Unavailable(
                Current.State == WorkerLifecycleState.Ready
                    ? "runtime-identity-not-ready"
                    : "worker-not-ready",
                effectiveCorrelationId);
        }

        var item = new ExecutionWorkItem(command, effectiveCorrelationId);
        try
        {
            using var admissionCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    executionCancellation.Token);
            Interlocked.Increment(ref _admissionWaiterCount);
            try
            {
                await queueSlots.WaitAsync(admissionCancellation.Token).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _admissionWaiterCount);
            }
            if (!queue.Writer.TryWrite(item))
            {
                queueSlots.Release();
                return Unavailable(
                    "worker-execution-queue-closed",
                    effectiveCorrelationId);
            }

            MarkAdmitted(item);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _clientCancellationBeforeAdmissionCount);
            return ClientCancelled(
                effectiveCorrelationId,
                WorkerExecutionDisposition.NotStarted);
        }
        catch (OperationCanceledException) when (executionCancellation.IsCancellationRequested)
        {
            return Unavailable("worker-execution-queue-closed", effectiveCorrelationId);
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
                WorkerExecutionDisposition.StartedOutcomeUnknown);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await StopRuntimeLoops().ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
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
        var executionCancellation = _executionCancellation;
        var executionQueue = _executionQueue;
        var executionTask = _executionTask;
        _executionCancellation = null;
        _executionQueue = null;
        _executionQueueSlots = null;
        _executionTask = null;
        executionQueue?.Writer.TryComplete();
        if (executionCancellation is not null)
        {
            await executionCancellation.CancelAsync().ConfigureAwait(false);
        }

        var monitorCancellation = _monitorCancellation;
        var monitorTask = _monitorTask;
        if (monitorCancellation is not null)
        {
            await monitorCancellation.CancelAsync().ConfigureAwait(false);
        }
        if (monitorTask is not null)
        {
            try
            {
                await monitorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
        if (executionTask is not null)
        {
            await executionTask.ConfigureAwait(false);
        }

        _monitorTask = null;
        _monitorCancellation = null;
        monitorCancellation?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        await StopAsync().ConfigureAwait(false);
        _gate.Dispose();
    }


    private async Task ProcessExecutions(
        ChannelReader<ExecutionWorkItem> reader,
        SemaphoreSlim queueSlots,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await item.WaitUntilAdmitted().ConfigureAwait(false);
                Interlocked.Decrement(ref _queuedRequestCount);
                queueSlots.Release();
                Interlocked.Increment(ref _activeExecutionCount);
                WorkerExecutionOutcome outcome;
                try
                {
                    outcome = await ExecuteWorker(
                        item.Command,
                        item.CorrelationId,
                        cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeExecutionCount);
                }

                Complete(item, outcome);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            while (reader.TryRead(out var item))
            {
                await item.WaitUntilAdmitted().ConfigureAwait(false);
                Interlocked.Decrement(ref _queuedRequestCount);
                queueSlots.Release();
                Complete(
                    item,
                    Unavailable("worker-supervisor-stopping", item.CorrelationId));
            }
        }
    }

    private void MarkAdmitted(ExecutionWorkItem item)
    {
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

        item.TrySetResult(outcome);
    }

    private void StartRuntimeLoops()
    {
        _executionCancellation = new CancellationTokenSource();
        _executionQueue = Channel.CreateBounded<ExecutionWorkItem>(
            new BoundedChannelOptions(_executionPolicy.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        _executionQueueSlots = new SemaphoreSlim(
            _executionPolicy.QueueCapacity,
            _executionPolicy.QueueCapacity);
        _executionTask = ProcessExecutions(
            _executionQueue.Reader,
            _executionQueueSlots,
            _executionCancellation.Token);
        _monitorCancellation = new CancellationTokenSource();
        _monitorTask = MonitorWorker(_monitorCancellation.Token);
    }

    private async Task<WorkerExecutionOutcome> ExecuteWorker(
        WorkerMpCommand command,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var acquired = false;
        var requestMayHaveStarted = false;
        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            var generation = Current.Generation;
            var worker = _worker;
            if (Current.State != WorkerLifecycleState.Ready || worker is null ||
                Current.RuntimeIdentity?.AllowsExecution != true)
            {
                return Unavailable(
                    Current.State == WorkerLifecycleState.Ready && worker is not null
                        ? "runtime-identity-not-ready"
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
                requestMayHaveStarted = true;
                await worker.SendAsync(
                    WorkerControlMessage.Execute(correlationId, command),
                    watchdog.Token).ConfigureAwait(false);
                var response = await worker.ReceiveAsync(watchdog.Token).ConfigureAwait(false);
                if (response.Kind != WorkerControlMessageKind.ExecutionResult ||
                    response.CorrelationId != correlationId ||
                    response.ExecutionResponse is null)
                {
                    throw new InvalidDataException(
                        "The worker returned an invalid execution response.");
                }

                var executionResponse = response.ExecutionResponse;
                if ((executionResponse.Status == WorkerExecutionResponseStatus.Completed) !=
                    (executionResponse.Execution is not null) ||
                    executionResponse.Connection.RuntimeIdentity !=
                        Current.Connection?.RuntimeIdentity ||
                    executionResponse.Execution is { ExecuteStepReturned: true, MpSucceeded: true } execution &&
                    !OutputsMatch(command.OutputArguments, execution.OutputValues))
                {
                    throw new InvalidDataException(
                        "The worker execution response has an invalid result shape.");
                }

                return new WorkerExecutionOutcome(
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
            }
            catch (OperationCanceledException) when (watchdog.IsCancellationRequested)
            {
                _ = await RecoverWorker(
                    "worker-execution-watchdog-timeout",
                    cancellationToken).ConfigureAwait(false);
                return new WorkerExecutionOutcome(
                    WorkerExecutionStatus.WatchdogTimeout,
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    Execution: null,
                    Connection: null,
                    "worker-execution-watchdog-timeout",
                    generation,
                    correlationId);
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                var diagnosticCode = worker.HasExited
                    ? "worker-exited-during-execution"
                    : "worker-execution-control-failed";
                _ = await RecoverWorker(diagnosticCode, cancellationToken)
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

    private async Task MonitorWorker(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await Task.Delay(
                    _policy.HeartbeatInterval,
                    _timeProvider,
                    cancellationToken).ConfigureAwait(false);
                await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (Current.State != WorkerLifecycleState.Ready)
                    {
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var (healthy, diagnosticCode) = await ProbeWorker().ConfigureAwait(false);
                    if (!healthy &&
                        !await RecoverWorker(diagnosticCode, cancellationToken)
                            .ConfigureAwait(false))
                    {
                        return;
                    }
                }
                finally
                {
                    _gate.Release();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await CleanupWorker(force: true).ConfigureAwait(false);
                Transition(
                    WorkerLifecycleState.Degraded,
                    processId: null,
                    WorkerTerminationKind.Forced,
                    "worker-startup-timeout");
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
                    "worker-startup-failed");
                return false;
            }
        }

        _reportedProcessId = ready.ProcessId!.Value;
        var connection = ready.Connection!;
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

    private async Task<(bool Healthy, string DiagnosticCode)> ProbeWorker()
    {
        var worker = _worker;
        if (worker is null)
        {
            return (false, "worker-missing");
        }

        if (worker.HasExited)
        {
            return (false, "worker-exited");
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
            return response.Kind == WorkerControlMessageKind.Pong &&
                response.CorrelationId == correlationId
                    ? (true, "worker-responsive")
                    : (false, "worker-invalid-heartbeat");
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return (false, "worker-heartbeat-timeout");
        }
        catch (Exception exception) when (IsRecoverableProcessFailure(exception))
        {
            return (false, worker.HasExited ? "worker-exited" : "worker-control-failed");
        }
    }

    private async Task<bool> RecoverWorker(
        string diagnosticCode,
        CancellationToken cancellationToken)
    {
        await RetireWorker(diagnosticCode).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        while (TryRecordRestart())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (_policy.RestartDelay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(
                        _policy.RestartDelay,
                        _timeProvider,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            }

            try
            {
                if (await StartWorker(cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                if (_worker is not null)
                {
                    await RetireWorker("worker-restart-cancelled").ConfigureAwait(false);
                }

                return false;
            }

            if (Current.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.OperatorRecoveryRequired)
            {
                return false;
            }
        }

        Transition(
            WorkerLifecycleState.Degraded,
            processId: null,
            Current.LastTermination,
            "restart-budget-exhausted");
        return false;
    }

    private async Task RetireWorker(string diagnosticCode)
    {
        var termination = _worker?.HasExited == true
            ? WorkerTerminationKind.Crash
            : WorkerTerminationKind.Forced;
        Transition(
            WorkerLifecycleState.Degraded,
            _reportedProcessId == 0 ? null : _reportedProcessId,
            termination,
            diagnosticCode);
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

    private bool TryRecordRestart()
    {
        var now = _timeProvider.GetUtcNow();
        while (_restartTimes.TryPeek(out var first) &&
            now - first >= _policy.RestartWindow)
        {
            _restartTimes.Dequeue();
        }

        if (_restartTimes.Count >= _policy.MaximumRestarts)
        {
            return false;
        }

        _restartTimes.Enqueue(now);
        _restartCount++;
        return true;
    }

    private void Transition(
        WorkerLifecycleState state,
        int? processId,
        WorkerTerminationKind termination,
        string diagnosticCode,
        WorkerConnectionSnapshot? connection = null)
    {
        var snapshot = new WorkerLifecycleSnapshot(
            state,
            _generation,
            processId,
            _restartCount,
            termination,
            diagnosticCode,
            connection,
            _timeProvider.GetUtcNow(),
            _identityPolicy.Evaluate(connection?.RuntimeIdentity));
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
            snapshot.State,
            snapshot.Generation,
            snapshot.RestartCount,
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
        Level = LogLevel.Information,
        Message = "Worker transitioned to {WorkerState} at generation {Generation} with restart count {RestartCount}, termination {Termination}, diagnostic {DiagnosticCode}, connection state {ConnectionState}, execution readiness {ExecutionReadinessState}, ConnectEx status {StatusCode}, activated SDK identity {ActivatedSdkIdentitySource}/{ActivatedSdkIdentityMatchState}, and connected SA identity {ConnectedSaIdentitySource}/{ConnectedSaIdentityMatchState}.")]
    private partial void LogWorkerTransition(
        WorkerLifecycleState workerState,
        int generation,
        int restartCount,
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

    private static WorkerExecutionDisposition ClassifyExecutionDisposition(
        WorkerMpExecutionResult execution) =>
        execution.DiagnosticCode == "sdk-argument-rejected"
            ? WorkerExecutionDisposition.NotStarted
            : execution.MpResultRetrieved
                ? WorkerExecutionDisposition.Completed
                : WorkerExecutionDisposition.StartedOutcomeUnknown;
    private sealed class ExecutionWorkItem(WorkerMpCommand command, Guid correlationId)
    {
        private readonly TaskCompletionSource _admitted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<WorkerExecutionOutcome> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkerMpCommand Command { get; } = new(
            command.OperationId,
            command.StepName,
            [.. command.InputArguments],
            [.. command.OutputArguments]);
        public Guid CorrelationId { get; } = correlationId;


        public Task<WorkerExecutionOutcome> Task => _completion.Task;

        public Task WaitUntilAdmitted() => _admitted.Task;

        public void MarkAdmitted() => _admitted.TrySetResult();

        public void TrySetResult(WorkerExecutionOutcome outcome) =>
            _completion.TrySetResult(outcome);
    }

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
