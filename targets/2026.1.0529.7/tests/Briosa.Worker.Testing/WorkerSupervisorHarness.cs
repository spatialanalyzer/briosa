using Briosa.Worker.Sdk;

namespace Briosa.Worker.Testing;

internal enum SupervisedExecutionStatus
{
    Completed,
    WatchdogTimeout,
    WorkerCrash
}

internal sealed record SupervisedExecutionResult(
    SupervisedExecutionStatus Status,
    SdkExecutionResult? Execution);

internal interface IWorkerEndpoint : IAsyncDisposable
{
    Task<SdkExecutionResult> ExecuteAsync(
        SdkCommand command,
        CancellationToken cancellationToken = default);

    ValueTask TerminateAsync();
}

/// <summary>
/// Portable supervisor seam used to verify replacement policy without starting a real process.
/// </summary>
internal sealed class WorkerSupervisorHarness : IAsyncDisposable
{
    private readonly Func<IWorkerEndpoint> _workerFactory;
    private readonly TimeSpan _watchdogTimeout;
    private IWorkerEndpoint _worker;
    private int _disposeState;

    public WorkerSupervisorHarness(
        Func<IWorkerEndpoint> workerFactory,
        TimeSpan watchdogTimeout)
    {
        ArgumentNullException.ThrowIfNull(workerFactory);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(watchdogTimeout, TimeSpan.Zero);

        _workerFactory = workerFactory;
        _watchdogTimeout = watchdogTimeout;
        _worker = workerFactory();
    }

    public int ReplacementCount { get; private set; }

    public async Task<SupervisedExecutionResult> ExecuteAsync(
        SdkCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);

        var worker = _worker;
        var execution = worker.ExecuteAsync(command, cancellationToken);
        try
        {
            var result = await execution.WaitAsync(_watchdogTimeout, cancellationToken);
            return new SupervisedExecutionResult(SupervisedExecutionStatus.Completed, result);
        }
        catch (TimeoutException)
        {
            await ReplaceWorker(worker, execution);
            return new SupervisedExecutionResult(SupervisedExecutionStatus.WatchdogTimeout, null);
        }
        catch (SimulatedWorkerCrashException)
        {
            await ReplaceWorker(worker, execution);
            return new SupervisedExecutionResult(SupervisedExecutionStatus.WorkerCrash, null);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        await _worker.TerminateAsync();
        await _worker.DisposeAsync();
    }

    private async Task ReplaceWorker(IWorkerEndpoint failedWorker, Task failedExecution)
    {
        await failedWorker.TerminateAsync();

        try
        {
            await failedExecution;
        }
        catch (Exception exception) when (
            exception is SimulatedWorkerCrashException or ObjectDisposedException)
        {
        }

        await failedWorker.DisposeAsync();
        _worker = _workerFactory();
        ReplacementCount++;
    }
}

internal sealed class ScriptedWorkerEndpoint : IWorkerEndpoint
{
    private readonly ScriptedSdkPlan _plan;
    private readonly SerializedSdkExecutor _executor;
    private int _disposeState;

    public ScriptedWorkerEndpoint(ScriptedSdkPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _plan = plan;
        _executor = new SerializedSdkExecutor(plan.CreateSdk);
    }

    public Task<SdkExecutionResult> ExecuteAsync(
        SdkCommand command,
        CancellationToken cancellationToken = default) =>
        _executor.ExecuteAsync(command, cancellationToken);

    public async ValueTask TerminateAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        _plan.ReleaseBlockedCalls();
        await _executor.DisposeAsync();
    }

    public ValueTask DisposeAsync() => TerminateAsync();
}
