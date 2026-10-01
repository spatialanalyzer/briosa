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
// Package configuration, including the operation allowlist, belongs to the
// installation rather than the caller's working directory. An explicit
// --contentRoot, ASPNETCORE_CONTENTROOT, or DOTNET_CONTENTROOT still wins.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = HasExplicitContentRoot(args) ? null : AppContext.BaseDirectory
});
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
builder.Services.AddGrpc(options =>
    options.MaxReceiveMessageSize = Briosa.Worker.Control.WorkerControlProtocol.MaximumMessageBytes);
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
    $"Briosa server {ServerBuildIdentity.Version}"));

app.MapGrpcHealthChecksService();
app.MapGrpcService<ServerDiscoveryService>();
app.MapGrpcService<SpatialAnalyzerSdkLifecycleService>();
app.MapGrpcService<SpatialAnalyzerLifecycleService>();
app.MapSpatialAnalyzerServices();
app.MapBriosaDevelopmentGrpcReflection();

app.Run();
desktopAnnouncement.Complete();

// Mirrors the host's content-root sources and their precedence.
static bool HasExplicitContentRoot(string[] arguments)
{
    var configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables("ASPNETCORE_")
        .AddEnvironmentVariables("DOTNET_")
        .AddCommandLine(arguments)
        .Build();
    try
    {
        return !string.IsNullOrEmpty(configuration[HostDefaults.ContentRootKey]);
    }
    finally
    {
        (configuration as IDisposable)?.Dispose();
    }
}

internal partial class Program;
