using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

public sealed class ProbePlanTests
{
    private static ProbeStep Count(string id, ProbePhase phase) =>
        new(id, "setup", ProbeStepKind.Guard, phase, "count", ProbeOperations.GetNumberOfCollections,
            new Api.GetNumberOfCollectionsRequest(), CommandVariant.Shipped, ProbeOutcomes.Succeeded);

    private static ProbeStep CreateCloud(string cloud, string? key) =>
        new($"s-{cloud}", "setup", ProbeStepKind.Setup, ProbePhase.PublicApi, "cloud", ProbeOperations.ConstructCloudFromGroup,
            new Api.ConstructPointCloudsFromExistingPointGroupRequest
            {
                PointGroupName = new Api.CollectionObjectName { CollectionName = FixtureNames.Collection, ObjectName = "G", ObjectType = Api.ObjectType.PointGroup },
                CloudName = new Api.CollectionObjectName { CollectionName = FixtureNames.Collection, ObjectName = cloud, ObjectType = Api.ObjectType.Cloud }
            },
            CommandVariant.Shipped, ProbeOutcomes.Succeeded)
        {
            CreatesFixture = key
        };

    private static ProbeStep Delete(string collection, string cloud, string? target, ProbePhase phase = ProbePhase.PublicApi)
    {
        var request = new Api.DeleteCloudPointsByXYZRangeRequest { DeleteInside = true, XMin = 1, XMax = 2 };
        request.CloudNames.Add(new Api.CollectionObjectName { CollectionName = collection, ObjectName = cloud, ObjectType = Api.ObjectType.Cloud });
        return new ProbeStep("p-delete", "15", ProbeStepKind.Probe, phase, "delete", ProbeOperations.DeleteCloudPointsByXyzRange,
            request, CommandVariant.Shipped, ProbeOutcomes.AnyDeterminateSdkOutcome)
        {
            DestructiveTarget = target
        };
    }

    [Fact]
    public void BothPhasePlansValidate()
    {
        Assert.NotEmpty(ProbePlan.Create(ProbePhase.PublicApi, FixtureManifest.Placeholder).Steps);
        Assert.NotEmpty(ProbePlan.Create(ProbePhase.Worker, FixtureManifest.Placeholder).Steps);
    }

    [Fact]
    public void TheDeterminateSetHoldsExactlyTheDeterminateKinds()
    {
        Assert.Equal(
            ProbeOutcomes.Succeeded | ProbeOutcomes.MpFailed | ProbeOutcomes.ArgumentRejected | ProbeOutcomes.OutputRetrievalFailed,
            ProbeOutcomes.AnyDeterminateSdkOutcome);
        Assert.Equal(
            [ProbeOutcomeKind.ExecuteStepRejected, ProbeOutcomeKind.MpResultUnavailable, ProbeOutcomeKind.Indeterminate],
            Enum.GetValues<ProbeOutcomeKind>().Where(static kind => kind.IsCompletionUnknown()));
        Assert.All(Enum.GetValues<ProbeOutcomeKind>().Where(static kind => kind.IsCompletionUnknown()),
            static kind => Assert.Equal(ProbeOutcomes.None, kind.ToFlag()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoPlannedStepAcceptsAnOutcomeWhoseCompletionIsUnknown(bool worker)
    {
        var phase = worker ? ProbePhase.Worker : ProbePhase.PublicApi;
        var unknown = Enum.GetValues<ProbeOutcomeKind>().Where(static kind => kind.IsCompletionUnknown()).ToList();
        Assert.Contains(ProbeOutcomeKind.ExecuteStepRejected, unknown);
        Assert.Contains(ProbeOutcomeKind.MpResultUnavailable, unknown);

        foreach (var step in ProbePlan.Create(phase, FixtureManifest.Placeholder).Steps)
        {
            Assert.Equal(ProbeOutcomes.None, step.Acceptable & ~ProbeOutcomes.AnyDeterminateSdkOutcome);
            foreach (var kind in unknown)
            {
                foreach (var disposition in new[] { "Completed", ProbeOutcome.StartedOutcomeUnknown, null })
                {
                    var outcome = TestSupport.Outcome(ProbeOutcomeKind.Succeeded) with { Kind = kind, ExecutionDisposition = disposition };
                    Assert.False(step.Acceptable.Accepts(outcome), $"{step.Id} accepts {kind} ({disposition ?? "no disposition"})");
                }
            }
        }
    }

    [Fact]
    public void ValidationRefusesAStepThatAcceptsAnOutcomeOutsideTheDeterminateSet()
    {
        var undefined = Count("undefined", ProbePhase.PublicApi) with { Acceptable = ProbeOutcomes.Succeeded | (ProbeOutcomes)16 };

        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [undefined]));
    }

    [Fact]
    public void DryRunNamesTheDeterminateOutcomeSet()
    {
        var text = ProbePlan.Create(ProbePhase.Worker, FixtureManifest.Placeholder).RenderDryRun();

        Assert.Contains("accept: any determinate SDK outcome (Succeeded, MpFailed, ArgumentRejected, or OutputRetrievalFailed)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteStepRejected", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MpResultUnavailable", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationRefusesDuplicateIdentifiers()
    {
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi,
            [Count("same", ProbePhase.PublicApi), Count("same", ProbePhase.PublicApi)]));
    }

    [Fact]
    public void ValidationRefusesVariantsInThePublicPhase()
    {
        var varied = Count("varied", ProbePhase.PublicApi) with { Kind = ProbeStepKind.Probe, Variant = CommandVariant.StepText("Other") };

        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi, [varied]));
    }

    [Fact]
    public void ValidationRefusesVariantsOutsideProbesAndControlsThatAcceptFailure()
    {
        var setupVariant = Count("guard", ProbePhase.Worker) with { Variant = CommandVariant.StepText("Other") };
        var narrowVariant = Count("probe", ProbePhase.Worker) with { Kind = ProbeStepKind.Probe, Variant = CommandVariant.StepText("Other") };

        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.Worker, [setupVariant]));
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.Worker, [narrowVariant]));
    }

    [Fact]
    public void ValidationRefusesStepsFromAnotherPhase()
    {
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.Worker, [Count("x", ProbePhase.PublicApi)]));
    }

    [Fact]
    public void ValidationAcceptsADeleteOfAnEarlierHarnessCloud()
    {
        ProbePlan.Validate(ProbePhase.PublicApi,
            [CreateCloud("C1", "cloud:C1"), Delete(FixtureNames.Collection, "C1", "cloud:C1")]);
    }

    [Fact]
    public void ValidationRefusesADeleteWithoutAnEarlierCreation()
    {
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi,
            [Delete(FixtureNames.Collection, "C1", "cloud:C1"), CreateCloud("C1", "cloud:C1")]));
    }

    [Fact]
    public void ValidationRefusesADeleteWithoutANamedTarget()
    {
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi,
            [CreateCloud("C1", "cloud:C1"), Delete(FixtureNames.Collection, "C1", null)]));
    }

    [Theory]
    [InlineData("OTHER", "C1")]
    [InlineData(FixtureNames.Collection, "C2")]
    public void ValidationRefusesADeleteThatCouldReachAnotherObject(string collection, string cloud)
    {
        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.PublicApi,
            [CreateCloud("C1", "cloud:C1"), Delete(collection, cloud, "cloud:C1")]));
    }

    [Fact]
    public void ValidationRefusesAnyDestructiveStepInTheWorkerPhase()
    {
        var create = CreateCloud("C1", "cloud:C1") with { Phase = ProbePhase.Worker };

        Assert.Throws<InvalidOperationException>(() => ProbePlan.Validate(ProbePhase.Worker,
            [create, Delete(FixtureNames.Collection, "C1", "cloud:C1", ProbePhase.Worker)]));
    }

    [Fact]
    public void DryRunRendersSequencesWithoutManualValues()
    {
        var text = ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest()).RenderDryRun();

        Assert.Contains("DRY RUN", text, StringComparison.Ordinal);
        Assert.Contains("\"Starting Condition Geometry (optional)\" OMITTED (setter not called)", text, StringComparison.Ordinal);
        Assert.Contains("\"Nominals Group Name (blank for none)\" = BLANK", text, StringComparison.Ordinal);
        Assert.Contains("\"Group to Fit\" = value (recased from \"Group To Fit\")", text, StringComparison.Ordinal);
        Assert.Contains($"SetStep \"{ProbeCatalog.AngleDocumentationStep}\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, text, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicDryRunMarksDestructiveAndOperatorSteps()
    {
        var text = ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest()).RenderDryRun();

        Assert.Contains("DESTRUCTIVE: only the harness-created cloud:CDEL15", text, StringComparison.Ordinal);
        Assert.Contains("DESTRUCTIVE: only the harness-created cloud:CDEL16", text, StringComparison.Ordinal);
        Assert.Contains("OPERATOR:", text, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, text, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicPhaseNamesItsNonDefaultServerAdmission()
    {
        var plan = ProbePlan.Create(ProbePhase.PublicApi, TestSupport.SentinelManifest());

        var arguments = plan.ServerAdmissionArguments;

        Assert.Equal("--Briosa:Security:Operations:Profile=device", arguments[0]);
        Assert.Contains(
            "--Briosa:Security:Operations:Overrides:construction_operations:make_collection_vector_group_name_ref_list_runtime_select=allow",
            arguments);
        Assert.All(arguments.Skip(1), argument =>
        {
            Assert.StartsWith("--Briosa:Security:Operations:Overrides:", argument, StringComparison.Ordinal);
            Assert.EndsWith("=allow", argument, StringComparison.Ordinal);
        });
        var text = plan.RenderDryRun();
        Assert.Contains("Server admission: start Briosa.Server.exe with", text, StringComparison.Ordinal);
        Assert.All(arguments, argument => Assert.Contains(argument, text, StringComparison.Ordinal));
        Assert.DoesNotContain("Server admission", ProbePlan.Create(ProbePhase.Worker, TestSupport.SentinelManifest()).RenderDryRun(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void NoAdmissionSettingCanReachAnExclusiveWorkflow()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProbePlan.CreateServerAdmissionArguments(["/briosa.InstrumentOperations/StartInstrumentInterface"]));

        Assert.Contains("operation-isolation-unsupported", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("5-6", new[] { "5", "6" })]
    [InlineData("12", new[] { "12" })]
    [InlineData("setup", new string[0])]
    public void ProbeRangesExpand(string probe, string[] expected)
    {
        Assert.Equal(expected, ProbePlan.ExpandProbe(probe));
    }
}
