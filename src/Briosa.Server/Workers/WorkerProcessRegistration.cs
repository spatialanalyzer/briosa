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

        services.TryAddSingleton(_ =>
        {
            var processFactory = new NamedPipeWorkerProcessFactory(
                _ => new WorkerProcessLaunch(
                    executablePath,
                    workingDirectory: Path.GetDirectoryName(executablePath)));
            var policy = new WorkerRestartPolicy(
                maximumRestarts: 3,
                restartWindow: TimeSpan.FromMinutes(1),
                heartbeatInterval: TimeSpan.FromSeconds(1),
                heartbeatTimeout: TimeSpan.FromSeconds(5),
                startupTimeout: TimeSpan.FromSeconds(10),
                shutdownTimeout: TimeSpan.FromSeconds(5),
                restartDelay: TimeSpan.FromSeconds(1));
            return new WorkerProcessSupervisor(processFactory, policy);
        });
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
        if (started)
        {
            LogWorkerReady(_supervisor.Current.Generation);
        }
        else
        {
            LogWorkerDegraded(_supervisor.Current.DiagnosticCode);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _supervisor.StopAsync(cancellationToken);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Briosa worker generation {Generation} is ready.")]
    private partial void LogWorkerReady(int generation);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Briosa worker is degraded: {DiagnosticCode}.")]
    private partial void LogWorkerDegraded(string diagnosticCode);
}
