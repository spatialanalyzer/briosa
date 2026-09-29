using System.Diagnostics.CodeAnalysis;
using Briosa.Server.Workers;

namespace Briosa.Server.Tests;

public sealed class WorkerHeartbeatMonitorTests
{
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
        await clock.FireNextAsync();
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
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
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
            await Task.WhenAll(firstStop, secondStop).WaitAsync(TimeSpan.FromSeconds(3));
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
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await Assert.ThrowsAsync<InvalidOperationException>(() => monitor.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => monitor.DisposeAsync().AsTask());
    }
}
