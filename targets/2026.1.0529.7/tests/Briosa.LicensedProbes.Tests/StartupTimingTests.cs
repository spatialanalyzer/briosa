using System.Globalization;
using System.Text.Json;

namespace Briosa.LicensedProbes.Tests;

// The offline cold-start report (#371, #402): it reads the server's own JSON log records of this
// exact target and adds no timing of its own.
public sealed class StartupTimingTests
{
    private const string Instance = "0123456789abcdef0123456789abcdef";
    private static readonly DateTimeOffset At = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Stock = TimeSpan.FromSeconds(10);

    // One record as the server's file sink writes it (Serilog JsonFormatter, message not rendered).
    private static string Line(double seconds, int eventId, string name, string properties, string level = "Information",
        string instance = Instance, string? target = null) =>
        "{\"Timestamp\":\"" + At.AddSeconds(seconds).ToString("O", CultureInfo.InvariantCulture) +
        "\",\"Level\":\"" + level + "\",\"MessageTemplate\":\"" + name + "\",\"Properties\":{\"ServerInstanceId\":\"" + instance +
        "\",\"SpatialAnalyzerTarget\":\"" + (target ?? ProbeTarget.SpatialAnalyzerTarget) + "\"," + properties +
        "\"EventId\":{\"Id\":" + eventId.ToString(CultureInfo.InvariantCulture) + ",\"Name\":\"" + name + "\"}}}";

    private static string Started(double seconds, string rpc) => Line(seconds, 1303, "LifecycleRpcStarted", $"\"Rpc\":\"{rpc}\",");

    private static string Completed(double seconds, string rpc, long elapsed) =>
        Line(seconds, 1304, "LifecycleRpcCompleted", $"\"Rpc\":\"{rpc}\",\"GrpcStatus\":\"OK\",\"ElapsedMilliseconds\":{elapsed},");

    private static string Phase(double seconds, int generation, string phase, long elapsed) =>
        Line(seconds, 1203, "WorkerStartPhase", $"\"Generation\":{generation},\"Phase\":\"{phase}\",\"ElapsedMilliseconds\":{elapsed},");

    private static IReadOnlyList<string> ColdThenWarm() =>
    [
        Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":2875,"),
        Started(1, StartupTiming.StartRpc),
        Phase(1.2, 1, "process-created", 214),
        Phase(3.9, 1, "control-pipe-connected", 2911),
        Phase(8.4, 1, "ready-received", 7432),
        Completed(8.5, StartupTiming.StartRpc, 7502),
        Started(9, StartupTiming.ConnectRpc),
        Completed(10, StartupTiming.ConnectRpc, 1011),
        Started(20, StartupTiming.StartRpc),
        Phase(20.1, 2, "process-created", 40),
        Phase(20.4, 2, "control-pipe-connected", 350),
        Phase(21.1, 2, "ready-received", 1060),
        Completed(21.2, StartupTiming.StartRpc, 1130)
    ];

    private static StartupTiming.Report Analyze(IEnumerable<string> lines, StartupCondition condition = StartupCondition.AfterReboot, TimeSpan? bound = null) =>
        StartupTiming.Analyze(condition, bound ?? Stock,
            [.. lines.Select(StartupTiming.ParseLine).Where(static line => line.Kind == StartupTiming.LineKind.Event).Select(static line => line.Event!)], 0, null);

    [Fact]
    public void TheFirstStartIsTheMeasuredConditionOnlyWithProofAndLaterStartsAreRepeats()
    {
        var report = Analyze(ColdThenWarm());

        Assert.Equal(2875, report.EndpointListeningMilliseconds);
        Assert.Equal(2, report.Attempts.Count);
        var cold = report.Attempts[0];
        Assert.Equal(("after-reboot", StartupTiming.Unambiguous, (int?)1), (cold.Label, cold.Correlation, cold.Generation));
        Assert.Equal(new StartupTiming.RpcTiming("completed", "OK", null, 7502), cold.Start);
        Assert.Equal(["process-created", "control-pipe-connected", "ready-received"], cold.Phases.Select(static phase => phase.Phase));
        Assert.Equal(7432, cold.Phases[^1].ElapsedMilliseconds);
        Assert.Equal(1011, cold.Connect!.ElapsedMilliseconds);
        Assert.Equal(("repeat", (int?)2), (report.Attempts[1].Label, report.Attempts[1].Generation));
        Assert.Null(report.Attempts[1].Connect);
        Assert.True(report.StockConfiguration);

        using var json = JsonDocument.Parse(StartupTiming.ToJson(report));
        var attempts = json.RootElement.GetProperty("start_attempts").EnumerateArray().ToList();
        Assert.Equal("after-reboot", attempts[0].GetProperty("label").GetString());
        Assert.Equal("repeat", attempts[1].GetProperty("label").GetString());
        Assert.Equal(7432, attempts[0].GetProperty("phases_ms").GetProperty("ready-received").GetInt64());
        Assert.Equal(10000, json.RootElement.GetProperty("default_worker_startup_timeout_ms").GetInt64());
        Assert.Equal("stock", json.RootElement.GetProperty("configuration").GetString());
        Assert.Contains("| 1 | after-reboot | unambiguous | completed `OK` | 7502 ms | 214 | 2911 | 7432 | completed `OK` | 1011 ms |",
            StartupTiming.ToMarkdown(report), StringComparison.Ordinal);
    }

    [Fact]
    public void ARaisedStartupBoundIsLabelledAChangedConfigurationEverywhere()
    {
        var report = Analyze(ColdThenWarm(), bound: TimeSpan.FromSeconds(60));
        using var json = JsonDocument.Parse(StartupTiming.ToJson(report));

        Assert.False(report.StockConfiguration);
        Assert.Equal("changed configuration: worker startup bound 60 s, not a stock start", report.ConfigurationLabel);
        Assert.False(json.RootElement.GetProperty("stock_configuration").GetBoolean());
        Assert.Equal(60000, json.RootElement.GetProperty("configured_worker_startup_timeout_ms").GetInt64());
        Assert.Contains("**Configuration: changed configuration: worker startup bound 60 s, not a stock start.**", StartupTiming.ToMarkdown(report), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("no-listening")]
    [InlineData("earlier-phase")]
    [InlineData("second-generation")]
    public void AFirstAttemptWithoutProofIsNeverLabelledCold(string condition)
    {
        var lines = ColdThenWarm().ToList();
        switch (condition)
        {
            case "no-listening": lines.RemoveAt(0); break;
            case "earlier-phase": lines.Insert(1, Phase(0.5, 1, "process-created", 1)); break;
            case "second-generation":
                lines = [.. lines.Select(static line => line.Replace("\"Generation\":1,", "\"Generation\":3,", StringComparison.Ordinal))];
                break;
        }

        var first = Analyze(lines).Attempts[0];

        Assert.Equal("first-attempt-unproven", first.Label);
        Assert.NotEqual("after-reboot", first.Label);
    }

    [Fact]
    public void OverlappingRequestsAreAmbiguousAndKeepNoPhases()
    {
        var report = Analyze(
        [
            Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":5,"),
            Started(1, StartupTiming.StartRpc),
            Started(1.1, "StopSpatialAnalyzerSdk"),
            Phase(1.2, 1, "process-created", 100),
            Completed(2, StartupTiming.StartRpc, 1000)
        ]);

        var attempt = Assert.Single(report.Attempts);
        Assert.Equal((StartupTiming.OverlappingRequests, "first-attempt-unproven"), (attempt.Correlation, attempt.Label));
        Assert.Empty(attempt.Phases);
        Assert.Null(attempt.Generation);
    }

    [Fact]
    public void AStartThatMissedItsBoundShowsTheExpiredPhaseAndTheTypedRejection()
    {
        var report = Analyze(
        [
            Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":5,"),
            Started(1, StartupTiming.StartRpc),
            Phase(1.2, 1, "process-created", 300),
            Line(11.1, 1203, "WorkerStartPhase", "\"Generation\":1,\"Phase\":\"startup-bound-expired\",\"ElapsedMilliseconds\":10004,", "Warning"),
            Line(11.3, 1302, "LifecycleRejected",
                "\"Rpc\":\"StartSpatialAnalyzerSdk\",\"GrpcStatus\":\"FailedPrecondition\",\"DiagnosticCode\":\"worker-startup-timeout\",\"ElapsedMilliseconds\":10310,", "Warning")
        ], StartupCondition.FreshInstall);

        var attempt = Assert.Single(report.Attempts);
        Assert.Equal(new StartupTiming.RpcTiming("rejected", "FailedPrecondition", "worker-startup-timeout", 10310), attempt.Start);
        Assert.Equal("fresh-install", attempt.Label);
        Assert.Contains("startup-bound-expired at 10004", StartupTiming.ToMarkdown(report), StringComparison.Ordinal);
    }

    [Fact]
    public void UnlistedValuesAreDroppedAndUnusableLinesAreCountedNeverThrown()
    {
        var (kind, withheld) = StartupTiming.ParseLine(Line(1, 1203, "WorkerStartPhase",
            "\"Phase\":\"C:\\\\SENTINEL\\\\worker.exe\",\"DiagnosticCode\":\"desktop-sentinel-01\",\"Rpc\":\"SENTINEL\",\"GrpcStatus\":\"SENTINEL\",\"ElapsedMilliseconds\":5,"));

        Assert.Equal(StartupTiming.LineKind.Event, kind);
        Assert.Equal(["DiagnosticCode", "ElapsedMilliseconds"], withheld!.Fields.Keys);
        Assert.Equal(ExternalValues.Withheld, withheld.Fields["DiagnosticCode"]);
        foreach (var line in new[]
        {
            "not json",
            "{\"Timestamp\":\"x\"}",
            "[1,2,3]",
            Line(1, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,", instance: "SENTINEL"),
            Line(1, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,").Replace("\"Id\":1001", "\"Id\":\"1001\"", StringComparison.Ordinal),
            Line(1, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,").Replace("\"Properties\":{", "\"Properties\":[{", StringComparison.Ordinal),
            "{\"Level\":\"Warning\",\"MessageTemplate\":\"LogRecordTooLarge\",\"Properties\":{\"SchemaVersion\":1}}"
        })
        {
            Assert.Equal(StartupTiming.LineKind.Unreadable, StartupTiming.ParseLine(line).Kind);
        }

        Assert.Equal(StartupTiming.LineKind.OtherTarget,
            StartupTiming.ParseLine(Line(1, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,", target: "1999.1.0000.0")).Kind);
    }

    [Fact]
    public void AStartWithNoExitRecordIsSaidToHaveNone()
    {
        var report = Analyze([Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":9,"), Started(1, StartupTiming.StartRpc)]);

        var attempt = Assert.Single(report.Attempts);
        Assert.Equal((StartupTiming.NoExitRecorded, StartupTiming.NoExitRecorded), (attempt.Start.Exit, attempt.Correlation));
        Assert.Empty(Analyze([Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":9,")]).Attempts);
    }

    [Fact]
    public async Task TheCommandWritesBothRecordsWithoutAPathOrAValue()
    {
        var root = Directory.CreateTempSubdirectory("briosa-startup-");
        try
        {
            var logs = Directory.CreateDirectory(Path.Combine(root.FullName, "server-logs"));
            await File.WriteAllLinesAsync(Path.Combine(logs.FullName, $"briosa-{Instance}-20261123.jsonl"),
                [.. ColdThenWarm(), "garbage", Line(30, 1303, "LifecycleRpcStarted", "\"Rpc\":\"StartSpatialAnalyzerSdk\",\"DiagnosticCode\":\"C:\\\\SENTINEL\",")]);
            var output = new StringWriter();
            var error = new StringWriter();
            var target = Path.Combine(root.FullName, "out");

            var code = await StartupTiming.RunAsync(
                ["--startup-timing", "--condition", "fresh-install", "--startup-timeout-seconds", "60", "--server-log-directory", logs.FullName,
                    "--output-directory", target], output, error, CancellationToken.None);

            Assert.Equal(0, code);
            var json = await File.ReadAllTextAsync(Path.Combine(target, $"startup-timing-fresh-install-{ProbeTarget.SpatialAnalyzerTarget}.json"));
            var markdown = await File.ReadAllTextAsync(Path.Combine(target, $"startup-timing-fresh-install-{ProbeTarget.SpatialAnalyzerTarget}.md"));
            foreach (var text in new[] { json, markdown, output.ToString() })
            {
                Assert.DoesNotContain(TestSupport.Sentinel, text, StringComparison.Ordinal);
                Assert.DoesNotContain(root.FullName, text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(Instance, text, StringComparison.Ordinal);
            }

            using var document = JsonDocument.Parse(json);
            Assert.Equal(1, document.RootElement.GetProperty("unreadable_lines").GetInt32());
            Assert.Equal(3, document.RootElement.GetProperty("start_attempts").GetArrayLength());
            Assert.Contains("changed configuration", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(2, await StartupTiming.RunAsync(
                ["--startup-timing", "--condition", "fresh-install", "--startup-timeout-seconds", "60", "--server-log-directory", logs.FullName,
                    "--output-directory", target], output, error, CancellationToken.None));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("instances")]
    [InlineData("other-target")]
    public async Task ALogFolderThatCannotBeAttributedIsRefused(string condition)
    {
        var root = Directory.CreateTempSubdirectory("briosa-startup-");
        try
        {
            await File.WriteAllLinesAsync(Path.Combine(root.FullName, "briosa-a-1.jsonl"),
            [
                Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,"),
                condition == "instances"
                    ? Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,", instance: "fedcba9876543210fedcba9876543210")
                    : Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,", target: "1999.1.0000.0")
            ]);
            var error = new StringWriter();
            var code = await StartupTiming.RunAsync(
                ["--startup-timing", "--condition", "warm", "--startup-timeout-seconds", "10", "--server-log-directory", root.FullName,
                    "--output-directory", Path.Combine(root.FullName, "out")], new StringWriter(), error, CancellationToken.None);

            Assert.Equal(2, code);
            Assert.Contains(condition == "instances" ? "2 server instances" : "another SpatialAnalyzer target", error.ToString(), StringComparison.Ordinal);
            Assert.False(Directory.Exists(Path.Combine(root.FullName, "out")));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("--condition must be", "--startup-timing", "--condition", "sometime", "--startup-timeout-seconds", "10", "--server-log-directory", "x", "--output-directory", "y")]
    [InlineData("An unknown option", "--startup-timing", "--address", "http://127.0.0.1:1")]
    [InlineData("--startup-timeout-seconds must give", "--startup-timing", "--condition", "warm", "--server-log-directory", "x", "--output-directory", "y")]
    [InlineData("--startup-timeout-seconds must give", "--startup-timing", "--condition", "warm", "--startup-timeout-seconds", "0", "--server-log-directory", "x", "--output-directory", "y")]
    [InlineData("does not exist", "--startup-timing", "--condition", "warm", "--startup-timeout-seconds", "10", "--server-log-directory", "Z:\\no-such-briosa-folder", "--output-directory", "y")]
    [InlineData("--server-log-directory is required", "--startup-timing", "--condition", "warm", "--startup-timeout-seconds", "10", "--output-directory", "y")]
    public async Task InvalidStartupTimingArgumentsAreUsageErrors(string message, params string[] arguments)
    {
        var error = new StringWriter();

        Assert.Equal(2, await StartupTiming.RunAsync(arguments, new StringWriter(), error, CancellationToken.None));
        Assert.Contains(message, error.ToString(), StringComparison.Ordinal);
    }
}
