using System.Text.Json;

namespace Briosa.LicensedProbes;

/// <summary>
/// The rerun rule of F1 and D17 (#402), enforced before a rerun is planned: given the JSON
/// record of the stopped run (<c>--rerun-of</c>), a rerun may name only probes of that run none
/// of whose steps were sent. When a shared guard or fixture-setup step had unknown completion,
/// every possible rerun would send that step again, so none is allowed. The record's values are
/// never echoed: every refusal is one fixed message.
/// </summary>
internal static class RerunGate
{
    public const string Option = "--rerun-of";

    public const string Unreadable = "The stopped run's record could not be read.";
    public const string WrongRecord = "--rerun-of must name the JSON record of a licensed run of this phase and target.";
    public const string NothingToRerun = "The stopped run completed; there is nothing to rerun.";
    public const string SharedStepUnknown =
        "A shared guard or fixture-setup step had unknown completion in the stopped run: record that run unfinished. It is not rerun in session 4 (F1).";
    public const string NoneRemain = "No probe of the stopped run is wholly unstarted: end the run (F1).";
    public const string SelectionRequired = "--rerun-of needs --probe naming only probes of the stopped run none of whose steps were sent (F1).";
    public const string StartedSelected = "--probe may name only probes of the stopped run none of whose steps were sent (F1).";

    /// <summary>The probes a rerun may still name; throws <see cref="ProbeUsageException"/> when none may run.</summary>
    public static IReadOnlySet<string> WhollyUnstarted(string recordJson, ProbePhase phase)
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
                !root.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            {
                throw new ProbeUsageException(WrongRecord);
            }

            if (root.TryGetProperty("completed", out var completed) && completed.ValueKind == JsonValueKind.True)
            {
                throw new ProbeUsageException(NothingToRerun);
            }

            var planned = new HashSet<string>(StringComparer.Ordinal);
            var started = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in steps.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object || Text(step, "kind") is not { } kind || Text(step, "classification") is not { } classification)
                {
                    throw new ProbeUsageException(WrongRecord);
                }

                // Observed and unexpected steps reached the transport; refused, excluded,
                // fixture-missing and not-run steps never did.
                var sent = classification is "observed" or "unexpected";
                if (kind is "guard" or "setup")
                {
                    if (sent && CompletionUnknown(step))
                    {
                        throw new ProbeUsageException(SharedStepUnknown);
                    }

                    continue;
                }

                if (Text(step, "probe") is not { } probe)
                {
                    throw new ProbeUsageException(WrongRecord);
                }

                foreach (var number in ProbePlan.ExpandProbe(probe))
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
    public static void Check(string recordJson, ProbePhase phase, IReadOnlyList<int> selectedProbes)
    {
        ArgumentNullException.ThrowIfNull(selectedProbes);
        var allowed = WhollyUnstarted(recordJson, phase);
        if (selectedProbes.Count == 0)
        {
            throw new ProbeUsageException(SelectionRequired);
        }

        if (selectedProbes.Any(probe => !allowed.Contains(probe.ToString(System.Globalization.CultureInfo.InvariantCulture))))
        {
            throw new ProbeUsageException(StartedSelected);
        }
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
