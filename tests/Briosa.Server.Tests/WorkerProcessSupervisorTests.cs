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
            Assert.Equal(
                WorkerExecutionReadinessState.ExecutionReady,
                ready.Connection.ExecutionReadinessState);
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
    public async Task ExplicitRecoveryReplacesRuntimeLoopsAfterReplacementProbeQuarantine()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation switch
            {
                1 => "crash-on-ping",
                2 => "hang-on-verify",
                _ => "normal"
            }),
            CreatePolicy(),
            CreateExecutionPolicy(TimeSpan.FromMilliseconds(150)));

        Assert.True(await supervisor.StartAsync());
        var quarantined = await WaitFor(
            supervisor,
            snapshot => snapshot.Generation == 2 &&
                snapshot.Connection?.ExecutionReadinessState ==
                    WorkerExecutionReadinessState.OperatorRecoveryRequired);

        Assert.Equal(1, quarantined.RestartCount);
        Assert.True(await supervisor.RecoverExecutionAsync());
        var completed = await supervisor.ExecuteAsync(CreateCommand("after-recovery"));

        Assert.Equal(3, supervisor.Current.Generation);
        Assert.Equal(WorkerExecutionStatus.Completed, completed.Status);
        Assert.Equal(3, completed.Generation);
    }

    [Theory]
    [InlineData(
        "hang-on-verify",
        "Forced",
        "execution-readiness-probe-timeout")]
    [InlineData(
        "crash-on-verify",
        "Crash",
        "execution-readiness-worker-exited")]
    public async Task AmbiguousVerificationQuarantinesWithoutAutomaticRestart(
        string firstScenario,
        string expectedTermination,
        string expectedDiagnosticCode)
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? firstScenario : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(TimeSpan.FromMilliseconds(150)));

        Assert.False(await supervisor.StartAsync());
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(1, supervisor.Current.Generation);
        Assert.Equal(0, supervisor.Current.RestartCount);
        Assert.Equal(WorkerLifecycleState.Degraded, supervisor.Current.State);
        Assert.Equal(
            Enum.Parse<WorkerTerminationKind>(expectedTermination),
            supervisor.Current.LastTermination);
        Assert.Equal(expectedDiagnosticCode, supervisor.Current.DiagnosticCode);
        Assert.Equal(
            WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.CompetingClientSuspected);
        Assert.Equal(
            1,
            supervisor.History.Count(snapshot =>
                snapshot.DiagnosticCode == "worker-starting"));

        Assert.True(await supervisor.RecoverExecutionAsync());
        Assert.Equal(2, supervisor.Current.Generation);
        Assert.Equal(
            WorkerExecutionReadinessState.ExecutionReady,
            supervisor.Current.Connection!.ExecutionReadinessState);
    }

    [Fact]
    public async Task CompletedVerificationFailureRequiresExplicitRecoveryWithoutCompetingClaim()
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? "reject-verify" : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.False(await supervisor.StartAsync());

        Assert.Equal(1, supervisor.Current.Generation);
        Assert.Equal("execution-readiness-probe-mp-failed", supervisor.Current.DiagnosticCode);
        Assert.Equal(
            WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
        Assert.DoesNotContain(
            supervisor.History,
            snapshot => snapshot.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.CompetingClientSuspected);
        Assert.True(await supervisor.RecoverExecutionAsync());
    }

    [Fact]
    public async Task CancellationDuringVerificationStillQuarantinesAmbiguousOwnership()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("hang-on-verify"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(TimeSpan.FromSeconds(5)));
        using var cancellation = new CancellationTokenSource();

        var starting = supervisor.StartAsync(cancellation.Token);
        _ = await WaitFor(
            supervisor,
            snapshot => snapshot.Connection?.ExecutionReadinessState ==
                WorkerExecutionReadinessState.Verifying);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => starting);

        Assert.Equal("execution-readiness-probe-cancelled", supervisor.Current.DiagnosticCode);
        Assert.Equal(
            WorkerExecutionReadinessState.OperatorRecoveryRequired,
            supervisor.Current.Connection!.ExecutionReadinessState);
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
                snapshot => snapshot.DiagnosticCode == "worker-starting"));

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
    public async Task FullQueueCancellationIsNotAdmittedAndDrainIsObservable()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(queueCapacity: 2));

        Assert.True(await supervisor.StartAsync());
        var active = supervisor.ExecuteAsync(CreateCommand("active"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.ActiveExecutions == 1);
        var queued = new[]
        {
            supervisor.ExecuteAsync(CreateCommand("queued-1")),
            supervisor.ExecuteAsync(CreateCommand("queued-2"))
        };
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.QueuedRequests == 2);
        using var cancellation = new CancellationTokenSource();
        var blocked = supervisor.ExecuteAsync(
            CreateCommand("blocked"),
            cancellation.Token);
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.WaitingForAdmission == 1);

        await cancellation.CancelAsync();
        var rejected = await blocked;
        var completed = await Task.WhenAll([active, .. queued]);
        var drained = await WaitForExecution(
            supervisor,
            snapshot => snapshot.TerminalRequests == 3);

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, rejected.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, rejected.ExecutionDisposition);
        Assert.All(
            completed,
            outcome => Assert.Equal(WorkerExecutionStatus.Completed, outcome.Status));
        Assert.Equal(2, drained.QueueCapacity);
        Assert.Equal(0, drained.QueuedRequests);
        Assert.Equal(0, drained.WaitingForAdmission);
        Assert.Equal(0, drained.ActiveExecutions);
        Assert.Equal(2, drained.PeakQueuedRequests);
        Assert.Equal(3, drained.AdmittedRequests);
        Assert.Equal(3, drained.TerminalRequests);
        Assert.Equal(1, drained.ClientCancellationsBeforeAdmission);
        Assert.Equal(0, drained.ClientCancellationsAfterAdmission);
    }

    [Fact]
    public async Task CancellationAfterAdmissionDrainsToATerminalOutcome()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(queueCapacity: 2));

        Assert.True(await supervisor.StartAsync());
        var active = supervisor.ExecuteAsync(CreateCommand("active"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.ActiveExecutions == 1);
        using var cancellation = new CancellationTokenSource();
        var queued = supervisor.ExecuteAsync(
            CreateCommand("cancelled-after-admission"),
            cancellation.Token);
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.QueuedRequests == 1);

        await cancellation.CancelAsync();
        var cancelled = await queued;
        _ = await active;
        var drained = await WaitForExecution(
            supervisor,
            snapshot => snapshot.TerminalRequests == 2);

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, cancelled.Status);
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            cancelled.ExecutionDisposition);
        Assert.Equal(2, drained.AdmittedRequests);
        Assert.Equal(2, drained.TerminalRequests);
        Assert.Equal(0, drained.QueuedRequests);
        Assert.Equal(0, drained.ActiveExecutions);
        Assert.Equal(1, drained.ClientCancellationsAfterAdmission);
    }

    [Fact]
    public async Task StopWakesCapacityWaitersAndTerminatesEveryAdmission()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("delay-first-execute"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(queueCapacity: 1));

        Assert.True(await supervisor.StartAsync());
        var active = supervisor.ExecuteAsync(CreateCommand("active"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.ActiveExecutions == 1);
        var queued = supervisor.ExecuteAsync(CreateCommand("queued"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.QueuedRequests == 1);
        var waiting = supervisor.ExecuteAsync(CreateCommand("waiting"));
        _ = await WaitForExecution(
            supervisor,
            snapshot => snapshot.WaitingForAdmission == 1);

        var stopping = supervisor.StopAsync();
        var outcomes = await Task.WhenAll(active, queued, waiting);
        await stopping;
        var drained = supervisor.ExecutionSnapshot;

        Assert.Contains(
            outcomes,
            outcome => outcome.DiagnosticCode == "worker-execution-queue-closed" &&
                outcome.ExecutionDisposition == WorkerExecutionDisposition.NotStarted);
        Assert.Equal(2, drained.AdmittedRequests);
        Assert.Equal(2, drained.TerminalRequests);
        Assert.Equal(0, drained.QueuedRequests);
        Assert.Equal(0, drained.WaitingForAdmission);
        Assert.Equal(0, drained.ActiveExecutions);
    }

    [Fact]
    public async Task RepeatedWatchdogReplacementRemainsBoundedAndObservable()
    {
        const int failureCount = 6;
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(
                generation <= failureCount ? "drop-execution-response" : "normal"),
            CreatePolicy(
                maximumRestarts: failureCount,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                lifecycleHistoryCapacity: 16),
            CreateExecutionPolicy(
                watchdogTimeout: TimeSpan.FromMilliseconds(150),
                queueCapacity: 2));

        Assert.True(await supervisor.StartAsync());
        var failures = new List<WorkerExecutionOutcome>();
        for (var index = 0; index < failureCount; index++)
        {
            failures.Add(await supervisor.ExecuteAsync(CreateCommand($"watchdog-{index}")));
        }

        var recovered = await supervisor.ExecuteAsync(CreateCommand("recovered"));
        var snapshot = supervisor.ExecutionSnapshot;

        Assert.All(
            failures,
            outcome =>
            {
                Assert.Equal(WorkerExecutionStatus.WatchdogTimeout, outcome.Status);
                Assert.Equal(
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    outcome.ExecutionDisposition);
            });
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(failureCount + 1, recovered.Generation);
        Assert.Equal(failureCount, snapshot.WatchdogTimeouts);
        Assert.Equal(failureCount + 1, snapshot.AdmittedRequests);
        Assert.Equal(snapshot.AdmittedRequests, snapshot.TerminalRequests);
        Assert.InRange(supervisor.History.Count, 1, 16);
        Assert.Equal(supervisor.Current, supervisor.History[^1]);
    }

    [Fact]
    public async Task RepeatedCrashReplacementRemainsBoundedAndObservable()
    {
        const int failureCount = 8;
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(
                generation <= failureCount ? "crash-on-execute" : "normal"),
            CreatePolicy(
                maximumRestarts: failureCount,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                lifecycleHistoryCapacity: 16),
            CreateExecutionPolicy(queueCapacity: 2));

        Assert.True(await supervisor.StartAsync());
        var failures = new List<WorkerExecutionOutcome>();
        for (var index = 0; index < failureCount; index++)
        {
            failures.Add(await supervisor.ExecuteAsync(CreateCommand($"crash-{index}")));
        }

        var recovered = await supervisor.ExecuteAsync(CreateCommand("recovered"));
        var snapshot = supervisor.ExecutionSnapshot;

        Assert.All(
            failures,
            outcome =>
            {
                Assert.Equal(WorkerExecutionStatus.WorkerFailure, outcome.Status);
                Assert.Equal(
                    WorkerExecutionDisposition.StartedOutcomeUnknown,
                    outcome.ExecutionDisposition);
            });
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(failureCount + 1, recovered.Generation);
        Assert.Equal(failureCount, snapshot.WorkerFailures);
        Assert.Equal(failureCount + 1, snapshot.AdmittedRequests);
        Assert.Equal(snapshot.AdmittedRequests, snapshot.TerminalRequests);
        Assert.InRange(supervisor.History.Count, 1, 16);
        Assert.Equal(supervisor.Current, supervisor.History[^1]);
    }

    [Fact]
    public async Task LifecycleHistoryRetainsOnlyTheReviewedCapacity()
    {
        const int historyCapacity = 6;
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(
                heartbeatInterval: TimeSpan.FromSeconds(10),
                lifecycleHistoryCapacity: historyCapacity));

        for (var cycle = 0; cycle < 5; cycle++)
        {
            Assert.True(await supervisor.StartAsync());
            await supervisor.StopAsync();
            Assert.InRange(supervisor.History.Count, 1, historyCapacity);
            Assert.Equal(supervisor.Current, supervisor.History[^1]);
        }

        Assert.Equal(historyCapacity, supervisor.History.Count);
        Assert.DoesNotContain(
            supervisor.History,
            snapshot => snapshot.DiagnosticCode == "not-started");
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
    public async Task CallerCancellationAfterEnqueueIsUnknownWithoutDesynchronizingThePipe()
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
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            cancelled.ExecutionDisposition);
        Assert.Equal("client-wait-cancelled", cancelled.DiagnosticCode);
        Assert.Equal(WorkerExecutionStatus.Completed, next.Status);
        Assert.Equal(1, next.Generation);
        Assert.Equal(1, supervisor.Current.Generation);
    }

    [Fact]
    public async Task CancellationBeforeEnqueueProvesExecutionDidNotStart()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True(await supervisor.StartAsync());
        using var clientCancellation = new CancellationTokenSource();
        await clientCancellation.CancelAsync();

        var cancelled = await supervisor.ExecuteAsync(
            CreateCommand("cancelled-before-enqueue"),
            clientCancellation.Token);

        Assert.Equal(WorkerExecutionStatus.ClientCancelled, cancelled.Status);
        Assert.Equal(WorkerExecutionDisposition.NotStarted, cancelled.ExecutionDisposition);
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
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            timedOut.ExecutionDisposition);
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
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            failed.ExecutionDisposition);
        Assert.Null(failed.Execution);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(2, recovered.Generation);
        Assert.Contains(
            supervisor.History,
            snapshot => snapshot.State == WorkerLifecycleState.Degraded &&
                snapshot.LastTermination == WorkerTerminationKind.Crash);
    }

    [Theory]
    [InlineData("crash-after-execute", (int)WorkerExecutionStatus.WorkerFailure)]
    [InlineData("drop-execution-response", (int)WorkerExecutionStatus.WatchdogTimeout)]
    public async Task CompletionBeforeCrashOrLostResponseRequiresReconciliation(
        string scenario,
        int expectedStatus)
    {
        await using var supervisor = CreateSupervisor(
            generation => CreateLaunch(generation == 1 ? scenario : "normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)),
            CreateExecutionPolicy(TimeSpan.FromMilliseconds(150)));

        Assert.True(await supervisor.StartAsync());
        var ambiguous = await supervisor.ExecuteAsync(CreateCommand("ambiguous-completion"));
        var recovered = await supervisor.ExecuteAsync(CreateCommand("after-ambiguous-completion"));

        Assert.Equal((WorkerExecutionStatus)expectedStatus, ambiguous.Status);
        Assert.Equal(
            WorkerExecutionDisposition.StartedOutcomeUnknown,
            ambiguous.ExecutionDisposition);
        Assert.Null(ambiguous.Execution);
        Assert.Equal(1, ambiguous.Generation);
        Assert.Equal(WorkerExecutionStatus.Completed, recovered.Status);
        Assert.Equal(WorkerExecutionDisposition.Completed, recovered.ExecutionDisposition);
        Assert.Equal(2, recovered.Generation);
    }


    [Fact]
    public async Task RequestedOutputArgumentsRoundTripAcrossTheWorkerPipe()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True(await supervisor.StartAsync());
        var result = await supervisor.ExecuteAsync(CreateCommand("mixed-outputs"));

        Assert.Equal(WorkerExecutionStatus.Completed, result.Status);
        var outputs = result.Execution!.OutputValues;
        Assert.Equal(7, outputs.Count);
        Assert.All(outputs, output => Assert.True(output.Retrieved));
        Assert.Equal(
            1.25,
            Assert.Single(outputs, output => output.Name == "Planar Offset")
                .DoubleValue);
        Assert.Equal(
            "scripted-output",
            Assert.Single(outputs, output => output.Name == "Working Directory")
                .StringValue);

        var point = Assert.Single(outputs, output => output.Name == "Point Name")
            .PointNameValue;
        Assert.Equal("Collection", point!.CollectionName);
        Assert.Equal("Group", point.GroupName);
        Assert.Equal("Point", point.TargetName);

        var vector = Assert.Single(
            outputs,
            output => output.Name == "Component Weights").VectorValue;
        Assert.Equal(new WorkerVectorValue(1, 2, 3), vector);

        var tolerance = Assert.Single(
            outputs,
            output => output.Name == "Position Tolerance")
            .ToleranceVectorOptionsValue;
        Assert.True(tolerance!.HighX.Enabled);
        Assert.Equal(1, tolerance.HighX.Value);
        Assert.False(tolerance.LowMagnitude.Enabled);
        Assert.Equal(-4, tolerance.LowMagnitude.Value);
    }

    [Fact]
    public async Task IdentityReferenceValuesRoundTripAcrossTheWorkerPipe()
    {
        await using var supervisor = CreateSupervisor(
            _ => CreateLaunch("normal"),
            CreatePolicy(heartbeatInterval: TimeSpan.FromSeconds(10)));

        Assert.True(await supervisor.StartAsync());
        var result = await supervisor.ExecuteAsync(CreateIdentityReferenceCommand());

        Assert.Equal(WorkerExecutionStatus.Completed, result.Status);
        var outputs = result.Execution!.OutputValues;
        Assert.Equal(7, outputs.Count);
        Assert.All(outputs, output => Assert.True(output.Retrieved));
        Assert.Equal(
            17,
            outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionInstrumentId)
                .CollectionInstrumentIdValue!.InstrumentId);
        Assert.Equal(
            WorkerItemTypeValue.Picture,
            outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionItemName)
                .CollectionItemNameValue!.ItemType);
        Assert.Equal(
            WorkerItemTypeValue.SaReport,
            outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionItemNameList)
                .CollectionItemNameListValue!.Values[0].ItemType);
        Assert.Equal(
            WorkerObjectTypeValue.PointGroup,
            outputs.Single(value => value.Kind == WorkerMpValueKind.CollectionObjectName)
                .CollectionObjectNameValue!.ObjectType);
        Assert.Equal(
            "Point",
            outputs.Single(value => value.Kind == WorkerMpValueKind.PointNameList)
                .PointNameListValue!.Values[0].TargetName);
        Assert.Equal(
            ["A", "B"],
            outputs.Single(value => value.Kind == WorkerMpValueKind.StringList)
                .StringListValue!.Values);
        Assert.Equal(
            "Vector",
            outputs.Single(value => value.Kind == WorkerMpValueKind.VectorNameList)
                .VectorNameListValue!.Values[0].VectorName);
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
        Assert.Equal(3, result.Execution.MpResultCode);
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
        Assert.Equal(
            WorkerExecutionReadinessState.Unverified,
            supervisor.Current.Connection.ExecutionReadinessState);
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


    private static WorkerMpCommand CreateIdentityReferenceCommand() =>
        new(
            "identity-reference-pipe",
            "Identity Reference Pipe",
            [
                new WorkerMpInputArgument(
                    "Item",
                    WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameValue: new WorkerCollectionItemNameValue(
                        "Collection",
                        "Picture",
                        WorkerItemTypeValue.Picture)),
                new WorkerMpInputArgument(
                    "Items",
                    WorkerMpValueKind.CollectionItemNameList,
                    CollectionItemNameListValue: new WorkerCollectionItemNameListValue(
                        [new WorkerCollectionItemNameValue(
                            "Collection",
                            "Report",
                            WorkerItemTypeValue.SaReport)])),
                new WorkerMpInputArgument(
                    "Object",
                    WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameValue: new WorkerCollectionObjectNameValue(
                        "Collection",
                        "Object",
                        WorkerObjectTypeValue.PointGroup)),
                new WorkerMpInputArgument(
                    "Points",
                    WorkerMpValueKind.PointNameList,
                    PointNameListValue: new WorkerPointNameListValue(
                        [new WorkerPointNameValue("Collection", "Group", "Point")])),
                new WorkerMpInputArgument(
                    "Strings",
                    WorkerMpValueKind.StringList,
                    StringListValue: new WorkerStringListValue([])),
                new WorkerMpInputArgument(
                    "Machine",
                    WorkerMpValueKind.CollectionMachineId,
                    CollectionMachineIdValue:
                        new WorkerCollectionMachineIdValue("Collection", 4))
            ],
            [
                new WorkerMpOutputArgument(
                    "Instrument",
                    WorkerMpValueKind.CollectionInstrumentId),
                new WorkerMpOutputArgument(
                    "Item",
                    WorkerMpValueKind.CollectionItemName),
                new WorkerMpOutputArgument(
                    "Items",
                    WorkerMpValueKind.CollectionItemNameList),
                new WorkerMpOutputArgument(
                    "Object",
                    WorkerMpValueKind.CollectionObjectName),
                new WorkerMpOutputArgument(
                    "Points",
                    WorkerMpValueKind.PointNameList),
                new WorkerMpOutputArgument(
                    "Strings",
                    WorkerMpValueKind.StringList),
                new WorkerMpOutputArgument(
                    "Vectors",
                    WorkerMpValueKind.VectorNameList)
            ]);
    private static WorkerMpCommand CreateCommand(string operationId) =>
        new(
            operationId,
            "Scripted Step",
            [
                new WorkerMpInputArgument(
                    "Enabled",
                    WorkerMpValueKind.Logical,
                    BooleanValue: true),
                new WorkerMpInputArgument(
                    "Count",
                    WorkerMpValueKind.WholeNumber,
                    IntegerValue: 2),
                new WorkerMpInputArgument(
                    "Tolerance",
                    WorkerMpValueKind.FloatingPoint,
                    DoubleValue: 0.01),
                new WorkerMpInputArgument(
                    "Label",
                    WorkerMpValueKind.Text,
                    StringValue: "portable-test"),
                new WorkerMpInputArgument(
                    "Point Name",
                    WorkerMpValueKind.PointName,
                    PointNameValue: new WorkerPointNameValue("", "", "")),
                new WorkerMpInputArgument(
                    "Direction",
                    WorkerMpValueKind.Vector,
                    VectorValue: new WorkerVectorValue(1, 0, 0)),
                new WorkerMpInputArgument(
                    "Position Tolerance",
                    WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsValue: CreateToleranceVectorOptions())
            ],
            [
                new WorkerMpOutputArgument("Enabled Result", WorkerMpValueKind.Logical),
                new WorkerMpOutputArgument("Count Result", WorkerMpValueKind.WholeNumber),
                new WorkerMpOutputArgument("Planar Offset", WorkerMpValueKind.FloatingPoint),
                new WorkerMpOutputArgument("Working Directory", WorkerMpValueKind.Text),
                new WorkerMpOutputArgument("Point Name", WorkerMpValueKind.PointName),
                new WorkerMpOutputArgument("Component Weights", WorkerMpValueKind.Vector),
                new WorkerMpOutputArgument(
                    "Position Tolerance",
                    WorkerMpValueKind.ToleranceVectorOptions)
            ]);

    private static WorkerToleranceVectorOptionsValue CreateToleranceVectorOptions() =>
        new(
            new WorkerToleranceLimit(Enabled: true, Value: 1),
            new WorkerToleranceLimit(Enabled: true, Value: 2),
            new WorkerToleranceLimit(Enabled: true, Value: 3),
            new WorkerToleranceLimit(Enabled: true, Value: 4),
            new WorkerToleranceLimit(Enabled: false, Value: -1),
            new WorkerToleranceLimit(Enabled: false, Value: -2),
            new WorkerToleranceLimit(Enabled: false, Value: -3),
            new WorkerToleranceLimit(Enabled: false, Value: -4));

    private static WorkerExecutionPolicy CreateExecutionPolicy(
        TimeSpan? watchdogTimeout = null,
        int queueCapacity = 16) =>
        new(
            watchdogTimeout ?? TimeSpan.FromSeconds(2),
            queueCapacity);

    private static WorkerRestartPolicy CreatePolicy(
        int maximumRestarts = 3,
        TimeSpan? heartbeatInterval = null,
        TimeSpan? shutdownTimeout = null,
        int lifecycleHistoryCapacity = 256) =>
        new(
            maximumRestarts,
            restartWindow: TimeSpan.FromSeconds(10),
            heartbeatInterval ?? TimeSpan.FromMilliseconds(50),
            heartbeatTimeout: TimeSpan.FromMilliseconds(250),
            startupTimeout: TimeSpan.FromSeconds(5),
            shutdownTimeout ?? TimeSpan.FromMilliseconds(500),
            restartDelay: TimeSpan.FromMilliseconds(10),
            lifecycleHistoryCapacity);

    private static async Task<WorkerExecutionSnapshot> WaitForExecution(
        WorkerProcessSupervisor supervisor,
        Func<WorkerExecutionSnapshot, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var snapshot = supervisor.ExecutionSnapshot;
            if (predicate(snapshot))
            {
                return snapshot;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

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
