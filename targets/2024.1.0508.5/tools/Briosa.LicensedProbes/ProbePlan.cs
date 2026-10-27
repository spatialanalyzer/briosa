using System.Globalization;
using System.Text;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// A validated, ordered, immutable list of steps for one phase: the probes the operator
/// selected, or else every probe that is not opt-in, less any probes the operator
/// excluded. Guard and fixture setup steps are never excluded.
/// </summary>
internal sealed class ProbePlan
{
    public const string ExclusionReason = "reason not recorded by harness";

    private ProbePlan(
        ProbePhase phase,
        IReadOnlyList<ProbeStep> catalogSteps,
        IReadOnlySet<string> excludedStepIds,
        IReadOnlyList<int> excludedProbes,
        bool placeholderFixtures,
        InteractionModeSettings interactionMode,
        bool interactionModeRestoreOnly,
        IReadOnlyList<int> selectedProbes)
    {
        Phase = phase;
        CatalogSteps = catalogSteps;
        ExcludedStepIds = excludedStepIds;
        ExcludedProbes = excludedProbes;
        Steps = [.. catalogSteps.Where(step => !excludedStepIds.Contains(step.Id))];
        PlaceholderFixtures = placeholderFixtures;
        InteractionMode = interactionMode;
        InteractionModeRestoreOnly = interactionModeRestoreOnly;
        SelectedProbes = selectedProbes;
    }

    public ProbePhase Phase { get; }

    public InteractionModeSettings InteractionMode { get; }

    /// <summary>The recovery plan: only the probe 25 restore step.</summary>
    public bool InteractionModeRestoreOnly { get; }

    /// <summary>A step that runs changes SA's persisted interaction mode (probes 25 to 29).</summary>
    public bool SetsInteractionMode =>
        Steps.Any(static step => ReferenceEquals(step.Operation, ProbeOperations.SetInteractionMode));

    /// <summary>The steps that run, in order. Excluded steps are never sent.</summary>
    public IReadOnlyList<ProbeStep> Steps { get; }

    /// <summary>Every step the plan was built from (the selected or default probes) in order, including excluded ones.</summary>
    public IReadOnlyList<ProbeStep> CatalogSteps { get; }

    public IReadOnlySet<string> ExcludedStepIds { get; }

    /// <summary>Probe numbers the operator excluded, ascending.</summary>
    public IReadOnlyList<int> ExcludedProbes { get; }

    /// <summary>Probe numbers the operator selected with --probe, ascending; empty for the default plan.</summary>
    public IReadOnlyList<int> SelectedProbes { get; }

    /// <summary>The planned stall step that ends this plan (#313), or null.</summary>
    public ProbeStep? StallStep => Steps.Count > 0 && Steps[^1].ExpectedStall is not null ? Steps[^1] : null;

    /// <summary>
    /// The server setting Briosa:SpatialAnalyzer:InteractionMode the public phase needs:
    /// Silent for probe 29, otherwise Unchanged, so that only the plan's own steps set the mode.
    /// </summary>
    public string ServerInteractionModeSetting => ServerSetting(Steps) is { } mode
        ? InteractionModeSettings.ModeName(mode)
        : UnchangedSetting;

    /// <summary>The server sets SA Interaction Mode to Silent at Connect (probe 29).</summary>
    public bool ServerSetsSilent => ServerSetting(Steps) == Api.SaInteractionMode.Silent;

    // The setting's default: the server never sets the mode.
    public const string UnchangedSetting = "Unchanged";

    /// <summary>The server argument for <see cref="ServerInteractionModeSetting"/>; none in the worker phase, which has no server.</summary>
    public IReadOnlyList<string> ServerSettingArguments => Phase == ProbePhase.PublicApi
        ? [$"--{SpatialAnalyzerConnectionOptions.InteractionModeKey}={ServerInteractionModeSetting}"]
        : [];

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

    public static ProbePlan Create(ProbePhase phase, FixtureManifest manifest, IReadOnlyCollection<int> excludedProbes) =>
        Create(phase, manifest, excludedProbes, InteractionModeSettings.Placeholder);

    /// <summary>
    /// Builds the phase plan: the selected probes, or with no selection every probe that
    /// is not opt-in, less the excluded probes. A selection or exclusion that is not in
    /// this phase, splits a step serving several probes, drops a prerequisite of a
    /// remaining step, or leaves no probe is refused with <see cref="ProbeUsageException"/>,
    /// and so is a selection combined with an exclusion.
    /// </summary>
    public static ProbePlan Create(
        ProbePhase phase,
        FixtureManifest manifest,
        IReadOnlyCollection<int> excludedProbes,
        InteractionModeSettings interactionMode,
        IReadOnlyCollection<int>? selectedProbes = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(excludedProbes);
        ArgumentNullException.ThrowIfNull(interactionMode);
        manifest.Validate();
        var selected = (selectedProbes ?? []).Distinct().Order().ToList();
        if (selected.Count > 0 && excludedProbes.Count > 0)
        {
            throw new ProbeUsageException("--probe and --exclude-probe cannot be combined; name only the probes to run.");
        }

        var catalog = SelectSteps(phase, ProbeCatalog.Create(phase, manifest, interactionMode), selected);
        Validate(phase, catalog);

        // The stall leaves no step to restore another mode afterwards, and #313 asks for SA in Manual.
        if (catalog.Any(static step => step.ExpectedStall is not null) && interactionMode.Restore != Api.SaInteractionMode.Manual)
        {
            throw new ProbeUsageException(
                $"A stall probe runs with SA in Manual and ends its run, so nothing could restore another mode; use {ProbeOptions.RestoreModeOption} manual.");
        }

        var probes = excludedProbes.Distinct().Order().ToList();
        var excluded = SelectExcludedSteps(phase, catalog, probes);
        var plan = new ProbePlan(phase, catalog, excluded, probes, manifest.IsPlaceholder, interactionMode, interactionModeRestoreOnly: false, selected);

        // The remaining plan must satisfy every invariant the full plan does.
        Validate(phase, plan.Steps);
        if (phase == ProbePhase.PublicApi)
        {
            // Fails closed when a public probe needs an exclusive workflow.
            _ = plan.ServerAdmissionArguments;
        }

        return plan;
    }

    /// <summary>
    /// The recovery plan after a session stopped between probe 25's Silent and restore
    /// steps: it only restores SA Interaction Mode, on a clean SA, in either phase.
    /// </summary>
    public static ProbePlan CreateInteractionModeRestore(ProbePhase phase, InteractionModeSettings interactionMode)
    {
        ArgumentNullException.ThrowIfNull(interactionMode);
        var steps = ProbeCatalog.CreateInteractionModeRestore(phase, interactionMode);
        Validate(phase, steps);
        var plan = new ProbePlan(phase, steps, new HashSet<string>(StringComparer.Ordinal), [], placeholderFixtures: false,
            interactionMode, interactionModeRestoreOnly: true, selectedProbes: []);
        if (phase == ProbePhase.PublicApi)
        {
            _ = plan.ServerAdmissionArguments;
        }

        return plan;
    }

    /// <summary>
    /// A plan over explicit steps with no exclusions, validated like a catalog plan.
    /// Portable tests use it for step options that no current catalog step sets, such
    /// as a before-dispatch checkpoint or a step's own client deadline.
    /// </summary>
    internal static ProbePlan FromSteps(ProbePhase phase, IReadOnlyList<ProbeStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Validate(phase, steps);
        return new ProbePlan(phase, steps, new HashSet<string>(StringComparer.Ordinal), [], placeholderFixtures: true,
            InteractionModeSettings.Placeholder, interactionModeRestoreOnly: false, selectedProbes: []);
    }

    /// <summary>
    /// The catalog steps a plan is built from. With no selection, every probe that is not
    /// opt-in (<see cref="ProbeCatalog.OptInProbeNumbers"/>); with one, only the selected
    /// probes. Guard and fixture setup steps are always kept.
    /// </summary>
    internal static IReadOnlyList<ProbeStep> SelectSteps(ProbePhase phase, IReadOnlyList<ProbeStep> catalog, IReadOnlyList<int> selectedProbes)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(selectedProbes);
        if (selectedProbes.Count == 0)
        {
            return [.. catalog.Where(static step =>
                !IsProbeOrCheck(step) || !ExpandProbe(step.Probe).Intersect(ProbeCatalog.OptInProbeNumbers, StringComparer.Ordinal).Any())];
        }

        var selected = selectedProbes.Select(static probe => probe.ToString(CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal);
        foreach (var probe in selected)
        {
            if (!catalog.Any(step => IsProbeOrCheck(step) && ExpandProbe(step.Probe).Contains(probe, StringComparer.Ordinal)))
            {
                throw new ProbeUsageException(
                    $"Probe #{probe} has no steps in the {PhaseName(phase)} phase; --probe accepts only probes of the selected phase.");
            }
        }

        var kept = new List<ProbeStep>();
        foreach (var step in catalog)
        {
            if (!IsProbeOrCheck(step))
            {
                kept.Add(step);
                continue;
            }

            var served = ExpandProbe(step.Probe).ToList();
            var hits = served.Count(selected.Contains);
            if (hits == 0)
            {
                continue;
            }

            if (hits != served.Count)
            {
                throw new ProbeUsageException(
                    $"Step '{step.Id}' serves probes {string.Join(" and ", served.Select(static p => $"#{p}"))} together; select all of them or none.");
            }

            kept.Add(step);
        }

        var keptIds = kept.Select(static step => step.Id).ToHashSet(StringComparer.Ordinal);
        var byId = catalog.ToDictionary(static step => step.Id, StringComparer.Ordinal);
        foreach (var step in kept)
        {
            foreach (var prerequisite in step.Prerequisites.Where(prerequisite => !keptIds.Contains(prerequisite)))
            {
                var owner = ExpandProbe(byId[prerequisite].Probe).ToList();
                throw new ProbeUsageException(
                    $"Step '{step.Id}' needs '{prerequisite}' of probe {string.Join(" and ", owner.Select(static p => $"#{p}"))}; " +
                    $"also select it (--probe {string.Join(" --probe ", owner)}).");
            }
        }

        // The opt-in rules that validation enforces, as the operator's usage errors.
        var stalls = kept.Where(static step => step.ExpectedStall is not null).Select(static step => $"#{step.Probe}").ToList();
        if (stalls.Count > 1)
        {
            throw new ProbeUsageException(
                $"Probes {string.Join(" and ", stalls)} each end their run in a planned stall; select one of them per run.");
        }

        var setting = kept.FirstOrDefault(static step => step.ServerInteractionMode is not null);
        if (setting is { ServerInteractionMode: { } mode } &&
            kept.Any(step => IsProbeOrCheck(step) && step.ServerInteractionMode != mode))
        {
            throw new ProbeUsageException(
                $"Probe #{setting.Probe} needs the server setting {SpatialAnalyzerConnectionOptions.InteractionModeKey}={InteractionModeSettings.ModeName(mode)}; " +
                $"run it alone (--probe {setting.Probe}).");
        }

        return kept;
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

        // A stall step ends its run on purpose (#313): it is the plan's only stall and its
        // last step, so nothing can ever be sent after a stall.
        Require(steps.Count(static step => step.ExpectedStall is not null) <= 1 && steps.Take(steps.Count - 1).All(static step => step.ExpectedStall is null),
            "A stall step must be the plan's only stall step and its last step.");

        // Probe 29 needs the server setting Silent, which would change every other probe's
        // conditions, so its run holds no other probe. The server sets Silent at Connect.
        var setting = ServerSetting(steps);
        if (setting is not null)
        {
            Require(phase == ProbePhase.PublicApi, "A step needs a server setting, and the worker phase has no server.");
            Require(steps.Where(IsProbeOrCheck).All(step => step.ServerInteractionMode == setting),
                "A plan whose server needs another interaction mode setting holds no other probe.");
        }

        var created = new HashSet<string>(StringComparer.Ordinal);
        var earlier = new HashSet<string>(StringComparer.Ordinal);
        var silent = setting == Api.SaInteractionMode.Silent;
        foreach (var step in steps)
        {
            Require(step.Phase == phase, $"Step '{step.Id}' belongs to another phase.");
            Require(step.Acceptable != ProbeOutcomes.None || step.ExpectedStall is not null, $"Step '{step.Id}' accepts no outcome.");
            Require(step.ClientTimeout is null || (step.ClientTimeout > TimeSpan.Zero && step.ClientTimeout <= MaximumClientTimeout),
                $"Step '{step.Id}' has a client deadline outside the reviewed range.");

            // A stall step accepts nothing but its stall, touches no fixture, and has the
            // operator check SA before and after it under its own declared deadline.
            if (step.ExpectedStall is not null)
            {
                Require(step.Kind == ProbeStepKind.Probe && step.Acceptable == ProbeOutcomes.None && step.Requirement is null &&
                    step.CreatesFixture is null && step.DestructiveTarget is null && step.ServerInteractionMode is null,
                    $"Step '{step.Id}' is a stall step; it accepts only its stall and creates or destroys nothing.");
                Require(step.Checkpoints == (OperatorCheckpoint.Before | OperatorCheckpoint.After) && step.ClientTimeout is not null,
                    $"Step '{step.Id}' is a stall step; it needs checkpoints before and after it and its own client deadline.");
            }

            // SA keeps its interaction mode across restarts and has no getter, so a step
            // that sets it is a control whose failure stops the session.
            if (step.Request is Api.SetInteractionModeRequest mode)
            {
                Require(step.Kind == ProbeStepKind.Probe && step.Variant.Kind == VariantKind.Shipped &&
                    step.Acceptable == ProbeOutcomes.Succeeded && step.Checkpoints.HasFlag(OperatorCheckpoint.After),
                    $"Step '{step.Id}' changes SA Interaction Mode; it must be a shipped control the operator checks afterwards.");
                silent = mode.SaInteractionMode == Api.SaInteractionMode.Silent;
            }

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
            // answer. An outcome whose completion is unknown is never an answer, except
            // the stall a stall step plans to end in.
            if (step.Variant.Kind != VariantKind.Shipped)
            {
                Require(step.ExpectedStall is not null || step.Acceptable == ProbeOutcomes.AnyDeterminateSdkOutcome,
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

        // A plan never ends with SA left in Silent: a later step must restore another mode.
        Require(!silent, "A plan that sets SA Interaction Mode to Silent must restore another mode in a later step.");
    }

    // The one interaction-mode setting the steps need from the server, or null when none
    // needs one. Steps that need different settings cannot share a server.
    private static Api.SaInteractionMode? ServerSetting(IReadOnlyList<ProbeStep> steps)
    {
        var settings = steps.Select(static step => step.ServerInteractionMode).OfType<Api.SaInteractionMode>().Distinct().ToList();
        if (settings.Count > 1)
        {
            throw new InvalidOperationException("The plan's steps need different server interaction mode settings.");
        }

        return settings.Count == 0 ? null : settings[0];
    }

    /// <summary>The longest client deadline a step may declare.</summary>
    public static TimeSpan MaximumClientTimeout { get; } = TimeSpan.FromHours(1);

    // The only reviewed destructive operation deletes cloud points around points (probe 24,
    // the #307 control R0); its request may name exactly one cloud, the harness-created
    // disposable target, and only points in the harness collection. (Delete Cloud Points by
    // X Y Z Range, the operation of the retired probes 15 and 16, was removed for #307.)
    private static bool TargetsOnly(ProbeStep step) =>
        step.Request is Api.DeleteCloudPointsByRadialDistanceFromPointsRequest delete &&
        delete.CloudNames.Count == 1 &&
        string.Equals(delete.CloudNames[0].CollectionName, FixtureNames.Collection, StringComparison.Ordinal) &&
        string.Equals("cloud:" + delete.CloudNames[0].ObjectName, step.DestructiveTarget, StringComparison.Ordinal) &&
        delete.Points.All(static point => string.Equals(point.CollectionName, FixtureNames.Collection, StringComparison.Ordinal));

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
        if (SelectedProbes.Count > 0)
        {
            text.Append(SelectionStatement(SelectedProbes)).Append(".\n");
        }

        if (ExcludedProbes.Count > 0)
        {
            text.Append(ExclusionStatement(ExcludedProbes)).Append(".\n");
            text.Append(CultureInfo.InvariantCulture,
                $"Excluded steps (never sent): {string.Join(", ", CatalogSteps.Where(IsExcluded).Select(static step => step.Id))}. ")
                .Append("Guard and fixture setup steps are never excluded.\n");
        }

        if (InteractionModeRestoreOnly)
        {
            text.Append("Recovery plan: only restores SA Interaction Mode after a session stopped inside probe 25 or 29.\n");
        }

        if (SetsInteractionMode)
        {
            // Harness constants and the operator's restore mode; no manifest value.
            var modeSteps = Steps.Where(static step => step.Request is Api.SetInteractionModeRequest).ToList();
            var probes = modeSteps.Select(static step => step.Probe).Distinct(StringComparer.Ordinal).ToList();
            var silentStep = modeSteps.Any(static step => step.Request is Api.SetInteractionModeRequest
            {
                SaInteractionMode: Api.SaInteractionMode.Silent
            });
            var subject = (modeSteps.Any(static step => step.Id.EndsWith("-baseline", StringComparison.Ordinal)),
                    modeSteps.Any(static step => step.Id.EndsWith("-restore", StringComparison.Ordinal))) switch
            {
                (true, true) => "the baseline and restore steps send",
                (true, false) => "the baseline step sends",
                (false, true) => "the restore step sends",
                _ => "its steps send"
            };
            text.Append(CultureInfo.InvariantCulture, $"Interaction mode ({(probes.Count == 1 ? "probe" : "probes")} {string.Join(", ", probes)}): ")
                .Append(ServerSetsSilent ? "the server's setting sets Silent after Connect, " : string.Empty)
                .Append(CultureInfo.InvariantCulture, $"{subject} SA mode {InteractionModeSettings.ModeName(InteractionMode.Restore)} ")
                .Append(InteractionMode.Supplied
                    ? "(from --restore-sa-interaction-mode)"
                    : "(placeholder; a licensed run requires --restore-sa-interaction-mode)")
                .Append(silentStep ? ", the Silent step sends Silent," : ",")
                .Append(" and every step sends MP mode Halt on Failure or Partial Success ")
                .Append("and MP dialog mode Allow Application Interaction. SA keeps these across restarts.\n");
        }

        if (StallStep is { } stall)
        {
            text.Append(CultureInfo.InvariantCulture,
                $"Planned stall: the last step, {stall.Id}, should stall on a SpatialAnalyzer dialog until its watchdog ends it. ")
                .Append("Leave that dialog open until the step returns. Nothing is sent after it; recover as the runbook says for a stall run.\n");
        }

        if (Steps.Any(static step => step.Checkpoints != OperatorCheckpoint.None))
        {
            text.Append("Operator checkpoints: a licensed run pauses at each one until the operator writes its response file.\n");
        }

        if (Phase == ProbePhase.PublicApi)
        {
            text.Append("Server admission: start Briosa.Server.exe with\n");
            foreach (var argument in ServerAdmissionArguments)
            {
                text.Append("  ").Append(argument).Append('\n');
            }

            text.Append("Server interaction mode setting (#313): also start it with\n");
            foreach (var argument in ServerSettingArguments)
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

            text.Append(CultureInfo.InvariantCulture, $"      accept: {AcceptanceText(step)}\n");
            if (step.ExpectedStall is { } expected)
            {
                text.Append(CultureInfo.InvariantCulture, $"      STALL: {StallText(expected)}. Leave the dialog open until the step returns\n");
            }

            if (step.ServerInteractionMode is { } setting)
            {
                text.Append(CultureInfo.InvariantCulture,
                    $"      server setting: {SpatialAnalyzerConnectionOptions.InteractionModeKey}={InteractionModeSettings.ModeName(setting)}\n");
            }

            if (step.DestructiveTarget is not null)
            {
                text.Append(CultureInfo.InvariantCulture,
                    $"      DESTRUCTIVE: only the harness-created {step.DestructiveTarget}; refused unless created this session\n");
            }

            if (step.RequiresOperator)
            {
                text.Append("      OPERATOR: SpatialAnalyzer prompts the operator during this step\n");
            }

            if (step.Checkpoints.HasFlag(OperatorCheckpoint.Before))
            {
                text.Append("      CHECKPOINT before dispatch: the operator confirms that no SpatialAnalyzer dialog is open\n");
            }

            if (step.Checkpoints.HasFlag(OperatorCheckpoint.After))
            {
                text.Append("      CHECKPOINT after the result: the operator records and dismisses any dialog\n");
            }

            if (step.ClientTimeout is { } deadline)
            {
                text.Append(CultureInfo.InvariantCulture, $"      client deadline: {deadline.TotalSeconds:0} s\n");
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

    /// <summary>The statement every output carries when the operator selected probes.</summary>
    public static string SelectionStatement(IReadOnlyList<int> selectedProbes)
    {
        ArgumentNullException.ThrowIfNull(selectedProbes);
        var probes = string.Join(", ", selectedProbes.Select(static probe => "#" + probe.ToString(CultureInfo.InvariantCulture)));
        return $"Selected by operator (--probe): {probes}; no other probe is part of this plan";
    }

    /// <summary>What a step accepts: its determinate outcomes, or only its planned stall.</summary>
    public static string AcceptanceText(ProbeStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return step.ExpectedStall is null ? AcceptableName(step.Acceptable) : "only its planned stall";
    }

    /// <summary>The expected stall in words: harness constants only.</summary>
    public static string StallText(StallExpectation stall)
    {
        ArgumentNullException.ThrowIfNull(stall);
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"completion unknown through {stall.Transport} with diagnostic {stall.DiagnosticCode}");
        if (stall.FailureKind is not null)
        {
            text.Append(CultureInfo.InvariantCulture, $", failure kind {stall.FailureKind}");
        }

        if (stall.RecoveryGuidance is not null)
        {
            text.Append(CultureInfo.InvariantCulture, $", recovery guidance {stall.RecoveryGuidance}");
        }

        return text.ToString();
    }

    public static string AcceptableName(ProbeOutcomes outcomes) =>
        outcomes == ProbeOutcomes.AnyDeterminateSdkOutcome
            ? "any determinate SDK outcome (Succeeded, MpFailed, ArgumentRejected, or OutputRetrievalFailed)"
            : string.Join(" or ", Enum.GetValues<ProbeOutcomes>()
                .Where(flag => flag is not ProbeOutcomes.None and not ProbeOutcomes.AnyDeterminateSdkOutcome && outcomes.HasFlag(flag))
                .Select(static flag => flag.ToString()));

    private static string Binding(WorkerMpInputArgument argument) => argument.SdkBinding ?? "setter";

    private static string Flag(bool value) => value ? "yes" : "no";
}
