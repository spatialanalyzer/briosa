using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Briosa.Server.Services;

internal static class HealthAndDiscoveryRegistration
{
    public static IServiceCollection AddBriosaHealthAndDiscovery(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<
            IServerBuildIdentityProvider,
            AssemblyServerBuildIdentityProvider>();
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
