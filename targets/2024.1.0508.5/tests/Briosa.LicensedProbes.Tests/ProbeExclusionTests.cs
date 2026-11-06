using System.Globalization;
using System.Text.Json;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

public sealed class ProbeExclusionTests
{
    private const string Statement2 = "Excluded by operator: #2 — reason not recorded by harness";

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

        var options = ProbeOptions.Parse(["--phase", phase, "--exclude-probe", "12", .. mode, "--exclude-probe", "3"]);

        Assert.Equal([3, 12], options.ExcludedProbes);
        Assert.Empty(ProbeOptions.Parse(["--phase", phase, .. mode]).ExcludedProbes);
    }

    [Fact]
    public void ADuplicateExclusionIsRefused()
    {
        var exception = Assert.Throws<ProbeUsageException>(() =>
            ProbeOptions.Parse(["--phase", "public-api", "--dry-run", "--exclude-probe", "12", "--exclude-probe", "12"]));

        Assert.Contains("more than once", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("33")]
    [InlineData("99")]
    public void AnUnknownProbeNumberIsRefused(string probe)
    {
        var exception = Assert.Throws<ProbeUsageException>(() =>
            ProbeOptions.Parse(["--phase", "public-api", "--dry-run", "--exclude-probe", probe]));

        Assert.Contains("not a probe in the approved list", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("012")]
    [InlineData("+12")]
    [InlineData("-12")]
    [InlineData(" 12")]
    [InlineData("12.0")]
    [InlineData("5-6")]
    [InlineData("p12")]
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
    [InlineData(true, 12)]
    [InlineData(true, 23)]
    public void AProbeOutsideTheSelectedPhaseIsRefused(bool worker, int probe)
    {
        var phase = Phase(worker);
        var exception = Assert.Throws<ProbeUsageException>(() => Plan(phase, probe));

        Assert.Contains($"#{probe} has no steps in the {ProbePlan.PhaseName(phase)} phase", exception.Message, StringComparison.Ordinal);
    }

    // Plan filtering

    [Fact]
    public void ExcludingProbe2RemovesOnlyItsProbeAndCheckSteps()
    {
        var full = Plan(ProbePhase.PublicApi);
        var plan = Plan(ProbePhase.PublicApi, 2);

        Assert.Equal(["c02-value", "p02-value"], plan.ExcludedStepIds.Order(StringComparer.Ordinal));
        Assert.Equal([2], plan.ExcludedProbes);
        Assert.Equal(Ids(full.Steps), Ids(plan.CatalogSteps));
        Assert.Equal(Ids(full.Steps).Where(static id => id is not ("p02-value" or "c02-value")), Ids(plan.Steps));
        Assert.DoesNotContain("2", plan.ProbesCovered());

        // The fit group's fixture steps stay with the probe that used them.
        Assert.All(ProbeCatalog.FitPoints, point => Assert.Contains($"s-point-{FixtureNames.GroupFit}-{point.Name}", Ids(plan.Steps)));

        // Only probe 32's partial-success fit still sends Fit Geometry to Point Group.
        Assert.Equal(["p32-partial"], plan.Steps
            .Where(static step => step.Operation.FullyQualifiedMethod == ProbeOperations.FitGeometryToPointGroup.FullyQualifiedMethod)
            .Select(static step => step.Id));
    }

    [Theory]
    [InlineData(false, new[] { 5, 6 })]
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
        Assert.Contains("prerequisites (run earlier): p02-shipped", Plan(ProbePhase.Worker).RenderDryRun(), StringComparison.Ordinal);
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
        var plan = Plan(ProbePhase.PublicApi, 2);

        var text = plan.RenderDryRun();

        Assert.Contains(Statement2 + ".", text, StringComparison.Ordinal);
        Assert.Contains("Excluded steps (never sent): p02-value, c02-value.", text, StringComparison.Ordinal);
        Assert.Contains("[---] p02-value (probe, probe 2)", text, StringComparison.Ordinal);
        Assert.Contains("[---] c02-value (check, probe 2)", text, StringComparison.Ordinal);
        Assert.Contains("Excluded by operator; never sent.", text, StringComparison.Ordinal);
        Assert.Contains(string.Create(CultureInfo.InvariantCulture, $"[{plan.Steps.Count:D3}]"), text, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Create(CultureInfo.InvariantCulture, $"[{plan.Steps.Count + 1:D3}]"), text, StringComparison.Ordinal);
        Assert.DoesNotContain("Excluded by operator", Plan(ProbePhase.PublicApi).RenderDryRun(), StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExcludedStepsAreNeverSentAndTheRemainingPlanCompletes()
    {
        var plan = Plan(ProbePhase.PublicApi, 2);
        var transport = new FakeTransport(ProbePhase.PublicApi, TestSupport.Succeeding);

        var record = await ProbeSession.RunAsync(plan, transport, Clock, SessionIdentity.None, new FakeOperator(), CancellationToken.None);

        Assert.True(record.Completed);
        Assert.Equal(Ids(plan.Steps), transport.Executed);
        Assert.DoesNotContain("p02-value", transport.Executed);
        Assert.Equal(Ids(plan.CatalogSteps), record.Steps.Select(static step => step.Step.Id));
        Assert.Equal(StepClassification.Excluded, record.Steps.Single(static step => step.Step.Id == "p02-value").Classification);
        Assert.Equal([2], record.ExcludedProbes);
    }

    [Fact]
    public async Task RecordsDistinguishExcludedFromNotRun()
    {
        var plan = Plan(ProbePhase.PublicApi, 2);
        var transport = new FakeTransport(ProbePhase.PublicApi, step => step.Id == "p12"
            ? ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure")
            : TestSupport.Succeeding(step));
        var record = await ProbeSession.RunAsync(plan, transport, Clock, SessionIdentity.None, new FakeOperator(), CancellationToken.None);

        using var document = JsonDocument.Parse(ProbeRecorder.ToJson(record));
        var root = document.RootElement;
        var exclusions = root.GetProperty("exclusions");
        var steps = root.GetProperty("steps").EnumerateArray()
            .ToDictionary(static step => step.GetProperty("id").GetString()!, static step => step.GetProperty("classification").GetString());
        var markdown = ProbeRecorder.ToMarkdown(record);

        Assert.Equal(ProbeRecorder.SchemaVersion, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(Statement2, exclusions.GetProperty("statement").GetString());
        Assert.Equal("operator", exclusions.GetProperty("excluded_by").GetString());
        Assert.False(exclusions.GetProperty("reason_recorded_by_harness").GetBoolean());
        Assert.Equal([2], exclusions.GetProperty("probes").EnumerateArray().Select(static probe => probe.GetInt32()));
        Assert.Equal(["p02-value", "c02-value"], exclusions.GetProperty("steps").EnumerateArray().Select(static step => step.GetString()));
        Assert.Equal("excluded", steps["p02-value"]);
        Assert.Equal("excluded", steps["c02-value"]);
        Assert.Equal("unexpected", steps["p12"]);
        Assert.Equal("not-run", steps["p13"]);

        Assert.Contains($"**{Statement2}.**", markdown, StringComparison.Ordinal);
        Assert.Contains("justification for each exclusion must be stated in the committed evidence", markdown, StringComparison.Ordinal);
        Assert.Contains("| `p02-value` | 2 |", markdown, StringComparison.Ordinal);
        Assert.Contains("Excluded by operator; not sent.", markdown, StringComparison.Ordinal);
        var notRun = markdown.Split('\n').Single(static line => line.StartsWith("Not run:", StringComparison.Ordinal));
        Assert.Contains("`p13`", notRun, StringComparison.Ordinal);
        Assert.DoesNotContain("`p02-value`", notRun, StringComparison.Ordinal);
        Assert.DoesNotContain("`c02-value`", notRun, StringComparison.Ordinal);
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
            static _ => throw new InvalidOperationException("No transport may be created."), FakeOperator.Factory, Clock, CancellationToken.None);
        return (exit, output.ToString(), error.ToString());
    }

    [Fact]
    public async Task TheDryRunCommandPrintsTheExclusion()
    {
        var (exit, output, _) = await Run("--phase", "public-api", "--dry-run", "--exclude-probe", "2");

        Assert.Equal(0, exit);
        Assert.Contains(Statement2, output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("worker", "2", "--exclude-probe 17")]
    [InlineData("public-api", "17", "no steps in the public-api phase")]
    [InlineData("worker", "12", "no steps in the worker phase")]
    [InlineData("public-api", "33", "not a probe in the approved list")]
    [InlineData("public-api", "26", "names an opt-in probe")]
    [InlineData("worker", "28", "names an opt-in probe")]
    [InlineData("public-api", "7", "names a retired probe")]
    [InlineData("public-api", "11", "names a retired probe")]
    [InlineData("worker", "14", "names a retired probe")]
    [InlineData("public-api", "15", "names a retired probe")]
    [InlineData("public-api", "22", "names a retired probe")]
    public async Task ARefusedExclusionExitsWithAUsageErrorBeforeAnyTransport(string phase, string probe, string message)
    {
        var (exit, output, error) = await Run("--phase", phase, "--dry-run", "--exclude-probe", probe);

        Assert.Equal(2, exit);
        Assert.Empty(output);
        Assert.Contains(message, error, StringComparison.Ordinal);
    }
}
