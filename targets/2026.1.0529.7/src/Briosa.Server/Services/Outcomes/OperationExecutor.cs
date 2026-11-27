using Briosa.Server.Operations;
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
    TimeProvider timeProvider,
    BriosaTelemetry? telemetry = null)
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

    // gRPC receives at most 64 KiB. Budget eight bytes of retained work per
    // protobuf wire byte plus 16 KiB for the mapped command and queue metadata.
    // This is a conservative admission budget, not an exact CLR heap measurement.
    private static int RetainedRequestBudget<TRequest>(TRequest request)
    {
        if (request is not Google.Protobuf.IMessage message) return 1024 * 1024;
        return (int)Math.Min(1024 * 1024, 16 * 1024L + 8L * message.CalculateSize());
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
        using var activity = BriosaTelemetry.Start("briosa.rpc", operation.OperationId);
        _auditLogger.RequestStarted(effectiveCorrelationId, operation, actorCategory);
        var startedAt = _timeProvider.GetTimestamp();
        var rpcStatus = StatusCode.Internal;
        WorkerExecutionOutcome? outcome = null;
        OperationAuditSummary? failureSummary = null;
        var mapping = new RequestMappingAttempt<TRequest>(request, createCommand);
        try
        {
            try
            {
                outcome = await _executor.ExecuteAsync(
                    new WorkerCommandSubmission(operation.OperationId, mapping.Create,
                        RetainedRequestBudget(request), request as Google.Protobuf.IMessage),
                    effectiveCorrelationId,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (ArgumentException exception) when (mapping.FailedAfterMapping(exception))
            {
                // A typed violation that the request mapping did not throw, or any argument
                // failure after a repeated mapping, once a mapped command left this
                // executor: not a request validation failure, and Briosa cannot prove that
                // the command did not start. The audit records the same disposition.
                const WorkerExecutionDisposition disposition = WorkerExecutionDisposition.StartedOutcomeUnknown;
                failureSummary = OperationAuditSummary.WithoutExecution(disposition);
                throw GrpcOperationOutcomeMapper.CreateInternalFailure(
                    operation.OperationId,
                    operation.ReplaySafety,
                    generation: 0,
                    disposition,
                    "request-violation-after-mapping");
            }
            catch (ArgumentException exception)
            {
                // Only the request mapping's own violation names its field, and only when
                // the path resolves to a field of this request. Any other argument failure,
                // and every exception message, stays out of the error.
                throw GrpcOperationOutcomeMapper.CreateValidationFailure(
                    operation.OperationId,
                    "request-validation-failed",
                    replaySafety: operation.ReplaySafety,
                    requestValidation: mapping.Details(exception));
            }

            var completed = GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                operation.OperationId,
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
                operation.OperationId,
                outcome.Generation,
                RequestDurationMilliseconds(startedAt),
                OperationAuditSummary.Create(outcome, operation.OperationId));
            rpcStatus = StatusCode.OK;
            return result;
        }
        catch (RpcException exception)
        {
            rpcStatus = exception.StatusCode;
            _auditLogger.OperationFailed(
                EffectiveCorrelationId(outcome, effectiveCorrelationId),
                operation.OperationId,
                outcome?.Generation ?? 0,
                RequestDurationMilliseconds(startedAt),
                failureSummary ?? OperationAuditSummary.Create(outcome, operation.OperationId),
                exception.StatusCode,
                GrpcOperationOutcomeMapper.GetDiagnosticCode(exception));
            throw;
        }
        finally
        {
            activity?.SetTag("rpc.status", rpcStatus.ToString());
            telemetry?.RpcCompleted(operation.OperationId, rpcStatus.ToString(),
                _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);
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
