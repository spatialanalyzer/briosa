using Briosa.Protocol;
using Briosa.Server.Services;
using Briosa.Server.Services.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Workers;

var builder = WebApplication.CreateBuilder(args);
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
