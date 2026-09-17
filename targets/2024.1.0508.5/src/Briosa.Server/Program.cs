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
var desktopOptions = DesktopHostOptions.Create(builder.Configuration);
using var desktopAnnouncement = new DesktopAnnouncement(desktopOptions);
try
{
    builder.Logging.AddBriosaLogging(builder.Configuration);
    builder.Services.AddSingleton(BriosaTelemetryOptions.Bind(builder.Configuration));
}
catch (InvalidOperationException)
{
    Console.Error.WriteLine("Invalid Briosa observability configuration.");
    Environment.ExitCode = 2;
    return;
}
builder.Services.AddSingleton<BriosaTelemetry>();
builder.Services.AddSingleton<LifecycleAuditLogger>();
builder.Services.AddHostedService<BriosaTelemetryExport>();
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
builder.Services.AddSingleton(desktopOptions with { Announced = true });
builder.Services.AddSingleton<ServerDiscoveryService>();
builder.Services.AddHostedService<DesktopHost>();

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
desktopAnnouncement.Complete();

internal partial class Program;
