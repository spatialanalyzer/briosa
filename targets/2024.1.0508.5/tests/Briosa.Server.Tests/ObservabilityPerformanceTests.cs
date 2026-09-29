using System.Diagnostics;
using System.Text.Json;
using Briosa.Server.Services;
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
public sealed class ObservabilityPerformanceTests
{
    [Theory]
    [InlineData("disabled")]
    [InlineData("normal")]
    [InlineData("slow")]
    [InlineData("failed")]
    public async Task FakeWorkerMeasuresCompleteLoggingPipeline(string mode)
    {
        var directory = ObservabilityTests.NewDirectory();
        using var slow = new ObservabilityTests.PausedSink();
        ILogEventSink? sink = mode switch
        {
            "slow" => slow,
            "failed" => new ObservabilityTests.FailedSink(),
            _ => null
        };
        var health = new BriosaLogHealth();
        using var provider = new BriosaLogProvider(ObservabilityTests.Options() with
        {
            Directory = directory,
            FileEnabled = mode != "disabled",
            QueueCapacity = mode == "slow" ? 32 : 4096,
            ShutdownTimeoutMilliseconds = 10000
        }, health, sink);
        using var logs = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(mode == "disabled" ? LogLevel.None : LogLevel.Information)
            .AddProvider(provider));
        using var telemetry = new BriosaTelemetry(health);
        try
        {
            await using var supervisor = ObservabilityTests.Supervisor("normal", logs, telemetry);
            Assert.True((await supervisor.StartAsync()).Succeeded);
            var executor = ObservabilityTests.Executor(supervisor, logs, telemetry);
            for (var index = 0; index < 64; index++) await ObservabilityTests.Run(executor);
            const int count = 512;
            var samples = new double[count];
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
            var started = Stopwatch.GetTimestamp();
            for (var index = 0; index < count; index++)
            {
                var requestStarted = Stopwatch.GetTimestamp();
                await ObservabilityTests.Run(executor);
                samples[index] = Stopwatch.GetElapsedTime(requestStarted).TotalMilliseconds;
            }
            var elapsed = Stopwatch.GetElapsedTime(started).TotalSeconds;
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            var retained = GC.GetTotalMemory(forceFullCollection: true) - memoryBefore;
            var queuedAtEnd = health.Queued;
            await supervisor.StopAsync();
            slow.Release.Set();
            provider.Dispose();
            Assert.Equal(576, supervisor.ExecutionSnapshot.TerminalRequests);
            Assert.Equal(0, supervisor.ExecutionSnapshot.WatchdogTimeouts);
            Assert.Equal(0, supervisor.ExecutionSnapshot.WorkerFailures);
            if (mode == "slow") Assert.True(health.Dropped > 0);
            if (mode == "failed") Assert.True(health.Failures > 0);
            if (mode == "normal")
            {
                Assert.Equal(0, health.Failures);
                Assert.Equal(0, health.Dropped);
                Assert.NotEmpty(Directory.GetFiles(directory, "*.jsonl"));
            }
            Array.Sort(samples);
            var evidenceDirectory = Environment.GetEnvironmentVariable("BRIOSA_OBSERVABILITY_EVIDENCE_DIRECTORY");
            if (evidenceDirectory is not null)
            {
                Directory.CreateDirectory(evidenceDirectory);
                var evidence = new
                {
                    schema_version = 1,
                    mode,
                    requests = count,
                    boundary = "OperationExecutor through fake named-pipe worker and configured logging provider; no SpatialAnalyzer",
                    p50_ms = samples[(int)Math.Ceiling(count * 0.50) - 1],
                    p95_ms = samples[(int)Math.Ceiling(count * 0.95) - 1],
                    p99_ms = samples[(int)Math.Ceiling(count * 0.99) - 1],
                    requests_per_second = count / elapsed,
                    allocated_bytes = allocated,
                    retained_managed_bytes_delta = retained,
                    queued_at_end = queuedAtEnd,
                    dropped_records = health.Dropped,
                    sink_failures = health.Failures
                };
                await File.WriteAllTextAsync(Path.Combine(evidenceDirectory, mode + ".json"),
                    JsonSerializer.Serialize(evidence));
            }
        }
        finally
        {
            slow.Release.Set();
            provider.Dispose();
            Directory.Delete(directory, recursive: true);
        }
    }
}
