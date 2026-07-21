using System.Text.Json;
using Briosa.Server.Workers;
using Briosa.Worker.Control;

namespace Briosa.Server.Tests;

[Collection("Worker process lifecycle")]
public sealed class WorkerProcessSupervisorTests
{
    [Fact]
    public async Task NormalLifecycleReportsStatesAndReleasesStaOnOwningThread()
    {
        var lifecycleRecordPath = Path.GetTempFileName();
        try
        {
            await using var supervisor = CreateSupervisor(
                _ => CreateLaunch("normal", lifecycleRecordPath),
                CreatePolicy());

            Assert.True(await supervisor.StartAsync());
            var ready = supervisor.Current;
            await supervisor.StopAsync();

            Assert.Equal(WorkerLifecycleState.Ready, ready.State);
            Assert.True(ready.ProcessId > 0);
            Assert.Equal(WorkerConnectionState.Connected, ready.Connection!.State);
            Assert.Equal("localhost", ready.Connection.TargetHost);
            Assert.Equal(0, ready.Connection.StatusCode);
            Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
            Assert.Equal(WorkerTerminationKind.Graceful, supervisor.Current.LastTermination);
            Assert.Contains(
                supervisor.History,
                snapshot => snapshot.State == WorkerLifecycleState.Starting);
            Assert.Contains(
                supervisor.History,
                snapshot => snapshot.State == WorkerLifecycleState.Ready);

            using var lifecycle = JsonDocument.Parse(
                await File.ReadAllTextAsync(lifecycleRecordPath));
            var root = lifecycle.RootElement;
            Assert.Equal("STA", root.GetProperty("InitializationApartment").GetString());
            Assert.Equal("STA", root.GetProperty("ReleaseApartment").GetString());
            Assert.Equal(
                root.GetProperty("InitializationThreadId").GetInt32(),
                root.GetProperty("ReleaseThreadId").GetInt32());
        }
        finally
        {
            File.Delete(lifecycleRecordPath);
        }
    }

    [Fact]
    public async Task HungWorkerIsForcedDownAndReplacedWithoutRestartingSupervisor()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? "hang-on-ping" : "normal"),
            CreatePolicy());

        Assert.True(await supervisor.StartAsync());
        var recovered = await WaitFor(
            supervisor,
            snapshot => snapshot.State == WorkerLifecycleState.Ready &&
                snapshot.Generation >= 2);

        Assert.Equal(2, recovered.Generation);
        Assert.Equal(1, recovered.RestartCount);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Forced &&
                snapshot.DiagnosticCode == "worker-heartbeat-timeout");

        await supervisor.StopAsync();
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
    }

    [Fact]
    public async Task CrashedWorkerIsObservedAndReplaced()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? "crash-on-ping" : "normal"),
            CreatePolicy());

        Assert.True(await supervisor.StartAsync());
        var recovered = await WaitFor(
            supervisor,
            snapshot => snapshot.State == WorkerLifecycleState.Ready &&
                snapshot.Generation >= 2);

        Assert.Equal(1, recovered.RestartCount);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Crash);

        await supervisor.StopAsync();
    }

    [Fact]
    public async Task RestartBudgetStopsAnInfiniteCrashLoop()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("crash-on-ping"),
            CreatePolicy(maximumRestarts: 2));

        Assert.True(await supervisor.StartAsync());
        var exhausted = await WaitFor(
            supervisor,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.DiagnosticCode == "restart-budget-exhausted");

        Assert.Equal(3, exhausted.Generation);
        Assert.Equal(2, exhausted.RestartCount);
        Assert.Equal(
            3,
            supervisor.History.Count(
                snapshot => snapshot.State == WorkerLifecycleState.Starting));

        await supervisor.StopAsync();
        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
    }

    [Fact]
    public async Task GracefulStopTimeoutEscalatesToForcedTermination()
    {
        var policy = CreatePolicy(
            heartbeatInterval: TimeSpan.FromSeconds(10),
            shutdownTimeout: TimeSpan.FromMilliseconds(200));
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("ignore-stop"),
            policy);

        Assert.True(await supervisor.StartAsync());
        await supervisor.StopAsync();

        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Equal(WorkerTerminationKind.Forced, supervisor.Current.LastTermination);
        Assert.Equal("worker-stop-timeout", supervisor.Current.DiagnosticCode);
    }


    [Fact]
    public async Task ConcurrentRequestsRemainSerializedAcrossTheWorkerPipe()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True(await supervisor.StartAsync());
        var first = supervisor.ExecuteAsync(CreateCommand("first"));
        await Task.Delay(TimeSpan.FromMilliseconds(25));
        var second = supervisor.ExecuteAsync(CreateCommand("second"));
        await Task.Delay(TimeSpan.FromMilliseconds(75));

        Assert.False(second.IsCompleted);
        var results = await Task.WhenAll(first, second);

        Assert.All(
            results,
            result => Assert.Equal(WorkerExecutionStatus.Completed, result.Status));
        Assert.Equal(300, results[0].Execution!.DurationMilliseconds);
        Assert.Equal(5, results[1].Execution!.DurationMilliseconds);
        Assert.All(results, result => Assert.Equal(1, result.Generation));
    }

    [Fact]
    public async Task CallerCancellationStopsWaitingWithoutDesynchronizingThePipe()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True(await supervisor.StartAsync());
        using var clientCancellation = new CancellationTokenSource(
            TimeSpan.FromMilliseconds(50));
        var cancelled = await supervisor.ExecuteAsync(
            CreateCommand("cancelled-wait"),
            clientCancellation.Token);
        var next = await supervisor.ExecuteAsync(CreateCommand("after-cancellation"));

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, cancelled.Status);
        Assert.Equal("client-wait-cancelled", cancelled.DiagnosticCode);
        Assert.Equal(WorkerExecutionStatus.Completed, next.Status);
        Assert.Equal(1, next.Generation);
        Assert.Equal(1, supervisor.Current.Generation);
    }

    [Fact]
    public async Task ExecutionWatchdogForcesReplacementAndTheNextCallSucceeds()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(
                generation == 1 ? "hang-on-execute" : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(TimeSpan.FromMilliseconds(150)));

        Assert.True(await supervisor.StartAsync());
        var timedOut = await supervisor.ExecuteAsync(CreateCommand("hang"));
        var recovered = await supervisor.ExecuteAsync(CreateCommand("after-hang"));

        Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, timedOut.Status);
        Assert.Null(timedOut.Execution);
        Assert.Equal(1, timedOut.Generation);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Forced &&
                snapshot.DiagnosticCode == "worker-execution-watchdog-timeout");
    }

    [Fact]
    public async Task WorkerCrashDuringExecutionIsReplacedAndReportedSeparately()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(
                generation == 1 ? "crash-on-execute" : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True(await supervisor.StartAsync());
        var failed = await supervisor.ExecuteAsync(CreateCommand("crash"));
        var recovered = await supervisor.ExecuteAsync(CreateCommand("after-crash"));

        Assert.Equal(WorkerExecutionStatus.WorkerFailure, failed.Status);
        Assert.Null(failed.Execution);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Crash);
    }

    [Fact]
    public async Task MpFailureIsPreservedWhenExecuteStepReturnsTrue()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("mp-failure"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True(await supervisor.StartAsync());
        var result = await supervisor.ExecuteAsync(CreateCommand("mp-failure"));

        Assert.Equal(WorkerExecutionStatus.Completed, result.Status);
        Assert.True(result.Execution!.ExecuteStepReturned);
        Assert.False(result.Execution.MpSucceeded);
        Assert.Equal(42, result.Execution.MpResultCode);
        Assert.Equal("scripted-mp-failure", result.DiagnosticCode);
    }

    [Fact]
    public async Task ProductionWorkerCompletesControlLifecycleWithoutSpatialAnalyzer()
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-under-test",
            "Briosa.Worker.exe");
        Assert.True(
            File.Exists(executable),
            $"The worker executable was not found at '{executable}'.");
        await using var supervisor = CreateSupervisor(
            _ => new WorkerProcessLaunch(
                executable,
                ["--disable-sdk-activation", "--sa-host", "sa-lab"],
                workingDirectory: Path.GetDirectoryName(executable)),
            CreatePolicy());

        Assert.True(await supervisor.StartAsync());
        Assert.Equal(WorkerLifecycleState.Ready, supervisor.Current.State);
        Assert.Equal(WorkerConnectionState.Faulted, supervisor.Current.Connection!.State);
        Assert.Equal("sa-lab", supervisor.Current.Connection.TargetHost);
        Assert.Null(supervisor.Current.Connection.StatusCode);
        Assert.Equal(
            "sdk-client-activation-failed",
            supervisor.Current.Connection.DiagnosticCode);

        var unavailable = await supervisor.ExecuteAsync(
            CreateCommand("sdk-unavailable"));
        Assert.Equal(WorkerExecutionStatus.Unavailable, unavailable.Status);
        Assert.Null(unavailable.Execution);
        Assert.Equal("sdk-connection-not-ready", unavailable.DiagnosticCode);
        Assert.Equal(WorkerConnectionState.Faulted, unavailable.Connection!.State);

        await supervisor.StopAsync();

        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Equal(WorkerTerminationKind.Graceful, supervisor.Current.LastTermination);
    }

    private static WorkerProcessSupervisor CreateSupervisor(
        Func<int, WorkerProcessLaunch> launchFactory,
        WorkerRestartPolicy policy,
        WorkerExecutionPolicy? executionPolicy = null) =>
        new(
            new NamedPipeWorkerProcessFactory(launchFactory),
            policy,
            executionPolicy ?? CreateExecutionPolicy());

    private static WorkerProcessLaunch CreateLaunch(
        string scenario,
        string? lifecycleRecordPath = null)
    {
        var executable = Path.Combine(
            AppContext.BaseDirectory,
            "worker-test-host",
            "Briosa.Worker.TestHost.exe");
        Assert.True(File.Exists(executable), $"The fake worker executable was not found at '{executable}'.");

        var arguments = new List<string>
        {
            "--scenario",
            scenario
        };
        if (lifecycleRecordPath is not null)
        {
            arguments.Add("--lifecycle-record");
            arguments.Add(lifecycleRecordPath);
        }

        return new WorkerProcessLaunch(
            executable,
            arguments,
            Path.GetDirectoryName(executable));
    }


    private static WorkerMpCommand CreateCommand(string operationId) =>
        new(
            operationId,
            "Scripted Step",
            [
                new WorkerMpArgument(
                    "Enabled",
                    WorkerMpArgumentKind.Logical,
                    BooleanValue: true),
                new WorkerMpArgument(
                    "Count",
                    WorkerMpArgumentKind.WholeNumber,
                    IntegerValue: 2),
                new WorkerMpArgument(
                    "Tolerance",
                    WorkerMpArgumentKind.FloatingPoint,
                    DoubleValue: 0.01),
                new WorkerMpArgument(
                    "Label",
                    WorkerMpArgumentKind.Text,
                    StringValue: "portable-test")
            ]);

    private static WorkerExecutionPolicy CreateExecutionPolicy(
        TimeSpan? watchdogTimeout = null) =>
        new(
            watchdogTimeout ?? TimeSpan.FromSeconds(2),
            queueCapacity: 16);

    private static WorkerRestartPolicy CreatePolicy(
        int maximumRestarts = 3,
        TimeSpan? heartbeatInterval = null,
        TimeSpan? shutdownTimeout = null) =>
        new(
            maximumRestarts,
            restartWindow: TimeSpan.FromSeconds(10),
            heartbeatInterval ?? TimeSpan.FromMilliseconds(50),
            heartbeatTimeout: TimeSpan.FromMilliseconds(250),
            startupTimeout: TimeSpan.FromSeconds(5),
            shutdownTimeout ?? TimeSpan.FromMilliseconds(500),
            restartDelay: TimeSpan.FromMilliseconds(10));

    private static async Task<WorkerLifecycleSnapshot> WaitFor(
        WorkerProcessSupervisor supervisor,
        Func<WorkerLifecycleSnapshot, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var snapshot = supervisor.Current;
            if (predicate(snapshot))
            {
                return snapshot;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

}

[CollectionDefinition("Worker process lifecycle", DisableParallelization = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "xUnit requires collection definition classes to be public.")]
public sealed class WorkerProcessLifecycleGroup;
