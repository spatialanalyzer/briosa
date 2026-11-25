using Briosa.Server.Workers;
using Api = global::Briosa;

namespace Briosa.Server.Services;

/// <summary>
/// The lifecycle allowances and the configured interaction mode that GetServerInfo
/// publishes (#400). It is built once from the instances the server enforces: the
/// worker policy projection the supervisor holds, the application options the
/// application lifecycle uses, and the final host options the host stops with. Each
/// allowance is composed first and then rounded up to a whole millisecond once. It
/// never reads SpatialAnalyzer, so it is the same before the SDK starts, while it is
/// disconnected, and while it is connected.
/// </summary>
internal sealed class ServerLifecycleDiscovery
{
    /// <summary>
    /// The longest finite delay a .NET timer arms, 4,294,967,294 ms. The host arms its
    /// shutdown timeout with a cancellation timer; for a longer finite value it accepts
    /// the option and then throws from StopAsync instead of arming it.
    /// </summary>
    internal static readonly TimeSpan MaximumArmableTimeout =
        TimeSpan.FromTicks((uint.MaxValue - 1L) * TimeSpan.TicksPerMillisecond);

    private readonly Api.LifecycleBounds _bounds;

    public ServerLifecycleDiscovery(
        WorkerLifecycleProjection worker,
        SpatialAnalyzerApplicationOptions application,
        HostOptions host)
    {
        ArgumentNullException.ThrowIfNull(worker);
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(host);
        _bounds = new Api.LifecycleBounds();
        if (ToMilliseconds(worker.StartSdk) is { } start) _bounds.StartSdkMilliseconds = start;
        if (ToMilliseconds(application.StartupTimeout) is { } launch)
            _bounds.LaunchSpatialAnalyzerMilliseconds = launch;
        if (ToMilliseconds(worker.ConnectSpatialAnalyzer) is { } connect)
            _bounds.ConnectSpatialAnalyzerMilliseconds = connect;
        if (ToMilliseconds(worker.RecoverSdk) is { } recover) _bounds.RecoverSdkMilliseconds = recover;
        if (ToMilliseconds(worker.StopSdk) is { } stop) _bounds.StopSdkMilliseconds = stop;
        if (ToMilliseconds(application.ShutdownTimeout) is { } close)
            _bounds.CloseOwnedSpatialAnalyzerMilliseconds = close;
        // The sizing rule: an ordinary disconnected Start, the optional launch, then Connect.
        if (ToMilliseconds(worker.OrdinaryStart + application.StartupTimeout + worker.ConnectSpatialAnalyzer)
            is { } withLaunch)
            _bounds.StartupWithLaunchMilliseconds = withLaunch;
        if (ToMilliseconds(worker.OrdinaryStart + worker.ConnectSpatialAnalyzer) is { } withoutLaunch)
            _bounds.StartupWithoutLaunchMilliseconds = withoutLaunch;
        if (ToGracefulShutdownBound(host.ShutdownTimeout) is { } shutdown)
            _bounds.HostGracefulShutdown = shutdown;
        InteractionMode = ToProtocol(worker.InteractionMode);
    }

    /// <summary>The configured mode, mapped by name. It is never read back from SpatialAnalyzer.</summary>
    public Api.ConfiguredSpatialAnalyzerInteractionMode InteractionMode { get; }

    /// <summary>A fresh copy, so a caller cannot change what later replies publish.</summary>
    public Api.LifecycleBounds CreateBounds() => _bounds.Clone();

    /// <summary>
    /// A finite bound only when the host can arm it, unbounded only for an infinite
    /// timeout, and otherwise none: unavailable, never a new configuration rejection.
    /// A value less than a millisecond above the limit, which the host truncates and
    /// still arms, is omitted too, because its rounded allowance would exceed the limit.
    /// </summary>
    internal static Api.GracefulShutdownBound? ToGracefulShutdownBound(TimeSpan timeout)
    {
        if (timeout == Timeout.InfiniteTimeSpan)
            return new Api.GracefulShutdownBound { Unbounded = new Api.UnboundedShutdownWait() };
        if (timeout <= TimeSpan.Zero || timeout > MaximumArmableTimeout) return null;
        return new Api.GracefulShutdownBound { FiniteMilliseconds = CeilingMilliseconds(timeout) };
    }

    // By name, never by number: the SaInteractionMode numbers differ.
    internal static Api.ConfiguredSpatialAnalyzerInteractionMode ToProtocol(
        SpatialAnalyzerInteractionMode mode) =>
        mode switch
        {
            SpatialAnalyzerInteractionMode.Unchanged =>
                Api.ConfiguredSpatialAnalyzerInteractionMode.Unchanged,
            SpatialAnalyzerInteractionMode.Silent =>
                Api.ConfiguredSpatialAnalyzerInteractionMode.Silent,
            SpatialAnalyzerInteractionMode.Automatic =>
                Api.ConfiguredSpatialAnalyzerInteractionMode.Automatic,
            SpatialAnalyzerInteractionMode.Manual =>
                Api.ConfiguredSpatialAnalyzerInteractionMode.Manual,
            _ => Api.ConfiguredSpatialAnalyzerInteractionMode.Unspecified
        };

    // A positive allowance rounded up to whole milliseconds, or none when it does not fit.
    private static uint? ToMilliseconds(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return null;
        var milliseconds = CeilingMilliseconds(duration);
        return milliseconds <= uint.MaxValue ? (uint)milliseconds : null;
    }

    // Called with positive durations only; a partial millisecond rounds up.
    private static ulong CeilingMilliseconds(TimeSpan duration)
    {
        var whole = Math.DivRem(duration.Ticks, TimeSpan.TicksPerMillisecond, out var remainder);
        return (ulong)whole + (remainder > 0 ? 1UL : 0UL);
    }
}
