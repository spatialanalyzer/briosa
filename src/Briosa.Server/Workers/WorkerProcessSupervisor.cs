using System.ComponentModel;
using System.Text.Json;
using System.Threading.Channels;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class WorkerProcessSupervisor : IWorkerCommandExecutor, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<WorkerLifecycleSnapshot> _history = [];
    private readonly Lock _historyLock = new();
    private readonly WorkerExecutionPolicy _executionPolicy;
    private readonly IWorkerProcessFactory _processFactory;
    private readonly WorkerRestartPolicy _policy;
    private readonly Queue<DateTimeOffset> _restartTimes = new();
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _executionCancellation;
    private Channel<ExecutionWorkItem>? _executionQueue;
    private Task? _executionTask;
    private CancellationTokenSource? _monitorCancellation;
    private Task? _monitorTask;
    private IWorkerProcess? _worker;
    private WorkerLifecycleSnapshot _current;
    private int _generation;
    private int _reportedProcessId;
    private int _restartCount;
    private int _disposeState;

    public WorkerProcessSupervisor(
        IWorkerProcessFactory processFactory,
        WorkerRestartPolicy policy,
        WorkerExecutionPolicy? executionPolicy = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(policy);
        _processFactory = processFactory;
        _policy = policy;
        _executionPolicy = executionPolicy ?? new WorkerExecutionPolicy(
            TimeSpan.FromSeconds(30),
            queueCapacity: 64);
        _timeProvider = timeProvider ?? TimeProvider.System;
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
                _executionCancellation = new CancellationTokenSource();
                _executionQueue = Channel.CreateBounded<ExecutionWorkItem>(
                    new BoundedChannelOptions(_executionPolicy.QueueCapacity)
                    {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                        SingleWriter = false
                    });
                _executionTask = ProcessExecutions(
                    _executionQueue.Reader,
                    _executionCancellation.Token);
                _monitorCancellation = new CancellationTokenSource();
                _monitorTask = MonitorWorker(_monitorCancellation.Token);
            }

            return started;
        }
        finally
        {
            _gate.Release();
        }
    }


    public async Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (cancellationToken.IsCancellationRequested)
        {
            return ClientCancelled();
        }

        var queue = _executionQueue;
        if (queue is null || Current.State != WorkerLifecycleState.Ready)
        {
            return Unavailable("worker-not-ready");
        }

        var item = new ExecutionWorkItem(command);
        try
        {
            await queue.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ClientCancelled();
        }
        catch (ChannelClosedException)
        {
            return Unavailable("worker-execution-queue-closed");
        }

        try
        {
            return await item.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ClientCancelled();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var executionCancellation = _executionCancellation;
        var executionTask = _executionTask;
        _executionCancellation = null;
        _executionQueue = null;
        _executionTask = null;
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

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopWorker().ConfigureAwait(false);
            _monitorTask = null;
            _monitorCancellation = null;
            monitorCancellation?.Dispose();
            executionCancellation?.Dispose();
        }
        finally
        {
            _gate.Release();
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
    }


    private async Task ProcessExecutions(
        ChannelReader<ExecutionWorkItem> reader,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var outcome = await ExecuteWorker(item.Command, cancellationToken)
                    .ConfigureAwait(false);
                item.TrySetResult(outcome);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            while (reader.TryRead(out var item))
            {
                item.TrySetResult(Unavailable("worker-supervisor-stopping"));
            }
        }
    }

    private async Task<WorkerExecutionOutcome> ExecuteWorker(
        WorkerMpCommand command,
        CancellationToken cancellationToken)
    {
        var acquired = false;
        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            var generation = Current.Generation;
            var worker = _worker;
            if (Current.State != WorkerLifecycleState.Ready || worker is null)
            {
                return Unavailable("worker-not-ready");
            }

            using var watchdog = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            watchdog.CancelAfter(_executionPolicy.WatchdogTimeout);
            var correlationId = Guid.NewGuid();
            try
            {
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
                    executionResponse.Execution,
                    executionResponse.Connection,
                    executionResponse.DiagnosticCode ??
                        executionResponse.Execution?.DiagnosticCode ??
                        "worker-execution-completed",
                    generation);
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested)
            {
                _ = await RecoverWorker(
                    "worker-execution-watchdog-timeout",
                    cancellationToken).ConfigureAwait(false);
                return new WorkerExecutionOutcome(
                    WorkerExecutionStatus.WatchdogTimeout,
                    Execution: null,
                    Connection: null,
                    "worker-execution-watchdog-timeout",
                    generation);
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
                    Execution: null,
                    Connection: null,
                    diagnosticCode,
                    generation);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Unavailable("worker-supervisor-stopping");
        }
        finally
        {
            if (acquired)
            {
                _gate.Release();
            }
        }
    }

    private WorkerExecutionOutcome ClientCancelled() =>
        new(
            WorkerExecutionStatus.ClientCancelled,
            Execution: null,
            Current.Connection,
            "client-wait-cancelled",
            Current.Generation);

    private WorkerExecutionOutcome Unavailable(string diagnosticCode) =>
        new(
            WorkerExecutionStatus.Unavailable,
            Execution: null,
            Current.Connection,
            diagnosticCode,
            Current.Generation);

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

                    var (healthy, diagnosticCode) = await ProbeWorker(cancellationToken).ConfigureAwait(false);
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

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_policy.StartupTimeout);
        try
        {
            _worker = await _processFactory.StartAsync(_generation, timeout.Token)
                .ConfigureAwait(false);
            var ready = await _worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            if (ready.Kind != WorkerControlMessageKind.Ready ||
                ready.ProcessId is not > 0 ||
                ready.Connection is null)
            {
                throw new InvalidDataException("The worker did not provide a valid ready message.");
            }

            _reportedProcessId = ready.ProcessId.Value;
            Transition(
                WorkerLifecycleState.Ready,
                _reportedProcessId,
                Current.LastTermination,
                "worker-ready",
                ready.Connection);
            return true;
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

    private async Task<(bool Healthy, string DiagnosticCode)> ProbeWorker(
        CancellationToken cancellationToken)
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

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_policy.HeartbeatTimeout);
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
        catch (OperationCanceledException) when (
            !cancellationToken.IsCancellationRequested)
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
        var termination = _worker?.HasExited == true
            ? WorkerTerminationKind.Crash
            : WorkerTerminationKind.Forced;
        Transition(
            WorkerLifecycleState.Degraded,
            _reportedProcessId == 0 ? null : _reportedProcessId,
            termination,
            diagnosticCode);
        await CleanupWorker(force: true).ConfigureAwait(false);

        while (TryRecordRestart())
        {
            if (_policy.RestartDelay > TimeSpan.Zero)
            {
                await Task.Delay(
                    _policy.RestartDelay,
                    _timeProvider,
                    cancellationToken).ConfigureAwait(false);
            }

            if (await StartWorker(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        Transition(
            WorkerLifecycleState.Degraded,
            processId: null,
            termination,
            "restart-budget-exhausted");
        return false;
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
            try
            {
                await worker.SendAsync(
                    WorkerControlMessage.Stop(correlationId),
                    timeout.Token).ConfigureAwait(false);
                var response = await worker.ReceiveAsync(timeout.Token).ConfigureAwait(false);
                if (response.Kind != WorkerControlMessageKind.Stopped ||
                    response.CorrelationId != correlationId)
                {
                    throw new InvalidDataException(
                        "The worker returned an invalid stop acknowledgement.");
                }

                await worker.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                termination = WorkerTerminationKind.Forced;
                diagnosticCode = "worker-stop-timeout";
            }
            catch (Exception exception) when (IsRecoverableProcessFailure(exception))
            {
                termination = worker.HasExited
                    ? WorkerTerminationKind.Crash
                    : WorkerTerminationKind.Forced;
                diagnosticCode = "worker-stop-failed";
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
            _timeProvider.GetUtcNow());
        lock (_historyLock)
        {
            _current = snapshot;
            _history.Add(snapshot);
        }
    }


    private static bool OutputsMatch(
        IReadOnlyList<WorkerMpOutputArgument> requested,
        IReadOnlyList<WorkerMpOutputValue> returned) =>
        requested.Count == returned.Count &&
        requested.Zip(returned).All(pair =>
            pair.First.Name == pair.Second.Name &&
            pair.First.Kind == pair.Second.Kind);
    private sealed class ExecutionWorkItem(WorkerMpCommand command)
    {
        private readonly TaskCompletionSource<WorkerExecutionOutcome> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkerMpCommand Command { get; } = new(
            command.OperationId,
            command.StepName,
            [.. command.InputArguments],
            [.. command.OutputArguments]);

        public Task<WorkerExecutionOutcome> Task => _completion.Task;

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
}
