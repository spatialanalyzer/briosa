using Briosa.Server.Workers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Briosa.Server.Services;

internal static class HealthAndDiscoveryRegistration
{
    public static IServiceCollection AddBriosaHealthAndDiscovery(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<
            IServerBuildIdentityProvider,
            BuildIdentityProvider>();
        // Resolved at first use, after the host options are final (#400).
        services.TryAddSingleton(static provider => new ServerLifecycleDiscovery(
            provider.GetRequiredService<WorkerLifecycleProjection>(),
            provider.GetRequiredService<SpatialAnalyzerApplicationOptions>(),
            provider.GetRequiredService<IOptions<HostOptions>>().Value));
        services.AddGrpcHealthChecks(options =>
            {
                options.Services.Map(
                    WorkerReadinessHealthCheck.LivenessServiceName,
                    static context => context.Name ==
                        WorkerReadinessHealthCheck.LivenessServiceName);
                options.Services.Map(
                    WorkerReadinessHealthCheck.ReadinessServiceName,
                    static context => context.Name ==
                        WorkerReadinessHealthCheck.ReadinessServiceName);
            })
            .AddCheck(
                WorkerReadinessHealthCheck.LivenessServiceName,
                static () => HealthCheckResult.Healthy())
            .AddCheck<WorkerReadinessHealthCheck>(
                WorkerReadinessHealthCheck.ReadinessServiceName);
        return services;
    }
}
