using Briosa.Core.V1Alpha1;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;

namespace Briosa.Server.Tests;

public sealed class GrpcOperationOutcomeMapperTests
{
    private const string OperationId = "file_operations.get_working_directory";
    private static readonly OperationOutputContract[] Outputs =
    [
        new("directory", "Directory", WorkerMpValueKind.Text)
    ];

    public static TheoryData<
        int,
        bool,
        bool,
        StatusCode,
        OperationFailureKind,
        RetryGuidance> TransportFailures =>
        new()
        {
            {
                (int)WorkerExecutionStatus.Unavailable,
                false,
                true,
                StatusCode.Unavailable,
                OperationFailureKind.SpatialAnalyzerUnavailable,
                RetryGuidance.RetryAfterReadiness
            },
            {
                (int)WorkerExecutionStatus.Unavailable,
                false,
                false,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerUnavailable,
                RetryGuidance.RetryAfterReadiness
            },
            {
                (int)WorkerExecutionStatus.ClientCancelled,
                false,
                false,
                StatusCode.Cancelled,
                OperationFailureKind.CallerCancelled,
                RetryGuidance.CallerControlled
            },
            {
                (int)WorkerExecutionStatus.ClientCancelled,
                true,
                false,
                StatusCode.DeadlineExceeded,
                OperationFailureKind.CallerDeadlineExceeded,
                RetryGuidance.CallerControlled
            },
            {
                (int)WorkerExecutionStatus.WatchdogTimeout,
                false,
                false,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerWatchdogTimeout,
                RetryGuidance.RetryAfterWorkerReplacement
            },
            {
                (int)WorkerExecutionStatus.WorkerFailure,
                false,
                false,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerFailure,
                RetryGuidance.RetryAfterWorkerReplacement
            },
            {
                (int)WorkerExecutionStatus.PolicyDenied,
                false,
                false,
                StatusCode.PermissionDenied,
                OperationFailureKind.PolicyDenied,
                RetryGuidance.DoNotRetry
            },
            {
                (int)WorkerExecutionStatus.Unsupported,
                false,
                false,
                StatusCode.Unimplemented,
                OperationFailureKind.Unsupported,
                RetryGuidance.DoNotRetry
            }
        };

    [Theory]
    [MemberData(nameof(TransportFailures))]
    public void TransportFailuresHaveStableStatusKindAndRetryGuidance(
        int executionStatus,
        bool deadlineExceeded,
        bool spatialAnalyzerUnavailable,
        StatusCode expectedStatus,
        OperationFailureKind expectedKind,
        RetryGuidance expectedRetryGuidance)
    {
        var workerExecutionStatus = (WorkerExecutionStatus)executionStatus;
        var connection = spatialAnalyzerUnavailable
            ? Connection(WorkerConnectionState.Faulted)
            : null;
        var outcome = new WorkerExecutionOutcome(
            workerExecutionStatus,
            Execution: null,
            connection,
            Diagnostic(workerExecutionStatus, spatialAnalyzerUnavailable),
            Generation: 7);

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                Outputs,
                deadlineExceeded));
        var error = Error(exception);

        Assert.Equal(expectedStatus, exception.StatusCode);
        Assert.Equal(expectedKind, error.Kind);
        Assert.Equal(expectedRetryGuidance, error.RetryGuidance);
        Assert.Equal(OperationId, error.OperationId);
        Assert.True(error.HasWorkerGeneration);
        Assert.Equal(7, error.WorkerGeneration);
        Assert.Null(error.MpExecution);
    }

    [Fact]
    public void ValidationAndUnsupportedFailuresUseCanonicalStatuses()
    {
        var invalid = GrpcOperationOutcomeMapper.CreateValidationFailure(
            OperationId,
            "request-validation-failed");
        var unsupported = GrpcOperationOutcomeMapper.CreateUnsupportedFailure(
            OperationId,
            "operation-unsupported");

        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(OperationFailureKind.Validation, Error(invalid).Kind);
        Assert.Equal(RetryGuidance.DoNotRetry, Error(invalid).RetryGuidance);
        Assert.Equal(StatusCode.Unimplemented, unsupported.StatusCode);
        Assert.Equal(OperationFailureKind.Unsupported, Error(unsupported).Kind);
        Assert.Equal(RetryGuidance.DoNotRetry, Error(unsupported).RetryGuidance);
    }

    [Theory]
    [InlineData(false, true, OperationFailureKind.ExecuteStepRejected, MpExecutionState.ExecuteStepRejected)]
    [InlineData(true, false, OperationFailureKind.MpFailure, MpExecutionState.Failed)]
    public void MpFailuresPreserveResultAndMarkOutputsNotAttempted(
        bool executeStepReturned,
        bool mpSucceeded,
        OperationFailureKind expectedKind,
        MpExecutionState expectedState)
    {
        var outcome = Completed(
            executeStepReturned,
            mpSucceeded,
            outputs: [],
            diagnosticCode: mpSucceeded ? "execute-step-rejected" : "mp-command-failed");

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                Outputs,
                callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
        Assert.Equal(expectedKind, error.Kind);
        Assert.NotNull(error.MpExecution);
        Assert.Equal(expectedState, error.MpExecution.State);
        Assert.True(error.MpExecution.HasMpResultCode);
        Assert.Equal(42, error.MpExecution.MpResultCode);
        var retrieval = Assert.Single(error.MpExecution.OutputRetrievals);
        Assert.Equal("directory", retrieval.FieldName);
        Assert.Equal(OutputRetrievalState.NotAttempted, retrieval.State);
    }

    [Fact]
    public void GetterFailureIsDataLossWithoutLeakingTheReturnedValue()
    {
        const string sensitiveValue = @"C:\Sensitive\Customer";
        var outcome = Completed(
            executeStepReturned: true,
            mpSucceeded: true,
            [
                new WorkerMpOutputValue(
                    "Directory",
                    WorkerMpValueKind.Text,
                    Retrieved: false,
                    StringValue: sensitiveValue)
            ],
            "sdk-output-retrieval-failed");

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                Outputs,
                callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal(OperationFailureKind.OutputRetrievalFailure, error.Kind);
        Assert.Equal(RetryGuidance.DoNotRetry, error.RetryGuidance);
        var retrieval = Assert.Single(error.MpExecution.OutputRetrievals);
        Assert.Equal(OutputRetrievalState.Failed, retrieval.State);
        Assert.Equal("sdk-output-retrieval-failed", retrieval.DiagnosticCode);
        Assert.DoesNotContain(sensitiveValue, exception.Status.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveValue, error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SuccessfulOutputHasExplicitRetrievalStateAndTypedValue()
    {
        var output = new WorkerMpOutputValue(
            "Directory",
            WorkerMpValueKind.Text,
            Retrieved: true,
            StringValue: string.Empty);
        var outcome = Completed(
            executeStepReturned: true,
            mpSucceeded: true,
            [output],
            diagnosticCode: null);

        var result = GrpcOperationOutcomeMapper.RequireSuccess(
            outcome,
            OperationId,
            Outputs,
            callerDeadlineExceeded: false);

        Assert.Same(output, Assert.Single(result.Execution.OutputValues));
        Assert.Equal(MpExecutionState.Succeeded, result.Details.State);
        Assert.True(result.Details.HasMpResultCode);
        Assert.Equal(42, result.Details.MpResultCode);
        Assert.Equal(
            OutputRetrievalState.Retrieved,
            Assert.Single(result.Details.OutputRetrievals).State);
    }

    [Fact]
    public void MissingOutputIsAnInternalShapeFailure()
    {
        var outcome = Completed(
            executeStepReturned: true,
            mpSucceeded: true,
            outputs: [],
            diagnosticCode: null);

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                Outputs,
                callerDeadlineExceeded: false));

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal(OperationFailureKind.Internal, Error(exception).Kind);
    }

    private static WorkerExecutionOutcome Completed(
        bool executeStepReturned,
        bool mpSucceeded,
        IReadOnlyList<WorkerMpOutputValue> outputs,
        string? diagnosticCode) =>
        new(
            WorkerExecutionStatus.Completed,
            new WorkerMpExecutionResult(
                executeStepReturned,
                mpSucceeded,
                MpResultCode: 42,
                DurationMilliseconds: 5,
                outputs,
                diagnosticCode),
            Connection(WorkerConnectionState.Connected),
            diagnosticCode ?? "completed",
            Generation: 7);

    private static WorkerConnectionSnapshot Connection(WorkerConnectionState state) =>
        new(
            state,
            "localhost",
            StatusCode: state == WorkerConnectionState.Connected ? 0 : -1,
            Attempt: 1,
            MaximumAttempts: 3,
            DiagnosticCode: state == WorkerConnectionState.Connected
                ? "sdk-connected"
                : "sdk-connection-not-ready",
            DateTimeOffset.UnixEpoch);

    private static string Diagnostic(
        WorkerExecutionStatus status,
        bool spatialAnalyzerUnavailable) =>
        spatialAnalyzerUnavailable
            ? "sdk-connection-not-ready"
            : status switch
            {
                WorkerExecutionStatus.ClientCancelled => "client-wait-cancelled",
                WorkerExecutionStatus.WatchdogTimeout => "worker-execution-watchdog-timeout",
                WorkerExecutionStatus.WorkerFailure => "worker-execution-control-failed",
                WorkerExecutionStatus.PolicyDenied => "operation-policy-denied",
                WorkerExecutionStatus.Unsupported => "operation-unsupported",
                _ => "worker-not-ready"
            };

    private static OperationError Error(RpcException exception)
    {
        var entry = Assert.Single(
            exception.Trailers,
            item => item.Key == GrpcOperationOutcomeMapper.ErrorTrailerName);
        return OperationError.Parser.ParseFrom(entry.ValueBytes);
    }
}
