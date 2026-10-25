using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

// The second licensed session's probes: #309 (probe 11), #311 (probe 2), #312
// (probe 22), #307 R0 (probe 24), and #313 Silent verification (probe 25).
public sealed class FollowUpProbeTests
{
    private static readonly FixedClock Clock = new();

    private static ProbePlan Plan(ProbePhase phase, params int[] excluded) =>
        ProbePlan.Create(phase, TestSupport.SentinelManifest(), excluded);

    private static ProbeStep Step(ProbePhase phase, string id) => Plan(phase).Steps.Single(step => step.Id == id);

    // #309

    [Fact]
    public void Probe11IsTheFirstProbeAfterTheFixturesAndWaitsForTheOperator()
    {
        var steps = Plan(ProbePhase.PublicApi).Steps;

        var first = steps.First(static step => step.Kind is ProbeStepKind.Probe or ProbeStepKind.Check);

        Assert.Equal("p11", first.Id);
        Assert.Equal(OperatorCheckpoint.Before | OperatorCheckpoint.After, first.Checkpoints);
        Assert.All(steps.TakeWhile(static step => step.Id != "p11"),
            static step => Assert.True(step.Kind is ProbeStepKind.Guard or ProbeStepKind.Setup));
    }

    [Fact]
    public void Probe11OutwaitsTheServersLongRunningBudget()
    {
        var p11 = Step(ProbePhase.PublicApi, "p11");

        Assert.Equal(TimeSpan.FromSeconds(630), p11.ClientTimeout);
        Assert.True(p11.ClientTimeout > TimeSpan.FromMinutes(10));
        Assert.Contains("client deadline: 630 s", Plan(ProbePhase.PublicApi).RenderDryRun(), StringComparison.Ordinal);
    }

    [Fact]
    public void AStepsClientDeadlineReplacesTheSessionTimeoutOnlyWithinTheReviewedRange()
    {
        var count = new ProbeStep("x", "setup", ProbeStepKind.Guard, ProbePhase.PublicApi, "count", ProbeOperations.GetNumberOfCollections,
            new Api.GetNumberOfCollectionsRequest(), CommandVariant.Shipped, ProbeOutcomes.Succeeded);

        ProbePlan.Validate(ProbePhase.PublicApi, [count with { ClientTimeout = TimeSpan.FromMinutes(11) }]);
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [count with { ClientTimeout = TimeSpan.Zero }]));
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [count with { ClientTimeout = TimeSpan.FromHours(2) }]));
    }

    // #311

    [Theory]
    [InlineData(false, "p02-value")]
    [InlineData(true, "p02-shipped")]
    [InlineData(true, "p02-omit")]
    [InlineData(true, "p02-blank")]
    [InlineData(true, "p17-recased")]
    public void EveryFitUsesTheCoplanarGroup(bool worker, string id)
    {
        var fit = Assert.IsType<Api.FitGeometryToPointGroupRequest>(Step(worker ? ProbePhase.Worker : ProbePhase.PublicApi, id).Request);

        Assert.Equal(FixtureNames.GroupFit, fit.GroupToFit.ObjectName);
        Assert.Equal(FixtureNames.Collection, fit.GroupToFit.CollectionName);
    }

    [Fact]
    public void TheFitGroupIsCoplanarAndBuiltInBothPhases()
    {
        Assert.Equal(4, ProbeCatalog.FitPoints.Count);
        Assert.All(ProbeCatalog.FitPoints, static point => Assert.Equal(0, point.Z));
        foreach (var phase in new[] { ProbePhase.PublicApi, ProbePhase.Worker })
        {
            var ids = Plan(phase).Steps.Select(static step => step.Id).ToList();
            Assert.All(ProbeCatalog.FitPoints, point => Assert.Contains($"s-point-{FixtureNames.GroupFit}-{point.Name}", ids));
        }
    }

    // #312

    [Fact]
    public void Probe22PromptsForTheDocumentedGestureAndIsCheckedAfterwards()
    {
        var p22 = Step(ProbePhase.PublicApi, "p22");
        var request = Assert.IsType<Api.MakeCollectionVectorGroupNameRefListRuntimeSelectRequest>(p22.Request);

        Assert.True(p22.RequiresOperator);
        Assert.Equal(OperatorCheckpoint.After, p22.Checkpoints);
        Assert.Contains("press F2", request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("click OK, then press Enter", request.UserPrompt, StringComparison.Ordinal);
    }

    // #307 R0

    [Fact]
    public void EachRadialDeleteTargetsOnlyItsOwnDisposableCloud()
    {
        var steps = Plan(ProbePhase.PublicApi).Steps;
        foreach (var (id, cloud, type) in new[]
        {
            ("p24-r0", FixtureNames.DeleteCloudR0, Api.ObjectType.Cloud),
            ("p24-r0a", FixtureNames.DeleteCloudR0a, Api.ObjectType.Any)
        })
        {
            var step = steps.Single(step => step.Id == id);
            var delete = Assert.IsType<Api.DeleteCloudPointsByRadialDistanceFromPointsRequest>(step.Request);
            var name = Assert.Single(delete.CloudNames);
            var point = Assert.Single(delete.Points);

            Assert.Equal((FixtureNames.Collection, cloud, type), (name.CollectionName, name.ObjectName, name.ObjectType));
            Assert.Equal((FixtureNames.Collection, FixtureNames.GroupDelete, "D1"), (point.CollectionName, point.GroupName, point.TargetName));
            Assert.Equal((1d, true), (delete.Radius, delete.DeleteInside));
            Assert.Equal(OperatorCheckpoint.After, step.Checkpoints);
            Assert.Equal([$"c24-{id[4..]}-before"], step.Prerequisites);
            Assert.Single(steps, other => other.CreatesFixture == step.DestructiveTarget && other.Kind == ProbeStepKind.Setup);
        }
    }

    [Fact]
    public void TheSentinelIsCountedAroundTheDeletesAndNeverTargeted()
    {
        var steps = Plan(ProbePhase.PublicApi).Steps.ToList();
        var ids = steps.Select(static step => step.Id).ToList();

        Assert.True(ids.IndexOf("c24-sentinel-before") < ids.IndexOf("p24-r0"));
        Assert.True(ids.IndexOf("p24-r0a") < ids.IndexOf("c24-sentinel-after"));
        Assert.DoesNotContain(steps, static step => step.DestructiveTarget == FixtureNames.CloudKey(FixtureNames.DeleteSentinelCloud));
        Assert.DoesNotContain(steps, static step => step.CreatesFixture == FixtureNames.CloudKey(FixtureNames.DeleteSentinelCloud));
        foreach (var id in new[] { "c24-sentinel-before", "c24-sentinel-after", "c24-r0-before", "c24-r0a-before" })
        {
            Assert.NotNull(steps.Single(step => step.Id == id).Requirement);
        }
    }

    [Fact]
    public void OnlyD1LiesWithinTheDeleteRadius()
    {
        var d1 = ProbeCatalog.DeletePoints.Single(static point => point.Name == "D1");
        var within = ProbeCatalog.DeletePoints.Count(point =>
            Math.Sqrt(Math.Pow(point.X - d1.X, 2) + Math.Pow(point.Y - d1.Y, 2) + Math.Pow(point.Z - d1.Z, 2)) <= 1);

        Assert.Equal(8, ProbeCatalog.DeletePoints.Count);
        Assert.Equal(1, within);
    }

    [Fact]
    public async Task ASentinelCountOtherThanEightStopsTheSession()
    {
        var plan = Plan(ProbePhase.PublicApi);
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == "c24-sentinel-after"
            ? TestSupport.Outcome(ProbeOutcomeKind.Succeeded, ("points_count", "7"))
            : TestSupport.Succeeding(step));

        var record = await ProbeSession.RunAsync(plan, transport, Clock, SessionIdentity.None, new FakeOperator(), CancellationToken.None);

        Assert.Equal("c24-sentinel-after", record.StoppedAt);
        Assert.Equal(ProbeSession.RequirementStop, record.StopReason);
        Assert.DoesNotContain("p01-value", transport.Executed);
    }

    [Fact]
    public async Task AnAfterCountMayRecordAFailedCountWithoutStopping()
    {
        var plan = Plan(ProbePhase.PublicApi);
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == "c24-r0-after"
            ? TestSupport.Outcome(ProbeOutcomeKind.MpFailed)
            : TestSupport.Succeeding(step));

        var record = await ProbeSession.RunAsync(plan, transport, Clock, SessionIdentity.None, new FakeOperator(), CancellationToken.None);

        Assert.True(record.Completed);
    }

    // #313

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Probe25RunsLastAndEndsByRestoringTheOperatorsMode(bool worker)
    {
        var phase = worker ? ProbePhase.Worker : ProbePhase.PublicApi;
        var steps = ProbePlan.Create(phase, TestSupport.SentinelManifest(), [],
            new InteractionModeSettings(Api.SaInteractionMode.Automatic, Supplied: true)).Steps;
        var modes = steps.Where(static step => step.Request is Api.SetInteractionModeRequest)
            .Select(static step => (step.Id, ((Api.SetInteractionModeRequest)step.Request).SaInteractionMode))
            .ToList();

        Assert.Equal(
            [("p25-baseline", Api.SaInteractionMode.Automatic), ("p25-silent", Api.SaInteractionMode.Silent), ("p25-restore", Api.SaInteractionMode.Automatic)],
            modes);
        Assert.Equal("p25-restore", steps[^1].Id);
        Assert.All(steps.SkipWhile(static step => step.Id != "p25-baseline"), static step => Assert.Equal("25", step.Probe));
        Assert.All(steps.Where(static step => step.Probe == "25"), static step => Assert.True(step.Checkpoints.HasFlag(OperatorCheckpoint.After)));
        Assert.All(steps.Where(static step => step.Request is Api.SetInteractionModeRequest), static step =>
        {
            var request = (Api.SetInteractionModeRequest)step.Request;
            Assert.Equal(Api.MpInteractionMode.HaltOnFailureOrPartialSuccess, request.MeasurementPlanInteractionMode);
            Assert.Equal(Api.MpDialogInteractionMode.AllowApplicationInteraction, request.MeasurementPlanDialogInteractionMode);
            Assert.Equal(ProbeOutcomes.Succeeded, step.Acceptable);
        });
    }

    [Fact]
    public void ThePublicSilentBlockRepeatsEachBaselineDialogUnderSilent()
    {
        var ids = Plan(ProbePhase.PublicApi).Steps.Select(static step => step.Id).ToList();
        var silent = ids.IndexOf("p25-silent");

        foreach (var trigger in new[] { "c25-zero-match", "p25-chart-missing", "p25-ask" })
        {
            Assert.InRange(ids.IndexOf($"{trigger}-baseline"), ids.IndexOf("p25-baseline") + 1, silent - 1);
            Assert.InRange(ids.IndexOf($"{trigger}-silent"), silent + 1, ids.IndexOf("p25-restore") - 1);
        }

        Assert.InRange(ids.IndexOf("p25-runtime-select-silent"), silent + 1, ids.IndexOf("p25-restore") - 1);
        var chart = Assert.IsType<Api.CreateChartFromVectorGroupRequest>(Step(ProbePhase.PublicApi, "p25-chart-missing-silent").Request);
        Assert.Equal(ProbeCatalog.MissingTemplateChart, chart.TemplateChartName.Name);
    }

    [Fact]
    public void TheWorkerSilentBlockBlanksEachPlaneNameBeforeAndUnderSilent()
    {
        var steps = Plan(ProbePhase.Worker).Steps;
        var blanks = steps.Where(static step => step.Probe == "25" && step.Variant.Kind == VariantKind.BlankArgument)
            .Select(static step => (step.Id, step.Variant.ArgumentLabel))
            .ToList();

        Assert.Equal(
            [
                ("p25-blank-high-baseline", ProbeCatalog.HighPlaneLabel), ("p25-blank-low-baseline", ProbeCatalog.LowPlaneLabel),
                ("p25-blank-high-silent", ProbeCatalog.HighPlaneLabel), ("p25-blank-low-silent", ProbeCatalog.LowPlaneLabel)
            ],
            blanks);
        Assert.DoesNotContain(steps, static step => step.RequiresOperator);
    }

    [Fact]
    public void TheAskForStringObservationsCompareOnlyWithHarnessConstants()
    {
        var request = new Api.AskForStringRequest { QuestionToAsk = "q", InitialAnswer = ProbeCatalog.AskInitialAnswer };

        Assert.Equal(("false", "true"), Observe(request, ProbeCatalog.AskOperatorEntry));
        Assert.Equal(("true", "false"), Observe(request, ProbeCatalog.AskInitialAnswer));
        Assert.Equal(("false", "false"), Observe(request, TestSupport.Sentinel));

        static (string Initial, string Entry) Observe(Api.AskForStringRequest request, string answer)
        {
            var observations = ((ProbeOperation<Api.AskForStringRequest, Api.AskForStringResult>)ProbeOperations.AskForString)
                .ObservePublic(request, new Api.AskForStringResult { Answer = answer });

            // Flags only: the answer itself never reaches a record.
            Assert.Equal(["answer_is_initial", "answer_is_operator_entry"], observations.Keys);
            Assert.All(observations.Values, static value => Assert.True(value is "true" or "false"));
            return (observations["answer_is_initial"], observations["answer_is_operator_entry"]);
        }
    }

    [Fact]
    public void ValidationRefusesAPlanThatLeavesSilentInEffect()
    {
        var steps = Plan(ProbePhase.PublicApi).Steps;

        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [.. steps.Where(static step => step.Id != "p25-restore")]));
    }

    [Fact]
    public void ValidationRefusesAnInteractionModeStepThatIsNotACheckedControl()
    {
        var steps = Plan(ProbePhase.PublicApi).Steps;
        var unwatched = steps.Select(static step => step.Id == "p25-silent" ? step with { Checkpoints = OperatorCheckpoint.None } : step).ToList();
        var lenient = steps.Select(static step => step.Id == "p25-restore" ? step with { Acceptable = ProbeOutcomes.AnyDeterminateSdkOutcome } : step).ToList();

        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, unwatched));
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, lenient));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExcludingProbe25RemovesEveryInteractionModeStep(bool worker)
    {
        var plan = Plan(worker ? ProbePhase.Worker : ProbePhase.PublicApi, 25);

        Assert.False(plan.SetsInteractionMode);
        Assert.DoesNotContain(plan.Steps, static step => step.Probe == "25");
        Assert.DoesNotContain("Interaction mode (probe 25)", plan.RenderDryRun(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheRestoreOnlyPlanIsOneCheckedRestoreStep(bool worker)
    {
        var phase = worker ? ProbePhase.Worker : ProbePhase.PublicApi;

        var plan = ProbePlan.CreateInteractionModeRestore(phase, new InteractionModeSettings(Api.SaInteractionMode.Manual, Supplied: true));

        var step = Assert.Single(plan.Steps);
        Assert.Equal("p25-restore", step.Id);
        Assert.Equal(Api.SaInteractionMode.Manual, Assert.IsType<Api.SetInteractionModeRequest>(step.Request).SaInteractionMode);
        Assert.True(plan.InteractionModeRestoreOnly);
        Assert.Contains("Recovery plan", plan.RenderDryRun(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheDryRunStatesTheInteractionModesAndWhetherTheRestoreModeWasSupplied()
    {
        var placeholder = Plan(ProbePhase.Worker).RenderDryRun();
        var supplied = ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest(), [],
            new InteractionModeSettings(Api.SaInteractionMode.Automatic, Supplied: true)).RenderDryRun();

        Assert.Contains("send SA mode Manual (placeholder; a licensed run requires --restore-sa-interaction-mode)", placeholder, StringComparison.Ordinal);
        Assert.Contains("send SA mode Automatic (from --restore-sa-interaction-mode)", supplied, StringComparison.Ordinal);
        Assert.Contains("MP dialog mode Allow Application Interaction", supplied, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePublicPhaseAdmitsTheInteractionModeAndAskOperations()
    {
        var arguments = Plan(ProbePhase.PublicApi).ServerAdmissionArguments;

        Assert.Contains("--Briosa:Security:Operations:Overrides:process_flow_operations:ask_for_string=allow", arguments);
        Assert.Contains("--Briosa:Security:Operations:Overrides:utility_operations:set_interaction_mode=allow", arguments);
    }
}
