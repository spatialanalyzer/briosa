using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Briosa.Server.Services;

internal static class SpatialAnalyzerLifecycleRegistration
{
    public static IServiceCollection AddSpatialAnalyzerLifecycle(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var options = SpatialAnalyzerApplicationOptions.BindAndValidate(configuration);
        services.TryAddSingleton(options);
        // Host shutdown waits for an accepted launch (#322), which a Connect can queue behind.
        services.AddLifecycleShutdownBound("application", options.StartupTimeout);
        services.TryAddSingleton<ISpatialAnalyzerProcessPlatform,
            WindowsSpatialAnalyzerProcessPlatform>();
        services.TryAddSingleton<ISpatialAnalyzerWindowSource,
            WindowsSpatialAnalyzerWindowSource>();
        services.TryAddSingleton<ISpatialAnalyzerModalDialogProbe,
            SpatialAnalyzerModalDialogProbe>();
        services.TryAddSingleton<SpatialAnalyzerLifecycleCoordinator>();
        services.TryAddSingleton<ISpatialAnalyzerLifecycleStateProvider>(provider =>
            provider.GetRequiredService<SpatialAnalyzerLifecycleCoordinator>());
        return services;
    }
}
