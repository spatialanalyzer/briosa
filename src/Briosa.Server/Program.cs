using Briosa.Protocol;
using Briosa.Server.Workers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddWorkerProcessLifecycle(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    $"Briosa server scaffold ({ProtocolAssembly.MarkerType.Assembly.GetName().Version})"));

app.Run();

internal partial class Program;
