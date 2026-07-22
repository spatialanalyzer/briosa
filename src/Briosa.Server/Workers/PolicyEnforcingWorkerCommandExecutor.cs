using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class PolicyEnforcingWorkerCommandExecutor(
    WorkerProcessSupervisor supervisor,
    OperationPolicy policy,
    OperationAuditLogger auditLogger) : IWorkerCommandExecutor
{
    private readonly OperationAuditLogger _auditLogger =
        auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    private readonly OperationPolicy _policy =
        policy ?? throw new ArgumentNullException(nameof(policy));
    private readonly WorkerProcessSupervisor _supervisor =
        supervisor ?? throw new ArgumentNullException(nameof(supervisor));

    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(command, Guid.NewGuid(), cancellationToken);

    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var effectiveCorrelationId = correlationId != Guid.Empty
            ? correlationId
            : Guid.NewGuid();
        var decision = _policy.Evaluate(command);
        _auditLogger.PolicyEvaluated(effectiveCorrelationId, decision);
        return decision.Kind switch
        {
            OperationPolicyDecisionKind.Allowed => _supervisor.ExecuteAsync(
                command,
                effectiveCorrelationId,
                cancellationToken),
            OperationPolicyDecisionKind.Denied => Task.FromResult(Rejected(
                WorkerExecutionStatus.PolicyDenied,
                decision.DiagnosticCode,
                effectiveCorrelationId)),
            _ => Task.FromResult(Rejected(
                WorkerExecutionStatus.Unsupported,
                decision.DiagnosticCode,
                effectiveCorrelationId))
        };
    }

    private WorkerExecutionOutcome Rejected(
        WorkerExecutionStatus status,
        string diagnosticCode,
        Guid correlationId) =>
        new(
            status,
            Execution: null,
            _supervisor.Current.Connection,
            diagnosticCode,
            _supervisor.Current.Generation,
            correlationId);
}
