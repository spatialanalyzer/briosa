namespace Briosa.Server.Workers;

/// <summary>
/// The caller stopped waiting for an accepted lifecycle exchange (F10, #305). The
/// exchange continues under its own bounds, so this snapshot is the state when the
/// caller left, not the outcome; the caller refreshes state for the terminal result.
/// </summary>
internal sealed record WorkerLifecycleDetached(WorkerLifecycleSnapshot Snapshot) : WorkerLifecycleResult(Snapshot);
