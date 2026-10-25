namespace Briosa.LicensedProbes.Tests;

public sealed class ProbeCatalogTests
{
    private static ProbePlan Plan(ProbePhase phase) => ProbePlan.Create(phase, TestSupport.SentinelManifest());

    [Fact]
    public void BothPhasesTogetherCoverEveryApprovedProbe()
    {
        var covered = Plan(ProbePhase.PublicApi).ProbesCovered()
            .Concat(Plan(ProbePhase.Worker).ProbesCovered())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

        Assert.Equal(ProbeCatalog.ActiveProbeNumbers.Order(StringComparer.Ordinal), covered);
    }

    [Fact]
    public void RetiredProbesHaveNoStepsInEitherPhase()
    {
        Assert.Equal(["7", "8", "9", "10", "14", "15", "16"], ProbeCatalog.RetiredProbeNumbers);
        Assert.All(ProbeCatalog.RetiredProbeNumbers, probe => Assert.Contains(probe, ProbeCatalog.ProbeNumbers));
        foreach (var phase in new[] { ProbePhase.PublicApi, ProbePhase.Worker })
        {
            Assert.DoesNotContain(Plan(phase).Steps, step =>
                ProbePlan.ExpandProbe(step.Probe).Intersect(ProbeCatalog.RetiredProbeNumbers, StringComparer.Ordinal).Any());
        }
    }

    [Fact]
    public void ThePublicPhaseSendsOnlyShippedSequences()
    {
        Assert.All(Plan(ProbePhase.PublicApi).Steps, step => Assert.Equal(VariantKind.Shipped, step.Variant.Kind));
    }

    [Fact]
    public void TheWorkerPhaseIsUnattendedAndNeverDestructive()
    {
        Assert.All(Plan(ProbePhase.Worker).Steps, step =>
        {
            Assert.False(step.Operation.IsDestructive);
            Assert.Null(step.DestructiveTarget);
            Assert.False(step.RequiresOperator);
        });
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("6")]
    public void OmitAndBlankProbesHaveAPublicValueAndWorkerControls(string probe)
    {
        var worker = Plan(ProbePhase.Worker).Steps.Where(step => ProbePlan.ExpandProbe(step.Probe).Contains(probe)).ToList();
        var publicSteps = Plan(ProbePhase.PublicApi).Steps.Where(step => ProbePlan.ExpandProbe(step.Probe).Contains(probe)).ToList();

        Assert.Contains(worker, static step => step.Variant.Kind == VariantKind.OmitArgument);
        Assert.Contains(worker, static step => step.Variant.Kind == VariantKind.BlankArgument);
        Assert.Contains(worker, static step => step.Kind == ProbeStepKind.Probe && step.Variant.Kind == VariantKind.Shipped &&
            step.Acceptable == ProbeOutcomes.Succeeded);
        Assert.Contains(publicSteps, static step => step.Kind == ProbeStepKind.Probe);
    }

    [Fact]
    public void OmittedAndBlankedLabelsAreTheShippedOptionalLabels()
    {
        var labels = Plan(ProbePhase.Worker).Steps
            .Where(static step => step.Variant.Kind is VariantKind.OmitArgument or VariantKind.BlankArgument)
            .Select(static step => step.Variant.ArgumentLabel!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

        Assert.Equal(
            new[]
            {
                ProbeCatalog.NominalsLabel, ProbeCatalog.SeedGeometryLabel, ProbeCatalog.TemplateChartLabel,
                ProbeCatalog.ProfileFileLabel, ProbeCatalog.HighPlaneLabel, ProbeCatalog.LowPlaneLabel
            }.Order(StringComparer.Ordinal),
            labels);
    }

    [Theory]
    [InlineData("19")]
    [InlineData("20")]
    [InlineData("21")]
    public void ExactlyOneStepTextOfEachPairIsShippedByThisTarget(string probe)
    {
        var pair = Plan(ProbePhase.Worker).Steps.Where(step => step.Probe == probe).ToList();

        Assert.Equal(2, pair.Count);
        Assert.All(pair, static step => Assert.Equal(VariantKind.ReplaceStepText, step.Variant.Kind));
        Assert.Single(pair, static step => string.Equals(step.Command.StepName, step.ShippedCommand.StepName, StringComparison.Ordinal));
        Assert.Equal(
            pair[0].ShippedCommand.InputArguments.Select(static argument => (argument.Name, argument.SdkBinding)),
            pair[1].Command.InputArguments.Select(static argument => (argument.Name, argument.SdkBinding)));
    }

    [Fact]
    public void StepTextPairsDifferOnlyByTheReviewedSdkAndDocumentationForms()
    {
        Assert.Equal(ProbeCatalog.OrientationDocumentationStep, ProbeCatalog.OrientationSdkStep.Replace(",", ", ", StringComparison.Ordinal));
        Assert.Equal(ProbeCatalog.AngleDocumentationStep, ProbeCatalog.AngleSdkStep.Replace('\'', '\u2019'));
        Assert.Equal(ProbeCatalog.ShowHideDocumentationStep, ProbeCatalog.ShowHideSdkStep.Replace(" / ", "/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("17", ProbeCatalog.GroupToFitLabel, ProbeCatalog.GroupToFitRecased)]
    [InlineData("18", ProbeCatalog.NumberSuffixLabel, ProbeCatalog.NumberSuffixRecased)]
    public void CaseProbesRecaseAnExactShippedLabel(string probe, string exact, string recased)
    {
        var step = Assert.Single(Plan(ProbePhase.Worker).Steps, step => step.Probe == probe && step.Variant.Kind == VariantKind.RecaseArgumentLabel);

        Assert.Contains(step.ShippedCommand.InputArguments, argument => argument.Name == exact);
        Assert.Contains(step.Command.InputArguments, argument => argument.Name == recased);
        Assert.DoesNotContain(step.Command.InputArguments, argument => argument.Name == exact);
    }

    [Fact]
    public void OnlyTheTwoR0DeletesAreDestructive()
    {
        // Probes 15 and 16 were retired with their operation (#307). The only destructive
        // steps are the #307 re-probe controls R0 and R0a, each on its own disposable cloud.
        var destructive = Plan(ProbePhase.PublicApi).Steps.Where(static step => step.Operation.IsDestructive).ToList();

        Assert.Equal(["p24-r0", "p24-r0a"], destructive.Select(static step => step.Id));
        Assert.Equal(
            [FixtureNames.CloudKey(FixtureNames.DeleteCloudR0), FixtureNames.CloudKey(FixtureNames.DeleteCloudR0a)],
            destructive.Select(static step => step.DestructiveTarget));
        Assert.DoesNotContain(Plan(ProbePhase.Worker).Steps, static step => step.Operation.IsDestructive || step.DestructiveTarget is not null);
    }

    [Fact]
    public void TheRelationshipFixtureYieldsAFractionalInTolerancePercentage()
    {
        var deviations = ProbeCatalog.MeasuredPoints.Zip(ProbeCatalog.GroupOnePoints)
            .Select(static pair => Math.Sqrt(Math.Pow(pair.First.X - pair.Second.X, 2) + Math.Pow(pair.First.Y - pair.Second.Y, 2) +
                Math.Pow(pair.First.Z - pair.Second.Z, 2)))
            .ToList();
        var inTolerance = deviations.Count(static deviation => deviation <= 1);

        Assert.Equal(1, inTolerance);
        Assert.True(ProbeOperations.IsFractional(100.0 * inTolerance / deviations.Count));
        Assert.False(ProbeOperations.IsFractional(100));
    }

    [Fact]
    public void RotatedGroupIsARigidOneDegreeRotation()
    {
        foreach (var (original, rotated) in ProbeCatalog.GroupOnePoints.Zip(ProbeCatalog.RotatedPoints))
        {
            Assert.Equal(original.Name, rotated.Name);
            Assert.Equal(Math.Sqrt((original.X * original.X) + (original.Y * original.Y)),
                Math.Sqrt((rotated.X * rotated.X) + (rotated.Y * rotated.Y)), 9);
            Assert.Equal(original.Z, rotated.Z);
        }
    }

    [Fact]
    public void ManualFixturesNeverUseTheHarnessCollection()
    {
        var manifest = TestSupport.SentinelManifest() with
        {
            UsmnNominalsGroup = new ManifestObject { Collection = "b277", Name = "Nominals" }
        };

        Assert.Throws<InvalidDataException>(manifest.Validate);
    }

    [Fact]
    public void ManifestParsingIsStrictAboutShapeAndTarget()
    {
        const string valid = """
            {
              "schema_version": 1,
              "spatial_analyzer_target": "2026.1.0529.7",
              "usmn_instruments": [ { "collection": "M", "instrument_id": 0 }, { "collection": "M", "instrument_id": 1 } ],
              "usmn_nominals_group": { "collection": "M", "name": "Nominals" },
              "template_chart_name": "Template",
              "ui_profile_file": "C:\\fixtures\\profile.xml"
            }
            """;

        var manifest = FixtureManifest.Parse(valid);

        Assert.Equal(2, manifest.UsmnInstruments.Count);
        Assert.Equal("Default", manifest.UiProfileName);
        Assert.False(manifest.IsPlaceholder);
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(valid.Replace("2026.1.0529.7", "1.0.0.0", StringComparison.Ordinal)));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => FixtureManifest.Parse(valid.Replace("\"template_chart_name\"", "\"unknown\": 1, \"template_chart_name\"", StringComparison.Ordinal)));
        Assert.Throws<InvalidDataException>(() => FixtureManifest.Parse(valid.Replace("C:\\\\fixtures\\\\profile.xml", "profile.xml", StringComparison.Ordinal)));
    }
}
