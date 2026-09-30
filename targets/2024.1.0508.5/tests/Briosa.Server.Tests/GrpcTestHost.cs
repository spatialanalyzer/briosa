using System.Net;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Tests;

internal sealed class GrpcTestHost : IAsyncDisposable
{
    private readonly WebApplication app;

    private GrpcTestHost(WebApplication app)
    {
        this.app = app;
        Channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
    }

    public GrpcChannel Channel { get; }

    public static async Task<GrpcTestHost> StartAsync<TService>(IWorkerCommandExecutor worker)
        where TService : class
        => await StartAsync(worker, app => app.MapGrpcService<TService>()).ConfigureAwait(true);

    public static async Task<GrpcTestHost> StartAsync<TFirstService, TSecondService>(IWorkerCommandExecutor worker)
        where TFirstService : class
        where TSecondService : class
        => await StartAsync(worker, app =>
        {
            app.MapGrpcService<TFirstService>();
            app.MapGrpcService<TSecondService>();
        }).ConfigureAwait(true);

    public static async Task<GrpcTestHost> StartAsync(IWorkerCommandExecutor worker, Action<WebApplication> mapServices)
        => await StartAsync(services => services.AddSingleton(new OperationExecutor(worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System)),
            mapServices).ConfigureAwait(true);

    /// <summary>Hosts the public SDK lifecycle service over the supplied coordinator.</summary>
    public static async Task<GrpcTestHost> StartSdkLifecycleAsync(SpatialAnalyzerSdkLifecycleCoordinator coordinator)
        => await StartAsync(services => services.AddSingleton(coordinator),
            app => app.MapGrpcService<SpatialAnalyzerSdkLifecycleService>()).ConfigureAwait(true);

    private static async Task<GrpcTestHost> StartAsync(
        Action<IServiceCollection> configureServices,
        Action<WebApplication> mapServices)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        configureServices(builder.Services);
        var app = builder.Build();
        mapServices(app);
        try
        {
            await app.StartAsync().ConfigureAwait(true);
            return new GrpcTestHost(app);
        }
        catch
        {
            await app.DisposeAsync().ConfigureAwait(true);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Channel.Dispose();
        await app.DisposeAsync().ConfigureAwait(true);
    }
}
