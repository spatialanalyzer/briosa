namespace Briosa.Server.Tests;

// Virtual time for every supervisor timer: heartbeat delays and the startup,
// connect, readiness, watchdog, heartbeat-timeout, shutdown, and cleanup deadlines.
// Time moves only when a test advances it; due timers fire in due-time order.
internal sealed class HeartbeatTestClock : TimeProvider
{
    // Only a failure guard: a real fake worker may need to start before it schedules.
    private static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(30);
    private readonly Lock _lock = new();
    private readonly List<VirtualTimer> _timers = [];
    private TaskCompletionSource _scheduled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _sequence;
    private long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(GetTimestamp());

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new VirtualTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    // Fires the earliest scheduled timer, then waits for a new timer with the same
    // delay: a rescheduled heartbeat proves that this monitor iteration has finished.
    public async Task TickAsync()
    {
        var fired = await FireNextAsync().ConfigureAwait(false);
        _ = await WaitForScheduledAsync(timer =>
            timer.DueTime == fired.DueTime && timer.Sequence > fired.Sequence, count: 1).ConfigureAwait(false);
    }

    // Waits for any scheduled timer, advances virtual time to the earliest due
    // timer, and fires every timer due by then.
    public Task<ScheduledTimer> FireNextAsync() => FireNextAsync(static _ => true, count: 1);

    // Waits until `count` timers scheduled with this delay exist, advances virtual
    // time to the last of them, and fires every timer due by then, earliest first.
    // A cleanup attempt schedules its own deadline and the outer wait bound.
    public Task<ScheduledTimer> FireNextAsync(TimeSpan dueTime, int count = 1) =>
        FireNextAsync(timer => timer.DueTime == dueTime, count);

    // Waits until a timer scheduled with this delay exists without advancing time,
    // so a test can prove which bound was armed before moving the clock.
    public Task<ScheduledTimer> WaitForScheduledAsync(TimeSpan dueTime) =>
        WaitForScheduledAsync(timer => timer.DueTime == dueTime, count: 1);

    public void Advance(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        AdvanceTo(GetTimestamp() + delta.Ticks);
    }

    private async Task<ScheduledTimer> FireNextAsync(Func<ScheduledTimer, bool> match, int count)
    {
        var timer = await WaitForScheduledAsync(match, count).ConfigureAwait(false);
        AdvanceTo(timer.DueAt);
        return timer;
    }

    private async Task<ScheduledTimer> WaitForScheduledAsync(Func<ScheduledTimer, bool> match, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        using var timeout = new CancellationTokenSource(WaitBound);
        while (true)
        {
            Task scheduled;
            lock (_lock)
            {
                var matches = Scheduled(match).Take(count).ToList();
                if (matches.Count == count) return matches[^1].Scheduled!.Value;
                scheduled = _scheduled.Task;
            }

            await scheduled.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
    }

    private void AdvanceTo(long target)
    {
        while (true)
        {
            VirtualTimer? due;
            lock (_lock)
            {
                due = Scheduled(timer => timer.DueAt <= target).FirstOrDefault();
                if (due is null)
                {
                    if (target > _timestamp) Interlocked.Exchange(ref _timestamp, target);
                    return;
                }

                var scheduled = due.Scheduled!.Value;
                if (scheduled.DueAt > _timestamp) Interlocked.Exchange(ref _timestamp, scheduled.DueAt);
                due.Scheduled = due.Period is { } period
                    ? scheduled with { DueAt = scheduled.DueAt + period.Ticks, Sequence = ++_sequence }
                    : null;
            }

            // Like a real timer, the callback runs outside the scheduler's lock.
            due.Fire();
        }
    }

    // Called under _lock: matching scheduled timers in firing order.
    private IEnumerable<VirtualTimer> Scheduled(Func<ScheduledTimer, bool> match) => _timers
        .Where(timer => timer.Scheduled is { } scheduled && match(scheduled))
        .OrderBy(timer => timer.Scheduled!.Value.DueAt)
        .ThenBy(timer => timer.Scheduled!.Value.Sequence);

    private bool Schedule(VirtualTimer timer, TimeSpan dueTime, TimeSpan period)
    {
        TaskCompletionSource signal;
        lock (_lock)
        {
            if (timer.Disposed) return false;
            if (!_timers.Contains(timer)) _timers.Add(timer);
            timer.Period = period == Timeout.InfiniteTimeSpan || period == TimeSpan.Zero ? null : period;
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                timer.Scheduled = null;
                return true;
            }

            timer.Scheduled = new ScheduledTimer(dueTime, _timestamp + dueTime.Ticks, ++_sequence);
            signal = _scheduled;
            _scheduled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        signal.TrySetResult();
        return true;
    }

    private void Remove(VirtualTimer timer)
    {
        lock (_lock)
        {
            timer.Disposed = true;
            timer.Scheduled = null;
            _timers.Remove(timer);
        }
    }

    private bool IsDisposed(VirtualTimer timer)
    {
        lock (_lock)
        {
            return timer.Disposed;
        }
    }

    internal readonly record struct ScheduledTimer(TimeSpan DueTime, long DueAt, long Sequence);

    // Mutable state is guarded by the owning clock's lock.
    private sealed class VirtualTimer(HeartbeatTestClock clock, TimerCallback callback, object? state) : ITimer
    {
        public ScheduledTimer? Scheduled { get; set; }
        public TimeSpan? Period { get; set; }
        public bool Disposed { get; set; }

        public void Fire()
        {
            if (!clock.IsDisposed(this)) callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => clock.Schedule(this, dueTime, period);

        public void Dispose() => clock.Remove(this);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
