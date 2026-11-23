using System.Text.Json;

namespace Briosa.LicensedProbes;

/// <summary>
/// The rerun rule of F1 and D12 (#402), enforced before a rerun is planned: given the JSON
/// record of the stopped run (<c>--rerun-of</c>), a rerun may name only probes of that run none
/// of whose steps were sent. When a shared guard or fixture-setup step had unknown completion,
/// every possible rerun would send that step again, so none is allowed. The gate fails closed:
/// it accepts only the record of a stopped, licensed, first run of this phase and target, made
/// by the same harness revision, whose steps are exactly the recorded plan's steps with
/// consistent classifications and outcomes. It checks the record's consistency; it does not
/// defend against a record edited on purpose. The record's values are never echoed: every
/// refusal is one fixed message that names no path.
/// </summary>
internal static class RerunGate
{
    public const string Option = "--rerun-of";

    public const string Unreadable = "The stopped run's record could not be read.";
    public const string WrongRecord = "--rerun-of must name the unedited JSON record of a stopped licensed run of this phase and target.";
    public const string RevisionMismatch =
        "--rerun-of needs --harness-revision, and the stopped run's record must name the same harness revision.";
    public const string AlreadyRerun = "The stopped run was itself a rerun: no further rerun (D12).";
    public const string NothingToRerun = "The stopped run completed; there is nothing to rerun.";
    public const string SharedStepUnknown =
        "A shared guard or fixture-setup step had unknown completion in the stopped run: record that run unfinished. It is not rerun in session 4 (F1).";
    public const string NoneRemain = "No probe of the stopped run is wholly unstarted: end the run (F1).";
    public const string SelectionRequired = "--rerun-of needs --probe naming only probes of the stopped run none of whose steps were sent (F1).";
    public const string StartedSelected = "--probe may name only probes of the stopped run none of whose steps were sent (F1).";

    private static readonly HashSet<string> SentClassifications = new(StringComparer.Ordinal) { "observed", "unexpected", "refused" };
    private static readonly HashSet<string> UnsentClassifications = new(StringComparer.Ordinal) { "not-run", "excluded", "fixture-missing" };

    /// <summary>The probes a rerun may still name; throws <see cref="ProbeUsageException"/> when none may run.</summary>
    public static IReadOnlySet<string> WhollyUnstarted(string recordJson, ProbePhase phase, string? harnessRevision)
    {
        ArgumentNullException.ThrowIfNull(recordJson);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(recordJson);
        }
        catch (JsonException)
        {
            throw new ProbeUsageException(Unreadable);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !Is(root, "spatial_analyzer_target", ProbeTarget.SpatialAnalyzerTarget) ||
                !Is(root, "phase", ProbePlan.PhaseName(phase)) ||
                !Is(root, "mode", "licensed") ||
                !root.TryGetProperty("schema_version", out var schema) || schema.ValueKind != JsonValueKind.Number ||
                !schema.TryGetInt32(out var version) || version != ProbeRecorder.SchemaVersion ||
                !root.TryGetProperty("completed", out var completed) || completed.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("rerun", out var rerun) || rerun.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            {
                throw new ProbeUsageException(WrongRecord);
            }

            // A completed run has nothing to rerun; a stopped one names why it stopped.
            if (completed.ValueKind == JsonValueKind.True)
            {
                throw new ProbeUsageException(NothingToRerun);
            }

            if (Text(root, "stop_reason") is null)
            {
                throw new ProbeUsageException(WrongRecord);
            }

            if (rerun.ValueKind == JsonValueKind.True)
            {
                throw new ProbeUsageException(AlreadyRerun);
            }

            // The same harness build: its plan and classification are the ones that made the record.
            if (harnessRevision is null ||
                !root.TryGetProperty("identity", out var identity) || identity.ValueKind != JsonValueKind.Object ||
                !string.Equals(Text(identity, "harness_revision"), harnessRevision, StringComparison.Ordinal))
            {
                throw new ProbeUsageException(RevisionMismatch);
            }

            var plan = RecordedPlan(root, phase);
            var expected = plan.CatalogSteps;
            var recorded = steps.EnumerateArray().ToList();
            if (recorded.Count != expected.Count)
            {
                throw new ProbeUsageException(WrongRecord);
            }

            var planned = new HashSet<string>(StringComparer.Ordinal);
            var started = new HashSet<string>(StringComparer.Ordinal);
            var stopped = false;
            for (var index = 0; index < expected.Count; index++)
            {
                var step = recorded[index];
                var plannedStep = expected[index];
                if (step.ValueKind != JsonValueKind.Object ||
                    !Is(step, "id", plannedStep.Id) ||
                    !Is(step, "kind", ProbePlan.KindName(plannedStep.Kind)) ||
                    !Is(step, "probe", plannedStep.Probe) ||
                    Text(step, "classification") is not { } classification)
                {
                    throw new ProbeUsageException(WrongRecord);
                }

                var sent = Consistent(step, plannedStep, plan, classification);

                // A stopped run sends nothing after its first step that was not run.
                if (classification == "not-run")
                {
                    stopped = true;
                }
                else if (stopped && sent)
                {
                    throw new ProbeUsageException(WrongRecord);
                }

                if (plannedStep.Kind is ProbeStepKind.Guard or ProbeStepKind.Setup)
                {
                    if (sent && CompletionUnknown(step))
                    {
                        throw new ProbeUsageException(SharedStepUnknown);
                    }

                    continue;
                }

                foreach (var number in ProbePlan.ExpandProbe(plannedStep.Probe))
                {
                    planned.Add(number);
                    if (sent)
                    {
                        started.Add(number);
                    }
                }
            }

            planned.ExceptWith(started);
            if (planned.Count == 0)
            {
                throw new ProbeUsageException(NoneRemain);
            }

            return planned;
        }
    }

    /// <summary>Refuses a rerun selection that is empty or names a probe the stopped run had started.</summary>
    public static void Check(string recordJson, ProbePhase phase, IReadOnlyList<int> selectedProbes, string? harnessRevision)
    {
        ArgumentNullException.ThrowIfNull(selectedProbes);
        var allowed = WhollyUnstarted(recordJson, phase, harnessRevision);
        if (selectedProbes.Count == 0)
        {
            throw new ProbeUsageException(SelectionRequired);
        }

        if (selectedProbes.Any(probe => !allowed.Contains(probe.ToString(System.Globalization.CultureInfo.InvariantCulture))))
        {
            throw new ProbeUsageException(StartedSelected);
        }
    }

    // The plan the record was made from, rebuilt from its own selection and exclusions. Its
    // steps do not depend on which manual fixtures were present.
    private static ProbePlan RecordedPlan(JsonElement root, ProbePhase phase)
    {
        var selected = Probes(root, "selection", nullable: true);
        var excluded = Probes(root, "exclusions", nullable: false);
        try
        {
            return ProbePlan.Create(phase, FixtureManifest.Placeholder, excluded, InteractionModeSettings.Placeholder, selected);
        }
        catch (Exception exception) when (exception is ProbeUsageException or InvalidDataException)
        {
            throw new ProbeUsageException(WrongRecord);
        }
    }

    private static List<int> Probes(JsonElement root, string name, bool nullable)
    {
        if (!root.TryGetProperty(name, out var section) || (section.ValueKind == JsonValueKind.Null && !nullable))
        {
            throw new ProbeUsageException(WrongRecord);
        }

        if (section.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (section.ValueKind != JsonValueKind.Object || !section.TryGetProperty("probes", out var probes) || probes.ValueKind != JsonValueKind.Array)
        {
            throw new ProbeUsageException(WrongRecord);
        }

        var numbers = new List<int>();
        foreach (var probe in probes.EnumerateArray())
        {
            if (probe.ValueKind != JsonValueKind.Number || !probe.TryGetInt32(out var number) || numbers.Contains(number))
            {
                throw new ProbeUsageException(WrongRecord);
            }

            numbers.Add(number);
        }

        return numbers;
    }

    // Whether the step was sent, after checking that its classification and outcome agree:
    // a sent step has an outcome of a known kind (a harness refusal for a refused step, and
    // never one for an observed or unexpected step); an unsent one has none; an excluded step
    // is one the plan excludes; and only a step with a manual fixture can have been missing it.
    private static bool Consistent(JsonElement step, ProbeStep plannedStep, ProbePlan plan, string classification)
    {
        var hasOutcome = step.TryGetProperty("outcome", out var outcome);
        if (!hasOutcome)
        {
            throw new ProbeUsageException(WrongRecord);
        }

        if (UnsentClassifications.Contains(classification))
        {
            if (outcome.ValueKind != JsonValueKind.Null ||
                (classification == "excluded") != plan.IsExcluded(plannedStep) ||
                (classification == "fixture-missing" && plannedStep.RequiredFixture is null))
            {
                throw new ProbeUsageException(WrongRecord);
            }

            return false;
        }

        if (!SentClassifications.Contains(classification) || plan.IsExcluded(plannedStep) ||
            outcome.ValueKind != JsonValueKind.Object ||
            Text(outcome, "kind") is not { } kindText || !Enum.TryParse<ProbeOutcomeKind>(kindText, ignoreCase: false, out var kind) ||
            !Enum.IsDefined(kind))
        {
            throw new ProbeUsageException(WrongRecord);
        }

        // A harness refusal was never sent: the session's own (refused), or a transport's that
        // a step met after the license signal (unexpected); an observed step is never one.
        if (kind == ProbeOutcomeKind.RefusedByHarness)
        {
            return classification == "observed" ? throw new ProbeUsageException(WrongRecord) : false;
        }

        return classification == "refused" ? throw new ProbeUsageException(WrongRecord) : true;
    }

    // Unknown completion as the session classifies it: the outcome kind, or its disposition.
    private static bool CompletionUnknown(JsonElement step)
    {
        if (!step.TryGetProperty("outcome", out var outcome) || outcome.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return (Text(outcome, "kind") is { } kind && Enum.TryParse<ProbeOutcomeKind>(kind, ignoreCase: false, out var parsed) && parsed.IsCompletionUnknown()) ||
            string.Equals(Text(outcome, "execution_disposition"), ProbeOutcome.StartedOutcomeUnknown, StringComparison.Ordinal);
    }

    private static bool Is(JsonElement element, string name, string expected) =>
        string.Equals(Text(element, name), expected, StringComparison.Ordinal);

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
