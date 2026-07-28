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
        int,
        bool,
        bool,
        ReplaySafety,
        StatusCode,
        OperationFailureKind,
        ExecutionDisposition,
        RecoveryGuidance,
        ReplayGuidance> TransportFailures =>
        new()
        {
            {
                (int)WorkerExecutionStatus.Unavailable,
                (int)WorkerExecutionDisposition.NotStarted,
                false,
                true,
                ReplaySafety.Safe,
                StatusCode.Unavailable,
                OperationFailureKind.SpatialAnalyzerUnavailable,
                ExecutionDisposition.NotStarted,
                RecoveryGuidance.WaitForReadiness,
                ReplayGuidance.MayReplay
            },
            {
                (int)WorkerExecutionStatus.Unavailable,
                (int)WorkerExecutionDisposition.NotStarted,
                false,
                false,
                ReplaySafety.Safe,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerUnavailable,
                ExecutionDisposition.NotStarted,
                RecoveryGuidance.WaitForReadiness,
                ReplayGuidance.MayReplay
            },
            {
                (int)WorkerExecutionStatus.ClientCancelled,
                (int)WorkerExecutionDisposition.NotStarted,
                false,
                false,
                ReplaySafety.Unknown,
                StatusCode.Cancelled,
                OperationFailureKind.CallerCancelled,
                ExecutionDisposition.NotStarted,
                RecoveryGuidance.None,
                ReplayGuidance.MayReplay
            },
            {
                (int)WorkerExecutionStatus.ClientCancelled,
                (int)WorkerExecutionDisposition.StartedOutcomeUnknown,
                true,
                false,
                ReplaySafety.Unsafe,
                StatusCode.DeadlineExceeded,
                OperationFailureKind.CallerDeadlineExceeded,
                ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.None,
                ReplayGuidance.ReconcileBeforeReplay
            },
            {
                (int)WorkerExecutionStatus.WatchdogTimeout,
                (int)WorkerExecutionDisposition.StartedOutcomeUnknown,
                false,
                false,
                ReplaySafety.Unsafe,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerWatchdogTimeout,
                ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.WorkerReplacement,
                ReplayGuidance.ReconcileBeforeReplay
            },
            {
                (int)WorkerExecutionStatus.WatchdogTimeout,
                (int)WorkerExecutionDisposition.StartedOutcomeUnknown,
                false,
                false,
                ReplaySafety.Safe,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerWatchdogTimeout,
                ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.WorkerReplacement,
                ReplayGuidance.MayReplay
            },
            {
                (int)WorkerExecutionStatus.WorkerFailure,
                (int)WorkerExecutionDisposition.StartedOutcomeUnknown,
                false,
                false,
                ReplaySafety.Unknown,
                StatusCode.Unavailable,
                OperationFailureKind.WorkerFailure,
                ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.WorkerReplacement,
                ReplayGuidance.ReconcileBeforeReplay
            },
            {
                (int)WorkerExecutionStatus.PolicyDenied,
                (int)WorkerExecutionDisposition.NotStarted,
                false,
                false,
                ReplaySafety.Safe,
                StatusCode.PermissionDenied,
                OperationFailureKind.PolicyDenied,
                ExecutionDisposition.NotStarted,
                RecoveryGuidance.None,
                ReplayGuidance.DoNotReplay
            }
        };

    [Theory]
    [MemberData(nameof(TransportFailures))]
    public void TransportFailuresSeparateExecutionRecoveryAndReplay(
        int executionStatus,
        int workerDisposition,
        bool deadlineExceeded,
        bool spatialAnalyzerUnavailable,
        ReplaySafety replaySafety,
        StatusCode expectedStatus,
        OperationFailureKind expectedKind,
        ExecutionDisposition expectedDisposition,
        RecoveryGuidance expectedRecoveryGuidance,
        ReplayGuidance expectedReplayGuidance)
    {
        var workerExecutionStatus = (WorkerExecutionStatus)executionStatus;
        var connection = spatialAnalyzerUnavailable
            ? Connection(WorkerConnectionState.Faulted)
            : null;
        var outcome = new WorkerExecutionOutcome(
            workerExecutionStatus,
            (WorkerExecutionDisposition)workerDisposition,
            Execution: null,
            connection,
            Diagnostic(workerExecutionStatus, spatialAnalyzerUnavailable),
            Generation: 7);

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                replaySafety,
                Outputs,
                deadlineExceeded));
        var error = Error(exception);

        Assert.Equal(expectedStatus, exception.StatusCode);
        Assert.Equal(expectedKind, error.Kind);
        Assert.Equal(expectedDisposition, error.ExecutionDisposition);
        Assert.Equal(expectedRecoveryGuidance, error.RecoveryGuidance);
        Assert.Equal(expectedReplayGuidance, error.ReplayGuidance);
        Assert.Equal(replaySafety, error.ReplaySafety);
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
        Assert.Equal(ExecutionDisposition.NotStarted, Error(invalid).ExecutionDisposition);
        Assert.Equal(ReplayGuidance.DoNotReplay, Error(invalid).ReplayGuidance);
        Assert.Equal(StatusCode.Unimplemented, unsupported.StatusCode);
        Assert.Equal(OperationFailureKind.Unsupported, Error(unsupported).Kind);
        Assert.Equal(ExecutionDisposition.NotStarted, Error(unsupported).ExecutionDisposition);
        Assert.Equal(ReplayGuidance.DoNotReplay, Error(unsupported).ReplayGuidance);
    }

    [Fact]
    public void QuarantinedTargetRequiresOperatorRecoveryWithoutChangingReplayPolicy()
    {
        var outcome = new WorkerExecutionOutcome(
            WorkerExecutionStatus.Unavailable,
            WorkerExecutionDisposition.NotStarted,
            Execution: null,
            new WorkerConnectionSnapshot(
                WorkerConnectionState.Connected,
                WorkerExecutionReadinessState.OperatorRecoveryRequired,
                StatusCode: 0,
                Attempt: 1,
                MaximumAttempts: 1,
                "execution-readiness-operator-recovery-required",
                DateTimeOffset.UnixEpoch),
            "execution-readiness-operator-recovery-required",
            Generation: 7);

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                ReplaySafety.Unsafe,
                Outputs,
                callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(ExecutionDisposition.NotStarted, error.ExecutionDisposition);
        Assert.Equal(
            RecoveryGuidance.OperatorInterventionRequired,
            error.RecoveryGuidance);
        Assert.Equal(ReplayGuidance.MayReplay, error.ReplayGuidance);
    }

    [Theory]
    [InlineData(false, true, OperationFailureKind.ExecuteStepRejected, MpExecutionState.ExecuteStepRejected, ExecutionDisposition.StartedOutcomeUnknown, ReplayGuidance.MayReplay)]
    [InlineData(true, false, OperationFailureKind.MpFailure, MpExecutionState.Failed, ExecutionDisposition.Completed, ReplayGuidance.DoNotReplay)]
    public void MpFailuresPreserveResultAndMarkOutputsNotAttempted(
        bool executeStepReturned,
        bool mpSucceeded,
        OperationFailureKind expectedKind,
        MpExecutionState expectedState,
        ExecutionDisposition expectedDisposition,
        ReplayGuidance expectedReplayGuidance)
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
                ReplaySafety.Safe,
                Outputs,
                callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
        Assert.Equal(expectedKind, error.Kind);
        Assert.Equal(expectedDisposition, error.ExecutionDisposition);
        Assert.Equal(expectedReplayGuidance, error.ReplayGuidance);
        Assert.NotNull(error.MpExecution);
        Assert.Equal(expectedState, error.MpExecution.State);
        Assert.Equal(executeStepReturned, error.MpExecution.HasMpResultCode);
        if (executeStepReturned)
        {
            Assert.Equal(3, error.MpExecution.MpResultCode);
        }
        var retrieval = Assert.Single(error.MpExecution.OutputRetrievals);
        Assert.Equal("directory", retrieval.FieldName);
        Assert.Equal(OutputRetrievalState.NotAttempted, retrieval.State);
    }

    [Fact]
    public void SetterRejectionProvesExecutionDidNotStart()
    {
        var outcome = Completed(
            executeStepReturned: false,
            mpSucceeded: false,
            outputs: [],
            diagnosticCode: "sdk-argument-rejected");

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                ReplaySafety.Unsafe,
                Outputs,
                callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(OperationFailureKind.SdkArgumentRejected, error.Kind);
        Assert.Equal(MpExecutionState.ArgumentRejected, error.MpExecution.State);
        Assert.Equal(ExecutionDisposition.NotStarted, error.ExecutionDisposition);
        Assert.Equal(ReplayGuidance.DoNotReplay, error.ReplayGuidance);
    }

    [Fact]
    public void MpResultRetrievalFailureIsExplicitAndHasNoResultCode()
    {
        var outcome = new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: false,
                MpSucceeded: false,
                MpResultCode: null,
                DurationMilliseconds: 5,
                OutputValues: [],
                DiagnosticCode: "sdk-mp-result-retrieval-failed"),
            Connection(WorkerConnectionState.Connected),
            "sdk-mp-result-retrieval-failed",
            Generation: 7);

        var exception = Assert.Throws<RpcException>(() =>
            GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                OperationId,
                ReplaySafety.Safe,
                Outputs,
                callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal(OperationFailureKind.MpResultRetrievalFailure, error.Kind);
        Assert.Equal(ExecutionDisposition.StartedOutcomeUnknown, error.ExecutionDisposition);
        Assert.Equal(ReplayGuidance.ReconcileBeforeReplay, error.ReplayGuidance);
        Assert.Equal(MpExecutionState.ResultUnavailable, error.MpExecution.State);
        Assert.False(error.MpExecution.HasMpResultCode);
        Assert.Equal(
            OutputRetrievalState.NotAttempted,
            Assert.Single(error.MpExecution.OutputRetrievals).State);
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
                ReplaySafety.Safe,
                Outputs,
                callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal(OperationFailureKind.OutputRetrievalFailure, error.Kind);
        Assert.Equal(ExecutionDisposition.Completed, error.ExecutionDisposition);
        Assert.Equal(ReplayGuidance.DoNotReplay, error.ReplayGuidance);
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
            ReplaySafety.Safe,
            Outputs,
            callerDeadlineExceeded: false);

        Assert.Same(output, Assert.Single(result.Execution.OutputValues));
        Assert.Equal(MpExecutionState.Succeeded, result.Details.State);
        Assert.True(result.Details.HasMpResultCode);
        Assert.Equal(2, result.Details.MpResultCode);
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
                ReplaySafety.Safe,
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
            diagnosticCode == "sdk-argument-rejected"
                ? WorkerExecutionDisposition.NotStarted
                : executeStepReturned
                    ? WorkerExecutionDisposition.Completed
                    : WorkerExecutionDisposition.StartedOutcomeUnknown,
            new WorkerMpExecutionResult(
                executeStepReturned,
                MpResultRetrieved: executeStepReturned,
                MpSucceeded: executeStepReturned && mpSucceeded,
                MpResultCode: executeStepReturned ? (mpSucceeded ? 2 : 3) : null,
                DurationMilliseconds: 5,
                outputs,
                diagnosticCode),
            Connection(WorkerConnectionState.Connected),
            diagnosticCode ?? "completed",
            Generation: 7);

    private static WorkerConnectionSnapshot Connection(WorkerConnectionState state) =>
        new(
            state,
            state == WorkerConnectionState.Connected
                ? WorkerExecutionReadinessState.ExecutionReady
                : WorkerExecutionReadinessState.Unverified,
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
