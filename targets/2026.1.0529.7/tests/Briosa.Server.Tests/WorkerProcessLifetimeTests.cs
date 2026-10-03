using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

public sealed class WorkerProcessLifetimeTests
{
    private static readonly TimeSpan CleanupBound = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task IgnoredTerminationCancellationRetainsOneAttemptUntilExit()
    {
        var worker = new ControlledProcess { HoldTermination = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var lifetime = new WorkerProcessLifetime(worker, clock);
        try
        {
            Assert.Equal(WorkerCleanupStatus.ExitUnconfirmed, await ReleaseAfterBound(lifetime, clock, timers: 2));
            Assert.Equal(WorkerCleanupStatus.ExitUnconfirmed, await ReleaseAfterBound(lifetime, clock, timers: 1));
            Assert.Equal(1, worker.Terminations);
            Assert.Equal(0, worker.Disposals);
            Assert.False(worker.HasExited);
        }
        finally
        {
            worker.ReleaseTermination.TrySetResult();
        }
        Assert.Equal(WorkerCleanupStatus.Complete, await Release(lifetime));
        Assert.True(worker.HasExited);
        Assert.Equal(1, worker.Disposals);
    }

    [Fact]
    public async Task FailedKillRemainsObservableAndCanBeRetriedExplicitly()
    {
        var worker = new ControlledProcess { FailTermination = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var lifetime = new WorkerProcessLifetime(worker, clock);
        Assert.Equal(WorkerCleanupStatus.ExitUnconfirmed, await Release(lifetime));
        Assert.Equal(0, worker.Disposals);
        worker.FailTermination = false;
        Assert.Equal(WorkerCleanupStatus.Complete, await Release(lifetime));
        Assert.Equal(2, worker.Terminations);
    }

    [Fact]
    public async Task ReturningFromTerminationDoesNotProveProcessExit()
    {
        var worker = new ControlledProcess { OmitExit = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var lifetime = new WorkerProcessLifetime(worker, clock);
        Assert.Equal(WorkerCleanupStatus.ExitUnconfirmed, await Release(lifetime));
        Assert.False(worker.HasExited);
        Assert.Equal(0, worker.Disposals);
        worker.OmitExit = false;
        Assert.Equal(WorkerCleanupStatus.Complete, await Release(lifetime));
    }

    [Fact]
    public async Task DelayedDisposalIsBoundedAndNeverOverlapped()
    {
        var worker = new ControlledProcess { HoldDisposal = true };
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var lifetime = new WorkerProcessLifetime(worker, clock);
        try
        {
            Assert.Equal(WorkerCleanupStatus.ResourcesUnreleased, await ReleaseAfterBound(lifetime, clock, timers: 2));
            Assert.Equal(WorkerCleanupStatus.ResourcesUnreleased, await ReleaseAfterBound(lifetime, clock, timers: 1));
            Assert.True(worker.HasExited);
            Assert.Equal(1, worker.Terminations);
            Assert.Equal(1, worker.Disposals);
        }
        finally
        {
            worker.ReleaseDisposal.TrySetResult();
        }
        Assert.Equal(WorkerCleanupStatus.Complete, await Release(lifetime));
        Assert.Equal(1, worker.Disposals);
    }

    [Fact]
    public async Task GracefulCleanupDoesNotClaimALiveProcessWasReleased()
    {
        var worker = new ControlledProcess();
        await using var workerScope = worker.ConfigureAwait(true);
        var clock = new HeartbeatTestClock();
        var lifetime = new WorkerProcessLifetime(worker, clock);
        Assert.Equal(WorkerCleanupStatus.ExitUnconfirmed,
            await lifetime.ReleaseAsync(force: false, CleanupBound));
        Assert.Equal(0, worker.Terminations);
        Assert.Equal(0, worker.Disposals);
        Assert.Equal(WorkerCleanupStatus.Complete, await Release(lifetime));
    }

    private static Task<WorkerCleanupStatus> Release(WorkerProcessLifetime lifetime) =>
        lifetime.ReleaseAsync(force: true, CleanupBound).WaitAsync(TimeSpan.FromSeconds(3));

    // The cleanup bound is virtual: a stalled attempt resolves only when the test
    // fires its deadline and outer wait bound (one new bound when an attempt is reused).
    private static async Task<WorkerCleanupStatus> ReleaseAfterBound(
        WorkerProcessLifetime lifetime, HeartbeatTestClock clock, int timers)
    {
        var release = lifetime.ReleaseAsync(force: true, CleanupBound);
        await clock.FireNextAsync(CleanupBound, timers).ConfigureAwait(true);
        return await release.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(true);
    }

    private sealed class ControlledProcess : IWorkerProcess
    {
        public bool HoldTermination { get; init; }
        public bool HoldDisposal { get; init; }
        public bool FailTermination { get; set; }
        public bool OmitExit { get; set; }
        public int Terminations { get; private set; }
        public int Disposals { get; private set; }
        public bool HasExited { get; private set; }
        public int? ExitCode => HasExited ? 0 : null;
        public TaskCompletionSource ReleaseTermination { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseDisposal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask SendAsync(WorkerControlMessage message, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public ValueTask<WorkerControlMessage> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async ValueTask TerminateAsync(CancellationToken cancellationToken = default)
        {
            Terminations++;
            if (FailTermination) throw new System.ComponentModel.Win32Exception("Test kill failure");
            if (HoldTermination) await ReleaseTermination.Task.ConfigureAwait(false);
            if (!OmitExit) HasExited = true;
        }

        public async ValueTask DisposeAsync()
        {
            Disposals++;
            if (HoldDisposal) await ReleaseDisposal.Task.ConfigureAwait(false);
        }
    }
}
