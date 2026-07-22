using Briosa.Core.V1Alpha1;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Services;

internal sealed record OperationOutputContract(
    string FieldName,
    string ArgumentName,
    WorkerMpValueKind Kind);

internal sealed record SuccessfulOperationExecution(
    WorkerMpExecutionResult Execution,
    MpExecutionDetails Details);

internal static class GrpcOperationOutcomeMapper
{
    public const string DiagnosticTrailerName = "briosa-diagnostic-code";
    public const string ErrorTrailerName = "briosa-operation-error-bin";
    public const string MpResultTrailerName = "briosa-mp-result-code";

    public static SuccessfulOperationExecution RequireSuccess(
        WorkerExecutionOutcome outcome,
        string operationId,
        IReadOnlyList<OperationOutputContract> outputs,
        bool callerDeadlineExceeded)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(outputs);

        if (outcome.Status != WorkerExecutionStatus.Completed)
        {
            throw CreateTransportFailure(
                outcome,
                operationId,
                callerDeadlineExceeded);
        }

        var execution = outcome.Execution ??
            throw CreateInternalFailure(
                operationId,
                outcome.Generation,
                "worker-result-missing");

        if (!execution.ExecuteStepReturned)
        {
            var details = CreateMpDetails(
                execution,
                outputs,
                MpExecutionState.ExecuteStepRejected,
                OutputRetrievalState.NotAttempted);
            throw CreateFailure(
                StatusCode.FailedPrecondition,
                operationId,
                OperationFailureKind.ExecuteStepRejected,
                NormalizeDiagnosticCode(execution.DiagnosticCode, "execute-step-rejected"),
                RetryGuidance.DoNotRetry,
                outcome.Generation,
                details,
                "SpatialAnalyzer rejected the MP execution request.");
        }

        if (!execution.MpSucceeded)
        {
            var details = CreateMpDetails(
                execution,
                outputs,
                MpExecutionState.Failed,
                OutputRetrievalState.NotAttempted);
            throw CreateFailure(
                StatusCode.FailedPrecondition,
                operationId,
                OperationFailureKind.MpFailure,
                NormalizeDiagnosticCode(execution.DiagnosticCode, "mp-command-failed"),
                RetryGuidance.DoNotRetry,
                outcome.Generation,
                details,
                "The SpatialAnalyzer MP command failed.");
        }

        if (!OutputsMatch(outputs, execution.OutputValues))
        {
            throw CreateInternalFailure(
                operationId,
                outcome.Generation,
                "worker-output-shape-invalid");
        }

        var successfulDetails = CreateSuccessfulMpDetails(execution, outputs);
        if (successfulDetails.OutputRetrievals.Any(detail =>
            detail.State == OutputRetrievalState.Failed))
        {
            throw CreateFailure(
                StatusCode.DataLoss,
                operationId,
                OperationFailureKind.OutputRetrievalFailure,
                NormalizeDiagnosticCode(
                    execution.DiagnosticCode,
                    "sdk-output-retrieval-failed"),
                RetryGuidance.DoNotRetry,
                outcome.Generation,
                successfulDetails,
                "SpatialAnalyzer did not return every requested output.");
        }

        return new SuccessfulOperationExecution(execution, successfulDetails);
    }

    public static RpcException CreateValidationFailure(
        string operationId,
        string diagnosticCode,
        int generation = 0) =>
        CreateFailure(
            StatusCode.InvalidArgument,
            operationId,
            OperationFailureKind.Validation,
            NormalizeDiagnosticCode(diagnosticCode, "request-validation-failed"),
            RetryGuidance.DoNotRetry,
            generation,
            mpExecution: null,
            "The request is invalid.");

    public static RpcException CreateUnsupportedFailure(
        string operationId,
        string diagnosticCode,
        int generation = 0) =>
        CreateFailure(
            StatusCode.Unimplemented,
            operationId,
            OperationFailureKind.Unsupported,
            NormalizeDiagnosticCode(diagnosticCode, "operation-unsupported"),
            RetryGuidance.DoNotRetry,
            generation,
            mpExecution: null,
            "The operation is not supported by this Briosa target.");

    private static RpcException CreateTransportFailure(
        WorkerExecutionOutcome outcome,
        string operationId,
        bool callerDeadlineExceeded)
    {
        var diagnosticCode = NormalizeDiagnosticCode(
            outcome.DiagnosticCode,
            "worker-execution-failed");
        return outcome.Status switch
        {
            WorkerExecutionStatus.ClientCancelled when callerDeadlineExceeded => CreateFailure(
                StatusCode.DeadlineExceeded,
                operationId,
                OperationFailureKind.CallerDeadlineExceeded,
                diagnosticCode,
                RetryGuidance.CallerControlled,
                outcome.Generation,
                mpExecution: null,
                "The caller's deadline elapsed while waiting for the operation."),
            WorkerExecutionStatus.ClientCancelled => CreateFailure(
                StatusCode.Cancelled,
                operationId,
                OperationFailureKind.CallerCancelled,
                diagnosticCode,
                RetryGuidance.CallerControlled,
                outcome.Generation,
                mpExecution: null,
                "The caller stopped waiting for the operation."),
            WorkerExecutionStatus.WatchdogTimeout => CreateFailure(
                StatusCode.Unavailable,
                operationId,
                OperationFailureKind.WorkerWatchdogTimeout,
                diagnosticCode,
                RetryGuidance.RetryAfterWorkerReplacement,
                outcome.Generation,
                mpExecution: null,
                "The SpatialAnalyzer worker is being replaced after a watchdog timeout."),
            WorkerExecutionStatus.WorkerFailure => CreateFailure(
                StatusCode.Unavailable,
                operationId,
                OperationFailureKind.WorkerFailure,
                diagnosticCode,
                RetryGuidance.RetryAfterWorkerReplacement,
                outcome.Generation,
                mpExecution: null,
                "The SpatialAnalyzer worker failed and is being replaced."),
            WorkerExecutionStatus.Unavailable when IsSpatialAnalyzerUnavailable(outcome) =>
                CreateFailure(
                    StatusCode.Unavailable,
                    operationId,
                    OperationFailureKind.SpatialAnalyzerUnavailable,
                    diagnosticCode,
                    RetryGuidance.RetryAfterReadiness,
                    outcome.Generation,
                    mpExecution: null,
                    "SpatialAnalyzer is not ready for MP execution."),
            WorkerExecutionStatus.Unavailable => CreateFailure(
                StatusCode.Unavailable,
                operationId,
                OperationFailureKind.WorkerUnavailable,
                diagnosticCode,
                RetryGuidance.RetryAfterReadiness,
                outcome.Generation,
                mpExecution: null,
                "The SpatialAnalyzer worker is not ready."),
            _ => CreateInternalFailure(operationId, outcome.Generation, diagnosticCode)
        };
    }

    private static RpcException CreateInternalFailure(
        string operationId,
        int generation,
        string diagnosticCode) =>
        CreateFailure(
            StatusCode.Internal,
            operationId,
            OperationFailureKind.Internal,
            NormalizeDiagnosticCode(diagnosticCode, "internal-operation-failure"),
            RetryGuidance.DoNotRetry,
            generation,
            mpExecution: null,
            "The operation returned an invalid internal result.");

    private static RpcException CreateFailure(
        StatusCode statusCode,
        string operationId,
        OperationFailureKind kind,
        string diagnosticCode,
        RetryGuidance retryGuidance,
        int generation,
        MpExecutionDetails? mpExecution,
        string detail)
    {
        var error = new OperationError
        {
            OperationId = operationId,
            Kind = kind,
            DiagnosticCode = diagnosticCode,
            RetryGuidance = retryGuidance,
            WorkerGeneration = generation,
            MpExecution = mpExecution
        };
        var trailers = new Metadata
        {
            { DiagnosticTrailerName, diagnosticCode },
            { ErrorTrailerName, error.ToByteArray() }
        };
        if (mpExecution is not null && mpExecution.HasMpResultCode)
        {
            trailers.Add(
                MpResultTrailerName,
                mpExecution.MpResultCode.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
        }

        return new RpcException(new Status(statusCode, detail), trailers);
    }

    private static MpExecutionDetails CreateMpDetails(
        WorkerMpExecutionResult execution,
        IReadOnlyList<OperationOutputContract> outputs,
        MpExecutionState state,
        OutputRetrievalState retrievalState)
    {
        var details = new MpExecutionDetails
        {
            State = state,
            MpResultCode = execution.MpResultCode
        };
        details.OutputRetrievals.AddRange(outputs.Select(output =>
            new OutputRetrievalDetails
            {
                FieldName = output.FieldName,
                State = retrievalState
            }));
        return details;
    }

    private static MpExecutionDetails CreateSuccessfulMpDetails(
        WorkerMpExecutionResult execution,
        IReadOnlyList<OperationOutputContract> outputs)
    {
        var details = new MpExecutionDetails
        {
            State = MpExecutionState.Succeeded,
            MpResultCode = execution.MpResultCode
        };
        foreach (var output in outputs)
        {
            var value = execution.OutputValues.Single(candidate =>
                candidate.Name == output.ArgumentName &&
                candidate.Kind == output.Kind);
            var retrieved = value.Retrieved && HasTypedValue(value);
            var retrieval = new OutputRetrievalDetails
            {
                FieldName = output.FieldName,
                State = retrieved
                    ? OutputRetrievalState.Retrieved
                    : OutputRetrievalState.Failed
            };
            if (!retrieved)
            {
                retrieval.DiagnosticCode = NormalizeDiagnosticCode(
                    execution.DiagnosticCode,
                    "sdk-output-retrieval-failed");
            }

            details.OutputRetrievals.Add(retrieval);
        }

        return details;
    }

    private static bool OutputsMatch(
        IReadOnlyList<OperationOutputContract> requested,
        IReadOnlyList<WorkerMpOutputValue> returned) =>
        requested.Count == returned.Count &&
        requested.All(output => returned.Count(value =>
            value.Name == output.ArgumentName &&
            value.Kind == output.Kind) == 1);

    private static bool HasTypedValue(WorkerMpOutputValue value) =>
        value.Kind switch
        {
            WorkerMpValueKind.Logical => value.BooleanValue.HasValue,
            WorkerMpValueKind.WholeNumber => value.IntegerValue.HasValue,
            WorkerMpValueKind.FloatingPoint => value.DoubleValue.HasValue,
            WorkerMpValueKind.Text => value.StringValue is not null,
            WorkerMpValueKind.PointName => value.PointNameValue is not null,
            WorkerMpValueKind.Vector => value.VectorValue is not null,
            WorkerMpValueKind.ToleranceVectorOptions =>
                value.ToleranceVectorOptionsValue is not null,
            _ => false
        };

    private static bool IsSpatialAnalyzerUnavailable(WorkerExecutionOutcome outcome) =>
        outcome.Connection is { State: not WorkerConnectionState.Connected } ||
        outcome.DiagnosticCode.StartsWith("sdk-connection-", StringComparison.Ordinal);

    private static string NormalizeDiagnosticCode(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) && value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
                ? value
                : fallback;
}
