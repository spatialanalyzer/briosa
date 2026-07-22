using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Briosa.Server.Services;

internal sealed class WorkerReadinessHealthCheck(IWorkerStatusProvider statusProvider) : IHealthCheck
{
    internal const string LivenessServiceName = "briosa.liveness";
    internal const string ReadinessServiceName = "briosa.readiness";

    private readonly IWorkerStatusProvider _statusProvider =
        statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var snapshot = _statusProvider.Current;
        var result = IsReady(snapshot)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
        return Task.FromResult(result);
    }

    internal static bool IsReady(WorkerLifecycleSnapshot snapshot) =>
        snapshot.State == WorkerLifecycleState.Ready &&
        snapshot.Connection?.State == WorkerConnectionState.Connected;
}
