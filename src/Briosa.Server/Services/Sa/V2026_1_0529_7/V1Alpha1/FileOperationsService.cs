using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using TargetProtocol = global::Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Services.Sa.V2026_1_0529_7.V1Alpha1;

internal sealed partial class FileOperationsService(
    IWorkerCommandExecutor executor,
    ILogger<FileOperationsService> logger,
    TimeProvider timeProvider) : TargetProtocol.FileOperations.FileOperationsBase
{
    private readonly IWorkerCommandExecutor _executor =
        executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<FileOperationsService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
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
            context.Deadline).ConfigureAwait(false);
    }

    internal async Task<TargetProtocol.GetWorkingDirectoryResult> ExecuteGetWorkingDirectory(
        TargetProtocol.GetWorkingDirectoryRequest request,
        CancellationToken cancellationToken,
        DateTime? deadline = null)
    {
        var command = FileOperationsGetWorkingDirectoryBinding.CreateCommand(request);
        WorkerExecutionOutcome? outcome = null;
        try
        {
            outcome = await _executor.ExecuteAsync(command, cancellationToken)
                .ConfigureAwait(false);
            var completed = GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                command.OperationId,
                FileOperationsGetWorkingDirectoryBinding.OutputContracts,
                deadline is not null &&
                deadline.Value != DateTime.MaxValue &&
                deadline.Value <= _timeProvider.GetUtcNow().UtcDateTime);

            LogOperationCompleted(
                command.OperationId,
                outcome.Generation,
                completed.Execution.DurationMilliseconds,
                completed.Execution.MpResultCode);
            return FileOperationsGetWorkingDirectoryBinding.CreateResult(completed);
        }
        catch (RpcException exception)
        {
            var diagnosticCode = exception.Trailers
                .Single(entry => entry.Key == GrpcOperationOutcomeMapper.DiagnosticTrailerName)
                .Value;
            LogOperationFailed(
                command.OperationId,
                outcome?.Generation ?? 0,
                exception.StatusCode,
                diagnosticCode);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Operation {OperationId} completed on worker generation {Generation} in {DurationMilliseconds} ms with MP result {MpResultCode}.")]
    private partial void LogOperationCompleted(
        string operationId,
        int generation,
        long durationMilliseconds,
        int mpResultCode);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Warning,
        Message = "Operation {OperationId} failed on worker generation {Generation} with gRPC status {GrpcStatus} and diagnostic {DiagnosticCode}.")]
    private partial void LogOperationFailed(
        string operationId,
        int generation,
        StatusCode grpcStatus,
        string diagnosticCode);
}
