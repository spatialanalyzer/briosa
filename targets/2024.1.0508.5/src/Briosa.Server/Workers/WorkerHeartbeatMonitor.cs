namespace Briosa.Server.Workers;

// Owns scheduling and cancellation only. The generation controller decides
// whether a tick may probe and what a response means for readiness.
internal sealed class WorkerHeartbeatMonitor : IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Lock _stopLock = new();
    private readonly Task _completion;
    private Task? _stopTask;

    public WorkerHeartbeatMonitor(TimeSpan interval, TimeProvider timeProvider,
        Func<CancellationToken, Task<bool>> onTick)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(onTick);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        _completion = Run(interval, timeProvider, onTick, _cancellation.Token);
    }

    public ValueTask DisposeAsync()
    {
        lock (_stopLock)
        {
            // Every caller waits for the same stop, including an entered exchange.
            return new ValueTask(_stopTask ??= StopCore());
        }
    }

    private async Task StopCore()
    {
        try
        {
            await _cancellation.CancelAsync().ConfigureAwait(false);
            await _completion.ConfigureAwait(false);
        }
        finally
        {
            _cancellation.Dispose();
        }
    }

    private static async Task Run(TimeSpan interval, TimeProvider timeProvider,
        Func<CancellationToken, Task<bool>> onTick, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await Task.Delay(interval, timeProvider, cancellationToken).ConfigureAwait(false);
                if (!await onTick(cancellationToken).ConfigureAwait(false)) return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
