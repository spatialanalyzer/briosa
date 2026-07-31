using System.Diagnostics;
using System.Text.Json;
using Briosa.Core.V1Alpha1;
using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.Configuration;
using TargetProtocol = global::Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
public sealed class RuntimePerformanceEvidenceTests
{
    private const int WarmupRequestCount = 64;
    private const int SampleRequestCount = 512;
    private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public async Task FakeWorkerDispatchProducesRepeatableBoundedStateEvidence()
    {
        await using var supervisor = new WorkerProcessSupervisor(
            new NamedPipeWorkerProcessFactory(_ => CreateLaunch()),
            new WorkerRestartPolicy(
                maximumRestarts: 3,
                restartWindow: TimeSpan.FromMinutes(1),
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTimeout: TimeSpan.FromSeconds(1),
                startupTimeout: TimeSpan.FromSeconds(5),
                shutdownTimeout: TimeSpan.FromSeconds(1),
                restartDelay: TimeSpan.Zero),
            new WorkerExecutionPolicy(
                watchdogTimeout: TimeSpan.FromSeconds(2),
                queueCapacity: 64),
            identityPolicy: ExactTargetIdentityPolicy.CreateForTesting(
                "2026.1.0529.7",
                activatedSdkVersion: "2026.1.0529.7",
                connectedSpatialAnalyzerVersion: "2026.1.0529.7"));

        Assert.True(await supervisor.StartAsync());
        for (var index = 0; index < WarmupRequestCount; index++)
        {
            var warmup = await supervisor.ExecuteAsync(CreateCommand(index));
            Assert.Equal(WorkerExecutionStatus.Completed, warmup.Status);
        }

        var samples = new double[SampleRequestCount];
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var retainedBefore = GC.GetTotalMemory(forceFullCollection: true);
        for (var index = 0; index < SampleRequestCount; index++)
        {
            var started = Stopwatch.GetTimestamp();
            var outcome = await supervisor.ExecuteAsync(CreateCommand(index));
            samples[index] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Assert.Equal(WorkerExecutionStatus.Completed, outcome.Status);
            Assert.Equal(WorkerExecutionDisposition.Completed, outcome.ExecutionDisposition);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var retainedAfter = GC.GetTotalMemory(forceFullCollection: true);
        var retainedManagedBytes = Math.Max(0, retainedAfter - retainedBefore);
        Array.Sort(samples);
        var percentileIndex = (int)Math.Ceiling(samples.Length * 0.95) - 1;
        var dispatchP95Milliseconds = samples[percentileIndex];
        var snapshot = supervisor.ExecutionSnapshot;

        var requestMappingP95Milliseconds = MeasureP95(CreateRequestMappingAction());
        var discoveryP95Milliseconds = MeasureP95(CreateDiscoveryAction());

        Assert.Equal(
            WarmupRequestCount + SampleRequestCount,
            snapshot.AdmittedRequests);
        Assert.Equal(snapshot.AdmittedRequests, snapshot.TerminalRequests);
        Assert.Equal(0, snapshot.QueuedRequests);
        Assert.Equal(0, snapshot.WaitingForAdmission);
        Assert.Equal(0, snapshot.ActiveExecutions);
        Assert.Equal(0, snapshot.WatchdogTimeouts);
        Assert.Equal(0, snapshot.WorkerFailures);

        var evidencePath = Environment.GetEnvironmentVariable(
            "BRIOSA_RUNTIME_PERFORMANCE_EVIDENCE_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            var fullPath = Path.GetFullPath(evidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var evidence = new
            {
                schema_version = 1,
                harness = "named-pipe-fake-worker",
                warmup_requests = WarmupRequestCount,
                sample_requests = SampleRequestCount,
                measurement_boundaries = new
                {
                    dispatch =
                        "WorkerProcessSupervisor.ExecuteAsync through the private named-pipe control channel and vendor-independent fake worker; no SpatialAnalyzer SDK call",
                    request_mapping =
                        "handwritten Get Working Directory CreateCommand, GrpcOperationOutcomeMapper.RequireSuccess, and CreateResult with a fixed completed outcome",
                    discovery =
                        "ListCapabilities response creation over every operation allowed by the implemented operation registry"
                },
                dispatch_p95_milliseconds = dispatchP95Milliseconds,
                request_mapping_p95_milliseconds = requestMappingP95Milliseconds,
                discovery_p95_milliseconds = discoveryP95Milliseconds,
                retained_managed_memory_bytes = retainedManagedBytes,
                execution = snapshot
            };
            await File.WriteAllTextAsync(
                fullPath,
                JsonSerializer.Serialize(evidence, EvidenceJsonOptions) + "\n");
        }
    }

    private static WorkerProcessLaunch CreateLaunch()
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(
            File.Exists(executable),
            $"The fake worker executable was not found at '{executable}'.");
        return new WorkerProcessLaunch(
            executable,
            ["--scenario", "normal"],
            Path.GetDirectoryName(executable));
    }

    private static WorkerMpCommand CreateCommand(int index) =>
        new($"performance-{index}", "Performance Evidence", [], []);

    private static Action CreateRequestMappingAction()
    {
        var outcome = new WorkerExecutionOutcome(
            WorkerExecutionStatus.Completed,
            WorkerExecutionDisposition.Completed,
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: true,
                MpResultCode: 2,
                DurationMilliseconds: 0,
                OutputValues:
                [
                    new WorkerMpOutputValue(
                        "Directory",
                        WorkerMpValueKind.Text,
                        Retrieved: true,
                        StringValue: "redacted-performance-value")
                ],
                DiagnosticCode: null),
            Connection: null,
            DiagnosticCode: "completed",
            Generation: 1);
        var request = new TargetProtocol.GetWorkingDirectoryRequest();
        return () =>
        {
            var command = GetWorkingDirectoryOperation.CreateCommand(request);
            var successful = GrpcOperationOutcomeMapper.RequireSuccess(
                outcome,
                command.OperationId,
                ReplaySafety.Safe,
                GetWorkingDirectoryOperation.OutputContracts,
                callerDeadlineExceeded: false);
            var result = GetWorkingDirectoryOperation.CreateResult(successful);
            if (!result.HasDirectory || result.Execution is null)
            {
                throw new InvalidOperationException(
                    "The request-mapping performance sample returned an invalid result.");
            }
        };
    }

    private static Action CreateDiscoveryAction()
    {
        var policyValues = SpatialAnalyzerApi.Operations
            .Select((operation, index) => new KeyValuePair<string, string?>(
                $"{OperationPolicy.AllowKey}:{index}",
                operation.OperationId));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(policyValues)
            .Build();
        var policy = OperationPolicy.Create(
            configuration,
            SpatialAnalyzerApi.Operations);
        var service = new ServerDiscoveryService(
            new FixedStatusProvider(),
            new FixedBuildIdentityProvider(),
            policy);
        return () =>
        {
            var result = service.CreateCapabilities();
            if (result.Operations.Count != policy.AllowedOperations.Count)
            {
                throw new InvalidOperationException(
                    "The discovery performance sample omitted an allowed operation.");
            }
        };
    }

    private static double MeasureP95(Action action)
    {
        for (var index = 0; index < WarmupRequestCount; index++)
        {
            action();
        }

        var samples = new double[SampleRequestCount];
        for (var index = 0; index < samples.Length; index++)
        {
            var started = Stopwatch.GetTimestamp();
            action();
            samples[index] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }

        Array.Sort(samples);
        return samples[(int)Math.Ceiling(samples.Length * 0.95) - 1];
    }

    private sealed class FixedStatusProvider : IWorkerStatusProvider
    {
        public WorkerLifecycleSnapshot Current { get; } = new(
            WorkerLifecycleState.Ready,
            Generation: 1,
            ProcessId: null,
            RestartCount: 0,
            WorkerTerminationKind.None,
            "performance-ready",
            Connection: null,
            DateTimeOffset.UnixEpoch);
    }

    private sealed class FixedBuildIdentityProvider : IServerBuildIdentityProvider
    {
        public VersionCoordinates CreateVersionCoordinates() => new();
    }
}
