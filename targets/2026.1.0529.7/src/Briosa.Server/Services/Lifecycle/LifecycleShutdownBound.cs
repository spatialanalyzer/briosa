using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Briosa.Server.Services;

/// <summary>
/// How long a graceful host stop may wait for one lifecycle's accepted work (#321).
/// The application lifecycle registers its launch bound; the worker lifecycle registers
/// its longest accepted SDK exchange plus the worker stop. A Connect can wait behind
/// an accepted launch, so the host waits for their sum.
/// </summary>
internal sealed record LifecycleShutdownBound(string Lifecycle, TimeSpan Duration);

/// <summary>
/// Raises the host shutdown timeout to the sum of the registered lifecycle bounds, so a
/// graceful stop lets accepted lifecycle work reach its own outcome before the worker
/// stops. A longer or infinite timeout configured for the host is kept.
/// </summary>
internal sealed class LifecycleHostShutdownOptions(IEnumerable<LifecycleShutdownBound> bounds)
    : IConfigureOptions<HostOptions>
{
    private readonly IEnumerable<LifecycleShutdownBound> _bounds = bounds;

    public void Configure(HostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        // A lifecycle registered twice counts once, like the TryAdd registrations.
        var required = _bounds
            .DistinctBy(static bound => bound.Lifecycle, StringComparer.Ordinal)
            .Aggregate(TimeSpan.Zero, static (sum, bound) => sum + bound.Duration);
        if (options.ShutdownTimeout != Timeout.InfiniteTimeSpan && options.ShutdownTimeout < required)
            options.ShutdownTimeout = required;
    }
}

internal static class LifecycleShutdownRegistration
{
    public static IServiceCollection AddLifecycleShutdownBound(
        this IServiceCollection services,
        string lifecycle,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(new LifecycleShutdownBound(lifecycle, duration));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<HostOptions>, LifecycleHostShutdownOptions>());
        return services;
    }
}
