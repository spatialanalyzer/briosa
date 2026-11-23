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

    /// <summary>
    /// Verifies identity and readiness. Throws <see cref="ProbeRefusedException"/> to refuse. The
    /// transport consults <paramref name="licenseAbort"/> before it starts, sends or terminates
    /// anything, including in disposal, for the rest of the run.
    /// </summary>
    Task<SessionIdentity> StartAsync(ProbePlan plan, LicenseAbortSignal licenseAbort, CancellationToken cancellationToken);

    /// <summary>
    /// Sends one step once. Transport failures become an indeterminate outcome.
    /// After an outcome whose completion is unknown the session sends no further
    /// step, and a transport that owns a worker terminates it.
    /// </summary>
    Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken);

    /// <summary>
    /// A license prompt is open (#402): from now on the transport sends nothing, not even a
    /// graceful stop, and terminates nothing. Disposal then only releases the harness's own handles.
    /// </summary>
    void Abandon();
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

    /// <summary>
    /// The operator's license-abort signal (#402), raised by the signal file or by a checkpoint
    /// answer. Distinct from Ctrl+C, which cancels and lets the run clean up: from the harness's
    /// next check of it, nothing more is sent, started or terminated, not even a graceful stop.
    /// </summary>
    LicenseAbortSignal LicenseAbort { get; }
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
    Excluded,

    // Its manual fixture is missing from the manifest (#402); never sent. It is not
    // run, and never a pass.
    FixtureMissing
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

    /// <summary>Probe numbers the operator selected with --probe, ascending; empty for the default plan.</summary>
    public IReadOnlyList<int> SelectedProbes { get; init; } = [];

    /// <summary>The server interaction-mode setting the public plan needed (#313); null in the worker phase.</summary>
    public string? ServerInteractionModeSetting { get; init; }

    /// <summary>
    /// The stop reason a license prompt took precedence over (#402), for example a cancellation
    /// or an unknown outcome that was still being cleaned up when the signal was raised.
    /// </summary>
    public string? SupersededStopReason { get; init; }

    /// <summary>
    /// Every step that could be sent was observed as planned. Excluded and fixture-missing
    /// steps were never sent; the record lists them.
    /// </summary>
    public bool Completed => StopReason is null && !DryRun &&
        Steps.All(static step => step.Classification is StepClassification.Observed or StepClassification.Excluded or
            StepClassification.FixtureMissing);

    /// <summary>The manifest keys whose absence kept steps from running, in step order.</summary>
    public IReadOnlyList<string> MissingFixtures =>
        [.. Steps.Where(static step => step.Classification == StepClassification.FixtureMissing)
            .Select(static step => step.Step.RequiredFixture!).Distinct(StringComparer.Ordinal)];

    /// <summary>The run ended, as planned, in its stall step's stall: that step's completion is unknown.</summary>
    public bool EndedInPlannedStall => Steps.LastOrDefault(static step =>
        step.Classification is not (StepClassification.Excluded or StepClassification.FixtureMissing)) is
    {
        Classification: StepClassification.Observed, Step.ExpectedStall: not null
    };
}

/// <summary>
/// Runs a plan, stopping at the first unexpected disposition. An outcome whose
/// completion is unknown is never accepted, except the planned stall of a stall step,
/// which is always the plan's last step; nothing is ever sent after such an outcome,
/// and it is never replayed.
/// </summary>
internal static class ProbeSession
{
    public const string UnknownOutcomeStop = "outcome-unknown-do-not-replay";
    public const string UnexpectedDispositionStop = "unexpected-disposition";
    public const string RequirementStop = "requirement-not-satisfied";
    public const string DestructiveRefusalStop = "destructive-target-not-created-by-harness";
    public const string CheckpointStop = "operator-checkpoint-not-confirmed";

    // The operator answered license-prompt-abort: a license prompt is open, and nothing more
    // is sent (#402). The prompt is never copied or dismissed by the run.
    public const string LicensePromptStop = "license-prompt-operator-abort";

    /// <summary>Records the plan without starting any transport.</summary>
    public static ProbeSessionRecord DryRun(ProbePlan plan, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(clock);
        var now = clock.GetUtcNow();
        return Describe(new ProbeSessionRecord(plan.Phase, DryRun: true, plan.PlaceholderFixtures, now, now, SessionIdentity.None,
            [.. plan.CatalogSteps.Select(step => Unsent(plan, step))], StoppedAt: null, StopReason: null), plan);
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
        var licenseAbort = console.LicenseAbort;

        // A license prompt already signalled: the transport is never started.
        if (licenseAbort.Raised)
        {
            transport.Abandon();
            records.AddRange(plan.CatalogSteps.Select(step => Unsent(plan, step)));
            return Describe(new ProbeSessionRecord(plan.Phase, DryRun: false, plan.PlaceholderFixtures, started, clock.GetUtcNow(),
                operatorIdentity, records, StoppedAt: null, LicensePromptStop), plan);
        }

        SessionIdentity identity;
        try
        {
            identity = Merge(await transport.StartAsync(plan, licenseAbort, cancellationToken).ConfigureAwait(false), operatorIdentity);
        }
        catch (ProbeRefusedException refusal)
        {
            records.AddRange(plan.CatalogSteps.Select(step => Unsent(plan, step)));
            return WithLicensePrecedence(Describe(new ProbeSessionRecord(plan.Phase, DryRun: false, plan.PlaceholderFixtures, started,
                clock.GetUtcNow(), operatorIdentity, records, StoppedAt: null, refusal.DiagnosticCode), plan), licenseAbort, transport);
        }

        var history = new Dictionary<string, ProbeOutcome>(StringComparer.Ordinal);
        var created = new HashSet<string>(StringComparer.Ordinal);
        string? stoppedAt = null;
        string? stopReason = null;
        string? stalledAt = null;
        var ordinal = 0;
        foreach (var step in plan.CatalogSteps)
        {
            // An operator-excluded step, or one whose manual fixture is missing, is recorded
            // in its catalog position and never sent.
            if (plan.IsExcluded(step) || plan.IsFixtureMissing(step))
            {
                records.Add(Unsent(plan, step));
                continue;
            }

            // Defence in depth: validation makes a stall step the plan's last step, and
            // nothing is ever sent after an outcome whose completion is unknown.
            if (stopReason is null && stalledAt is not null)
            {
                (stoppedAt, stopReason) = (stalledAt, UnknownOutcomeStop);
            }

            if (stopReason is not null)
            {
                records.Add(NotRun(step));
                continue;
            }

            // A license prompt signalled between checkpoints: this step and every later one is
            // never sent.
            if (licenseAbort.Raised)
            {
                records.Add(new ProbeStepRecord(step, ProbeOutcome.Refused(LicensePromptStop), StepClassification.Refused, [], null));
                (stoppedAt, stopReason) = (step.Id, LicensePromptStop);
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
                if (before is null || OperatorObservationText.IsLicensePromptAbort(before))
                {
                    var license = OperatorObservationText.IsLicensePromptAbort(before);
                    if (license)
                    {
                        licenseAbort.Raise();
                    }

                    records.Add(new ProbeStepRecord(step, ProbeOutcome.Refused(license ? LicensePromptStop : "operator-checkpoint-not-confirmed"),
                        StepClassification.Refused, [], null)
                    {
                        OperatorObservations = license ? [new OperatorObservation(OperatorCheckpoint.Before, before!)] : []
                    });
                    (stoppedAt, stopReason) = (step.Id, license ? LicensePromptStop : CheckpointStop);
                    continue;
                }

                operatorObservations.Add(new OperatorObservation(OperatorCheckpoint.Before, before));
            }

            if (licenseAbort.Raised)
            {
                records.Add(new ProbeStepRecord(step, ProbeOutcome.Refused(LicensePromptStop), StepClassification.Refused, [], null)
                {
                    OperatorObservations = operatorObservations
                });
                (stoppedAt, stopReason) = (step.Id, LicensePromptStop);
                continue;
            }

            console.Dispatching(step, ordinal, plan.Steps.Count);
            var dispatched = clock.GetUtcNow();
            var outcome = await transport.ExecuteAsync(step, cancellationToken).ConfigureAwait(false);
            var elapsed = (long)(clock.GetUtcNow() - dispatched).TotalMilliseconds;
            console.Completed(step, ordinal, plan.Steps.Count, outcome, elapsed);
            history[step.Id] = outcome;

            // A stall step accepts only the stall it plans to end in (#313); every other
            // step accepts only its determinate outcomes.
            var stalled = step.ExpectedStall?.Matches(outcome) == true;
            var accepted = stalled || step.Acceptable.Accepts(outcome);
            bool? requirement = step.Requirement is null || !accepted ? null : step.Requirement.IsSatisfied(history, outcome);
            var hypotheses = step.Hypotheses
                .Select(hypothesis => new HypothesisResult(hypothesis.Label, hypothesis.ObservationKey, hypothesis.ExpectedValue,
                    string.Equals(outcome.Fact(hypothesis.ObservationKey), hypothesis.ExpectedValue, StringComparison.Ordinal)))
                .ToList();

            // Whatever the outcome, the operator records and dismisses any dialog
            // before anything else is sent.
            // A license prompt signalled while the step ran: its own result stands, and no
            // checkpoint or later step follows.
            var unanswered = false;
            var licensePrompt = licenseAbort.Raised;
            if (!licensePrompt && step.Checkpoints.HasFlag(OperatorCheckpoint.After))
            {
                var after = await console.CheckpointAsync(step, OperatorCheckpoint.After, cancellationToken).ConfigureAwait(false);
                if (after is null)
                {
                    unanswered = true;
                }
                else
                {
                    operatorObservations.Add(new OperatorObservation(OperatorCheckpoint.After, after));
                    licensePrompt = OperatorObservationText.IsLicensePromptAbort(after);
                }

                if (licensePrompt)
                {
                    licenseAbort.Raise();
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

                // The step's own result stands; nothing later is sent without the operator. Its
                // ordinary stop, if any, comes first; a license prompt then takes precedence once,
                // at the end of the run, and keeps that reason as superseded.
                if (unanswered)
                {
                    (stoppedAt, stopReason) = (step.Id, CheckpointStop);
                }
                else if (stalled)
                {
                    stalledAt = step.Id;
                }

                if (licensePrompt && stopReason is null)
                {
                    (stoppedAt, stopReason) = (step.Id, LicensePromptStop);
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
            // A license prompt signalled during the step takes precedence once, at the end of the
            // run, and keeps this reason as superseded.
            stopReason = outcome.CompletionUnknown
                ? UnknownOutcomeStop
                : accepted ? RequirementStop : UnexpectedDispositionStop;
        }

        return WithLicensePrecedence(Describe(new ProbeSessionRecord(plan.Phase, DryRun: false, plan.PlaceholderFixtures, started,
            clock.GetUtcNow(), identity, records, stoppedAt, stopReason), plan), licenseAbort, transport);
    }

    /// <summary>
    /// A license prompt signalled at any time during the run, including while a stop or a
    /// cancellation was being handled, is the run's stop reason, and the transport sends nothing
    /// more, including on disposal. The reason it took precedence over is kept.
    /// </summary>
    internal static ProbeSessionRecord WithLicensePrecedence(ProbeSessionRecord record, LicenseAbortSignal licenseAbort, IProbeTransport? transport)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(licenseAbort);
        if (!licenseAbort.Raised && record.StopReason != LicensePromptStop)
        {
            return record;
        }

        licenseAbort.Raise();
        transport?.Abandon();
        return record.StopReason == LicensePromptStop
            ? record
            : record with { StopReason = LicensePromptStop, SupersededStopReason = record.StopReason };
    }

    // The plan-level facts every record states, whether or not anything ran.
    private static ProbeSessionRecord Describe(ProbeSessionRecord record, ProbePlan plan) => record with
    {
        ExcludedProbes = plan.ExcludedProbes,
        SelectedProbes = plan.SelectedProbes,
        InteractionMode = plan.SetsInteractionMode ? plan.InteractionMode : null,
        ServerInteractionModeSetting = plan.Phase == ProbePhase.PublicApi ? plan.ServerInteractionModeSetting : null
    };

    private static ProbeStepRecord NotRun(ProbeStep step) => new(step, null, StepClassification.NotRun, [], null);

    private static ProbeStepRecord Excluded(ProbeStep step) => new(step, null, StepClassification.Excluded, [], null);

    private static ProbeStepRecord FixtureMissing(ProbeStep step) => new(step, null, StepClassification.FixtureMissing, [], null);

    // A step that is never sent by design, or else not run yet.
    private static ProbeStepRecord Unsent(ProbePlan plan, ProbeStep step) =>
        plan.IsExcluded(step) ? Excluded(step) : plan.IsFixtureMissing(step) ? FixtureMissing(step) : NotRun(step);

    private static SessionIdentity Merge(SessionIdentity runtime, SessionIdentity operatorIdentity) => runtime with
    {
        ConnectedSpatialAnalyzerAttestedVersion = operatorIdentity.ConnectedSpatialAnalyzerAttestedVersion,
        ConnectedSpatialAnalyzerAttestationReference = operatorIdentity.ConnectedSpatialAnalyzerAttestationReference,
        ActivatedSdkAttestationReference = operatorIdentity.ActivatedSdkAttestationReference,
        HarnessRevision = operatorIdentity.HarnessRevision
    };
}
