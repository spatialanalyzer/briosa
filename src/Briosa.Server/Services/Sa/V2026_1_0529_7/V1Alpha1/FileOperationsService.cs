using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using TargetProtocol = global::Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Services.Sa.V2026_1_0529_7.V1Alpha1;

internal sealed class FileOperationsService(
    IWorkerCommandExecutor executor,
    OperationAuditLogger auditLogger,
    TimeProvider timeProvider) : TargetProtocol.FileOperations.FileOperationsBase
{
    private static readonly CatalogOperationDescriptor OperationDescriptor =
        TargetCatalogMetadata.Operations.Single(operation =>
            operation.OperationId == FileOperationsGetWorkingDirectoryBinding.OperationId);

    private readonly OperationAuditLogger _auditLogger =
        auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    private readonly IWorkerCommandExecutor _executor =
        executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    [OperationImplementation(FileOperationsGetWorkingDirectoryBinding.OperationId)]
    public override async Task<TargetProtocol.GetWorkingDirectoryResult> GetWorkingDirectory(
        TargetProtocol.GetWorkingDirectoryRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return await ExecuteGetWorkingDirectory(
            request,
            context.CancellationToken,
            context.Deadline,
            Guid.NewGuid(),
            ClassifyActor(context.Peer)).ConfigureAwait(false);
    }

    internal async Task<TargetProtocol.GetWorkingDirectoryResult> ExecuteGetWorkingDirectory(
        TargetProtocol.GetWorkingDirectoryRequest request,
        CancellationToken cancellationToken,
        DateTime? deadline = null,
        Guid? correlationId = null,
        string actorCategory = "internal-unattributed")
    {
        var effectiveCorrelationId = correlationId is { } value && value != Guid.Empty
            ? value
            : Guid.NewGuid();
        _auditLogger.RequestStarted(
            effectiveCorrelationId,
            OperationDescriptor,
            actorCategory);
        var startedAt = _timeProvider.GetTimestamp();
        var command = FileOperationsGetWorkingDirectoryBinding.CreateCommand(request);
        WorkerExecutionOutcome? outcome = null;
        try
        {
            outcome = await _executor.ExecuteAsync(
                command,
                effectiveCorrelationId,
                cancellationToken).ConfigureAwait(false);
            var completed = GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                command.OperationId,
                FileOperationsGetWorkingDirectoryBinding.OutputContracts,
                deadline is not null &&
                deadline.Value != DateTime.MaxValue &&
                deadline.Value <= _timeProvider.GetUtcNow().UtcDateTime);

            _auditLogger.OperationCompleted(
                EffectiveCorrelationId(outcome, effectiveCorrelationId),
                command.OperationId,
                outcome.Generation,
                RequestDurationMilliseconds(startedAt),
                OperationAuditSummary.Create(outcome));
            return FileOperationsGetWorkingDirectoryBinding.CreateResult(completed);
        }
        catch (RpcException exception)
        {
            var diagnosticCode = exception.Trailers
                .FirstOrDefault(entry =>
                    entry.Key == GrpcOperationOutcomeMapper.DiagnosticTrailerName)
                ?.Value ?? "grpc-operation-failed";
            _auditLogger.OperationFailed(
                EffectiveCorrelationId(outcome, effectiveCorrelationId),
                command.OperationId,
                outcome?.Generation ?? 0,
                RequestDurationMilliseconds(startedAt),
                OperationAuditSummary.Create(outcome),
                exception.StatusCode,
                diagnosticCode);
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
