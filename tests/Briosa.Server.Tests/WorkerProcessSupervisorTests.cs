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

        await supervisor.StopAsync();

        Assert.Equal(WorkerLifecycleState.Stopped, supervisor.Current.State);
        Assert.Equal(WorkerTerminationKind.Graceful, supervisor.Current.LastTermination);
    }

    private static WorkerProcessSupervisor CreateSupervisor(
        Func<int, WorkerProcessLaunch> launchFactory,
        WorkerRestartPolicy policy) =>
        new(new NamedPipeWorkerProcessFactory(launchFactory), policy);

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
