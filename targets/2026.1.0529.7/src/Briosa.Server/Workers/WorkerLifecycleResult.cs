namespace Briosa.Server.Workers;

/// <summary>The outcome and immutable state captured before a lifecycle action releases ownership.</summary>
internal abstract record WorkerLifecycleResult(WorkerLifecycleSnapshot Snapshot)
{
    public bool Succeeded => this is WorkerLifecycleSucceeded;
}
