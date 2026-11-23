namespace Briosa.LicensedProbes.Tests;

public sealed class ProbeOptionsTests
{
    private static readonly string[] PublicLicensed =
        ["--phase", "public-api", ProbeTarget.ConfirmFlag, "--fixtures", "m.json", "--output-directory", "out"];

    private static readonly string[] WorkerLicensed =
    [
        "--phase", "worker", ProbeTarget.ConfirmFlag, "--fixtures", "m.json", "--output-directory", "out",
        "--worker-path", @"C:\package\Briosa.Worker.exe",
        "--connected-sa-attested-version", ProbeTarget.SpatialAnalyzerTarget,
        "--connected-sa-attestation-reference", "sa-title-bar"
    ];

    [Fact]
    public void ADryRunNeedsNoConfirmationOrConnection()
    {
        var options = ProbeOptions.Parse(["--phase", "worker", "--dry-run"]);

        Assert.True(options.DryRun);
        Assert.Equal(ProbePhase.Worker, options.Phase);
        Assert.Null(options.Address);
        Assert.Null(options.WorkerPath);
    }

    [Theory]
    [InlineData("--phase", "worker", "--dry-run", ProbeTarget.ConfirmFlag)]
    [InlineData("--phase", "public-api", "--dry-run", "--address", "http://127.0.0.1:50051")]
    [InlineData("--phase", "worker", "--dry-run", "--worker-path", "Briosa.Worker.exe")]
    public void ADryRunRefusesConnectionOptions(params string[] arguments)
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(arguments));
    }

    [Fact]
    public void ALicensedSessionRequiresTheExactTargetConfirmation()
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(PublicLicensed.Where(static a => a != ProbeTarget.ConfirmFlag).ToArray()));
        var options = ProbeOptions.Parse(PublicLicensed);
        Assert.Equal(new Uri("http://127.0.0.1:50051"), options.Address);
    }

    [Theory]
    [InlineData("--confirm-licensed-sa1999")]
    [InlineData("--confirm-licensed-sa")]
    [InlineData("--confirm-licensed-saX")]
    public void AnotherTargetsConfirmationIsRefused(string flag)
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, flag]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(["--phase", "worker", "--dry-run", flag]));
    }

    [Theory]
    [InlineData("http://192.0.2.10:50051")]
    [InlineData("https://127.0.0.1:50051")]
    public void ThePublicAddressMustBeLoopbackHttp(string address)
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, "--address", address]));
    }

    [Fact]
    public void TheWorkerPhaseRequiresAnExactConnectedSaAttestation()
    {
        var options = ProbeOptions.Parse(WorkerLicensed);
        Assert.Equal("sa-title-bar", options.ConnectedSaAttestationReference);

        var wrongVersion = WorkerLicensed.Select(static a => a == ProbeTarget.SpatialAnalyzerTarget ? "1.0.0.0" : a).ToArray();
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(wrongVersion));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(WorkerLicensed.Take(WorkerLicensed.Length - 2).ToArray()));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(WorkerLicensed.Select(static a => a == "sa-title-bar" ? @"C:\evidence\file" : a).ToArray()));
    }

    [Theory]
    [InlineData("SENTINEL-PC")]
    [InlineData("alice_smith")]
    [InlineData("probe-s3-2026-sa-titlebar")]
    [InlineData("SA-TITLE-BAR")]
    public void AnAttestationReferenceIsOnlyAPredefinedEvidenceAlias(string reference)
    {
        // Well formed is not enough: a machine, account or free-form name is refused.
        var exception = Assert.Throws<ProbeUsageException>(() =>
            ProbeOptions.Parse(WorkerLicensed.Select(a => a == "sa-title-bar" ? reference : a).ToArray()));
        Assert.Contains("predefined evidence aliases", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(reference, exception.Message, StringComparison.Ordinal);
        Assert.All(ProbeOptions.AttestationAliases, static alias => Assert.True(ProbeOptions.IsSafeReference(alias)));
    }

    [Fact]
    public void PhaseSpecificOptionsCannotBeMixed()
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. WorkerLicensed, "--address", "http://127.0.0.1:50051"]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, "--worker-path", "Briosa.Worker.exe"]));
    }

    [Fact]
    public void UnknownRepeatedAndMissingOptionsAreRefused()
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, "--retry"]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, "--fixtures", "other.json"]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(["--dry-run"]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(["--phase", "both", "--dry-run"]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(["--phase"]));
    }

    [Theory]
    [InlineData("--step-timeout-seconds", "4")]
    [InlineData("--step-timeout-seconds", "601")]
    [InlineData("--operator-timeout-seconds", "29")]
    [InlineData("--harness-revision", "ABC")]
    public void BoundedValuesAreValidated(string option, string value)
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, option, value]));
    }

    [Theory]
    [InlineData("manual", Briosa.SaInteractionMode.Manual)]
    [InlineData("automatic", Briosa.SaInteractionMode.Automatic)]
    public void TheRestoreModeIsParsedAsSupplied(string value, Briosa.SaInteractionMode expected)
    {
        var options = ProbeOptions.Parse([.. PublicLicensed, ProbeOptions.RestoreModeOption, value]);

        Assert.Equal(expected, options.RestoreSaInteractionMode);
        Assert.Equal(new InteractionModeSettings(expected, Supplied: true), options.InteractionMode);
        Assert.False(ProbeOptions.Parse(PublicLicensed).InteractionMode.Supplied);
    }

    [Theory]
    [InlineData("silent")]
    [InlineData("Manual")]
    [InlineData("0")]
    [InlineData("")]
    public void ARestoreModeOtherThanManualOrAutomaticIsRefused(string value)
    {
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, ProbeOptions.RestoreModeOption, value]));
    }

    [Fact]
    public void TheRestoreOnlyPlanNeedsARestoreModeAndTakesNoExclusion()
    {
        Assert.True(ProbeOptions.Parse([.. WorkerLicensed, ProbeOptions.RestoreOnlyFlag, ProbeOptions.RestoreModeOption, "manual"])
            .InteractionModeRestoreOnly);
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse([.. PublicLicensed, ProbeOptions.RestoreOnlyFlag]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(
            [.. PublicLicensed, ProbeOptions.RestoreOnlyFlag, ProbeOptions.RestoreModeOption, "manual", "--exclude-probe", "12"]));
        Assert.Throws<ProbeUsageException>(() => ProbeOptions.Parse(
            [.. PublicLicensed, ProbeOptions.RestoreOnlyFlag, ProbeOptions.RestoreOnlyFlag, ProbeOptions.RestoreModeOption, "manual"]));
    }

    [Fact]
    public void TimeoutsAreParsed()
    {
        var options = ProbeOptions.Parse([.. PublicLicensed, "--step-timeout-seconds", "30", "--operator-timeout-seconds", "600"]);

        Assert.Equal(TimeSpan.FromSeconds(30), options.StepTimeout);
        Assert.Equal(TimeSpan.FromSeconds(600), options.OperatorTimeout);
    }
}
