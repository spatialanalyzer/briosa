using System.Globalization;
using System.Text;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Worker.Control;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// A validated, ordered, immutable list of steps for one phase, less any probes
/// the operator excluded. Guard and fixture setup steps are never excluded.
/// </summary>
internal sealed class ProbePlan
{
    public const string ExclusionReason = "reason not recorded by harness";

    private ProbePlan(
        ProbePhase phase,
        IReadOnlyList<ProbeStep> catalogSteps,
        IReadOnlySet<string> excludedStepIds,
        IReadOnlyList<int> excludedProbes,
        bool placeholderFixtures)
    {
        Phase = phase;
        CatalogSteps = catalogSteps;
        ExcludedStepIds = excludedStepIds;
        ExcludedProbes = excludedProbes;
        Steps = [.. catalogSteps.Where(step => !excludedStepIds.Contains(step.Id))];
        PlaceholderFixtures = placeholderFixtures;
    }

    public ProbePhase Phase { get; }

    /// <summary>The steps that run, in order. Excluded steps are never sent.</summary>
    public IReadOnlyList<ProbeStep> Steps { get; }

    /// <summary>Every catalog step of the phase in order, including excluded ones.</summary>
    public IReadOnlyList<ProbeStep> CatalogSteps { get; }

    public IReadOnlySet<string> ExcludedStepIds { get; }

    /// <summary>Probe numbers the operator excluded, ascending.</summary>
    public IReadOnlyList<int> ExcludedProbes { get; }

    public bool PlaceholderFixtures { get; }

    public bool IsExcluded(ProbeStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return ExcludedStepIds.Contains(step.Id);
    }

    /// <summary>The statement every output carries when probes were excluded.</summary>
    public static string ExclusionStatement(IReadOnlyList<int> excludedProbes)
    {
        ArgumentNullException.ThrowIfNull(excludedProbes);
        var probes = string.Join(", ", excludedProbes.Select(static probe => "#" + probe.ToString(CultureInfo.InvariantCulture)));
        return $"Excluded by operator: {probes} — {ExclusionReason}";
    }

    public IReadOnlyList<string> FullyQualifiedMethods =>
        [.. Steps.Select(static step => step.Operation.FullyQualifiedMethod).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>
    /// The non-default admission profile the public phase needs. The packaged
    /// <c>standard</c> profile excludes the device and operator-guided probes.
    /// </summary>
    public const string ServerAdmissionProfile = "device";

    /// <summary>
    /// The server command-line arguments that admit what the public phase sends:
    /// <see cref="ServerAdmissionProfile"/> plus one per-operation <c>allow</c>
    /// override for each probed operation that profile does not admit with the
    /// probe's own request (for example an operator-guided selector, or a request
    /// that opts into a dialog). No setting can admit an exclusive workflow or a
    /// request that leaves device work running, so a plan that needs one is refused.
    /// </summary>
    public IReadOnlyList<string> ServerAdmissionArguments => CreateServerAdmissionArguments(
        [.. Steps.Select(static step => (step.Operation.FullyQualifiedMethod, (IMessage?)step.Request))]);

    public static ProbePlan Create(ProbePhase phase, FixtureManifest manifest) => Create(phase, manifest, []);

    /// <summary>
    /// Builds the phase plan less the excluded probes. An exclusion that is not in
    /// this phase, splits a step serving several probes, removes a prerequisite of
    /// a remaining step, or leaves no probe is refused with <see cref="ProbeUsageException"/>.
    /// </summary>
    public static ProbePlan Create(ProbePhase phase, FixtureManifest manifest, IReadOnlyCollection<int> excludedProbes)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(excludedProbes);
        manifest.Validate();
        var catalog = ProbeCatalog.Create(phase, manifest);
        Validate(phase, catalog);
        var probes = excludedProbes.Distinct().Order().ToList();
        var excluded = SelectExcludedSteps(phase, catalog, probes);
        var plan = new ProbePlan(phase, catalog, excluded, probes, manifest.IsPlaceholder);

        // The remaining plan must satisfy every invariant the full plan does.
        Validate(phase, plan.Steps);
        if (phase == ProbePhase.PublicApi)
        {
            // Fails closed when a public probe needs an exclusive workflow.
            _ = plan.ServerAdmissionArguments;
        }

        return plan;
    }

    /// <summary>The identifiers of the probe, variant, and check steps an exclusion removes.</summary>
    internal static IReadOnlySet<string> SelectExcludedSteps(
        ProbePhase phase, IReadOnlyList<ProbeStep> catalog, IReadOnlyList<int> excludedProbes)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(excludedProbes);
        var removed = new HashSet<string>(StringComparer.Ordinal);
        if (excludedProbes.Count == 0)
        {
            return removed;
        }

        var excluded = excludedProbes.Select(static probe => probe.ToString(CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal);
        foreach (var probe in excluded)
        {
            if (!catalog.Any(step => IsProbeOrCheck(step) && ExpandProbe(step.Probe).Contains(probe, StringComparer.Ordinal)))
            {
                throw new ProbeUsageException(
                    $"Probe #{probe} has no steps in the {PhaseName(phase)} phase; --exclude-probe accepts only probes of the selected phase.");
            }
        }

        foreach (var step in catalog)
        {
            // Guard and fixture setup steps are never excluded.
            if (!IsProbeOrCheck(step))
            {
                continue;
            }

            var served = ExpandProbe(step.Probe).ToList();
            var hits = served.Count(excluded.Contains);
            if (hits == 0)
            {
                continue;
            }

            if (hits != served.Count)
            {
                throw new ProbeUsageException(
                    $"Step '{step.Id}' serves probes {string.Join(" and ", served.Select(static p => $"#{p}"))} together; exclude all of them or none.");
            }

            removed.Add(step.Id);
        }

        var byId = catalog.ToDictionary(static step => step.Id, StringComparer.Ordinal);
        foreach (var step in catalog.Where(step => !removed.Contains(step.Id)))
        {
            foreach (var prerequisite in step.Prerequisites.Where(removed.Contains))
            {
                var owner = string.Join(" and ", ExpandProbe(byId[prerequisite].Probe).Select(static p => $"#{p}"));
                var dependent = string.Join(" and ", ExpandProbe(step.Probe).Select(static p => $"#{p}"));
                throw new ProbeUsageException(
                    $"Excluding probe {owner} would remove '{prerequisite}', a prerequisite of remaining step '{step.Id}'" +
                    (dependent.Length > 0
                        ? $"; also exclude probe {dependent} (--exclude-probe {string.Join(" --exclude-probe ", ExpandProbe(step.Probe))}) or do not exclude {owner}."
                        : "; that exclusion is not allowed."));
            }
        }

        if (!catalog.Any(step => IsProbeOrCheck(step) && !removed.Contains(step.Id)))
        {
            throw new ProbeUsageException($"The exclusions leave no probe in the {PhaseName(phase)} phase.");
        }

        return removed;
    }

    private static bool IsProbeOrCheck(ProbeStep step) => step.Kind is ProbeStepKind.Probe or ProbeStepKind.Check;

    internal static IReadOnlyList<string> CreateServerAdmissionArguments(
        IReadOnlyList<(string Method, IMessage? Request)> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var operations = SpatialAnalyzerApi.Operations.ToDictionary(
            static operation => operation.FullyQualifiedMethod, StringComparer.Ordinal);
        var profile = OperationPolicy.Create(PolicyConfiguration([]), SpatialAnalyzerApi.Operations);
        var overrides = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (method, request) in requests)
        {
            if (!operations.TryGetValue(method, out var operation))
            {
                throw new InvalidOperationException($"Probe RPC '{method}' is not a registered operation.");
            }

            // Decide the probe's own request, as the server will.
            var decision = profile.EvaluateRequest(operation.OperationId, request);
            if (decision.Kind == OperationPolicyDecisionKind.Allowed)
            {
                continue;
            }

            if (decision.DiagnosticCode is not ("operation-policy-denied" or "operation-option-denied") ||
                !decision.PolicyRule.EndsWith($"profile.{ServerAdmissionProfile}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Probe operation '{operation.OperationId}' cannot be admitted ({decision.DiagnosticCode}).");
            }

            overrides[$"{OperationPolicy.OverridesKey}:{operation.OperationId.Replace('.', ':')}"] =
                OperationPolicy.AllowValue;
        }

        // Prove the arguments admit every probed request before anyone uses them.
        var admitted = OperationPolicy.Create(PolicyConfiguration(overrides), SpatialAnalyzerApi.Operations);
        if (requests.Any(step => admitted.EvaluateRequest(
                operations[step.Method].OperationId, step.Request).Kind != OperationPolicyDecisionKind.Allowed))
        {
            throw new InvalidOperationException("The probe admission settings do not admit every probed request.");
        }

        return
        [
            $"--{OperationPolicy.ProfileKey}={ServerAdmissionProfile}",
            .. overrides
                .Select(static setting => $"--{setting.Key}={setting.Value}")
                .Order(StringComparer.Ordinal)
        ];
    }

    private static IConfiguration PolicyConfiguration(IEnumerable<KeyValuePair<string, string?>> overrides) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                overrides.Prepend(new(OperationPolicy.ProfileKey, ServerAdmissionProfile)))
            .Build();

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
        var earlier = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            Require(step.Phase == phase, $"Step '{step.Id}' belongs to another phase.");
            Require(step.Acceptable != ProbeOutcomes.None, $"Step '{step.Id}' accepts no outcome.");

            // A step's control or before-count must run earlier in the same session.
            Require(step.Prerequisites.All(earlier.Contains),
                $"Step '{step.Id}' has a prerequisite that does not run before it.");

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

            earlier.Add(step.Id);
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
        text.Append(CultureInfo.InvariantCulture, $"Steps: {Steps.Count}. Probes: {string.Join(", ", ProbesCovered())}.\n");
        if (ExcludedProbes.Count > 0)
        {
            text.Append(ExclusionStatement(ExcludedProbes)).Append(".\n");
            text.Append(CultureInfo.InvariantCulture,
                $"Excluded steps (never sent): {string.Join(", ", CatalogSteps.Where(IsExcluded).Select(static step => step.Id))}. ")
                .Append("Guard and fixture setup steps are never excluded.\n");
        }

        if (Phase == ProbePhase.PublicApi)
        {
            text.Append("Server admission: start Briosa.Server.exe with\n");
            foreach (var argument in ServerAdmissionArguments)
            {
                text.Append("  ").Append(argument).Append('\n');
            }
        }

        text.Append('\n');
        var index = 0;
        foreach (var step in CatalogSteps)
        {
            if (IsExcluded(step))
            {
                text.Append(CultureInfo.InvariantCulture,
                    $"[---] {step.Id} ({KindName(step.Kind)}, probe {step.Probe}) {step.Purpose}\n");
                text.Append("      Excluded by operator; never sent.\n");
                continue;
            }

            text.Append(CultureInfo.InvariantCulture,
                $"[{++index:D3}] {step.Id} ({KindName(step.Kind)}, probe {step.Probe}) {step.Purpose}\n");
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

            if (step.Prerequisites.Count > 0)
            {
                text.Append(CultureInfo.InvariantCulture, $"      prerequisites (run earlier): {string.Join(", ", step.Prerequisites)}\n");
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
