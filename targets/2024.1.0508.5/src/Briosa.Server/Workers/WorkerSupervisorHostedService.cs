namespace Briosa.Server.Workers;

internal sealed partial class WorkerSupervisorHostedService(
    WorkerProcessSupervisor supervisor,
    ILogger<WorkerSupervisorHostedService> logger) : IHostedService
{
    private readonly ILogger<WorkerSupervisorHostedService> _logger = logger;
    private readonly WorkerProcessSupervisor _supervisor = supervisor;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogControlPlaneReady();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _supervisor.StopAsync(cancellationToken);

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Briosa control plane is ready; SpatialAnalyzer SDK startup awaits an explicit lifecycle RPC.")]
    private partial void LogControlPlaneReady();
}
