using System.Globalization;
using System.Text;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>A validated, ordered, immutable list of steps for one phase.</summary>
internal sealed class ProbePlan
{
    private ProbePlan(ProbePhase phase, IReadOnlyList<ProbeStep> steps, bool placeholderFixtures)
    {
        Phase = phase;
        Steps = steps;
        PlaceholderFixtures = placeholderFixtures;
    }

    public ProbePhase Phase { get; }

    public IReadOnlyList<ProbeStep> Steps { get; }

    public bool PlaceholderFixtures { get; }

    public IReadOnlyList<string> FullyQualifiedMethods =>
        [.. Steps.Select(static step => step.Operation.FullyQualifiedMethod).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    public static ProbePlan Create(ProbePhase phase, FixtureManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        manifest.Validate();
        var steps = ProbeCatalog.Create(phase, manifest);
        Validate(phase, steps);
        return new ProbePlan(phase, steps, manifest.IsPlaceholder);
    }

    /// <summary>Enforces the session safety invariants before anything can connect.</summary>
    public static void Validate(ProbePhase phase, IReadOnlyList<ProbeStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        Require(steps.Count > 0, "A probe plan must contain steps.");
        Require(steps.Select(static step => step.Id).Distinct(StringComparer.Ordinal).Count() == steps.Count,
            "Probe step identifiers must be unique.");
        var created = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            Require(step.Phase == phase, $"Step '{step.Id}' belongs to another phase.");
            Require(step.Acceptable != ProbeOutcomes.None, $"Step '{step.Id}' accepts no outcome.");

            // Only determinate outcomes can be accepted; completion-unknown kinds have no flag.
            Require((step.Acceptable & ~ProbeOutcomes.AnyDeterminateSdkOutcome) == ProbeOutcomes.None,
                $"Step '{step.Id}' accepts an outcome outside the determinate set.");

            // Building the command runs the shipped mapper and applies the variant;
            // an invalid request or variant fails here, never against SpatialAnalyzer.
            var command = step.Command;
            Require(command.StepName.Length > 0, $"Step '{step.Id}' has no MP step.");

            if (phase == ProbePhase.PublicApi)
            {
                Require(step.Variant.Kind == VariantKind.Shipped,
                    $"Step '{step.Id}' varies the shipped sequence; only the worker phase may do that.");
            }
            else
            {
                Require(!step.Operation.IsDestructive && step.DestructiveTarget is null,
                    $"Step '{step.Id}' is destructive; the worker phase never runs destructive operations.");
                Require(!step.RequiresOperator, $"Step '{step.Id}' needs an operator; the worker phase is unattended.");
            }

            if (step.Kind != ProbeStepKind.Probe)
            {
                Require(step.Variant.Kind == VariantKind.Shipped, $"Step '{step.Id}' varies a non-probe sequence.");
            }

            // A variant is the question itself: every determinate SDK outcome is an
            // answer. An outcome whose completion is unknown is never an answer.
            if (step.Variant.Kind != VariantKind.Shipped)
            {
                Require(step.Acceptable == ProbeOutcomes.AnyDeterminateSdkOutcome,
                    $"Step '{step.Id}' must record every determinate outcome of its variant.");
            }

            if (step.Operation.IsDestructive || step.DestructiveTarget is not null)
            {
                Require(step.Operation.IsDestructive && step.DestructiveTarget is not null && step.Kind == ProbeStepKind.Probe,
                    $"Step '{step.Id}' must name the disposable target it may destroy.");
                Require(created.Contains(step.DestructiveTarget!),
                    $"Step '{step.Id}' targets an object no earlier step creates.");
                Require(TargetsOnly(step), $"Step '{step.Id}' could affect an object other than its disposable target.");
            }

            if (step.CreatesFixture is not null)
            {
                Require(step.Kind == ProbeStepKind.Setup, $"Step '{step.Id}' creates a fixture outside setup.");
                Require(created.Add(step.CreatesFixture), $"Fixture '{step.CreatesFixture}' is created twice.");
            }
        }
    }

    // The only reviewed destructive operation deletes cloud points; its request
    // may name exactly one cloud, the harness-created disposable target.
    private static bool TargetsOnly(ProbeStep step) =>
        step.Request is Api.DeleteCloudPointsByXYZRangeRequest delete &&
        delete.CloudNames.Count == 1 &&
        string.Equals(delete.CloudNames[0].CollectionName, FixtureNames.Collection, StringComparison.Ordinal) &&
        string.Equals("cloud:" + delete.CloudNames[0].ObjectName, step.DestructiveTarget, StringComparison.Ordinal);

    /// <summary>Renders the planned SDK sequences without connecting to anything.</summary>
    public string RenderDryRun()
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture,
            $"Briosa licensed probe plan ({ProbeTarget.Issue}) for SpatialAnalyzer {ProbeTarget.SpatialAnalyzerTarget}\n");
        text.Append(CultureInfo.InvariantCulture, $"Phase: {PhaseName(Phase)}. DRY RUN: nothing was started or connected.\n");
        text.Append(PlaceholderFixtures
            ? "Fixture manifest: built-in placeholder. Manual fixture values are never printed.\n"
            : "Fixture manifest: supplied and validated. Manual fixture values are never printed.\n");
        text.Append(CultureInfo.InvariantCulture, $"Steps: {Steps.Count}. Probes: {string.Join(", ", ProbesCovered())}.\n\n");
        for (var index = 0; index < Steps.Count; index++)
        {
            var step = Steps[index];
            text.Append(CultureInfo.InvariantCulture,
                $"[{index + 1:D3}] {step.Id} ({KindName(step.Kind)}, probe {step.Probe}) {step.Purpose}\n");
            text.Append(CultureInfo.InvariantCulture, $"      RPC {step.Operation.FullyQualifiedMethod}");
            text.Append(Phase == ProbePhase.Worker ? " sequence sent directly to the worker\n" : "\n");
            foreach (var line in DescribeSequence(step))
            {
                text.Append("      ").Append(line).Append('\n');
            }

            text.Append(CultureInfo.InvariantCulture, $"      accept: {AcceptableName(step.Acceptable)}\n");
            if (step.DestructiveTarget is not null)
            {
                text.Append(CultureInfo.InvariantCulture,
                    $"      DESTRUCTIVE: only the harness-created {step.DestructiveTarget}; refused unless created this session\n");
            }

            if (step.RequiresOperator)
            {
                text.Append("      OPERATOR: SpatialAnalyzer prompts for a selection during this step\n");
            }

            if (step.Requirement is not null)
            {
                text.Append(CultureInfo.InvariantCulture, $"      requires: {step.Requirement.Description}\n");
            }

            foreach (var hypothesis in step.Hypotheses)
            {
                text.Append(CultureInfo.InvariantCulture,
                    $"      hypothesis: {hypothesis.ObservationKey} = {hypothesis.ExpectedValue} means {hypothesis.Label}\n");
            }
        }

        return text.ToString();
    }

    /// <summary>The sequence lines shown in dry-run output and evidence. Labels only, never values.</summary>
    public static IReadOnlyList<string> DescribeSequence(ProbeStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        var shipped = step.ShippedCommand;
        var command = step.Command;
        var lines = new List<string>();
        lines.Add(step.Variant.Kind == VariantKind.ReplaceStepText
            ? $"SetStep \"{command.StepName}\" (shipped text: {Flag(string.Equals(command.StepName, shipped.StepName, StringComparison.Ordinal))})"
            : $"SetStep \"{command.StepName}\"");
        foreach (var argument in shipped.InputArguments)
        {
            var treatment = step.Variant.TreatmentOf(argument.Name);
            lines.Add(treatment switch
            {
                "omitted" => $"{Binding(argument)} \"{argument.Name}\" OMITTED (setter not called)",
                "blank" => $"{Binding(argument)} \"{argument.Name}\" = BLANK",
                "recased" => $"{Binding(argument)} \"{step.Variant.Replacement}\" = value (recased from \"{argument.Name}\")",
                _ => $"{Binding(argument)} \"{argument.Name}\" = value"
            });
        }

        lines.Add("ExecuteStep; GetMPStepResult (MP code 2 is success)");
        foreach (var output in command.OutputArguments)
        {
            lines.Add($"{output.SdkBinding ?? "getter"} \"{output.Name}\" (only after MP code 2)");
        }

        return lines;
    }

    public IReadOnlyList<string> ProbesCovered() =>
        [.. Steps.Where(static step => step.Kind is ProbeStepKind.Probe or ProbeStepKind.Check)
            .SelectMany(static step => ExpandProbe(step.Probe))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static probe => int.Parse(probe, CultureInfo.InvariantCulture))];

    internal static IEnumerable<string> ExpandProbe(string probe)
    {
        var parts = probe.Split('-');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var first) &&
            int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var last) && last >= first)
        {
            return Enumerable.Range(first, last - first + 1).Select(static number => number.ToString(CultureInfo.InvariantCulture));
        }

        return int.TryParse(probe, NumberStyles.None, CultureInfo.InvariantCulture, out _) ? [probe] : [];
    }

    public static string PhaseName(ProbePhase phase) => phase == ProbePhase.PublicApi ? "public-api" : "worker";

    public static string KindName(ProbeStepKind kind) => kind switch
    {
        ProbeStepKind.Guard => "guard",
        ProbeStepKind.Setup => "setup",
        ProbeStepKind.Check => "check",
        _ => "probe"
    };

    public static string AcceptableName(ProbeOutcomes outcomes) =>
        outcomes == ProbeOutcomes.AnyDeterminateSdkOutcome
            ? "any determinate SDK outcome (Succeeded, MpFailed, ArgumentRejected, or OutputRetrievalFailed)"
            : string.Join(" or ", Enum.GetValues<ProbeOutcomes>()
                .Where(flag => flag is not ProbeOutcomes.None and not ProbeOutcomes.AnyDeterminateSdkOutcome && outcomes.HasFlag(flag))
                .Select(static flag => flag.ToString()));

    private static string Binding(WorkerMpInputArgument argument) => argument.SdkBinding ?? "setter";

    private static string Flag(bool value) => value ? "yes" : "no";
}
