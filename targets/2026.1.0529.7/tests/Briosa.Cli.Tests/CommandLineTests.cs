using Briosa.Cli.CommandLine;
using Briosa.Cli.Engine;

namespace Briosa.Cli.Tests;

public sealed class CommandLineTests
{
    [Theory]
    [InlineData("usage-unknown-command", "connect")]
    [InlineData("usage-unknown-command", "ops", "start")]
    [InlineData("usage-missing-command", "ops")]
    [InlineData("usage-unknown-option", "status", "--endpoint=http://127.0.0.1:1")]
    [InlineData("usage-unknown-option", "status", "--verbose")]
    [InlineData("usage-duplicate-option", "status", "--endpoint", "http://127.0.0.1:1", "--endpoint", "http://127.0.0.1:2")]
    [InlineData("usage-duplicate-option", "ops", "search", "--include-denied", "--include-denied")]
    [InlineData("usage-missing-value", "ops", "search", "--query")]
    [InlineData("usage-missing-argument", "ops", "describe")]
    [InlineData("usage-unexpected-argument", "status", "extra")]
    [InlineData("usage-input-required", "call", "file_operations.get_working_directory")]
    [InlineData("usage-input-conflict", "call", "file_operations.get_working_directory", "--json", "{}", "--input", "x.json")]
    [InlineData("usage-invalid-value", "call", "file_operations.get_working_directory", "--json", "{}", "--max-inline-bytes", "-1")]
    [InlineData("usage-invalid-value", "call", "file_operations.get_working_directory", "--json", "{}", "--deadline-seconds", "0")]
    [InlineData("usage-invalid-value", "ops", "search", "--limit", "101")]
    [InlineData("usage-invalid-value", "ops", "search", "--effect", "destructive")]
    [InlineData("usage-invalid-value", "ops", "search", "--risk-flag", "FILESYSTEM_READ")]
    [InlineData("usage-invalid-value", "ops", "search", "--tier", "all")]
    [InlineData("usage-invalid-value", "ops", "search", "--query", "")]
    public async Task UsageErrorsSendNothingAndNameNoArgument(string code, params string[] arguments)
    {
        var run = await CliRun.RunAsync(arguments);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal("usage_error", run.Outcome);
        Assert.Equal(code, run.DiagnosticCode);
        Assert.NotNull(run.Data["usage"]);
        Assert.DoesNotContain("x.json", run.StandardOutput, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://localhost:50051")]
    [InlineData("http://10.0.0.1:50051")]
    [InlineData("http://0.0.0.0:50051")]
    [InlineData("https://127.0.0.1:50051")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://127.0.0.1:0")]
    [InlineData("http://127.0.0.1:65536")]
    [InlineData("http://127.1:50051")]
    [InlineData("http://127.000.000.001:50051")]
    [InlineData("http://[::ffff:127.0.0.1]:50051")]
    [InlineData("http://user@127.0.0.1:50051")]
    [InlineData("http://127.0.0.1:50051/briosa")]
    [InlineData("http://127.0.0.1:50051?x=1")]
    [InlineData(" http://127.0.0.1:50051")]
    public async Task OnlyALoopbackIpEndpointIsAccepted(string endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        Assert.False(LoopbackEndpoint.TryParse(endpoint, out _));
        var run = await CliRun.RunAsync(["status", "--endpoint", endpoint]);

        Assert.Equal(3, run.ExitCode);
        Assert.Equal("endpoint-not-loopback", run.DiagnosticCode);
        Assert.DoesNotContain(endpoint.Trim(), run.StandardOutput, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://127.0.0.1:50051", "http://127.0.0.1:50051")]
    [InlineData("http://127.0.0.1:50051/", "http://127.0.0.1:50051")]
    [InlineData("http://127.12.0.3:1", "http://127.12.0.3:1")]
    [InlineData("http://[::1]:65535", "http://[::1]:65535")]
    public void LoopbackIpEndpointsAreNormalized(string text, string normalized)
    {
        Assert.True(LoopbackEndpoint.TryParse(text, out var endpoint));
        Assert.Equal(normalized, endpoint!.Text);
    }

    [Fact]
    public async Task HelpAndVersionNeedNoServer()
    {
        var help = await CliRun.RunAsync([]);
        var version = await CliRun.RunAsync(["version"]);

        Assert.Equal(0, help.ExitCode);
        Assert.Equal(13, help.Data["usage"]!.AsArray().Count);
        Assert.Equal(0, version.ExitCode);
        Assert.Equal(Wire.Target, version.Data["spatial_analyzer_target"]!.GetValue<string>());
        Assert.Equal(2, version.Data["minimum_revision_for_call"]!.GetValue<int>());
        Assert.Empty(version.StandardError);
    }

    [Fact]
    public async Task OutputIsOneLineOfUtf8WithoutAByteOrderMark()
    {
        var run = await CliRun.RunAsync(["version"]);

        Assert.False(run.StandardOutputBytes.AsSpan().StartsWith(System.Text.Encoding.UTF8.Preamble));
        Assert.Equal((byte)'\n', run.StandardOutputBytes[^1]);
        Assert.Equal(1, run.StandardOutputBytes.Count(value => value == (byte)'\n'));
    }

    [Fact]
    public void EveryOutcomeHasOneExitCodeAndTheTableIsPinned()
    {
        var table = BridgeOutcomes.All.ToDictionary(BridgeOutcomes.Name, ExitCodes.For);
        Assert.Equal(new Dictionary<string, int>
        {
            ["succeeded"] = 0,
            ["internal_error"] = 1,
            ["usage_error"] = 2,
            ["rejected_locally"] = 3,
            ["server_unreachable"] = 4,
            ["server_incompatible"] = 5,
            ["not_started"] = 6,
            ["invalid_request"] = 7,
            ["mp_failed"] = 8,
            ["partial_success"] = 9,
            ["outputs_incomplete"] = 9,
            ["completion_evidence_unavailable"] = 10,
            ["started_outcome_unknown"] = 11,
            ["output_write_failed"] = 12
        }, table);
    }
}
