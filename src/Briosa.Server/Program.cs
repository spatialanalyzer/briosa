using Briosa.Protocol;
using Briosa.Server.Services.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Workers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddWorkerProcessLifecycle(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    $"Briosa server scaffold ({ProtocolAssembly.MarkerType.Assembly.GetName().Version})"));

app.MapGrpcService<FileOperationsService>();

app.Run();

internal partial class Program;
