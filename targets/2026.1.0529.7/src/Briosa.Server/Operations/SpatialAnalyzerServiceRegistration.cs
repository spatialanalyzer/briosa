using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.DimensionOperations;
using Briosa.Server.Operations.EventOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.MpSubroutines;
using Briosa.Server.Operations.MpTaskOverview;
using Briosa.Server.Operations.ProcessFlowOperations;
using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Operations.ReportingOperations;
using Briosa.Server.Operations.ScaleBarOperations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Operations.VectorOperations;
using Briosa.Server.Operations.ViewControl;
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
        endpoints.MapGrpcService<DimensionOperationsService>();
        endpoints.MapGrpcService<EventOperationsService>();
        endpoints.MapGrpcService<FileOperationsService>();
        endpoints.MapGrpcService<MpSubroutinesService>();
        endpoints.MapGrpcService<MpTaskOverviewService>();
        endpoints.MapGrpcService<ProcessFlowOperationsService>();
        endpoints.MapGrpcService<RelationshipOperationsService>();
        endpoints.MapGrpcService<ReportingOperationsService>();
        endpoints.MapGrpcService<ScaleBarOperationsService>();
        endpoints.MapGrpcService<UtilityOperationsService>();
        endpoints.MapGrpcService<VariablesService>();
        endpoints.MapGrpcService<VectorOperationsService>();
        endpoints.MapGrpcService<ViewControlService>();
        return endpoints;
    }
}
