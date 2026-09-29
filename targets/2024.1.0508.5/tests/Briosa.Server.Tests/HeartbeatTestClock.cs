using System.Threading.Channels;

namespace Briosa.Server.Tests;

internal sealed class HeartbeatTestClock : TimeProvider
{
    private readonly Channel<Tick> _ticks = Channel.CreateUnbounded<Tick>();
    private long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(GetTimestamp());

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var tick = new Tick(callback, state, dueTime);
        _ticks.Writer.TryWrite(tick);
        return tick;
    }

    public async Task TickAsync()
    {
        await FireNextAsync().ConfigureAwait(false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        // The next scheduled delay proves this monitor iteration has finished.
        await _ticks.Reader.WaitToReadAsync(timeout.Token).ConfigureAwait(false);
    }

    public async Task FireNextAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var tick = await _ticks.Reader.ReadAsync(timeout.Token).ConfigureAwait(false);
        Interlocked.Add(ref _timestamp, tick.Delay.Ticks);
        tick.Fire();
    }

    private sealed class Tick(TimerCallback callback, object? state, TimeSpan delay) : ITimer
    {
        private int _disposed;
        public TimeSpan Delay { get; } = delay;
        public void Fire()
        {
            if (Volatile.Read(ref _disposed) == 0) callback(state);
        }
        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
