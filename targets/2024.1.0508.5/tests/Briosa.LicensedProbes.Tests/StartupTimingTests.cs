using System.Text.Json;

namespace Briosa.LicensedProbes.Tests;

// The offline cold-start report (#371, #402): it reads the server's own JSON log records and
// adds no timing of its own.
public sealed class StartupTimingTests
{
    private const string Instance = "0123456789abcdef0123456789abcdef";
    private static readonly DateTimeOffset At = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);

    // One record as the server's file sink writes it (Serilog JsonFormatter, message not rendered).
    private static string Line(double seconds, int eventId, string name, string properties, string level = "Information", string instance = Instance) =>
        "{\"Timestamp\":\"" + At.AddSeconds(seconds).ToString("O", System.Globalization.CultureInfo.InvariantCulture) +
        "\",\"Level\":\"" + level + "\",\"MessageTemplate\":\"" + name + "\",\"Properties\":{\"ServerInstanceId\":\"" + instance +
        "\",\"SpatialAnalyzerTarget\":\"" + ProbeTarget.SpatialAnalyzerTarget + "\"," + properties +
        "\"EventId\":{\"Id\":" + eventId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"Name\":\"" + name + "\"}}}";

    private static IReadOnlyList<string> ColdThenWarm() =>
    [
        Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":2875,"),
        Line(1, 1303, "LifecycleRpcStarted", "\"Rpc\":\"StartSpatialAnalyzerSdk\","),
        Line(1.2, 1203, "WorkerStartPhase", "\"Generation\":1,\"Phase\":\"process-created\",\"ElapsedMilliseconds\":214,"),
        Line(3.9, 1203, "WorkerStartPhase", "\"Generation\":1,\"Phase\":\"control-pipe-connected\",\"ElapsedMilliseconds\":2911,"),
        Line(8.4, 1203, "WorkerStartPhase", "\"Generation\":1,\"Phase\":\"ready-received\",\"ElapsedMilliseconds\":7432,"),
        Line(8.5, 1304, "LifecycleRpcCompleted", "\"Rpc\":\"StartSpatialAnalyzerSdk\",\"GrpcStatus\":\"OK\",\"ElapsedMilliseconds\":7502,"),
        Line(9, 1303, "LifecycleRpcStarted", "\"Rpc\":\"ConnectToSpatialAnalyzer\","),
        Line(10, 1304, "LifecycleRpcCompleted", "\"Rpc\":\"ConnectToSpatialAnalyzer\",\"GrpcStatus\":\"OK\",\"ElapsedMilliseconds\":1011,"),
        Line(20, 1303, "LifecycleRpcStarted", "\"Rpc\":\"StartSpatialAnalyzerSdk\","),
        Line(20.1, 1203, "WorkerStartPhase", "\"Generation\":2,\"Phase\":\"process-created\",\"ElapsedMilliseconds\":40,"),
        Line(20.4, 1203, "WorkerStartPhase", "\"Generation\":2,\"Phase\":\"control-pipe-connected\",\"ElapsedMilliseconds\":350,"),
        Line(21.1, 1203, "WorkerStartPhase", "\"Generation\":2,\"Phase\":\"ready-received\",\"ElapsedMilliseconds\":1060,"),
        Line(21.2, 1304, "LifecycleRpcCompleted", "\"Rpc\":\"StartSpatialAnalyzerSdk\",\"GrpcStatus\":\"OK\",\"ElapsedMilliseconds\":1130,")
    ];

    private static StartupTiming.Report Analyze(IEnumerable<string> lines, StartupCondition condition = StartupCondition.AfterReboot) =>
        StartupTiming.Analyze(condition, [.. lines.Select(StartupTiming.ParseLine).OfType<StartupTiming.LogEvent>()], 0, null);

    [Fact]
    public void TheFirstStartIsTheMeasuredConditionAndLaterStartsAreRepeats()
    {
        var report = Analyze(ColdThenWarm());

        Assert.Equal(2875, report.EndpointListeningMilliseconds);
        Assert.Equal(2, report.Attempts.Count);
        var cold = report.Attempts[0];
        Assert.Equal(1, cold.Generation);
        Assert.Equal(new StartupTiming.RpcTiming("completed", "OK", null, 7502), cold.Start);
        Assert.Equal(["process-created", "control-pipe-connected", "ready-received"], cold.Phases.Select(static phase => phase.Phase));
        Assert.Equal(7432, cold.Phases[^1].ElapsedMilliseconds);
        Assert.Equal(1011, cold.Connect!.ElapsedMilliseconds);
        Assert.Equal(2, report.Attempts[1].Generation);
        Assert.Null(report.Attempts[1].Connect);

        using var json = JsonDocument.Parse(StartupTiming.ToJson(report));
        var attempts = json.RootElement.GetProperty("start_attempts").EnumerateArray().ToList();
        Assert.Equal("after-reboot", attempts[0].GetProperty("label").GetString());
        Assert.Equal("repeat", attempts[1].GetProperty("label").GetString());
        Assert.Equal(7432, attempts[0].GetProperty("phases_ms").GetProperty("ready-received").GetInt64());
        Assert.Equal(10000, json.RootElement.GetProperty("default_worker_startup_timeout_ms").GetInt64());
        Assert.Contains("| 1 | after-reboot | completed `OK` | 7502 ms | 214 | 2911 | 7432 | completed `OK` | 1011 ms |",
            StartupTiming.ToMarkdown(report), StringComparison.Ordinal);
    }

    [Fact]
    public void AStartThatMissedItsBoundShowsTheExpiredPhaseAndTheTypedRejection()
    {
        var report = Analyze(
        [
            Line(1, 1303, "LifecycleRpcStarted", "\"Rpc\":\"StartSpatialAnalyzerSdk\","),
            Line(1.2, 1203, "WorkerStartPhase", "\"Generation\":1,\"Phase\":\"process-created\",\"ElapsedMilliseconds\":300,"),
            Line(11.1, 1203, "WorkerStartPhase", "\"Generation\":1,\"Phase\":\"startup-bound-expired\",\"ElapsedMilliseconds\":10004,", "Warning"),
            Line(11.3, 1302, "LifecycleRejected",
                "\"Rpc\":\"StartSpatialAnalyzerSdk\",\"GrpcStatus\":\"FailedPrecondition\",\"DiagnosticCode\":\"worker-startup-timeout\",\"ElapsedMilliseconds\":10310,", "Warning")
        ], StartupCondition.FreshInstall);

        var attempt = Assert.Single(report.Attempts);
        Assert.Equal(new StartupTiming.RpcTiming("rejected", "FailedPrecondition", "worker-startup-timeout", 10310), attempt.Start);
        Assert.Contains("startup-bound-expired at 10004", StartupTiming.ToMarkdown(report), StringComparison.Ordinal);
    }

    [Fact]
    public void UnlistedValuesAreDroppedAndUnusableLinesIgnored()
    {
        var withheld = StartupTiming.ParseLine(Line(1, 1203, "WorkerStartPhase",
            "\"Phase\":\"C:\\\\SENTINEL\\\\worker.exe\",\"DiagnosticCode\":\"C:\\\\SENTINEL\",\"Rpc\":\"SENTINEL\",\"GrpcStatus\":\"SENTINEL\",\"ElapsedMilliseconds\":5,"));

        Assert.NotNull(withheld);
        Assert.Equal(["ElapsedMilliseconds"], withheld.Fields.Keys);
        Assert.Null(StartupTiming.ParseLine("not json"));
        Assert.Null(StartupTiming.ParseLine("{\"Timestamp\":\"x\"}"));
        Assert.Null(StartupTiming.ParseLine(Line(1, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,", instance: "SENTINEL")));
        Assert.Null(StartupTiming.ParseLine("{\"Level\":\"Warning\",\"MessageTemplate\":\"LogRecordTooLarge\",\"Properties\":{\"SchemaVersion\":1}}"));
    }

    [Fact]
    public void AStartWithNoExitRecordIsSaidToHaveNone()
    {
        var report = Analyze([Line(1, 1303, "LifecycleRpcStarted", "\"Rpc\":\"StartSpatialAnalyzerSdk\",")]);

        Assert.Equal("no-exit-recorded", Assert.Single(report.Attempts).Start.Exit);
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
                ["--startup-timing", "--condition", "fresh-install", "--server-log-directory", logs.FullName, "--output-directory", target],
                output, error, CancellationToken.None);

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
            Assert.Equal(2, await StartupTiming.RunAsync(
                ["--startup-timing", "--condition", "fresh-install", "--server-log-directory", logs.FullName, "--output-directory", target],
                output, error, CancellationToken.None));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task TwoServerInstancesInOneFolderAreRefused()
    {
        var root = Directory.CreateTempSubdirectory("briosa-startup-");
        try
        {
            await File.WriteAllLinesAsync(Path.Combine(root.FullName, "briosa-a-1.jsonl"),
            [
                Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,"),
                Line(0, 1001, "EndpointListening", "\"ElapsedMilliseconds\":1,", instance: "fedcba9876543210fedcba9876543210")
            ]);
            var error = new StringWriter();
            var code = await StartupTiming.RunAsync(
                ["--startup-timing", "--condition", "warm", "--server-log-directory", root.FullName, "--output-directory", Path.Combine(root.FullName, "out")],
                new StringWriter(), error, CancellationToken.None);

            Assert.Equal(2, code);
            Assert.Contains("2 server instances", error.ToString(), StringComparison.Ordinal);
            Assert.False(Directory.Exists(Path.Combine(root.FullName, "out")));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("--condition must be", "--startup-timing", "--condition", "sometime", "--server-log-directory", "x", "--output-directory", "y")]
    [InlineData("An unknown option", "--startup-timing", "--address", "http://127.0.0.1:1")]
    [InlineData("does not exist", "--startup-timing", "--condition", "warm", "--server-log-directory", "Z:\\no-such-briosa-folder", "--output-directory", "y")]
    [InlineData("--server-log-directory is required", "--startup-timing", "--condition", "warm", "--output-directory", "y")]
    public async Task InvalidStartupTimingArgumentsAreUsageErrors(string message, params string[] arguments)
    {
        var error = new StringWriter();

        Assert.Equal(2, await StartupTiming.RunAsync(arguments, new StringWriter(), error, CancellationToken.None));
        Assert.Contains(message, error.ToString(), StringComparison.Ordinal);
    }
}
