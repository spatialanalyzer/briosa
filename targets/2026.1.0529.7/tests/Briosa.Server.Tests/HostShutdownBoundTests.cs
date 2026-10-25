using Briosa.Server.Services;
using Briosa.Server.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Briosa.Server.Tests;

/// <summary>
/// #321: a graceful host stop waits for accepted lifecycle work (#315, #322), so the
/// host shutdown timeout must cover it. A Connect can wait behind an accepted launch,
/// so the bound is the application startup bound, then the longest accepted SDK
/// exchange to its typed failure, then the worker stop. The heartbeat timeout and the
/// worker shutdown timeout are fixed at 5 seconds each.
/// </summary>
public sealed class HostShutdownBoundTests
{
    [Theory]
    // Defaults: launch 30; Connect = ConnectEx 10 + probe 30 + association 10 +
    // liveness 5 + cleanup 5 = 60; worker stop = drain 5 + stop 5 + cleanup 5 = 15.
    [InlineData(null, null, null, 30 + 60 + 15)]
    // Connect uses the startup bound twice.
    [InlineData("00:01:00", null, null, 30 + (60 + 30 + 60 + 5 + 5) + 15)]
    [InlineData(null, "00:05:00", null, 30 + (10 + 300 + 10 + 5 + 5) + 15)]
    [InlineData(null, null, "00:10:00", 600 + 60 + 15)]
    // A startup bound below the worker shutdown bound makes Recover the longest:
    // drain 5 + cleanup 5 + start 2 + probe 30 + cleanup 5.
    [InlineData("00:00:02", null, null, 30 + (5 + 5 + 2 + 30 + 5) + 15)]
    public void HostShutdownTimeoutCoversAcceptedLifecycleWorkAndTheWorkerStop(
        string? startupTimeout,
        string? readinessProbeTimeout,
        string? applicationStartupTimeout,
        int expectedSeconds)
    {
        var settings = new Dictionary<string, string?>();
        if (startupTimeout is not null) settings[WorkerProcessOptions.StartupTimeoutKey] = startupTimeout;
        if (readinessProbeTimeout is not null)
            settings[WorkerProcessOptions.ReadinessProbeTimeoutKey] = readinessProbeTimeout;
        if (applicationStartupTimeout is not null)
            settings[SpatialAnalyzerApplicationOptions.StartupTimeoutKey] = applicationStartupTimeout;

        var options = HostOptionsFor(settings);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), options.ShutdownTimeout);
    }

    // The host's own longer or infinite shutdown timeout is never shortened.
    [Theory]
    [InlineData(600_000)]
    [InlineData(-1)]
    public void LongerConfiguredHostShutdownTimeoutIsKept(int milliseconds)
    {
        var configured = TimeSpan.FromMilliseconds(milliseconds);

        var options = HostOptionsFor(
            new Dictionary<string, string?>(),
            services => services.Configure<HostOptions>(host => host.ShutdownTimeout = configured));

        Assert.Equal(configured, options.ShutdownTimeout);
    }

    // The bound reaches a web host built as Program builds it, after the host's own
    // shutdownTimeoutSeconds setting: a longer setting wins, a shorter one is raised.
    [Theory]
    [InlineData(null, 105)]
    [InlineData("600", 600)]
    [InlineData("60", 105)]
    public async Task WebHostUsesTheLifecycleShutdownBound(string? shutdownTimeoutSeconds, int expectedSeconds)
    {
        var builder = WebApplication.CreateSlimBuilder();
        if (shutdownTimeoutSeconds is not null)
            builder.Configuration[WebHostDefaults.ShutdownTimeoutKey] = shutdownTimeoutSeconds;
        builder.Services.AddWorkerProcessLifecycle(builder.Configuration);
        builder.Services.AddSpatialAnalyzerLifecycle(builder.Configuration);
        await using var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), options.ShutdownTimeout);
    }

    // The host options as the server's registrations configure them. The host's own
    // configuration registers before them, as in Program.
    internal static HostOptions HostOptionsFor(
        IDictionary<string, string?> settings,
        Action<IServiceCollection>? configureHost = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddOptions();
        configureHost?.Invoke(services);
        services.AddWorkerProcessLifecycle(configuration);
        services.AddSpatialAnalyzerLifecycle(configuration);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<HostOptions>>().Value;
    }
}
