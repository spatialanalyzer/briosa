using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;

namespace Briosa.Server.Services;

/// <summary>
/// Owns the policy, outcome, and audit seam shared by handwritten MP services.
/// </summary>
internal sealed class OperationExecutor(
    IWorkerCommandExecutor executor,
    OperationAuditLogger auditLogger,
    TimeProvider timeProvider)
{
    private readonly OperationAuditLogger _auditLogger =
        auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    private readonly IWorkerCommandExecutor _executor =
        executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        OperationDescriptor operation,
        Func<TRequest, WorkerMpCommand> createCommand,
        IReadOnlyList<OperationOutputContract> outputContracts,
        Func<SuccessfulOperationExecution, TResponse> createResult)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return ExecuteAsync(
            request,
            operation,
            createCommand,
            outputContracts,
            createResult,
            context.CancellationToken,
            context.Deadline,
            Guid.NewGuid(),
            ClassifyActor(context.Peer));
    }

    internal async Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        OperationDescriptor operation,
        Func<TRequest, WorkerMpCommand> createCommand,
        IReadOnlyList<OperationOutputContract> outputContracts,
        Func<SuccessfulOperationExecution, TResponse> createResult,
        CancellationToken cancellationToken,
        DateTime? deadline = null,
        Guid? correlationId = null,
        string actorCategory = "internal-unattributed")
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(createCommand);
        ArgumentNullException.ThrowIfNull(outputContracts);
        ArgumentNullException.ThrowIfNull(createResult);

        var effectiveCorrelationId = correlationId is { } value && value != Guid.Empty
            ? value
            : Guid.NewGuid();
        _auditLogger.RequestStarted(effectiveCorrelationId, operation, actorCategory);
        var startedAt = _timeProvider.GetTimestamp();
        WorkerExecutionOutcome? outcome = null;
        try
        {
            WorkerMpCommand command;
            try
            {
                command = createCommand(request);
            }
            catch (ArgumentException)
            {
                throw GrpcOperationOutcomeMapper.CreateValidationFailure(
                    operation.OperationId,
                    "request-validation-failed",
                    replaySafety: operation.ReplaySafety);
            }

            outcome = await _executor.ExecuteAsync(
                command,
                effectiveCorrelationId,
                cancellationToken).ConfigureAwait(false);
            var completed = GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                command.OperationId,
                operation.ReplaySafety,
                outputContracts,
                deadline is not null &&
                deadline.Value != DateTime.MaxValue &&
                deadline.Value <= _timeProvider.GetUtcNow().UtcDateTime);

            TResponse result;
            try
            {
                result = createResult(completed) ??
                    throw new InvalidOperationException("The operation result mapper returned null.");
            }
            catch (Exception exception) when (exception is
                ArgumentException or InvalidOperationException or OverflowException)
            {
                throw GrpcOperationOutcomeMapper.CreateResultMappingFailure(
                    operation.OperationId,
                    operation.ReplaySafety,
                    outcome.Generation,
                    completed.Details);
            }

            _auditLogger.OperationCompleted(
                EffectiveCorrelationId(outcome, effectiveCorrelationId),
                command.OperationId,
                outcome.Generation,
                RequestDurationMilliseconds(startedAt),
                OperationAuditSummary.Create(outcome));
            return result;
        }
        catch (RpcException exception)
        {
            _auditLogger.OperationFailed(
                EffectiveCorrelationId(outcome, effectiveCorrelationId),
                operation.OperationId,
                outcome?.Generation ?? 0,
                RequestDurationMilliseconds(startedAt),
                OperationAuditSummary.Create(outcome),
                exception.StatusCode,
                GrpcOperationOutcomeMapper.GetDiagnosticCode(exception));
            throw;
        }
    }

    private long RequestDurationMilliseconds(long startedAt) =>
        Math.Max(0, (long)_timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);

    private static Guid EffectiveCorrelationId(
        WorkerExecutionOutcome? outcome,
        Guid fallback) =>
        outcome is { CorrelationId: var value } && value != Guid.Empty ? value : fallback;

    private static string ClassifyActor(string peer) =>
        peer.StartsWith("ipv4:127.", StringComparison.OrdinalIgnoreCase) ||
        peer.StartsWith("ipv6:[::1]", StringComparison.OrdinalIgnoreCase)
            ? "local-unauthenticated"
            : "unverified-unauthenticated";
}
