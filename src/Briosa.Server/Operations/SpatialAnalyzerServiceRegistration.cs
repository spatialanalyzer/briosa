using Briosa.Server.Operations.FileOperations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Briosa.Server.Operations;

internal static class SpatialAnalyzerServiceRegistration
{
    public static IEndpointRouteBuilder MapSpatialAnalyzerServices(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGrpcService<FileOperationsService>();
        return endpoints;
    }
}
