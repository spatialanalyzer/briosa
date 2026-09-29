namespace Briosa.Server.Workers;

// Called under the generation controller's gate. An incomplete release retains
// both the process and its pending cleanup; it never authorizes a replacement.
internal sealed class WorkerProcessLifetime(IWorkerProcess process)
{
    private Task<WorkerCleanupStatus>? _cleanup;
    private volatile WorkerCleanupStatus _incompleteStatus = WorkerCleanupStatus.ExitUnconfirmed;

    public IWorkerProcess Process { get; } = process ?? throw new ArgumentNullException(nameof(process));

    public bool ReleaseStarted => _cleanup is not null;

    public async Task<WorkerCleanupStatus> ReleaseAsync(bool force, TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        if (_cleanup is null || _cleanup.IsCompletedSuccessfully && await _cleanup.ConfigureAwait(false) != WorkerCleanupStatus.Complete)
            _cleanup = ReleaseCoreAsync(force, timeout);

        try
        {
            // The outer bound also covers an implementation that ignores cancellation,
            // including DisposeAsync, which has no cancellation parameter.
            return await _cleanup.WaitAsync(timeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return _incompleteStatus;
        }
    }

    private async Task<WorkerCleanupStatus> ReleaseCoreAsync(bool force, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        try
        {
            if (!Process.HasExited)
            {
                if (!force) return WorkerCleanupStatus.ExitUnconfirmed;
                await Process.TerminateAsync(deadline.Token).ConfigureAwait(false);
                await Process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
                if (!Process.HasExited) return WorkerCleanupStatus.ExitUnconfirmed;
            }

            _incompleteStatus = WorkerCleanupStatus.ResourcesUnreleased;
            await Process.DisposeAsync().ConfigureAwait(false);
            return WorkerCleanupStatus.Complete;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Observe late failures too. A later explicit cleanup may retry a
            // completed failed attempt, but never overlaps an outstanding one.
            return _incompleteStatus;
        }
    }
}
