using Briosa.Desktop;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Tests retain xUnit synchronization behavior.")]
public sealed class DesktopHostTests
{
    [Fact]
    public async Task PrivateChannelGuardsOwnershipAndGenerationAndReusesSdkLifecycle()
    {
        var instance = DesktopProtocol.NewInstance();
        var credential = DesktopProtocol.NewCredential();
        var configuration = new ConfigurationBuilder().Build();
        await using var supervisor = CreateSupervisor("disconnected");
        var application = new FakeApplication();
        await using var sdk = new SpatialAnalyzerSdkLifecycleCoordinator(supervisor, new(supervisor), application);
        var discovery = new ServerDiscoveryService(supervisor, new FakeIdentity(), OperationPolicy.Create(configuration, SpatialAnalyzerApi.Operations));
        using var lifetime = new FakeLifetime();
        using var host = new DesktopHost(new(true, false, instance, credential), lifetime, configuration, discovery, sdk,
            application, Array.Empty<ILoggerProvider>(), new() { FileEnabled = false });
        await host.StartAsync(CancellationToken.None);
        await lifetime.Started.CancelAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var monitor = await DesktopProtocol.SendAsync(new(instance), timeout.Token);
        Assert.False(monitor.CanManage);
        Assert.Equal("Running", monitor.HostState);
        Assert.False(DesktopState.FromReply(monitor).Ready);
        var rejected = await DesktopProtocol.SendAsync(new(instance, DesktopAction.StartSdk), timeout.Token);
        Assert.False(rejected.Accepted);
        Assert.Equal(SpatialAnalyzerSdkState.Stopped, sdk.Current.SdkState);
        var staleInstance = await host.DispatchAsync(new(DesktopProtocol.NewInstance(), DesktopAction.StartSdk, credential), timeout.Token);
        Assert.False(staleInstance.Accepted);
        var started = await DesktopProtocol.SendAsync(new(instance, DesktopAction.StartSdk, credential), timeout.Token);
        var startedState = DesktopState.FromReply(started);
        Assert.True(started.Accepted);
        Assert.Equal(SpatialAnalyzerSdkState.Running, startedState.Sdk!.SdkState);
        Assert.False(startedState.Ready);
        var stale = await DesktopProtocol.SendAsync(new(instance, DesktopAction.Connect, credential, 999), timeout.Token);
        Assert.False(stale.Accepted);
        var connected = await DesktopProtocol.SendAsync(new(instance, DesktopAction.Connect, credential, startedState.Sdk.SdkGeneration), timeout.Token);
        Assert.True(connected.Accepted);
        Assert.True(DesktopState.FromReply(connected).Ready);
        Assert.False((await DesktopProtocol.SendAsync(new(instance, DesktopAction.StopServer, DesktopProtocol.NewCredential()), timeout.Token)).Accepted);
        Assert.False(lifetime.Stopped.IsCancellationRequested);
        var stop = await DesktopProtocol.SendAsync(new(instance, DesktopAction.StopServer, credential), timeout.Token);
        Assert.True(stop.Accepted);
        await Task.Delay(50, timeout.Token);
        Assert.True(lifetime.Stopped.IsCancellationRequested);
        await host.StopAsync(timeout.Token);
    }

    [Fact]
    public async Task StartupFailureIsObservableAsRecoveryRequired()
    {
        var configuration = new ConfigurationBuilder().Build();
        await using var supervisor = CreateSupervisor("sdk-activation-failed");
        var application = new FakeApplication();
        await using var sdk = new SpatialAnalyzerSdkLifecycleCoordinator(supervisor, new(supervisor), application);
        var credential = DesktopProtocol.NewCredential();
        var instance = DesktopProtocol.NewInstance();
        using var lifetime = new FakeLifetime();
        using var host = new DesktopHost(new(true, false, instance, credential), lifetime, configuration,
            new(supervisor, new FakeIdentity(), OperationPolicy.Create(configuration, SpatialAnalyzerApi.Operations)), sdk,
            application, Array.Empty<ILoggerProvider>(), new() { FileEnabled = false });
        await host.StartAsync(CancellationToken.None);
        await lifetime.Started.CancelAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var failed = await DesktopProtocol.SendAsync(new(instance, DesktopAction.StartSdk, credential), timeout.Token);
        Assert.False(failed.Accepted);
        Assert.False(DesktopState.FromReply(failed).Ready);
        Assert.Equal(SpatialAnalyzerSdkRecoveryState.RecoveryAvailable, sdk.Current.RecoveryState);
        Assert.NotNull(sdk.Current.LastIncident);
        await host.StopAsync(timeout.Token);
    }

    private static WorkerProcessSupervisor CreateSupervisor(string scenario) => new(
        new NamedPipeWorkerProcessFactory(_ => new WorkerProcessLaunch(Path.Combine(AppContext.BaseDirectory,
            "worker-test-host", "Briosa.Worker.TestHost.exe"), ["--scenario", scenario])),
        new WorkerLifecyclePolicy(TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2)),
        new WorkerExecutionPolicy(TimeSpan.FromSeconds(2), 4),
        identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(DesktopProtocol.Target, DesktopProtocol.Target, DesktopProtocol.Target));

    private sealed class FakeApplication : ISpatialAnalyzerLifecycleStateProvider
    {
        public Task<SpatialAnalyzerLifecycleState> GetCurrentAsync(CancellationToken cancellationToken) => Task.FromResult(new SpatialAnalyzerLifecycleState
        { ApplicationState = SpatialAnalyzerApplicationState.Running, Ownership = SpatialAnalyzerOwnership.External });
    }

    private sealed class FakeIdentity : IServerBuildIdentityProvider
    {
        public VersionCoordinates CreateVersionCoordinates() => new() { SpatialAnalyzerTarget = DesktopProtocol.Target, ProtocolPackage = "briosa" };
    }

    private sealed class FakeLifetime : IHostApplicationLifetime, IDisposable
    {
        public CancellationTokenSource Started { get; } = new();
        public CancellationTokenSource Stopped { get; } = new();
        public CancellationToken ApplicationStarted => Started.Token;
        public CancellationToken ApplicationStopping => Stopped.Token;
        public CancellationToken ApplicationStopped => Stopped.Token;
        public void StopApplication() => Stopped.Cancel();
        public void Dispose() { Started.Dispose(); Stopped.Dispose(); }
    }
}
