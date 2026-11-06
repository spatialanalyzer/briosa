using System.Globalization;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>Which client executes a session. One session never mixes clients.</summary>
internal enum ProbePhase
{
    // Generated gRPC client against a running packaged Briosa server.
    PublicApi,

    // The harness hosts one unchanged Briosa.Worker process directly.
    Worker
}

internal enum ProbeStepKind
{
    Guard,
    Setup,
    Check,
    Probe
}

/// <summary>Classified result of one executed step.</summary>
internal enum ProbeOutcomeKind
{
    // MP code 2, or code 4 (PARTIAL SUCCESS) as the shipped server reports it,
    // and every output retrieved. Completed.
    Succeeded,

    // GetMPStepResult returned true with an MP code other than 2 or 4, or code 4
    // from an operation the server's exception list reports as failed. Completed.
    MpFailed,

    // A setter returned false, so ExecuteStep was never called. Not started.
    ArgumentRejected,

    // Setters succeeded and ExecuteStep returned false. As in the shipped server,
    // completion is unknown: the session stops and never replays.
    ExecuteStepRejected,

    // ExecuteStep returned true and GetMPStepResult returned false. As in the
    // shipped server, completion is unknown: the session stops and never replays.
    MpResultUnavailable,

    // An MP code was retrieved but at least one output was not. Completed.
    OutputRetrievalFailed,

    // Definitely not started: validation, policy, or readiness refusal.
    NotStarted,

    // Completion is unknown. Never replayed; the session always stops.
    Indeterminate,

    // A precondition owned by the harness refused the step before dispatch.
    RefusedByHarness
}

/// <summary>
/// Outcomes a step may accept. Only determinate outcomes have a flag: an outcome
/// whose completion is unknown can never be accepted by any step.
/// </summary>
[Flags]
internal enum ProbeOutcomes
{
    None = 0,
    Succeeded = 1,
    MpFailed = 2,
    ArgumentRejected = 4,
    OutputRetrievalFailed = 8,

    // Every SDK sequence that definitely completed or definitely did not start.
    // ExecuteStepRejected, MpResultUnavailable, and Indeterminate (completion
    // unknown), not-started refusals, and harness refusals are in no set.
    AnyDeterminateSdkOutcome = Succeeded | MpFailed | ArgumentRejected | OutputRetrievalFailed
}

internal static class ProbeOutcomeKindExtensions
{
    // Completion-unknown kinds deliberately have no flag.
    public static ProbeOutcomes ToFlag(this ProbeOutcomeKind kind) => kind switch
    {
        ProbeOutcomeKind.Succeeded => ProbeOutcomes.Succeeded,
        ProbeOutcomeKind.MpFailed => ProbeOutcomes.MpFailed,
        ProbeOutcomeKind.ArgumentRejected => ProbeOutcomes.ArgumentRejected,
        ProbeOutcomeKind.OutputRetrievalFailed => ProbeOutcomes.OutputRetrievalFailed,
        _ => ProbeOutcomes.None
    };

    /// <summary>
    /// True when the call may have started and its completion is unknown. The
    /// session stops there as do-not-replay and the worker is not reused.
    /// </summary>
    public static bool IsCompletionUnknown(this ProbeOutcomeKind kind) =>
        kind is ProbeOutcomeKind.ExecuteStepRejected or ProbeOutcomeKind.MpResultUnavailable or ProbeOutcomeKind.Indeterminate;

    /// <summary>Whether a step that accepts <paramref name="acceptable"/> accepts this outcome.</summary>
    public static bool Accepts(this ProbeOutcomes acceptable, ProbeOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var flag = outcome.Kind.ToFlag();
        return flag != ProbeOutcomes.None && acceptable.HasFlag(flag) && !outcome.CompletionUnknown;
    }
}

/// <summary>
/// Where a licensed run pauses for the operator. The session sends nothing until
/// the operator answers, and an unanswered checkpoint stops the session.
/// </summary>
[Flags]
internal enum OperatorCheckpoint
{
    None = 0,

    // Before dispatch: the operator confirms that no SpatialAnalyzer dialog is open.
    Before = 1,

    // After the result: the operator records any dialog the step raised and dismisses it.
    After = 2
}

/// <summary>
/// A labelled interpretation of one structural observation, or of one outcome field
/// named with the <c>outcome.</c> prefix (see <see cref="ProbeOutcome.Fact"/>).
/// </summary>
internal sealed record ProbeHypothesis(string Label, string ObservationKey, string ExpectedValue);

/// <summary>
/// The stall a stall step is planned to end in (#313): a SpatialAnalyzer dialog blocks
/// the MP until a watchdog ends the step. Only a completion-unknown outcome with these
/// values matches. A null field is not compared.
/// </summary>
internal sealed record StallExpectation(string Transport, string DiagnosticCode, string? FailureKind, string? RecoveryGuidance)
{
    // The diagnostic the server reports for a stall while a SpatialAnalyzer dialog is open.
    public const string ModalDialogDiagnosticCode = WorkerProcessSupervisor.ModalDialogDiagnosticCode;

    // The server's watchdog ends the exchange and reports the open dialog (PR #355).
    public static StallExpectation ServerReportsModalDialog { get; } = new(
        "grpc:" + Grpc.Core.StatusCode.Unavailable, ModalDialogDiagnosticCode,
        Api.OperationFailureKind.WorkerWatchdogTimeout.ToString(), Api.RecoveryGuidance.OperatorInterventionRequired.ToString());

    // The worker phase has no server: the harness's own step deadline ends the step and
    // the server's detector, run by the harness, reports the open dialog.
    public static StallExpectation HarnessReportsModalDialog { get; } = new(
        WorkerTransport.TimeoutTransport, ModalDialogDiagnosticCode, null, null);

    public bool Matches(ProbeOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return outcome is { Kind: ProbeOutcomeKind.Indeterminate, CompletionUnknown: true } &&
            string.Equals(outcome.Transport, Transport, StringComparison.Ordinal) &&
            string.Equals(outcome.DiagnosticCode, DiagnosticCode, StringComparison.Ordinal) &&
            (FailureKind is null || string.Equals(outcome.FailureKind, FailureKind, StringComparison.Ordinal)) &&
            (RecoveryGuidance is null || string.Equals(outcome.RecoveryGuidance, RecoveryGuidance, StringComparison.Ordinal));
    }
}

/// <summary>A cross-step requirement such as "the collection count increased by one".</summary>
internal sealed record ProbeRequirement(
    string Description,
    Func<IReadOnlyDictionary<string, ProbeOutcome>, ProbeOutcome, bool> IsSatisfied);

/// <summary>One planned call. Steps are immutable and executed at most once.</summary>
internal sealed record ProbeStep(
    string Id,
    string Probe,
    ProbeStepKind Kind,
    ProbePhase Phase,
    string Purpose,
    ProbeOperation Operation,
    IMessage Request,
    CommandVariant Variant,
    ProbeOutcomes Acceptable)
{
    // Fixture key of a harness-created disposable object this step may destroy.
    public string? DestructiveTarget { get; init; }

    // Fixture key this step creates when it succeeds.
    public string? CreatesFixture { get; init; }

    public bool RequiresOperator { get; init; }

    public OperatorCheckpoint Checkpoints { get; init; }

    // Replaces the session's step or operator timeout as this step's client deadline.
    public TimeSpan? ClientTimeout { get; init; }

    // A stall step (#313) accepts no determinate outcome: it is planned to end the run
    // in this stall, and nothing is ever sent after it.
    public StallExpectation? ExpectedStall { get; init; }

    // The server setting Briosa:SpatialAnalyzer:InteractionMode this step needs (probe
    // 29). Null when the step relies on the default, Unchanged.
    public Api.SaInteractionMode? ServerInteractionMode { get; init; }

    public IReadOnlyList<ProbeHypothesis> Hypotheses { get; init; } = [];

    public ProbeRequirement? Requirement { get; init; }

    // Steps that must run earlier in the same session for this step's result to
    // mean what the plan says (for example a control or a before-count). An
    // operator exclusion that would remove one of them is refused.
    public IReadOnlyList<string> Prerequisites { get; init; } = [];

    /// <summary>The exact sequence shipped by this target for the request.</summary>
    public WorkerMpCommand ShippedCommand => Operation.Build(Request);

    /// <summary>The exact sequence this step sends (shipped plus at most one variation).</summary>
    public WorkerMpCommand Command => Variant.Apply(ShippedCommand);
}

internal sealed record OutputObservation(string Name, bool Retrieved);

/// <summary>Structural result of one step. It never contains argument or returned values.</summary>
internal sealed record ProbeOutcome(
    ProbeOutcomeKind Kind,
    string Transport,
    bool? ExecuteStepReturned,
    bool? MpResultRetrieved,
    int? MpResultCode,
    IReadOnlyList<OutputObservation> Outputs,
    string? DiagnosticCode,
    string? FailureKind,
    string? ExecutionDisposition,
    IReadOnlyDictionary<string, string> Observations)
{
    public const string StartedOutcomeUnknown = "StartedOutcomeUnknown";

    // The prefix of hypothesis keys that name an outcome field rather than an observation.
    public const string FactPrefix = "outcome.";

    /// <summary>The call may have started and its completion is unknown, by kind or by reported disposition.</summary>
    public bool CompletionUnknown =>
        Kind.IsCompletionUnknown() || string.Equals(ExecutionDisposition, StartedOutcomeUnknown, StringComparison.Ordinal);

    // The public error's recovery guidance (for example OperatorInterventionRequired). Null in the worker phase.
    public string? RecoveryGuidance { get; init; }

    // The public result's typed MP execution state (for example PartialSuccess, #351). Null in the worker phase.
    public string? ExecutionState { get; init; }

    /// <summary>
    /// One outcome field by its <c>outcome.</c> name, or else one observation, for
    /// hypotheses. Null when the outcome has no such value.
    /// </summary>
    public string? Fact(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key switch
        {
            FactPrefix + "kind" => Kind.ToString(),
            FactPrefix + "mp_result_code" => MpResultCode?.ToString(CultureInfo.InvariantCulture),
            FactPrefix + "execution_state" => ExecutionState,
            FactPrefix + "diagnostic_code" => DiagnosticCode,
            FactPrefix + "failure_kind" => FailureKind,
            FactPrefix + "recovery_guidance" => RecoveryGuidance,
            _ when key.StartsWith(FactPrefix, StringComparison.Ordinal) => null,
            _ => Observations.TryGetValue(key, out var value) ? value : null
        };
    }

    public static ProbeOutcome Refused(string diagnosticCode) =>
        new(ProbeOutcomeKind.RefusedByHarness, "harness", null, null, null, [],
            SafeCode.OrNull(diagnosticCode), null, null, EmptyObservations);

    public static ProbeOutcome Unknown(string transport, string diagnosticCode) =>
        new(ProbeOutcomeKind.Indeterminate, transport, null, null, null, [],
            SafeCode.OrNull(diagnosticCode), null, null, EmptyObservations);

    public static IReadOnlyDictionary<string, string> EmptyObservations { get; } =
        new SortedDictionary<string, string>(StringComparer.Ordinal);
}

/// <summary>Accepts only stable lowercase diagnostic codes; anything else is dropped.</summary>
internal static class SafeCode
{
    public static string? OrNull(string? value) =>
        value is { Length: > 0 and <= 128 } && value.All(static c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
            ? value
            : null;
}
