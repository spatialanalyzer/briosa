using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Briosa.Server.Operations;
using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
public sealed class GeneratedClientPerformanceEvidenceTests
{
    private const int WarmupRequests = 40;
    private const int SampleRequests = 400;
    private static readonly JsonSerializerOptions EvidenceJsonOptions = new() { WriteIndented = true };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneratedClientMeasuresTypedRuntimeWithBoundedAdmission(bool logging)
    {
        var directory = Path.Combine(Path.GetTempPath(), "briosa-grpc-performance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await RunAsync(directory, logging);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task RunAsync(string directory, bool logging)
    {
        var workerReportPath = Path.Combine(directory, "worker.json");
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.Sources.Clear(); // Keep packaged appsettings and operator environment out of this fixture.
        builder.Configuration.AddInMemoryCollection(Settings(directory, logging));
        builder.Logging.AddBriosaLogging(builder.Configuration);
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = WorkerControlProtocol.MaximumMessageBytes;
            options.EnableDetailedErrors = true; // Synthetic test data only; expose fixture failures.
        });
        builder.Services.AddSingleton<WorkerProcessSupervisor>(services => new(
            new NamedPipeWorkerProcessFactory(_ => CreateLaunch(workerReportPath)),
            new WorkerLifecyclePolicy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)),
            new WorkerExecutionPolicy(TimeSpan.FromSeconds(10), 64),
            logger: services.GetRequiredService<ILogger<WorkerProcessSupervisor>>()));
        builder.Services.AddSingleton<OperationAuditLogger>();
        builder.Services.AddSingleton(_ => OperationPolicy.Create(builder.Configuration, SpatialAnalyzerApi.Operations));
        builder.Services.AddSingleton<IWorkerCommandExecutor, PolicyEnforcingWorkerCommandExecutor>();
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddSingleton<OperationExecutor>();
        var app = builder.Build();
        await using var appLifetime = app.ConfigureAwait(true);
        app.MapGrpcService<VariablesService>();
        app.MapGrpcService<RelationshipOperationsService>();
        var supervisor = app.Services.GetRequiredService<WorkerProcessSupervisor>();
        var health = app.Services.GetRequiredService<BriosaLogHealth>();
        var rows = new List<Measurement>();
        Assert.True(await supervisor.StartAsync().ConfigureAwait(true), supervisor.Current.DiagnosticCode);
        await app.StartAsync().ConfigureAwait(true);
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var variables = new Api.Variables.VariablesClient(channel);
        var relationships = new Api.RelationshipOperations.RelationshipOperationsClient(channel);
        // Every RPC remains bounded, independently of the worker watchdog.
        CallOptions Options() => new(deadline: DateTime.UtcNow.AddSeconds(30));
        var scalar = new Api.GetDoubleVariableRequest { Name = "benchmark" };
        var nested = new Api.SetRelationshipFitConstraintsScalarTypeRequest
        {
            RelationshipName = new() { ObjectName = "benchmark" },
            FitConstraintOptions = new() { High = new() { Enabled = true, Value = 1.25 }, Low = new() { Value = -0.5 } }
        };
        rows.Add(await MeasureAsync("scalar-get", async () =>
            Assert.Equal(1.25, (await variables.GetDoubleVariableAsync(scalar, Options())).Value)).ConfigureAwait(true));
        rows.Add(await MeasureAsync("nested-set", async () =>
            await relationships.SetRelationshipFitConstraintsScalarTypeAsync(nested, Options())).ConfigureAwait(true));
        rows.Add(await MeasureAsync("nested-get", async () =>
        {
            var result = await relationships.GetRelationshipFitConstraintsScalarTypeAsync(
                new() { RelationshipName = nested.RelationshipName }, Options());
            Assert.True(result.FitConstraintOptions.High.Enabled);
            Assert.Equal(1.25, result.FitConstraintOptions.High.Value);
            Assert.False(result.FitConstraintOptions.Low.Enabled);
            Assert.Equal(-0.5, result.FitConstraintOptions.Low.Value);
        }).ConfigureAwait(true));
        foreach (var count in new[] { 32, 1024, 4096 })
        {
            var set = new Api.SetNamedDoubleListVariableRequest { Name = "benchmark" };
            set.DoubleListVariable.AddRange(Enumerable.Range(0, count).Select(index => index * 0.25));
            rows.Add(await MeasureAsync($"list-{count}-set", async () =>
                await variables.SetNamedDoubleListVariableAsync(set, Options())).ConfigureAwait(true));
            rows.Add(await MeasureAsync($"list-{count}-get", async () =>
            {
                var result = await variables.GetNamedDoubleListVariableAsync(new() { Name = "benchmark" }, Options());
                Assert.Equal(set.DoubleListVariable, result.DoubleListVariable);
            }).ConfigureAwait(true));
        }
        rows.Add(await MeasureAsync("scalar-get-concurrency-16", async () =>
            Assert.Equal(1.25, (await variables.GetDoubleVariableAsync(scalar, Options())).Value), concurrency: 16).ConfigureAwait(true));
        var burst = await Task.WhenAll(Enumerable.Range(0, 256).Select(async _ =>
        {
            try
            {
                await variables.GetDoubleVariableAsync(new() { Name = "delay" }, Options());
                return true;
            }
            catch (RpcException error) when (error.StatusCode == StatusCode.ResourceExhausted)
            {
                var trailer = Assert.Single(error.Trailers, entry => entry.Key == "briosa-operation-error-bin");
                var detail = Api.OperationError.Parser.ParseFrom(trailer.ValueBytes);
                Assert.Equal(Api.OperationFailureKind.Overloaded, detail.Kind);
                Assert.Equal(Api.ExecutionDisposition.NotStarted, detail.ExecutionDisposition);
                Assert.Equal(Api.ReplayGuidance.MayReplay, detail.ReplayGuidance);
                return false;
            }
        })).ConfigureAwait(true);
        Assert.Contains(false, burst);
        Assert.Contains(true, burst);
        // A scheduled tick can skip a recent exchange, so allow the following idle tick too.
        // This wait is excluded from all timing samples.
        await Task.Delay(TimeSpan.FromMilliseconds(2250)).ConfigureAwait(true);
        Assert.True(supervisor.Current.ReadyForExecution);
        var execution = supervisor.ExecutionSnapshot;
        Assert.InRange(execution.PeakQueuedRequests, 1, 64);
        Assert.Equal(0, execution.QueuedRequests);
        Assert.Equal(0, execution.WaitingForAdmission);
        Assert.Equal(0, execution.ActiveExecutions);
        Assert.Equal(rows.Count * (WarmupRequests + SampleRequests) + burst.Count(completed => completed), execution.AdmittedRequests);
        Assert.Equal(execution.AdmittedRequests, execution.TerminalRequests);
        Assert.Equal(0, execution.WatchdogTimeouts);
        Assert.Equal(0, execution.WorkerFailures);
        await supervisor.StopAsync().ConfigureAwait(true);
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        await app.StopAsync().ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true); // Flush the owned log pipeline before recording its final state.
        Assert.Equal(0, health.Dropped);
        Assert.Equal(0, health.Failures);
        Assert.Equal(0, health.Queued);
        if (logging) Assert.NotEmpty(Directory.GetFiles(Path.Combine(directory, "logs"), "*.jsonl"));
        using var workerReport = JsonDocument.Parse(await File.ReadAllTextAsync(workerReportPath).ConfigureAwait(true));
        Assert.Equal(execution.TerminalRequests, workerReport.RootElement.GetProperty("executed").GetInt64());
        Assert.True(workerReport.RootElement.GetProperty("pings").GetInt32() >= 1);
        var evidenceDirectory = Environment.GetEnvironmentVariable("BRIOSA_GENERATED_CLIENT_PERFORMANCE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(evidenceDirectory)) return;
        Directory.CreateDirectory(evidenceDirectory);
        using var process = Process.GetCurrentProcess();
        var evidence = new
        {
            schema_version = 1,
            target = SpatialAnalyzerApi.TargetVersion,
            private_protocol = WorkerControlProtocol.CurrentVersion,
            runtime = Environment.Version.ToString(),
            logging,
            harness = "generated-client-http2-named-pipe-fake-worker",
            measurement_boundary = "Generated client, loopback HTTP/2, production service mapping, policy/admission, supervisor and named-pipe control codec; synthetic SDK peer",
            limitations = "Client, host and test assertions share allocation measurements; other test-runner activity may contribute. Worker allocation is separate. No SA timing claim or absolute performance gate. The idle wait after load is excluded from measured samples.",
            warmup_requests = WarmupRequests,
            sample_requests = SampleRequests,
            rows,
            overload = new { submitted = burst.Length, completed = burst.Count(completed => completed), rejected = burst.Count(completed => !completed) },
            execution,
            log_health = new { health.Dropped, health.Failures, health.Queued },
            retained_managed_bytes = GC.GetTotalMemory(forceFullCollection: true),
            peak_working_set_bytes = process.PeakWorkingSet64,
            worker = workerReport.RootElement
        };
        var path = Path.Combine(evidenceDirectory, logging ? "grpc-logging.json" : "grpc-no-logging.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, EvidenceJsonOptions) + "\n").ConfigureAwait(true);
    }

    private static async Task<Measurement> MeasureAsync(string name, Func<Task> call, int concurrency = 1)
    {
        for (var index = 0; index < WarmupRequests; index++) await call().ConfigureAwait(true);
        var samples = new double[SampleRequests];
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var started = Stopwatch.GetTimestamp();
        var next = -1;
        await Task.WhenAll(Enumerable.Range(0, concurrency).Select(async _ =>
        {
            while (true)
            {
                var index = Interlocked.Increment(ref next);
                if (index >= SampleRequests) return;
                var before = Stopwatch.GetTimestamp();
                await call().ConfigureAwait(true);
                samples[index] = Stopwatch.GetElapsedTime(before).TotalMilliseconds;
            }
        })).ConfigureAwait(true);
        var elapsed = Stopwatch.GetElapsedTime(started);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        Array.Sort(samples);
        return new(name, SampleRequests, concurrency, samples[199], samples[379], samples[395],
            SampleRequests / elapsed.TotalSeconds, allocated / (double)SampleRequests);
    }

    private static WorkerProcessLaunch CreateLaunch(string reportPath)
    {
        var executable = Path.Combine(AppContext.BaseDirectory, "worker-test-host", "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable));
        return new(executable, ["--performance", "--report", reportPath], Path.GetDirectoryName(executable));
    }

    private static Dictionary<string, string?> Settings(string directory, bool logging)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Briosa:Logging:ConsoleEnabled"] = "false",
            ["Briosa:Logging:File:Enabled"] = logging.ToString(),
            ["Briosa:Logging:File:Directory"] = Path.Combine(directory, "logs"),
            ["Briosa:Logging:File:MaxFileSizeMiB"] = "2",
            ["Briosa:Logging:File:MaxTotalSizeMiB"] = "20",
            ["Logging:LogLevel:Default"] = "Information"
        };
        string[] operations = ["variables.get_double_variable", "variables.get_named_double_list_variable",
            "variables.set_named_double_list_variable", "relationship_operations.get_relationship_fit_constraints_scalar_type",
            "relationship_operations.set_relationship_fit_constraints_scalar_type"];
        for (var index = 0; index < operations.Length; index++)
            settings[$"{OperationPolicy.AllowKey}:{index}"] = operations[index];
        return settings;
    }

    private sealed record Measurement(string Name, int Count, int Concurrency, double P50Milliseconds,
        double P95Milliseconds, double P99Milliseconds, double RequestsPerSecond,
        double CombinedClientServerAllocatedBytesPerCall);
}
