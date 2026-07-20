using Briosa.Protocol;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    $"Briosa server scaffold ({ProtocolAssembly.MarkerType.Assembly.GetName().Version})"));

app.Run();

public partial class Program;
