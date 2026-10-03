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
            request, CommandVariant.Shipped, ProbeOutcomes.AnyCompletedSdkOutcome)
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

    [Theory]
    [InlineData("5-6", new[] { "5", "6" })]
    [InlineData("12", new[] { "12" })]
    [InlineData("setup", new string[0])]
    public void ProbeRangesExpand(string probe, string[] expected)
    {
        Assert.Equal(expected, ProbePlan.ExpandProbe(probe));
    }
}
