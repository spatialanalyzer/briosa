using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class PolicyEnforcingWorkerCommandExecutor(
    IWorkerCommandDispatcher dispatcher,
    IWorkerStatusProvider statusProvider,
    OperationPolicy policy,
    OperationAuditLogger auditLogger) : IWorkerCommandExecutor
{
    private readonly OperationAuditLogger _auditLogger =
        auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    private readonly OperationPolicy _policy =
        policy ?? throw new ArgumentNullException(nameof(policy));
    private readonly IWorkerCommandDispatcher _dispatcher =
        dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private readonly IWorkerStatusProvider _statusProvider =
        statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));

    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(command, Guid.NewGuid(), cancellationToken);

    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerMpCommand command,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(new WorkerCommandSubmission(command.OperationId, () => command),
            correlationId, cancellationToken);

    public Task<WorkerExecutionOutcome> ExecuteAsync(
        WorkerCommandSubmission submission,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        var effectiveCorrelationId = correlationId != Guid.Empty
            ? correlationId
            : Guid.NewGuid();
        // Classify the typed request before mapping, reservation, or dispatch.
        var decision = _policy.EvaluateRequest(submission.OperationId, submission.Request);
        _auditLogger.PolicyEvaluated(effectiveCorrelationId, decision);
        return decision.Kind switch
        {
            OperationPolicyDecisionKind.Allowed => _dispatcher.ExecuteAsync(
                submission with
                {
                    CreateCommand = () => CreateValidatedCommand(submission),
                    DurationClass = decision.DurationClass
                },
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

    private WorkerMpCommand CreateValidatedCommand(WorkerCommandSubmission submission)
    {
        var command = submission.CreateCommand();
        if (command.OperationId != submission.OperationId ||
            _policy.EvaluateRequest(command, submission.Request).Kind != OperationPolicyDecisionKind.Allowed)
        {
            throw new ArgumentException("The operation command does not match its registration.");
        }
        return command;
    }

    private WorkerExecutionOutcome Rejected(
        WorkerExecutionStatus status,
        string diagnosticCode,
        Guid correlationId) =>
        new(
            status,
            WorkerExecutionDisposition.NotStarted,
            Execution: null,
            _statusProvider.Current.Connection,
            diagnosticCode,
            _statusProvider.Current.Generation,
            correlationId);
}
