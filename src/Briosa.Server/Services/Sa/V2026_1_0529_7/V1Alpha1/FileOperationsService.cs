using Briosa.Server.Generated.Sa.V2026_1_0529_7.V1Alpha1;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using TargetProtocol = global::Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Services.Sa.V2026_1_0529_7.V1Alpha1;

internal sealed partial class FileOperationsService(
    IWorkerCommandExecutor executor,
    ILogger<FileOperationsService> logger) : TargetProtocol.FileOperations.FileOperationsBase
{
    private const string DiagnosticTrailerName = "briosa-diagnostic-code";
    private const string MpResultTrailerName = "briosa-mp-result-code";
    private readonly IWorkerCommandExecutor _executor =
        executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<FileOperationsService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public override async Task<TargetProtocol.GetWorkingDirectoryResult> GetWorkingDirectory(
        TargetProtocol.GetWorkingDirectoryRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return await ExecuteGetWorkingDirectory(context.CancellationToken).ConfigureAwait(false);
    }

    internal async Task<TargetProtocol.GetWorkingDirectoryResult> ExecuteGetWorkingDirectory(
        CancellationToken cancellationToken)
    {
        var outcome = await _executor.ExecuteAsync(
            FileOperationsGetWorkingDirectoryBinding.CreateCommand(),
            cancellationToken).ConfigureAwait(false);
        if (outcome.Status != WorkerExecutionStatus.Completed)
        {
            var diagnosticCode = NormalizeDiagnosticCode(
                outcome.DiagnosticCode,
                "worker-execution-failed");
            LogOperationFailed(
                FileOperationsGetWorkingDirectoryBinding.OperationId,
                outcome.Generation,
                outcome.Status,
                diagnosticCode);
            throw CreateTransportFailure(outcome.Status, diagnosticCode);
        }

        var execution = outcome.Execution ?? throw CreateFailure(
            StatusCode.Internal,
            "worker-result-missing",
            "The worker returned no execution result.");
        if (!execution.ExecuteStepReturned)
        {
            throw CreateFailure(
                StatusCode.FailedPrecondition,
                NormalizeDiagnosticCode(execution.DiagnosticCode, "execute-step-rejected"),
                "SpatialAnalyzer rejected the MP execution request.");
        }

        if (!execution.MpSucceeded)
        {
            throw CreateFailure(
                StatusCode.FailedPrecondition,
                NormalizeDiagnosticCode(execution.DiagnosticCode, "mp-command-failed"),
                "The SpatialAnalyzer MP command failed.",
                execution.MpResultCode);
        }

        var output = execution.OutputValues.SingleOrDefault(value =>
            value.Name == FileOperationsGetWorkingDirectoryBinding.DirectoryArgumentName &&
            value.Kind == WorkerMpValueKind.Text) ??
            throw CreateFailure(
                StatusCode.Internal,
                "worker-output-shape-invalid",
                "The worker returned an invalid output shape.");

        if (!output.Retrieved || output.StringValue is null)
        {
            throw CreateFailure(
                StatusCode.DataLoss,
                NormalizeDiagnosticCode(
                    execution.DiagnosticCode,
                    "sdk-output-retrieval-failed"),
                "SpatialAnalyzer did not return the requested output.");
        }

        LogOperationCompleted(
            FileOperationsGetWorkingDirectoryBinding.OperationId,
            outcome.Generation,
            execution.DurationMilliseconds,
            execution.MpResultCode);
        return new TargetProtocol.GetWorkingDirectoryResult
        {
            Directory = output.StringValue
        };
    }

    private static RpcException CreateTransportFailure(
        WorkerExecutionStatus status,
        string diagnosticCode) =>
        status switch
        {
            WorkerExecutionStatus.ClientCancelled => CreateFailure(
                StatusCode.Cancelled,
                diagnosticCode,
                "The caller stopped waiting for the operation."),
            WorkerExecutionStatus.Unavailable or
            WorkerExecutionStatus.WatchdogTimeout or
            WorkerExecutionStatus.WorkerFailure => CreateFailure(
                StatusCode.Unavailable,
                diagnosticCode,
                "The SpatialAnalyzer worker is unavailable."),
            _ => CreateFailure(
                StatusCode.Internal,
                diagnosticCode,
                "The operation failed before completion.")
        };

    private static RpcException CreateFailure(
        StatusCode statusCode,
        string diagnosticCode,
        string detail,
        int? mpResultCode = null)
    {
        var trailers = new Metadata
        {
            { DiagnosticTrailerName, diagnosticCode }
        };
        if (mpResultCode.HasValue)
        {
            trailers.Add(
                MpResultTrailerName,
                mpResultCode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return new RpcException(new Status(statusCode, detail), trailers);
    }

    private static string NormalizeDiagnosticCode(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) && value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
                ? value
                : fallback;

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
        Message = "Operation {OperationId} failed on worker generation {Generation} with status {ExecutionStatus} and diagnostic {DiagnosticCode}.")]
    private partial void LogOperationFailed(
        string operationId,
        int generation,
        WorkerExecutionStatus executionStatus,
        string diagnosticCode);
}
