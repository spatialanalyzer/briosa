namespace Briosa.Server.Workers;

/// <summary>
/// What discovery publishes about the worker lifecycle (#400): the allowances of the
/// exact <see cref="WorkerLifecyclePolicy"/> instance that the supervisor enforces and
/// that sizes the host shutdown bound, and the interaction mode the supervisor sets
/// after each verified Connect. Registration builds it from the same instances it
/// passes to the supervisor, so the published values cannot drift from the enforced
/// ones. The policy is immutable, so the projection is too.
/// </summary>
internal sealed class WorkerLifecycleProjection
{
    public WorkerLifecycleProjection(
        WorkerLifecyclePolicy policy,
        SpatialAnalyzerInteractionMode interactionMode)
    {
        ArgumentNullException.ThrowIfNull(policy);
        Policy = policy;
        InteractionMode = interactionMode;
    }

    /// <summary>The policy instance that the supervisor holds.</summary>
    public WorkerLifecyclePolicy Policy { get; }

    /// <summary>The configured mode. It is never read back from SpatialAnalyzer.</summary>
    public SpatialAnalyzerInteractionMode InteractionMode { get; }

    /// <summary>An accepted Start to its typed outcome.</summary>
    public TimeSpan StartSdk => Policy.StartBound;

    /// <summary>An accepted Connect or Reconnect to its typed outcome.</summary>
    public TimeSpan ConnectSpatialAnalyzer => Policy.ConnectBound;

    /// <summary>An accepted Recover to the typed outcome of its replacement.</summary>
    public TimeSpan RecoverSdk => Policy.RecoverBound;

    /// <summary>The worker stop after any MP command in flight has finished.</summary>
    public TimeSpan StopSdk => Policy.StopBound;

    /// <summary>
    /// The ordinary Start of a disconnected SDK in the startup sizing rule: the startup
    /// bound alone, because a disconnected worker's Ready runs no readiness probe.
    /// </summary>
    public TimeSpan OrdinaryStart => Policy.StartupTimeout;
}
