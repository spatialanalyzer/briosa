using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

/// <summary>
/// #400: GetServerInfo publishes the lifecycle allowances the server enforces and the
/// configured SpatialAnalyzer interaction mode. S, P, A, and B are the configurable
/// worker startup, readiness probe, application startup, and application shutdown
/// bounds. The heartbeat timeout H and the worker shutdown timeout D are fixed at 5 s.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Tests retain xUnit synchronization behavior.")]
public sealed class LifecycleBoundsDiscoveryTests
{
    // Before the SDK starts, with every setting at its default (S, P, A, B = 10, 30, 30, 30 s).
    [Fact]
    public async Task DefaultAllowancesArePublishedBeforeTheSdkStarts()
    {
        await using var server = PublishedServer.Create();

        var info = server.Discovery.CreateServerInfo();

        Assert.Equal(Api.WorkerRuntimeState.Stopped, info.WorkerState);
        Assert.Equal(Api.SpatialAnalyzerConnectionState.Unspecified, info.SpatialAnalyzerConnectionState);
        var bounds = info.LifecycleBounds;
        Assert.NotNull(bounds);
        Assert.Equal(45_000U, bounds.StartSdkMilliseconds); // S + P + D
        Assert.Equal(30_000U, bounds.LaunchSpatialAnalyzerMilliseconds); // A
        Assert.Equal(60_000U, bounds.ConnectSpatialAnalyzerMilliseconds); // 2S + P + H + D
        Assert.Equal(55_000U, bounds.RecoverSdkMilliseconds); // S + P + H + 2D
        Assert.Equal(15_000U, bounds.StopSdkMilliseconds); // H + 2D
        Assert.Equal(30_000U, bounds.CloseOwnedSpatialAnalyzerMilliseconds); // B
        Assert.Equal(100_000U, bounds.StartupWithLaunchMilliseconds); // 3S + P + A + H + D
        Assert.Equal(70_000U, bounds.StartupWithoutLaunchMilliseconds); // 3S + P + H + D
        Assert.True(bounds.HasStartSdkMilliseconds && bounds.HasLaunchSpatialAnalyzerMilliseconds &&
            bounds.HasConnectSpatialAnalyzerMilliseconds && bounds.HasRecoverSdkMilliseconds &&
            bounds.HasStopSdkMilliseconds && bounds.HasCloseOwnedSpatialAnalyzerMilliseconds &&
            bounds.HasStartupWithLaunchMilliseconds && bounds.HasStartupWithoutLaunchMilliseconds);
        // A + max(Connect, Recover) + Stop.
        Assert.Equal(Api.GracefulShutdownBound.LimitOneofCase.FiniteMilliseconds,
            bounds.HostGracefulShutdown.LimitCase);
        Assert.Equal(105_000UL, bounds.HostGracefulShutdown.FiniteMilliseconds);
        Assert.Equal(Api.ConfiguredSpatialAnalyzerInteractionMode.Unchanged,
            info.ConfiguredSpatialAnalyzerInteractionMode);
        server.AssertPublishedEqualsEnforced(info);
    }

    // Each setting is changed in turn, and every published value follows it. Values in ms:
    // start, launch, connect, recover, stop, close, startup with launch, without, host.
    [Theory]
    [InlineData(WorkerProcessOptions.StartupTimeoutKey, "00:00:20",
        55_000, 30_000, 80_000, 65_000, 15_000, 30_000, 130_000, 100_000, 125_000)]
    // S below D makes Recover (2 + 30 + 15) longer than Connect (4 + 30 + 10), so the
    // host bound uses Recover: 30 + 47 + 15.
    [InlineData(WorkerProcessOptions.StartupTimeoutKey, "00:00:02",
        37_000, 30_000, 44_000, 47_000, 15_000, 30_000, 76_000, 46_000, 92_000)]
    // Composed first, then rounded up once: Connect is 60,000.8 ms, never 60,002.
    [InlineData(WorkerProcessOptions.StartupTimeoutKey, "00:00:10.0004",
        45_001, 30_000, 60_001, 55_001, 15_000, 30_000, 100_002, 70_002, 105_001)]
    [InlineData(WorkerProcessOptions.ReadinessProbeTimeoutKey, "00:01:00",
        75_000, 30_000, 90_000, 85_000, 15_000, 30_000, 130_000, 100_000, 135_000)]
    [InlineData(SpatialAnalyzerApplicationOptions.StartupTimeoutKey, "00:02:00",
        45_000, 120_000, 60_000, 55_000, 15_000, 30_000, 190_000, 70_000, 195_000)]
    // B bounds only the close; it is not part of the host bound.
    [InlineData(SpatialAnalyzerApplicationOptions.ShutdownTimeoutKey, "00:01:30",
        45_000, 30_000, 60_000, 55_000, 15_000, 90_000, 100_000, 70_000, 105_000)]
    // The host's own shutdown setting is covered with a real web host below.
    public async Task EachPublishedAllowanceFollowsItsSetting(
        string key,
        string value,
        uint start,
        uint launch,
        uint connect,
        uint recover,
        uint stop,
        uint close,
        uint withLaunch,
        uint withoutLaunch,
        ulong host)
    {
        await using var server = PublishedServer.Create(new Dictionary<string, string?> { [key] = value });

        var info = server.Discovery.CreateServerInfo();

        var bounds = info.LifecycleBounds;
        Assert.Equal(start, bounds.StartSdkMilliseconds);
        Assert.Equal(launch, bounds.LaunchSpatialAnalyzerMilliseconds);
        Assert.Equal(connect, bounds.ConnectSpatialAnalyzerMilliseconds);
        Assert.Equal(recover, bounds.RecoverSdkMilliseconds);
        Assert.Equal(stop, bounds.StopSdkMilliseconds);
        Assert.Equal(close, bounds.CloseOwnedSpatialAnalyzerMilliseconds);
        Assert.Equal(withLaunch, bounds.StartupWithLaunchMilliseconds);
        Assert.Equal(withoutLaunch, bounds.StartupWithoutLaunchMilliseconds);
        Assert.Equal(host, bounds.HostGracefulShutdown.FiniteMilliseconds);
        server.AssertPublishedEqualsEnforced(info);
    }

    // Finite only when the host can arm it, unbounded only for an infinite timeout, and
    // otherwise absent. The server still starts, and the other allowances stay usable.
    [Theory]
    [InlineData(-1, null, true)]
    [InlineData(600_000L, 600_000UL, false)]
    [InlineData(4_294_967_294L, 4_294_967_294UL, false)]
    [InlineData(4_294_967_295L, null, false)]
    [InlineData(50L * 24 * 60 * 60 * 1000, null, false)]
    public async Task HostShutdownIsFiniteUnboundedOrAbsent(long milliseconds, ulong? finite, bool unbounded)
    {
        var configured = TimeSpan.FromTicks(milliseconds * TimeSpan.TicksPerMillisecond);
        await using var server = PublishedServer.Create(
            configureHost: services => services.Configure<HostOptions>(options => options.ShutdownTimeout = configured));

        var info = server.Discovery.CreateServerInfo();

        Assert.Equal(configured, server.HostOptions.ShutdownTimeout);
        var bound = info.LifecycleBounds.HostGracefulShutdown;
        if (unbounded)
        {
            Assert.Equal(Api.GracefulShutdownBound.LimitOneofCase.Unbounded, bound.LimitCase);
        }
        else if (finite is { } expected)
        {
            Assert.Equal(Api.GracefulShutdownBound.LimitOneofCase.FiniteMilliseconds, bound.LimitCase);
            Assert.Equal(expected, bound.FiniteMilliseconds);
        }
        else
        {
            Assert.Null(bound);
        }

        Assert.Equal(45_000U, info.LifecycleBounds.StartSdkMilliseconds);
        Assert.Equal(100_000U, info.LifecycleBounds.StartupWithLaunchMilliseconds);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-2L)]
    public void ANonPositiveFiniteHostTimeoutIsNotPublished(long milliseconds) =>
        Assert.Null(ServerLifecycleDiscovery.ToGracefulShutdownBound(
            TimeSpan.FromTicks(milliseconds * TimeSpan.TicksPerMillisecond)));

    // Pins the runtime behavior the omission rule relies on: the host arms the limit
    // itself, and one millisecond more makes StopAsync throw instead of arming it.
    [Fact]
    public async Task TheHostArmsTheLimitAndRejectsOneMillisecondMoreWhenItStops()
    {
        await StopHost(ServerLifecycleDiscovery.MaximumArmableTimeout);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            StopHost(ServerLifecycleDiscovery.MaximumArmableTimeout + TimeSpan.FromMilliseconds(1)));
    }

    // The configured mode is published by name, whatever the worker and connection state.
    [Theory]
    [InlineData(null, Api.ConfiguredSpatialAnalyzerInteractionMode.Unchanged)]
    [InlineData("Unchanged", Api.ConfiguredSpatialAnalyzerInteractionMode.Unchanged)]
    [InlineData("Silent", Api.ConfiguredSpatialAnalyzerInteractionMode.Silent)]
    [InlineData("Automatic", Api.ConfiguredSpatialAnalyzerInteractionMode.Automatic)]
    [InlineData("Manual", Api.ConfiguredSpatialAnalyzerInteractionMode.Manual)]
    public async Task TheConfiguredInteractionModeIsPublishedInEveryState(
        string? configured,
        Api.ConfiguredSpatialAnalyzerInteractionMode expected)
    {
        await using var server = PublishedServer.Create(new Dictionary<string, string?>
        {
            [SpatialAnalyzerConnectionOptions.InteractionModeKey] = configured
        });
        Assert.Equal(expected, server.Discovery.CreateServerInfo().ConfiguredSpatialAnalyzerInteractionMode);
        Assert.Equal(expected, ServerLifecycleDiscovery.ToProtocol(server.Supervisor.InteractionMode));
        var expectedBounds = server.Discovery.CreateServerInfo().LifecycleBounds;
        var status = new FixedStatus(Snapshot(WorkerLifecycleState.Stopped, connection: null));
        var discovery = new ServerDiscoveryService(status, server.Provider.GetRequiredService<IServerBuildIdentityProvider>(),
            server.Provider.GetRequiredService<OperationPolicy>(), server.Lifecycle);

        foreach (var snapshot in new[]
        {
            Snapshot(WorkerLifecycleState.Stopped, connection: null),
            Snapshot(WorkerLifecycleState.Ready, WorkerConnectionState.Disconnected),
            Snapshot(WorkerLifecycleState.Ready, WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.ExecutionReady),
            Snapshot(WorkerLifecycleState.Degraded, WorkerConnectionState.Faulted)
        })
        {
            status.Current = snapshot;
            var info = discovery.CreateServerInfo();
            Assert.Equal(expected, info.ConfiguredSpatialAnalyzerInteractionMode);
            Assert.Equal(expectedBounds, info.LifecycleBounds);
        }
    }

    // Program's own composition: a web host whose shutdownTimeoutSeconds setting is kept
    // when longer and raised when shorter, with the bound published as the host has it.
    [Theory]
    [InlineData(null, 105_000UL)]
    [InlineData("600", 600_000UL)]
    [InlineData("60", 105_000UL)]
    public async Task AWebHostBuiltLikeTheServerPublishesItsFinalShutdownBound(
        string? shutdownTimeoutSeconds,
        ulong expected)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration[OperationPolicy.ProfileKey] = "standard";
        if (shutdownTimeoutSeconds is not null)
            builder.Configuration[WebHostDefaults.ShutdownTimeoutKey] = shutdownTimeoutSeconds;
        builder.Services.AddWorkerProcessLifecycle(builder.Configuration);
        builder.Services.AddBriosaHealthAndDiscovery();
        builder.Services.AddSpatialAnalyzerLifecycle(builder.Configuration);
        builder.Services.AddSingleton<ServerDiscoveryService>();
        await using var app = builder.Build();

        var info = app.Services.GetRequiredService<ServerDiscoveryService>().CreateServerInfo();

        Assert.Equal(expected, info.LifecycleBounds.HostGracefulShutdown.FiniteMilliseconds);
        Assert.Equal(
            TimeSpan.FromMilliseconds(expected),
            app.Services.GetRequiredService<IOptions<HostOptions>>().Value.ShutdownTimeout);
    }

    // Each reply gets its own copy, so changing one never changes what later replies publish.
    [Fact]
    public async Task EachReplyCarriesItsOwnCopyOfTheBounds()
    {
        await using var server = PublishedServer.Create();
        var first = server.Discovery.CreateServerInfo();

        first.LifecycleBounds.StartSdkMilliseconds = 1;
        first.LifecycleBounds.HostGracefulShutdown.Unbounded = new Api.UnboundedShutdownWait();

        var second = server.Discovery.CreateServerInfo();
        Assert.Equal(45_000U, second.LifecycleBounds.StartSdkMilliseconds);
        Assert.Equal(105_000UL, second.LifecycleBounds.HostGracefulShutdown.FiniteMilliseconds);
    }

    private static async Task StopHost(TimeSpan shutdownTimeout)
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
                services.Configure<HostOptions>(options => options.ShutdownTimeout = shutdownTimeout))
            .Build();
        await host.StartAsync();
        await host.StopAsync();
    }

    private static WorkerLifecycleSnapshot Snapshot(
        WorkerLifecycleState state,
        WorkerConnectionState? connection,
        WorkerExecutionReadinessState readiness = WorkerExecutionReadinessState.Unverified) =>
        new(state, Generation: 1, ProcessId: null, RecoveryCount: 0, WorkerTerminationKind.None,
            "test", connection is { } value
                ? new WorkerConnectionSnapshot(value, readiness, StatusCode: 0, Attempt: 1,
                    MaximumAttempts: 1, "test", DateTimeOffset.UtcNow)
                : null,
            DateTimeOffset.UtcNow);

    private sealed class FixedStatus(WorkerLifecycleSnapshot current) : IWorkerStatusProvider
    {
        public WorkerLifecycleSnapshot Current { get; set; } = current;
    }

    // The server's registrations, in Program's order, over in-memory configuration.
    private sealed class PublishedServer : IAsyncDisposable
    {
        private PublishedServer(ServiceProvider provider) => Provider = provider;

        public ServiceProvider Provider { get; }
        public ServerDiscoveryService Discovery => Provider.GetRequiredService<ServerDiscoveryService>();
        public ServerLifecycleDiscovery Lifecycle => Provider.GetRequiredService<ServerLifecycleDiscovery>();
        public WorkerProcessSupervisor Supervisor => Provider.GetRequiredService<WorkerProcessSupervisor>();
        public HostOptions HostOptions => Provider.GetRequiredService<IOptions<HostOptions>>().Value;

        public static PublishedServer Create(
            IDictionary<string, string?>? settings = null,
            Action<IServiceCollection>? configureHost = null)
        {
            var values = new Dictionary<string, string?> { [OperationPolicy.ProfileKey] = "standard" };
            foreach (var (key, value) in settings ?? new Dictionary<string, string?>()) values[key] = value;
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            // The host's own configuration registers before the server's, as in Program.
            configureHost?.Invoke(services);
            services.AddWorkerProcessLifecycle(configuration);
            services.AddBriosaHealthAndDiscovery();
            services.AddSpatialAnalyzerLifecycle(configuration);
            services.AddSingleton<ServerDiscoveryService>();
            return new PublishedServer(services.BuildServiceProvider());
        }

        // Every published allowance comes from the instances the server enforces: the
        // supervisor's own policy and mode, the application options the coordinator
        // uses, and the final host options the host stops with.
        public void AssertPublishedEqualsEnforced(Api.GetServerInfoResponse info)
        {
            var policy = Supervisor.LifecyclePolicy;
            Assert.Same(policy, Provider.GetRequiredService<WorkerLifecycleProjection>().Policy);
            var application = Provider.GetRequiredService<SpatialAnalyzerApplicationOptions>();
            var bounds = info.LifecycleBounds;
            Assert.Equal(Milliseconds(policy.StartBound), bounds.StartSdkMilliseconds);
            Assert.Equal(Milliseconds(application.StartupTimeout), bounds.LaunchSpatialAnalyzerMilliseconds);
            Assert.Equal(Milliseconds(policy.ConnectBound), bounds.ConnectSpatialAnalyzerMilliseconds);
            Assert.Equal(Milliseconds(policy.RecoverBound), bounds.RecoverSdkMilliseconds);
            Assert.Equal(Milliseconds(policy.StopBound), bounds.StopSdkMilliseconds);
            Assert.Equal(Milliseconds(application.ShutdownTimeout), bounds.CloseOwnedSpatialAnalyzerMilliseconds);
            Assert.Equal(Milliseconds(policy.StartupTimeout + application.StartupTimeout + policy.ConnectBound),
                bounds.StartupWithLaunchMilliseconds);
            Assert.Equal(Milliseconds(policy.StartupTimeout + policy.ConnectBound),
                bounds.StartupWithoutLaunchMilliseconds);
            Assert.Equal(Milliseconds(HostOptions.ShutdownTimeout), bounds.HostGracefulShutdown.FiniteMilliseconds);
            Assert.Equal(ServerLifecycleDiscovery.ToProtocol(Supervisor.InteractionMode),
                info.ConfiguredSpatialAnalyzerInteractionMode);
        }

        public ValueTask DisposeAsync() => Provider.DisposeAsync();

        private static uint Milliseconds(TimeSpan duration) =>
            checked((uint)Math.Ceiling(duration.Ticks / (double)TimeSpan.TicksPerMillisecond));
    }
}
