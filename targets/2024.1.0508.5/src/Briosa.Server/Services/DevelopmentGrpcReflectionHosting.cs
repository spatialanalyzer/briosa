using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Briosa.Server.Services;

internal static class DevelopmentGrpcReflectionHosting
{
    internal static bool IsEnabled(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
#if BRIOSA_DEVELOPMENT_REFLECTION
        return environment.IsDevelopment();
#else
        return false;
#endif
    }

    public static IServiceCollection AddBriosaDevelopmentGrpcReflection(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!IsEnabled(environment))
        {
            return services;
        }

#if BRIOSA_DEVELOPMENT_REFLECTION
        services.AddGrpcReflection();
#endif
        return services;
    }

    public static IEndpointRouteBuilder MapBriosaDevelopmentGrpcReflection(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        if (!IsEnabled(endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>()))
        {
            return endpoints;
        }

#if BRIOSA_DEVELOPMENT_REFLECTION
        endpoints.MapGrpcReflectionService();
#endif
        return endpoints;
    }
}
