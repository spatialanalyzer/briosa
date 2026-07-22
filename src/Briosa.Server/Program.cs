using Briosa.Protocol;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Services.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Workers;
using Microsoft.AspNetCore.Server.Kestrel.Core;

if (args is ["diagnostics"] or ["--diagnostics"])
{
    Environment.ExitCode = ServerDiagnosticsCommand.Run(Console.Out, AppContext.BaseDirectory);
    return;
}
var builder = WebApplication.CreateBuilder(args);
var publicEndpoint = PublicEndpointConfiguration.Resolve(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
    options.Listen(
        publicEndpoint.Address,
        publicEndpoint.Port,
        listenOptions => listenOptions.Protocols = HttpProtocols.Http2));
builder.Services.AddGrpc();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddWorkerProcessLifecycle(builder.Configuration);
builder.Services.AddBriosaHealthAndDiscovery();

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    $"Briosa server scaffold ({ProtocolAssembly.MarkerType.Assembly.GetName().Version})"));

app.MapGrpcHealthChecksService();
app.MapGrpcService<ServerDiscoveryService>();
app.MapGrpcService<FileOperationsService>();

app.Run();

internal partial class Program;
