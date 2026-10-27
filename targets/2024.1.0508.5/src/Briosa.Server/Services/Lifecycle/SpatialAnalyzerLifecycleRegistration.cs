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
        services.TryAddSingleton(
            SpatialAnalyzerApplicationOptions.BindAndValidate(configuration));
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
