using System.Diagnostics.CodeAnalysis;
using Briosa.Server.Workers;

namespace Briosa.Server.Tests;

public sealed class WorkerHeartbeatMonitorTests
{
    // Only a hang guard: handshakes and virtual time order every step.
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task StopCancelsTheScheduledTick()
    {
        var clock = new HeartbeatTestClock();
        var calls = 0;
        var monitor = new WorkerHeartbeatMonitor(TimeSpan.FromSeconds(1), clock,
            _ => { Interlocked.Increment(ref calls); return Task.FromResult(true); });
        await using var lifetime = monitor.ConfigureAwait(true);
        Assert.Equal(0, calls);
        await clock.TickAsync();
        Assert.Equal(1, calls);
        await monitor.DisposeAsync();
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ConcurrentStopsBothWaitForAnEnteredExchange()
    {
        var clock = new HeartbeatTestClock();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var monitor = new WorkerHeartbeatMonitor(TimeSpan.FromSeconds(1), clock,
            _ => { entered.TrySetResult(); return release.Task; });
        await clock.FireNextAsync();
        await entered.Task.WaitAsync(HangGuard);
        var firstStop = monitor.DisposeAsync().AsTask();
        var secondStop = monitor.DisposeAsync().AsTask();
        try
        {
            Assert.Same(firstStop, secondStop);
            Assert.False(firstStop.IsCompleted);
        }
        finally
        {
            release.TrySetResult(true);
            await Task.WhenAll(firstStop, secondStop).WaitAsync(HangGuard);
        }
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Both explicit disposal calls are inside Assert.ThrowsAsync because disposal must preserve the callback failure.")]
    public async Task CallbackFaultRemainsObservableAfterStop()
    {
        var clock = new HeartbeatTestClock();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var monitor = new WorkerHeartbeatMonitor(TimeSpan.FromSeconds(1), clock, _ =>
        {
            entered.TrySetResult();
            return Task.FromException<bool>(new InvalidOperationException("test-monitor-failure"));
        });
        await clock.FireNextAsync();
        await entered.Task.WaitAsync(HangGuard);
        await Assert.ThrowsAsync<InvalidOperationException>(() => monitor.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => monitor.DisposeAsync().AsTask());
    }
}
