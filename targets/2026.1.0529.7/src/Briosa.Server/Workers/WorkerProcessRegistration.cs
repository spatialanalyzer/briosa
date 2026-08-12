using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
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

        var workerOptions = WorkerProcessOptions.BindAndValidate(configuration);
        var spatialAnalyzerOptions =
            SpatialAnalyzerConnectionOptions.BindAndValidate(configuration);
        var identityPolicy = ExactTargetIdentityPolicy.Create(
            spatialAnalyzerOptions.Identity,
            SpatialAnalyzerApi.TargetVersion);
        services.TryAddSingleton(workerOptions);
        services.TryAddSingleton(spatialAnalyzerOptions);
        services.TryAddSingleton(identityPolicy);

        services.TryAddSingleton(provider =>
        {
            var processFactory = new NamedPipeWorkerProcessFactory(
                _ => new WorkerProcessLaunch(
                    workerOptions.ExecutablePath,
                    ["--sa-host", spatialAnalyzerOptions.Host],
                    workingDirectory: Path.GetDirectoryName(workerOptions.ExecutablePath)));
            var policy = new WorkerRestartPolicy(
                maximumRestarts: 3,
                restartWindow: TimeSpan.FromMinutes(1),
                heartbeatInterval: TimeSpan.FromSeconds(1),
                heartbeatTimeout: TimeSpan.FromSeconds(5),
                startupTimeout: TimeSpan.FromSeconds(10),
                shutdownTimeout: TimeSpan.FromSeconds(5),
                restartDelay: TimeSpan.FromSeconds(1));
            var executionPolicy = new WorkerExecutionPolicy(
                watchdogTimeout: workerOptions.ExecutionWatchdogTimeout,
                queueCapacity: 64);
            return new WorkerProcessSupervisor(
                processFactory,
                policy,
                executionPolicy,
                logger: provider.GetRequiredService<ILogger<WorkerProcessSupervisor>>(),
                identityPolicy: provider.GetRequiredService<ExactTargetIdentityPolicy>());
        });
        services.TryAddSingleton<OperationAuditLogger>();
        services.TryAddSingleton(_ => OperationPolicy.Create(
            configuration,
            SpatialAnalyzerApi.Operations));
        services.TryAddSingleton<PolicyEnforcingWorkerCommandExecutor>();
        services.TryAddSingleton<IWorkerCommandExecutor>(provider =>
            provider.GetRequiredService<PolicyEnforcingWorkerCommandExecutor>());
        services.TryAddSingleton<IWorkerStatusProvider>(provider =>
            provider.GetRequiredService<WorkerProcessSupervisor>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, WorkerSupervisorHostedService>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, OperationPolicyAuditHostedService>());
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
        cancellationToken.ThrowIfCancellationRequested();
        LogControlPlaneReady();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _supervisor.StopAsync(cancellationToken);

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Briosa control plane is ready; SpatialAnalyzer SDK startup awaits an explicit lifecycle RPC.")]
    private partial void LogControlPlaneReady();

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Briosa worker generation {Generation} is ready and connected with ConnectEx status {StatusCode}.")]
    private partial void LogWorkerReady(int generation, int? statusCode);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Briosa worker process is degraded: {DiagnosticCode}.")]
    private partial void LogWorkerDegraded(string diagnosticCode);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Briosa worker generation {Generation} is control-ready but its SDK connection is {ConnectionState} with execution readiness {ExecutionReadinessState}; ConnectEx status {StatusCode}, diagnostic {DiagnosticCode}.")]
    private partial void LogWorkerReadyWithoutSdk(
        int generation,
        WorkerConnectionState connectionState,
        WorkerExecutionReadinessState executionReadinessState,
        int? statusCode,
        string diagnosticCode);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Briosa worker generation {Generation} attached but exact-target identity is not ready, so the execution-channel probe was not admitted: activated SDK {ActivatedSdkIdentitySource}/{ActivatedSdkIdentityMatchState}, connected SA {ConnectedSaIdentitySource}/{ConnectedSaIdentityMatchState}.")]
    private partial void LogWorkerIdentityNotReady(
        int generation,
        RuntimeIdentityEvidenceSource activatedSdkIdentitySource,
        RuntimeIdentityMatchState activatedSdkIdentityMatchState,
        RuntimeIdentityEvidenceSource connectedSaIdentitySource,
        RuntimeIdentityMatchState connectedSaIdentityMatchState);
}
