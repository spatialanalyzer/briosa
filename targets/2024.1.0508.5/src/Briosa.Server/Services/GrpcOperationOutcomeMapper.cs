using global::Briosa;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Services;

internal static class GrpcOperationOutcomeMapper
{
    public const string ErrorTrailerName = "briosa-operation-error-bin";

    public static string GetDiagnosticCode(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var trailer = exception.Trailers.FirstOrDefault(entry => entry.Key == ErrorTrailerName);
        if (trailer is null)
        {
            return "grpc-operation-failed";
        }

        try
        {
            return NormalizeDiagnosticCode(
                OperationError.Parser.ParseFrom(trailer.ValueBytes).DiagnosticCode,
                "grpc-operation-failed");
        }
        catch (InvalidProtocolBufferException)
        {
            return "grpc-operation-failed";
        }
    }

    public static SuccessfulOperationExecution RequireSuccess(
        WorkerExecutionOutcome outcome,
        string operationId,
        ReplaySafety replaySafety,
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
                replaySafety,
                callerDeadlineExceeded);
        }

        var execution = outcome.Execution ??
            throw CreateInternalFailure(
                operationId,
                replaySafety,
                outcome.Generation,
                outcome.ExecutionDisposition,
                "worker-result-missing");

        if (!execution.ExecuteStepReturned)
        {
            var argumentRejected = execution is WorkerArgumentsRejected;
            var details = CreateMpDetails(
                execution,
                outputs,
                argumentRejected
                    ? MpExecutionState.ArgumentRejected
                    : MpExecutionState.ExecuteStepRejected,
                OutputRetrievalState.NotAttempted);
            throw CreateFailure(
                StatusCode.FailedPrecondition,
                operationId,
                argumentRejected
                    ? OperationFailureKind.SdkArgumentRejected
                    : OperationFailureKind.ExecuteStepRejected,
                NormalizeDiagnosticCode(
                    execution.DiagnosticCode,
                    argumentRejected ? "sdk-argument-rejected" : "execute-step-rejected"),
                argumentRejected
                    ? ExecutionDisposition.NotStarted
                    : ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.None,
                argumentRejected
                    ? ReplayGuidance.DoNotReplay
                    : ReplayAfterAmbiguousCompletion(replaySafety),
                replaySafety,
                outcome.Generation,
                details,
                argumentRejected
                    ? "SpatialAnalyzer rejected an MP input argument before execution."
                    : "SpatialAnalyzer rejected the MP execution request.");
        }

        if (!execution.MpResultRetrieved)
        {
            var details = CreateMpDetails(
                execution,
                outputs,
                MpExecutionState.ResultUnavailable,
                OutputRetrievalState.NotAttempted);
            throw CreateFailure(
                StatusCode.Internal,
                operationId,
                OperationFailureKind.MpResultRetrievalFailure,
                NormalizeDiagnosticCode(
                    execution.DiagnosticCode,
                    "sdk-mp-result-retrieval-failed"),
                ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.None,
                ReplayGuidance.ReconcileBeforeReplay,
                replaySafety,
                outcome.Generation,
                details,
                "SpatialAnalyzer did not return the MP execution result.");
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
                ExecutionDisposition.Completed,
                RecoveryGuidance.None,
                ReplayGuidance.DoNotReplay,
                replaySafety,
                outcome.Generation,
                details,
                "The SpatialAnalyzer MP command failed.");
        }

        if (execution is WorkerMpOutputsUnavailable)
        {
            throw CreateFailure(StatusCode.DataLoss, operationId,
                OperationFailureKind.OutputRetrievalFailure,
                NormalizeDiagnosticCode(execution.DiagnosticCode, "worker-output-encoding-rejected"),
                ExecutionDisposition.Completed, RecoveryGuidance.None, ReplayGuidance.DoNotReplay,
                replaySafety, outcome.Generation,
                CreateMpDetails(execution, outputs, MpExecutionState.Succeeded, OutputRetrievalState.Failed),
                "The MP command succeeded, but its output values could not be delivered.");
        }

        if (!OutputsMatch(outputs, execution.OutputValues))
        {
            throw CreateInternalFailure(
                operationId,
                replaySafety,
                outcome.Generation,
                WorkerExecutionDisposition.Completed,
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
                ExecutionDisposition.Completed,
                RecoveryGuidance.None,
                ReplayGuidance.DoNotReplay,
                replaySafety,
                outcome.Generation,
                successfulDetails,
                "SpatialAnalyzer did not return every requested output.");
        }

        return new SuccessfulOperationExecution(execution, successfulDetails);
    }

    public static RpcException CreateValidationFailure(
        string operationId,
        string diagnosticCode,
        int generation = 0,
        ReplaySafety replaySafety = ReplaySafety.Unknown) =>
        CreateFailure(
            StatusCode.InvalidArgument,
            operationId,
            OperationFailureKind.Validation,
            NormalizeDiagnosticCode(diagnosticCode, "request-validation-failed"),
            ExecutionDisposition.NotStarted,
            RecoveryGuidance.None,
            ReplayGuidance.DoNotReplay,
            replaySafety,
            generation,
            mpExecution: null,
            "The request is invalid.");

    public static RpcException CreateUnsupportedFailure(
        string operationId,
        string diagnosticCode,
        int generation = 0,
        ReplaySafety replaySafety = ReplaySafety.Unknown) =>
        CreateFailure(
            StatusCode.Unimplemented,
            operationId,
            OperationFailureKind.Unsupported,
            NormalizeDiagnosticCode(diagnosticCode, "operation-unsupported"),
            ExecutionDisposition.NotStarted,
            RecoveryGuidance.None,
            ReplayGuidance.DoNotReplay,
            replaySafety,
            generation,
            mpExecution: null,
            "The operation is not supported by this Briosa target.");

    public static RpcException CreateResultMappingFailure(
        string operationId,
        ReplaySafety replaySafety,
        int generation,
        MpExecutionDetails details) =>
        CreateFailure(
            StatusCode.DataLoss,
            operationId,
            OperationFailureKind.Internal,
            "result-mapping-failed",
            ExecutionDisposition.Completed,
            RecoveryGuidance.None,
            ReplayGuidance.DoNotReplay,
            replaySafety,
            generation,
            details ?? throw new ArgumentNullException(nameof(details)),
            "Briosa could not map the returned value to the exact-target result contract.");

    private static RpcException CreateTransportFailure(
        WorkerExecutionOutcome outcome,
        string operationId,
        ReplaySafety replaySafety,
        bool callerDeadlineExceeded)
    {
        var diagnosticCode = NormalizeDiagnosticCode(
            outcome.DiagnosticCode,
            "worker-execution-failed");
        return outcome.Status switch
        {
            WorkerExecutionStatus.Overloaded => CreateFailure(
                StatusCode.ResourceExhausted,
                operationId,
                OperationFailureKind.Overloaded,
                diagnosticCode,
                ExecutionDisposition.NotStarted,
                RecoveryGuidance.None,
                ReplayGuidance.MayReplay,
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The Briosa execution queue is full. The operation did not start."),
            WorkerExecutionStatus.RequestRejected => CreateValidationFailure(
                operationId, diagnosticCode, outcome.Generation, replaySafety),
            WorkerExecutionStatus.PolicyDenied => CreateFailure(
                StatusCode.PermissionDenied,
                operationId,
                OperationFailureKind.PolicyDenied,
                diagnosticCode,
                ExecutionDisposition.NotStarted,
                RecoveryGuidance.None,
                ReplayGuidance.DoNotReplay,
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The operation is denied by the Briosa operation policy."),
            WorkerExecutionStatus.Unsupported => CreateFailure(
                StatusCode.Unimplemented,
                operationId,
                OperationFailureKind.Unsupported,
                diagnosticCode,
                ExecutionDisposition.NotStarted,
                RecoveryGuidance.None,
                ReplayGuidance.DoNotReplay,
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The operation is not supported by this Briosa target."),
            WorkerExecutionStatus.ClientCancelled when callerDeadlineExceeded => CreateFailure(
                StatusCode.DeadlineExceeded,
                operationId,
                OperationFailureKind.CallerDeadlineExceeded,
                diagnosticCode,
                ToProtocolDisposition(outcome.ExecutionDisposition),
                RecoveryGuidance.None,
                ReplayAfterInterruption(outcome.ExecutionDisposition, replaySafety),
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The caller's deadline elapsed while waiting for the operation."),
            WorkerExecutionStatus.ClientCancelled => CreateFailure(
                StatusCode.Cancelled,
                operationId,
                OperationFailureKind.CallerCancelled,
                diagnosticCode,
                ToProtocolDisposition(outcome.ExecutionDisposition),
                RecoveryGuidance.None,
                ReplayAfterInterruption(outcome.ExecutionDisposition, replaySafety),
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The caller stopped waiting for the operation."),
            WorkerExecutionStatus.WatchdogTimeout => CreateFailure(
                StatusCode.Unavailable,
                operationId,
                OperationFailureKind.WorkerWatchdogTimeout,
                diagnosticCode,
                ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.WorkerReplacement,
                ReplayAfterAmbiguousCompletion(replaySafety),
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The SpatialAnalyzer worker requires explicit recovery after a watchdog timeout."),
            WorkerExecutionStatus.WorkerFailure => CreateFailure(
                StatusCode.Unavailable,
                operationId,
                OperationFailureKind.WorkerFailure,
                diagnosticCode,
                ExecutionDisposition.StartedOutcomeUnknown,
                RecoveryGuidance.WorkerReplacement,
                ReplayAfterAmbiguousCompletion(replaySafety),
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The SpatialAnalyzer worker failed and requires explicit recovery."),
            WorkerExecutionStatus.Unavailable when IsSpatialAnalyzerUnavailable(outcome) =>
                CreateFailure(
                    StatusCode.Unavailable,
                    operationId,
                    OperationFailureKind.SpatialAnalyzerUnavailable,
                    diagnosticCode,
                    ToProtocolDisposition(outcome.ExecutionDisposition),
                    RecoveryAfterUnavailable(outcome),
                    ReplayAfterUnavailable(outcome.ExecutionDisposition, replaySafety),
                    replaySafety,
                    outcome.Generation,
                    mpExecution: null,
                    "SpatialAnalyzer is not ready for MP execution."),
            WorkerExecutionStatus.Unavailable => CreateFailure(
                StatusCode.Unavailable,
                operationId,
                OperationFailureKind.WorkerUnavailable,
                diagnosticCode,
                ToProtocolDisposition(outcome.ExecutionDisposition),
                RecoveryAfterUnavailable(outcome),
                ReplayAfterUnavailable(outcome.ExecutionDisposition, replaySafety),
                replaySafety,
                outcome.Generation,
                mpExecution: null,
                "The SpatialAnalyzer worker is not ready."),
            _ => CreateInternalFailure(
                operationId,
                replaySafety,
                outcome.Generation,
                outcome.ExecutionDisposition,
                diagnosticCode)
        };
    }

    private static RpcException CreateInternalFailure(
        string operationId,
        ReplaySafety replaySafety,
        int generation,
        WorkerExecutionDisposition executionDisposition,
        string diagnosticCode) =>
        CreateFailure(
            StatusCode.Internal,
            operationId,
            OperationFailureKind.Internal,
            NormalizeDiagnosticCode(diagnosticCode, "internal-operation-failure"),
            ToProtocolDisposition(executionDisposition),
            RecoveryGuidance.None,
            executionDisposition == WorkerExecutionDisposition.StartedOutcomeUnknown
                ? ReplayAfterAmbiguousCompletion(replaySafety)
                : ReplayGuidance.DoNotReplay,
            replaySafety,
            generation,
            mpExecution: null,
            "The operation returned an invalid internal result.");

    private static RpcException CreateFailure(
        StatusCode statusCode,
        string operationId,
        OperationFailureKind kind,
        string diagnosticCode,
        ExecutionDisposition executionDisposition,
        RecoveryGuidance recoveryGuidance,
        ReplayGuidance replayGuidance,
        ReplaySafety replaySafety,
        int generation,
        MpExecutionDetails? mpExecution,
        string detail)
    {
        var error = new OperationError
        {
            OperationId = operationId,
            Kind = kind,
            DiagnosticCode = diagnosticCode,
            ExecutionDisposition = executionDisposition,
            RecoveryGuidance = recoveryGuidance,
            ReplayGuidance = replayGuidance,
            ReplaySafety = replaySafety,
            WorkerGeneration = generation,
            MpExecution = mpExecution
        };
        var trailers = new Metadata
        {
            { ErrorTrailerName, error.ToByteArray() }
        };

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
            State = state
        };
        if (execution.MpResultCode is { } resultCode)
        {
            details.MpResultCode = resultCode;
        }
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
            State = MpExecutionState.Succeeded
        };
        if (execution.MpResultCode is { } resultCode)
        {
            details.MpResultCode = resultCode;
        }
        for (var index = 0; index < outputs.Count; index++)
        {
            var output = outputs[index];
            var value = execution.OutputValues[index];
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
        IReadOnlyList<WorkerMpOutputValue> returned)
    {
        if (requested.Count != returned.Count)
        {
            return false;
        }

        for (var index = 0; index < requested.Count; index++)
        {
            if (requested[index].ArgumentName != returned[index].Name ||
                requested[index].Kind != returned[index].Kind)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasTypedValue(WorkerMpOutputValue value) =>
        value.Kind switch
        {
            WorkerMpValueKind.Logical => ((value.ReadValue() as WorkerBooleanValue)?.Value).HasValue,
            WorkerMpValueKind.WholeNumber => ((value.ReadValue() as WorkerIntegerValue)?.Value).HasValue,
            WorkerMpValueKind.FloatingPoint => ((value.ReadValue() as WorkerDoubleValue)?.Value).HasValue,
            WorkerMpValueKind.DoubleArray => (value.ReadValue() as WorkerDoubleArrayValue) is not null,
            WorkerMpValueKind.EditText => (value.ReadValue() as WorkerStringListValue) is not null,
            WorkerMpValueKind.Transform => (value.ReadValue() as WorkerTransformValue) is not null,
            WorkerMpValueKind.WorldTransform => (value.ReadValue() as WorkerWorldTransformValue) is not null,
            WorkerMpValueKind.FileReference => (value.ReadValue() as WorkerFileReferenceValue) is not null,
            WorkerMpValueKind.FitConstraintScalarOptions =>
                (value.ReadValue() as WorkerFitConstraintScalarOptionsValue) is not null,
            WorkerMpValueKind.ToleranceScalarOptions =>
                (value.ReadValue() as WorkerToleranceScalarOptionsValue) is not null,
            WorkerMpValueKind.Text or
            WorkerMpValueKind.ChartName or
            WorkerMpValueKind.CloudName or
            WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or
            WorkerMpValueKind.VectorGroupName or
            WorkerMpValueKind.ViewName => ((value.ReadValue() as WorkerTextValue)?.Value) is not null,
            WorkerMpValueKind.PointName => (value.ReadValue() as WorkerPointNameValue) is not null,
            WorkerMpValueKind.Vector => (value.ReadValue() as WorkerVectorValue) is not null,
            WorkerMpValueKind.ToleranceVectorOptions =>
                (value.ReadValue() as WorkerToleranceVectorOptionsValue) is not null,
            WorkerMpValueKind.CollectionInstrumentId =>
                (value.ReadValue() as WorkerCollectionInstrumentIdValue) is not null,
            WorkerMpValueKind.CollectionInstrumentIdList =>
                (value.ReadValue() as WorkerCollectionInstrumentIdListValue) is not null,
            WorkerMpValueKind.CollectionMachineId =>
                (value.ReadValue() as WorkerCollectionMachineIdValue) is not null,
            WorkerMpValueKind.CollectionItemName =>
                (value.ReadValue() as WorkerCollectionItemNameValue) is not null,
            WorkerMpValueKind.CollectionItemNameList =>
                (value.ReadValue() as WorkerCollectionItemNameListValue) is not null,
            WorkerMpValueKind.CollectionObjectName =>
                (value.ReadValue() as WorkerCollectionObjectNameValue) is not null,
            WorkerMpValueKind.CollectionObjectNameList =>
                (value.ReadValue() as WorkerCollectionObjectNameListValue) is not null,
            WorkerMpValueKind.CollectionGroupNameList =>
                (value.ReadValue() as WorkerCollectionGroupNameListValue) is not null,
            WorkerMpValueKind.CollectionVectorGroupName =>
                (value.ReadValue() as WorkerCollectionVectorGroupNameValue) is not null,
            WorkerMpValueKind.CollectionVectorGroupNameList =>
                (value.ReadValue() as WorkerCollectionVectorGroupNameListValue) is not null,
            WorkerMpValueKind.PointNameList => (value.ReadValue() as WorkerPointNameListValue) is not null,
            WorkerMpValueKind.StringList => (value.ReadValue() as WorkerStringListValue) is not null,
            WorkerMpValueKind.VectorNameList => (value.ReadValue() as WorkerVectorNameListValue) is not null,
            _ => false
        };

    private static ExecutionDisposition ToProtocolDisposition(
        WorkerExecutionDisposition disposition) =>
        disposition switch
        {
            WorkerExecutionDisposition.NotStarted => ExecutionDisposition.NotStarted,
            WorkerExecutionDisposition.StartedOutcomeUnknown =>
                ExecutionDisposition.StartedOutcomeUnknown,
            WorkerExecutionDisposition.Completed => ExecutionDisposition.Completed,
            _ => ExecutionDisposition.Unspecified
        };

    private static ReplayGuidance ReplayAfterInterruption(
        WorkerExecutionDisposition disposition,
        ReplaySafety replaySafety) =>
        disposition switch
        {
            WorkerExecutionDisposition.NotStarted => ReplayGuidance.MayReplay,
            WorkerExecutionDisposition.StartedOutcomeUnknown =>
                ReplayAfterAmbiguousCompletion(replaySafety),
            _ => ReplayGuidance.DoNotReplay
        };

    private static ReplayGuidance ReplayAfterUnavailable(
        WorkerExecutionDisposition disposition,
        ReplaySafety replaySafety) =>
        disposition == WorkerExecutionDisposition.NotStarted
            ? ReplayGuidance.MayReplay
            : ReplayAfterAmbiguousCompletion(replaySafety);

    private static RecoveryGuidance RecoveryAfterUnavailable(
        WorkerExecutionOutcome outcome) =>
        outcome.Connection?.ExecutionReadinessState ==
            WorkerExecutionReadinessState.OperatorRecoveryRequired
                ? RecoveryGuidance.OperatorInterventionRequired
                : RecoveryGuidance.WaitForReadiness;

    private static ReplayGuidance ReplayAfterAmbiguousCompletion(
        ReplaySafety replaySafety) =>
        replaySafety == ReplaySafety.Safe
            ? ReplayGuidance.MayReplay
            : ReplayGuidance.ReconcileBeforeReplay;

    private static bool IsSpatialAnalyzerUnavailable(WorkerExecutionOutcome outcome) =>
        outcome.Connection is { State: not WorkerConnectionState.Connected } ||
        outcome.Connection is
        {
            ExecutionReadinessState: not WorkerExecutionReadinessState.ExecutionReady
        };

    private static string NormalizeDiagnosticCode(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) && value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
                ? value
                : fallback;
}
