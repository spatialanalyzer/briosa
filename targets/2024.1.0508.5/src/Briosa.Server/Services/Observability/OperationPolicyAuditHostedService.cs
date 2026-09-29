using Briosa.Server.Security;

namespace Briosa.Server.Services;

internal sealed class OperationPolicyAuditHostedService(
    OperationPolicy policy,
    OperationAuditLogger auditLogger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        auditLogger.PolicyLoaded(policy);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
