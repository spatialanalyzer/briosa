using Briosa.Worker.Control;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Briosa.Server.Workers;

internal static class WorkerProcessRegistration
{
    public static IServiceCollection AddWorkerProcessLifecycle(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredPath = configuration["Briosa:Worker:ExecutablePath"];
        var executablePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "Briosa.Worker.exe")
            : Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
        var configuredHost = configuration["Briosa:SpatialAnalyzer:Host"];
        var targetHost = string.IsNullOrWhiteSpace(configuredHost)
            ? "localhost"
            : configuredHost;

        services.TryAddSingleton(_ =>
        {
            var processFactory = new NamedPipeWorkerProcessFactory(
                _ => new WorkerProcessLaunch(
                    executablePath,
                    ["--sa-host", targetHost],
                    workingDirectory: Path.GetDirectoryName(executablePath)));
            var policy = new WorkerRestartPolicy(
                maximumRestarts: 3,
                restartWindow: TimeSpan.FromMinutes(1),
                heartbeatInterval: TimeSpan.FromSeconds(1),
                heartbeatTimeout: TimeSpan.FromSeconds(5),
                startupTimeout: TimeSpan.FromSeconds(10),
                shutdownTimeout: TimeSpan.FromSeconds(5),
                restartDelay: TimeSpan.FromSeconds(1));
            var executionPolicy = new WorkerExecutionPolicy(
                watchdogTimeout: TimeSpan.FromSeconds(30),
                queueCapacity: 64);
            return new WorkerProcessSupervisor(
                processFactory,
                policy,
                executionPolicy);
        });
        services.TryAddSingleton<IWorkerCommandExecutor>(provider =>
            provider.GetRequiredService<WorkerProcessSupervisor>());
        services.TryAddSingleton<IWorkerStatusProvider>(provider =>
            provider.GetRequiredService<WorkerProcessSupervisor>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, WorkerSupervisorHostedService>());
        return services;
    }
}

internal sealed partial class WorkerSupervisorHostedService(
    WorkerProcessSupervisor supervisor,
    ILogger<WorkerSupervisorHostedService> logger) : IHostedService
{
    private readonly ILogger<WorkerSupervisorHostedService> _logger = logger;
    private readonly WorkerProcessSupervisor _supervisor = supervisor;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var started = await _supervisor.StartAsync(cancellationToken).ConfigureAwait(false);
        if (!started)
        {
            LogWorkerDegraded(_supervisor.Current.DiagnosticCode);
            return;
        }

        var connection = _supervisor.Current.Connection!;
        if (connection.State == WorkerConnectionState.Connected)
        {
            LogWorkerReady(
                _supervisor.Current.Generation,
                connection.TargetHost,
                connection.StatusCode);
        }
        else
        {
            LogWorkerReadyWithoutSdk(
                _supervisor.Current.Generation,
                connection.State,
                connection.TargetHost,
                connection.StatusCode,
                connection.DiagnosticCode);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _supervisor.StopAsync(cancellationToken);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Briosa worker generation {Generation} is ready and connected to {TargetHost} with ConnectEx status {StatusCode}.")]
    private partial void LogWorkerReady(int generation, string targetHost, int? statusCode);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Briosa worker process is degraded: {DiagnosticCode}.")]
    private partial void LogWorkerDegraded(string diagnosticCode);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Briosa worker generation {Generation} is control-ready but its SDK connection is {ConnectionState} for {TargetHost}; ConnectEx status {StatusCode}, diagnostic {DiagnosticCode}.")]
    private partial void LogWorkerReadyWithoutSdk(
        int generation,
        WorkerConnectionState connectionState,
        string targetHost,
        int? statusCode,
        string diagnosticCode);
}
