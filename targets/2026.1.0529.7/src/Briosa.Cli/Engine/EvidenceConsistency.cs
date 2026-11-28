using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Briosa.Cli.Engine;

/// <summary>
/// Decides whether what came back for one call is evidence about that call. A
/// typed error counts only when it names the requested operation and agrees with
/// the gRPC status, with itself, and with the result message; the allowed shapes are
/// exactly those the server's outcome mapper produces. Anything else is conflicting
/// evidence, which the CLI reports as completion evidence unavailable: it never
/// derives a disposition, a guidance, or advice to send again from it.
/// </summary>
internal static class EvidenceConsistency
{
    public const string Unrelated = "operation-error-unrelated";
    public const string Inconsistent = "operation-error-inconsistent";
    public const string KindUnrecognized = "operation-error-kind-unrecognized";
    public const string OkWithError = "ok-with-operation-error";
    public const string ResultOutputUnrelated = "result-output-unrelated";

    private static readonly MpExecutionState[] CompletedStates = [MpExecutionState.Succeeded, MpExecutionState.PartialSuccess];

    // For each failure kind, every (gRPC status, disposition, MP details) shape the
    // server's GrpcOperationOutcomeMapper produces on this target.
    private static readonly IReadOnlyDictionary<OperationFailureKind, Shape[]> Shapes =
        new Dictionary<OperationFailureKind, Shape[]>
        {
            [OperationFailureKind.Validation] = [new(StatusCode.InvalidArgument, [ExecutionDisposition.NotStarted], null)],
            [OperationFailureKind.Unsupported] = [new(StatusCode.Unimplemented, [ExecutionDisposition.NotStarted], null)],
            [OperationFailureKind.Overloaded] = [new(StatusCode.ResourceExhausted, [ExecutionDisposition.NotStarted], null)],
            [OperationFailureKind.PolicyDenied] = [new(StatusCode.PermissionDenied, [ExecutionDisposition.NotStarted], null)],
            [OperationFailureKind.CallerCancelled] =
                [new(StatusCode.Cancelled, [ExecutionDisposition.NotStarted, ExecutionDisposition.StartedOutcomeUnknown], null)],
            [OperationFailureKind.CallerDeadlineExceeded] =
                [new(StatusCode.DeadlineExceeded, [ExecutionDisposition.NotStarted, ExecutionDisposition.StartedOutcomeUnknown], null)],
            [OperationFailureKind.WorkerWatchdogTimeout] = [new(StatusCode.Unavailable, [ExecutionDisposition.StartedOutcomeUnknown], null)],
            [OperationFailureKind.WorkerFailure] = [new(StatusCode.Unavailable, [ExecutionDisposition.StartedOutcomeUnknown], null)],
            [OperationFailureKind.SpatialAnalyzerUnavailable] =
                [new(StatusCode.Unavailable, [ExecutionDisposition.NotStarted, ExecutionDisposition.StartedOutcomeUnknown], null)],
            [OperationFailureKind.WorkerUnavailable] =
                [new(StatusCode.Unavailable, [ExecutionDisposition.NotStarted, ExecutionDisposition.StartedOutcomeUnknown], null)],
            [OperationFailureKind.SdkArgumentRejected] =
                [new(StatusCode.FailedPrecondition, [ExecutionDisposition.NotStarted], [MpExecutionState.ArgumentRejected])],
            [OperationFailureKind.ExecuteStepRejected] =
                [new(StatusCode.FailedPrecondition, [ExecutionDisposition.StartedOutcomeUnknown], [MpExecutionState.ExecuteStepRejected])],
            [OperationFailureKind.MpResultRetrievalFailure] =
                [new(StatusCode.Internal, [ExecutionDisposition.StartedOutcomeUnknown], [MpExecutionState.ResultUnavailable])],
            [OperationFailureKind.MpFailure] =
                [new(StatusCode.FailedPrecondition, [ExecutionDisposition.Completed], [MpExecutionState.Failed])],
            [OperationFailureKind.OutputRetrievalFailure] =
                [new(StatusCode.DataLoss, [ExecutionDisposition.Completed], CompletedStates, OutputFailed: true)],
            [OperationFailureKind.SdkCallFaulted] =
            [
                new(StatusCode.Internal, [ExecutionDisposition.NotStarted], null),
                new(StatusCode.Internal, [ExecutionDisposition.StartedOutcomeUnknown], [MpExecutionState.ResultUnavailable]),
                new(StatusCode.DataLoss, [ExecutionDisposition.Completed], CompletedStates, OutputFailed: true)
            ],
            [OperationFailureKind.Internal] =
            [
                new(StatusCode.Internal,
                    [ExecutionDisposition.NotStarted, ExecutionDisposition.StartedOutcomeUnknown, ExecutionDisposition.Completed,
                        ExecutionDisposition.Unspecified],
                    null),
                new(StatusCode.DataLoss, [ExecutionDisposition.Completed], CompletedStates)
            ]
        };

    /// <summary>The consistency rules, as documented on the CLI page.</summary>
    public static IReadOnlyList<OperationFailureKind> KnownKinds => [.. Shapes.Keys];

    /// <summary>
    /// Null when the typed error is evidence about this call; otherwise the
    /// value-free code of the first conflict found.
    /// </summary>
    public static string? CheckError(OperationError error, StatusCode status, string operationId, MessageDescriptor result)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(result);
        if (!string.Equals(error.OperationId, operationId, StringComparison.Ordinal))
        {
            return Unrelated;
        }

        if (error.MpExecution is { } details && OutputsUnrelated(details, result))
        {
            return Unrelated;
        }

        if (!Shapes.TryGetValue(error.Kind, out var shapes))
        {
            return KindUnrecognized;
        }

        var disposition = Enum.IsDefined(error.ExecutionDisposition) ? error.ExecutionDisposition : ExecutionDisposition.Unspecified;
        return shapes.Any(shape => shape.Matches(status, disposition, error.MpExecution)) ? null : Inconsistent;
    }

    /// <summary>
    /// Null when the MP details of a gRPC OK result name only fields of that result.
    /// State and code are checked by the success rule itself.
    /// </summary>
    public static string? CheckResult(MpExecutionDetails details, MessageDescriptor result)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(result);
        return OutputsUnrelated(details, result) ? ResultOutputUnrelated : null;
    }

    // Every reported output must be a field of this operation's result message, other
    // than the execution details themselves.
    private static bool OutputsUnrelated(MpExecutionDetails details, MessageDescriptor result) =>
        details.OutputRetrievals.Any(output => result.FindFieldByName(output.FieldName) is not
            { FieldNumber: not ProtocolCatalog.ExecutionFieldNumber });

    private sealed record Shape(
        StatusCode Status,
        ExecutionDisposition[] Dispositions,
        MpExecutionState[]? States,
        bool OutputFailed = false)
    {
        public bool Matches(StatusCode status, ExecutionDisposition disposition, MpExecutionDetails? details)
        {
            if (status != Status || !Dispositions.Contains(disposition))
            {
                return false;
            }

            if (States is null)
            {
                return details is null;
            }

            return details is not null && States.Contains(details.State) && StateAgreesWithCode(details) &&
                OutputsAgree(details) && (!OutputFailed || details.OutputRetrievals.Any(output =>
                    output.State == OutputRetrievalState.Failed));
        }

        // The state and the MP code agree as the server sets them: 2 for success, 4
        // for partial success, any retrieved code but 2 for failure, none otherwise.
        private static bool StateAgreesWithCode(MpExecutionDetails details) => details.State switch
        {
            MpExecutionState.Succeeded => details is { HasMpResultCode: true, MpResultCode: 2 },
            MpExecutionState.PartialSuccess => details is { HasMpResultCode: true, MpResultCode: 4 },
            MpExecutionState.Failed => details is { HasMpResultCode: true, MpResultCode: not 2 },
            MpExecutionState.ResultUnavailable or MpExecutionState.ExecuteStepRejected or
                MpExecutionState.ArgumentRejected => !details.HasMpResultCode,
            _ => false
        };

        // Outputs are retrieved only after MP code 2 or 4; before that none was attempted.
        private static bool OutputsAgree(MpExecutionDetails details) =>
            details.State is MpExecutionState.Succeeded or MpExecutionState.PartialSuccess
                ? details.OutputRetrievals.All(output => output.State is OutputRetrievalState.Retrieved or
                    OutputRetrievalState.Failed)
                : details.OutputRetrievals.All(output => output.State == OutputRetrievalState.NotAttempted);
    }
}
