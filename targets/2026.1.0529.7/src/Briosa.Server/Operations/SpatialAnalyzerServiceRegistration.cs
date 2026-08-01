using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.UtilityOperations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Briosa.Server.Operations;

internal static class SpatialAnalyzerServiceRegistration
{
    public static IEndpointRouteBuilder MapSpatialAnalyzerServices(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGrpcService<AnalysisOperationsService>();
        endpoints.MapGrpcService<ConstructionOperationsService>();
        endpoints.MapGrpcService<FileOperationsService>();
        endpoints.MapGrpcService<UtilityOperationsService>();
        return endpoints;
    }
}
