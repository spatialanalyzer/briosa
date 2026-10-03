using System.Text.Json;

namespace Briosa.LicensedProbes.Tests;

public sealed class LicensedProbeProgramTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "briosa-probe-program-" + Guid.NewGuid().ToString("N"));

    public LicensedProbeProgramTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static IProbeTransport Forbidden(ProbeOptions options) =>
        throw new InvalidOperationException("No transport may be created.");

    private string Manifest()
    {
        var profile = Path.Combine(_root, "profile.xml");
        File.WriteAllText(profile, "<profile />");
        var manifest = Path.Combine(_root, "manifest.json");
        File.WriteAllText(manifest, JsonSerializer.Serialize(new
        {
            schema_version = 1,
            spatial_analyzer_target = ProbeTarget.SpatialAnalyzerTarget,
            usmn_instruments = new[] { new { collection = "SENTINEL-M", instrument_id = 0 }, new { collection = "SENTINEL-M", instrument_id = 1 } },
            usmn_nominals_group = new { collection = "SENTINEL-M", name = "SENTINEL-NOMINALS" },
            template_chart_name = "SENTINEL-CHART",
            ui_profile_file = profile
        }));
        return manifest;
    }

    private static async Task<(int Exit, string Output, string Error)> Run(Func<ProbeOptions, IProbeTransport> factory, params string[] arguments)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await LicensedProbeProgram.RunAsync(arguments, output, error, factory, new FixedClock(), CancellationToken.None);
        return (exit, output.ToString(), error.ToString());
    }

    [Theory]
    [InlineData("public-api")]
    [InlineData("worker")]
    public async Task ADryRunPrintsThePlanAndNeverCreatesATransport(string phase)
    {
        var (exit, output, _) = await Run(Forbidden, "--phase", phase, "--dry-run", "--fixtures", Manifest());

        Assert.Equal(0, exit);
        Assert.Contains("DRY RUN: nothing was started or connected.", output, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSupport.Sentinel, output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADryRunCanWritePlanRecords()
    {
        var output = Path.Combine(_root, "plan");

        var (exit, _, _) = await Run(Forbidden, "--phase", "worker", "--dry-run", "--output-directory", output);

        Assert.Equal(0, exit);
        Assert.Equal(2, Directory.GetFiles(output, "probe-277-worker-*-dry-run.*").Length);
    }

    [Fact]
    public async Task UsageErrorsExitBeforeAnyTransport()
    {
        var (exit, _, error) = await Run(Forbidden, "--phase", "public-api", "--fixtures", Manifest(), "--output-directory", Path.Combine(_root, "o"));

        Assert.Equal(2, exit);
        Assert.Contains(ProbeTarget.ConfirmFlag, error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ANonEmptyOutputDirectoryIsRefusedBeforeAnyTransport()
    {
        var output = Path.Combine(_root, "existing");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "keep.txt"), "x");

        var (exit, _, _) = await Run(Forbidden, "--phase", "public-api", ProbeTarget.ConfirmFlag, "--fixtures", Manifest(), "--output-directory", output);

        Assert.Equal(2, exit);
    }

    [Fact]
    public async Task AMissingProfileFileIsRefusedBeforeAnyTransport()
    {
        var manifest = Manifest();
        File.Delete(Path.Combine(_root, "profile.xml"));

        var (exit, _, error) = await Run(Forbidden, "--phase", "public-api", ProbeTarget.ConfirmFlag, "--fixtures", manifest,
            "--output-directory", Path.Combine(_root, "o"));

        Assert.Equal(2, exit);
        Assert.DoesNotContain(_root, error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ALicensedSessionWritesRecordsAndReportsCompletion()
    {
        var output = Path.Combine(_root, "records");
        FakeTransport? transport = null;

        var (exit, console, _) = await Run(options =>
            {
                Assert.Equal(ProbePhase.PublicApi, options.Phase);
                transport = new FakeTransport(options.Phase, TestSupport.Succeeding);
                return transport;
            },
            "--phase", "public-api", ProbeTarget.ConfirmFlag, "--fixtures", Manifest(), "--output-directory", output);

        Assert.Equal(0, exit);
        Assert.NotNull(transport);
        Assert.True(transport.Disposed);
        Assert.Contains("completed=True", console, StringComparison.Ordinal);
        var files = Directory.GetFiles(output);
        Assert.Equal(2, files.Length);
        Assert.All(files, file => Assert.DoesNotContain(TestSupport.Sentinel, File.ReadAllText(file), StringComparison.Ordinal));
    }

    [Fact]
    public async Task AStoppedSessionExitsNonZero()
    {
        var (exit, console, _) = await Run(options => new FakeTransport(options.Phase, step => step.Id == "p02-value"
                ? ProbeOutcome.Unknown("grpc:DeadlineExceeded", "untyped-rpc-failure")
                : TestSupport.Succeeding(step)),
            "--phase", "public-api", ProbeTarget.ConfirmFlag, "--fixtures", Manifest(), "--output-directory", Path.Combine(_root, "stopped"));

        Assert.Equal(1, exit);
        Assert.Contains("stopped_at=p02-value", console, StringComparison.Ordinal);
    }
}
