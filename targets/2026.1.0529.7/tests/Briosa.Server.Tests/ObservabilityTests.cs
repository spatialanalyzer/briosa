using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.Json;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
public sealed class ObservabilityTests
{
    private const string Sensitive = @"C:\Customers\Secret\geometry.xit";

    [Fact]
    public void StartupFiltersAreValidatedAndDoNotReload()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Briosa:Logging:File:Enabled"] = "false",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Briosa.Server.Services"] = "Debug"
            }).Build();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddBriosaLogging(configuration));
        using var container = services.BuildServiceProvider();
        var factory = container.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger("Briosa.Server.Services.OperationExecutor");
        Assert.True(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(factory.CreateLogger("Other").IsEnabled(LogLevel.Information));
        configuration["Logging:LogLevel:Briosa.Server.Services"] = "Trace";
        configuration.Reload();
        Assert.False(logger.IsEnabled(LogLevel.Trace));
    }

    [Fact]
    public async Task FailedDirectoryCannotReplaceOperationOutcome()
    {
        var file = Path.GetTempFileName();
        var health = new BriosaLogHealth();
        using var provider = new BriosaLogProvider(Options() with { Directory = file }, health);
        using var logs = LoggerFactory.Create(builder => builder.AddProvider(provider));
        using var telemetry = new BriosaTelemetry(health);
        try
        {
            await using var supervisor = Supervisor("normal", logs, telemetry);
            Assert.True(await supervisor.StartAsync());
            await Run(Executor(supervisor, logs, telemetry));
            await Until(() => health.Failures > 0);
            Assert.Equal(1, supervisor.ExecutionSnapshot.TerminalRequests);
        }
        finally { provider.Dispose(); File.Delete(file); }
    }

    [Fact]
    public void ExportRequiresExplicitValidConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(BriosaTelemetryOptions.Bind(configuration).Enabled);
        configuration["Briosa:Telemetry:Enabled"] = "true";
        var error = Assert.Throws<InvalidOperationException>(() => BriosaTelemetryOptions.Bind(configuration));
        Assert.Equal("Invalid Briosa telemetry configuration.", error.Message);
        configuration["Briosa:Telemetry:Endpoint"] = "http://localhost:4317";
        var options = BriosaTelemetryOptions.Bind(configuration);
        Assert.True(options.Enabled);
        Assert.Equal(0.1, options.SampleRatio);
        configuration["Briosa:Telemetry:SampleRatio"] = "NaN";
        Assert.Throws<InvalidOperationException>(() => BriosaTelemetryOptions.Bind(configuration));
    }

    [Theory]
    [InlineData("File:Directory", "relative")]
    [InlineData("File:MaxFileSizeMiB", "0")]
    [InlineData("File:RetainedFileCount", "1001")]
    [InlineData("File:MaxAgeDays", "366")]
    [InlineData("File:MaxTotalSizeMiB", "1")]
    [InlineData("QueueCapacity", "0")]
    [InlineData("ConsoleEnabled", Sensitive)]
    public void InvalidConfigurationHasValueFreeErrors(string key, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Briosa:Logging:" + key] = value }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => BriosaLoggingOptions.Bind(configuration));
        Assert.Equal("Invalid Briosa logging configuration.", error.Message);
        Assert.Null(error.InnerException);
    }

    [Fact]
    public async Task UnavailableCollectorDoesNotReplaceOperationOutcomeOrBlockShutdown()
    {
        using var port = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        port.Start();
        var endpoint = new Uri($"http://127.0.0.1:{((System.Net.IPEndPoint)port.LocalEndpoint).Port}");
        port.Stop();
        var health = new BriosaLogHealth();
        using var logs = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None));
        using var telemetry = new BriosaTelemetry(health);
        using var export = new BriosaTelemetryExport(new(true, endpoint, 1), health,
            logs.CreateLogger<BriosaTelemetryExport>());
        await export.StartAsync(CancellationToken.None);
        await using var supervisor = Supervisor("normal", logs, telemetry);
        Assert.True(await supervisor.StartAsync());
        await Run(Executor(supervisor, logs, telemetry));
        Assert.Equal(1, supervisor.ExecutionSnapshot.TerminalRequests);
        var shutdown = Stopwatch.GetTimestamp();
        await export.StopAsync(CancellationToken.None);
        Assert.True(Stopwatch.GetElapsedTime(shutdown) < TimeSpan.FromSeconds(8));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void AllLevelsKeepMetadataAndDiscardPayloadsExceptionsAndScopes(LogLevel level)
    {
        var sink = new CaptureSink();
        using var provider = new BriosaLogProvider(Options(), new BriosaLogHealth(), sink);
        var logger = provider.CreateLogger("Briosa.Server.Services.OperationAuditLogger");
        using (logger.BeginScope(new Dictionary<string, object?> { ["Secret"] = Sensitive }))
        {
            var state = new Dictionary<string, object?>
            {
                ["OperationId"] = GetWorkingDirectoryOperation.OperationId,
                ["Generation"] = 4,
                ["PolicyFingerprint"] = "sha256:" + new string('a', 64),
                ["Request"] = Sensitive,
                ["Response"] = Sensitive,
                ["DiagnosticCode"] = Sensitive,
                ["{OriginalFormat}"] = Sensitive
            };
            var failure = new IOException(Sensitive);
            logger.Log(level, new EventId(2004, Sensitive), state, failure, (_, _) => Sensitive);
            provider.CreateLogger("Microsoft.AspNetCore.Hosting").Log(
                LogLevel.Error, new EventId(99), Sensitive, new IOException(Sensitive), (_, _) => Sensitive);
        }
        provider.Dispose();
        Assert.Equal(2, sink.Events.Count);
        var text = string.Join("\n", sink.Events.Select(Serialize));
        Assert.DoesNotContain(Sensitive, text, StringComparison.Ordinal);
        Assert.Contains(GetWorkingDirectoryOperation.OperationId, text, StringComparison.Ordinal);
        Assert.Contains("RpcCompleted", text, StringComparison.Ordinal);
        Assert.Contains("sha256:" + new string('a', 64), text, StringComparison.Ordinal);
        Assert.Contains("FrameworkEvent", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SaturatedSinkDropsAndShutdownRemainsBounded()
    {
        using var sink = new PausedSink();
        var health = new BriosaLogHealth();
        var provider = new BriosaLogProvider(Options() with
        { QueueCapacity = 16, ShutdownTimeoutMilliseconds = 50 }, health, sink);
        try
        {
            var logger = provider.CreateLogger("Briosa.Server.Test");
            logger.Log(LogLevel.Information, new EventId(1000), "ready", null, static (value, _) => value);
            Assert.True(sink.Entered.Wait(TimeSpan.FromSeconds(5)));
            for (var index = 0; index < 256; index++)
                logger.Log(LogLevel.Information, new EventId(1000), "ready", null, static (value, _) => value);
            Assert.InRange(health.Queued, 1, 16);
            Assert.True(health.Dropped > 0);
            var started = Stopwatch.GetTimestamp();
            provider.Dispose();
            Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(2));
            Assert.True(health.Failures > 0);
        }
        finally
        {
            sink.Release.Set();
            provider.Dispose();
        }
    }

    [Fact]
    public async Task FilesRotateAcrossInstancesAndRespectDirectoryBudget()
    {
        var directory = NewDirectory();
        var options = Options() with
        { Directory = directory, MaxFileSizeMiB = 1, MaxTotalSizeMiB = 2, RetainedFileCount = 2 };
        var health = new BriosaLogHealth();
        try
        {
            // A stale prior-instance file and an unrelated file exercise cleanup scope.
            var stale = Path.Combine(directory, $"briosa-{Guid.NewGuid():N}-20000101.jsonl");
            await File.WriteAllTextAsync(stale, "old");
            File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddDays(-30));
            await File.WriteAllTextAsync(Path.Combine(directory, "unrelated.txt"), "keep");
            using (var first = new BriosaRetainedLogSink(options, Guid.NewGuid().ToString("N"), health))
            using (var second = new BriosaRetainedLogSink(options, Guid.NewGuid().ToString("N"), health))
            {
                await Task.WhenAll(Task.Run(() =>
                {
                    for (var index = 0; index < 1600; index++) first.Emit(TestEvent());
                }), Task.Run(() =>
                {
                    for (var index = 0; index < 1600; index++) second.Emit(TestEvent());
                }));
            }
            var files = new DirectoryInfo(directory).GetFiles("*.jsonl");
            Assert.InRange(files.Length, 1, 2);
            Assert.All(files, file => Assert.True(file.Length <= 1024 * 1024));
            Assert.True(files.Sum(file => file.Length) <= 2 * 1024 * 1024);
            Assert.False(File.Exists(stale));
            Assert.Equal("keep", await File.ReadAllTextAsync(Path.Combine(directory, "unrelated.txt")));
            foreach (var file in files)
                await foreach (var line in File.ReadLinesAsync(file.FullName))
                {
                    using var record = JsonDocument.Parse(line);
                    Assert.Equal(TimeSpan.Zero, record.RootElement.GetProperty("Timestamp").GetDateTimeOffset().Offset);
                }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task SingleFileBudgetKeepsWritingAfterReachingQuota()
    {
        var directory = NewDirectory();
        var health = new BriosaLogHealth();
        try
        {
            using (var sink = new BriosaRetainedLogSink(Options() with
            { Directory = directory, MaxFileSizeMiB = 1, MaxTotalSizeMiB = 1, RetainedFileCount = 1 },
                Guid.NewGuid().ToString("N"), health))
            {
                for (var index = 0; index < 1600; index++) sink.Emit(TestEvent());
                sink.Emit(new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null,
                    new MessageTemplateParser().Parse("AfterQuota"), []));
            }
            var file = Assert.Single(new DirectoryInfo(directory).GetFiles("*.jsonl"));
            Assert.InRange(file.Length, 1, 1024 * 1024);
            Assert.Contains("AfterQuota", await File.ReadAllTextAsync(file.FullName), StringComparison.Ordinal);
            Assert.Equal(0, health.Dropped);
            Assert.Equal(0, health.Failures);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task CancelledRpcStillRecordsLateExecutionWithOriginalContext()
    {
        var sink = new CaptureSink();
        var health = new BriosaLogHealth();
        using var provider = new BriosaLogProvider(Options(), health, sink);
        using var logs = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        using var telemetry = new BriosaTelemetry(health);
        var spans = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BriosaTelemetry.InstrumentationName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = spans.Enqueue
        };
        ActivitySource.AddActivityListener(listener);
        await using var supervisor = Supervisor("delay-first-execute", logs, telemetry);
        Assert.True(await supervisor.StartAsync());
        var executor = Executor(supervisor, logs, telemetry);
        var correlation = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        using var parent = new Activity("test-parent");
        parent.Start();
        var task = Run(executor, correlation, cancellation.Token);
        await Until(() => supervisor.ExecutionSnapshot.ActiveExecutions == 1);
        await cancellation.CancelAsync();
        var error = await Assert.ThrowsAsync<RpcException>(() => task);
        Assert.Equal(StatusCode.Cancelled, error.StatusCode);
        await Until(() => supervisor.ExecutionSnapshot.TerminalRequests == 1);
        await supervisor.StopAsync();
        provider.Dispose();
        var resolved = Assert.Single(sink.Events, entry => EventName(entry) == "ExecutionResolved");
        Assert.Equal(correlation, Scalar(resolved, "CorrelationId"));
        Assert.Equal(1, Scalar(resolved, "Generation"));
        Assert.Equal("completed", Scalar(resolved, "ExecutionDisposition"));
        Assert.Equal(2, Scalar(resolved, "MpResultCode"));
        Assert.Contains(sink.Events, entry => EventName(entry) == "RpcFailed");
        Assert.Contains(spans, span => span.OperationName == "briosa.worker.exchange" && span.TraceId == parent.TraceId);
        Assert.Contains(spans, span => span.OperationName == "briosa.queue" && span.TraceId == parent.TraceId);
    }

    [Theory]
    [InlineData("hang-on-execute")]
    [InlineData("crash-on-execute")]
    public async Task WorkerFailuresAreCorrelatedErrorsWithUnknownOutcome(string scenario)
    {
        var sink = new CaptureSink();
        var health = new BriosaLogHealth();
        using var provider = new BriosaLogProvider(Options(), health, sink);
        using var logs = LoggerFactory.Create(builder => builder.AddProvider(provider));
        using var telemetry = new BriosaTelemetry(health);
        await using var supervisor = Supervisor(scenario, logs, telemetry, TimeSpan.FromMilliseconds(200));
        Assert.True(await supervisor.StartAsync());
        await Assert.ThrowsAsync<RpcException>(() => Run(Executor(supervisor, logs, telemetry)));
        provider.Dispose();
        var entry = Assert.Single(sink.Events, entry => EventName(entry) == "ExecutionResolved");
        Assert.Equal(LogEventLevel.Error, entry.Level);
        Assert.Equal("started_outcome_unknown", Scalar(entry, "ExecutionDisposition"));
        Assert.Equal(1, Scalar(entry, "Generation"));
    }

    [Fact]
    public void MetricsUseCachedStateAndBoundUnknownOperationDimensions()
    {
        var measurements = new ConcurrentQueue<(string Name, string Tags)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, observer) =>
        {
            if (instrument.Meter.Name == BriosaTelemetry.InstrumentationName)
                observer.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
            measurements.Enqueue((instrument.Name, string.Join(",", tags.ToArray().Select(pair => pair.Value)))));
        listener.SetMeasurementEventCallback<int>((instrument, _, tags, _) =>
            measurements.Enqueue((instrument.Name, string.Join(",", tags.ToArray().Select(pair => pair.Value)))));
        listener.Start();
        using var telemetry = new BriosaTelemetry(new BriosaLogHealth());
        telemetry.RpcCompleted(Sensitive, "Cancelled", 2);
        listener.RecordObservableInstruments();
        Assert.Contains(measurements, value => value.Name == "briosa.rpc.completed" && value.Tags.Contains("unsupported", StringComparison.Ordinal));
        Assert.Contains(measurements, value => value.Name == "briosa.ready");
        Assert.DoesNotContain(measurements, value => value.Tags.Contains(Sensitive, StringComparison.Ordinal));
    }

    internal static BriosaLoggingOptions Options() => new() { ConsoleEnabled = false };
    internal static string NewDirectory() => Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "briosa-logging-tests-" + Guid.NewGuid().ToString("N"))).FullName;
    internal static WorkerProcessSupervisor Supervisor(string scenario, ILoggerFactory logs,
        BriosaTelemetry telemetry, TimeSpan? watchdog = null) => new(
        new NamedPipeWorkerProcessFactory(_ => new WorkerProcessLaunch(
            Path.Combine(AppContext.BaseDirectory, "worker-test-host", "Briosa.Worker.TestHost.exe"),
            ["--scenario", scenario])),
        new WorkerRestartPolicy(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.Zero),
        new WorkerExecutionPolicy(watchdog ?? TimeSpan.FromSeconds(5), 64),
        logger: logs.CreateLogger<WorkerProcessSupervisor>(),
        identityPolicy: ExactTargetIdentityPolicy.CreateForTesting("2026.1.0529.7",
            activatedSdkVersion: "2026.1.0529.7", connectedSpatialAnalyzerVersion: "2026.1.0529.7"),
        telemetry: telemetry);
    internal static OperationExecutor Executor(WorkerProcessSupervisor supervisor, ILoggerFactory logs, BriosaTelemetry telemetry) =>
        new(supervisor, new OperationAuditLogger(logs.CreateLogger<OperationAuditLogger>()), TimeProvider.System, telemetry);
    internal static Task<object> Run(OperationExecutor executor, Guid? correlation = null, CancellationToken cancellation = default) =>
        executor.ExecuteAsync<object, object>(new object(), GetWorkingDirectoryOperation.Descriptor,
            _ => new WorkerMpCommand(GetWorkingDirectoryOperation.OperationId, "Scripted Step", [], []),
            [], _ => new object(), cancellation, correlationId: correlation);
    internal static async Task Until(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!predicate()) await Task.Delay(10, timeout.Token).ConfigureAwait(false);
    }
    private static object? Scalar(LogEvent entry, string name) =>
        ((ScalarValue)entry.Properties[name]).Value;
    private static string EventName(LogEvent entry) => entry.MessageTemplate.Text;
    private static LogEvent TestEvent() => new(DateTimeOffset.Now, LogEventLevel.Information, null,
        new MessageTemplateParser().Parse("Test"), [new("Padding", new ScalarValue(new string('a', 1600)))]);
    private static string Serialize(LogEvent entry)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new Serilog.Formatting.Json.JsonFormatter().Format(entry, writer);
        return writer.ToString();
    }
    internal sealed class CaptureSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
    }
    internal sealed class PausedSink : ILogEventSink, IDisposable
    {
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public void Emit(LogEvent logEvent) { Entered.Set(); Release.Wait(); }
        public void Dispose() { Release.Set(); }
    }
    internal sealed class FailedSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => throw new IOException(Sensitive);
    }
}
