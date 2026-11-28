using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Briosa.Cli.Engine;

/// <summary>
/// Decides whether what came back for one call is evidence about that call. A
/// typed error counts only when every field the server's outcome mapper constrains
/// agrees with the mapper's own table for that failure kind: the gRPC status, the
/// disposition, the recovery and replay guidance (replay guidance follows the
/// operation's replay safety from discovery), the diagnostic code where the mapper
/// fixes it, the detail messages, and the MP details. Anything else is conflicting
/// evidence, which the CLI reports as completion evidence unavailable: it never
/// derives a disposition, a guidance, or advice to send again from it. A diagnostic
/// code the mapper does not fix is opaque data, whatever it says.
/// </summary>
internal static class EvidenceConsistency
{
    public const string Unrelated = "operation-error-unrelated";
    public const string Inconsistent = "operation-error-inconsistent";
    public const string KindUnrecognized = "operation-error-kind-unrecognized";
    public const string EnumUnrecognized = "operation-error-enum-unrecognized";
    public const string OkWithError = "ok-with-operation-error";
    public const string ResultOutputUnrelated = "result-output-unrelated";
    public const string ResultOutputMissing = "result-output-missing";
    public const string ResultExecutionContractViolation = "result-execution-contract-violation";

    // Codes the mapper writes itself, never taken from the worker.
    private const string ModalDialogOpen = "sa-modal-dialog-open";
    private const string RequestValidationFailed = "request-validation-failed";

    // For each failure kind, every combination the server's GrpcOperationOutcomeMapper
    // (and the executor that calls it) produces on this target.
    private static readonly IReadOnlyDictionary<OperationFailureKind, Shape[]> Shapes =
        new Dictionary<OperationFailureKind, Shape[]>
        {
            [OperationFailureKind.Validation] = [new(StatusCode.InvalidArgument, Disposition.NotStarted, Replay.DoNotReplay)],
            [OperationFailureKind.Unsupported] = [new(StatusCode.Unimplemented, Disposition.NotStarted, Replay.DoNotReplay)],
            [OperationFailureKind.Overloaded] = [new(StatusCode.ResourceExhausted, Disposition.NotStarted, Replay.MayReplay)],
            [OperationFailureKind.PolicyDenied] = [new(StatusCode.PermissionDenied, Disposition.NotStarted, Replay.DoNotReplay)],
            [OperationFailureKind.CallerCancelled] = Interrupted(StatusCode.Cancelled),
            [OperationFailureKind.CallerDeadlineExceeded] = Interrupted(StatusCode.DeadlineExceeded),
            // A SpatialAnalyzer dialog still blocks SA after the worker is replaced.
            [OperationFailureKind.WorkerWatchdogTimeout] =
            [
                new(StatusCode.Unavailable, Disposition.StartedOutcomeUnknown, Replay.AfterAmbiguousCompletion,
                    [Recovery.OperatorInterventionRequired], Code: ModalDialogOpen),
                new(StatusCode.Unavailable, Disposition.StartedOutcomeUnknown, Replay.AfterAmbiguousCompletion,
                    [Recovery.WorkerReplacement], NotCode: ModalDialogOpen)
            ],
            [OperationFailureKind.WorkerFailure] =
            [
                new(StatusCode.Unavailable, Disposition.StartedOutcomeUnknown, Replay.AfterAmbiguousCompletion,
                    [Recovery.WorkerReplacement])
            ],
            // Operator recovery is required only when SpatialAnalyzer's own readiness
            // says so, which always makes the failure SpatialAnalyzer's.
            [OperationFailureKind.SpatialAnalyzerUnavailable] =
                Unavailable([Recovery.WaitForReadiness, Recovery.OperatorInterventionRequired]),
            [OperationFailureKind.WorkerUnavailable] = Unavailable([Recovery.WaitForReadiness]),
            [OperationFailureKind.SdkArgumentRejected] =
                [new(StatusCode.FailedPrecondition, Disposition.NotStarted, Replay.DoNotReplay, Mp: Mp.ArgumentRejected)],
            [OperationFailureKind.ExecuteStepRejected] =
            [
                new(StatusCode.FailedPrecondition, Disposition.StartedOutcomeUnknown, Replay.AfterAmbiguousCompletion,
                    Mp: Mp.ExecuteStepRejected)
            ],
            [OperationFailureKind.MpResultRetrievalFailure] =
                [new(StatusCode.Internal, Disposition.StartedOutcomeUnknown, Replay.ReconcileBeforeReplay, Mp: Mp.ResultUnavailable)],
            [OperationFailureKind.MpFailure] =
                [new(StatusCode.FailedPrecondition, Disposition.Completed, Replay.DoNotReplay, Mp: Mp.Failed)],
            [OperationFailureKind.OutputRetrievalFailure] =
                [new(StatusCode.DataLoss, Disposition.Completed, Replay.DoNotReplay, Mp: Mp.OutputsFailed)],
            // The proven phase of the faulted SDK call selects the shape.
            [OperationFailureKind.SdkCallFaulted] =
            [
                new(StatusCode.Internal, Disposition.NotStarted, Replay.DoNotReplay),
                new(StatusCode.Internal, Disposition.StartedOutcomeUnknown, Replay.ReconcileBeforeReplay, Mp: Mp.ResultUnavailable),
                new(StatusCode.DataLoss, Disposition.Completed, Replay.DoNotReplay, Mp: Mp.OutputsFailed)
            ],
            // Every internal failure the server can reach has its own fixed code.
            [OperationFailureKind.Internal] =
            [
                new(StatusCode.Internal, Disposition.Completed, Replay.DoNotReplay, Code: "worker-output-shape-invalid"),
                new(StatusCode.Internal, Disposition.StartedOutcomeUnknown, Replay.AfterAmbiguousCompletion,
                    Code: "request-violation-after-mapping"),
                new(StatusCode.DataLoss, Disposition.Completed, Replay.DoNotReplay, Mp: Mp.AllRetrieved,
                    Code: "result-mapping-failed")
            ]
        };

    private enum Disposition
    {
        NotStarted = ExecutionDisposition.NotStarted,
        StartedOutcomeUnknown = ExecutionDisposition.StartedOutcomeUnknown,
        Completed = ExecutionDisposition.Completed
    }

    private enum Recovery
    {
        None = RecoveryGuidance.None,
        WaitForReadiness = RecoveryGuidance.WaitForReadiness,
        WorkerReplacement = RecoveryGuidance.WorkerReplacement,
        OperatorInterventionRequired = RecoveryGuidance.OperatorInterventionRequired
    }

    private enum Replay
    {
        DoNotReplay,
        MayReplay,
        ReconcileBeforeReplay,

        // MAY_REPLAY for a SAFE operation, otherwise RECONCILE_BEFORE_REPLAY.
        AfterAmbiguousCompletion
    }

    // The MP details a shape carries.
    private enum Mp
    {
        None,
        ArgumentRejected,
        ExecuteStepRejected,
        ResultUnavailable,
        Failed,

        // MP code 2 or 4; every output retrieved or failed, and at least one failed
        // unless the operation has no outputs at all.
        OutputsFailed,

        // MP code 2 or 4 and every output retrieved.
        AllRetrieved
    }

    /// <summary>The failure kinds the consistency rules know.</summary>
    public static IReadOnlyList<OperationFailureKind> KnownKinds => [.. Shapes.Keys];

    /// <summary>
    /// Null when the typed error is evidence about this call; otherwise the
    /// value-free code of the first conflict found.
    /// </summary>
    public static string? CheckError(OperationError error, StatusCode status, MpCallContext call)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(call);
        if (!string.Equals(error.OperationId, call.OperationId, StringComparison.Ordinal))
        {
            return Unrelated;
        }

        if (!Shapes.TryGetValue(error.Kind, out var shapes))
        {
            return KindUnrecognized;
        }

        if (!Enum.IsDefined(error.ExecutionDisposition) || !Enum.IsDefined(error.RecoveryGuidance) ||
            !Enum.IsDefined(error.ReplayGuidance) || !Enum.IsDefined(error.ReplaySafety) ||
            error.PolicyDenial is { } denial && !Enum.IsDefined(denial.Rule))
        {
            return EnumUnrecognized;
        }

        // The server copies the operation's replay safety into every error, so a
        // different one is about another operation.
        if (error.ReplaySafety != call.ReplaySafety)
        {
            return Unrelated;
        }

        if (error.MpExecution is { } details && !OutputsResolve(details, call.ResultDescriptor))
        {
            return Unrelated;
        }

        if (!error.HasWorkerGeneration || error.WorkerGeneration < 0 ||
            (error.Kind == OperationFailureKind.PolicyDenied) != (error.PolicyDenial is not null) ||
            error.PolicyDenial is { } stated && !DenialFits(stated, call.CallerRestriction) ||
            error.RequestValidation is not null && (error.Kind != OperationFailureKind.Validation ||
                !string.Equals(error.DiagnosticCode, RequestValidationFailed, StringComparison.Ordinal)) ||
            !shapes.Any(shape => shape.Matches(error, status, call)) ||
            error.MpExecution is { } reported && !OutputsCovered(reported, call.ResultDescriptor))
        {
            return Inconsistent;
        }

        return null;
    }

    /// <summary>
    /// Null when the MP details of a gRPC OK result are what the server sends with a
    /// success: MP code 2 or 4 in its own state, every output retrieved, and output
    /// rows that name, and cover, the result's own fields.
    /// </summary>
    public static string? CheckResult(MpExecutionDetails details, MessageDescriptor result)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(result);
        if (!IsCompleted(details) ||
            details.OutputRetrievals.Any(output => output.State != OutputRetrievalState.Retrieved))
        {
            return ResultExecutionContractViolation;
        }

        if (!OutputsResolve(details, result))
        {
            return ResultOutputUnrelated;
        }

        return OutputsCovered(details, result) ? null : ResultOutputMissing;
    }

    // A denial's detail carries exactly the fields its rule names, as the server's
    // OperationPolicyDenial writes them. A caller rule names what this CLI's own
    // restriction says: the server names the restriction's profile, and the first of
    // the request's flags that the restriction itself denies.
    private static bool DenialFits(PolicyDenialDetails denial, string callerRestriction)
    {
        var (profile, denied) = ReadRestriction(callerRestriction);
        var option = !denial.HasRequestOptionField || Stated(denial.HasRequestOptionField, denial.RequestOptionField);
        bool Only(bool deniedFlag = false, bool admissionProfile = false, bool callerProfile = false,
            bool callerFlag = false) =>
            denial.HasDeniedFlag == deniedFlag && denial.HasAdmissionProfile == admissionProfile &&
            denial.HasCallerAdmissionProfile == callerProfile && denial.HasCallerDeniedFlag == callerFlag;
        return denial.Rule switch
        {
            PolicyDenialRule.Profile => Only(admissionProfile: true) && option &&
                Stated(denial.HasAdmissionProfile, denial.AdmissionProfile),
            PolicyDenialRule.Flag => Only(deniedFlag: true) && option && Stated(denial.HasDeniedFlag, denial.DeniedFlag),
            PolicyDenialRule.Override or PolicyDenialRule.Unreviewed => Only() && !denial.HasRequestOptionField,
            PolicyDenialRule.Exclusive => Only() && option,
            PolicyDenialRule.CallerProfile => Only(callerProfile: true) && option &&
                string.Equals(denial.CallerAdmissionProfile, profile, StringComparison.Ordinal),
            PolicyDenialRule.CallerFlag => Only(callerFlag: true) && option &&
                denied.Contains(denial.CallerDeniedFlag, StringComparer.Ordinal),
            _ => false
        };
    }

    private static bool Stated(bool present, string value) => present && !string.IsNullOrWhiteSpace(value);

    // The profile and the denied flags of a restriction header in the server's syntax,
    // for example "profile=read-only; deny=interactive_ui".
    private static (string? Profile, string[] Denied) ReadRestriction(string restriction)
    {
        string? profile = null;
        string[] denied = [];
        foreach (var clause in restriction.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = clause.Split('=', 2);
            if (parts is ["profile", var name])
            {
                profile = name;
            }
            else if (parts is ["deny", var flags])
            {
                denied = flags.Split(',');
            }
        }

        return (profile, denied);
    }

    private static Shape[] Interrupted(StatusCode status) =>
    [
        new(status, Disposition.NotStarted, Replay.MayReplay),
        new(status, Disposition.StartedOutcomeUnknown, Replay.AfterAmbiguousCompletion)
    ];

    private static Shape[] Unavailable(Recovery[] recoveries) =>
    [
        new(StatusCode.Unavailable, Disposition.NotStarted, Replay.MayReplay, recoveries),
        new(StatusCode.Unavailable, Disposition.StartedOutcomeUnknown, Replay.AfterAmbiguousCompletion, recoveries)
    ];

    // MP code 2 with SUCCEEDED, or 4 with PARTIAL_SUCCESS: the codes that permit outputs.
    private static bool IsCompleted(MpExecutionDetails details) => details.State switch
    {
        MpExecutionState.Succeeded => details is { HasMpResultCode: true, MpResultCode: 2 },
        MpExecutionState.PartialSuccess => details is { HasMpResultCode: true, MpResultCode: 4 },
        _ => false
    };

    // The server writes one row per output contract, named by the contract's field
    // name: a field of the result, a dotted path into one, or the bare name of a
    // field nested inside one. Every row must name something in this result.
    private static bool OutputsResolve(MpExecutionDetails details, MessageDescriptor result) =>
        details.OutputRetrievals.All(output => OutputFields(result).Any(field => Names(output.FieldName, field)));

    // Every field of the result is written from its own outputs, so each field has a
    // row of its own: the rows can be matched to the fields so that every field gets
    // a different row that names it. One row never answers for two fields.
    private static bool OutputsCovered(MpExecutionDetails details, MessageDescriptor result)
    {
        var fields = OutputFields(result).ToArray();
        var rows = details.OutputRetrievals;
        var names = fields.Select(field => rows.Select(row => Names(row.FieldName, field)).ToArray()).ToArray();

        // Kuhn's augmenting paths: a maximum matching of fields to rows.
        var owner = Enumerable.Repeat(-1, rows.Count).ToArray();
        bool Assign(int field, bool[] tried)
        {
            for (var row = 0; row < rows.Count; row++)
            {
                if (names[field][row] && !tried[row])
                {
                    tried[row] = true;
                    if (owner[row] < 0 || Assign(owner[row], tried))
                    {
                        owner[row] = field;
                        return true;
                    }
                }
            }

            return false;
        }

        for (var field = 0; field < fields.Length; field++)
        {
            if (!Assign(field, new bool[rows.Count]))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<FieldDescriptor> OutputFields(MessageDescriptor result) =>
        result.Fields.InFieldNumberOrder().Where(field => field.FieldNumber != ProtocolCatalog.ExecutionFieldNumber);

    // Whether a row's name refers to this top-level result field or to a field inside it.
    private static bool Names(string name, FieldDescriptor field)
    {
        if (string.Equals(name, field.Name, StringComparison.Ordinal))
        {
            return true;
        }

        if (name.StartsWith(field.Name + ".", StringComparison.Ordinal))
        {
            return ResolvesBelow(field, name[(field.Name.Length + 1)..]);
        }

        return !name.Contains('.', StringComparison.Ordinal) && field.FieldType == FieldType.Message &&
            Contains(field.MessageType, name, []);
    }

    private static bool ResolvesBelow(FieldDescriptor field, string path)
    {
        foreach (var segment in path.Split('.'))
        {
            if (field.FieldType != FieldType.Message || field.MessageType.FindFieldByName(segment) is not { } next)
            {
                return false;
            }

            field = next;
        }

        return true;
    }

    private static bool Contains(MessageDescriptor message, string name, HashSet<string> visited)
    {
        if (!visited.Add(message.FullName))
        {
            return false;
        }

        foreach (var field in message.Fields.InFieldNumberOrder())
        {
            if (string.Equals(field.Name, name, StringComparison.Ordinal) ||
                field.FieldType == FieldType.Message && Contains(field.MessageType, name, visited))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record Shape(
        StatusCode Status,
        Disposition Disposition,
        Replay Replay,
        Recovery[]? Recoveries = null,
        Mp Mp = Mp.None,
        string? Code = null,
        string? NotCode = null)
    {
        public bool Matches(OperationError error, StatusCode status, MpCallContext call) =>
            status == Status &&
            error.ExecutionDisposition == (ExecutionDisposition)Disposition &&
            (Recoveries ?? [Recovery.None]).Any(recovery => error.RecoveryGuidance == (RecoveryGuidance)recovery) &&
            error.ReplayGuidance == Expected(call.ReplaySafety) &&
            (Code is null || string.Equals(error.DiagnosticCode, Code, StringComparison.Ordinal)) &&
            (NotCode is null || !string.Equals(error.DiagnosticCode, NotCode, StringComparison.Ordinal)) &&
            MpMatches(error.MpExecution, call.CodeFourMayBeFailure);

        private ReplayGuidance Expected(ReplaySafety replaySafety) => Replay switch
        {
            Replay.DoNotReplay => ReplayGuidance.DoNotReplay,
            Replay.MayReplay => ReplayGuidance.MayReplay,
            Replay.ReconcileBeforeReplay => ReplayGuidance.ReconcileBeforeReplay,
            _ => replaySafety == ReplaySafety.Safe ? ReplayGuidance.MayReplay : ReplayGuidance.ReconcileBeforeReplay
        };

        // The state, the MP code, and the output rows agree as the mapper sets them:
        // no code before a result was retrieved, and no output attempted before
        // MP code 2 or 4.
        private bool MpMatches(MpExecutionDetails? details, bool codeFourMayBeFailure) => Mp switch
        {
            Mp.None => details is null,
            Mp.ArgumentRejected => NotRetrieved(details, MpExecutionState.ArgumentRejected),
            Mp.ExecuteStepRejected => NotRetrieved(details, MpExecutionState.ExecuteStepRejected),
            Mp.ResultUnavailable => NotRetrieved(details, MpExecutionState.ResultUnavailable),
            // Code 2 is never a failure. Code 4 is one only for the server's reviewed
            // exceptions, none of which a read-only call can reach.
            Mp.Failed => details is { State: MpExecutionState.Failed, HasMpResultCode: true } &&
                details.MpResultCode != 2 && (codeFourMayBeFailure || details.MpResultCode != 4) &&
                AllRows(details, OutputRetrievalState.NotAttempted),
            Mp.OutputsFailed => details is not null && IsCompleted(details) &&
                details.OutputRetrievals.All(output => output.State is OutputRetrievalState.Retrieved or
                    OutputRetrievalState.Failed) &&
                (details.OutputRetrievals.Count == 0 ||
                    details.OutputRetrievals.Any(output => output.State == OutputRetrievalState.Failed)),
            _ => details is not null && IsCompleted(details) && AllRows(details, OutputRetrievalState.Retrieved)
        };

        private static bool NotRetrieved(MpExecutionDetails? details, MpExecutionState state) =>
            details is not null && details.State == state && !details.HasMpResultCode &&
            AllRows(details, OutputRetrievalState.NotAttempted);

        private static bool AllRows(MpExecutionDetails details, OutputRetrievalState state) =>
            details.OutputRetrievals.All(output => output.State == state);
    }
}
