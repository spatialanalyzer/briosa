using System.Globalization;
using System.Text.Json;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

public sealed class ProbeExclusionTests
{
    private const string Statement11 = "Excluded by operator: #11 — reason not recorded by harness";

    private static readonly FixedClock Clock = new();

    private static ProbePlan Plan(ProbePhase phase, params int[] excluded) =>
        ProbePlan.Create(phase, TestSupport.SentinelManifest(), excluded);

    private static ProbePhase Phase(bool worker) => worker ? ProbePhase.Worker : ProbePhase.PublicApi;

    private static IEnumerable<string> Ids(IEnumerable<ProbeStep> steps) => steps.Select(static step => step.Id);

    // Parsing

    [Theory]
    [InlineData("public-api", true)]
    [InlineData("public-api", false)]
    [InlineData("worker", true)]
    [InlineData("worker", false)]
    public void ExclusionsAreAcceptedInDryRunsAndLicensedRunsOfBothPhases(string phase, bool dryRun)
    {
        string[] mode = dryRun
            ? ["--dry-run"]
            : phase == "worker"
                ?
                [
                    ProbeTarget.ConfirmFlag, "--fixtures", "m.json", "--output-directory", "out",
                    "--worker-path", @"C:\package\Briosa.Worker.exe",
                    "--connected-sa-attested-version", ProbeTarget.SpatialAnalyzerTarget,
                    "--connected-sa-attestation-reference", "r277-connected"
                ]
                : [ProbeTarget.ConfirmFlag, "--fixtures", "m.json", "--output-directory", "out"];

        var options = ProbeOptions.Parse(["--phase", phase, "--exclude-probe", "11", .. mode, "--exclude-probe", "3"]);

        Assert.Equal([3, 11], options.ExcludedProbes);
        Assert.Empty(ProbeOptions.Parse(["--phase", phase, .. mode]).ExcludedProbes);
    }

    [Fact]
    public void ADuplicateExclusionIsRefused()
    {
        var exception = Assert.Throws<ProbeUsageException>(() =>
            ProbeOptions.Parse(["--phase", "public-api", "--dry-run", "--exclude-probe", "11", "--exclude-probe", "11"]));

        Assert.Contains("more than once", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("24")]
    [InlineData("99")]
    public void AnUnknownProbeNumberIsRefused(string probe)
    {
        var exception = Assert.Throws<ProbeUsageException>(() =>
            ProbeOptions.Parse(["--phase", "public-api", "--dry-run", "--exclude-probe", probe]));

        Assert.Contains("not a probe in the approved list", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("011")]
    [InlineData("+11")]
    [InlineData("-11")]
    [InlineData(" 11")]
    [InlineData("11.0")]
    [InlineData("5-6")]
    [InlineData("p11")]
    [InlineData("")]
    public void AnInvalidProbeNumberIsRefused(string probe)
    {
        Assert.Throws<ProbeUsageException>(() =>
            ProbeOptions.Parse(["--phase", "worker", "--dry-run", "--exclude-probe", probe]));
    }

    [Fact]
    public void AnExclusionNeedsAValue()
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(["--phase", "worker", "--dry-run", "--exclude-probe"]));
    }

    [Theory]
    [InlineData(false, 17)]
    [InlineData(false, 19)]
    [InlineData(true, 11)]
    [InlineData(true, 22)]
    public void AProbeOutsideTheSelectedPhaseIsRefused(bool worker, int probe)
    {
        var phase = Phase(worker);
        var exception = Assert.Throws<ProbeUsageException>(() => Plan(phase, probe));

        Assert.Contains($"#{probe} has no steps in the {ProbePlan.PhaseName(phase)} phase", exception.Message, StringComparison.Ordinal);
    }

    // Plan filtering

    [Fact]
    public void ExcludingProbe11RemovesOnlyItsProbeAndCheckSteps()
    {
        var full = Plan(ProbePhase.PublicApi);
        var plan = Plan(ProbePhase.PublicApi, 11);

        Assert.Equal(["c11", "p11"], plan.ExcludedStepIds.Order(StringComparer.Ordinal));
        Assert.Equal([11], plan.ExcludedProbes);
        Assert.Equal(Ids(full.Steps), Ids(plan.CatalogSteps));
        Assert.Equal(Ids(full.Steps).Where(static id => id is not ("p11" or "c11")), Ids(plan.Steps));
        Assert.DoesNotContain("11", plan.ProbesCovered());

        // The polygonize fixtures stay even though only #11 used them.
        Assert.Contains("s-grid-GRID", Ids(plan.Steps));
        Assert.Contains("s-cloud-CPOLY", Ids(plan.Steps));

        // The destructive probes keep their disposable clouds and before-counts.
        foreach (var id in new[] { "s-cloud-CDEL15", "s-cloud-CDEL16", "c15-before", "p15", "c15-after", "c16-before", "p16", "c16-after" })
        {
            Assert.Contains(id, Ids(plan.Steps));
        }

        Assert.DoesNotContain(ProbeOperations.ConstructPolygonizedSurface.FullyQualifiedMethod, plan.FullyQualifiedMethods);
    }

    [Theory]
    [InlineData(false, new[] { 5, 6, 15 })]
    [InlineData(true, new[] { 2, 5, 6 })]
    public void EveryAcceptedSingleExclusionKeepsEveryGuardAndFixtureStep(bool worker, int[] refusedAlone)
    {
        var phase = Phase(worker);
        var full = Plan(phase);
        var foundations = Ids(full.Steps.Where(static step => step.Kind is ProbeStepKind.Guard or ProbeStepKind.Setup)).ToList();
        var refused = new List<int>();
        foreach (var number in full.ProbesCovered().Select(static probe => int.Parse(probe, CultureInfo.InvariantCulture)))
        {
            ProbePlan plan;
            try
            {
                plan = Plan(phase, number);
            }
            catch (ProbeUsageException)
            {
                refused.Add(number);
                continue;
            }

            Assert.All(foundations, id => Assert.Contains(id, Ids(plan.Steps)));
            Assert.NotEmpty(plan.ExcludedStepIds);
            Assert.All(plan.CatalogSteps.Where(plan.IsExcluded), step =>
            {
                Assert.True(step.Kind is ProbeStepKind.Probe or ProbeStepKind.Check);
                Assert.Equal([number.ToString(CultureInfo.InvariantCulture)], ProbePlan.ExpandProbe(step.Probe));
            });
            Assert.Equal(full.Steps.Count - plan.ExcludedStepIds.Count, plan.Steps.Count);

            // The remaining plan passes every invariant the full plan does.
            ProbePlan.Validate(phase, plan.Steps);
        }

        Assert.Equal(refusedAlone, refused);
    }

    [Fact]
    public void ExcludingTheWorkerControlFor17WithoutProbe17IsRefused()
    {
        var exception = Assert.Throws<ProbeUsageException>(() => Plan(ProbePhase.Worker, 2));

        Assert.Contains("'p02-shipped'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'p17-recased'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("--exclude-probe 17", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcludingProbes2And17TogetherKeepsProbe18()
    {
        var plan = Plan(ProbePhase.Worker, 17, 2);

        Assert.Equal([2, 17], plan.ExcludedProbes);
        Assert.Equal(
            ["c02-blank", "c02-omit", "c02-shipped", "c17-recased", "p02-blank", "p02-omit", "p02-shipped", "p17-recased"],
            plan.ExcludedStepIds.Order(StringComparer.Ordinal));
        Assert.Contains("p18-shipped", Ids(plan.Steps));
        Assert.Contains("p18-recased", Ids(plan.Steps));
        Assert.DoesNotContain(plan.ProbesCovered(), static probe => probe is "2" or "17");
    }

    [Fact]
    public void TheControlDependencyIsDeclaredInThePlanModel()
    {
        var steps = Plan(ProbePhase.Worker).Steps;

        Assert.Equal(["p02-shipped"], steps.Single(static step => step.Id == "p17-recased").Prerequisites);
        Assert.Contains("p15", Plan(ProbePhase.PublicApi).Steps.Single(static step => step.Id == "p16").Prerequisites);
        Assert.Contains("prerequisites (run earlier): p02-shipped", Plan(ProbePhase.Worker).RenderDryRun(), StringComparison.Ordinal);
    }

    [Fact]
    public void ExcludingProbe15WithoutProbe16IsRefusedButBothTogetherKeepTheirClouds()
    {
        var exception = Assert.Throws<ProbeUsageException>(() => Plan(ProbePhase.PublicApi, 15));
        Assert.Contains("'p16'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("--exclude-probe 16", exception.Message, StringComparison.Ordinal);

        var plan = Plan(ProbePhase.PublicApi, 15, 16);
        Assert.DoesNotContain(plan.Steps, static step => step.DestructiveTarget is not null);
        Assert.Contains("s-cloud-CDEL15", Ids(plan.Steps));
        Assert.Contains("s-cloud-CDEL16", Ids(plan.Steps));
        Assert.Contains("p15", Ids(Plan(ProbePhase.PublicApi, 16).Steps));
    }

    [Theory]
    [InlineData(false, 5)]
    [InlineData(false, 6)]
    [InlineData(true, 5)]
    public void AStepServingSeveralProbesCannotBeSplit(bool worker, int probe)
    {
        var phase = Phase(worker);
        var exception = Assert.Throws<ProbeUsageException>(() => Plan(phase, probe));

        Assert.Contains("serves probes #5 and #6 together", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Plan(phase, 5, 6).Steps, static step => ProbePlan.ExpandProbe(step.Probe).Any(static p => p is "5" or "6"));
    }

    [Fact]
    public void AnExclusionThatLeavesNoProbeIsRefused()
    {
        var all = Plan(ProbePhase.Worker).ProbesCovered().Select(static probe => int.Parse(probe, CultureInfo.InvariantCulture)).ToArray();

        var exception = Assert.Throws<ProbeUsageException>(() => Plan(ProbePhase.Worker, all));

        Assert.Contains("leave no probe", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationRefusesAPrerequisiteThatDoesNotRunEarlier()
    {
        static ProbeStep Count(string id) =>
            new(id, "setup", ProbeStepKind.Guard, ProbePhase.PublicApi, "count", ProbeOperations.GetNumberOfCollections,
                new Api.GetNumberOfCollectionsRequest(), CommandVariant.Shipped, ProbeOutcomes.Succeeded);
        var dependent = Count("after") with { Prerequisites = ["before"] };

        ProbePlan.Validate(ProbePhase.PublicApi, [Count("before"), dependent]);
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [dependent, Count("before")]));
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [dependent]));
    }

    // Dry run, session, and records

    [Fact]
    public void TheDryRunStatesTheExclusionAndMarksExcludedSteps()
    {
        var plan = Plan(ProbePhase.PublicApi, 11);

        var text = plan.RenderDryRun();

        Assert.Contains(Statement11 + ".", text, StringComparison.Ordinal);
        Assert.Contains("Excluded steps (never sent): p11, c11.", text, StringComparison.Ordinal);
        Assert.Contains("[---] p11 (probe, probe 11)", text, StringComparison.Ordinal);
        Assert.Contains("[---] c11 (check, probe 11)", text, StringComparison.Ordinal);
        Assert.Contains("Excluded by operator; never sent.", text, StringComparison.Ordinal);
        Assert.Contains(string.Create(CultureInfo.InvariantCulture, $"[{plan.Steps.Count:D3}]"), text, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Create(CultureInfo.InvariantCulture, $"[{plan.Steps.Count + 1:D3}]"), text, StringComparison.Ordinal);
        Assert.DoesNotContain("Excluded by operator", Plan(ProbePhase.PublicApi).RenderDryRun(), StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExcludedStepsAreNeverSentAndTheRemainingPlanCompletes()
    {
        var plan = Plan(ProbePhase.PublicApi, 11);
        var transport = new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding);

        var record = await ProbeSession.RunAsync(plan, transport, Clock, SessionIdentity.None, CancellationToken.None);

        Assert.True(record.Completed);
        Assert.Equal(Ids(plan.Steps), transport.Executed);
        Assert.DoesNotContain("p11", transport.Executed);
        Assert.Equal(Ids(plan.CatalogSteps), record.Steps.Select(static step => step.Step.Id));
        Assert.Equal(StepClassification.Excluded, record.Steps.Single(static step => step.Step.Id == "p11").Classification);
        Assert.Equal([11], record.ExcludedProbes);
    }

    [Fact]
    public async Task RecordsDistinguishExcludedFromNotRun()
    {
        var plan = Plan(ProbePhase.PublicApi, 11);
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == "p12"
            ? ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure")
            : TestSupport.Succeeding(step));
        var record = await ProbeSession.RunAsync(plan, transport, Clock, SessionIdentity.None, CancellationToken.None);

        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var root = document.RootElement;
        var exclusions = root.GetProperty("exclusions");
        var steps = root.GetProperty("steps").EnumerateArray()
            .ToDictionary(static step => step.GetProperty("id").GetString()!, static step => step.GetProperty("classification").GetString());
        var markdown = ProbeRecorder.ToMarkdown(record);

        Assert.Equal(2, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(Statement11, exclusions.GetProperty("statement").GetString());
        Assert.Equal("operator", exclusions.GetProperty("excluded_by").GetString());
        Assert.False(exclusions.GetProperty("reason_recorded_by_harness").GetBoolean());
        Assert.Equal([11], exclusions.GetProperty("probes").EnumerateArray().Select(static probe => probe.GetInt32()));
        Assert.Equal(["p11", "c11"], exclusions.GetProperty("steps").EnumerateArray().Select(static step => step.GetString()));
        Assert.Equal("excluded", steps["p11"]);
        Assert.Equal("excluded", steps["c11"]);
        Assert.Equal("unexpected", steps["p12"]);
        Assert.Equal("not-run", steps["p13"]);

        Assert.Contains($"**{Statement11}.**", markdown, StringComparison.Ordinal);
        Assert.Contains("justification for each exclusion must be stated in the committed evidence", markdown, StringComparison.Ordinal);
        Assert.Contains("| `p11` | 11 |", markdown, StringComparison.Ordinal);
        Assert.Contains("Excluded by operator; not sent.", markdown, StringComparison.Ordinal);
        var notRun = markdown.Split('\n').Single(static line => line.StartsWith("Not run:", StringComparison.Ordinal));
        Assert.Contains("`p13`", notRun, StringComparison.Ordinal);
        Assert.DoesNotContain("`p11`", notRun, StringComparison.Ordinal);
        Assert.DoesNotContain("`c11`", notRun, StringComparison.Ordinal);
    }

    [Fact]
    public void DryRunRecordsMarkExcludedStepsAndOthersAsNotRun()
    {
        var record = ProbeSession.DryRun(Plan(ProbePhase.Worker, 2, 17), Clock);

        var markdown = ProbeRecorder.ToMarkdown(record);
        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var classifications = document.RootElement.GetProperty("steps").EnumerateArray()
            .ToDictionary(static step => step.GetProperty("id").GetString()!, static step => step.GetProperty("classification").GetString());

        Assert.Contains("**Excluded by operator: #2, #17 — reason not recorded by harness.**", markdown, StringComparison.Ordinal);
        Assert.Contains("| `p17-recased` | 17 |", markdown, StringComparison.Ordinal);
        Assert.Equal("excluded", classifications["p02-shipped"]);
        Assert.Equal("excluded", classifications["p17-recased"]);
        Assert.Equal("not-run", classifications["p18-shipped"]);
        Assert.Equal("not-run", classifications["g-collections-before"]);
    }

    [Fact]
    public void ARecordWithoutExclusionsSaysSo()
    {
        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(ProbeSession.DryRun(Plan(ProbePhase.Worker), Clock)));
        var exclusions = document.RootElement.GetProperty("exclusions");

        Assert.Equal(JsonValueKind.Null, exclusions.GetProperty("statement").ValueKind);
        Assert.Equal(JsonValueKind.Null, exclusions.GetProperty("excluded_by").ValueKind);
        Assert.Empty(exclusions.GetProperty("probes").EnumerateArray());
        Assert.Empty(exclusions.GetProperty("steps").EnumerateArray());
    }

    // Program

    private static async Task<(int Exit, string Output, string Error)> Run(params string[] arguments)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await LicensedProbeProgram.RunAsync(arguments, output, error,
            static _ => throw new InvalidOperationException("No transport may be created."), Clock, CancellationToken.None);
        return (exit, output.ToString(), error.ToString());
    }

    [Fact]
    public async Task TheDryRunCommandPrintsTheExclusion()
    {
        var (exit, output, _) = await Run("--phase", "public-api", "--dry-run", "--exclude-probe", "11");

        Assert.Equal(0, exit);
        Assert.Contains(Statement11, output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("worker", "2", "--exclude-probe 17")]
    [InlineData("public-api", "17", "no steps in the public-api phase")]
    [InlineData("worker", "11", "no steps in the worker phase")]
    [InlineData("public-api", "24", "not a probe in the approved list")]
    public async Task ARefusedExclusionExitsWithAUsageErrorBeforeAnyTransport(string phase, string probe, string message)
    {
        var (exit, output, error) = await Run("--phase", phase, "--dry-run", "--exclude-probe", probe);

        Assert.Equal(2, exit);
        Assert.Empty(output);
        Assert.Contains(message, error, StringComparison.Ordinal);
    }
}
