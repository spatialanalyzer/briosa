using System.Diagnostics.CodeAnalysis;

namespace Briosa.LicensedProbes;

/// <summary>
/// Executes planned steps through exactly one client. Implementations never
/// retry: a step is sent at most once, and an unknown outcome is reported, not
/// replayed. They report diagnostic codes only, never raw exception text.
/// </summary>
internal interface IProbeTransport : IAsyncDisposable
{
    ProbePhase Phase { get; }

    /// <summary>Verifies identity and readiness. Throws <see cref="ProbeRefusedException"/> to refuse.</summary>
    Task<SessionIdentity> StartAsync(ProbePlan plan, CancellationToken cancellationToken);

    /// <summary>
    /// Sends one step once. Transport failures become an indeterminate outcome.
    /// After an outcome whose completion is unknown the session sends no further
    /// step, and a transport that owns a worker terminates it.
    /// </summary>
    Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken);
}

/// <summary>
/// The operator at the SpatialAnalyzer desktop. It reports each step as it is
/// dispatched and completed, and it answers checkpoints. Progress carries step
/// identifiers, outcome kinds, and MP codes only, never values.
/// </summary>
internal interface IOperatorConsole
{
    void Dispatching(ProbeStep step, int ordinal, int count);

    void Completed(ProbeStep step, int ordinal, int count, ProbeOutcome outcome, long elapsedMilliseconds);

    /// <summary>
    /// Waits for the operator's observation at one checkpoint. Returns null when
    /// the operator did not answer in time or the session was cancelled.
    /// </summary>
    Task<string?> CheckpointAsync(ProbeStep step, OperatorCheckpoint timing, CancellationToken cancellationToken);
}

/// <summary>A refusal before any MP work, carrying one stable diagnostic code.</summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors",
    Justification = "The refusal carries exactly one stable diagnostic code and never wraps vendor exceptions.")]
internal sealed class ProbeRefusedException(string diagnosticCode) : Exception(diagnosticCode)
{
    public string DiagnosticCode { get; } = SafeCode.OrNull(diagnosticCode) ?? "probe-refused";
}

/// <summary>Non-sensitive identity facts recorded with a session.</summary>
internal sealed record SessionIdentity
{
    public string? ServerVersion { get; init; }

    public string? ServerSourceRevision { get; init; }

    public string? ActivatedSdkVersion { get; init; }

    public string? ActivatedSdkEvidence { get; init; }

    public string? ConnectedSpatialAnalyzerAttestedVersion { get; init; }

    public string? ConnectedSpatialAnalyzerAttestationReference { get; init; }

    public string? ActivatedSdkAttestationReference { get; init; }

    public string? HarnessRevision { get; init; }

    public static SessionIdentity None { get; } = new();
}

internal enum StepClassification
{
    NotRun,
    Observed,
    Unexpected,
    Refused,

    // Removed from the plan by an operator --exclude-probe; never sent. Distinct
    // from NotRun, which is a planned step that a stop or refusal prevented.
    Excluded
}

internal sealed record HypothesisResult(string Label, string ObservationKey, string ExpectedValue, bool Matched);

/// <summary>What the operator reported at one checkpoint, already validated as value-free text.</summary>
internal sealed record OperatorObservation(OperatorCheckpoint Timing, string Text);

internal sealed record ProbeStepRecord(
    ProbeStep Step,
    ProbeOutcome? Outcome,
    StepClassification Classification,
    IReadOnlyList<HypothesisResult> Hypotheses,
    bool? RequirementSatisfied)
{
    public IReadOnlyList<OperatorObservation> OperatorObservations { get; init; } = [];

    // From dispatch to the transport's answer, by the session clock. Null when not sent.
    public long? ElapsedMilliseconds { get; init; }
}

internal sealed record ProbeSessionRecord(
    ProbePhase Phase,
    bool DryRun,
    bool PlaceholderFixtures,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    SessionIdentity Identity,
    IReadOnlyList<ProbeStepRecord> Steps,
    string? StoppedAt,
    string? StopReason)
{
    /// <summary>Probe numbers the operator excluded, ascending. Their steps are recorded as excluded.</summary>
    public IReadOnlyList<int> ExcludedProbes { get; init; } = [];

    /// <summary>The interaction-mode settings when the plan changes SA's interaction mode; otherwise null.</summary>
    public InteractionModeSettings? InteractionMode { get; init; }

    /// <summary>Every step that was not excluded was observed as planned.</summary>
    public bool Completed => StopReason is null && !DryRun &&
        Steps.All(static step => step.Classification is StepClassification.Observed or StepClassification.Excluded);
}

/// <summary>
/// Runs a plan, stopping at the first unexpected disposition. An outcome whose
/// completion is unknown is never accepted and always stops the session as
/// do-not-replay.
/// </summary>
internal static class ProbeSession
{
    public const string UnknownOutcomeStop = "outcome-unknown-do-not-replay";
    public const string UnexpectedDispositionStop = "unexpected-disposition";
    public const string RequirementStop = "requirement-not-satisfied";
    public const string DestructiveRefusalStop = "destructive-target-not-created-by-harness";
    public const string CheckpointStop = "operator-checkpoint-not-confirmed";

    /// <summary>Records the plan without starting any transport.</summary>
    public static ProbeSessionRecord DryRun(ProbePlan plan, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(clock);
        var now = clock.GetUtcNow();
        return new ProbeSessionRecord(plan.Phase, DryRun: true, plan.PlaceholderFixtures, now, now, SessionIdentity.None,
            [.. plan.CatalogSteps.Select(step => plan.IsExcluded(step) ? Excluded(step) : NotRun(step))], StoppedAt: null, StopReason: null)
        {
            ExcludedProbes = plan.ExcludedProbes,
            InteractionMode = plan.SetsInteractionMode ? plan.InteractionMode : null
        };
    }

    public static async Task<ProbeSessionRecord> RunAsync(
        ProbePlan plan,
        IProbeTransport transport,
        TimeProvider clock,
        SessionIdentity operatorIdentity,
        IOperatorConsole console,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(operatorIdentity);
        ArgumentNullException.ThrowIfNull(console);
        if (transport.Phase != plan.Phase)
        {
            throw new InvalidOperationException("The transport serves a different phase than the plan.");
        }

        var started = clock.GetUtcNow();
        var records = new List<ProbeStepRecord>(plan.CatalogSteps.Count);
        SessionIdentity identity;
        try
        {
            identity = Merge(await transport.StartAsync(plan, cancellationToken).ConfigureAwait(false), operatorIdentity);
        }
        catch (ProbeRefusedException refusal)
        {
            records.AddRange(plan.CatalogSteps.Select(step => plan.IsExcluded(step) ? Excluded(step) : NotRun(step)));
            return new ProbeSessionRecord(plan.Phase, DryRun: false, plan.PlaceholderFixtures, started, clock.GetUtcNow(),
                operatorIdentity, records, StoppedAt: null, refusal.DiagnosticCode)
            {
                ExcludedProbes = plan.ExcludedProbes,
                InteractionMode = plan.SetsInteractionMode ? plan.InteractionMode : null
            };
        }

        var history = new Dictionary<string, ProbeOutcome>(StringComparer.Ordinal);
        var created = new HashSet<string>(StringComparer.Ordinal);
        string? stoppedAt = null;
        string? stopReason = null;
        var ordinal = 0;
        foreach (var step in plan.CatalogSteps)
        {
            // An operator-excluded step is recorded in its catalog position and never sent.
            if (plan.IsExcluded(step))
            {
                records.Add(Excluded(step));
                continue;
            }

            if (stopReason is not null)
            {
                records.Add(NotRun(step));
                continue;
            }

            ordinal++;

            // Defence in depth: the plan already proves this, and the session
            // re-checks that the disposable target was actually created now.
            if (step.DestructiveTarget is not null && !created.Contains(step.DestructiveTarget))
            {
                records.Add(new ProbeStepRecord(step, ProbeOutcome.Refused("destructive-target-not-created"),
                    StepClassification.Refused, [], null));
                (stoppedAt, stopReason) = (step.Id, DestructiveRefusalStop);
                continue;
            }

            // The step is sent only after the operator confirms SA's screen.
            var operatorObservations = new List<OperatorObservation>();
            if (step.Checkpoints.HasFlag(OperatorCheckpoint.Before))
            {
                var before = await console.CheckpointAsync(step, OperatorCheckpoint.Before, cancellationToken).ConfigureAwait(false);
                if (before is null)
                {
                    records.Add(new ProbeStepRecord(step, ProbeOutcome.Refused("operator-checkpoint-not-confirmed"),
                        StepClassification.Refused, [], null));
                    (stoppedAt, stopReason) = (step.Id, CheckpointStop);
                    continue;
                }

                operatorObservations.Add(new OperatorObservation(OperatorCheckpoint.Before, before));
            }

            console.Dispatching(step, ordinal, plan.Steps.Count);
            var dispatched = clock.GetUtcNow();
            var outcome = await transport.ExecuteAsync(step, cancellationToken).ConfigureAwait(false);
            var elapsed = (long)(clock.GetUtcNow() - dispatched).TotalMilliseconds;
            console.Completed(step, ordinal, plan.Steps.Count, outcome, elapsed);
            history[step.Id] = outcome;
            var accepted = step.Acceptable.Accepts(outcome);
            bool? requirement = step.Requirement is null || !accepted ? null : step.Requirement.IsSatisfied(history, outcome);
            var hypotheses = step.Hypotheses
                .Select(hypothesis => new HypothesisResult(hypothesis.Label, hypothesis.ObservationKey, hypothesis.ExpectedValue,
                    outcome.Observations.TryGetValue(hypothesis.ObservationKey, out var observed) &&
                    string.Equals(observed, hypothesis.ExpectedValue, StringComparison.Ordinal)))
                .ToList();

            // Whatever the outcome, the operator records and dismisses any dialog
            // before anything else is sent.
            var unanswered = false;
            if (step.Checkpoints.HasFlag(OperatorCheckpoint.After))
            {
                var after = await console.CheckpointAsync(step, OperatorCheckpoint.After, cancellationToken).ConfigureAwait(false);
                if (after is null)
                {
                    unanswered = true;
                }
                else
                {
                    operatorObservations.Add(new OperatorObservation(OperatorCheckpoint.After, after));
                }
            }

            if (accepted && requirement != false)
            {
                records.Add(new ProbeStepRecord(step, outcome, StepClassification.Observed, hypotheses, requirement)
                {
                    OperatorObservations = operatorObservations,
                    ElapsedMilliseconds = elapsed
                });
                if (step.CreatesFixture is not null && outcome.Kind == ProbeOutcomeKind.Succeeded)
                {
                    created.Add(step.CreatesFixture);
                }

                // The step's own result stands; nothing later is sent without the operator.
                if (unanswered)
                {
                    (stoppedAt, stopReason) = (step.Id, CheckpointStop);
                }

                continue;
            }

            records.Add(new ProbeStepRecord(step, outcome, StepClassification.Unexpected, hypotheses, requirement)
            {
                OperatorObservations = operatorObservations,
                ElapsedMilliseconds = elapsed
            });
            stoppedAt = step.Id;
            // ExecuteStep false, a missing MP result, or any other unknown completion:
            // the step is never replayed and no later step runs.
            stopReason = outcome.CompletionUnknown
                ? UnknownOutcomeStop
                : accepted ? RequirementStop : UnexpectedDispositionStop;
        }

        return new ProbeSessionRecord(plan.Phase, DryRun: false, plan.PlaceholderFixtures, started, clock.GetUtcNow(),
            identity, records, stoppedAt, stopReason)
        {
            ExcludedProbes = plan.ExcludedProbes,
            InteractionMode = plan.SetsInteractionMode ? plan.InteractionMode : null
        };
    }

    private static ProbeStepRecord NotRun(ProbeStep step) => new(step, null, StepClassification.NotRun, [], null);

    private static ProbeStepRecord Excluded(ProbeStep step) => new(step, null, StepClassification.Excluded, [], null);

    private static SessionIdentity Merge(SessionIdentity runtime, SessionIdentity operatorIdentity) => runtime with
    {
        ConnectedSpatialAnalyzerAttestedVersion = operatorIdentity.ConnectedSpatialAnalyzerAttestedVersion,
        ConnectedSpatialAnalyzerAttestationReference = operatorIdentity.ConnectedSpatialAnalyzerAttestationReference,
        ActivatedSdkAttestationReference = operatorIdentity.ActivatedSdkAttestationReference,
        HarnessRevision = operatorIdentity.HarnessRevision
    };
}
