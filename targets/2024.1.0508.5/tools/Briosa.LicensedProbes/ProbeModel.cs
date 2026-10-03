using Briosa.Worker.Control;
using Google.Protobuf;

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
    // MP code 2 and every output retrieved. Completed.
    Succeeded,

    // GetMPStepResult returned true with an MP code other than 2. Completed.
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

/// <summary>A labelled interpretation of one structural observation.</summary>
internal sealed record ProbeHypothesis(string Label, string ObservationKey, string ExpectedValue);

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

    public IReadOnlyList<ProbeHypothesis> Hypotheses { get; init; } = [];

    public ProbeRequirement? Requirement { get; init; }

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

    /// <summary>The call may have started and its completion is unknown, by kind or by reported disposition.</summary>
    public bool CompletionUnknown =>
        Kind.IsCompletionUnknown() || string.Equals(ExecutionDisposition, StartedOutcomeUnknown, StringComparison.Ordinal);

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
