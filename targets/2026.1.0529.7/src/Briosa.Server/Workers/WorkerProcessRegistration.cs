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
            var policy = new WorkerLifecyclePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(1),
                heartbeatTimeout: TimeSpan.FromSeconds(5),
                startupTimeout: TimeSpan.FromSeconds(10),
                shutdownTimeout: TimeSpan.FromSeconds(5));
            var executionPolicy = new WorkerExecutionPolicy(
                watchdogTimeout: workerOptions.ExecutionWatchdogTimeout,
                queueCapacity: 64);
            return new WorkerProcessSupervisor(
                processFactory,
                policy,
                executionPolicy,
                logger: provider.GetRequiredService<ILogger<WorkerProcessSupervisor>>(),
                identityPolicy: provider.GetRequiredService<ExactTargetIdentityPolicy>(),
                telemetry: provider.GetService<BriosaTelemetry>());
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
