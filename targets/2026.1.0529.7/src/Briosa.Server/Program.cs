using Briosa.Protocol;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Microsoft.AspNetCore.Server.Kestrel.Core;

if (args is ["diagnostics"] or ["--diagnostics"])
{
    Environment.ExitCode = ServerDiagnosticsCommand.Run(Console.Out, AppContext.BaseDirectory);
    return;
}
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBriosaLogging();
var publicEndpoint = PublicEndpointConfiguration.Resolve(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
    options.Listen(
        publicEndpoint.Address,
        publicEndpoint.Port,
        listenOptions => listenOptions.Protocols = HttpProtocols.Http2));
builder.Services.AddGrpc();
builder.Services.AddBriosaDevelopmentGrpcReflection(builder.Environment);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddWorkerProcessLifecycle(builder.Configuration);
builder.Services.AddBriosaHealthAndDiscovery();
builder.Services.AddSingleton<SpatialAnalyzerSdkLifecycleStateProjection>();
builder.Services.AddSingleton<ISpatialAnalyzerSdkLifecycleStateProvider>(provider =>
    provider.GetRequiredService<SpatialAnalyzerSdkLifecycleStateProjection>());
builder.Services.AddSpatialAnalyzerLifecycle(builder.Configuration);
builder.Services.AddSingleton<SpatialAnalyzerSdkLifecycleCoordinator>();
builder.Services.AddSingleton<OperationExecutor>();

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    $"Briosa server scaffold ({ProtocolAssembly.MarkerType.Assembly.GetName().Version})"));

app.MapGrpcHealthChecksService();
app.MapGrpcService<ServerDiscoveryService>();
app.MapGrpcService<SpatialAnalyzerSdkLifecycleService>();
app.MapGrpcService<SpatialAnalyzerLifecycleService>();
app.MapSpatialAnalyzerServices();
app.MapBriosaDevelopmentGrpcReflection();

app.Run();

internal partial class Program;
